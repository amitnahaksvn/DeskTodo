namespace DeskTodo.App.ViewModels;

/// <summary>A saved template, as offered in the widget's "New from template" picker.</summary>
public sealed record TemplateOption(Guid Id, string Name)
{
    public override string ToString() => Name;
}
