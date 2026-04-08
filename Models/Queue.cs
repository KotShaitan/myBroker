public class Queue
{
    public int QueueID {get; set;}
    public List<Message> Messages {get; set;} = new(); 

    public void AddMessage(Message message)
    {
        Messages.Add(message);
    }
}
