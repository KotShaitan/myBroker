var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<FileManager>();
builder.Services.AddSingleton<TopicService>();
builder.Services.AddSingleton<SubscribeService>();
builder.Services.AddSingleton<DeliveryService>();

var app = builder.Build();
TopicService topicService = app.Services.GetRequiredService<TopicService>();
SubscribeService subscribeService = app.Services.GetRequiredService<SubscribeService>();
await subscribeService.InitAsync();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await topicService.InitAsync();

app.MapControllers();

app.Run();
