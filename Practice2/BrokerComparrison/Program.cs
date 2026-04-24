using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

var settings = builder.Configuration.GetSection("Benchmark").Get<BenchmarkSettings>() ?? new BenchmarkSettings();
var outputDirectory = Path.GetFullPath(
    args.FirstOrDefault(arg => arg.StartsWith("--output=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2)[1]
    ?? settings.OutputDirectory);

if (settings.Scenarios.Count == 0)
{
    Console.WriteLine("No scenarios configured in appsettings.json");
    return;
}

Directory.CreateDirectory(outputDirectory);

var results = new List<BenchmarkResult>();

foreach (var scenario in settings.Scenarios)
{
    Console.WriteLine();
    Console.WriteLine($"=== {scenario.Name} | {scenario.Broker} | size={scenario.MessageSizeBytes}B | rate={scenario.MessagesPerSecond}/sec | duration={scenario.DurationSeconds}s ===");

    IBrokerBenchmark broker = scenario.Broker.Equals("rabbitmq", StringComparison.OrdinalIgnoreCase)
        ? new RabbitMqBenchmark(settings.RabbitMq)
        : new RedisBenchmark(settings.Redis);

    try
    {
        var result = await broker.RunAsync(scenario, settings.ConsumerCount, settings.DrainSeconds);
        results.Add(result);
        PrintResult(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Scenario failed: {ex.Message}");
        results.Add(BenchmarkResult.CreateFailed(scenario, ex.Message));
    }
}

var reportWriter = new ReportWriter(outputDirectory);
await reportWriter.WriteAsync(results);

Console.WriteLine();
Console.WriteLine("=== Final summary ===");
foreach (var result in results)
{
    PrintResult(result);
}

Console.WriteLine();
Console.WriteLine($"Saved results to: {outputDirectory}");

static void PrintResult(BenchmarkResult result)
{
    Console.WriteLine(
        $"{result.Scenario.Broker}: sent={result.Sent}, processed={result.Processed}, lost={result.Lost}, backlog={result.Backlog}, errors={result.Errors}, throughput={result.MessagesPerSecond:F2}/sec, avg={result.AverageLatencyMs:F2} ms, p95={result.P95LatencyMs:F2} ms, max={result.MaxLatencyMs:F2} ms, degraded={result.IsDegraded}");

    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
    {
        Console.WriteLine($"error: {result.ErrorMessage}");
    }
}

interface IBrokerBenchmark
{
    Task<BenchmarkResult> RunAsync(BenchmarkScenario scenario, int consumerCount, int drainSeconds);
}

sealed class RabbitMqBenchmark(RabbitMqSettings settings) : IBrokerBenchmark
{
    public async Task<BenchmarkResult> RunAsync(BenchmarkScenario scenario, int consumerCount, int drainSeconds)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };

        var queueName = $"practice.rabbit.{scenario.Name.ToLowerInvariant().Replace(' ', '-')}";
        var collector = new MetricCollector(scenario);

        await using var connection = await factory.CreateConnectionAsync();
        await using var setupChannel = await connection.CreateChannelAsync();

        try
        {
            await setupChannel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false);
        }
        catch
        {
        }

        await setupChannel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: true);

        var consumerChannels = new List<IChannel>();
        for (var i = 0; i < consumerCount; i++)
        {
            var channel = await connection.CreateChannelAsync();
            await channel.BasicQosAsync(0, scenario.PrefetchCount, false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, args) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<BrokerMessage>(args.Body.Span);
                    if (message is not null)
                    {
                        collector.RegisterProcessed(message.CreatedAtUtc);
                    }
                    else
                    {
                        collector.RegisterError();
                    }
                }
                catch
                {
                    collector.RegisterError();
                }

                await channel.BasicAckAsync(args.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer);
            consumerChannels.Add(channel);
        }

        await using var producerChannel = await connection.CreateChannelAsync();
        var sent = await ProducerHelper.ProduceAsync(
            scenario,
            async body => await producerChannel.BasicPublishAsync(string.Empty, queueName, body),
            collector.RegisterSent);

        await Task.Delay(TimeSpan.FromSeconds(drainSeconds));

        var queueState = await setupChannel.QueueDeclarePassiveAsync(queueName);
        var backlog = (long)queueState.MessageCount;

        foreach (var channel in consumerChannels)
        {
            await channel.CloseAsync();
            await channel.DisposeAsync();
        }

        await setupChannel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false);
        return collector.Build(sent, backlog);
    }
}

