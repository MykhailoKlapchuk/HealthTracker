using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Npgsql;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.AddRedisClient("redis");
builder.AddNpgsqlDataSource("healthdb");

var app = builder.Build();

var dbSource = app.Services.GetRequiredService<NpgsqlDataSource>();

// Initialize database tables
await using (var cmd = dbSource.CreateCommand("""
    CREATE TABLE IF NOT EXISTS "Users" (
        "Id"           SERIAL PRIMARY KEY,
        "Email"        VARCHAR(255) UNIQUE NOT NULL,
        "PasswordHash" VARCHAR(255) NOT NULL,
        "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
    );

    CREATE TABLE IF NOT EXISTS "VitalSigns" (
        "Id"               SERIAL PRIMARY KEY,
        "UserId"           INTEGER NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
        "HeartRate"        INTEGER,
        "TemperatureCelsius" DECIMAL(5,2),
        "OxygenSaturation" INTEGER,
        "Timestamp"        TIMESTAMPTZ NOT NULL,
        "Notes"            TEXT
    );

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

// Auth Endpoints
app.MapPost("/auth/register", async (RegisterRequest req, NpgsqlDataSource db, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest("Email and password required");

    var hash = HashPassword(req.Password);

    try
    {
        await using var cmd = db.CreateCommand(
            "INSERT INTO \"Users\" (\"Email\", \"PasswordHash\") VALUES ($1, $2) RETURNING \"Id\"");
        cmd.Parameters.AddWithValue(req.Email.ToLower());
        cmd.Parameters.AddWithValue(hash);

        var userId = await cmd.ExecuteScalarAsync();
        if (userId == null)
            return Results.BadRequest("Failed to create user");

        var userIdInt = (int)userId;
        var token = GenerateToken(userIdInt, req.Email);
        SetAuthCookie(context, token);
        return Results.Ok(new { message = "User created successfully", userId = userIdInt, email = req.Email });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505") // unique constraint
    {
        return Results.BadRequest("Email already exists");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Registration failed: {ex.Message}");
    }
});

app.MapPost("/auth/login", async (LoginRequest req, NpgsqlDataSource db, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest("Email and password required");

    await using var cmd = db.CreateCommand(
        "SELECT \"Id\", \"PasswordHash\" FROM \"Users\" WHERE \"Email\" = $1");
    cmd.Parameters.AddWithValue(req.Email.ToLower());

    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.Unauthorized();

    var userId = reader.GetInt32(0);
    var hash = reader.GetString(1);

    if (!VerifyPassword(req.Password, hash))
        return Results.Unauthorized();

    var token = GenerateToken(userId, req.Email);
    SetAuthCookie(context, token);
    return Results.Ok(new { userId, email = req.Email });
});

app.MapGet("/auth/validate", async (NpgsqlDataSource db, HttpContext context) =>
{
    var userId = GetUserIdFromToken(context);
    if (userId == null)
        return Results.Unauthorized();

    await using var cmd = db.CreateCommand("SELECT \"Email\" FROM \"Users\" WHERE \"Id\" = $1");
    cmd.Parameters.AddWithValue(userId.Value);

    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.Unauthorized();

    var email = reader.GetString(0);
    return Results.Ok(new { valid = true, email });
});

app.MapPost("/auth/logout", (HttpContext context) =>
{
    context.Response.Cookies.Delete("auth_token");
    return Results.Ok(new { message = "Logged out" });
});

// Vital Signs Endpoints
app.MapPost("/vitals", async (VitalSignsRequest req, NpgsqlDataSource db, IConnectionMultiplexer redis, HttpContext context, ILogger<Program> logger) =>
{
    var userId = GetUserIdFromToken(context);
    if (userId == null)
        return Results.Unauthorized();

    var timestamp = DateTimeOffset.UtcNow;

    await using var cmd = db.CreateCommand("""
        INSERT INTO "VitalSigns" ("UserId", "HeartRate", "TemperatureCelsius", "OxygenSaturation", "Timestamp", "Notes")
        VALUES ($1, $2, $3, $4, $5, $6)
        RETURNING "Id"
        """);
    cmd.Parameters.AddWithValue(userId.Value);
    cmd.Parameters.AddWithValue(req.HeartRate ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue(req.TemperatureCelsius ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue(req.OxygenSaturation ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue(timestamp);
    cmd.Parameters.AddWithValue(req.Notes ?? (object)DBNull.Value);

    var id = await cmd.ExecuteScalarAsync();
    var recordId = (int)(id ?? 0);

    var warnings = ComputeWarnings(req.HeartRate, req.TemperatureCelsius, req.OxygenSaturation);

    if (warnings.Any)
    {
        var warningList = new List<string>();
        if (warnings.HeartRateHigh) warningList.Add($"High Heart Rate: {req.HeartRate} BPM");
        if (warnings.HeartRateLow) warningList.Add($"Low Heart Rate: {req.HeartRate} BPM");
        if (warnings.TemperatureHigh) warningList.Add($"High Temperature: {req.TemperatureCelsius}°C");
        if (warnings.TemperatureLow) warningList.Add($"Low Temperature: {req.TemperatureCelsius}°C");
        if (warnings.OxygenLow) warningList.Add($"Low Oxygen Saturation: {req.OxygenSaturation}%");
        logger.LogWarning("VITAL SIGN WARNING - User {UserId}: {Warnings}", userId, string.Join(", ", warningList));
    }

    var redisDb = redis.GetDatabase();
    var redisKey = $"vitals:warnings:{userId.Value}:{recordId}";
    await redisDb.HashSetAsync(redisKey, new StackExchange.Redis.HashEntry[]
    {
        new("heartRateHigh", warnings.HeartRateHigh ? "1" : "0"),
        new("heartRateLow", warnings.HeartRateLow ? "1" : "0"),
        new("temperatureHigh", warnings.TemperatureHigh ? "1" : "0"),
        new("temperatureLow", warnings.TemperatureLow ? "1" : "0"),
        new("oxygenLow", warnings.OxygenLow ? "1" : "0"),
    });
    await redisDb.KeyExpireAsync(redisKey, TimeSpan.FromDays(7));

    return Results.Ok(new
    {
        id = recordId,
        timestamp,
        warnings = new
        {
            heartRateHigh = warnings.HeartRateHigh,
            heartRateLow = warnings.HeartRateLow,
            temperatureHigh = warnings.TemperatureHigh,
            temperatureLow = warnings.TemperatureLow,
            oxygenLow = warnings.OxygenLow,
            any = warnings.Any
        }
    });
});

app.MapGet("/vitals", async (NpgsqlDataSource db, IConnectionMultiplexer redis, HttpContext context) =>
{
    var userId = GetUserIdFromToken(context);
    if (userId == null)
        return Results.Unauthorized();

    await using var cmd = db.CreateCommand("""
        SELECT "Id", "HeartRate", "TemperatureCelsius", "OxygenSaturation", "Timestamp", "Notes"
        FROM "VitalSigns"
        WHERE "UserId" = $1
        ORDER BY "Timestamp" DESC
        LIMIT 50
        """);
    cmd.Parameters.AddWithValue(userId.Value);

    await using var reader = await cmd.ExecuteReaderAsync();
    var vitals = new List<object>();
    var redisDb = redis.GetDatabase();
    while (await reader.ReadAsync())
    {
        var recordId = reader.GetInt32(0);
        var fields = await redisDb.HashGetAllAsync($"vitals:warnings:{userId.Value}:{recordId}");
        var wf = fields.ToDictionary(e => e.Name.ToString(), e => e.Value == "1");

        vitals.Add(new
        {
            id = recordId,
            heartRate = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
            temperatureCelsius = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2),
            oxygenSaturation = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
            timestamp = reader.GetFieldValue<DateTimeOffset>(4),
            notes = reader.IsDBNull(5) ? null : reader.GetString(5),
            warnings = new
            {
                heartRateHigh = wf.GetValueOrDefault("heartRateHigh"),
                heartRateLow = wf.GetValueOrDefault("heartRateLow"),
                temperatureHigh = wf.GetValueOrDefault("temperatureHigh"),
                temperatureLow = wf.GetValueOrDefault("temperatureLow"),
                oxygenLow = wf.GetValueOrDefault("oxygenLow"),
                any = wf.Values.Any(v => v)
            }
        });
    }

    return Results.Ok(vitals);
});

// Legacy heartrate endpoint (for backward compatibility)
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

// Helper functions
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hash);
}

bool VerifyPassword(string password, string hash)
{
    var hashOfInput = HashPassword(password);
    return hashOfInput.Equals(hash);
}

string GenerateToken(int userId, string email)
{
    var payload = $"{userId}:{email}:{DateTimeOffset.UtcNow.AddDays(7):O}";
    var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    return token;
}

void SetAuthCookie(HttpContext ctx, string token) =>
    ctx.Response.Cookies.Append("auth_token", token, new CookieOptions
    {
        HttpOnly = true,
        Secure = !app.Environment.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
        Path = "/"
    });

int? GetUserIdFromToken(HttpContext context)
{
    string? token = null;

    // Try cookie first
    if (context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
    {
        token = cookieToken;
    }
    else
    {
        // Fall back to Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            token = authHeader["Bearer ".Length..];
    }

    if (string.IsNullOrEmpty(token)) return null;

    try
    {
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var parts = payload.Split(':');
        return int.Parse(parts[0]);
    }
    catch
    {
        return null;
    }
}

VitalWarnings ComputeWarnings(int? heartRate, decimal? temp, int? spo2) =>
    new(heartRate is > 100, heartRate is < 60, temp is > 38m, temp is < 36m, spo2 is < 95);

// Request/Response records
record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);
record HeartRateRequest(int Bpm);
record VitalSignsRequest(
    [property: JsonPropertyName("heartRate")] int? HeartRate,
    [property: JsonPropertyName("temperatureCelsius")] decimal? TemperatureCelsius,
    [property: JsonPropertyName("oxygenSaturation")] int? OxygenSaturation,
    [property: JsonPropertyName("notes")] string? Notes
);

record VitalWarnings(bool HeartRateHigh, bool HeartRateLow, bool TemperatureHigh, bool TemperatureLow, bool OxygenLow)
{
    public bool Any => HeartRateHigh || HeartRateLow || TemperatureHigh || TemperatureLow || OxygenLow;
}
