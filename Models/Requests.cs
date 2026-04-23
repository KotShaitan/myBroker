public class CreateTopicRequest
{
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
}

public class SubscribeRequest
{
    public string TopicName { get; set; } = string.Empty;
    public int ConsumerID { get; set; }
}

public class PublishRequest
{
    public int TopicID { get; set; }
    public int PubID { get; set; }
    public string Payload { get; set; } = string.Empty;
}

public class ConsumeRequest
{
    public string TopicName { get; set; } = string.Empty;
    public int ConsumerID { get; set; }
}

public class EditTopicRequest
{
    public int TopicID { get; set; }
    public int? Amount { get; set; }
}

public class DeleteTopicRequest
{
    public int TopicID { get; set; }
}

