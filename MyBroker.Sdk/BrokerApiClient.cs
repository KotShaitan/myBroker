using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyBroker.Sdk.Contracts;
using MyBroker.Sdk.Exceptions;

namespace MyBroker.Sdk;

public sealed class BrokerApiClient : IBrokerApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public BrokerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CreateTopicAsync(CreateTopicRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("broker/CreateTopic", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    public async Task SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("broker/Subscribe", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    public async Task<MessageDto> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("broker/Publish", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<MessageDto>(JsonOptions, cancellationToken);
        return result ?? throw new BrokerApiException(HttpStatusCode.InternalServerError, "Publish response body is empty");
    }

    public async Task<MessageDto?> ConsumeAsync(string topicName, int consumerId, CancellationToken cancellationToken = default)
    {
        var encodedName = Uri.EscapeDataString(topicName);
        var path = $"broker/Consume?TopicName={encodedName}&ConsumerID={consumerId}";

        using var response = await _httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MessageDto>(JsonOptions, cancellationToken);
    }

    public async Task EditTopicAsync(EditTopicRequest request, CancellationToken cancellationToken = default)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Patch, "broker/EditTopic")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    public async Task DeleteTopicAsync(DeleteTopicRequest request, CancellationToken cancellationToken = default)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "broker/DeleteTopic")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        throw new BrokerApiException(
            response.StatusCode,
            $"Broker request failed with status {(int)response.StatusCode} ({response.StatusCode})",
            body);
    }
}
