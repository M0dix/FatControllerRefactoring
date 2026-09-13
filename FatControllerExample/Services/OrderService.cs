using FatControllerExample.DTOs;
using FatControllerExample.Models;
using FatControllerExample.Repositories;

namespace FatControllerExample.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(OrderRequest request);
        Task<Order?> GetOrderAsync(int id);
    }

    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string email, string customerName, int orderId, decimal amount);
    }

    public interface ILoggerService
    {
        void LogInformation(string message, params object[] args);
        void LogError(Exception ex, string message, params object[] args);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEmailService _emailService;
        private readonly ILoggerService _loggerService;

        public OrderService(
            IOrderRepository orderRepository,
            IEmailService emailService,
            ILoggerService loggerService)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
            _loggerService = loggerService;
        }

        public async Task<Order> CreateOrderAsync(OrderRequest request)
        {
            // валидация бизнес-правил
            ValidateOrderRequest(request);

            // расчеты 
            decimal totalAmount = CalculateTotalAmount(request.Items);

            decimal discount = await CalculateDiscountAsync(request.PromoCode, totalAmount);

            decimal finalAmount = totalAmount - discount;

            // создание доменной модели из DTO
            var order = new Order
            {
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                TotalAmount = totalAmount,
                Discount = discount,
                FinalAmount = finalAmount,
                Status = "Pending",
                Items = request.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList()
            };

            try
            {
                // вызов Db-cлоя
                var createdOrder = await _orderRepository.CreateAsync(order);

                await UpdateInventoryAsync(request.Items);

                // вызов другого, еmail сервиса
                await _emailService.SendOrderConfirmationAsync(
                    request.CustomerEmail,
                    request.CustomerName,
                    createdOrder.Id,
                    finalAmount);

                _loggerService.LogInformation(
                    "Заказ {OrderId} создан для {CustomerEmail}. Сумма: {FinalAmount}",
                    createdOrder.Id, request.CustomerEmail, finalAmount);

                return createdOrder;
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Ошибка при создании заказа для {CustomerEmail}", request.CustomerEmail);
                throw;
            }
        }

        public Task<Order?> GetOrderAsync(int id)
        {
            return _orderRepository.GetByIdAsync(id);
        }

        private void ValidateOrderRequest(OrderRequest request)
        {
            if (request.Items.Count == 0)
                throw new ArgumentException("Заказ должен содержать хотя бы один товар");

            if (string.IsNullOrEmpty(request.CustomerEmail))
                throw new ArgumentException("Email обязателен");

            if (string.IsNullOrEmpty(request.CustomerName))
                throw new ArgumentException("Имя обязательно");
        }

        private decimal CalculateTotalAmount(List<OrderItemRequest> items)
        {
            return items.Sum(item => item.Price * item.Quantity);
        }

        private async Task<decimal> CalculateDiscountAsync(string? promoCode, decimal totalAmount)
        {
            if (string.IsNullOrEmpty(promoCode))
                return 0;

            var promo = await _orderRepository.GetPromoCodeAsync(promoCode);

            if (promo == null || !promo.IsActive || promo.ValidUntil < DateTime.UtcNow)
                return 0;

            return totalAmount * promo.DiscountPercent / 100;
        }

        private async Task UpdateInventoryAsync(List<OrderItemRequest> items)
        {
            foreach (var item in items)
            {
                await _orderRepository.UpdateProductStockAsync(item.ProductId, -item.Quantity);
            }
        }
    }
}