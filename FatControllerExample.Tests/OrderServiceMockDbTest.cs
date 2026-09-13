using FatControllerExample.DTOs;
using FatControllerExample.Models;
using FatControllerExample.Repositories;
using FatControllerExample.Services;
using Moq;
using Xunit;

namespace FatControllerExample.Tests
{
    /// <summary>
    /// Минимальный тест для демонстрации тестирования с моком репозитория (БД)
    /// Требование задания A1: показать, что сервис тестируется с моком репозитория (без БД)
    /// </summary>
    public class OrderServiceMockDbTest
    {
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ILoggerService> _loggerServiceMock;
        private readonly OrderService _orderService;

        public OrderServiceMockDbTest()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _loggerServiceMock = new Mock<ILoggerService>();
            _orderService = new OrderService(
                _orderRepositoryMock.Object,
                _emailServiceMock.Object,
                _loggerServiceMock.Object);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldCreateOrder_WithMockedRepository()
        {
            // Arrange - настраиваем моки (имитация БД без реальной базы)
            var orderRequest = new OrderRequest
            {
                CustomerName = "Иван Иванов",
                CustomerEmail = "ivan@example.com",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, ProductName = "Товар A", Price = 100, Quantity = 2 }
                }
            };

            // Мок репозитория (без реальной БД)
            _orderRepositoryMock
                .Setup(repo => repo.GetPromoCodeAsync(It.IsAny<string>()))
                .ReturnsAsync((PromoCode?)null);

            var createdOrder = new Order
            {
                Id = 123,
                CustomerName = "Иван Иванов",
                CustomerEmail = "ivan@example.com",
                TotalAmount = 200,
                Discount = 0,
                FinalAmount = 200,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _orderRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Order>()))
                .ReturnsAsync(createdOrder);

            _orderRepositoryMock
                .Setup(repo => repo.UpdateProductStockAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act - вызываем сервис
            var result = await _orderService.CreateOrderAsync(orderRequest);

            // Assert - проверяем результат
            Assert.NotNull(result);
            Assert.Equal(123, result.Id);
            Assert.Equal(200, result.TotalAmount);

            // Проверяем вызовы моков (имитация работы с БД)
            _orderRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Order>()), Times.Once);
            _orderRepositoryMock.Verify(repo => repo.UpdateProductStockAsync(1, -2), Times.Once);
            _emailServiceMock.Verify(service => service.SendOrderConfirmationAsync(
                "ivan@example.com",
                "Иван Иванов",
                123,
                200), Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldApplyDiscount_WithMockedPromoCode()
        {
            // Arrange - демонстрация мока промо-кода из БД
            var orderRequest = new OrderRequest
            {
                CustomerName = "Мария Петрова",
                CustomerEmail = "maria@example.com",
                PromoCode = "SUMMER20",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, ProductName = "Товар A", Price = 1000, Quantity = 1 }
                }
            };

            // Мок промо-кода из БД
            var promoCode = new PromoCode
            {
                Id = 1,
                Code = "SUMMER20",
                DiscountPercent = 20,
                IsActive = true,
                ValidUntil = DateTime.UtcNow.AddDays(30)
            };

            _orderRepositoryMock
                .Setup(repo => repo.GetPromoCodeAsync("SUMMER20"))
                .ReturnsAsync(promoCode);

            var createdOrder = new Order
            {
                Id = 456,
                CustomerName = "Мария Петрова",
                CustomerEmail = "maria@example.com",
                TotalAmount = 1000,
                Discount = 200, // 20% от 1000
                FinalAmount = 800,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _orderRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Order>()))
                .ReturnsAsync(createdOrder);

            _orderRepositoryMock
                .Setup(repo => repo.UpdateProductStockAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.CreateOrderAsync(orderRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(456, result.Id);
            Assert.Equal(1000, result.TotalAmount);
            Assert.Equal(200, result.Discount); // Проверяем скидку
            Assert.Equal(800, result.FinalAmount);
        }
    }
}