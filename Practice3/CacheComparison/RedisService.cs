using System.Text.Json;
using StackExchange.Redis;

public class RedisService
{
    private readonly IDatabase _db;

    public RedisService(IDatabase db)
    {
        _db = db;
    }

    public async Task<Item?> GetItemAsync(int id)
    {
        var value = await _db.StringGetAsync(GetItemKey(id));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<Item>(value!);
    }

    public Task SetItemAsync(Item item)
    {
        var payload = JsonSerializer.Serialize(item);
        return _db.StringSetAsync(GetItemKey(item.Id), payload);
    }

    public Task RemoveItemAsync(int id)
    {
        return _db.KeyDeleteAsync(GetItemKey(id));
    }

    public Task FlushAllAsync()
    {
        return _db.ExecuteAsync("FLUSHALL");
    }

    private static string GetItemKey(int id) => $"item:{id}";
}
