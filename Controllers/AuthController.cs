using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TFU_Architecture.Models;
using TFU_Architecture.Services;

namespace TFU_Architecture.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
    private readonly ISeguridadAuditoriaService _seguridad;

    public AuthController(ISeguridadAuditoriaService seguridad) => _seguridad = seguridad;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = await _seguridad.AutenticarAsync(request.Username, request.Password);
        if (usuario == null)
            return Unauthorized(new { error = "Usuario o contraseña incorrectos" });

        var token = _seguridad.GenerarToken(usuario);
        return Ok(new { token, usuario = new { usuario.Id, usuario.Username, usuario.Rol } });
    }
}
