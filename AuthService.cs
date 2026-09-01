using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TFU_Architecture.Models;

namespace TFU_Architecture;

public interface IAuthService
{
    string GenerateToken(User user);
    System.Threading.Tasks.Task<User?> AuthenticateAsync(string username, string password);
    List<User> Users { get; }
}

public class AuthService : IAuthService
{
    private readonly string _jwtSecret;
    
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "admin" },
        new User { Id = 2, Username = "user", PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"), Role = "user" }
    };

    public static readonly List<Models.Task> Tasks = new()
    {
        new Models.Task { Id = 1, Title = "Tarea 1", Description = "Primera", UserId = 1 },
        new Models.Task { Id = 2, Title = "Tarea 2", Description = "Segunda", UserId = 2 }
    };

    public List<User> Users => _users;

    public AuthService(string jwtSecret)
    {
        _jwtSecret = jwtSecret;
    }

    // Verificar credenciales del usuario
    public async System.Threading.Tasks.Task<User?> AuthenticateAsync(string username, string password)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;
            return user;
        });
    }

    // Generar JWT token con la identidad del usuario
    public string GenerateToken(User user)
    {
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
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
}
