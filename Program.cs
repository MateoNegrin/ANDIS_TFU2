using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TFU_Architecture.Data;
using TFU_Architecture.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "secret-key-change-in-production";
var key = Encoding.ASCII.GetBytes(jwtSecret);

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=emergencias;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

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

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddCors();

// Cada componente del modelo UML se registra como su propia interfaz/implementación.
builder.Services.AddScoped<IRecursosMedicosService, RecursosMedicosService>();
builder.Services.AddScoped<IClasificacionService, ClasificacionPriorizacionService>();
builder.Services.AddScoped<IEmergenciasService, EmergenciasService>();
builder.Services.AddScoped<ISeguridadAuditoriaService>(sp =>
    new SeguridadAuditoriaService(sp.GetRequiredService<AppDbContext>(), jwtSecret));
builder.Services.AddScoped<IAsignacionCoordinacionService, AsignacionCoordinacionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InicializarAsync(db);
}

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "node-default";

app.MapGet("/health", () => Results.Ok(new { status = "OK", nodeId, timestamp = DateTime.UtcNow }));

app.MapGet("/info", () => Results.Ok(new
{
    message = "API de Gestión de Emergencias Médicas",
    nodeId,
    componentes = new[]
    {
        "Gestión de Emergencias", "Gestión de Recursos Médicos", "Clasificación y Priorización",
        "Asignación y Coordinación", "Seguridad y Auditoría"
    },
    tacticas = new
    {
        rendimiento = "Múltiples copias de cómputo (3 réplicas) + balanceo Nginx (round-robin)",
        consistencia = "ACID: transacción con bloqueo de fila (SELECT ... FOR UPDATE) al asignar un recurso",
        estado = "Servicios sin estado: identidad viaja en JWT, estado persistido en PostgreSQL compartido"
    }
}));

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Run($"http://0.0.0.0:{port}");
