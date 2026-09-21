using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Data;
using TFU_Architecture.Models;

namespace TFU_Architecture.Services;

// Componente: Gestión de Emergencias - expone IEmergencias, consume IClasificacion
public interface IEmergenciasService
{
    Task<List<Emergencia>> ListarAsync();
    Task<Emergencia?> ObtenerAsync(int id);
    Task<Emergencia> CrearAsync(string descripcion, string ubicacion, int gravedad);
}

public class EmergenciasService : IEmergenciasService
{
    private readonly AppDbContext _db;
    private readonly IClasificacionService _clasificacion;

    public EmergenciasService(AppDbContext db, IClasificacionService clasificacion)
    {
        _db = db;
        _clasificacion = clasificacion;
    }

    public Task<List<Emergencia>> ListarAsync() =>
        _db.Emergencias.AsNoTracking().ToListAsync();

    public Task<Emergencia?> ObtenerAsync(int id) =>
        _db.Emergencias.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Emergencia> CrearAsync(string descripcion, string ubicacion, int gravedad)
    {
        var emergencia = new Emergencia
        {
            Descripcion = descripcion,
            Ubicacion = ubicacion,
            Gravedad = gravedad,
            NivelPrioridad = _clasificacion.Clasificar(gravedad),
            Estado = EstadoEmergencia.Pendiente
        };
        _db.Emergencias.Add(emergencia);
        await _db.SaveChangesAsync();
        return emergencia;
    }
}
