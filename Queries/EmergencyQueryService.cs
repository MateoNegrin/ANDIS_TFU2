using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Data;
using TFU_Architecture.Models;
using TFU_Architecture.Services;

namespace TFU_Architecture.Queries;

public interface IEmergencyQueryService
{
    Task<List<Emergency>> GetEmergenciesAsync(string? statusFilter = null);
    Task<Emergency?> GetEmergencyByIdAsync(int id);
    Task<List<Resource>> GetResourcesAsync();
    Task<List<Resource>> GetResourcesByTypeAsync(int typeId);
    Task<List<ResourceType>> GetResourceTypesAsync();
    Task<List<ResourceAssignment>> GetAssignmentsAsync();
    Task<List<Classification>> GetClassificationsAsync();
}

public class EmergencyQueryService : IEmergencyQueryService
{
    private readonly EmergencyDbContext _db;
    private readonly CacheService _cache;

    public EmergencyQueryService(EmergencyDbContext db, CacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<Emergency>> GetEmergenciesAsync(string? statusFilter = null)
    {
        var query = _db.Emergencies.AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
            query = query.Where(e => e.Status == statusFilter);

        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    public async Task<Emergency?> GetEmergencyByIdAsync(int id)
    {
        return await _db.Emergencies.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Resource>> GetResourcesAsync()
    {
        return await _db.Resources.OrderBy(r => r.Id).ToListAsync();
    }

    public async Task<List<Resource>> GetResourcesByTypeAsync(int typeId)
    {
        return await _db.Resources
            .Where(r => r.ResourceTypeId == typeId)
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<List<ResourceType>> GetResourceTypesAsync()
    {
        return await _cache.GetAsync(
            "resource_types",
            async () => await _db.ResourceTypes.OrderBy(rt => rt.Id).ToListAsync()
        ) ?? new();
    }

    public async Task<List<ResourceAssignment>> GetAssignmentsAsync()
    {
        return await _db.ResourceAssignments
            .OrderByDescending(ra => ra.AssignedAt)
            .ToListAsync();
    }

    public async Task<List<Classification>> GetClassificationsAsync()
    {
        return await _cache.GetAsync(
            "classifications",
            async () => await _db.Classifications.OrderBy(c => c.Priority).ToListAsync()
        ) ?? new();
    }
}
