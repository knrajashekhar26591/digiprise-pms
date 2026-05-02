using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Digiprise.PMS.Infrastructure.Data;

public class PmsDbContext : DbContext
{
    private readonly ICurrentUserContext _currentUserContext;

    public PmsDbContext(DbContextOptions<PmsDbContext> options, ICurrentUserContext currentUserContext) : base(options) 
    { 
        _currentUserContext = currentUserContext;
    }

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
    public DbSet<IssueJournal> IssueJournals { get; set; } = null!;
    
    // Phase 3 Modules
    public DbSet<Budget> Budgets { get; set; } = null!;
    public DbSet<CostType> CostTypes { get; set; } = null!;
    public DbSet<CostEntry> CostEntries { get; set; } = null!;
    public DbSet<SlaPolicy> SlaPolicies { get; set; } = null!;
    public DbSet<SlaBreach> SlaBreaches { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map relationships
        modelBuilder.Entity<Project>().HasOne(p => p.Tenant).WithMany(t => t.Projects).HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<User>().HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
        
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

        // Journal Index for Baseline API
        modelBuilder.Entity<IssueJournal>()
            .HasIndex(j => new { j.IssueId, j.CreatedAt })
            .IsDescending(false, true);

        // Phase 3 Module Mappings
        modelBuilder.Entity<Budget>().HasOne(b => b.Project).WithMany().HasForeignKey(b => b.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CostEntry>().HasOne(c => c.Issue).WithMany().HasForeignKey(c => c.IssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CostEntry>().HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CostEntry>().HasOne(c => c.CostType).WithMany().HasForeignKey(c => c.CostTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CostEntry>().HasOne(c => c.Budget).WithMany(b => b.CostEntries).HasForeignKey(c => c.BudgetId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SlaPolicy>().HasOne(s => s.Project).WithMany().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SlaBreach>().HasOne(s => s.Issue).WithMany().HasForeignKey(s => s.IssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SlaBreach>().HasOne(s => s.Policy).WithMany().HasForeignKey(s => s.PolicyId).OnDelete(DeleteBehavior.Cascade);

        // Global Query Filters for Tenant Isolation
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(ITenantScoped).IsAssignableFrom(e.ClrType)))
        {
            var method = typeof(PmsDbContext)
                .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.MakeGenericMethod(entity.ClrType);
            
            method?.Invoke(this, new object[] { modelBuilder });
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _currentUserContext.TenantId);
    }
}
