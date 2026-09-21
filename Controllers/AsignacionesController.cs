using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TFU_Architecture.Models;
using TFU_Architecture.Services;

namespace TFU_Architecture.Controllers;

[ApiController]
[Route("")]
[Authorize]
public class AsignacionesController : ControllerBase
{
    private readonly IAsignacionCoordinacionService _asignacion;
    private readonly string _nodeId;

    public AsignacionesController(IAsignacionCoordinacionService asignacion)
    {
        _asignacion = asignacion;
        _nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "node-default";
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("asignaciones")]
    public async Task<IActionResult> Listar() =>
        Ok(new { asignaciones = await _asignacion.ListarAsync(), nodeId = _nodeId });

    [HttpPost("asignaciones")]
    public async Task<IActionResult> Asignar([FromBody] AsignarRecursoRequest request)
    {
        var resultado = await _asignacion.AsignarAsync(request.EmergenciaId, request.RecursoId, GetUserId(), _nodeId);
        if (!resultado.Exitosa)
            return Conflict(new { error = resultado.Mensaje, nodeId = _nodeId });

        return Ok(new { message = resultado.Mensaje, asignacion = resultado.Asignacion, nodeId = _nodeId });
    }
}
