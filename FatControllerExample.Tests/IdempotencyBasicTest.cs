using FatControllerExample.Middlewares.Idempotency;
using FatControllerExample.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace FatControllerExample.Tests
{
    /// <summary>
    /// Минимальный тест для демонстрации идемпотентности
    /// Требование задания A3: проверить что повторный запрос с тем же Idempotency-Key возвращает сохраненный результат
    /// </summary>
    public class IdempotencyBasicTest
    {
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<ILogger<MemoryIdempotencyStore>> _loggerMock;
        private readonly IMemoryCache _memoryCache;
        private readonly IIdempotencyStore _idempotencyStore;

        public IdempotencyBasicTest()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _loggerMock = new Mock<ILogger<MemoryIdempotencyStore>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _idempotencyStore = new MemoryIdempotencyStore(_memoryCache, _loggerMock.Object);
        }

        [Fact]
        public async Task IdempotencyStore_ShouldReturnSameResult_ForSameKey()
        {
            // Arrange
            var idempotencyKey = "test-key-123";

            var firstResult = new IdempotencyResult
            {
                StatusCode = 200,
                ResponseBody = Encoding.UTF8.GetBytes(@"{""orderId"":123,""status"":""Pending""}"),
                ContentType = "application/json",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Act: сохраняем результат
            await _idempotencyStore.SetAsync(idempotencyKey, firstResult, TimeSpan.FromHours(24));

            // Act: получаем результат
            var retrievedResult = await _idempotencyStore.TryGetAsync(idempotencyKey);

            // Assert
            Assert.NotNull(retrievedResult);
            Assert.Equal(firstResult.StatusCode, retrievedResult.StatusCode);
            Assert.Equal(firstResult.ResponseBody, retrievedResult.ResponseBody);
            Assert.Equal(firstResult.ContentType, retrievedResult.ContentType);
        }

        [Fact]
        public async Task IdempotencyStore_ShouldNotReturnResult_ForDifferentKey()
        {
            // Arrange
            var savedKey = "saved-key";
            var differentKey = "different-key";

            var result = new IdempotencyResult
            {
                StatusCode = 200,
                ResponseBody = Array.Empty<byte>(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            await _idempotencyStore.SetAsync(savedKey, result, TimeSpan.FromHours(24));

            // Act
            var retrievedResult = await _idempotencyStore.TryGetAsync(differentKey);

            // Assert
            Assert.Null(retrievedResult);
        }

        [Fact]
        public async Task IdempotencyStore_ShouldHandleLocking_ForConcurrentRequests()
        {
            // Arrange
            var idempotencyKey = "concurrent-key";

            // Act: первый запрос захватывает лок
            var firstLockAcquired = await _idempotencyStore.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromSeconds(1));

            // Act: второй запрос пытается захватить тот же лок (должен провалиться)
            var secondLockAcquired = await _idempotencyStore.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(firstLockAcquired);
            Assert.False(secondLockAcquired); // Второй запрос не должен получить лок

            // Cleanup
            await _idempotencyStore.ReleaseLockAsync(idempotencyKey);
        }

        [Fact]
        public async Task IdempotencyStore_ShouldAllowLockAfterRelease()
        {
            // Arrange
            var idempotencyKey = "release-key";

            // Act: захватываем и освобождаем лок
            var firstAcquired = await _idempotencyStore.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromSeconds(1));
            await _idempotencyStore.ReleaseLockAsync(idempotencyKey);

            // Act: снова пытаемся захватить лок
            var secondAcquired = await _idempotencyStore.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromSeconds(1));

            // Assert
            Assert.True(firstAcquired);
            Assert.True(secondAcquired); // Должен получить после освобождения

            // Cleanup
            await _idempotencyStore.ReleaseLockAsync(idempotencyKey);
        }
    }
}