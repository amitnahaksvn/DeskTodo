using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskTodo.Infrastructure.Data.Configurations;

public sealed class ExportProfileConfiguration : IEntityTypeConfiguration<ExportProfile>
{
    public void Configure(EntityTypeBuilder<ExportProfile> builder)
    {
        builder.ToTable("ExportProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
    }
}
