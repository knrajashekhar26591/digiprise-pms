using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Infrastructure.Data;

public static class EfDataSeeder
{
    public static async Task SeedAsync(PmsDbContext context, ILogger logger)
    {
        if (await context.Tenants.AnyAsync()) return; // Already seeded

        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Default workflow statuses
            var statuses = new (int wfId, string name, StatusCategory cat, int pos, string color)[]
            {
                (1, "To Do",       StatusCategory.ToDo,       0, "#6B7280"),
                (1, "In Progress", StatusCategory.InProgress,  1, "#3B82F6"),
                (1, "In Review",   StatusCategory.InProgress,  2, "#F59E0B"),
                (1, "Done",        StatusCategory.Done,        3, "#10B981"),
            };
            foreach (var (wfId, name, cat, pos, color) in statuses)
            {
                var ws = WorkflowStatus.Create(wfId, name, cat, pos, color);
                context.WorkflowStatuses.Add(ws);
            }

            // Demo tenant
            var tenant = Tenant.Create("Digiprise Demo", "demo");
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync(); // Get ID

            // Users
            var hasher = new Digiprise.PMS.Application.Services.PasswordHasher();
            var admin = User.Create(tenant.Id, "admin@demo.digiprise.io", "Admin User", hasher.Hash("Admin@123!"), "Admin");
            var dev = User.Create(tenant.Id, "dev@demo.digiprise.io", "Dev User", hasher.Hash("Dev@123!"), "Developer");
            
            context.Users.AddRange(admin, dev);
            await context.SaveChangesAsync();

            // Project
            var project = Project.Create(tenant.Id, "DEMO", "Demo Project", admin.Id, BoardType.Scrum);
            project.Update("Demo Project", "A sample Jira-like project to explore PMS features.", "🚀", "#3B82F6", "Software", DateTime.UtcNow, DateTime.UtcNow.AddMonths(3));
            context.Projects.Add(project);
            await context.SaveChangesAsync();

            var m1 = ProjectMember.Create(project.Id, admin.Id, "ProjectAdmin");
            var m2 = ProjectMember.Create(project.Id, dev.Id, "Developer");
            context.ProjectMembers.AddRange(m1, m2);

            // Sprint
            var sprint = Sprint.Create(project.Id, project.Id, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14), "Ship the MVP!");
            sprint.Start();
            context.Sprints.Add(sprint);
            await context.SaveChangesAsync();

            // Issues
            var epic = Issue.Create(tenant.Id, project.Id, "DEMO-1", IssueType.Epic, "User Authentication System", admin.Id, 1, Priority.Critical);
            
            var s1 = Issue.Create(tenant.Id, project.Id, "DEMO-2", IssueType.Story, "Implement JWT login endpoint", admin.Id, 2, Priority.Major);
            s1.Assign(dev.Id, admin.Id); s1.SetStoryPoints(5, admin.Id); s1.AssignToSprint(sprint.Id, admin.Id);
            
            var s2 = Issue.Create(tenant.Id, project.Id, "DEMO-3", IssueType.Story, "Build user registration flow", admin.Id, 1, Priority.Major);
            s2.SetStoryPoints(8, admin.Id); s2.AssignToSprint(sprint.Id, admin.Id);
            
            var b1 = Issue.Create(tenant.Id, project.Id, "DEMO-4", IssueType.Bug, "Fix token expiry not refreshing on activity", admin.Id, 2, Priority.Critical);
            b1.Assign(dev.Id, admin.Id); b1.SetStoryPoints(3, admin.Id); b1.AssignToSprint(sprint.Id, admin.Id);
            
            var t1 = Issue.Create(tenant.Id, project.Id, "DEMO-5", IssueType.Story, "Design sprint board UI wireframes", admin.Id, 1, Priority.Minor);
            t1.SetStoryPoints(2, admin.Id);

            context.Issues.AddRange(epic, s1, s2, b1, t1);
            await context.SaveChangesAsync();

            // Comment
            var c1 = Comment.Create(s1.Id, admin.Id, "<p>Assigned to @Dev - please pick this up in Sprint 1.</p>");
            context.Comments.Add(c1);

            // Notification
            var notif = Notification.Create(dev.Id, tenant.Id, "Issue Assigned: DEMO-2", "Implement JWT login endpoint", "Issue", s1.Id);
            context.Notifications.Add(notif);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("✅ Seeded: 1 tenant, 1 project, 5 issues, 1 sprint");
            logger.LogInformation("🔑 admin@demo.digiprise.io / Admin@123!");
            logger.LogInformation("🔑 dev@demo.digiprise.io   / Dev@123!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to seed database.");
        }
    }
}
