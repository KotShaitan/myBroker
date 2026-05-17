using MyBroker.Sdk;
using MyBroker.Sdk.Contracts;
using MyBroker.Sdk.Exceptions;

var brokerUrl = Env("BROKER_URL", "http://mybroker:8080/");
var topicName = Env("TOPIC_NAME", "demo-topic");
var queueCount = EnvInt("QUEUE_COUNT", 2);
var messageCount = EnvInt("MESSAGE_COUNT", 10);
var delayMs = EnvInt("PUBLISH_DELAY_MS", 300);
var publisherId = EnvInt("PUBLISHER_ID", 5001);

using var httpClient = new HttpClient { BaseAddress = new Uri(brokerUrl) };
IBrokerApiClient broker = new BrokerApiClient(httpClient);

Console.WriteLine($"[producer] start broker={brokerUrl} topic={topicName} messages={messageCount}");

await WaitForBrokerAsync(broker);
await EnsureTopicAsync(broker, topicName, queueCount);

for (var i = 1; i <= messageCount; i++)
{
    var payload = $"event-{i}";
    var msg = await broker.PublishAsync(new PublishRequest
    {
        TopicName = topicName,
        PubID = publisherId,
        Payload = payload
    });

    Console.WriteLine($"[producer] published id={msg.ID} payload={payload} at={DateTime.UtcNow:O}");
    await Task.Delay(delayMs);
}

Console.WriteLine("[producer] done");

static async Task WaitForBrokerAsync(IBrokerApiClient broker)
{
    for (var attempt = 1; attempt <= 60; attempt++)
    {
        try
        {
            await broker.CreateTopicAsync(new CreateTopicRequest { Name = "health-probe", Amount = 1 });
            Console.WriteLine("[producer] broker is ready");
            return;
        }
        catch
        {
            await Task.Delay(1000);
        }
    }

    throw new Exception("Broker did not become ready in time");
}

static async Task EnsureTopicAsync(IBrokerApiClient broker, string topicName, int queueCount)
{
    try
    {
        await broker.CreateTopicAsync(new CreateTopicRequest { Name = topicName, Amount = queueCount });
        Console.WriteLine($"[producer] topic created: {topicName}");
    }
    catch (BrokerApiException ex) when ((int)ex.StatusCode == 400 && (ex.ResponseBody?.Contains("already exists", StringComparison.OrdinalIgnoreCase) ?? false))
    {
        Console.WriteLine($"[producer] topic already exists: {topicName}");
    }
}

static string Env(string key, string fallback) => Environment.GetEnvironmentVariable(key) ?? fallback;

static int EnvInt(string key, int fallback)
{
    var raw = Environment.GetEnvironmentVariable(key);
    return int.TryParse(raw, out var value) ? value : fallback;
}
