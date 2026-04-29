using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

/// <summary>Authentication and user registration endpoints</summary>
public class AuthController : BaseController
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    { _auth = auth; _logger = logger; }

    /// <summary>Authenticate and receive JWT tokens</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromQuery] int tenantId = 1, CancellationToken ct = default)
    {
        try
        {
            var result = await _auth.LoginAsync(request.Email, request.Password, tenantId, ct);
            if (result == null)
                return Unauthorized(new ProblemDetails { Title = "Invalid credentials", Status = 401 });
            return Ok(result);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Register a new user (and tenant if new subdomain)</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct = default)
    {
        try
        {
            var user = await _auth.RegisterAsync(request, ct);
            return CreatedAtAction(nameof(Login), user);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Refresh access token using refresh token</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct = default)
    {
        var result = await _auth.RefreshAsync(request.RefreshToken, ct);
        if (result == null) return Unauthorized(new ProblemDetails { Title = "Invalid or expired refresh token", Status = 401 });
        return Ok(result);
    }

    /// <summary>Revoke refresh token (logout)</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct = default)
    {
        await _auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
