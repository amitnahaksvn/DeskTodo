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
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly GridViewModel _sut;

    public GridViewModelTests()
    {
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        _sut = new GridViewModel(_taskService.Object, _categoryRepository.Object, _settingsService.Object, NullLogger<GridViewModel>.Instance);
    }

    private static TaskItem CreateTask(DateOnly planDate, int order, string title) => new()
    {
        PlanDate = planDate,
        DayOrder = order,
        Title = title,
    };

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
}
