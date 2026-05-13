using Microsoft.EntityFrameworkCore;

public class Writer
{
    private readonly RedisService _redis;
    private readonly AppDbContext _dbContext;
    private readonly Metrics _metrics;
    private readonly List<Item> _writeBackBuffer = new();

    public Writer(RedisService redis, AppDbContext dbContext, Metrics metrics)
    {
        _redis = redis;
        _dbContext = dbContext;
        _metrics = metrics;
    }

    public async Task LazyWriteAsync(Item item)
    {
        _metrics.WriteRequests++;
        _metrics.DbWrites++;

        await UpdateItemInDbAsync(item);
        await _redis.RemoveItemAsync(item.Id);
    }

    public async Task ThroughWriteAsync(Item item)
    {
        _metrics.WriteRequests++;
        _metrics.DbWrites++;

        await UpdateItemInDbAsync(item);
        await _redis.SetItemAsync(item);
    }

    public async Task BackWriteAsync(Item item)
    {
        _metrics.WriteRequests++;
        await _redis.SetItemAsync(item);
        _writeBackBuffer.Add(item);

        if (_writeBackBuffer.Count > _metrics.MaxWriteBackBuffer)
        {
            _metrics.MaxWriteBackBuffer = _writeBackBuffer.Count;
        }

        if (_writeBackBuffer.Count >= 200)
        {
            await FlushWriteBackAsync();
        }
    }

    public async Task FlushWriteBackAsync()
    {
        if (_writeBackBuffer.Count == 0)
        {
            return;
        }

        foreach (var item in _writeBackBuffer)
        {
            await UpdateItemInDbAsync(item);
        }

        _metrics.DbWrites += _writeBackBuffer.Count;
        _writeBackBuffer.Clear();
    }

    public int PendingWriteBackCount => _writeBackBuffer.Count;

    private Task<int> UpdateItemInDbAsync(Item item)
    {
        return _dbContext.Items
            .Where(x => x.Id == item.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, item.Name)
                .SetProperty(x => x.Price, item.Price));
    }
}
