using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskTodo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MigrationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LogEntries = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationRuns", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MigrationRuns");
        }
    }
}
