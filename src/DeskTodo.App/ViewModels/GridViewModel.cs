using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Excel-style grid view (Phase 20): every non-archived task, across every
/// day, in one editable <c>DataGrid</c> — column resize/reorder/sort are the
/// <c>DataGrid</c> control's own built-in behavior (<c>CanUserResizeColumns</c>/
/// <c>CanUserReorderColumns</c>/<c>CanUserSortColumns</c> in <c>GridWindow.axaml</c>),
/// not anything this ViewModel drives. Same for column freezing — a fixed
/// <c>FrozenColumnCount</c> in XAML, not a user-facing toggle.
/// </summary>
/// <remarks>
/// Each row (<see cref="TaskGridRowViewModel"/>) persists its own edits immediately —
/// this class subscribes to every row's <c>PropertyChanged</c> and saves on any change
/// other than <see cref="TaskGridRowViewModel.IsSelected"/> (pure view state). The
/// subscription happens after each row is constructed (see <see cref="LoadAsync"/>),
/// not from inside the row's own constructor, so the constructor's initial property
/// assignments never trigger a redundant re-save of just-loaded state — the same
/// footgun <see cref="TaskItemViewModel"/>'s doc comments warn about.
/// </remarks>
public sealed partial class GridViewModel : ViewModelBase
{
    private readonly ITaskService _taskService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<GridViewModel> _logger;

