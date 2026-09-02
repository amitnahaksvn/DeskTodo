using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Inbox window (Feature 39, Roadmap-39-100.md) — a quick capture queue, separate
/// from the day-scoped task list: "write it down now, decide where it goes later."
/// </summary>
public sealed partial class InboxViewModel(IInboxService inboxService, TimeProvider timeProvider, ILogger<InboxViewModel> logger) : ViewModelBase
{
    public ObservableCollection<InboxItemOption> Items { get; } = [];

    [ObservableProperty]
    public partial string NewItemContent { get; set; } = string.Empty;

    /// <summary>Raised after a successful "Convert to Task" — <c>WidgetWindow</c> reloads its own task list in response, the same hand-off Trash's restore uses.</summary>
    public event EventHandler? ItemConverted;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await inboxService.GetUnprocessedAsync(cancellationToken);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new InboxItemOption(item.Id, item.Content, item.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt")));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load inbox items");
        }
    }

    [RelayCommand]
    private async Task CaptureAsync()
    {
        var content = NewItemContent.Trim();
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        try
        {
            await inboxService.CaptureAsync(content);
            NewItemContent = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to capture inbox item");
        }
    }

    [RelayCommand]
    private async Task ConvertToTaskAsync(Guid itemId)
    {
        try
        {
            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            await inboxService.ConvertToTaskAsync(itemId, today);
            await LoadAsync();
            ItemConverted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to convert inbox item {ItemId} to a task", itemId);
        }
    }

    [RelayCommand]
    private async Task ArchiveAsync(Guid itemId)
    {
        try
        {
            await inboxService.ArchiveAsync(itemId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to archive inbox item {ItemId}", itemId);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Guid itemId)
    {
        try
        {
            await inboxService.DeleteAsync(itemId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete inbox item {ItemId}", itemId);
        }
    }
}
