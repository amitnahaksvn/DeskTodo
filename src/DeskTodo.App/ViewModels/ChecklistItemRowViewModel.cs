using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Display/interaction wrapper around a single <see cref="ChecklistItem"/>
/// inside the full-field task editor. Mirrors <see cref="TaskItemViewModel"/>'s
/// shape (owns its own persistence, calls back to the owner to remove itself
/// from the visible list) at a much smaller scale — a checklist row has only
/// check/remove, no rename-in-place, priority, or category.
/// </summary>
public sealed partial class ChecklistItemRowViewModel : ObservableObject
{
    private readonly IChecklistService _checklistService;
    private readonly ILogger _logger;
    private readonly Action<ChecklistItemRowViewModel> _requestRemove;

    public ChecklistItemRowViewModel(ChecklistItem item, IChecklistService checklistService, ILogger logger, Action<ChecklistItemRowViewModel> requestRemove)
    {
        _checklistService = checklistService;
        _logger = logger;
        _requestRemove = requestRemove;

        Id = item.Id;
        Text = item.Text;
        IsChecked = item.IsChecked;
    }

    public Guid Id { get; }

    public string Text { get; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    /// <summary>Bound to the row's CheckBox <c>Command</c> (not <c>IsChecked</c> two-way) so only a genuine user click persists anything — same reasoning as <see cref="TaskItemViewModel.ToggleCompleteCommand"/>.</summary>
    [RelayCommand]
    private async Task ToggleAsync()
    {
        var newValue = !IsChecked;
        try
        {
            await _checklistService.ToggleItemAsync(Id);
            IsChecked = newValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle checklist item {ItemId}", Id);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        try
        {
            await _checklistService.RemoveItemAsync(Id);
            _requestRemove(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove checklist item {ItemId}", Id);
        }
    }
}
