using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using TFU_Architecture.Commands;
using TFU_Architecture.Data;
using TFU_Architecture.Middleware;
using TFU_Architecture.Queries;
using TFU_Architecture.Services;

var builder = WebApplication.CreateBuilder(args);

// Environment variables
var appRole = Environment.GetEnvironmentVariable("APP_ROLE") ?? "commands";
var appVersion = Environment.GetEnvironmentVariable("APP_VERSION") ?? "v1";
var instanceName = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "default";
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-secret-key-change-in-production";
var cacheEnabled = bool.TryParse(Environment.GetEnvironmentVariable("CACHE_ENABLED"), out var enabled) ? enabled : true;
var cacheTtlSeconds = int.TryParse(Environment.GetEnvironmentVariable("CACHE_TTL_SECONDS"), out var ttl) ? ttl : 60;
var cbFailureThreshold = int.TryParse(Environment.GetEnvironmentVariable("CB_FAILURE_THRESHOLD"), out var threshold) ? threshold : 3;
var cbOpenSeconds = int.TryParse(Environment.GetEnvironmentVariable("CB_OPEN_SECONDS"), out var openSec) ? openSec : 10;

var key = Encoding.ASCII.GetBytes(jwtSecret);

// Add DbContext with PostgreSQL
var dbConnectionString = $"Server={Environment.GetEnvironmentVariable("DB_SERVER") ?? "db"};Database={Environment.GetEnvironmentVariable("DB_NAME") ?? "emergencies"};Username={Environment.GetEnvironmentVariable("DB_USER") ?? "postgres"};Password={Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres"}";
builder.Services.AddDbContext<EmergencyDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

// Add Redis Connection
try
{
    var redisConnection = ConnectionMultiplexer.Connect(
        Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379");
    builder.Services.AddSingleton(redisConnection);
}
catch
{
    Console.WriteLine("[WARNING] Redis unavailable, caching will be disabled");
}

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Services
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddSingleton<CircuitBreakerService>(new CircuitBreakerService(cbFailureThreshold, cbOpenSeconds));
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmergencyCommandService, EmergencyCommandService>();
builder.Services.AddScoped<IEmergencyQueryService, EmergencyQueryService>();

// Register CacheService based on Redis availability
builder.Services.AddScoped<CacheService>(sp =>
{
    try
    {
        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
        return new CacheService(redis, cacheEnabled, cacheTtlSeconds);
    }
    catch
    {
        var fakeRedis = ConnectionMultiplexer.Connect("localhost:6379");
        return new CacheService(fakeRedis, false, cacheTtlSeconds);
    }
});

var app = builder.Build();

// Middleware pipeline
app.UseRouting();
app.UseCors();

// Gatekeeper Middleware (BEFORE UseAuthentication)
var auditService = app.Services.CreateScope().ServiceProvider.GetRequiredService<AuditService>();
app.UseMiddleware<GatekeeperMiddleware>(jwtSecret, auditService);

app.UseAuthentication();
app.UseAuthorization();

// Response headers middleware
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    if (!headers.ContainsKey("X-Instance"))
        headers.Add("X-Instance", instanceName);
    if (!headers.ContainsKey("X-App-Role"))
        headers.Add("X-App-Role", appRole);
    if (!headers.ContainsKey("X-App-Version"))
        headers.Add("X-App-Version", appVersion);
    await next();
});

app.MapControllers();

// Health check endpoint (no DB query)
app.MapGet("/health", () => new
{
    status = "UP",
    instance = instanceName,
    version = appVersion,
    timestamp = DateTime.UtcNow
}).WithName("Health");

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");



