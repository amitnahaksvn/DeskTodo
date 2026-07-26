namespace DeskTodo.Domain.Entities;

/// <summary>
/// A user- or system-defined grouping for tasks (e.g. Personal, Office,
/// Fitness), each with its own display color.
/// </summary>
public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Display color as a "#RRGGBB" hex string.</summary>
    public required string ColorHex { get; set; }

    /// <summary>
    /// True for the seeded default categories (Personal, Office, Learning,
    /// Fitness, Shopping, Finance, Family); false for user-created ones.
    /// Built-in categories can't be deleted, only renamed/recolored.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
