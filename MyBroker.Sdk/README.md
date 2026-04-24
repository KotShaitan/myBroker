# MyBroker SDK

## Quick Start

```csharp
using MyBroker.Sdk;
using MyBroker.Sdk.Contracts;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:8000/")
};

IBrokerApiClient broker = new BrokerApiClient(httpClient);

await broker.CreateTopicAsync(new CreateTopicRequest
{
    Name = "orders",
    Amount = 3
});

await broker.SubscribeAsync(new SubscribeRequest
{
    TopicName = "orders",
    ConsumerID = 1
});

var published = await broker.PublishAsync(new PublishRequest
{
    TopicID = 1,
    PubID = 10,
    Payload = "hello"
});

var consumed = await broker.ConsumeAsync("orders", 1);
```

## DI registration

```csharp
builder.Services.AddMyBrokerSdk("http://localhost:8000");
```
