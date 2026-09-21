using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TFU_Architecture.Models;
using TFU_Architecture.Services;

namespace TFU_Architecture.Controllers;

[ApiController]
[Route("")]
[Authorize]
public class EmergenciasController : ControllerBase
{
    private readonly IEmergenciasService _emergencias;

    public EmergenciasController(IEmergenciasService emergencias) => _emergencias = emergencias;

    [HttpGet("emergencias")]
    public async Task<IActionResult> Listar() => Ok(new { emergencias = await _emergencias.ListarAsync() });

    [HttpGet("emergencias/{id}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var emergencia = await _emergencias.ObtenerAsync(id);
        return emergencia == null ? NotFound(new { error = "Emergencia no encontrada" }) : Ok(emergencia);
    }

    [HttpPost("emergencias")]
    public async Task<IActionResult> Crear([FromBody] CrearEmergenciaRequest request)
    {
        var emergencia = await _emergencias.CrearAsync(request.Descripcion, request.Ubicacion, request.Gravedad);
        return Ok(new { message = "Emergencia registrada", emergencia });
    }
}
