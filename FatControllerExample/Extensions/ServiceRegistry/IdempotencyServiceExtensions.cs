using FatControllerExample.Middlewares.Idempotency;

namespace FatControllerExample.Extensions.ServiceRegistry
{
    public static class IdempotencyServiceExtensions
    {
        public static IServiceCollection AddIdempotency(this IServiceCollection services)
        {
            services.AddMemoryCache();

            services.AddSingleton<IIdempotencyStore, SafeMemoryIdempotencyStore>();

            return services;
        }

        public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
        {
            return app.UseMiddleware<IdempotencyMiddleware>();
        }
    }
}