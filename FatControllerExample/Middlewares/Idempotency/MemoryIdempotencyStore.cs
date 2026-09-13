using Microsoft.Extensions.Caching.Memory;

namespace FatControllerExample.Middlewares.Idempotency
{
    public class MemoryIdempotencyStore : IIdempotencyStore, IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly Dictionary<string, SemaphoreSlim> _keyLocks;
        private readonly ILogger<MemoryIdempotencyStore> _logger;

        public MemoryIdempotencyStore(IMemoryCache cache, ILogger<MemoryIdempotencyStore> logger)
        {
            _cache = cache;
            _keyLocks = new Dictionary<string, SemaphoreSlim>();
            _logger = logger;
        }

        public async Task<IdempotencyResult?> TryGetAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Не передан ключ", nameof(key));
            }

            try
            {
                if (_cache.TryGetValue(GetCacheKey(key), out IdempotencyResult? cachedResult))
                {
                    if (cachedResult != null && cachedResult.ExpiresAt > DateTime.UtcNow)
                    {
                        return cachedResult;
                    }
                    else
                    {
                        _cache.Remove(GetCacheKey(key));
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении результата по ключу идемпотентности: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync(string key, IdempotencyResult result, TimeSpan expiration)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Не передан ключ", nameof(key));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            try
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.Add(expiration)
                };

                _cache.Set(GetCacheKey(key), result, cacheOptions);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> TryAcquireLockAsync(string key, TimeSpan lockTimeout)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Не передан ключ", nameof(key));
            }

            lock (_keyLocks)
            {
                if (!_keyLocks.ContainsKey(key))
                {
                    _keyLocks[key] = new SemaphoreSlim(1, 1);
                }
            }

            var semaphore = _keyLocks[key];
            var acquired = await semaphore.WaitAsync(lockTimeout);

            return acquired;
        }

        public Task ReleaseLockAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Не передан ключ", nameof(key));
            }

            lock (_keyLocks)
            {
                if (_keyLocks.TryGetValue(key, out var semaphore))
                {
                    try
                    {
                        semaphore.Release();
                        _logger.LogDebug("Лок освобожден для ключа: {Key}", key);

                        if (semaphore.CurrentCount == 1)
                        {
                            semaphore.Dispose();
                            _keyLocks.Remove(key);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        _keyLocks.Remove(key);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private string GetCacheKey(string key) => $"Idempotency:{key}";

        public void Dispose()
        {
            lock (_keyLocks)
            {
                foreach (var semaphore in _keyLocks.Values)
                {
                    try
                    {
                        semaphore.Dispose();
                    }
                    catch
                    {

                    }
                }
                _keyLocks.Clear();
            }
        }
    }

    // декорируем обычный стор
    public class SafeMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly MemoryIdempotencyStore _innerStore;
        private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(30);

        public SafeMemoryIdempotencyStore(IMemoryCache cache, ILogger<MemoryIdempotencyStore> logger)
        {
            _innerStore = new MemoryIdempotencyStore(cache, logger);
        }

        public async Task<IdempotencyResult?> TryGetAsync(string key)
        {
            using var cts = new CancellationTokenSource(_operationTimeout);
            return await _innerStore.TryGetAsync(key);
        }

        public async Task SetAsync(string key, IdempotencyResult result, TimeSpan expiration)
        {
            using var cts = new CancellationTokenSource(_operationTimeout);
            await _innerStore.SetAsync(key, result, expiration);
        }

        public async Task<bool> TryAcquireLockAsync(string key, TimeSpan lockTimeout)
        {
            using var cts = new CancellationTokenSource(_operationTimeout.Add(lockTimeout));
            return await _innerStore.TryAcquireLockAsync(key, lockTimeout);
        }

        public async Task ReleaseLockAsync(string key)
        {
            using var cts = new CancellationTokenSource(_operationTimeout);
            await _innerStore.ReleaseLockAsync(key);
        }
    }
}