using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TFU_Architecture.Models;
using TFU_Architecture.Services;

namespace TFU_Architecture.Controllers;

[ApiController]
[Route("")]
[Authorize]
public class RecursosController : ControllerBase
{
    private readonly IRecursosMedicosService _recursos;

    public RecursosController(IRecursosMedicosService recursos) => _recursos = recursos;

    [HttpGet("recursos")]
    public async Task<IActionResult> Listar() => Ok(new { recursos = await _recursos.ListarAsync() });

    [HttpGet("recursos/{id}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var recurso = await _recursos.ObtenerAsync(id);
        return recurso == null ? NotFound(new { error = "Recurso no encontrado" }) : Ok(recurso);
    }

    [HttpPost("recursos")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Crear([FromBody] CrearRecursoRequest request)
    {
        var recurso = await _recursos.CrearAsync(new RecursoMedico
        {
            Nombre = request.Nombre,
            Tipo = request.Tipo,
            Ubicacion = request.Ubicacion,
            Disponible = true
        });
        return Ok(new { message = "Recurso creado", recurso });
    }
}
