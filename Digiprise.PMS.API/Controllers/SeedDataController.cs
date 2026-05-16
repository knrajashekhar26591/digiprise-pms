using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Digiprise.PMS.Domain.Enums;

namespace Digiprise.PMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/dev")]
    public class SeedDataController : BaseController
    {
        private readonly PmsDbContext _db;

        public SeedDataController(PmsDbContext db)
        {
            _db = db;
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            var tenantId = CurrentTenantId;
            var userId = CurrentUserId;

            if (tenantId == 0) return BadRequest("No tenant active");

            var random = new Random();
            var issueTypes = new[] { IssueType.Story, IssueType.Bug, IssueType.Task };
            var priorities = new[] { Priority.Minor, Priority.Major, Priority.Critical };
            var statuses = new[] { 1, 2, 4 }; // To Do, In Progress, Done

            for (int i = 1; i <= 20; i++)
            {
                var suffix = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                var pName = $"Test Project {suffix}";
                var pKey = $"TP{suffix}";

                var project = Project.Create(tenantId, pKey, pName, userId);
                _db.Projects.Add(project);
                await _db.SaveChangesAsync();

                var sprint1 = Sprint.Create(tenantId, project.Id, project.Id, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14));
                var sprint2 = Sprint.Create(tenantId, project.Id, project.Id, "Sprint 2", DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(29));
                _db.Sprints.Add(sprint1);
                _db.Sprints.Add(sprint2);
                await _db.SaveChangesAsync();

                int numIssues = random.Next(20, 101);
                for (int j = 1; j <= numIssues; j++)
                {
                    var iType = issueTypes[random.Next(issueTypes.Length)];
                    var prio = priorities[random.Next(priorities.Length)];
                    var stat = statuses[random.Next(statuses.Length)];

                    var issue = Issue.Create(tenantId, project.Id, $"{pKey}-{j}", iType, $"Generated {iType} {j} for {pKey}", userId, stat, prio);

                    var sprintChoice = random.Next(0, 3);
                    if (sprintChoice == 1) issue.AssignToSprint(sprint1.Id, userId);
                    else if (sprintChoice == 2) issue.AssignToSprint(sprint2.Id, userId);

                    // Randomly assign story points
                    if (random.Next(0, 2) == 1) issue.SetStoryPoints(random.Next(1, 9), userId);

                    _db.Issues.Add(issue);
                }
                
                await _db.SaveChangesAsync();
            }

            return Ok(new { message = "Seeded 20 projects with 20-100 random issues each." });
        }
    }
}
