using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Patients.Api.Features.Auth.Application;
using Patients.Api.Features.Auth.Contracts;

namespace Patients.Api.Features.Auth.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var (success, error) = await authService.RegisterAsync(request, cancellationToken);
        if (!success)
        {
            if (error?.Contains("already exists", StringComparison.OrdinalIgnoreCase) is true)
            {
                return Conflict(new { message = error });
            }

            return BadRequest(new { message = error });
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var (success, error, response) = await authService.LoginAsync(request, cancellationToken);
        if (!success)
        {
            return Unauthorized(new { message = error });
        }

        return Ok(response);
    }
}
