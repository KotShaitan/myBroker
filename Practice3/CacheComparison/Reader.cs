public class Reader
{
    private readonly RedisService _redis;
    private readonly AppDbContext _dbContext;
    private readonly Metrics _metrics;

    public Reader(RedisService redis, AppDbContext dbContext, Metrics metrics)
    {
        _redis = redis;
        _dbContext = dbContext;
        _metrics = metrics;
    }

    public async Task<Item?> LazyReadAsync(int id)
    {
        return await ReadThroughCacheAsync(id);
    }

    public async Task<Item?> ThroughReadAsync(int id)
    {
        return await ReadThroughCacheAsync(id);
    }

    public async Task<Item?> BackReadAsync(int id)
    {
        return await ReadThroughCacheAsync(id);
    }

    private async Task<Item?> ReadThroughCacheAsync(int id)
    {
        _metrics.ReadRequests++;
        var fromCache = await _redis.GetItemAsync(id);
        if (fromCache is not null)
        {
            _metrics.CacheHits++;
            return fromCache;
        }

        _metrics.CacheMisses++;
        _metrics.DbReads++;
        var fromDb = await _dbContext.Items.FindAsync(id);
        if (fromDb is not null)
        {
            await _redis.SetItemAsync(fromDb);
        }

        return fromDb;
    }
}
