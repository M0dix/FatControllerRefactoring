namespace FatControllerExample.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(string providerName, PaymentDetails paymentDetails);
        List<string> GetAvailableProviders();
    }

    public class PaymentService : IPaymentService
    {
        private readonly Dictionary<string, IPaymentProvider> _paymentProviders;

        public PaymentService(IEnumerable<IPaymentProvider> paymentProviders)
        {
            // заполняем словарь по сервисам, зарегистрированным в DI 
            _paymentProviders = paymentProviders.ToDictionary(p => p.Name.ToLowerInvariant(), p => p);
        }

        public async Task<PaymentResult> ProcessPaymentAsync(string providerName, PaymentDetails paymentDetails)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("Имя провайдера не может быть пустым", nameof(providerName));
            }

            var normalizedProviderName = providerName.ToLowerInvariant();

            if (!_paymentProviders.TryGetValue(normalizedProviderName, out var provider))
            {
                throw new ArgumentException($"Провайдер платежей '{providerName}' не найден.");
            }

            try
            {
                return await provider.ChargeAsync(paymentDetails);
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка при обработке платежа через провайдер '{providerName}': {ex.Message}"
                };
            }
        }

        public List<string> GetAvailableProviders()
        {
            return _paymentProviders.Keys.OrderBy(k => k).ToList();
        }
    }
}