using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digiprise.PMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTenantRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Tenants_TenantId1",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId1",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_ProjectId",
                table: "SlaPolicies",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_IssueId",
                table: "SlaBreaches",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreaches_PolicyId",
                table: "SlaBreaches",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEntries_BudgetId",
                table: "CostEntries",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEntries_CostTypeId",
                table: "CostEntries",
                column: "CostTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEntries_IssueId",
                table: "CostEntries",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEntries_UserId",
                table: "CostEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ProjectId",
                table: "Budgets",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Projects_ProjectId",
                table: "Budgets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CostEntries_Budgets_BudgetId",
                table: "CostEntries",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CostEntries_CostTypes_CostTypeId",
                table: "CostEntries",
                column: "CostTypeId",
                principalTable: "CostTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CostEntries_Issues_IssueId",
                table: "CostEntries",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CostEntries_Users_UserId",
                table: "CostEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlaBreaches_Issues_IssueId",
                table: "SlaBreaches",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlaBreaches_SlaPolicies_PolicyId",
                table: "SlaBreaches",
                column: "PolicyId",
                principalTable: "SlaPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlaPolicies_Projects_ProjectId",
                table: "SlaPolicies",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_Projects_ProjectId",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_CostEntries_Budgets_BudgetId",
                table: "CostEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CostEntries_CostTypes_CostTypeId",
                table: "CostEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CostEntries_Issues_IssueId",
                table: "CostEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CostEntries_Users_UserId",
                table: "CostEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SlaBreaches_Issues_IssueId",
                table: "SlaBreaches");

            migrationBuilder.DropForeignKey(
                name: "FK_SlaBreaches_SlaPolicies_PolicyId",
                table: "SlaBreaches");

            migrationBuilder.DropForeignKey(
                name: "FK_SlaPolicies_Projects_ProjectId",
                table: "SlaPolicies");

            migrationBuilder.DropIndex(
                name: "IX_SlaPolicies_ProjectId",
                table: "SlaPolicies");

            migrationBuilder.DropIndex(
                name: "IX_SlaBreaches_IssueId",
                table: "SlaBreaches");

            migrationBuilder.DropIndex(
                name: "IX_SlaBreaches_PolicyId",
                table: "SlaBreaches");

            migrationBuilder.DropIndex(
                name: "IX_CostEntries_BudgetId",
                table: "CostEntries");

            migrationBuilder.DropIndex(
                name: "IX_CostEntries_CostTypeId",
                table: "CostEntries");

            migrationBuilder.DropIndex(
                name: "IX_CostEntries_IssueId",
                table: "CostEntries");

            migrationBuilder.DropIndex(
                name: "IX_CostEntries_UserId",
                table: "CostEntries");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_ProjectId",
                table: "Budgets");

            migrationBuilder.AddColumn<int>(
                name: "TenantId1",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId1",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId1",
                table: "Users",
                column: "TenantId1");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId1",
                table: "Projects",
                column: "TenantId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Tenants_TenantId1",
                table: "Projects",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");
        }
    }
}
