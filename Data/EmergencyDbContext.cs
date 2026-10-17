using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Models;

namespace TFU_Architecture.Data;

public class EmergencyDbContext : DbContext
{
    public EmergencyDbContext(DbContextOptions<EmergencyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Emergency> Emergencies { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceType> ResourceTypes { get; set; }
    public DbSet<Classification> Classifications { get; set; }
    public DbSet<ResourceAssignment> ResourceAssignments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed usuarios
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "operador", PasswordHash = BCrypt.Net.BCrypt.HashPassword("operador123"), Role = "OPERADOR" },
            new User { Id = 2, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "ADMIN" }
        );

        // Seed clasificaciones
        modelBuilder.Entity<Classification>().HasData(
            new Classification { Id = 1, Name = "Urgencia", Priority = 3 },
            new Classification { Id = 2, Name = "Emergencia", Priority = 2 },
            new Classification { Id = 3, Name = "Catástrofe", Priority = 1 }
        );

        // Seed tipos de recursos
        modelBuilder.Entity<ResourceType>().HasData(
            new ResourceType { Id = 1, Name = "Ambulancia Básica", Description = "Ambulancia con equipamiento básico" },
            new ResourceType { Id = 2, Name = "Ambulancia UTI", Description = "Ambulancia con cuidados intensivos" },
            new ResourceType { Id = 3, Name = "Médico", Description = "Personal médico especializado" },
            new ResourceType { Id = 4, Name = "Helicóptero", Description = "Helicóptero de emergencias" }
        );

        // Seed recursos
        for (int i = 1; i <= 6; i++)
        {
            modelBuilder.Entity<Resource>().HasData(
                new Resource { Id = i, Name = $"Recurso-{i}", ResourceTypeId = ((i - 1) % 4) + 1, Status = "DISPONIBLE", UpdatedAt = DateTime.UtcNow }
            );
        }

        // Seed emergencias de ejemplo
        modelBuilder.Entity<Emergency>().HasData(
            new Emergency { Id = 1, Description = "Accidente vehicular en Av. Principal", ClassificationId = 2, LocationLatitude = -34, LocationLongitude = -58, Status = "RESUELTA", CreatedAt = DateTime.UtcNow.AddHours(-2), UpdatedAt = DateTime.UtcNow },
            new Emergency { Id = 2, Description = "Infarto en centro comercial", ClassificationId = 2, LocationLatitude = -34, LocationLongitude = -58, Status = "EN_PROCESO", CreatedAt = DateTime.UtcNow.AddMinutes(-30), UpdatedAt = DateTime.UtcNow }
        );
    }
}
