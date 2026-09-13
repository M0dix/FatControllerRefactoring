namespace FatControllerExample.Services
{
    // способ оплаты криптовалютой
    public class CryptoPaymentProvider : IPaymentProvider
    {
        public string Name => "crypto";

        public async Task<PaymentResult> ChargeAsync(PaymentDetails paymentDetails)
        {
            await Task.Delay(200);

            var walletAddress = paymentDetails.AdditionalData.TryGetValue("WalletAddress", out var wallet)
                ? wallet
                : "не указан";

            var isSuccess = new Random().NextDouble() > 0.2;

            return new PaymentResult
            {
                IsSuccess = isSuccess,
                TransactionId = isSuccess ? Guid.NewGuid().ToString() : "",
                ErrorMessage = isSuccess ? "" : $"Ошибка при оплате криптовалютой (кошелек: {walletAddress})",
                Amount = paymentDetails.Amount,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}