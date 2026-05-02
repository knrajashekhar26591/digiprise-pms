using System.Security.Claims;
using System.Text.Encodings.Web;
using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Digiprise.PMS.API.Middleware;

/// <summary>
/// Custom JWT authentication handler — uses only built-in ASP.NET 8 types.
/// No Microsoft.AspNetCore.Authentication.JwtBearer NuGet package needed.
/// </summary>
public class JwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IJwtService _jwtService;

    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IJwtService jwtService)
        : base(options, logger, encoder)
    {
        _jwtService = jwtService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string token = string.Empty;

        // Check Authorization header
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = headerValue["Bearer ".Length..].Trim();
            }
        }

        // Also support SignalR query string token
        if (string.IsNullOrEmpty(token))
        {
            token = Request.Query["access_token"].ToString();
            if (string.IsNullOrEmpty(token) || !Request.Path.StartsWithSegments("/hubs"))
                return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var (userId, tenantId) = _jwtService.ValidateAccessToken(token);

            // Parse roles from the token manually
            var parts = token.Split('.');
            if (parts.Length == 3)
            {
                var payloadJson = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(parts[1].Replace('-', '+').Replace('_', '/') +
                    new string('=', (4 - parts[1].Length % 4) % 4)));

                var claims = new List<Claim>
                {
                    new("sub", userId.ToString()),
                    new(ClaimTypes.NameIdentifier, userId.ToString()),
                    new("tenantId", tenantId.ToString()),
                };

                // Parse email and roles from payload
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                    if (doc.RootElement.TryGetProperty("email", out var emailEl))
                        claims.Add(new Claim(ClaimTypes.Email, emailEl.GetString() ?? ""));
                    if (doc.RootElement.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var role in rolesEl.EnumerateArray())
                            claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                }
                catch { /* non-critical */ }

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid token structure."));
        }
        catch (SecurityTokenException ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "JWT authentication failed");
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed."));
        }
    }
}
