public class SubscribeService
{
    private readonly FileManager _fileManager;
    private List<Subscription> _subscriptions = new();
    public SubscribeService(FileManager fileManager)
    {
        _fileManager = fileManager;
    }
    public bool IsSubscribed(int topicId, int consumerId)
    {
        return _subscriptions.Any(s => s.TopicID == topicId && s.ConsumerID == consumerId);
    }
    public async Task InitAsync()
    {
        _subscriptions = await _fileManager.LoadSubscriptionsAsync();
    }
    public async Task Subscribe(int topicId, int consumerId)
    {
        bool exists = _subscriptions.Any(s => s.TopicID == topicId && s.ConsumerID == consumerId);
        if (exists) return;

        _subscriptions.Add(new Subscription
        {
            TopicID = topicId,
            ConsumerID = consumerId
        });

        await _fileManager.SaveSubscriptionsAsync(_subscriptions);    
    }
}