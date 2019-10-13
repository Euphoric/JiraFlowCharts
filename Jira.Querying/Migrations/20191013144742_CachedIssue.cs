using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jira.Querying.Migrations
{
    public partial class CachedIssue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Key = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    Resolution = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: true),
                    Updated = table.Column<DateTime>(nullable: true),
                    Resolved = table.Column<DateTime>(nullable: true),
                    OriginalEstimate = table.Column<int>(nullable: true),
                    TimeSpent = table.Column<int>(nullable: true),
                    StoryPoints = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Key);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Issues");
        }
    }
}
