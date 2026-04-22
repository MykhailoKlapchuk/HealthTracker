var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("healthdb");

builder.AddProject<Projects.HealthTracker_Api>("healthtracker-api")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.HealthTracker_Worker>("healthtracker-worker")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(postgres)
    .WaitFor(postgres);

// Web app runs separately - start with: cd healthtracker-web && npm start
// API will be available at http://localhost:5000

builder.Build().Run();
