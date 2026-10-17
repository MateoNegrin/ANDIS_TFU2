using StackExchange.Redis;

namespace TFU_Architecture.Services;

// Patrón: Cache-Aside
public class CacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly bool _enabled;
    private readonly int _ttlSeconds;
    public long CacheHits { get; private set; }
    public long CacheMisses { get; private set; }

    public CacheService(IConnectionMultiplexer redis, bool enabled = true, int ttlSeconds = 60)
    {
        _redis = redis;
        _enabled = enabled;
        _ttlSeconds = ttlSeconds;
    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T>> fallback)
    {
        if (!_enabled)
        {
            return await fallback();
        }

        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync(key);

            if (cached.HasValue)
            {
                CacheHits++;
                return System.Text.Json.JsonSerializer.Deserialize<T>(cached.ToString());
            }

            CacheMisses++;
            var value = await fallback();

            if (value != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(value);
                await db.StringSetAsync(key, json, TimeSpan.FromSeconds(_ttlSeconds));
            }

            return value;
        }
        catch
        {
            // Si Redis falla, caer a la BD
            return await fallback();
        }
    }

    public async Task InvalidateAsync(string key)
    {
        if (!_enabled) return;

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch { }
    }
}
