using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskTodo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTemplatesAndRecurringSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TaskItems = table.Column<string>(type: "TEXT", nullable: false),
                    MilestoneItems = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurringProjectSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProjectTemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    GeneratedProjectNamePattern = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    NextOccurrenceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GeneratedProjectIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringProjectSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringProjectSchedules_ProjectTemplates_ProjectTemplateId",
                        column: x => x.ProjectTemplateId,
                        principalTable: "ProjectTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringProjectSchedules_ProjectTemplateId",
                table: "RecurringProjectSchedules",
                column: "ProjectTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringProjectSchedules");

            migrationBuilder.DropTable(
                name: "ProjectTemplates");
        }
    }
}
