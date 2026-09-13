namespace FatControllerExample.Services
{
    // способ оплаты через СБП
    public class SbpPaymentProvider : IPaymentProvider
    {
        public string Name => "sbp";

        public async Task<PaymentResult> ChargeAsync(PaymentDetails paymentDetails)
        {
            await Task.Delay(150);

            var phoneNumber = paymentDetails.AdditionalData.TryGetValue("PhoneNumber", out var phone)
                ? phone
                : "не указан";

            var isSuccess = new Random().NextDouble() > 0.05;

            return new PaymentResult
            {
                IsSuccess = isSuccess,
                TransactionId = isSuccess ? Guid.NewGuid().ToString() : "",
                ErrorMessage = isSuccess ? "" : $"Ошибка при оплате через СБП",
                Amount = paymentDetails.Amount,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}