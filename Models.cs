namespace TFU_Architecture.Models;

// Patrón: Gatekeeper - Autenticación
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// Dominio: Gestión de Usuarios
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "OPERADOR"; // OPERADOR, ADMIN
}

// Dominio: Gestión de Emergencias
public class Emergency
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ClassificationId { get; set; }
    public int LocationLatitude { get; set; }
    public int LocationLongitude { get; set; }
    public string Status { get; set; } = "REGISTRADA"; // REGISTRADA, EN_PROCESO, RESUELTA
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Dominio: Gestión de Recursos
public class Resource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ResourceTypeId { get; set; }
    public string Status { get; set; } = "DISPONIBLE"; // DISPONIBLE, EN_USO, MANTENIMIENTO
    public DateTime UpdatedAt { get; set; }
}

public class ResourceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Ambulancia Básica, Ambulancia UTI, Médico, Helicóptero
    public string Description { get; set; } = string.Empty;
}

// Dominio: Clasificación y Priorización
public class Classification
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Urgencia, Emergencia, Catástrofe
    public int Priority { get; set; } // 1 (alta) a 5 (baja)
}

// Dominio: Asignación y Coordinación
public class ResourceAssignment
{
    public int Id { get; set; }
    public int EmergencyId { get; set; }
    public int ResourceId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
}

// Dominio: Seguridad y Auditoría
public class AuditLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Result { get; set; } = "OK"; // OK, RECHAZADO
    public int HttpCode { get; set; }
}

// Request/Response DTOs
public class CreateEmergencyRequest
{
    public string Description { get; set; } = string.Empty;
    public int ClassificationId { get; set; }
    public int LocationLatitude { get; set; }
    public int LocationLongitude { get; set; }
}

public class AssignResourceRequest
{
    public int EmergencyId { get; set; }
    public int ResourceId { get; set; }
}

public class UpdateEmergencyStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class CreateResourceTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateResourceTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class HealthResponse
{
    public string Status { get; set; } = "UP";
    public string Instance { get; set; } = string.Empty;
    public string Version { get; set; } = "v1";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CircuitBreakerStatus
{
    public string State { get; set; } = "CLOSED";
    public int FailureCount { get; set; } = 0;
    public DateTime? LastFailureTime { get; set; }
}

public class StatsResponse
{
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public long DbQueriesCatalog { get; set; }
}
