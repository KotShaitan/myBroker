using StackExchange.Redis;

var redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
var tester = new Tester(redis);

try
{
    await tester.RunAllAsync();
}
finally
{
    await redis.CloseAsync();
    redis.Dispose();
}
