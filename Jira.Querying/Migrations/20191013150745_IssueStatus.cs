using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jira.Querying.Migrations
{
    public partial class IssueStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedIssueStatusChangeDb",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IssueKey = table.Column<string>(nullable: false),
                    ChangeTime = table.Column<DateTime>(nullable: false),
                    State = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedIssueStatusChangeDb", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CachedIssueStatusChangeDb_Issues_IssueKey",
                        column: x => x.IssueKey,
                        principalTable: "Issues",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedIssueStatusChangeDb_IssueKey",
                table: "CachedIssueStatusChangeDb",
                column: "IssueKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedIssueStatusChangeDb");
        }
    }
}
