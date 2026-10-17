using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Data;
using TFU_Architecture.Models;
using TFU_Architecture.Services;

namespace TFU_Architecture.Commands;

public interface IEmergencyCommandService
{
    Task<Emergency> RegisterEmergencyAsync(CreateEmergencyRequest request);
    Task<ResourceAssignment> AssignResourceAsync(AssignResourceRequest request);
    Task<Emergency> UpdateEmergencyStatusAsync(int emergencyId, string status);
    Task<ResourceType> CreateResourceTypeAsync(string name, string description);
    Task<ResourceType> UpdateResourceTypeAsync(int id, string name, string description);
    Task DeleteResourceTypeAsync(int id);
}

public class EmergencyCommandService : IEmergencyCommandService
{
    private readonly EmergencyDbContext _db;
    private readonly CircuitBreakerService _circuitBreaker;
    private readonly AuditService _auditService;

    public EmergencyCommandService(
        EmergencyDbContext db,
        CircuitBreakerService circuitBreaker,
        AuditService auditService)
    {
        _db = db;
        _circuitBreaker = circuitBreaker;
        _auditService = auditService;
    }

    public async Task<Emergency> RegisterEmergencyAsync(CreateEmergencyRequest request)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var emergency = new Emergency
                {
                    Description = request.Description,
                    ClassificationId = request.ClassificationId,
                    LocationLatitude = request.LocationLatitude,
                    LocationLongitude = request.LocationLongitude,
                    Status = "REGISTRADA",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Emergencies.Add(emergency);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return emergency;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<ResourceAssignment> AssignResourceAsync(AssignResourceRequest request)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // DB-level locking with SELECT ... FOR UPDATE
                var resource = await _db.Resources
                    .FromSqlInterpolated($"SELECT * FROM public.\"Resources\" WHERE \"Id\" = {request.ResourceId} FOR UPDATE")
                    .FirstOrDefaultAsync();

                if (resource == null)
                    throw new InvalidOperationException($"Resource {request.ResourceId} not found");

                if (resource.Status != "DISPONIBLE")
                    throw new InvalidOperationException($"Resource is {resource.Status}, not available");

                var assignment = new ResourceAssignment
                {
                    EmergencyId = request.EmergencyId,
                    ResourceId = request.ResourceId,
                    AssignedAt = DateTime.UtcNow
                };

                resource.Status = "EN_USO";
                resource.UpdatedAt = DateTime.UtcNow;

                _db.ResourceAssignments.Add(assignment);
                _db.Resources.Update(resource);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return assignment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<Emergency> UpdateEmergencyStatusAsync(int emergencyId, string status)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var emergency = await _db.Emergencies.FindAsync(emergencyId);
                if (emergency == null)
                    throw new InvalidOperationException($"Emergency {emergencyId} not found");

                emergency.Status = status;
                emergency.UpdatedAt = DateTime.UtcNow;

                _db.Emergencies.Update(emergency);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return emergency;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<ResourceType> CreateResourceTypeAsync(string name, string description)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var resourceType = new ResourceType
                {
                    Name = name,
                    Description = description
                };

                _db.ResourceTypes.Add(resourceType);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache
                await _db.Database.ExecuteSqlAsync($"SELECT 1");

                return resourceType;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<ResourceType> UpdateResourceTypeAsync(int id, string name, string description)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var resourceType = await _db.ResourceTypes.FindAsync(id);
                if (resourceType == null)
                    throw new InvalidOperationException($"ResourceType {id} not found");

                resourceType.Name = name;
                resourceType.Description = description;

                _db.ResourceTypes.Update(resourceType);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return resourceType;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task DeleteResourceTypeAsync(int id)
    {
        await _circuitBreaker.ExecuteAsync<object>(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var resourceType = await _db.ResourceTypes.FindAsync(id);
                if (resourceType == null)
                    throw new InvalidOperationException($"ResourceType {id} not found");

                _db.ResourceTypes.Remove(resourceType);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            return null;
        });
    }
}
