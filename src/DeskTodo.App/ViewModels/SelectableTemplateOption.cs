using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskTodo.App.ViewModels;

/// <summary>A <see cref="TemplateOption"/> plus a bindable checked state — used by <see cref="TaskGroupViewModel"/>'s template picker, where <see cref="TemplateOption"/> itself (an immutable record, used elsewhere for plain "choose one" ComboBoxes) has no need for one.</summary>
public sealed partial class SelectableTemplateOption(Guid id, string name) : ObservableObject
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
