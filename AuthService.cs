using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TFU_Architecture.Data;
using TFU_Architecture.Models;

namespace TFU_Architecture.Services;

public interface IAuthService
{
    string GenerateToken(User user);
    Task<User?> AuthenticateAsync(string username, string password);
}

public class AuthService : IAuthService
{
    private readonly string _jwtSecret;
    private readonly EmergencyDbContext _db;

    public AuthService(string jwtSecret, EmergencyDbContext db)
    {
        _jwtSecret = jwtSecret;
        _db = db;
    }

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

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;
        return user;
    }
}
