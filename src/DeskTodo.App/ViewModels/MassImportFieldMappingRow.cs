using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskTodo.App.ViewModels;

/// <summary>One target <see cref="Application.DTOs.TaskExportRecord"/> field's mapping in the Mass Import Wizard — which CSV column (if any) supplies it. <see cref="SelectedHeader"/> is null for "(not mapped)".</summary>
public sealed partial class MassImportFieldMappingRow(string fieldName, ObservableCollection<string?> headerOptions) : ObservableObject
{
    public string FieldName { get; } = fieldName;

    public ObservableCollection<string?> HeaderOptions { get; } = headerOptions;

    [ObservableProperty]
    public partial string? SelectedHeader { get; set; }
}
