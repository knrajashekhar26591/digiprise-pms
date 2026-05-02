using Digiprise.PMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Digiprise.PMS.API.Services;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out int id))
                    return id;
            }
            return 1; // Default/Fallback
        }
    }

    public int TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            // Allow explicit tenant header override for API requests
            if (httpContext?.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) == true)
            {
                if (int.TryParse(tenantHeader, out int tenantId))
                    return tenantId;
            }

            var user = httpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = user.FindFirst("TenantId");
                if (tenantClaim != null && int.TryParse(tenantClaim.Value, out int id))
                    return id;
            }
            return 1; // Default fallback to tenant 1
        }
    }

    public string Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? "admin@digiprise.com";

    public string[] Roles => _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public bool IsAdmin => Roles.Contains("SystemAdmin") || Roles.Contains("TenantAdmin");
}
