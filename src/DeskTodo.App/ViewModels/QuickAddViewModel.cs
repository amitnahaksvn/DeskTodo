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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<QuickAddViewModel> _logger;

    public QuickAddViewModel(ITaskService taskService, ICategoryRepository categoryRepository, TimeProvider timeProvider, ILogger<QuickAddViewModel> logger)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
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
            await _taskService.CreateTaskAsync(today, title, priority: Priority, categoryId: SelectedCategory.Id);
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
