using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskTodo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceTaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetTaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskRelationships_Tasks_SourceTaskId",
                        column: x => x.SourceTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskRelationships_Tasks_TargetTaskId",
                        column: x => x.TargetTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskRelationships_SourceTaskId",
                table: "TaskRelationships",
                column: "SourceTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskRelationships_SourceTaskId_TargetTaskId_RelationshipType",
                table: "TaskRelationships",
                columns: new[] { "SourceTaskId", "TargetTaskId", "RelationshipType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskRelationships_TargetTaskId",
                table: "TaskRelationships",
                column: "TargetTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskRelationships");
        }
    }
}
