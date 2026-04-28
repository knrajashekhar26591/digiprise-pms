using System.Collections.Concurrent;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Infrastructure.Data;

/// <summary>
/// Thread-safe in-memory data store.
/// Production: swap IRepository implementations with EF Core + SQL Server.
/// </summary>
public class InMemoryDataStore
{
    private int _nextId = 1;
    private readonly object _idLock = new();

    public ConcurrentDictionary<int, Tenant> Tenants { get; } = new();
    public ConcurrentDictionary<int, User> Users { get; } = new();
    public ConcurrentDictionary<int, Project> Projects { get; } = new();
    public ConcurrentDictionary<int, ProjectMember> ProjectMembers { get; } = new();
    public ConcurrentDictionary<int, Issue> Issues { get; } = new();
    public ConcurrentDictionary<int, Comment> Comments { get; } = new();
    public ConcurrentDictionary<int, Attachment> Attachments { get; } = new();
    public ConcurrentDictionary<int, IssueLink> IssueLinks { get; } = new();
    public ConcurrentDictionary<int, IssueHistory> IssueHistories { get; } = new();
    public ConcurrentDictionary<int, WorkflowStatus> WorkflowStatuses { get; } = new();
    public ConcurrentDictionary<int, Sprint> Sprints { get; } = new();
    public ConcurrentDictionary<int, SprintSnapshot> SprintSnapshots { get; } = new();
    public ConcurrentDictionary<int, Notification> Notifications { get; } = new();
    public ConcurrentDictionary<int, AuditLog> AuditLogs { get; } = new();
    public ConcurrentDictionary<int, AutomationRule> AutomationRules { get; } = new();

    // Refresh token store: token -> (userId, tenantId, expiry)
    public ConcurrentDictionary<string, (int UserId, int TenantId, DateTime Expiry)> RefreshTokens { get; } = new();

    public int NextId() { lock (_idLock) return _nextId++; }

    public static T SetId<T>(T entity, int id) where T : BaseEntity
    {
        typeof(BaseEntity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}

public static class DataSeeder
{
    public static void Seed(InMemoryDataStore store, ILogger logger)
    {
        // Default workflow statuses
        var statuses = new (int id, int wfId, string name, StatusCategory cat, int pos, string color)[]
        {
            (1, 1, "To Do",       StatusCategory.ToDo,       0, "#6B7280"),
            (2, 1, "In Progress", StatusCategory.InProgress,  1, "#3B82F6"),
            (3, 1, "In Review",   StatusCategory.InProgress,  2, "#F59E0B"),
            (4, 1, "Done",        StatusCategory.Done,        3, "#10B981"),
        };
        foreach (var (id, wfId, name, cat, pos, color) in statuses)
        {
            var ws = WorkflowStatus.Create(wfId, name, cat, pos, color);
            InMemoryDataStore.SetId(ws, id);
            store.WorkflowStatuses[id] = ws;
        }

        // Demo tenant
        var tenant = Tenant.Create("Digiprise Demo", "demo");
        InMemoryDataStore.SetId(tenant, store.NextId());
        store.Tenants[tenant.Id] = tenant;

        // Users
        var hasher = new Digiprise.PMS.Application.Services.PasswordHasher();
        var admin = User.Create(tenant.Id, "admin@demo.digiprise.io", "Admin User", hasher.Hash("Admin@123!"), "Admin");
        InMemoryDataStore.SetId(admin, store.NextId());
        store.Users[admin.Id] = admin;

        var dev = User.Create(tenant.Id, "dev@demo.digiprise.io", "Dev User", hasher.Hash("Dev@123!"), "Developer");
        InMemoryDataStore.SetId(dev, store.NextId());
        store.Users[dev.Id] = dev;

        // Project
        var project = Project.Create(tenant.Id, "DEMO", "Demo Project", admin.Id, BoardType.Scrum);
        project.Update("Demo Project", "A sample Jira-like project to explore PMS features.",
            "🚀", "#3B82F6", "Software", DateTime.UtcNow, DateTime.UtcNow.AddMonths(3));
        InMemoryDataStore.SetId(project, store.NextId());
        store.Projects[project.Id] = project;

        var m1 = ProjectMember.Create(project.Id, admin.Id, "ProjectAdmin");
        InMemoryDataStore.SetId(m1, store.NextId());
        store.ProjectMembers[m1.Id] = m1;

        var m2 = ProjectMember.Create(project.Id, dev.Id, "Developer");
        InMemoryDataStore.SetId(m2, store.NextId());
        store.ProjectMembers[m2.Id] = m2;

        // Sprint
        var sprint = Sprint.Create(project.Id, project.Id, "Sprint 1",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(14), "Ship the MVP!");
        InMemoryDataStore.SetId(sprint, store.NextId());
        sprint.Start();
        store.Sprints[sprint.Id] = sprint;

        // Issues
        void AddIssue(Issue i) { InMemoryDataStore.SetId(i, store.NextId()); store.Issues[i.Id] = i; }

        var epic = Issue.Create(tenant.Id, project.Id, "DEMO-1", IssueType.Epic,
            "User Authentication System", admin.Id, 1, Priority.Critical);
        AddIssue(epic);

        var s1 = Issue.Create(tenant.Id, project.Id, "DEMO-2", IssueType.Story,
            "Implement JWT login endpoint", admin.Id, 2, Priority.Major);
        s1.Assign(dev.Id, admin.Id); s1.SetStoryPoints(5, admin.Id); s1.AssignToSprint(sprint.Id, admin.Id);
        AddIssue(s1);

        var s2 = Issue.Create(tenant.Id, project.Id, "DEMO-3", IssueType.Story,
            "Build user registration flow", admin.Id, 1, Priority.Major);
        s2.SetStoryPoints(8, admin.Id); s2.AssignToSprint(sprint.Id, admin.Id);
        AddIssue(s2);

        var b1 = Issue.Create(tenant.Id, project.Id, "DEMO-4", IssueType.Bug,
            "Fix token expiry not refreshing on activity", admin.Id, 2, Priority.Critical);
        b1.Assign(dev.Id, admin.Id); b1.SetStoryPoints(3, admin.Id); b1.AssignToSprint(sprint.Id, admin.Id);
        AddIssue(b1);

        var t1 = Issue.Create(tenant.Id, project.Id, "DEMO-5", IssueType.Story,
            "Design sprint board UI wireframes", admin.Id, 1, Priority.Minor);
        t1.SetStoryPoints(2, admin.Id);
        AddIssue(t1);

        // Comment
        var c1 = Comment.Create(s1.Id, admin.Id, "<p>Assigned to @Dev - please pick this up in Sprint 1.</p>");
        InMemoryDataStore.SetId(c1, store.NextId());
        store.Comments[c1.Id] = c1;

        // Notification
        var notif = Notification.Create(dev.Id, tenant.Id, "Issue Assigned: DEMO-2",
            "Implement JWT login endpoint", "Issue", s1.Id);
        InMemoryDataStore.SetId(notif, store.NextId());
        store.Notifications[notif.Id] = notif;

        logger.LogInformation("✅ Seeded: {T} tenant, {P} project, {I} issues, {S} sprint",
            store.Tenants.Count, store.Projects.Count, store.Issues.Count, store.Sprints.Count);
        logger.LogInformation("🔑 admin@demo.digiprise.io / Admin@123!");
        logger.LogInformation("🔑 dev@demo.digiprise.io   / Dev@123!");
    }
}
