namespace FatControllerExample.Services
{
    // способ оплаты банковской картой
    public class CardPaymentProvider : IPaymentProvider
    {
        public string Name => "card";

        public async Task<PaymentResult> ChargeAsync(PaymentDetails paymentDetails)
        {
            await Task.Delay(100);

            var hasCardPayToken = paymentDetails.AdditionalData.TryGetValue("AuthToken", out var token)
               && !string.IsNullOrEmpty(token);

            var isSuccess = new Random().NextDouble() > 0.1;

            return new PaymentResult
            {
                IsSuccess = isSuccess,
                TransactionId = isSuccess ? Guid.NewGuid().ToString() : "",
                ErrorMessage = isSuccess ? "" : "Ошибка при оплате картой",
                Amount = paymentDetails.Amount,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}