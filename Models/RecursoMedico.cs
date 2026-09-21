namespace TFU_Architecture.Models;

public class RecursoMedico
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty; // Ambulancia, Cama, Medico
    public string Ubicacion { get; set; } = string.Empty;
    public bool Disponible { get; set; } = true;
}
