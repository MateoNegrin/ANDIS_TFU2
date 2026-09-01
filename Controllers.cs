using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TFU_Architecture.Models;

namespace TFU_Architecture.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
            return Unauthorized(new { error = "Usuario o contraseña incorrectos" });

        var token = _authService.GenerateToken(user);
        return Ok(new { token, user = new { user.Id, user.Username, user.Role } });
    }
}

[ApiController]
[Route("")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IAuthService _authService;

    public TasksController(IAuthService authService)
    {
        _authService = authService;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("tasks")]
    public IActionResult GetTasks()
    {
        var userId = GetUserId();
        var tasks = AuthService.Tasks.Where(t => t.UserId == userId).ToList();
        return Ok(new { tasks });
    }

    [HttpPost("tasks")]
    public IActionResult CreateTask([FromBody] Models.Task taskRequest)
    {
        var task = new Models.Task
        {
            Id = AuthService.Tasks.Count + 1,
            Title = taskRequest.Title,
            Description = taskRequest.Description ?? "",
            UserId = GetUserId()
        };
        AuthService.Tasks.Add(task);
        return Ok(new { message = "Tarea creada", task });
    }

    [HttpDelete("tasks/{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult DeleteTask(int id)
    {
        var task = AuthService.Tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return NotFound(new { error = "Tarea no encontrada" });

        AuthService.Tasks.Remove(task);
        return Ok(new { message = "Tarea eliminada" });
    }
}

[ApiController]
[Route("")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var users = _authService.Users.Select(u => new { u.Id, u.Username, u.Role }).ToList();
        return Ok(new { users });
    }
}
