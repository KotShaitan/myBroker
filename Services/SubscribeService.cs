public class SubscribeService
{
    private readonly FileManager _fileManager;
    private readonly TopicService _topicService;
    private List<Subscription> _subscriptions = new();

    public SubscribeService(FileManager fileManager, TopicService topicService)
    {
        _fileManager = fileManager;
        _topicService = topicService;
    }

    public bool IsSubscribed(int topicId, int consumerId)
    {
        return _subscriptions.Any(s => s.TopicID == topicId && s.ConsumerID == consumerId);
    }

    public async Task InitAsync()
    {
        _subscriptions = await _fileManager.LoadSubscriptionsAsync();
    }

    public async Task Subscribe(string topicName, int consumerId)
    {
        var topic = _topicService.GetByName(topicName)
            ?? throw new KeyNotFoundException("Topic not found");

        bool exists = _subscriptions.Any(s => s.TopicID == topic.ID && s.ConsumerID == consumerId);
        if (exists) return;

        _subscriptions.Add(new Subscription
        {
            TopicID = topic.ID,
            ConsumerID = consumerId
        });

        await _fileManager.SaveSubscriptionsAsync(_subscriptions);
    }
}
