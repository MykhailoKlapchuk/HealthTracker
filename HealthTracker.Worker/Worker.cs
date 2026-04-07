using StackExchange.Redis;

namespace HealthTracker.Worker;

public class Worker(ILogger<Worker> logger, IConnectionMultiplexer redis) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var db = redis.GetDatabase();
            var latest = await db.ListGetByIndexAsync("heartrate:history", 0);

            if (latest.HasValue)
            {
                var parts = ((string)latest!).Split('|');
                var timestamp = parts[0];
                var bpm = int.Parse(parts[1]);

                if (bpm > 100)
                    logger.LogWarning("High BPM detected: {Bpm} at {Timestamp}", bpm, timestamp);
                else
                    logger.LogInformation("Latest heart rate: {Bpm} BPM at {Timestamp}", bpm, timestamp);
            }
            else
            {
                logger.LogInformation("No heart rate data in Redis.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
