using MyBroker.Sdk;
using MyBroker.Sdk.Contracts;
using MyBroker.Sdk.Exceptions;

var serviceName = Env("SERVICE_NAME", "consumer-b");
var brokerUrl = Env("BROKER_URL", "http://mybroker:8080/");
var topicName = Env("TOPIC_NAME", "demo-topic");
var consumerId = EnvInt("CONSUMER_ID", 102);
var expectedCount = EnvInt("EXPECTED_COUNT", 10);

using var httpClient = new HttpClient { BaseAddress = new Uri(brokerUrl) };
IBrokerApiClient broker = new BrokerApiClient(httpClient);

Console.WriteLine($"[{serviceName}] start broker={brokerUrl} topic={topicName} consumerId={consumerId}");

await WaitForSubscriptionAsync(broker, topicName, consumerId, serviceName);

var received = new List<string>();
while (received.Count < expectedCount)
{
    var msg = await broker.ConsumeAsync(topicName, consumerId);
    if (msg is null)
    {
        await Task.Delay(250);
        continue;
    }

    received.Add(msg.Payload ?? string.Empty);

    await broker.AckAsync(new AckRequest
    {
        TopicName = topicName,
        ConsumerID = consumerId,
        MessageID = msg.ID,
        Success = true
    });

    Console.WriteLine($"[{serviceName}] received+acked #{received.Count}/{expectedCount} id={msg.ID} payload={msg.Payload} at={DateTime.UtcNow:O}");
}

Console.WriteLine($"[{serviceName}] done received_all={received.Count}");

static async Task WaitForSubscriptionAsync(IBrokerApiClient broker, string topicName, int consumerId, string serviceName)
{
    for (var attempt = 1; attempt <= 60; attempt++)
    {
        try
        {
            await broker.SubscribeAsync(new SubscribeRequest { TopicName = topicName, ConsumerID = consumerId });
            Console.WriteLine($"[{serviceName}] subscribed to topic={topicName}");
            return;
        }
        catch (BrokerApiException)
        {
            await Task.Delay(1000);
        }
    }

    throw new Exception($"[{serviceName}] could not subscribe in time");
}

static string Env(string key, string fallback) => Environment.GetEnvironmentVariable(key) ?? fallback;

static int EnvInt(string key, int fallback)
{
    var raw = Environment.GetEnvironmentVariable(key);
    return int.TryParse(raw, out var value) ? value : fallback;
}
