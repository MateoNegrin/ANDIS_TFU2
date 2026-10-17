using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TFU_Architecture.Services;

namespace TFU_Architecture.Middleware;

// Patrón: Gatekeeper
public class GatekeeperMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _jwtSecret;
    private readonly AuditService _auditService;
    private readonly HashSet<string> _publicRoutes = new()
    {
        "/auth/login",
        "/health"
    };

    public GatekeeperMiddleware(RequestDelegate next, string jwtSecret, AuditService auditService)
    {
        _next = next;
        _jwtSecret = jwtSecret;
        _auditService = auditService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Rutas públicas: sin validación
        if (_publicRoutes.Any(r => path.StartsWith(r)))
        {
            await _next(context);
            return;
        }

        // Validar token JWT
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        var claims = ValidateToken(token);

        if (claims == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "No autorizado" });

            // Auditar rechazo
            if (token == null)
            {
                await _auditService.LogAsync("ANONYMOUS", "NONE", context.Request.Method, path, "RECHAZADO", 401);
            }
            return;
        }

        // Verificar permisos por ruta
        var username = claims.FindFirst(ClaimTypes.Name)?.Value ?? "UNKNOWN";
        var role = claims.FindFirst(ClaimTypes.Role)?.Value ?? "NONE";

        var hasPermission = CheckPermissions(path, context.Request.Method, role);

        if (!hasPermission)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Permiso insuficiente" });
            await _auditService.LogAsync(username, role, context.Request.Method, path, "RECHAZADO", 403);
            return;
        }

        // Usuario autenticado y autorizado
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims.Claims, "Bearer"));
        
        // Registrar en auditoría al final (se hace en el middleware de respuesta)
        var originalBodyStream = context.Response.Body;

        using (var memoryStream = new MemoryStream())
        {
            context.Response.Body = memoryStream;

            await _next(context);

            var statusCode = context.Response.StatusCode;
            var result = statusCode >= 200 && statusCode < 300 ? "OK" : "RECHAZADO";

            await _auditService.LogAsync(username, role, context.Request.Method, path, result, statusCode);

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
    }

    private ClaimsIdentity? ValidateToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal.Identity as ClaimsIdentity;
        }
        catch
        {
            return null;
        }
    }

    private bool CheckPermissions(string path, string method, string role)
    {
        // OPERADOR: lectura y registros de emergencias, asignaciones
        // ADMIN: todo

        if (role == "ADMIN") return true;

        if (role == "OPERADOR")
        {
            // Operador puede: GET (cualquiera), POST emergencias, PUT asignaciones
            if (method == "GET" || method == "HEAD") return true;
            if (path.StartsWith("/emergencias") && method == "POST") return true;
            if (path.StartsWith("/asignaciones") && (method == "POST" || method == "PUT")) return true;
            if (path.StartsWith("/recursos") && method == "GET") return true;
            return false;
        }

        return false;
    }
}
