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
            _UnitOfWork = unitOfWork;
        }

        public async Task<Cart> GetOrCreateUserCartAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart != null) return cart;

            var newCart = new Cart
            {
                ApplicationUserId = userId,
                Items = new List<CartItem>()
            };

            await _UnitOfWork.Cart.AddAsync(newCart);
            _UnitOfWork.Save();
            return newCart;
        }

        public async Task<CartItem> AddToCartAsync(string userId, int productId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));
            if (quantity <= 0)
                throw new ArgumentException("quantity must be greater than zero", nameof(quantity));

            var cart = await GetOrCreateUserCartAsync(userId);

            var existingItem = await _UnitOfWork.Cart.GetCartItemByProductAsync(cart.Id, productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _UnitOfWork.CartItem.Update(existingItem);
                _UnitOfWork.Save();
                return existingItem;
            }

            var newItem = new CartItem
            {
                ProductId = productId,
                CartId = cart.Id,
                Quantity = quantity
            };

            await _UnitOfWork.CartItem.AddAsync(newItem);
            _UnitOfWork.Save();
            return newItem;
        }

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
                _UnitOfWork.CartItem.Remove(item);
                _UnitOfWork.Save();
                return null!;
            }

            item.Quantity = quantity;
            _UnitOfWork.CartItem.Update(item);
            _UnitOfWork.Save();
            return item;
        }


        public async Task RemoveFromCartAsync(string userId, int productId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null) return;

            var item = await _UnitOfWork.Cart.GetCartItemByProductAsync(cart.Id, productId);
            if (item == null) return;

            _UnitOfWork.CartItem.Remove(item);
            _UnitOfWork.Save();
        }


        public async Task ClearCartAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null) return;

            var items = await _UnitOfWork.CartItem.GetCartItemsByCartIdAsync(cart.Id);
            if (items == null) return;
            _UnitOfWork.CartItem.RemoveRange(items);
            _UnitOfWork.Save();
        }


        public async Task<decimal> CalculateCartTotalAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var cart = await _UnitOfWork.Cart.GetCartByUserIdAsync(userId);
            if (cart == null) return 0m;

            var cartWithItems = await _UnitOfWork.Cart.GetCartWithItemsAsync(cart.Id);
            if (cartWithItems == null || cartWithItems.Items == null || !cartWithItems.Items.Any())
                return 0m;

            decimal total = 0m;

            foreach (var item in cartWithItems.Items)
            {
                Product? product = item.Product;
                if (product == null)
                {
                    var ItemWithProduct = await _UnitOfWork.CartItem.GetCartItemWithProductAsync(item.Id);
                    product = ItemWithProduct?.Product;
                }

                if (product == null)
                {
                    var prod = await _UnitOfWork.Product.GetByIdAsync(item.ProductId);
                    product = prod;
                }

                if (product == null)
                    continue;

                total += product.Price * item.Quantity;
            }

            return total;
        }


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
