using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class GridViewModelTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IProjectService> _projectService = new();
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
    private readonly GridViewModel _sut;

    public GridViewModelTests()
    {
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        _sut = new GridViewModel(_taskService.Object, _categoryRepository.Object, _projectService.Object, _settingsService.Object, _timeProvider, NullLogger<GridViewModel>.Instance);
    }

    private static TaskItem CreateTask(
        DateOnly planDate,
        int order,
        string title,
        TaskPriority priority = TaskPriority.Medium,
        Guid? categoryId = null,
        Guid? projectId = null,
        DateTime? dueDate = null,
        bool isFavorite = false,
        bool isPinned = false)
    {
        var task = new TaskItem
        {
            PlanDate = planDate,
            DayOrder = order,
            Title = title,
            Priority = priority,
            CategoryId = categoryId,
            ProjectId = projectId,
            DueDate = dueDate,
        };

        // IsFavorite/IsPinned have private setters (only settable via MarkFavorite()/Pin()) —
        // a deliberate domain invariant, so tests go through the same public API.
        if (isFavorite)
        {
            task.MarkFavorite();
        }

        if (isPinned)
        {
            task.Pin();
        }

        return task;
    }

    [Fact]
    public async Task LoadAsync_PopulatesRowsFromAllNonArchivedTasks_OrderedByDate()
    {
        var archived = CreateTask(new DateOnly(2026, 7, 1), 0, "Archived");
        archived.Archive();
        var later = CreateTask(new DateOnly(2026, 7, 28), 0, "Later");
        var earlier = CreateTask(new DateOnly(2026, 7, 27), 0, "Earlier");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([archived, later, earlier]);

        await _sut.LoadAsync();

        Assert.Equal(["Earlier", "Later"], _sut.Rows.Select(r => r.Title));
    }

    [Fact]
    public async Task LoadAsync_NeverPersistsTheJustLoadedRows()
    {
        // Regression test mirroring TaskItemViewModelTests' constructor-persistence
        // footgun check: GridViewModel subscribes to each row's PropertyChanged only
        // after construction, so LoadAsync's own initial property assignments must
        // never trigger a save.
        var task = CreateTask(new DateOnly(2026, 7, 27), 0, "Water plants");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await _sut.LoadAsync();

        _taskService.Verify(s => s.UpdateTaskAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EditingARowsTitle_PersistsTheChange()
    {
        var task = CreateTask(new DateOnly(2026, 7, 27), 0, "Original");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync();
        var row = _sut.Rows[0];

        row.Title = "Updated";

        _taskService.Verify(s => s.UpdateTaskAsync(It.Is<TaskItem>(t => t.Title == "Updated"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditingARowsTitleToBlank_DoesNotPersist()
    {
        var task = CreateTask(new DateOnly(2026, 7, 27), 0, "Original");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync();
        var row = _sut.Rows[0];

        row.Title = "   ";

        _taskService.Verify(s => s.UpdateTaskAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TogglingARowsCompleted_CallsCompleteOrReopen()
    {
        var task = CreateTask(new DateOnly(2026, 7, 27), 0, "Task");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        _taskService.Setup(s => s.GetTaskAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        await _sut.LoadAsync();
        var row = _sut.Rows[0];

        row.IsCompleted = true;

        Assert.True(task.IsCompleted);
        _taskService.Verify(s => s.UpdateTaskAsync(It.Is<TaskItem>(t => t.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TogglingARowsIsSelected_DoesNotPersist_ButUpdatesSelectedCount()
    {
        var task = CreateTask(new DateOnly(2026, 7, 27), 0, "Task");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        await _sut.LoadAsync();
        var row = _sut.Rows[0];

        row.IsSelected = true;

        Assert.Equal(1, _sut.SelectedCount);
        Assert.True(_sut.HasSelection);
        _taskService.Verify(s => s.UpdateTaskAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskService.Verify(s => s.GetTaskAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSelectedAsync_DeletesEverySelectedRow_AndReloads()
    {
        var keep = CreateTask(new DateOnly(2026, 7, 27), 0, "Keep");
        var remove = CreateTask(new DateOnly(2026, 7, 27), 1, "Remove");
        _taskService.SetupSequence(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([keep, remove])
            .ReturnsAsync([keep]);
        await _sut.LoadAsync();
        _sut.Rows.Single(r => r.Title == "Remove").IsSelected = true;

        await _sut.DeleteSelectedCommand.ExecuteAsync(null);

        _taskService.Verify(s => s.DeleteTaskAsync(remove.Id, It.IsAny<CancellationToken>()), Times.Once);
        _taskService.Verify(s => s.DeleteTaskAsync(keep.Id, It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(_sut.Rows);
        Assert.Equal("Keep", _sut.Rows[0].Title);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested()
    {
        var raised = false;
        _sut.CloseRequested += (_, _) => raised = true;

        _sut.CloseCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public async Task BuildClipboardText_WithNoSelection_IncludesEveryRow()
    {
        var first = CreateTask(new DateOnly(2026, 7, 27), 0, "First");
        var second = CreateTask(new DateOnly(2026, 7, 28), 0, "Second");
        second.Notes = "Some notes";
        second.Complete();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);
        await _sut.LoadAsync();

        var text = _sut.BuildClipboardText();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length); // header + 2 rows
        Assert.StartsWith("Title\tDate\tPriority\tCategory\tDue\tDone\tNotes", lines[0]);
        Assert.Contains("First", lines[1]);
        Assert.Contains("Second", lines[2]);
        Assert.Contains("Some notes", lines[2]);
        Assert.Contains("Yes", lines[2]); // Done column
    }

    [Fact]
    public async Task BuildClipboardText_WithASelection_IncludesOnlySelectedRows()
    {
        var first = CreateTask(new DateOnly(2026, 7, 27), 0, "First");
        var second = CreateTask(new DateOnly(2026, 7, 28), 0, "Second");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);
        await _sut.LoadAsync();
        _sut.Rows.Single(r => r.Title == "Second").IsSelected = true;

        var text = _sut.BuildClipboardText();

        Assert.DoesNotContain("First", text);
        Assert.Contains("Second", text);
    }

    [Fact]
    public async Task PasteFromClipboardAsync_CreatesATaskPerDataRow_SkippingTheHeaderAndBlankLines()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync();
        var created = CreateTask(new DateOnly(2026, 8, 1), 0, "Pasted task");
        _taskService.Setup(s => s.CreateTaskAsync(
                new DateOnly(2026, 8, 1), "Pasted task", null, TaskPriority.High, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var clipboardText = "Title\tDate\tPriority\tCategory\tDue\tDone\tNotes\n" +
                             "Pasted task\t2026-08-01\tHigh\t\t\tNo\t\n" +
                             "\n";

        await _sut.PasteFromClipboardAsync(clipboardText);

        _taskService.Verify(s => s.CreateTaskAsync(
            new DateOnly(2026, 8, 1), "Pasted task", null, TaskPriority.High, null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PasteFromClipboardAsync_MarksARowCompleteWhenDoneColumnSaysYes()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync();
        var created = CreateTask(new DateOnly(2026, 8, 1), 0, "Finished task");
        _taskService.Setup(s => s.CreateTaskAsync(It.IsAny<DateOnly>(), "Finished task", It.IsAny<string?>(), It.IsAny<TaskPriority>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        await _sut.PasteFromClipboardAsync("Finished task\t2026-08-01\tMedium\t\t\tYes\t");

        _taskService.Verify(s => s.CompleteTaskAsync(created.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHiddenColumnsAsync_ReflectsPersistedSettings()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { HiddenGridColumns = ["Notes", "Due"] });

        var hidden = await _sut.GetHiddenColumnsAsync();

        Assert.Equal(["Due", "Notes"], hidden.OrderBy(n => n));
    }

    [Fact]
    public async Task SetColumnHiddenAsync_AddsAndRemovesFromThePersistedList()
    {
        var settings = new AppSettings();
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.SetColumnHiddenAsync("Notes", isHidden: true);

        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.HiddenGridColumns.Contains("Notes")), It.IsAny<CancellationToken>()), Times.Once);

        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { HiddenGridColumns = ["Notes"] });
        await _sut.SetColumnHiddenAsync("Notes", isHidden: false);

        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.HiddenGridColumns.Contains("Notes")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetColumnsFrozenAsync_ReflectsPersistedSettings()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { GridColumnsFrozen = false });

        Assert.False(await _sut.GetColumnsFrozenAsync());
    }

    [Fact]
    public async Task GetColumnsFrozenAsync_DefaultsToTrue()
    {
        Assert.True(await _sut.GetColumnsFrozenAsync());
    }

    [Fact]
    public async Task SetColumnsFrozenAsync_PersistsTheValue()
    {
        var settings = new AppSettings();
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.SetColumnsFrozenAsync(false);

        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.GridColumnsFrozen), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSavedViewsAsync_ReflectsPersistedSettings()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { GridSavedViews = [new GridSavedView { Name = "Compact", HiddenColumns = ["Notes"] }] });

        var views = await _sut.GetSavedViewsAsync();

        Assert.Single(views);
        Assert.Equal("Compact", views[0].Name);
    }

    [Fact]
    public async Task SaveCurrentViewAsync_SavesTheCurrentHiddenColumnsUnderTheGivenName()
    {
        var settings = new AppSettings { HiddenGridColumns = ["Notes", "Due"] };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.SaveCurrentViewAsync("Compact");

        _settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.GridSavedViews.Any(v => v.Name == "Compact" && v.HiddenColumns.Contains("Notes") && v.HiddenColumns.Contains("Due"))),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveCurrentViewAsync_WithABlankName_DoesNotSave()
    {
        await _sut.SaveCurrentViewAsync("   ");

        _settingsService.Verify(s => s.SaveAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCurrentViewAsync_OverwritesAnExistingViewWithTheSameName_CaseInsensitively()
    {
        var settings = new AppSettings
        {
            HiddenGridColumns = ["Due"],
            GridSavedViews = [new GridSavedView { Name = "compact", HiddenColumns = ["Notes"] }],
        };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.SaveCurrentViewAsync("Compact");

        _settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.GridSavedViews.Count == 1 && a.GridSavedViews[0].HiddenColumns.Contains("Due")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSavedViewAsync_RemovesTheNamedView()
    {
        var settings = new AppSettings { GridSavedViews = [new GridSavedView { Name = "Compact", HiddenColumns = [] }] };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.DeleteSavedViewAsync("Compact");

        _settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.GridSavedViews.Count == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyViewAsync_CopiesTheViewsHiddenColumnsIntoTheCurrentLayout()
    {
        var settings = new AppSettings
        {
            HiddenGridColumns = ["Due"],
            GridSavedViews = [new GridSavedView { Name = "Compact", HiddenColumns = ["Notes", "Category"] }],
        };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.ApplyViewAsync("Compact");

        _settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.HiddenGridColumns.Contains("Notes") && a.HiddenGridColumns.Contains("Category") && !a.HiddenGridColumns.Contains("Due")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyViewAsync_WithAnUnknownName_DoesNotSave()
    {
        await _sut.ApplyViewAsync("Nonexistent");

        _settingsService.Verify(s => s.SaveAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshVisibleRows_WithSearchText_FiltersByTitleOrNotes()
    {
        var matching = CreateTask(new DateOnly(2026, 7, 27), 0, "Water plants");
        var other = CreateTask(new DateOnly(2026, 7, 27), 1, "Read book");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([matching, other]);
        await _sut.LoadAsync();

        _sut.SearchText = "water";

        Assert.Single(_sut.VisibleRows);
        Assert.Equal("Water plants", _sut.VisibleRows[0].Title);
        Assert.Equal(2, _sut.Rows.Count); // Rows itself stays unfiltered
    }

    [Fact]
    public async Task RefreshVisibleRows_WithProjectFilter_ShowsOnlyTasksInThatProject()
    {
        var projectId = Guid.NewGuid();
        var inProject = CreateTask(new DateOnly(2026, 7, 27), 0, "Filed", projectId: projectId);
        var unassigned = CreateTask(new DateOnly(2026, 7, 27), 1, "Unfiled");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([inProject, unassigned]);
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Project { Id = projectId, Name = "Website Redesign", ColorHex = "#6366F1" }]);
        await _sut.LoadAsync();

        var projectOption = Assert.Single(_sut.ProjectFilterOptions, p => p.Id == projectId);
        _sut.SelectedProjectFilter = projectOption;

        Assert.Single(_sut.VisibleRows);
        Assert.Equal("Filed", _sut.VisibleRows[0].Title);
    }

    [Theory]
    [InlineData(GridSmartFilter.Favorites)]
    [InlineData(GridSmartFilter.Pinned)]
    public async Task RefreshVisibleRows_WithFavoritesOrPinnedSmartFilter_ShowsOnlyMatchingTasks(GridSmartFilter smartFilter)
    {
        var flagged = CreateTask(new DateOnly(2026, 7, 27), 0, "Flagged", isFavorite: smartFilter == GridSmartFilter.Favorites, isPinned: smartFilter == GridSmartFilter.Pinned);
        var plain = CreateTask(new DateOnly(2026, 7, 27), 1, "Plain");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([flagged, plain]);
        await _sut.LoadAsync();

        _sut.SelectedSmartFilter = smartFilter;

        Assert.Single(_sut.VisibleRows);
        Assert.Equal("Flagged", _sut.VisibleRows[0].Title);
    }

    [Fact]
    public async Task RefreshVisibleRows_WithOverdueSmartFilter_ExcludesCompletedAndFutureTasks()
    {
        var overdue = CreateTask(new DateOnly(2026, 8, 1), 0, "Overdue", dueDate: new DateTime(2026, 8, 10));
        var completedButLate = CreateTask(new DateOnly(2026, 8, 1), 1, "Done late", dueDate: new DateTime(2026, 8, 10));
        completedButLate.Complete();
        var future = CreateTask(new DateOnly(2026, 8, 1), 2, "Future", dueDate: new DateTime(2026, 8, 20));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([overdue, completedButLate, future]);
        await _sut.LoadAsync();

        _sut.SelectedSmartFilter = GridSmartFilter.Overdue;

        Assert.Single(_sut.VisibleRows);
        Assert.Equal("Overdue", _sut.VisibleRows[0].Title);
    }

    [Fact]
    public async Task RefreshVisibleRows_WithDueTodaySmartFilter_ShowsOnlyTasksDueToday()
    {
        var dueToday = CreateTask(new DateOnly(2026, 8, 1), 0, "Due today", dueDate: new DateTime(2026, 8, 12, 9, 0, 0));
        var dueTomorrow = CreateTask(new DateOnly(2026, 8, 1), 1, "Due tomorrow", dueDate: new DateTime(2026, 8, 13));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([dueToday, dueTomorrow]);
        await _sut.LoadAsync();

        _sut.SelectedSmartFilter = GridSmartFilter.DueToday;

        Assert.Single(_sut.VisibleRows);
        Assert.Equal("Due today", _sut.VisibleRows[0].Title);
    }

    [Fact]
    public async Task RefreshVisibleRows_WithHighPrioritySmartFilter_IncludesHighAndCritical()
    {
        var high = CreateTask(new DateOnly(2026, 7, 27), 0, "High", priority: TaskPriority.High);
        var critical = CreateTask(new DateOnly(2026, 7, 27), 1, "Critical", priority: TaskPriority.Critical);
        var medium = CreateTask(new DateOnly(2026, 7, 27), 2, "Medium", priority: TaskPriority.Medium);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([high, critical, medium]);
        await _sut.LoadAsync();

        _sut.SelectedSmartFilter = GridSmartFilter.HighPriority;

        Assert.Equal(["High", "Critical"], _sut.VisibleRows.Select(r => r.Title));
    }

    [Fact]
    public async Task RefreshVisibleRows_WithNoProjectSmartFilter_ShowsOnlyUnassignedTasks()
    {
        var assigned = CreateTask(new DateOnly(2026, 7, 27), 0, "Assigned", projectId: Guid.NewGuid());
        var unassigned = CreateTask(new DateOnly(2026, 7, 27), 1, "Unassigned");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([assigned, unassigned]);
        await _sut.LoadAsync();

        _sut.SelectedSmartFilter = GridSmartFilter.NoProject;

        Assert.Single(_sut.VisibleRows);
        Assert.Equal("Unassigned", _sut.VisibleRows[0].Title);
    }

    [Fact]
    public async Task SaveCurrentViewAsync_SavesTheCurrentFilterState_AsPartOfTheNamedPreset()
    {
        var categoryId = Guid.NewGuid();
        var settings = new AppSettings();
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        _sut.SearchText = "urgent";
        _sut.SelectedCategoryFilter = new CategoryFilterOption(categoryId, "Work");
        _sut.SelectedStatusFilter = TaskStatusFilter.Active;
        _sut.SelectedSmartFilter = GridSmartFilter.Favorites;

        await _sut.SaveCurrentViewAsync("My Search");

        _settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.GridSavedViews.Any(v =>
                v.Name == "My Search" &&
                v.SearchText == "urgent" &&
                v.CategoryId == categoryId &&
                v.StatusFilter == nameof(TaskStatusFilter.Active) &&
                v.SmartFilter == nameof(GridSmartFilter.Favorites))),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyViewAsync_RestoresTheSavedFilterState()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Work", ColorHex = "#3B82F6" };
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([category]);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync(); // Populates CategoryFilterOptions so ApplyViewAsync can resolve the saved CategoryId.

        var settings = new AppSettings
        {
            GridSavedViews =
            [
                new GridSavedView
                {
                    Name = "My Search",
                    SearchText = "urgent",
                    CategoryId = categoryId,
                    StatusFilter = nameof(TaskStatusFilter.Completed),
                    SmartFilter = nameof(GridSmartFilter.Pinned),
                },
            ],
        };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.ApplyViewAsync("My Search");

        Assert.Equal("urgent", _sut.SearchText);
        Assert.Equal(categoryId, _sut.SelectedCategoryFilter.Id);
        Assert.Equal(TaskStatusFilter.Completed, _sut.SelectedStatusFilter);
        Assert.Equal(GridSmartFilter.Pinned, _sut.SelectedSmartFilter);
    }

    [Fact]
    public async Task ApplyViewAsync_WithNoSavedFilterState_ResetsFiltersToDefaults()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.LoadAsync();
        _sut.SearchText = "stale filter";
        _sut.SelectedSmartFilter = GridSmartFilter.Favorites;

        var settings = new AppSettings { GridSavedViews = [new GridSavedView { Name = "Compact", HiddenColumns = ["Notes"] }] };
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        await _sut.ApplyViewAsync("Compact");

        Assert.Equal(string.Empty, _sut.SearchText);
        Assert.Equal(GridSmartFilter.None, _sut.SelectedSmartFilter);
    }

    // Regression coverage for a real crash: SelectedCategoryFilter/SelectedProjectFilter are
    // declared non-nullable, but a bound ComboBox's SelectedItem can transiently go null when
    // its ItemsSource (CategoryFilterOptions/ProjectFilterOptions) is cleared and repopulated
    // — which LoadAsync does on every call. That transient null used to reach
    // RefreshVisibleRows() unguarded and throw a NullReferenceException.
    [Fact]
    public void SettingStatusFilter_WithSelectedCategoryAndProjectFilterNull_DoesNotThrow()
    {
        _sut.SelectedCategoryFilter = null!;
        _sut.SelectedProjectFilter = null!;

        var exception = Record.Exception(() => _sut.SelectedStatusFilter = TaskStatusFilter.Active);

        Assert.Null(exception);
        Assert.Empty(_sut.VisibleRows);
    }

    [Fact]
    public async Task SaveCurrentViewAsync_WithSelectedCategoryAndProjectFilterNull_DoesNotThrow_AndSavesNullIds()
    {
        var settings = new AppSettings();
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        _sut.SelectedCategoryFilter = null!;
        _sut.SelectedProjectFilter = null!;

        var exception = await Record.ExceptionAsync(() => _sut.SaveCurrentViewAsync("My Search"));

        Assert.Null(exception);
        _settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.GridSavedViews.Any(v => v.Name == "My Search" && v.CategoryId == null && v.ProjectId == null)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
