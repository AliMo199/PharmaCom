using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Domain.Models;
using PharmaCom.Service.Interfaces;

namespace PharmaCom.WebApp.Controllers
{
    //[Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ICartService cartService, UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _userManager = userManager;
        }

        // GET: Cart/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login","Account");
                }

                var cart = await _cartService.GetOrCreateUserCartAsync(user.Id);
                var cartTotal = await _cartService.CalculateCartTotalAsync(user.Id);

                ViewBag.CartTotal = cartTotal;
                ViewBag.CurrentPage = "cart";

                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading cart: " + ex.Message;
                return View(new Cart { Items = new List<CartItem>() });
            }
        }

        // POST: Cart/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int productId, int quantity = 1)
        {
            try
            {
                // Check if user is authenticated
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "Please login to add items to cart.";

                    // Get the referring page to return after login
                    var returnUrl = Request.Headers["Referer"].ToString();
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        returnUrl = Url.Action("Store", "Home");
                    }

                    return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please login again.";
                    return RedirectToAction("Login", "Account");
                }

                await _cartService.AddToCartAsync(user.Id, productId, quantity);
                TempData["SuccessMessage"] = "Product added to cart successfully!";

                // Return to previous page or store
                var refererUrl = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(refererUrl) && Url.IsLocalUrl(refererUrl))
                {
                    return Redirect(refererUrl);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding item to cart: " + ex.Message;
                return RedirectToAction("Store", "Home");
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                await _cartService.UpdateCartItemAsync(user.Id, productId, quantity);
                var cartTotal = await _cartService.CalculateCartTotalAsync(user.Id);

                return Json(new { success = true, cartTotal = cartTotal.ToString("F2") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                await _cartService.RemoveFromCartAsync(user.Id, productId);
                var cartTotal = await _cartService.CalculateCartTotalAsync(user.Id);
                var itemCount = await _cartService.GetCartItemsCountAsync(user.Id);

                TempData["SuccessMessage"] = "Item removed from cart.";

                return Json(new { success = true, cartTotal = cartTotal.ToString("F2"), itemCount = itemCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not authenticated";
                    return RedirectToAction("Index");
                }

                await _cartService.ClearCartAsync(user.Id);
                TempData["SuccessMessage"] = "Cart cleared successfully.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error clearing cart: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        // GET: Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = true, count = 0 });
                }

                var count = await _cartService.GetCartItemsCountAsync(user.Id);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, count = 0, message = ex.Message });
            }
        }
    }
}