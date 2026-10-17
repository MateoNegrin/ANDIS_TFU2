using TFU_Architecture.Data;
using TFU_Architecture.Models;

namespace TFU_Architecture.Services;

// Patrón: Gatekeeper & Dominio: Seguridad y Auditoría
public class AuditService
{
    private readonly EmergencyDbContext _db;

    public AuditService(EmergencyDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string username, string role, string method, string route, string result, int httpCode)
    {
        var log = new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            Username = username,
            Role = role,
            Method = method,
            Route = route,
            Result = result,
            HttpCode = httpCode
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
