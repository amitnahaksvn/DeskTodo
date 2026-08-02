using System.Text.Json;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
    // Fixed GUIDs (not Guid.NewGuid()) so the seeded rows are stable across
    // migrations — EF Core's HasData diffs seed data by key on every model build,
    // the same reasoning CategoryConfiguration's seeded rows already use.
    public static readonly Guid MorningRoutineId = Guid.Parse("00000000-0000-0000-0002-000000000001");
    public static readonly Guid SprintPlanningPrepId = Guid.Parse("00000000-0000-0000-0002-000000000002");
    public static readonly Guid StudySessionId = Guid.Parse("00000000-0000-0000-0002-000000000003");
    public static readonly Guid WorkoutId = Guid.Parse("00000000-0000-0000-0002-000000000004");
    public static readonly Guid WeeklyGroceryRunId = Guid.Parse("00000000-0000-0000-0002-000000000005");
    public static readonly Guid PayMonthlyBillsId = Guid.Parse("00000000-0000-0000-0002-000000000006");
    public static readonly Guid FamilyGameNightId = Guid.Parse("00000000-0000-0000-0002-000000000007");

    public void Configure(EntityTypeBuilder<TaskTemplate> builder)
    {
        builder.ToTable("TaskTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.TaskTitle).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.Notes).HasMaxLength(4000);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // A manual JSON-string conversion (rather than relying on EF Core's newer built-in
        // primitive-collection-to-JSON-column mapping) since that support's exact behavior
        // on the SQLite provider version this project pins wasn't verified — this approach
        // is unambiguous and has worked in EF Core/SQLite for years.
        var checklistComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        builder.Property(t => t.ChecklistItems)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(checklistComparer);

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seven starter templates, one per seeded Category (CategoryConfiguration) — so
        // "New from template" isn't an empty dropdown on a brand-new install. Ordinary,
        // user-editable/deletable rows once seeded, not a distinct "built-in" concept the
        // way Category.IsBuiltIn is — a template is just a saved shape, there's no behavior
        // that needs to treat these differently from one a user saves themselves.
        builder.HasData(
            new TaskTemplate
            {
                Id = MorningRoutineId, Name = "Morning routine", TaskTitle = "Morning routine",
                CategoryId = CategoryConfiguration.PersonalId, Priority = TaskPriority.Medium,
                ChecklistItems = ["Meditate", "Journal", "Read for 15 minutes"], CreatedAt = seededAt,
            },
            new TaskTemplate
            {
                Id = SprintPlanningPrepId, Name = "Sprint planning prep", TaskTitle = "Prepare for sprint planning",
                CategoryId = CategoryConfiguration.OfficeId, Priority = TaskPriority.High,
                ChecklistItems = ["Review backlog", "Update ticket estimates", "Prepare demo"], CreatedAt = seededAt,
            },
            new TaskTemplate
            {
                Id = StudySessionId, Name = "Study session", TaskTitle = "Study session",
                CategoryId = CategoryConfiguration.LearningId, Priority = TaskPriority.Medium,
                ChecklistItems = ["Review notes", "Practice problems", "Summarize key points"], CreatedAt = seededAt,
            },
            new TaskTemplate
            {
                Id = WorkoutId, Name = "Workout", TaskTitle = "Workout session",
                CategoryId = CategoryConfiguration.FitnessId, Priority = TaskPriority.Medium,
                ChecklistItems = ["Warm up", "Main workout", "Cool down & stretch"], CreatedAt = seededAt,
            },
            new TaskTemplate
            {
                Id = WeeklyGroceryRunId, Name = "Weekly grocery run", TaskTitle = "Grocery shopping",
                CategoryId = CategoryConfiguration.ShoppingId, Priority = TaskPriority.Low,
                ChecklistItems = ["Milk", "Eggs", "Bread", "Fruits & vegetables"], CreatedAt = seededAt,
            },
            new TaskTemplate
            {
                Id = PayMonthlyBillsId, Name = "Pay monthly bills", TaskTitle = "Pay bills",
                CategoryId = CategoryConfiguration.FinanceId, Priority = TaskPriority.High,
                ChecklistItems = ["Rent", "Electricity", "Internet", "Phone"], CreatedAt = seededAt,
            },
            new TaskTemplate
            {
                Id = FamilyGameNightId, Name = "Family game night", TaskTitle = "Family game night",
                CategoryId = CategoryConfiguration.FamilyId, Priority = TaskPriority.Low,
                ChecklistItems = ["Pick a game", "Prepare snacks", "Remind everyone"], CreatedAt = seededAt,
            });
    }
}
