namespace DeskTodo.App.ViewModels;

/// <summary>A lightweight (Id, Name) pair for binding a <c>ComboBox</c> to an existing <see cref="Domain.Entities.ProjectTemplate"/> without loading its full task/milestone item lists.</summary>
public sealed record ProjectTemplateOption(Guid Id, string Name)
{
    public override string ToString() => Name;
}
