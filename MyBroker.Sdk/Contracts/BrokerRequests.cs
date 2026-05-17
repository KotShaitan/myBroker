namespace MyBroker.Sdk.Contracts;

public sealed class CreateTopicRequest
{
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
}

public sealed class SubscribeRequest
{
    public string TopicName { get; set; } = string.Empty;
    public int ConsumerID { get; set; }
}

public sealed class PublishRequest
{
    public int? TopicID { get; set; }
    public string? TopicName { get; set; }
    public int PubID { get; set; }
    public string Payload { get; set; } = string.Empty;
}

public sealed class AckRequest
{
    public string TopicName { get; set; } = string.Empty;
    public int ConsumerID { get; set; }
    public int MessageID { get; set; }
    public bool Success { get; set; } = true;
}

public sealed class EditTopicRequest
{
    public int TopicID { get; set; }
    public int? Amount { get; set; }
}

public sealed class DeleteTopicRequest
{
    public int TopicID { get; set; }
}
