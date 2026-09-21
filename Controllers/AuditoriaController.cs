using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TFU_Architecture.Services;

namespace TFU_Architecture.Controllers;

[ApiController]
[Route("")]
[Authorize(Roles = "admin")]
public class AuditoriaController : ControllerBase
{
    private readonly ISeguridadAuditoriaService _seguridad;

    public AuditoriaController(ISeguridadAuditoriaService seguridad) => _seguridad = seguridad;

    [HttpGet("auditoria")]
    public async Task<IActionResult> Listar() => Ok(new { auditoria = await _seguridad.ObtenerAuditoriaAsync() });
}
