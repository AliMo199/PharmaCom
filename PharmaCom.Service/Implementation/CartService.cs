using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PharmaCom.DataInfrastructure.Implementation;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Service.Interfaces;

namespace PharmaCom.Service.Implementation
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _UnitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }


        /// Get an existing cart for the user or create a new unsaved one.
        public async Task<Cart> GetOrCreateUserCartAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            // Try to get existing cart
            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart != null) return cart;

            var newCart = new Cart
            {
                ApplicationUserId = userId,
                Items = new List<CartItem>()
            };

            // Persist via repository (AddAsync assumed)
            await _UnitOfWork.Cart.AddAsync(newCart);
            return newCart;
        }

        /// Adds a product to the user's cart. If the product already exists in the cart, increases the quantity.
        public async Task<CartItem> AddToCartAsync(string userId, int productId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));
            if (quantity <= 0)
                throw new ArgumentException("quantity must be greater than zero", nameof(quantity));

            var cart = await GetOrCreateUserCartAsync(userId);

            // Ensure we have a cart id; if the cart was newly added but not saved its Id might be 0.
            // Repositories should handle cart-less items if necessary; we'll set CartId regardless.
            var existingItem = await _UnitOfWork.Cart.GetCartItemByProductAsync(cart.Id, productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _UnitOfWork.CartItem.Update(existingItem);
                return existingItem;
            }

            var newItem = new CartItem
            {
                ProductId = productId,
                CartId = cart.Id,
                Quantity = quantity
            };

            await _UnitOfWork.CartItem.AddAsync(newItem);
            return newItem;
        }

        /// Update a cart item's quantity. If quantity <= 0, the item is removed.
        public async Task<CartItem> UpdateCartItemAsync(string userId, int productId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null)
                throw new InvalidOperationException("Cart not found for user.");

            var item = await _UnitOfWork.Cart.GetCartItemByProductAsync(cart.Id, productId);
            if (item == null)
                throw new InvalidOperationException("Cart item not found.");

            if (quantity <= 0)
            {
                // remove item
                _UnitOfWork.CartItem.Remove(item);
                return null!;
            }

            item.Quantity = quantity;
            _UnitOfWork.CartItem.Update(item);
            return item;
        }


        /// Remove a product from the user's cart.
        public async Task RemoveFromCartAsync(string userId, int productId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null) return;

            var item = await _UnitOfWork.Cart.GetCartItemByProductAsync(cart.Id, productId);
            if (item == null) return;

            _UnitOfWork.CartItem.Remove(item);
        }


        /// Clears all items from the user's cart.
        public async Task ClearCartAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null) return;

            // Use repository method to get items (if available)
            var items = await _UnitOfWork.CartItem.GetCartItemsByCartIdAsync(cart.Id);
            if (items == null) return;

            foreach (var item in items.ToList())
            {
                _UnitOfWork.CartItem.Remove(item);
            }
        }


        /// Calculates cart total by summing product price * quantity for each item.
        public async Task<decimal> CalculateCartTotalAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null) return 0m;

            // Ensure we have the cart with items
            var cartWithItems = await _UnitOfWork.Cart.GetCartWithItemsAsync(cart.Id);
            if (cartWithItems == null || cartWithItems.Items == null || !cartWithItems.Items.Any())
                return 0m;

            decimal total = 0m;

            // For each item make sure product price is available
            foreach (var item in cartWithItems.Items)
            {
                // If repository populated Product, use it; otherwise load product via cartItem repo or product repo.
                Product? product = item.Product;
                if (product == null)
                {
                    // try to get cart item with product loaded
                    var ItemWithProduct = await _UnitOfWork.CartItem.GetCartItemWithProductAsync(item.Id);
                    product = ItemWithProduct?.Product;
                }

                if (product == null)
                {
                    // As a fallback, try product repository (may have GetByIdAsync signature).
                    var prod = await _UnitOfWork.Product.GetByIdAsync(item.ProductId);
                    product = prod;
                }

                if (product == null)
                    continue; // safety: skip items with missing product

                total += product.Price * item.Quantity;
            }

            return total;
        }


        /// Proper returning method for items count. Recommended interface change: Task<int> GetCartItemsCountAsync(string userId).
        public async Task<int> GetCartItemsCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null) return 0;

            var items = await _UnitOfWork.CartItem.GetCartItemsByCartIdAsync(cart.Id);
            return items?.Sum(i => i.Quantity) ?? 0;
        }
    }
}
