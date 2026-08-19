namespace DeskTodo.App.ViewModels;

/// <summary>A <see cref="Domain.Entities.TaskGroup"/> as shown in <see cref="TaskGroupViewModel"/>'s list — <see cref="MemberSummary"/> is a display-only, comma-joined list of member template names, resolved once at load time rather than re-joined by XAML on every render.</summary>
public sealed record TaskGroupOption(Guid Id, string Name, string MemberSummary);
