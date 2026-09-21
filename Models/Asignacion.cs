namespace TFU_Architecture.Models;

public class Asignacion
{
    public int Id { get; set; }
    public int EmergenciaId { get; set; }
    public int RecursoId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
