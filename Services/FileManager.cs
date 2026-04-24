using System.Text.Json;

public class FileManager
{
    private readonly string _topicsPath = "Data/topics.json";
    
    public async Task SaveTopicsAsync(List<Topic> topics)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_topicsPath)!);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(topics, options);
        await File.WriteAllTextAsync(_topicsPath, json);
    }
    public async Task<List<Topic>> LoadTopicsAsync()
    {
        if (!File.Exists(_topicsPath))
            return new List<Topic>();

        var json = await File.ReadAllTextAsync(_topicsPath);
        return JsonSerializer.Deserialize<List<Topic>>(json) ?? new List<Topic>();
    }

    public async Task SaveSubscriptionsAsync(List<Subscription> subscriptions)
    {
        Directory.CreateDirectory("Data");
        var json = JsonSerializer.Serialize(subscriptions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync("Data/subscriptions.json", json);
    }

    public async Task<List<Subscription>> LoadSubscriptionsAsync()
    {
        const string path = "Data/subscriptions.json";
        if (!File.Exists(path)) return new List<Subscription>();

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<Subscription>>(json) ?? new List<Subscription>();
    }
}