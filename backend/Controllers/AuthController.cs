using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto dto)
    {
        Console.WriteLine($"REGISTER HIT — PlayerId: {dto.PlayerId}");
        return Ok();
    }
}

public class RegisterDto
{
    public string PlayerId { get; set; }
    public string AccessToken { get; set; }
}
