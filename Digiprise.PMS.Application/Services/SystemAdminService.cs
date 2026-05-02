using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;


namespace Digiprise.PMS.Application.Services;

public class SystemAdminService : ISystemAdminService
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<SystemSetting> _settings;
    private readonly IRepository<UserInvite> _invites;
    private readonly ILogger<SystemAdminService> _logger;

    public SystemAdminService(
        IRepository<Tenant> tenants,
        IRepository<SystemSetting> settings,
        IRepository<UserInvite> invites,
        ILogger<SystemAdminService> logger)
    {
        _tenants = tenants;
        _settings = settings;
        _invites = invites;
        _logger = logger;
    }

    public async Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default)
    {
        return await _tenants.GetAllAsync(ct);
    }

    public async Task<Tenant> CreateTenantAsync(string name, string subdomain, CancellationToken ct = default)
    {
        var tenant = Tenant.Create(name, subdomain);
        await _tenants.AddAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);
        _logger.LogInformation("New tenant created: {Name} ({Subdomain})", name, subdomain);
        return tenant;
    }

    public async Task<string> InviteUserAsync(int tenantId, string email, string role, CancellationToken ct = default)
    {
        var invite = UserInvite.Create(tenantId, email, role);
        await _invites.AddAsync(invite, ct);
        await _invites.SaveChangesAsync(ct);
        
        // In a real app, send email here
        _logger.LogInformation("User invited: {Email} to tenant {TenantId}", email, tenantId);
        
        return invite.Token;
    }

    public async Task<IEnumerable<SystemSetting>> GetSystemSettingsAsync(CancellationToken ct = default)
    {
        return await _settings.GetAllAsync(ct);
    }

    public async Task UpdateSystemSettingAsync(string key, string value, CancellationToken ct = default)
    {
        var settings = await _settings.GetAllAsync(ct);
        var setting = settings.FirstOrDefault(s => s.Key == key);
        
        if (setting == null)
        {
            setting = SystemSetting.Create(key, value);
            await _settings.AddAsync(setting, ct);
        }
        else
        {
            setting.UpdateValue(value);
            await _settings.UpdateAsync(setting, ct);
        }
        
        await _settings.SaveChangesAsync(ct);
    }
}
