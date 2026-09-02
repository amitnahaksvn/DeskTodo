using DeskTodo.Domain.Enums;

namespace DeskTodo.App.ViewModels;

/// <summary>A read-only row shown in the Export Profiles window's saved-profile list.</summary>
public sealed record ExportProfileRow(Guid Id, string Name, ExportFormat Format, string ProjectName, ExportDateRange DateRange);
