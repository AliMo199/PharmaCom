using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Domain.Models;
using PharmaCom.Service.Interfaces;
using System.Security.Claims;

namespace PharmaCom.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }



        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.GetOrCreateUserCartAsync(userId);
                return Ok(new { success = true, data = cart });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cartItem = await _cartService.AddToCartAsync(userId, request.ProductId, request.Quantity);
                return Ok(new { success = true, message = "Product added to cart successfully", data = cartItem });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cartItem = await _cartService.UpdateCartItemAsync(userId, request.ProductId, request.Quantity);
                if (cartItem == null)
                    return Ok(new { success = true, message = "Item removed from cart" });
                return Ok(new { success = true, message = "Cart item updated successfully", data = cartItem });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _cartService.RemoveFromCartAsync(userId, productId);
                return Ok(new { success = true, message = "Product removed from cart successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _cartService.ClearCartAsync(userId);
                return Ok(new { success = true, message = "Cart cleared successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("total")]
        public async Task<IActionResult> GetCartTotal()
        {
            try
            {
                var userId = GetCurrentUserId();
                var total = await _cartService.CalculateCartTotalAsync(userId);
                return Ok(new { success = true, data = new { total } });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartItemsCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _cartService.GetCartItemsCountAsync(userId);
                return Ok(new { success = true, data = new { count } });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetCartItems()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.GetOrCreateUserCartAsync(userId);

                if (cart == null || cart.Items == null || !cart.Items.Any())
                    return Ok(new { success = true, data = new List<CartItem>() });

                return Ok(new { success = true, data = cart.Items });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }


        private string GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }
            return userId;
        }



        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class UpdateCartItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}