using CommunityToolkit.Mvvm.ComponentModel;

namespace DeskTodo.App.ViewModels;

/// <summary>One event type's checkbox in the "New Webhook" form — same checkbox-row shape as <see cref="SelectableTemplateOption"/>/<see cref="RelationshipTypeFilterOption"/>.</summary>
public sealed partial class WebhookEventTypeOption(string eventType) : ObservableObject
{
    public string EventType { get; } = eventType;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
