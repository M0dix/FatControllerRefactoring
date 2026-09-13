using FatControllerExample.DTOs;
using FatControllerExample.Services;
using Microsoft.AspNetCore.Mvc;

namespace FatControllerExample.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class RefactoredFatOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<RefactoredFatOrderController> _logger;

        public RefactoredFatOrderController(
            IOrderService orderService,
            ILogger<RefactoredFatOrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder(OrderRequest req)
        {
            // валидация модели 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // вызов сервиса
                var order = await _orderService.CreateOrderAsync(req);

                // маппинг ответа
                var response = new OrderResponse
                {
                    OrderId = order.Id,
                    TotalAmount = order.TotalAmount,
                    Discount = order.Discount,
                    FinalAmount = order.FinalAmount,
                    Status = order.Status,
                    CreatedAt = order.CreatedAt
                };

                _logger.LogInformation("Заказ {OrderId} успешно создан для {CustomerEmail}",
                    order.Id, req.CustomerEmail);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Невалидные данные в запросе от {CustomerEmail}", req.CustomerEmail);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа для {CustomerEmail}", req.CustomerEmail);
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }
    }
}