using Microsoft.EntityFrameworkCore;
using TFU_Architecture.Models;

namespace TFU_Architecture.Data;

// Las 3 réplicas arrancan en paralelo y comparten la misma base: se reintenta
// la conexión (Postgres puede tardar en levantar) y se tolera que la semilla
// inicial choque contra otra réplica que ya la insertó (servicio sin estado).
public static class DbInitializer
{
    public static async Task InicializarAsync(AppDbContext db)
    {
        for (var intento = 1; intento <= 15; intento++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                break;
            }
            catch when (intento < 15)
            {
                await Task.Delay(2000);
            }
        }

        if (await db.Usuarios.AnyAsync())
            return;

        db.Usuarios.AddRange(
            new Usuario { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Rol = "admin" },
            new Usuario { Username = "operador", PasswordHash = BCrypt.Net.BCrypt.HashPassword("operador123"), Rol = "operador" }
        );

        db.Recursos.AddRange(
            new RecursoMedico { Nombre = "Ambulancia 1", Tipo = "Ambulancia", Ubicacion = "Base Norte", Disponible = true },
            new RecursoMedico { Nombre = "Ambulancia 2", Tipo = "Ambulancia", Ubicacion = "Base Sur", Disponible = true },
            new RecursoMedico { Nombre = "Cama UCI 1", Tipo = "Cama", Ubicacion = "Hospital Central", Disponible = true }
        );

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Otra réplica insertó la semilla primero: no es un error.
        }
    }
}
