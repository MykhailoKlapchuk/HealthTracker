using HealthTracker.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddRedisClient("redis");
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
