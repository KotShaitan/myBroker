public class Subscription
{
    public int TopicID {get; set;}
    public int ConsumerID {get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}