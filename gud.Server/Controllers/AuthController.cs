using gud.Server.DTO;
using gud.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace gud.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, error, response) = await authService.RegisterAsync(request);
        return success ? Ok(response) : BadRequest(error);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, response) = await authService.LoginAsync(request);
        return success ? Ok(response) : BadRequest(error);
    }
}