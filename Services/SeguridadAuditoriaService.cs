using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TFU_Architecture.Data;
using TFU_Architecture.Models;

namespace TFU_Architecture.Services;

// Componente: Seguridad y Auditoría - expone ISeguridad
// Autentica actores (login), transporta identidad (JWT) y registra auditoría
// de las acciones que realiza Asignación y Coordinación.
public interface ISeguridadAuditoriaService
{
    Task<Usuario?> AutenticarAsync(string username, string password);
    string GenerarToken(Usuario usuario);
    Task RegistrarAuditoriaAsync(int usuarioId, string accion, string detalle, string nodeId);
    Task<List<AuditLog>> ObtenerAuditoriaAsync();
}

public class SeguridadAuditoriaService : ISeguridadAuditoriaService
{
    private readonly AppDbContext _db;
    private readonly string _jwtSecret;

    public SeguridadAuditoriaService(AppDbContext db, string jwtSecret)
    {
        _db = db;
        _jwtSecret = jwtSecret;
    }

    public async Task<Usuario?> AutenticarAsync(string username, string password)
    {
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            return null;
        return usuario;
    }

    public string GenerarToken(Usuario usuario)
    {
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = new JwtSecurityTokenHandler().CreateToken(tokenDescriptor);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task RegistrarAuditoriaAsync(int usuarioId, string accion, string detalle, string nodeId)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UsuarioId = usuarioId,
            Accion = accion,
            Detalle = detalle,
            NodeId = nodeId
        });
        await _db.SaveChangesAsync();
    }

    public Task<List<AuditLog>> ObtenerAuditoriaAsync() =>
        _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.Timestamp).ToListAsync();
}
