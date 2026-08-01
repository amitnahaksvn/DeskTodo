using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A tag assigned to the task being edited, as shown/removed in the
/// editor's tag chip list. Owns its own removal (mirrors
/// <see cref="ChecklistItemRowViewModel"/>) rather than the parent
/// exposing a shared "RemoveTag(TagChip)" command bound via an ambient
/// XAML parent-DataContext lookup — a plain per-row command is simpler
/// and more robust than that binding path.
/// </summary>
public sealed partial class TagChip : ObservableObject
{
    private readonly Guid _taskId;
    private readonly ITagService _tagService;
    private readonly ILogger _logger;
    private readonly Action<TagChip> _requestRemove;

    public TagChip(Guid id, string name, Guid taskId, ITagService tagService, ILogger logger, Action<TagChip> requestRemove)
    {
        Id = id;
        Name = name;
        _taskId = taskId;
        _tagService = tagService;
        _logger = logger;
        _requestRemove = requestRemove;
    }

    public Guid Id { get; }

    public string Name { get; }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        try
        {
            await _tagService.RemoveTagAsync(_taskId, Id);
            _requestRemove(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove tag {TagId} from task {TaskId}", Id, _taskId);
        }
    }
}
