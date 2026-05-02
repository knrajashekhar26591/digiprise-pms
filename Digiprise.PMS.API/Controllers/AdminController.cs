using Digiprise.PMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminController : BaseController
{
    private readonly ISystemAdminService _admin;
    public AdminController(ISystemAdminService admin) => _admin = admin;

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(CancellationToken ct)
        => Ok(await _admin.GetTenantsAsync(ct));

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken ct)
        => Ok(await _admin.CreateTenantAsync(request.Name, request.Subdomain, ct));

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request, CancellationToken ct)
        => Ok(new { Token = await _admin.InviteUserAsync(request.TenantId, request.Email, request.Role, ct) });

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
        => Ok(await _admin.GetSystemSettingsAsync(ct));

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] string value, CancellationToken ct)
    {
        await _admin.UpdateSystemSettingAsync(key, value, ct);
        return NoContent();
    }
}

public record CreateTenantRequest(string Name, string Subdomain);
public record InviteUserRequest(int TenantId, string Email, string Role);
