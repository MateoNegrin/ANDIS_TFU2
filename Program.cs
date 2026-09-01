using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TFU_Architecture;

var builder = WebApplication.CreateBuilder(args);

// Configurar JWT
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "secret-key-change-in-production";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

// Servicios
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddSingleton<IAuthService>(new AuthService(jwtSecret));

var app = builder.Build();

// Middleware
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoints simples
var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "node-default";

app.MapGet("/health", () => Results.Ok(new { status = "OK", nodeId, timestamp = DateTime.UtcNow }));

app.MapGet("/info", () => Results.Ok(new
{
    message = "API de Tácticas de Arquitectura",
    nodeId,
    tactics = new
    {
        performance = "Múltiples copias de cómputo (3 réplicas)",
        security = new[] { "Autenticar actores", "Identificar actores", "Autorizar actores" }
    }
}));

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Run($"http://0.0.0.0:{port}");

