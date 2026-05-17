public class Message
{
    public int ID { get; set; }
    public int TopicID { get; set; }
    public int PubID { get; set; }
    public string? Payload { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public int DeliveryAttempts { get; set; }
}
