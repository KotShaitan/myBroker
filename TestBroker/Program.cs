using MyBroker.Sdk;
using MyBroker.Sdk.Contracts;
using MyBroker.Sdk.Exceptions;

var brokerUrl = Environment.GetEnvironmentVariable("BROKER_URL") ?? "http://localhost:8000/";
var topicName = $"demo-topic-{DateTime.UtcNow:yyyyMMddHHmmss}";
var publisherId = 9001;
var consumers = new[] { 101, 102, 103 };
var messageCount = 5;

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(brokerUrl)
};

IBrokerApiClient broker = new BrokerApiClient(httpClient);

Console.WriteLine($"[INFO] Broker URL: {brokerUrl}");
Console.WriteLine($"[INFO] Creating topic: {topicName}");

try
{
    await broker.CreateTopicAsync(new CreateTopicRequest
    {
        Name = topicName,
        Amount = 2
    });
    Console.WriteLine("[OK] Topic created");

    foreach (var consumerId in consumers)
    {
        await broker.SubscribeAsync(new SubscribeRequest
        {
            TopicName = topicName,
            ConsumerID = consumerId
        });
        Console.WriteLine($"[OK] Consumer {consumerId} subscribed");
    }

    var publishedPayloads = new List<string>();
    for (var i = 1; i <= messageCount; i++)
    {
        var payload = $"message-{i}";
        publishedPayloads.Add(payload);

        var published = await broker.PublishAsync(new PublishRequest
        {
            TopicName = topicName,
            PubID = publisherId,
            Payload = payload
        });

        Console.WriteLine($"[PUB] id={published.ID}, payload={payload}");
    }

    Console.WriteLine("[INFO] Consuming messages for each consumer...");

    foreach (var consumerId in consumers)
    {
        var received = new List<string>();

        while (received.Count < messageCount)
        {
            var msg = await ConsumeUntilMessageAsync(broker, topicName, consumerId);
            if (msg is null)
            {
                throw new Exception($"Consumer {consumerId} did not receive all messages. Received: {received.Count}/{messageCount}");
            }

            received.Add(msg.Payload ?? string.Empty);
        }

        Console.WriteLine($"[OK] Consumer {consumerId} received {received.Count}/{messageCount}: {string.Join(", ", received)}");
    }

    Console.WriteLine("[DONE] All consumers received all published messages");
}
catch (BrokerApiException ex)
{
    Console.WriteLine($"[API ERROR] {(int)ex.StatusCode} {ex.StatusCode}");
    if (!string.IsNullOrWhiteSpace(ex.ResponseBody))
    {
        Console.WriteLine($"[API BODY] {ex.ResponseBody}");
    }
    Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] {ex.Message}");
    Environment.ExitCode = 1;
}

static async Task<MessageDto?> ConsumeUntilMessageAsync(IBrokerApiClient broker, string topicName, int consumerId)
{
    const int maxAttempts = 30;

    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        var msg = await broker.ConsumeAsync(topicName, consumerId);
        if (msg is not null)
        {
            return msg;
        }

        await Task.Delay(150);
    }

    return null;
}
