using CommunityToolkit.Mvvm.ComponentModel;
using DeskTodo.Domain.Enums;

namespace DeskTodo.App.ViewModels;

/// <summary>One relationship type's checkbox in <see cref="TaskGraphViewModel"/>'s filter row — this feature's own "Filter relationship types" requirement.</summary>
public sealed partial class RelationshipTypeFilterOption(TaskRelationshipType type) : ObservableObject
{
    public TaskRelationshipType Type { get; } = type;

    public string Label { get; } = type.ToString();

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;
}