    public GridViewModel(ITaskService taskService, ICategoryRepository categoryRepository, ISettingsService settingsService, ILogger<GridViewModel> logger)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _settingsService = settingsService;
        _logger = logger;
    }

    public ObservableCollection<TaskGridRowViewModel> Rows { get; } = [];

    public ObservableCollection<CategoryOption> Categories { get; } = [CategoryOption.None];

    public IReadOnlyList<TaskPriority> Priorities { get; } = Enum.GetValues<TaskPriority>();

    /// <summary>The columns a user can hide — Title/Date/Priority/Done stay mandatory, since hiding those would leave the grid meaningless.</summary>
    public IReadOnlyList<string> HideableColumnNames { get; } = ["Category", "Due", "Notes", "Status", "Progress"];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public int SelectedCount => Rows.Count(r => r.IsSelected);

    public bool HasSelection => SelectedCount > 0;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            Categories.Clear();
            Categories.Add(CategoryOption.None);
            foreach (var category in categories.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                Categories.Add(new CategoryOption(category.Id, category.Name, category.ColorHex));
            }

            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);

            foreach (var existing in Rows)
            {
                existing.PropertyChanged -= OnRowPropertyChanged;
            }

            Rows.Clear();
            foreach (var task in tasks.Where(t => !t.IsArchived).OrderBy(t => t.PlanDate).ThenBy(t => t.DayOrder))
            {
                var row = new TaskGridRowViewModel(task, Priorities, Categories);
                row.PropertyChanged += OnRowPropertyChanged;
                Rows.Add(row);
            }

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tasks for the grid view");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TaskGridRowViewModel row)
        {
            return;
        }

        if (e.PropertyName == nameof(TaskGridRowViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            return;
        }

        // Display-only computed properties (PlanDateDisplay/DueDateDisplay/StatusDisplay) also
        // raise PropertyChanged (via [NotifyPropertyChangedFor]) — nothing to persist for those.
        if (e.PropertyName is nameof(TaskGridRowViewModel.PlanDateDisplay) or nameof(TaskGridRowViewModel.DueDateDisplay) or nameof(TaskGridRowViewModel.StatusDisplay))
        {
            return;
        }

        _ = SaveRowAsync(row);
    }

    private async Task SaveRowAsync(TaskGridRowViewModel row)
    {
        // A blank title is a no-op rather than persisting an invalid task — matches
        // WidgetViewModel.AddTaskAsync's "blank title is a no-op" rule elsewhere.
        if (string.IsNullOrWhiteSpace(row.Title))
        {
            return;
        }

        try
        {
            var task = await _taskService.GetTaskAsync(row.Id);
            if (task is null)
            {
                return;
            }

            task.Title = row.Title.Trim();
            task.PlanDate = DateOnly.FromDateTime(row.PlanDate.DateTime);
            task.Priority = row.Priority;
            task.CategoryId = row.Category.Id;
            task.DueDate = row.DueDate?.DateTime;
            task.Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim();

            if (row.IsCompleted != task.IsCompleted)
            {
                if (row.IsCompleted)
                {
                    task.Complete();
                }
                else
                {
                    task.Reopen();
                }
            }

            await _taskService.UpdateTaskAsync(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save grid edit for task {TaskId}", row.Id);
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = Rows.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var row in selected)
            {
                await _taskService.DeleteTaskAsync(row.Id);
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk-delete {Count} selected grid rows", selected.Count);
        }
    }

    /// <summary>
    /// Builds a TSV block (a header row, then one row per data row) — Excel's own copy
    /// format is tab-separated, so pasting this directly into a real spreadsheet reproduces
    /// the columns. Copies the selection if there is one, otherwise every loaded row.
    /// </summary>
    public string BuildClipboardText()
    {
        var rows = Rows.Where(r => r.IsSelected).ToList();
        if (rows.Count == 0)
        {
            rows = Rows.ToList();
        }

        var builder = new StringBuilder();
        builder.AppendLine(string.Join('\t', "Title", "Date", "Priority", "Category", "Due", "Done", "Notes"));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join('\t',
                EscapeCell(row.Title),
                row.PlanDate.ToString("yyyy-MM-dd"),
                row.Priority.ToString(),
                EscapeCell(row.Category.Name),
                row.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                row.IsCompleted ? "Yes" : "No",
                EscapeCell(row.Notes)));
        }

        return builder.ToString();
    }

    /// <summary>Tab/newline can't survive a single TSV cell, so both are flattened to spaces on the way out — the same lossy tradeoff any TSV clipboard exchange makes.</summary>
    private static string EscapeCell(string value) => value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    /// <summary>
    /// Parses clipboard text as TSV (one task per line: Title, Date, Priority, Category,
    /// Due, Done, Notes — extra/missing trailing columns are tolerated) and creates a task
    /// per row. Accepts either this grid's own <see cref="BuildClipboardText"/> output
    /// (its header line is recognized and skipped) or a real range copied from Excel.
    /// </summary>
    public async Task PasteFromClipboardAsync(string clipboardText, CancellationToken cancellationToken = default)
    {
        var lines = clipboardText.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var pastedCount = 0;

        foreach (var line in lines)
        {
            var columns = line.Split('\t');
            var title = columns.Length > 0 ? columns[0].Trim() : string.Empty;
            if (string.IsNullOrEmpty(title) || string.Equals(title, "Title", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var planDate = columns.Length > 1 && DateOnly.TryParse(columns[1], out var parsedDate) ? parsedDate : today;
            var priority = columns.Length > 2 && Enum.TryParse<TaskPriority>(columns[2], ignoreCase: true, out var parsedPriority) ? parsedPriority : TaskPriority.Medium;
            var categoryId = columns.Length > 3
                ? Categories.FirstOrDefault(c => string.Equals(c.Name, columns[3].Trim(), StringComparison.OrdinalIgnoreCase))?.Id
                : null;
            DateTime? dueDate = columns.Length > 4 && DateTime.TryParse(columns[4], out var parsedDue) ? parsedDue : null;
            var isCompleted = columns.Length > 5 && string.Equals(columns[5].Trim(), "Yes", StringComparison.OrdinalIgnoreCase);
            var notes = columns.Length > 6 ? columns[6].Trim() : null;

            try
            {
                var task = await _taskService.CreateTaskAsync(planDate, title, priority: priority, categoryId: categoryId, dueDate: dueDate, cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(notes))
                {
                    task.Notes = notes;
                    await _taskService.UpdateTaskAsync(task, cancellationToken);
                }

                if (isCompleted)
                {
                    await _taskService.CompleteTaskAsync(task.Id, cancellationToken);
                }

                pastedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create a task from a pasted grid row: '{Line}'", line);
            }
        }

        if (pastedCount > 0)
        {
            await LoadAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlySet<string>> GetHiddenColumnsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);
        return settings.HiddenGridColumns.ToHashSet();
    }

    public async Task SetColumnHiddenAsync(string columnName, bool isHidden, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            var hidden = settings.HiddenGridColumns.ToHashSet();
            if (isHidden)
            {
                hidden.Add(columnName);
            }
            else
            {
                hidden.Remove(columnName);
            }

            settings.HiddenGridColumns = hidden.ToList();
            await _settingsService.SaveAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist grid column visibility for '{ColumnName}'", columnName);
        }
    }

    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
