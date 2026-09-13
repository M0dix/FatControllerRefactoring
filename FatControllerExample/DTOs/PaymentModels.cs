using System.ComponentModel.DataAnnotations;

namespace FatControllerExample.DTOs
{
    public class PaymentRequest
    {
        [Required(ErrorMessage = "Имя провайдера обязательно")]
        [StringLength(20, ErrorMessage = "Имя провайдера не должно превышать 20 символов")]
        public string ProviderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Сумма платежа обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Email клиента обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя клиента обязательно")]
        [StringLength(100, ErrorMessage = "Имя не должно превышать 100 символов")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID заказа обязателен")]
        public string OrderId { get; set; } = string.Empty;

        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }

    public class PaymentResponse
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ProviderName { get; set; } = string.Empty;
    }
}