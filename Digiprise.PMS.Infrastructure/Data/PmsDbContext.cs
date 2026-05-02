using Digiprise.PMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Digiprise.PMS.Infrastructure.Data;

public class PmsDbContext : DbContext
{
    public PmsDbContext(DbContextOptions<PmsDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<Issue> Issues { get; set; } = null!;
    public DbSet<Sprint> Sprints { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<Attachment> Attachments { get; set; } = null!;
    public DbSet<IssueLink> IssueLinks { get; set; } = null!;
    public DbSet<IssueHistory> IssueHistories { get; set; } = null!;
    public DbSet<IssueCustomField> IssueCustomFields { get; set; } = null!;
    public DbSet<WorkflowStatus> WorkflowStatuses { get; set; } = null!;
    public DbSet<SprintSnapshot> SprintSnapshots { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<AutomationRule> AutomationRules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map relationships
        modelBuilder.Entity<Tenant>().HasMany<Project>().WithOne(p => p.Tenant).HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Tenant>().HasMany<User>().WithOne(u => u.Tenant).HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Project>().HasMany(p => p.Issues).WithOne(i => i.Project).HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>().HasMany(p => p.Sprints).WithOne().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>().HasMany(p => p.Members).WithOne().HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>().HasOne(p => p.Lead).WithMany().HasForeignKey(p => p.LeadUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Issue>().HasMany(i => i.Comments).WithOne().HasForeignKey(c => c.IssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Issue>().HasMany(i => i.Attachments).WithOne().HasForeignKey(a => a.IssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Issue>().HasMany(i => i.IssueLinks).WithOne().HasForeignKey(il => il.SourceIssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Issue>().HasMany(i => i.History).WithOne().HasForeignKey(h => h.IssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Issue>().HasMany(i => i.CustomFields).WithOne().HasForeignKey(cf => cf.IssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Issue>().HasOne(i => i.Assignee).WithMany().HasForeignKey(i => i.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Issue>().HasOne(i => i.Reporter).WithMany().HasForeignKey(i => i.ReporterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Issue>().HasOne(i => i.Sprint).WithMany(s => s.Issues).HasForeignKey(i => i.SprintId).OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<Comment>().HasOne(c => c.Author).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProjectMember>().HasOne(pm => pm.User).WithMany().HasForeignKey(pm => pm.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
