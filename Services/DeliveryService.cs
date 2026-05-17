public class DeliveryService
{
    private readonly TopicService _topicService;
    private readonly FileManager _fileManager;
    private readonly SubscribeService _subscribeService;

    private readonly Dictionary<int, int> _nextQueueByTopic = new();
    private readonly Dictionary<(int topicId, int consumerId, int queueId), int> _offsets = new();
    private readonly Dictionary<(int topicId, int consumerId), int> _nextQueueForConsumer = new();

    private readonly Dictionary<(int topicId, int consumerId), PendingDelivery> _pending = new();
    private readonly Dictionary<(int topicId, int consumerId), List<Message>> _deadLetter = new();

    private const int MaxAttempts = 3;
    private static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(20);

    private sealed class PendingDelivery
    {
        public required Message Message { get; init; }
        public required int QueueID { get; init; }
        public required DateTime ExpireAtUtc { get; init; }
    }

    public DeliveryService(TopicService topicService, FileManager fileManager, SubscribeService subscribeService)
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
            Payload = payload,
            DeliveryAttempts = 0
        };

        queue.Messages.Add(message);

        await _fileManager.SaveTopicsAsync(_topicService.GetAll().ToList());

        return message;
    }

    public Message? Consume(string topicName, int consumerId)
    {
        var topic = _topicService.GetByName(topicName)
                    ?? throw new KeyNotFoundException("Topic not found");

        var topicId = topic.ID;

        if (!_subscribeService.IsSubscribed(topicId, consumerId))
            throw new InvalidOperationException("Consumer is not subscribed");

        HandlePendingTimeout(topic, topicId, consumerId);

        if (_pending.ContainsKey((topicId, consumerId)))
        {
            return null;
        }

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

                _pending[(topicId, consumerId)] = new PendingDelivery
                {
                    Message = msg,
                    QueueID = queue.QueueID,
                    ExpireAtUtc = DateTime.UtcNow + AckTimeout
                };

                _nextQueueForConsumer[consumerKey] = (qIndex + 1) % topic.Queues.Count;
                return msg;
            }
        }

        return null;
    }

    public async Task<bool> AckAsync(string topicName, int consumerId, int messageId, bool success)
    {
        var topic = _topicService.GetByName(topicName)
                    ?? throw new KeyNotFoundException("Topic not found");

        var topicId = topic.ID;
        var key = (topicId, consumerId);

        if (!_pending.TryGetValue(key, out var pending))
        {
            return false;
        }

        if (pending.Message.ID != messageId)
        {
            return false;
        }

        if (success)
        {
            AdvanceOffset(topicId, consumerId, pending.QueueID);
            _pending.Remove(key);
            return true;
        }

        var movedToDlq = MoveOrRetry(topicId, consumerId, pending);
        _pending.Remove(key);

        if (movedToDlq)
        {
            await _fileManager.SaveTopicsAsync(_topicService.GetAll().ToList());
        }

        return true;
    }

    public IReadOnlyList<Message> GetDeadLetters(string topicName, int consumerId)
    {
        var topic = _topicService.GetByName(topicName)
                    ?? throw new KeyNotFoundException("Topic not found");

        var key = (topic.ID, consumerId);
        return _deadLetter.TryGetValue(key, out var list) ? list.AsReadOnly() : Array.Empty<Message>();
    }

    private void HandlePendingTimeout(Topic topic, int topicId, int consumerId)
    {
        var key = (topicId, consumerId);
        if (!_pending.TryGetValue(key, out var pending))
        {
            return;
        }

        if (DateTime.UtcNow <= pending.ExpireAtUtc)
        {
            return;
        }

        MoveOrRetry(topicId, consumerId, pending);
        _pending.Remove(key);
    }

    private bool MoveOrRetry(int topicId, int consumerId, PendingDelivery pending)
    {
        pending.Message.DeliveryAttempts++;

        if (pending.Message.DeliveryAttempts < MaxAttempts)
        {
            return false;
        }

        var deadKey = (topicId, consumerId);
        if (!_deadLetter.TryGetValue(deadKey, out var list))
        {
            list = new List<Message>();
            _deadLetter[deadKey] = list;
        }

        list.Add(pending.Message);
        AdvanceOffset(topicId, consumerId, pending.QueueID);
        return true;
    }

    private void AdvanceOffset(int topicId, int consumerId, int queueId)
    {
        var offsetKey = (topicId, consumerId, queueId);
        var current = _offsets.TryGetValue(offsetKey, out var off) ? off : 0;
        _offsets[offsetKey] = current + 1;
    }
}
