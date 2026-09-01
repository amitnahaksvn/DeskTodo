using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskTodo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskVersions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskVersions_TaskId",
                table: "TaskVersions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskVersions_TaskId_VersionNumber",
                table: "TaskVersions",
                columns: new[] { "TaskId", "VersionNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskVersions");
        }
    }
}
