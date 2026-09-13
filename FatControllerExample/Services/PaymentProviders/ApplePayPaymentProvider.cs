namespace FatControllerExample.Services
{
    // способ оплаты apple pay
    public class ApplePayPaymentProvider : IPaymentProvider
    {
        public string Name => "applepay";

        public async Task<PaymentResult> ChargeAsync(PaymentDetails paymentDetails)
        {
            await Task.Delay(80);
            var hasApplePayToken = paymentDetails.AdditionalData.TryGetValue("AuthToken", out var token)
                && !string.IsNullOrEmpty(token);

            if (!hasApplePayToken)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Авторизационный токен отсутствует",
                    Amount = paymentDetails.Amount,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            var isSuccess = new Random().NextDouble() > 0.03;

            return new PaymentResult
            {
                IsSuccess = isSuccess,
                TransactionId = isSuccess ? Guid.NewGuid().ToString() : "",
                ErrorMessage = isSuccess ? "" : $"Ошибка при оплате через Apple Pay",
                Amount = paymentDetails.Amount,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}