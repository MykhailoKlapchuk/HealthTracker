using Npgsql;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddRedisClient("redis");
builder.AddNpgsqlDataSource("healthdb");

var app = builder.Build();

var dbSource = app.Services.GetRequiredService<NpgsqlDataSource>();
await using (var cmd = dbSource.CreateCommand("""
    CREATE TABLE IF NOT EXISTS "HeartRateHistory" (
        "Id"        SERIAL PRIMARY KEY,
        "Bpm"       INTEGER NOT NULL,
        "Timestamp" TIMESTAMPTZ NOT NULL,
        "State"     TEXT NOT NULL
    )
    """))
{
    await cmd.ExecuteNonQueryAsync();
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();


app.MapPost("/heartrate", async (HeartRateRequest request, IConnectionMultiplexer redis, NpgsqlDataSource db) =>
{
    var timestamp = DateTimeOffset.UtcNow;

    var redisDb = redis.GetDatabase();
    var entry = $"{timestamp:O}|{request.Bpm}";
    await redisDb.ListLeftPushAsync("heartrate:history", entry);
    await redisDb.ListTrimAsync("heartrate:history", 0, 99);

    var state = request.Bpm >= 100 ? "warning" : "normal";

    await using var cmd = db.CreateCommand("""
        INSERT INTO "HeartRateHistory" ("Bpm", "Timestamp", "State") VALUES ($1, $2, $3)
        """);
    cmd.Parameters.AddWithValue(request.Bpm);
    cmd.Parameters.AddWithValue(timestamp);
    cmd.Parameters.AddWithValue(state);
    await cmd.ExecuteNonQueryAsync();

    return Results.Ok(new { request.Bpm, Timestamp = timestamp });
});

app.MapGet("/heartrate", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var entries = await db.ListRangeAsync("heartrate:history", 0, 4);
    var results = entries.Select(e =>
    {
        var parts = ((string)e!).Split('|');
        return new { Timestamp = parts[0], Bpm = int.Parse(parts[1]) };
    });
    return Results.Ok(results);
});

app.Run();

record HeartRateRequest(int Bpm);
