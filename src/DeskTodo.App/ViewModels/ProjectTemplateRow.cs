namespace DeskTodo.App.ViewModels;

/// <summary>A read-only row shown in the Project Templates window's template list.</summary>
public sealed record ProjectTemplateRow(Guid Id, string Name, string? Description, int TaskCount, int MilestoneCount);
