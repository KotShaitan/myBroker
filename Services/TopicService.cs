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
        var normalizedName = name.Trim();

        bool exists = _topics.Any(t =>
            t.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException("Topic name already exists");
        }

        int nextTopicId = _topics.Count == 0 ? 1 : _topics.Max(t => t.ID) + 1;
        Topic topic = new Topic(normalizedName) { ID = nextTopicId };

        for (int i = 0; i < amount; i++)
        {
            topic.AddQueue();
        }

        _topics.Add(topic);
        await _fileManager.SaveTopicsAsync(_topics);
    }

    public Topic? GetById(int id)
    {
        foreach (var topic in _topics)
        {
            if (topic.ID == id)
            {
                return topic;
            }
        }

        return null;
    }

    public Topic? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalizedName = name.Trim();
        return _topics.FirstOrDefault(t => t.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<Topic> GetAll()
    {
        return _topics.AsReadOnly();
    }

    public async Task EditTopic(int topicID, int? amount)
    {
        var topic = GetById(topicID);

        if (topic is null) return;

        if (amount is not null)
        {
            if (amount > _topics.Count)
            {
                for (int i = 0; i < amount - _topics.Count; i++)
                {
                    topic.AddQueue();
                }
            }
            else
            {
                for (int i = 0; i < _topics.Count - amount; i++)
                {
                    topic.RemoveQueue();
                }
            }

            for (int i = 0; i < amount; i++)
            {
                topic.AddQueue();
            }
        }

        await _fileManager.SaveTopicsAsync(_topics);
    }

    public async Task RemoveTopic(int topicID)
    {
        var topic = GetById(topicID);
        if (topic is null) return;

        _topics.Remove(topic);
        await _fileManager.SaveTopicsAsync(_topics);
    }
}