sealed class RedisBenchmark(RedisSettings settings) : IBrokerBenchmark
{
    public async Task<BenchmarkResult> RunAsync(BenchmarkScenario scenario, int consumerCount, int drainSeconds)
    {
        var collector = new MetricCollector(scenario);
        var queueName = $"practice:redis:{scenario.Name.ToLowerInvariant().Replace(' ', '-')}";

        using var connection = await ConnectionMultiplexer.ConnectAsync(settings.ConnectionString);
        var database = connection.GetDatabase();
        await database.KeyDeleteAsync(queueName);

        var cts = new CancellationTokenSource();
        var consumers = Enumerable.Range(0, consumerCount)
            .Select(_ => ConsumeAsync(database, queueName, collector, cts.Token))
            .ToArray();

        var sent = await ProducerHelper.ProduceAsync(
            scenario,
            async body => await database.ListLeftPushAsync(queueName, body),
            collector.RegisterSent);

        await Task.Delay(TimeSpan.FromSeconds(drainSeconds));
        var backlog = await database.ListLengthAsync(queueName);

        cts.Cancel();

        try
        {
            await Task.WhenAll(consumers);
        }
        catch (OperationCanceledException)
        {
        }

        await database.KeyDeleteAsync(queueName);
        return collector.Build(sent, backlog);
    }

    private static async Task ConsumeAsync(IDatabase database, string queueName, MetricCollector collector, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var value = await database.ExecuteAsync("BRPOP", queueName, 1);
                if (value.IsNull)
                {
                    continue;
                }

                var parts = (RedisResult[])value!;
                if (parts.Length < 2 || parts[1].IsNull)
                {
                    continue;
                }

                var bytes = Encoding.UTF8.GetBytes((string)parts[1]!);
                var message = JsonSerializer.Deserialize<BrokerMessage>(bytes);
                if (message is not null)
                {
                    collector.RegisterProcessed(message.CreatedAtUtc);
                }
                else
                {
                    collector.RegisterError();
                }
            }
            catch (RedisServerException)
            {
                collector.RegisterError();
            }
        }
    }
}

sealed class MetricCollector(BenchmarkScenario scenario)
{
    private long _sent;
    private long _processed;
    private long _errors;
    private readonly ConcurrentBag<double> _latencies = [];

    public void RegisterSent() => Interlocked.Increment(ref _sent);

    public void RegisterProcessed(DateTime createdAtUtc)
    {
        Interlocked.Increment(ref _processed);
        _latencies.Add((DateTime.UtcNow - createdAtUtc).TotalMilliseconds);
    }

    public void RegisterError() => Interlocked.Increment(ref _errors);

    public BenchmarkResult Build(long expectedSent, long backlog)
    {
        var latencies = _latencies.OrderBy(x => x).ToArray();
        var avg = latencies.Length == 0 ? 0 : latencies.Average();
        var max = latencies.Length == 0 ? 0 : latencies[^1];
        var p95 = latencies.Length == 0 ? 0 : latencies[(int)Math.Ceiling(latencies.Length * 0.95) - 1];
        var actualSent = _sent == 0 ? expectedSent : _sent;
        var lost = Math.Max(0, actualSent - _processed);
        var throughput = scenario.DurationSeconds == 0 ? 0 : _processed / (double)scenario.DurationSeconds;
        var degraded = backlog > 0 || _errors > 0 || _processed < actualSent;

        return new BenchmarkResult(
            scenario,
            actualSent,
            _processed,
            lost,
            backlog,
            _errors,
            throughput,
            avg,
            p95,
            max,
            degraded,
            null);
    }
}

sealed record BrokerMessage(Guid Id, DateTime CreatedAtUtc, string Payload);

static class ProducerHelper
{
    public static async Task<long> ProduceAsync(BenchmarkScenario scenario, Func<byte[], Task> publishAsync, Action registerSent)
    {
        var payload = BuildPayload(scenario.MessageSizeBytes);
        var totalMessages = scenario.MessagesPerSecond * scenario.DurationSeconds;
        var delay = TimeSpan.FromSeconds(1d / scenario.MessagesPerSecond);
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < totalMessages; i++)
        {
            var message = new BrokerMessage(Guid.NewGuid(), DateTime.UtcNow, payload);
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            await publishAsync(body);
            registerSent();

            var expected = delay * (i + 1);
            var remaining = expected - stopwatch.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining);
            }
        }

        return totalMessages;
    }

    private static string BuildPayload(int targetBytes)
    {
        if (targetBytes <= 0)
        {
            return string.Empty;
        }

        return new string('x', targetBytes);
    }
}

