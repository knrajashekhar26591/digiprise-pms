using Digiprise.PMS.Domain.Interfaces;

namespace Digiprise.PMS.Domain.Entities;

public class User : BaseEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public string SystemRole { get; private set; } = "User"; // Admin, User

    public Tenant? Tenant { get; private set; }

    protected User() { }

    public static User Create(int tenantId, string email, string displayName, string passwordHash, string role = "User")
    {
        return new User
        {
            TenantId = tenantId,
            Email = email.ToLowerInvariant(),
            DisplayName = displayName,
            PasswordHash = passwordHash,
            SystemRole = role,
            IsActive = true
        };
    }

    public void RecordLogin() { LastLoginAt = DateTime.UtcNow; Touch(); }
    public void UpdateProfile(string displayName, string? avatarUrl) { DisplayName = displayName; AvatarUrl = avatarUrl; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
    public void SetPasswordHash(string hash) { PasswordHash = hash; Touch(); }
}
