using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DeskTodo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultTaskTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TaskTemplates",
                columns: new[] { "Id", "CategoryId", "ChecklistItems", "CreatedAt", "Description", "EstimatedMinutes", "Name", "Notes", "Priority", "TaskTitle" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000001"), new Guid("00000000-0000-0000-0001-000000000001"), "[\"Meditate\",\"Journal\",\"Read for 15 minutes\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Morning routine", null, 1, "Morning routine" },
                    { new Guid("00000000-0000-0000-0002-000000000002"), new Guid("00000000-0000-0000-0001-000000000002"), "[\"Review backlog\",\"Update ticket estimates\",\"Prepare demo\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Sprint planning prep", null, 2, "Prepare for sprint planning" },
                    { new Guid("00000000-0000-0000-0002-000000000003"), new Guid("00000000-0000-0000-0001-000000000003"), "[\"Review notes\",\"Practice problems\",\"Summarize key points\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Study session", null, 1, "Study session" },
                    { new Guid("00000000-0000-0000-0002-000000000004"), new Guid("00000000-0000-0000-0001-000000000004"), "[\"Warm up\",\"Main workout\",\"Cool down \\u0026 stretch\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Workout", null, 1, "Workout session" },
                    { new Guid("00000000-0000-0000-0002-000000000005"), new Guid("00000000-0000-0000-0001-000000000005"), "[\"Milk\",\"Eggs\",\"Bread\",\"Fruits \\u0026 vegetables\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Weekly grocery run", null, 0, "Grocery shopping" },
                    { new Guid("00000000-0000-0000-0002-000000000006"), new Guid("00000000-0000-0000-0001-000000000006"), "[\"Rent\",\"Electricity\",\"Internet\",\"Phone\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Pay monthly bills", null, 2, "Pay bills" },
                    { new Guid("00000000-0000-0000-0002-000000000007"), new Guid("00000000-0000-0000-0001-000000000007"), "[\"Pick a game\",\"Prepare snacks\",\"Remind everyone\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Family game night", null, 0, "Family game night" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TaskTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000001"));

            migrationBuilder.DeleteData(
                table: "TaskTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000002"));

            migrationBuilder.DeleteData(
                table: "TaskTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000003"));

            migrationBuilder.DeleteData(
                table: "TaskTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000004"));

            migrationBuilder.DeleteData(
                table: "TaskTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000005"));

            migrationBuilder.DeleteData(
                table: "TaskTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000006"));

            migrationBuilder.DeleteData(
                table: "TaskTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000007"));
        }
    }
}
