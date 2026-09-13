using FatControllerExample.DTOs;
using FatControllerExample.Services;
using Microsoft.AspNetCore.Mvc;

namespace FatControllerExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }


        [HttpGet("providers")]
        public IActionResult GetAvailableProviders()
        {
            var providers = _paymentService.GetAvailableProviders();
            return Ok(providers);
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var paymentDetails = new PaymentDetails
                {
                    Amount = request.Amount,
                    CustomerEmail = request.CustomerEmail,
                    CustomerName = request.CustomerName,
                    OrderId = request.OrderId,
                    AdditionalData = request.AdditionalData
                };

                var result = await _paymentService.ProcessPaymentAsync(request.ProviderName, paymentDetails);

                var response = new PaymentResponse
                {
                    IsSuccess = result.IsSuccess,
                    TransactionId = result.TransactionId,
                    ErrorMessage = result.ErrorMessage,
                    Amount = result.Amount,
                    ProcessedAt = result.ProcessedAt,
                    ProviderName = request.ProviderName
                };

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Платеж успешно обработан. TransactionId: {TransactionId}", result.TransactionId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Платеж не удался: {ErrorMessage}", result.ErrorMessage);
                    return BadRequest(response);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Невалидные данные в запросе платежа");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке платежа через провайдер '{Provider}'", request.ProviderName);
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }
    }
}