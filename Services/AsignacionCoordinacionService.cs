using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Data;
using TFU_Architecture.Models;

namespace TFU_Architecture.Services;

public record AsignacionResultado(bool Exitosa, string Mensaje, Asignacion? Asignacion);

// Componente: Asignación y Coordinación - expone IAsignacion, consume
// IRecursos/IEmergencias (vía el propio DbContext) y ISeguridad para auditar.
public interface IAsignacionCoordinacionService
{
    Task<AsignacionResultado> AsignarAsync(int emergenciaId, int recursoId, int usuarioId, string nodeId);
    Task<List<Asignacion>> ListarAsync();
}

public class AsignacionCoordinacionService : IAsignacionCoordinacionService
{
    private readonly AppDbContext _db;
    private readonly ISeguridadAuditoriaService _seguridad;

    public AsignacionCoordinacionService(AppDbContext db, ISeguridadAuditoriaService seguridad)
    {
        _db = db;
        _seguridad = seguridad;
    }

    // Táctica ACID: la transacción toma un bloqueo de fila (SELECT ... FOR UPDATE)
    // sobre el recurso. Si dos asignaciones concurrentes compiten por el mismo
    // recurso, la segunda espera a que la primera confirme (o revierta) y recién
    // ahí lee el estado real, evitando la doble asignación (atomicidad + consistencia).
    public async Task<AsignacionResultado> AsignarAsync(int emergenciaId, int recursoId, int usuarioId, string nodeId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var recurso = await _db.Recursos
                .FromSqlInterpolated($"SELECT * FROM \"Recursos\" WHERE \"Id\" = {recursoId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (recurso == null)
            {
                await tx.RollbackAsync();
                return new AsignacionResultado(false, "Recurso no encontrado", null);
            }
            if (!recurso.Disponible)
            {
                await tx.RollbackAsync();
                return new AsignacionResultado(false, "Recurso no disponible: ya fue asignado", null);
            }

            var emergencia = await _db.Emergencias.FirstOrDefaultAsync(e => e.Id == emergenciaId);
            if (emergencia == null)
            {
                await tx.RollbackAsync();
                return new AsignacionResultado(false, "Emergencia no encontrada", null);
            }

            recurso.Disponible = false;
            emergencia.Estado = EstadoEmergencia.Asignada;

            var asignacion = new Asignacion
            {
                EmergenciaId = emergenciaId,
                RecursoId = recursoId,
                UsuarioId = usuarioId
            };
            _db.Asignaciones.Add(asignacion);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _seguridad.RegistrarAuditoriaAsync(usuarioId, "ASIGNACION",
                $"Recurso {recursoId} asignado a emergencia {emergenciaId}", nodeId);

            return new AsignacionResultado(true, "Recurso asignado correctamente", asignacion);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public Task<List<Asignacion>> ListarAsync() =>
        _db.Asignaciones.AsNoTracking().ToListAsync();
}
