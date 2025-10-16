using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.Static;
using PharmaCom.Service.Interfaces;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Service.Implementation
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;

        public OrderService(IUnitOfWork unitOfWork, ICartService cartService)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
        }

        public async Task<Order> CreateOrderFromCartAsync(string userId, int addressId)
        {
            var cart = await _cartService.GetOrCreateUserCartAsync(userId);
            var cartItems = await _unitOfWork.CartItem.GetCartItemsByCartIdAsync(cart.Id);

            if (!cartItems.Any())
                throw new InvalidOperationException("Cart is empty");

            var address = await _unitOfWork.Address.GetByIdAsync(addressId);
            if (address == null)
                throw new ArgumentException("Address not found");

            var prescriptionRequiredProducts = cartItems
                .Where(ci => ci.Product.IsRxRequired)
                .Select(ci => ci.Product.Name)
                .ToList();

            if (prescriptionRequiredProducts.Any())
            {
                throw new InvalidOperationException(
                $"The following products require a prescription: {string.Join(", ", prescriptionRequiredProducts)}. " +
                "Please remove them from your cart or upload a prescription before checkout.");
            }

            var order = new Order
            {
                ApplicationUserId = userId,
                AddressId = addressId,
                OrderDate = DateTime.UtcNow,
                Status = ST.Pending,
                TotalAmount = await _cartService.CalculateCartTotalAsync(userId),
                OrderItems = new List<OrderItem>()
            };

            foreach (var cartItem in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity
                });
            }

            await _unitOfWork.Order.AddAsync(order);
            _unitOfWork.Save();

            await _cartService.ClearCartAsync(userId);

            return order;
        }

        public async Task<Session> CreateStripeCheckoutSessionAsync(int orderId, string successUrl, string cancelUrl)
        {
            var order = await _unitOfWork.Order.GetOrderWithDetailsAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = order.OrderItems.Select(oi => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = oi.Product.Name,
                            Description = oi.Product.Description,
                        },
                        UnitAmount = (long)(oi.Product.Price * 100), // Convert to cents
                    },
                    Quantity = oi.Quantity,
                }).ToList(),
                Mode = "payment",
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                ClientReferenceId = orderId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", orderId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Update order with session ID
            order.SessionId = session.Id;
            _unitOfWork.Order.Update(order);
            _unitOfWork.Save();

            return session;
        }

        public async Task<bool> ProcessStripePaymentSuccessAsync(string sessionId)
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            var order = await _unitOfWork.Order.GetOrderBySessionIdAsync(sessionId);
            if (order == null)
                return false;

            if (session.PaymentStatus == "paid")
            {
                order.Status = ST.PaymentReceived;
                order.PaymentIntentId = session.PaymentIntentId;
                _unitOfWork.Order.Update(order);
                _unitOfWork.Save();
                return true;
            }

            return false;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            if (!ST.Statuses.Contains(status))
                throw new ArgumentException("Invalid order status");

            var order = await _unitOfWork.Order.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            order.Status = status;
            _unitOfWork.Order.Update(order);
            _unitOfWork.Save();
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        {
            return await _unitOfWork.Order.GetOrdersByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
        {
            return await _unitOfWork.Order.GetOrdersByStatusAsync(status);
        }

        public async Task<Order> GetOrderWithDetailsAsync(int orderId)
        {
            return await _unitOfWork.Order.GetOrderWithDetailsAsync(orderId);
        }

        public async Task<Order> GetOrderBySessionIdAsync(string sessionId)
        {
            return await _unitOfWork.Order.GetOrderBySessionIdAsync(sessionId);
        }
    }
}
