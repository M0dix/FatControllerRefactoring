using FatControllerExample.Services;

namespace FatControllerExample.Extensions.ServiceRegistry
{
    public static class PaymentServiceExtensions
    {
        public static IServiceCollection AddPaymentServices(this IServiceCollection services)
        {
            services.AddTransient<IPaymentProvider, CardPaymentProvider>();
            services.AddTransient<IPaymentProvider, SbpPaymentProvider>();
            services.AddTransient<IPaymentProvider, CryptoPaymentProvider>();
            services.AddTransient<IPaymentProvider, ApplePayPaymentProvider>();

            services.AddTransient<IPaymentService, PaymentService>();

            return services;
        }
    }
}