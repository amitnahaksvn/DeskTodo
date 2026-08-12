using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskTodo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskSnoozeAndNotificationSound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SnoozedUntil",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SnoozedUntil",
                table: "Tasks");
        }
    }
}
