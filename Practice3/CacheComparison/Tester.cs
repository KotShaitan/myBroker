using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

public enum CacheStrategy
{
    CacheAside,
    WriteThrough,
    WriteBack
}

public sealed class Metrics
{
    public long ReadRequests { get; set; }
    public long WriteRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public long DbReads { get; set; }
    public long DbWrites { get; set; }
    public int MaxWriteBackBuffer { get; set; }

    public long TotalRequests => ReadRequests + WriteRequests;
    public long TotalDbCalls => DbReads + DbWrites;
}

public sealed record WorkloadProfile(string Name, int ReadPercent);
public sealed record WorkloadOperation(bool IsRead, int ItemId, decimal NewPrice);

public class Tester
{
    private const int ItemsCount = 1000;
    private const int OperationsPerRun = 15000;

    private readonly ConnectionMultiplexer _redis;

    public Tester(ConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task RunAllAsync()
    {
        var profiles = new[]
        {
            new WorkloadProfile("read-heavy (80/20)", 80),
            new WorkloadProfile("balanced (50/50)", 50),
            new WorkloadProfile("write-heavy (20/80)", 20)
        };

        await EnsureDatabaseReadyAsync();

        foreach (var profile in profiles)
        {
            Console.WriteLine($"\n=== PROFILE: {profile.Name} ===");
            var operations = GenerateOperations(profile, OperationsPerRun, ItemsCount, seed: 42);

            foreach (var strategy in Enum.GetValues<CacheStrategy>())
            {
                await PrepareStateAsync();
                await ExecuteScenarioAsync(strategy, profile, operations);
            }
        }
    }

    private async Task ExecuteScenarioAsync(
        CacheStrategy strategy,
        WorkloadProfile profile,
        IReadOnlyList<WorkloadOperation> operations)
    {
        await using var db = new AppDbContext();
        var redisService = new RedisService(_redis.GetDatabase());
        var metrics = new Metrics();
        var reader = new Reader(redisService, db, metrics);
        var writer = new Writer(redisService, db, metrics);

        var sw = Stopwatch.StartNew();
        foreach (var op in operations)
        {
            if (op.IsRead)
            {
                switch (strategy)
                {
                    case CacheStrategy.CacheAside:
                        await reader.LazyReadAsync(op.ItemId);
                        break;
                    case CacheStrategy.WriteThrough:
                        await reader.ThroughReadAsync(op.ItemId);
                        break;
                    case CacheStrategy.WriteBack:
                        await reader.BackReadAsync(op.ItemId);
                        break;
                }
            }
            else
            {
                var item = new Item
                {
                    Id = op.ItemId,
                    Name = $"Item {op.ItemId}",
                    Price = op.NewPrice
                };

                switch (strategy)
                {
                    case CacheStrategy.CacheAside:
                        await writer.LazyWriteAsync(item);
                        break;
                    case CacheStrategy.WriteThrough:
                        await writer.ThroughWriteAsync(item);
                        break;
                    case CacheStrategy.WriteBack:
                        await writer.BackWriteAsync(item);
                        break;
                }
            }
        }

        if (strategy == CacheStrategy.WriteBack)
        {
            Console.WriteLine($"Write-Back buffered before final flush: {writer.PendingWriteBackCount}");
            await writer.FlushWriteBackAsync();
        }

        sw.Stop();

        PrintMetrics(strategy, profile, metrics, sw.Elapsed);
    }

    private static IReadOnlyList<WorkloadOperation> GenerateOperations(
        WorkloadProfile profile,
        int operationsCount,
        int maxItemId,
        int seed)
    {
        var random = new Random(seed + profile.ReadPercent);
        var operations = new List<WorkloadOperation>(operationsCount);

        for (var i = 0; i < operationsCount; i++)
        {
            var isRead = random.Next(1, 101) <= profile.ReadPercent;
            var itemId = random.Next(1, maxItemId + 1);
            var newPrice = Math.Round((decimal)(random.NextDouble() * 1000), 2);

            operations.Add(new WorkloadOperation(isRead, itemId, newPrice));
        }

        return operations;
    }

    private async Task EnsureDatabaseReadyAsync()
    {
        await using var db = new AppDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    private async Task PrepareStateAsync()
    {
        await using var db = new AppDbContext();
        var redisService = new RedisService(_redis.GetDatabase());

        await redisService.FlushAllAsync();

        db.Items.RemoveRange(db.Items);

        var items = Enumerable.Range(1, ItemsCount)
            .Select(i => new Item
            {
                Id = i,
                Name = $"Item {i}",
                Price = 100 + i
            });

        await db.Items.AddRangeAsync(items);
        await db.SaveChangesAsync();
    }

    private static void PrintMetrics(CacheStrategy strategy, WorkloadProfile profile, Metrics metrics, TimeSpan elapsed)
    {
        var throughput = metrics.TotalRequests / elapsed.TotalSeconds;
        var avgLatencyMs = elapsed.TotalMilliseconds / metrics.TotalRequests;
        var hitRate = metrics.ReadRequests == 0
            ? 0
            : (double)metrics.CacheHits / metrics.ReadRequests * 100;

        Console.WriteLine($"Strategy: {strategy}");
        Console.WriteLine($"Profile: {profile.Name}");
        Console.WriteLine($"Requests: {metrics.TotalRequests} (read={metrics.ReadRequests}, write={metrics.WriteRequests})");
        Console.WriteLine($"Throughput: {throughput:F2} req/sec");
        Console.WriteLine($"Average latency: {avgLatencyMs:F3} ms/req");
        Console.WriteLine($"DB calls: {metrics.TotalDbCalls} (reads={metrics.DbReads}, writes={metrics.DbWrites})");
        Console.WriteLine($"Cache hit rate: {hitRate:F2}% (hits={metrics.CacheHits}, misses={metrics.CacheMisses})");

        if (strategy == CacheStrategy.WriteBack)
        {
            Console.WriteLine($"Write-Back max buffered writes: {metrics.MaxWriteBackBuffer}");
        }

        Console.WriteLine(new string('-', 60));
    }
}
