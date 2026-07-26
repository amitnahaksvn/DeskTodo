using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    // Fixed GUIDs (not Guid.NewGuid()) so the seeded rows are stable across
    // migrations — EF Core's HasData diffs seed data by key on every model build.
    public static readonly Guid PersonalId = Guid.Parse("00000000-0000-0000-0001-000000000001");
    public static readonly Guid OfficeId = Guid.Parse("00000000-0000-0000-0001-000000000002");
    public static readonly Guid LearningId = Guid.Parse("00000000-0000-0000-0001-000000000003");
    public static readonly Guid FitnessId = Guid.Parse("00000000-0000-0000-0001-000000000004");
    public static readonly Guid ShoppingId = Guid.Parse("00000000-0000-0000-0001-000000000005");
    public static readonly Guid FinanceId = Guid.Parse("00000000-0000-0000-0001-000000000006");
    public static readonly Guid FamilyId = Guid.Parse("00000000-0000-0000-0001-000000000007");

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ColorHex).IsRequired().HasMaxLength(9);
        builder.HasIndex(c => c.Name).IsUnique();

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Category { Id = PersonalId, Name = "Personal", ColorHex = "#6366F1", IsBuiltIn = true, CreatedAt = seededAt },
            new Category { Id = OfficeId, Name = "Office", ColorHex = "#0EA5E9", IsBuiltIn = true, CreatedAt = seededAt },
            new Category { Id = LearningId, Name = "Learning", ColorHex = "#8B5CF6", IsBuiltIn = true, CreatedAt = seededAt },
            new Category { Id = FitnessId, Name = "Fitness", ColorHex = "#22C55E", IsBuiltIn = true, CreatedAt = seededAt },
            new Category { Id = ShoppingId, Name = "Shopping", ColorHex = "#F59E0B", IsBuiltIn = true, CreatedAt = seededAt },
            new Category { Id = FinanceId, Name = "Finance", ColorHex = "#10B981", IsBuiltIn = true, CreatedAt = seededAt },
            new Category { Id = FamilyId, Name = "Family", ColorHex = "#EC4899", IsBuiltIn = true, CreatedAt = seededAt });
    }
}
