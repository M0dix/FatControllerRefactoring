namespace FatControllerExample.Services
{
    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class PaymentDetails
    {
        public decimal Amount { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }

    public interface IPaymentProvider
    {
        string Name { get; }
        Task<PaymentResult> ChargeAsync(PaymentDetails paymentDetails);
    }
}