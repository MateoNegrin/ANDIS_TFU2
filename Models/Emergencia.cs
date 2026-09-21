namespace TFU_Architecture.Models;

public class Emergencia
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int Gravedad { get; set; } // 1-10, entrada para la clasificación
    public NivelPrioridad NivelPrioridad { get; set; }
    public EstadoEmergencia Estado { get; set; } = EstadoEmergencia.Pendiente;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
