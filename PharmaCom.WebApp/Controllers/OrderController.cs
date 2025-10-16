using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Service.Interfaces;
using PharmaCom.WebApp.ViewModels; // <-- Using the new ViewModels
using Stripe.Checkout;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmaCom.WebApp.Controllers
{
    //[Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: /Order
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ordersFromDb = await _orderService.GetUserOrdersAsync(userId);

            // Convert DB models to ViewModels
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

            // Convert the detailed DB model to the detailed ViewModel
            var orderDetailViewModel = new OrderDetailViewModel
            {
                Id = orderFromDb.Id,
                OrderDate = orderFromDb.OrderDate,
                Status = orderFromDb.Status,
                TotalAmount = orderFromDb.TotalAmount,

                // --- THIS IS THE FIX ---
                // We check if the address is null before trying to read from it.
                ShippingAddress = orderFromDb.Address != null ? new AddressViewModel
                {
                    Line1 = orderFromDb.Address.Line1,
                    City = orderFromDb.Address.City,
                    Governorate = orderFromDb.Address.Governorate
                } : new AddressViewModel(), // If it's null, create an empty AddressViewModel

                OrderItems = orderFromDb.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Product.Price
                }).ToList()
            };

            return View(orderDetailViewModel);
        }

        // POST: /Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int addressId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                return BadRequest("Session ID is required.");
            }

            var success = await _orderService.ProcessStripePaymentSuccessAsync(session_id);

            if (success)
            {
                var order = await _orderService.GetOrderBySessionIdAsync(session_id);
                return View("Success", order);
            }

            return View("Failure");
        }
    }
}