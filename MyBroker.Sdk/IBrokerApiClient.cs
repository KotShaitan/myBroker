using MyBroker.Sdk.Contracts;

namespace MyBroker.Sdk;

public interface IBrokerApiClient
{
    Task CreateTopicAsync(CreateTopicRequest request, CancellationToken cancellationToken = default);
    Task SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default);
    Task<MessageDto> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default);
    Task<MessageDto?> ConsumeAsync(string topicName, int consumerId, CancellationToken cancellationToken = default);
    Task AckAsync(AckRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MessageDto>> GetDlqAsync(string topicName, int consumerId, CancellationToken cancellationToken = default);
    Task EditTopicAsync(EditTopicRequest request, CancellationToken cancellationToken = default);
    Task DeleteTopicAsync(DeleteTopicRequest request, CancellationToken cancellationToken = default);
}
