namespace TFU_Architecture.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CrearEmergenciaRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int Gravedad { get; set; }
}

public class CrearRecursoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
}

public class AsignarRecursoRequest
{
    public int EmergenciaId { get; set; }
    public int RecursoId { get; set; }
}
