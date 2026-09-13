using Microsoft.Extensions.Primitives;

namespace FatControllerExample.Middlewares.Idempotency
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly ILogger<IdempotencyMiddleware> _logger;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);
        private readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(5);


        public IdempotencyMiddleware(
            RequestDelegate next,
            IIdempotencyStore idempotencyStore,
            ILogger<IdempotencyMiddleware> logger)
        {
            _next = next;
            _idempotencyStore = idempotencyStore;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!ShouldProcessRequest(context))
            {
                await _next(context);
                return;
            }

            var idempotencyKey = ExtractIdempotencyKey(context);
            if (string.IsNullOrEmpty(idempotencyKey))
            {
                await _next(context);
                return;
            }

            var cachedResult = await _idempotencyStore.TryGetAsync(idempotencyKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Найден уже обработаннй результат для ключа идемпотентности: {Key}", idempotencyKey);
                await WriteCachedResponse(context, cachedResult);
                return;
            }

            var lockAcquired = await _idempotencyStore.TryAcquireLockAsync(idempotencyKey, _lockTimeout);
            if (!lockAcquired)
            {
                _logger.LogWarning("Не удалось получить лок для ключа идемпотентности: {Key}. Возможно другой запрос обрабатывается.", idempotencyKey);
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsync("Запрос с таким ключом идемпотентности уже обрабатывается");
                return;
            }

            try
            {
                var originalBodyStream = context.Response.Body;

                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                await SaveIdempotencyResult(context, idempotencyKey, responseBodyStream, originalBodyStream);
            }
            finally
            {
                await _idempotencyStore.ReleaseLockAsync(idempotencyKey);
            }
        }

        // не включаем в идемпотентность специфичные запросы
        private bool ShouldProcessRequest(HttpContext context)
        {
            var path = context.Request.Path.ToString();

            if (path.Contains("health") || path.Contains("ping"))
            {
                return false;
            }

            return true;
        }

        private string? ExtractIdempotencyKey(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("Idempotency-Key", out StringValues keyValues) &&
                !string.IsNullOrEmpty(keyValues.FirstOrDefault()))
            {
                return keyValues.First()!.Trim();
            }

            return null;
        }

        private async Task WriteCachedResponse(HttpContext context, IdempotencyResult cachedResult)
        {
            context.Response.StatusCode = cachedResult.StatusCode;
            context.Response.ContentType = cachedResult.ContentType;

            foreach (var header in cachedResult.Headers)
            {
                context.Response.Headers[header.Key] = header.Value;
            }

            // обозначаем что взято из хранилища
            context.Response.Headers["X-Idempotent-Cached"] = "true";
            context.Response.Headers["X-Idempotent-Processed-At"] = cachedResult.CreatedAt.ToString("o");

            await context.Response.Body.WriteAsync(cachedResult.ResponseBody);
        }

        private async Task SaveIdempotencyResult(
            HttpContext context,
            string idempotencyKey,
            MemoryStream responseBodyStream,
            Stream originalBodyStream)
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            await responseBodyStream.CopyToAsync(originalBodyStream);

            context.Response.Body = originalBodyStream;

            // не сохраняем результат для серверных ошибок 
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 500)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);

                var result = new IdempotencyResult
                {
                    StatusCode = context.Response.StatusCode,
                    ResponseBody = responseBodyStream.ToArray(),
                    ContentType = context.Response.ContentType ?? "application/json",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(_defaultExpiration)
                };

                foreach (var header in context.Response.Headers)
                {
                    result.Headers[header.Key] = header.Value.ToString();
                }

                try
                {
                    await _idempotencyStore.SetAsync(idempotencyKey, result, _defaultExpiration);
                    _logger.LogInformation("Сохранен результат для ключа идемпотентности: {Key}, статус: {StatusCode}",
                        idempotencyKey, context.Response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при сохранении результата для ключа идемпотентности: {Key}", idempotencyKey);
                }
            }
            else
            {
                _logger.LogDebug("Не сохраняем результат для ключа {Key} с статусом {StatusCode}",
                    idempotencyKey, context.Response.StatusCode);
            }
        }
    }
}