public class TopicService
{
    private readonly FileManager _fileManager;
    private List<Topic> _topics = new();
    public TopicService(FileManager fileManager)
    {
        _fileManager = fileManager;
    }
    public async Task InitAsync()
    {
        _topics = await _fileManager.LoadTopicsAsync();
    }
    public async Task CreateTopic(string name, int amount)
    {
        int nextTopicId = _topics.Count == 0 ? 1 : _topics.Max(t => t.ID) + 1;
        Topic topic = new Topic(name) { ID = nextTopicId };
        for (int i=0; i < amount; i++)
        {
            topic.AddQueue();
        }
        _topics.Add(topic);
        await _fileManager.SaveTopicsAsync(_topics);
    }
    public Topic? GetById(int id)
    {
        foreach (var topic in _topics) {
            if (topic.ID == id)
            {
                return topic;
            }
        }
        return null;
    }
    public IReadOnlyList<Topic> GetAll()
    {
        return _topics.AsReadOnly();
    }
    public async Task EditTopic(int amount)
    {
        
    }
}
