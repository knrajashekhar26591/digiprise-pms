namespace Digiprise.PMS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Subdomain { get; private set; } = string.Empty;
    public string Plan { get; private set; } = "Standard";
    public bool IsActive { get; private set; } = true;
    public string? Settings { get; private set; } // JSON

    private readonly List<User> _users = new();
    private readonly List<Project> _projects = new();

    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    public IReadOnlyCollection<Project> Projects => _projects.AsReadOnly();

    protected Tenant() { }

    public static Tenant Create(string name, string subdomain, string plan = "Standard")
    {
        return new Tenant
        {
            Name = name,
            Subdomain = subdomain.ToLowerInvariant(),
            Plan = plan,
            IsActive = true
        };
    }

    public void UpdateSettings(string settingsJson)
    {
        Settings = settingsJson;
        Touch();
    }

    public void Deactivate() { IsActive = false; Touch(); }
}
