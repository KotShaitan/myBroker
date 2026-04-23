public class DeliveryService
{
    private readonly TopicService _topicService;
    private readonly FileManager _fileManager;
    private readonly SubscribeService _subscribeService;

    private readonly Dictionary<int, int> _nextQueueByTopic = new();

     private readonly Dictionary<(int topicId, int consumerId, int queueId), int> _offsets = new();
    private readonly Dictionary<(int topicId, int consumerId), int> _nextQueueForConsumer = new();

    public DeliveryService(TopicService topicService, FileManager fileManager, SubscribeService subscribeService )
    {
        _topicService = topicService;
        _fileManager = fileManager;
        _subscribeService = subscribeService;
    }

    public async Task<Message> PublishAsync(int topicId, int publisherId, string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is empty");

            var topic = _topicService.GetById(topicId)
                        ?? throw new KeyNotFoundException("Topic not found");

            if (topic.Queues.Count == 0)
                throw new InvalidOperationException("Topic has no queues");

            var next = _nextQueueByTopic.TryGetValue(topicId, out var i) ? i : 0;
            var queueIndex = next % topic.Queues.Count;
            _nextQueueByTopic[topicId] = (queueIndex + 1) % topic.Queues.Count;

            var queue = topic.Queues[queueIndex];

            int nextMessageId = _topicService
                .GetAll()
                .SelectMany(t => t.Queues)
                .SelectMany(q => q.Messages)
                .DefaultIfEmpty(new Message { ID = 0 })
                .Max(m => m.ID) + 1;

            var message = new Message
            {
                ID = nextMessageId,
                TopicID = topicId,
                PubID = publisherId,
                Payload = payload
            };

            queue.Messages.Add(message);

            await _fileManager.SaveTopicsAsync(_topicService.GetAll().ToList());

            return message;
    }
    public  Message? Consume(int topicId, int consumerId)
    {
        var topic = _topicService.GetById(topicId)
                    ?? throw new KeyNotFoundException("Topic not found");

        if (!_subscribeService.IsSubscribed(topicId, consumerId))
            throw new InvalidOperationException("Consumer is not subscribed");

        if (topic.Queues.Count == 0)
            return null;

        var consumerKey = (topicId, consumerId);
        var startQueue = _nextQueueForConsumer.TryGetValue(consumerKey, out var idx) ? idx : 0;

        for (int step = 0; step < topic.Queues.Count; step++)
        {
            var qIndex = (startQueue + step) % topic.Queues.Count;
            var queue = topic.Queues[qIndex];

            var offsetKey = (topicId, consumerId, queue.QueueID);
            var offset = _offsets.TryGetValue(offsetKey, out var off) ? off : 0;

            if (offset < queue.Messages.Count)
            {
                var msg = queue.Messages[offset];
                _offsets[offsetKey] = offset + 1;
                _nextQueueForConsumer[consumerKey] = (qIndex + 1) % topic.Queues.Count;
                return msg;
            }
        }

        return null;
    }
}