sealed class ReportWriter(string outputDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(IReadOnlyCollection<BenchmarkResult> results)
    {
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "results.json"), JsonSerializer.Serialize(results, JsonOptions));
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "results.csv"), BuildCsv(results));
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "summary.md"), BuildMarkdown(results));
    }

    private static string BuildCsv(IEnumerable<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Category,Broker,MessageSizeBytes,MessagesPerSecond,DurationSeconds,Sent,Processed,Lost,Backlog,Errors,Throughput,AverageLatencyMs,P95LatencyMs,MaxLatencyMs,IsDegraded,ErrorMessage");

        foreach (var result in results)
        {
            sb.AppendLine(string.Join(",",
                Escape(result.Scenario.Name),
                Escape(result.Scenario.Category),
                Escape(result.Scenario.Broker),
                result.Scenario.MessageSizeBytes.ToString(CultureInfo.InvariantCulture),
                result.Scenario.MessagesPerSecond.ToString(CultureInfo.InvariantCulture),
                result.Scenario.DurationSeconds.ToString(CultureInfo.InvariantCulture),
                result.Sent.ToString(CultureInfo.InvariantCulture),
                result.Processed.ToString(CultureInfo.InvariantCulture),
                result.Lost.ToString(CultureInfo.InvariantCulture),
                result.Backlog.ToString(CultureInfo.InvariantCulture),
                result.Errors.ToString(CultureInfo.InvariantCulture),
                result.MessagesPerSecond.ToString("F2", CultureInfo.InvariantCulture),
                result.AverageLatencyMs.ToString("F2", CultureInfo.InvariantCulture),
                result.P95LatencyMs.ToString("F2", CultureInfo.InvariantCulture),
                result.MaxLatencyMs.ToString("F2", CultureInfo.InvariantCulture),
                result.IsDegraded.ToString(),
                Escape(result.ErrorMessage ?? string.Empty)));
        }

        return sb.ToString();
    }

    private static string BuildMarkdown(IEnumerable<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Benchmark Summary");
        sb.AppendLine();
        sb.AppendLine("| Name | Category | Broker | Size | Rate | Sent | Processed | Lost | Backlog | Errors | msg/sec | avg ms | p95 ms | max ms | degraded |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (var result in results)
        {
            sb.AppendLine(
                $"| {result.Scenario.Name} | {result.Scenario.Category} | {result.Scenario.Broker} | {result.Scenario.MessageSizeBytes} B | {result.Scenario.MessagesPerSecond}/sec | {result.Sent} | {result.Processed} | {result.Lost} | {result.Backlog} | {result.Errors} | {result.MessagesPerSecond:F2} | {result.AverageLatencyMs:F2} | {result.P95LatencyMs:F2} | {result.MaxLatencyMs:F2} | {result.IsDegraded} |");
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

sealed record BenchmarkResult(
    BenchmarkScenario Scenario,
    long Sent,
    long Processed,
    long Lost,
    long Backlog,
    long Errors,
    double MessagesPerSecond,
    double AverageLatencyMs,
    double P95LatencyMs,
    double MaxLatencyMs,
    bool IsDegraded,
    string? ErrorMessage)
{
    public static BenchmarkResult CreateFailed(BenchmarkScenario scenario, string errorMessage) =>
        new(scenario, 0, 0, 0, 0, 1, 0, 0, 0, 0, true, errorMessage);
}

sealed class BenchmarkSettings
{
    public RabbitMqSettings RabbitMq { get; init; } = new();
    public RedisSettings Redis { get; init; } = new();
    public int ConsumerCount { get; init; } = 1;
    public int DrainSeconds { get; init; } = 3;
    public string OutputDirectory { get; init; } = "results";
    public List<BenchmarkScenario> Scenarios { get; init; } = [];
}

sealed class RabbitMqSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
}

sealed class RedisSettings
{
    public string ConnectionString { get; init; } = "localhost:6379";
}

sealed class BenchmarkScenario
{
    public string Name { get; init; } = "baseline-rabbitmq";
    public string Category { get; init; } = "baseline";
    public string Broker { get; init; } = "RabbitMq";
    public int MessageSizeBytes { get; init; } = 128;
    public int MessagesPerSecond { get; init; } = 1000;
    public int DurationSeconds { get; init; } = 10;
    public ushort PrefetchCount { get; init; } = 100;
}
