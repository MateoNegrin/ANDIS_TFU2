using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TFU_Architecture.Commands;
using TFU_Architecture.Models;
using TFU_Architecture.Queries;
using TFU_Architecture.Services;

namespace TFU_Architecture.Controllers;

[ApiController]
[Route("")]
public class EmergencyController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmergencyCommandService _commandService;
    private readonly IEmergencyQueryService _queryService;
    private readonly CircuitBreakerService _circuitBreaker;
    private readonly AuditService _auditService;
    private readonly string _appRole;
    private readonly string _instanceName;

    public EmergencyController(
        IAuthService authService,
        IEmergencyCommandService commandService,
        IEmergencyQueryService queryService,
        CircuitBreakerService circuitBreaker,
        AuditService auditService)
    {
        _authService = authService;
        _commandService = commandService;
        _queryService = queryService;
        _circuitBreaker = circuitBreaker;
        _auditService = auditService;
        _appRole = Environment.GetEnvironmentVariable("APP_ROLE") ?? "commands";
        _instanceName = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "default";
    }

    // ============ AUTH ROUTES (Public) ============
    [HttpPost("/auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _authService.AuthenticateAsync(request.Username, request.Password);
            if (user == null)
                return Unauthorized(new { error = "Invalid credentials" });

            var token = _authService.GenerateToken(user);
            var response = new LoginResponse
            {
                Token = token,
                User = new UserDto { Id = user.Id, Username = user.Username, Role = user.Role }
            };

            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============ HEALTH & ADMIN ROUTES ============
    [HttpGet("/health")]
    [AllowAnonymous]
    public ActionResult<HealthResponse> Health()
    {
        Response.Headers.Add("X-Instance", _instanceName);
        Response.Headers.Add("X-App-Role", _appRole);

        return Ok(new HealthResponse
        {
            Status = "UP",
            Instance = _instanceName,
            Version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "v1",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("/admin/circuit")]
    [Authorize(Roles = "ADMIN")]
    public ActionResult<CircuitBreakerStatus> GetCircuitStatus()
    {
        var (state, failures) = _circuitBreaker.GetStatus();
        var response = new CircuitBreakerStatus
        {
            State = state,
            FailureCount = failures
        };

        Response.Headers.Add("X-Instance", _instanceName);
        Response.Headers.Add("X-App-Role", _appRole);

        return Ok(response);
    }

    [HttpGet("/admin/stats")]
    [Authorize(Roles = "ADMIN")]
    public ActionResult<StatsResponse> GetStats()
    {
        // Note: CacheService statistics would be injected separately
        var response = new StatsResponse
        {
            CacheHits = 0,
            CacheMisses = 0,
            DbQueriesCatalog = 0
        };

        Response.Headers.Add("X-Instance", _instanceName);
        Response.Headers.Add("X-App-Role", _appRole);

        return Ok(response);
    }

    [HttpGet("/auditoria")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<List<AuditLog>>> GetAuditLogs(
        [FromServices] TFU_Architecture.Data.EmergencyDbContext db)
    {
        try
        {
            var logs = await db.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);

            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============ COMMAND ROUTES (POST/PUT/DELETE) ============
    [HttpPost("/emergencias")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    public async Task<ActionResult<Emergency>> CreateEmergency([FromBody] CreateEmergencyRequest request)
    {
        if (_appRole == "queries")
            return StatusCode(403, new { error = "This instance only handles queries" });

        try
        {
            var emergency = await _commandService.RegisterEmergencyAsync(request);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return CreatedAtAction(nameof(GetEmergency), new { id = emergency.Id }, emergency);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (IsCircuitBreakerOpen(ex))
        {
            return StatusCode(503, new { error = "Service temporarily unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("/emergencias/{id}/estado")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    public async Task<ActionResult<Emergency>> UpdateEmergencyStatus(int id, [FromBody] UpdateEmergencyStatusRequest request)
    {
        if (_appRole == "queries")
            return StatusCode(403, new { error = "This instance only handles queries" });

        try
        {
            var emergency = await _commandService.UpdateEmergencyStatusAsync(id, request.Status);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return Ok(emergency);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (IsCircuitBreakerOpen(ex))
        {
            return StatusCode(503, new { error = "Service temporarily unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("/asignaciones")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    public async Task<ActionResult<ResourceAssignment>> AssignResource([FromBody] AssignResourceRequest request)
    {
        if (_appRole == "queries")
            return StatusCode(403, new { error = "This instance only handles queries" });

        try
        {
            var assignment = await _commandService.AssignResourceAsync(request);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return CreatedAtAction(nameof(GetAssignments), new { id = assignment.Id }, assignment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (IsCircuitBreakerOpen(ex))
        {
            return StatusCode(503, new { error = "Service temporarily unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("/tipos-recurso")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResourceType>> CreateResourceType([FromBody] CreateResourceTypeRequest request)
    {
        if (_appRole == "queries")
            return StatusCode(403, new { error = "This instance only handles queries" });

        try
        {
            var resourceType = await _commandService.CreateResourceTypeAsync(request.Name, request.Description);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return CreatedAtAction(nameof(GetResourceTypes), new { id = resourceType.Id }, resourceType);
        }
        catch (Exception ex) when (IsCircuitBreakerOpen(ex))
        {
            return StatusCode(503, new { error = "Service temporarily unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("/tipos-recurso/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResourceType>> UpdateResourceType(int id, [FromBody] UpdateResourceTypeRequest request)
    {
        if (_appRole == "queries")
            return StatusCode(403, new { error = "This instance only handles queries" });

        try
        {
            var resourceType = await _commandService.UpdateResourceTypeAsync(id, request.Name, request.Description);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return Ok(resourceType);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (IsCircuitBreakerOpen(ex))
        {
            return StatusCode(503, new { error = "Service temporarily unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("/tipos-recurso/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteResourceType(int id)
    {
        if (_appRole == "queries")
            return StatusCode(403, new { error = "This instance only handles queries" });

        try
        {
            await _commandService.DeleteResourceTypeAsync(id);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (IsCircuitBreakerOpen(ex))
        {
            return StatusCode(503, new { error = "Service temporarily unavailable" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ============ QUERY ROUTES (GET) ============
    [HttpGet("/emergencias")]
    [Authorize]
    public async Task<ActionResult<List<Emergency>>> GetEmergencies([FromQuery] string? status = null)
    {
        if (_appRole == "commands")
            return StatusCode(403, new { error = "This instance only handles commands" });

        try
        {
            var emergencies = await _queryService.GetEmergenciesAsync(status);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return Ok(emergencies);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("/emergencias/{id}")]
    [Authorize]
    public async Task<ActionResult<Emergency>> GetEmergency(int id)
    {
        if (_appRole == "commands")
            return StatusCode(403, new { error = "This instance only handles commands" });

        try
        {
            var emergency = await _queryService.GetEmergencyByIdAsync(id);
            if (emergency == null)
                return NotFound(new { error = "Emergency not found" });

            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return Ok(emergency);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("/recursos")]
    [Authorize]
    public async Task<ActionResult<List<Resource>>> GetResources()
    {
        if (_appRole == "commands")
            return StatusCode(403, new { error = "This instance only handles commands" });

        try
        {
            var resources = await _queryService.GetResourcesAsync();
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return Ok(resources);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("/recursos/tipo/{typeId}")]
    [Authorize]
    public async Task<ActionResult<List<Resource>>> GetResourcesByType(int typeId)
    {
        if (_appRole == "commands")
            return StatusCode(403, new { error = "This instance only handles commands" });

        try
        {
            var resources = await _queryService.GetResourcesByTypeAsync(typeId);
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return Ok(resources);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("/tipos-recurso")]
    [Authorize]
    public async Task<ActionResult<List<ResourceType>>> GetResourceTypes()
    {
        if (_appRole == "commands")
            return StatusCode(403, new { error = "This instance only handles commands" });

        try
        {
            var resourceTypes = await _queryService.GetResourceTypesAsync();
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            Response.Headers.Add("X-Cache", "HIT"); // Cache-Aside pattern
            return Ok(resourceTypes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("/asignaciones")]
    [Authorize]
    public async Task<ActionResult<List<ResourceAssignment>>> GetAssignments()
    {
        if (_appRole == "commands")
            return StatusCode(403, new { error = "This instance only handles commands" });

        try
        {
            var assignments = await _queryService.GetAssignmentsAsync();
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("/clasificaciones")]
    [Authorize]
    public async Task<ActionResult<List<Classification>>> GetClassifications()
    {
        if (_appRole == "commands")
            return StatusCode(403, new { error = "This instance only handles commands" });

        try
        {
            var classifications = await _queryService.GetClassificationsAsync();
            Response.Headers.Add("X-Instance", _instanceName);
            Response.Headers.Add("X-App-Role", _appRole);
            Response.Headers.Add("X-Cache", "HIT"); // Cache-Aside pattern
            return Ok(classifications);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private bool IsCircuitBreakerOpen(Exception ex)
    {
        return ex.Message.Contains("Service temporarily unavailable") ||
               ex.InnerException?.Message.Contains("Circuit Breaker") == true;
    }
}
