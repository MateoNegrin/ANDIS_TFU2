using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Data;
using TFU_Architecture.Models;

namespace TFU_Architecture.Services;

// Componente: Gestión de Recursos Médicos - expone IRecursos
public interface IRecursosMedicosService
{
    Task<List<RecursoMedico>> ListarAsync();
    Task<RecursoMedico?> ObtenerAsync(int id);
    Task<RecursoMedico> CrearAsync(RecursoMedico recurso);
}

public class RecursosMedicosService : IRecursosMedicosService
{
    private readonly AppDbContext _db;

    public RecursosMedicosService(AppDbContext db) => _db = db;

    public Task<List<RecursoMedico>> ListarAsync() =>
        _db.Recursos.AsNoTracking().ToListAsync();

    public Task<RecursoMedico?> ObtenerAsync(int id) =>
        _db.Recursos.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<RecursoMedico> CrearAsync(RecursoMedico recurso)
    {
        _db.Recursos.Add(recurso);
        await _db.SaveChangesAsync();
        return recurso;
    }
}
