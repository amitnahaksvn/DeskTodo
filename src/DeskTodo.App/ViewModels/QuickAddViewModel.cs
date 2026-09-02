using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Phase 22's Quick Add — title, priority, category, deliberately **not** the full-field
/// editor's everything-else (due date, notes, checklist, subtasks, ...). The whole point of
/// a "fast to summon from the tray" window is that it stays fast; anything beyond a quick
/// capture belongs in the real editor, one "Edit" click away after the fact. Always creates
/// the task on today's date — Quick Add has no day-navigation of its own, unlike the widget.
/// </summary>
public sealed partial class QuickAddViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQuickAddParser _quickAddParser;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<QuickAddViewModel> _logger;

    public QuickAddViewModel(ITaskService taskService, ICategoryRepository categoryRepository, IQuickAddParser quickAddParser, TimeProvider timeProvider, ILogger<QuickAddViewModel> logger)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _quickAddParser = quickAddParser;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    public IReadOnlyList<TaskPriority> Priorities { get; } = Enum.GetValues<TaskPriority>();

    [ObservableProperty]
    public partial TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public ObservableCollection<CategoryOption> Categories { get; } = [CategoryOption.None];

    [ObservableProperty]
    public partial CategoryOption SelectedCategory { get; set; } = CategoryOption.None;

    /// <summary>Raised once the task is created (or the user cancels) — the window closing itself is the caller's job, same "ViewModel signals, View acts" split as <see cref="TaskEditViewModel.Saved"/>/<see cref="TaskEditViewModel.CancelRequested"/>.</summary>
    public event EventHandler? Closed;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Title = string.Empty;
        Priority = TaskPriority.Medium;

        try
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            Categories.Clear();
            Categories.Add(CategoryOption.None);
            foreach (var category in categories.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                Categories.Add(new CategoryOption(category.Id, category.Name, category.ColorHex));
            }

            SelectedCategory = CategoryOption.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories for Quick Add");
        }
    }

    /// <summary>
    /// Feature 41's Natural Language Quick Add — the last-parsed preview of <see cref="Title"/>,
    /// shown under the input so a "tomorrow 5pm !high" typed title doesn't silently vanish into
    /// due date/priority with no feedback. Recomputed on every keystroke (see
    /// <c>OnTitleChanged</c>) — parsing is pure, synchronous regex work, cheap enough to run
    /// live rather than debounced.
    /// </summary>
    [ObservableProperty]
    public partial string? ParsePreview { get; set; }

    partial void OnTitleChanged(string value)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var draft = _quickAddParser.Parse(value, today);
        var parts = new List<string>();
        if (draft.DueDate is { } due)
        {
            parts.Add(due.ToString("MMM d 'at' h:mm tt", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (draft.Priority is { } priority)
        {
            parts.Add($"{priority} priority");
        }

        if (draft.EstimatedMinutes is { } minutes)
        {
            parts.Add($"{minutes}m");
        }

        ParsePreview = parts.Count > 0 && !string.IsNullOrWhiteSpace(draft.Title) ? string.Join(" · ", parts) : null;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var title = Title.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        try
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
            var draft = _quickAddParser.Parse(title, today);
            var effectiveTitle = string.IsNullOrWhiteSpace(draft.Title) ? title : draft.Title;

            var task = await _taskService.CreateTaskAsync(
                today,
                effectiveTitle,
                priority: draft.Priority ?? Priority,
                categoryId: SelectedCategory.Id,
                dueDate: draft.DueDate);

            if (draft.EstimatedMinutes is { } minutes)
            {
                task.EstimatedMinutes = minutes;
                await _taskService.UpdateTaskAsync(task);
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task '{Title}' from Quick Add", title);
        }
    }

    [RelayCommand]
    private void Cancel() => Closed?.Invoke(this, EventArgs.Empty);
}
