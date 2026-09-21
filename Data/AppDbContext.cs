using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Models;

namespace TFU_Architecture.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RecursoMedico> Recursos => Set<RecursoMedico>();
    public DbSet<Emergencia> Emergencias => Set<Emergencia>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Emergencia>().Property(e => e.NivelPrioridad).HasConversion<string>();
        modelBuilder.Entity<Emergencia>().Property(e => e.Estado).HasConversion<string>();
    }
}
