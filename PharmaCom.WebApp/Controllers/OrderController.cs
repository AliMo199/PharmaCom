using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.Static;
using PharmaCom.Domain.ViewModels;
using PharmaCom.Service.Interfaces;
using Stripe.Checkout;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmaCom.WebApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly IUnitOfWork _UnitOfWork;
        private readonly IPrescriptionService _prescriptionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(IOrderService orderService, ICartService cartService, IPrescriptionService prescriptionService,UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _orderService = orderService;
            _cartService = cartService;
            _prescriptionService = prescriptionService;
            _userManager = userManager;
            _UnitOfWork = unitOfWork;
        }

        // GET: /Order
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ordersFromDb = await _orderService.GetUserOrdersAsync(userId);

            var orderViewModels = ordersFromDb.Select(o => new OrderViewModel
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status
            }).ToList();

            return View(orderViewModels);
        }

        // GET: /Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var orderFromDb = await _orderService.GetOrderWithDetailsAsync(id);
            if (orderFromDb == null)
            {
                return NotFound();
            }

            // Get prescription if exists
            if (orderFromDb.PrescriptionId.HasValue)
            {
                var prescription = await _prescriptionService.GetPrescriptionByIdAsync(orderFromDb.PrescriptionId.Value);
                ViewBag.Prescription = prescription;
            }

            var orderDetailViewModel = new OrderDetailViewModel
            {
                Id = orderFromDb.Id,
                OrderDate = orderFromDb.OrderDate,
                Status = orderFromDb.Status,
                TotalAmount = orderFromDb.TotalAmount,

                ShippingAddress = orderFromDb.Address != null ? new AddressViewModel
                {
                    Line1 = orderFromDb.Address.Line1,
                    City = orderFromDb.Address.City,
                    Governorate = orderFromDb.Address.Governorate
                } : new AddressViewModel(),

                OrderItems = orderFromDb.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Product.Price,
                    IsRxRequired = oi.Product.IsRxRequired
                }).ToList()
            };

            return View(orderDetailViewModel);
        }

        // POST: /Order/Create
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _cartService.GetOrCreateUserCartAsync(user.Id);
            var cartItems = await _UnitOfWork.CartItem.GetCartItemsByCartIdAsync(cart.Id);

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            var userWithAddresses = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (userWithAddresses?.Addresses == null || !userWithAddresses.Addresses.Any())
            {
                TempData["ErrorMessage"] = "Please add a delivery address first.";
                return RedirectToAction("Profile", "Account");
            }

            var address = userWithAddresses.Addresses.FirstOrDefault(a => a.IsDefault)
                          ?? userWithAddresses.Addresses.First();

            // ✅ Check if any items require prescription
            var requiresPrescription = cartItems.Any(ci => ci.Product.IsRxRequired);

            var cartTotal = await _cartService.CalculateCartTotalAsync(user.Id);

            var model = new CheckoutViewModel
            {
                CartItems = cartItems.ToList(),
                ShippingAddress = new AddressViewModel
                {
                    Line1 = address.Line1,
                    City = address.City,
                    Governorate = address.Governorate
                },
                TotalAmount = cartTotal,
                RequiresPrescription = requiresPrescription
            };

            return View(model);
        }

        // ✅ NEW: Process the actual checkout and create order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(int? prescriptionId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var cart = await _cartService.GetOrCreateUserCartAsync(user.Id);
                var cartItems = await _UnitOfWork.CartItem.GetCartItemsByCartIdAsync(cart.Id);

                if (!cartItems.Any())
                {
                    return Json(new { success = false, message = "Cart is empty" });
                }

                var userWithAddresses = await _userManager.Users
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                var address = userWithAddresses?.Addresses.FirstOrDefault(a => a.IsDefault)
                              ?? userWithAddresses?.Addresses.First();

                if (address == null)
                {
                    return Json(new { success = false, message = "No address found" });
                }

                // ✅ Check if prescription is required and provided
                var requiresPrescription = cartItems.Any(ci => ci.Product.IsRxRequired);
                if (requiresPrescription && !prescriptionId.HasValue)
                {
                    return Json(new { success = false, message = "Prescription is required for this order" });
                }

                // ✅ Create the order
                var order = await _orderService.CreateOrderFromCartAsync(user.Id, address.Id);

                // ✅ Associate prescription with order if provided
                if (prescriptionId.HasValue)
                {
                    var prescription = await _prescriptionService.GetPrescriptionByIdAsync(prescriptionId.Value);
                    if (prescription != null && prescription.UploadedByUserId == user.Id)
                    {
                        prescription.OrderId = order.Id;
                        order.PrescriptionId = prescriptionId.Value;

                        _UnitOfWork.Prescription.Update(prescription);
                        _UnitOfWork.Order.Update(order);
                        _UnitOfWork.Save();
                    }
                }

                // ✅ Create Stripe session
                var domain = $"{Request.Scheme}://{Request.Host}";
                var successUrl = $"{domain}/Order/PaymentConfirmation?session_id={{CHECKOUT_SESSION_ID}}";
                var cancelUrl = $"{domain}/Order/CancelPayment?orderId={order.Id}";

                var session = await _orderService.CreateStripeCheckoutSessionAsync(order.Id, successUrl, cancelUrl);

                return Json(new { success = true, redirectUrl = session.Url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Order/PaymentConfirmation
        public async Task<IActionResult> PaymentConfirmation(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                return BadRequest("Invalid Session.");
            }


            var success = await _orderService.ProcessStripePaymentSuccessAsync(session_id);

            if (success)
            {
                var order = await _orderService.GetOrderBySessionIdAsync(session_id);
                return RedirectToAction("Details", new { id = order.Id });
            }

            return View("Failure");
        }
        [HttpGet]
        public async Task<IActionResult> CancelPayment(int orderId)
        {
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order != null && order.Status == ST.Pending)
            {
                await _orderService.DeleteOrderAsync(orderId);
            }

            return RedirectToAction("Index", "Cart");
        }

        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> Manage()
        {
            var pendingOrders = await _orderService.GetOrdersByStatusAsync("Payment Received");
            return View(pendingOrders);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                await _orderService.UpdateOrderStatusAsync(orderId, status);
                TempData["Success"] = $"Order status updated to {status}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating order status: {ex.Message}";
            }

            return RedirectToAction("Manage");
        }
    }
}