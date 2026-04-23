public class Topic
{
    public int ID {get; set;}
    public string Name {get; set;}
    public List<Queue> Queues {get; set;}= new();

    public Topic()
    {
        Name = string.Empty;
    }

    public Topic(string name)
    {
        Name = name;
    }
    public void AddQueue()
    {
        int nextQueueId = Queues.Count == 0 ? 1 : Queues.Max(q => q.QueueID) + 1;
        Queues.Add(new Queue { QueueID = nextQueueId });
    }

    public void RemoveQueue()
    {
        if (Queues.Count > 0)
        {    
            if (Queues[Queues.Count - 1].Messages.Count == 0) {
                Queues.RemoveAt(Queues.Count - 1);
            }
            else           {
                throw new InvalidOperationException("Невозможно удалить очередь, так как она содержит сообщения");
            }
        }
    }
}
