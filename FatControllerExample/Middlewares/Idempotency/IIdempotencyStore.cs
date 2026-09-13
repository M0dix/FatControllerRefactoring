namespace FatControllerExample.Middlewares.Idempotency
{

    public interface IIdempotencyStore
    {
        Task<IdempotencyResult?> TryGetAsync(string key);

        Task SetAsync(string key, IdempotencyResult result, TimeSpan expiration);

        Task<bool> TryAcquireLockAsync(string key, TimeSpan lockTimeout);

        Task ReleaseLockAsync(string key);
    }
    public class IdempotencyResult
    {
        public int StatusCode { get; set; }
        public byte[] ResponseBody { get; set; } = Array.Empty<byte>();
        public Dictionary<string, string> Headers { get; set; } = new();
        public string ContentType { get; set; } = "application/json";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}