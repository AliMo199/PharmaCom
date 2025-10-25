using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Service.Interfaces;
using PharmaCom.Domain.ViewModels;
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
        private readonly IPrescriptionService _prescriptionService;

        public OrderController(IOrderService orderService, ICartService cartService, IPrescriptionService prescriptionService)
        {
            _orderService = orderService;
            _cartService = cartService;
            _prescriptionService = prescriptionService;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int addressId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartTotal = await _cartService.CalculateCartTotalAsync(userId);
            if (cartTotal == 0)
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }
            try
            {
                var order = await _orderService.CreateOrderFromCartAsync(userId, addressId);
                var domain = $"{Request.Scheme}://{Request.Host}";
                var successUrl = $"{domain}/Order/PaymentConfirmation?session_id={{CHECKOUT_SESSION_ID}}";
                var cancelUrl = $"{domain}/Cart/Index";
                var session = await _orderService.CreateStripeCheckoutSessionAsync(order.Id, successUrl, cancelUrl);
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Cart");
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
                return View("Success", order);
            }

            return View("Failure");
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