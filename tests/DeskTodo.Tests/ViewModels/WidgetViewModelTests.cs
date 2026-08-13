using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class WidgetViewModelTests
{
    private static TaskItem CreateTask(
        DateOnly planDate,
        int order,
        string title,
        TaskPriority priority = TaskPriority.Medium,
        Guid? categoryId = null,
        Guid? projectId = null,
        DateTime? dueDate = null,
        string? notes = null,
        string? description = null) => new()
    {
        PlanDate = planDate,
        DayOrder = order,
        Title = title,
        Priority = priority,
        CategoryId = categoryId,
        ProjectId = projectId,
        DueDate = dueDate,
        Notes = notes,
        Description = description,
    };

    private static ICategoryRepository CreateEmptyCategoryRepository()
    {
        var mock = new Mock<ICategoryRepository>();
        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Category>());
        return mock.Object;
    }

    private static IProjectService CreateEmptyProjectService()
    {
        var mock = new Mock<IProjectService>();
        mock.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Project>());
        return mock.Object;
    }

    private static ISettingsService CreateDefaultSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        return mock.Object;
    }

    private static INotificationService CreateDefaultNotificationService() => new NullNotificationService();

    private static ITagService CreateEmptyTagService()
    {
        var mock = new Mock<ITagService>();
        mock.Setup(s => s.GetAllTagsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Tag>());
        mock.Setup(s => s.GetTagsForTaskAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Tag>());
        return mock.Object;
    }

    private static ITaskTemplateService CreateEmptyTemplateService()
    {
        var mock = new Mock<ITaskTemplateService>();
        mock.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TaskTemplate>());
        return mock.Object;
    }

    private static Mock<ITaskRepository> CreateRepositoryWithTasks(DateOnly planDate, IReadOnlyList<TaskItem> tasks)
    {
        var repository = new Mock<ITaskRepository>();
        repository.Setup(r => r.GetByDateAsync(planDate, It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => tasks.FirstOrDefault(t => t.Id == id));
        return repository;
    }

    [Fact]
    public async Task TaskEditRequested_FiresWithTheRowsTaskId_WhenItsOpenEditorCommandRuns()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var task = CreateTask(today, 0, "Read System Design");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        Guid? requestedId = null;
        sut.TaskEditRequested += (_, id) => requestedId = id;
        sut.Tasks[0].OpenEditorCommand.Execute(null);

        Assert.Equal(task.Id, requestedId);
    }

    [Fact]
    public async Task OpenEditorCommand_AddsTheTaskToRecentlyViewed()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var task = CreateTask(today, 0, "Read System Design");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.Tasks[0].OpenEditorCommand.Execute(null);

        Assert.Single(sut.RecentlyViewed);
        Assert.Equal(task.Id, sut.RecentlyViewed[0].Id);
        Assert.Equal("Read System Design", sut.RecentlyViewed[0].Title);
    }

    [Fact]
    public async Task RecentlyViewed_ReopeningTheSameTask_MovesItToTheFrontWithoutDuplicating()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var first = CreateTask(today, 0, "First");
        var second = CreateTask(today, 1, "Second");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.Tasks[0].OpenEditorCommand.Execute(null);
        sut.Tasks[1].OpenEditorCommand.Execute(null);
        sut.Tasks[0].OpenEditorCommand.Execute(null);

        Assert.Equal(2, sut.RecentlyViewed.Count);
        Assert.Equal(first.Id, sut.RecentlyViewed[0].Id);
        Assert.Equal(second.Id, sut.RecentlyViewed[1].Id);
    }

    [Fact]
    public async Task RecentlyViewed_IsCappedAtFive()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = Enumerable.Range(0, 6).Select(i => CreateTask(today, i, $"Task {i}")).ToList();
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        foreach (var row in sut.Tasks)
        {
            row.OpenEditorCommand.Execute(null);
        }

        Assert.Equal(5, sut.RecentlyViewed.Count);
        Assert.Equal("Task 5", sut.RecentlyViewed[0].Title);
    }

    [Fact]
    public async Task RecentlyViewed_ChipsOpenCommand_ReRaisesTaskEditRequested()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var task = CreateTask(today, 0, "Read System Design");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();
        sut.Tasks[0].OpenEditorCommand.Execute(null);

        Guid? requestedId = null;
        sut.TaskEditRequested += (_, id) => requestedId = id;
        sut.RecentlyViewed[0].OpenCommand.Execute(null);

        Assert.Equal(task.Id, requestedId);
    }

    [Fact]
    public async Task ReorderAsync_PersistsTheNewSequence_AndReloads()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var first = CreateTask(today, 0, "First");
        var second = CreateTask(today, 1, "Second");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.ReorderAsync(second.Id, first.Id);

        taskRepository.Verify(
            r => r.ReorderAsync(today, It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2 && ids[0] == second.Id && ids[1] == first.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        // LoadTasksAsync is called again as part of the reorder — GetByDateAsync should
        // have run twice (initial load + post-reorder reload).
        taskRepository.Verify(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ReorderAsync_WithSameSourceAndTarget_IsANoOp()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var task = CreateTask(today, 0, "Only task");
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        var taskService = new TaskService(taskRepository.Object);
        using var sut = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.ReorderAsync(task.Id, task.Id);

        taskRepository.Verify(r => r.ReorderAsync(It.IsAny<DateOnly>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Constructor_DefaultsToTodayAndIsToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        Assert.Equal(today, sut.PlanDate);
        Assert.True(sut.IsToday);
        Assert.Equal("No tasks for today", sut.EmptyStateText);
    }

    [Fact]
    public async Task GoToPreviousDayCommand_MovesBackOneDay_AndReloads()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.GoToPreviousDayCommand.ExecuteAsync(null);

        Assert.Equal(today.AddDays(-1), sut.PlanDate);
        Assert.False(sut.IsToday);
        Assert.Equal("No tasks for this day", sut.EmptyStateText);
        taskRepository.Verify(r => r.GetByDateAsync(today.AddDays(-1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoToNextDayCommand_MovesForwardOneDay()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.GoToNextDayCommand.ExecuteAsync(null);

        Assert.Equal(today.AddDays(1), sut.PlanDate);
        Assert.False(sut.IsToday);
    }

    [Fact]
    public async Task GoToTodayCommand_FromADifferentDay_ReturnsToToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.GoToNextDayCommand.ExecuteAsync(null);
        Assert.False(sut.IsToday);

        await sut.GoToTodayCommand.ExecuteAsync(null);

        Assert.Equal(today, sut.PlanDate);
        Assert.True(sut.IsToday);
    }

    [Fact]
    public void SelectedDate_Setter_NavigatesToThePickedDate()
    {
        // SelectedDate's setter kicks off navigation fire-and-forget: CalendarDatePicker's
        // two-way binding needs a plain synchronous CLR setter, so there's no "await the
        // command" the way GoToNextDayCommand etc. offer. That's fine to assert on
        // synchronously here because ITaskRepository.GetByDateAsync is mocked with
        // ReturnsAsync (an already-completed Task) — awaiting an already-completed task
        // continues synchronously rather than yielding, so the whole NavigateToAsync ->
        // LoadTasksAsync chain finishes before the property setter returns.
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        var picked = new DateTime(2026, 12, 25);

        sut.SelectedDate = picked;

        Assert.Equal(DateOnly.FromDateTime(picked), sut.PlanDate);
        Assert.Equal(picked, sut.SelectedDate);
    }

    [Fact]
    public async Task NavigatingToTheSameDayAgain_IsANoOp()
    {
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.GoToTodayCommand.ExecuteAsync(null);

        // Already on today — GoToToday should not trigger a second load.
        taskRepository.Verify(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshVisibleTasks_WithSearchText_MatchesTitleNotesOrDescription()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem>
        {
            CreateTask(today, 0, "Buy groceries"),
            CreateTask(today, 1, "Read book", notes: "groceries list is on the fridge"),
            CreateTask(today, 2, "Call dentist", description: "reschedule groceries pickup"),
            CreateTask(today, 3, "Unrelated task"),
        };
        var taskRepository = CreateRepositoryWithTasks(today, tasks);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.SearchText = "groceries";

        Assert.Equal(3, sut.VisibleTasks.Count);
        Assert.DoesNotContain(sut.VisibleTasks, t => t.Title == "Unrelated task");
    }

    [Fact]
    public async Task RefreshVisibleTasks_WithStatusFilter_ShowsOnlyMatchingTasks()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var completed = CreateTask(today, 0, "Done already");
        completed.Complete();
        var active = CreateTask(today, 1, "Still open");
        var taskRepository = CreateRepositoryWithTasks(today, [completed, active]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.SelectedStatusFilter = TaskStatusFilter.Active;
        Assert.Single(sut.VisibleTasks);
        Assert.Equal("Still open", sut.VisibleTasks[0].Title);

        sut.SelectedStatusFilter = TaskStatusFilter.Completed;
        Assert.Single(sut.VisibleTasks);
        Assert.Equal("Done already", sut.VisibleTasks[0].Title);

        sut.SelectedStatusFilter = TaskStatusFilter.All;
        Assert.Equal(2, sut.VisibleTasks.Count);
    }

    [Fact]
    public async Task RefreshVisibleTasks_WithCategoryFilter_ShowsOnlyTasksInThatCategory()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var categoryId = Guid.NewGuid();
        var inCategory = CreateTask(today, 0, "Filed", categoryId: categoryId);
        var uncategorized = CreateTask(today, 1, "Unfiled");
        var taskRepository = CreateRepositoryWithTasks(today, [inCategory, uncategorized]);
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Category { Id = categoryId, Name = "Work", ColorHex = "#3B82F6" }]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), categoryRepository.Object, CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        var workOption = Assert.Single(sut.Categories, c => c.Id == categoryId);
        sut.SelectedCategoryFilter = workOption;

        Assert.Single(sut.VisibleTasks);
        Assert.Equal("Filed", sut.VisibleTasks[0].Title);
    }

    [Fact]
    public async Task RefreshVisibleTasks_WithProjectFilter_ShowsOnlyTasksInThatProject()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var projectId = Guid.NewGuid();
        var inProject = CreateTask(today, 0, "Filed", projectId: projectId);
        var unassigned = CreateTask(today, 1, "Unfiled");
        var taskRepository = CreateRepositoryWithTasks(today, [inProject, unassigned]);
        var projectService = new Mock<IProjectService>();
        projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Project { Id = projectId, Name = "Website Redesign", ColorHex = "#6366F1" }]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), projectService.Object, CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        var projectOption = Assert.Single(sut.Projects, p => p.Id == projectId);
        sut.SelectedProjectFilter = projectOption;

        Assert.Single(sut.VisibleTasks);
        Assert.Equal("Filed", sut.VisibleTasks[0].Title);
    }

    [Fact]
    public async Task RefreshVisibleTasks_WithTagFilter_ShowsOnlyTasksWithThatTag()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tag = new Tag { Name = "Urgent" };
        var tagged = CreateTask(today, 0, "Tagged");
        tagged.Tags.Add(tag);
        var untagged = CreateTask(today, 1, "Untagged");
        var taskRepository = CreateRepositoryWithTasks(today, [tagged, untagged]);
        var tagService = new Mock<ITagService>();
        tagService.Setup(s => s.GetAllTagsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([tag]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), tagService.Object, CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        var urgentOption = Assert.Single(sut.Tags, t => t.Id == tag.Id);
        sut.SelectedTagFilter = urgentOption;

        Assert.Single(sut.VisibleTasks);
        Assert.Equal("Tagged", sut.VisibleTasks[0].Title);
    }

    [Fact]
    public async Task RefreshVisibleTasks_SortByCategory_GroupsSameCategoryTasksTogether_UncategorizedLast()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var work = new Category { Name = "Work", ColorHex = "#3B82F6" };
        var home = new Category { Name = "Home", ColorHex = "#22C55E" };
        var uncategorized = CreateTask(today, 0, "Uncategorized");
        var homeTask = CreateTask(today, 1, "Home task", categoryId: home.Id);
        homeTask.Category = home;
        var workTask1 = CreateTask(today, 2, "Work task 1", categoryId: work.Id);
        workTask1.Category = work;
        var workTask2 = CreateTask(today, 3, "Work task 2", categoryId: work.Id);
        workTask2.Category = work;
        // The real TaskRepository.GetByDateAsync always Includes Category (see its doc
        // comment); this mock has to populate the nav property itself to match, since
        // TaskItemViewModel.CategoryName reads task.Category?.Name, not CategoryId.
        var taskRepository = CreateRepositoryWithTasks(today, [uncategorized, homeTask, workTask1, workTask2]);
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([work, home]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), categoryRepository.Object, CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.SelectedSortOption = TaskSortOption.Category;

        Assert.Equal(["Home task", "Work task 1", "Work task 2", "Uncategorized"], sut.VisibleTasks.Select(t => t.Title));
    }

    [Fact]
    public async Task SelectedTemplateToApply_CreatesATaskFromTheTemplate_ThenResetsToNull()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var templateId = Guid.NewGuid();
        var templateService = new Mock<ITaskTemplateService>();
        templateService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TaskTemplate { Id = templateId, Name = "Sprint prep", TaskTitle = "Sprint planning prep" }]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), templateService.Object, CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();
        var option = Assert.Single(sut.Templates, t => t.Id == templateId);

        // Fire-and-forget from the property setter — safe to assert synchronously right after
        // (rather than awaiting anything) since every mocked async call below is already-completed
        // (ReturnsAsync), so the whole chain finishes before the setter returns. See
        // SelectedDate_Setter_NavigatesToThePickedDate's doc comment for the same reasoning.
        sut.SelectedTemplateToApply = option;

        templateService.Verify(s => s.CreateTaskFromTemplateAsync(templateId, today, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(sut.SelectedTemplateToApply);
    }

    [Fact]
    public async Task LoadTasksAsync_WithAutoRescheduleEnabled_ReschedulesOverdueTasksOnce()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        taskRepository.Setup(r => r.GetIncompleteBeforeDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { AutoRescheduleOverdueTasks = true });
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadSettingsAsync();

        await sut.LoadTasksAsync();
        await sut.LoadTasksAsync();

        taskRepository.Verify(r => r.GetIncompleteBeforeDateAsync(today, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadTasksAsync_WithAutoRescheduleDisabled_NeverChecksForOverdueTasks()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.LoadTasksAsync();

        taskRepository.Verify(r => r.GetIncompleteBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshVisibleTasks_SortByTitle_OrdersAlphabetically()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem>
        {
            CreateTask(today, 0, "Zebra"),
            CreateTask(today, 1, "Apple"),
            CreateTask(today, 2, "Mango"),
        };
        var taskRepository = CreateRepositoryWithTasks(today, tasks);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.SelectedSortOption = TaskSortOption.Title;

        Assert.Equal(["Apple", "Mango", "Zebra"], sut.VisibleTasks.Select(t => t.Title));
    }

    [Fact]
    public async Task RefreshVisibleTasks_SortByPriority_OrdersHighestFirst()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem>
        {
            CreateTask(today, 0, "Low", priority: TaskPriority.Low),
            CreateTask(today, 1, "Critical", priority: TaskPriority.Critical),
            CreateTask(today, 2, "Medium", priority: TaskPriority.Medium),
        };
        var taskRepository = CreateRepositoryWithTasks(today, tasks);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.SelectedSortOption = TaskSortOption.Priority;

        Assert.Equal(["Critical", "Medium", "Low"], sut.VisibleTasks.Select(t => t.Title));
    }

    [Fact]
    public async Task RefreshVisibleTasks_SortManual_PreservesDayOrder()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem>
        {
            CreateTask(today, 0, "First", priority: TaskPriority.Low),
            CreateTask(today, 1, "Second", priority: TaskPriority.Critical),
        };
        var taskRepository = CreateRepositoryWithTasks(today, tasks);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.SelectedSortOption = TaskSortOption.Priority;
        sut.SelectedSortOption = TaskSortOption.Manual;

        Assert.Equal(["First", "Second"], sut.VisibleTasks.Select(t => t.Title));
    }

    [Fact]
    public async Task ToggleSelectModeCommand_CascadesIsSelectModeActive_ToAllRows()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem> { CreateTask(today, 0, "Only task") };
        var taskRepository = CreateRepositoryWithTasks(today, tasks);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.ToggleSelectModeCommand.Execute(null);
        Assert.True(sut.IsSelectMode);
        Assert.True(sut.Tasks[0].IsSelectModeActive);

        sut.Tasks[0].IsSelected = true;
        sut.ToggleSelectModeCommand.Execute(null);

        Assert.False(sut.IsSelectMode);
        Assert.False(sut.Tasks[0].IsSelectModeActive);
        Assert.False(sut.Tasks[0].IsSelected);
    }

    [Fact]
    public async Task SelectAllVisibleThenClearSelection_UpdatesSelectedCountAndHasSelection()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem> { CreateTask(today, 0, "A"), CreateTask(today, 1, "B") };
        var taskRepository = CreateRepositoryWithTasks(today, tasks);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        sut.SelectAllVisibleCommand.Execute(null);
        Assert.Equal(2, sut.SelectedCount);
        Assert.True(sut.HasSelection);

        sut.ClearSelectionCommand.Execute(null);
        Assert.Equal(0, sut.SelectedCount);
        Assert.False(sut.HasSelection);
    }

    [Fact]
    public async Task BulkCompleteAsync_CompletesOnlySelectedIncompleteTasks_AndExitsSelectMode()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var alreadyDone = CreateTask(today, 0, "Already done");
        alreadyDone.Complete();
        var toComplete = CreateTask(today, 1, "To complete");
        var untouched = CreateTask(today, 2, "Untouched");
        var taskRepository = CreateRepositoryWithTasks(today, [alreadyDone, toComplete, untouched]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();
        sut.ToggleSelectModeCommand.Execute(null);
        sut.Tasks[0].IsSelected = true;
        sut.Tasks[1].IsSelected = true;

        await sut.BulkCompleteCommand.ExecuteAsync(null);

        Assert.True(sut.Tasks.Single(t => t.Title == "To complete").IsCompleted);
        Assert.False(sut.Tasks.Single(t => t.Title == "Untouched").IsCompleted);
        Assert.False(sut.IsSelectMode);
        Assert.All(sut.Tasks, t => Assert.False(t.IsSelected));
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => t.Id == toComplete.Id && t.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_DeletesSelectedTasks_ExitsSelectModeAndReloads()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var toDelete = CreateTask(today, 0, "Delete me");
        var toKeep = CreateTask(today, 1, "Keep me");
        var taskRepository = CreateRepositoryWithTasks(today, [toDelete, toKeep]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();
        sut.ToggleSelectModeCommand.Execute(null);
        sut.Tasks[0].IsSelected = true;

        await sut.BulkDeleteCommand.ExecuteAsync(null);

        Assert.False(sut.IsSelectMode);
        taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => t.Id == toDelete.Id && t.IsDeleted), It.IsAny<CancellationToken>()), Times.Once);
        // LoadTasksAsync runs again after the bulk delete — GetByDateAsync should have been called twice (initial + reload).
        taskRepository.Verify(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task LoadTasksAsync_PopulatesCategoriesWithAllOptionFirst()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = CreateRepositoryWithTasks(today, []);
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Category { Name = "Zeta", ColorHex = "#000000" },
                new Category { Name = "Alpha", ColorHex = "#111111" },
            ]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), categoryRepository.Object, CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.LoadTasksAsync();

        Assert.Equal(["All Categories", "Alpha", "Zeta"], sut.Categories.Select(c => c.Name));
        Assert.Equal(CategoryFilterOption.All, sut.SelectedCategoryFilter);
    }

    [Fact]
    public async Task LoadSettingsAsync_PopulatesAccentColorOpacityAndWindowBounds()
    {
        var taskRepository = new Mock<ITaskRepository>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings
        {
            AccentColorHex = "#EC4899",
            Theme = "Dark",
            WidgetOpacity = 0.8,
            WindowLeft = 100,
            WindowTop = 200,
            WindowWidth = 340,
            WindowHeight = 560,
            ShowInTaskbar = false,
            IsMiniWidgetMode = true,
            PreferredMonitorId = "monitor-2",
        });
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.LoadSettingsAsync();

        Assert.Equal("#EC4899", sut.AccentColorHex);
        Assert.Equal("Dark", sut.Theme);
        Assert.Equal(0.8, sut.WidgetOpacity);
        Assert.Equal(100, sut.WindowLeft);
        Assert.Equal(200, sut.WindowTop);
        Assert.Equal(340, sut.WindowWidth);
        Assert.Equal(560, sut.WindowHeight);
        Assert.False(sut.ShowInTaskbar);
        Assert.True(sut.IsMiniWidgetMode);
        Assert.Equal("monitor-2", sut.PreferredMonitorId);
    }

    [Fact]
    public async Task ToggleMiniWidgetModeCommand_FlipsTheFlag_AndPersistsIt()
    {
        var taskRepository = new Mock<ITaskRepository>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => new AppSettings());
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        Assert.False(sut.IsMiniWidgetMode);

        await sut.ToggleMiniWidgetModeCommand.ExecuteAsync(null);

        Assert.True(sut.IsMiniWidgetMode);
        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => a.IsMiniWidgetMode), It.IsAny<CancellationToken>()), Times.Once);

        await sut.ToggleMiniWidgetModeCommand.ExecuteAsync(null);

        Assert.False(sut.IsMiniWidgetMode);
        settingsService.Verify(s => s.SaveAsync(It.Is<AppSettings>(a => !a.IsMiniWidgetMode), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ShowInTaskbar_DefaultsToTrue_SoExistingUsersSeeNoBehaviorChange()
    {
        var taskRepository = new Mock<ITaskRepository>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        Assert.True(sut.ShowInTaskbar);
    }

    [Fact]
    public void WidgetBackgroundHex_ReflectsWidgetOpacityAsAlphaChannel()
    {
        var taskRepository = new Mock<ITaskRepository>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        sut.WidgetOpacity = 1.0;
        Assert.Equal("#FFFFFFFF", sut.WidgetBackgroundHex);

        sut.WidgetOpacity = 0.5;
        Assert.Equal("#80FFFFFF", sut.WidgetBackgroundHex);
    }

    [Fact]
    public void WidgetBackgroundHex_UsesADarkSlateBase_WhenIsDarkThemeIsSet()
    {
        var taskRepository = new Mock<ITaskRepository>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        sut.WidgetOpacity = 1.0;
        Assert.Equal("#FFFFFFFF", sut.WidgetBackgroundHex);

        sut.IsDarkTheme = true;

        Assert.Equal("#FF1E293B", sut.WidgetBackgroundHex);
    }

    [Fact]
    public async Task SaveWindowBoundsAsync_ReloadsThenPersistsWithNewBounds()
    {
        // Must reload-then-save (not just serialize this ViewModel's own fields), so a
        // concurrent accent/opacity change made via the Settings window during this
        // session isn't clobbered by a stale copy — see the method's doc comment.
        var taskRepository = new Mock<ITaskRepository>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { AccentColorHex = "#8B5CF6" });
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.SaveWindowBoundsAsync(10, 20, 300, 500);

        settingsService.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(a => a.AccentColorHex == "#8B5CF6" && a.WindowLeft == 10 && a.WindowTop == 20 && a.WindowWidth == 300 && a.WindowHeight == 500),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void OpenSettingsCommand_RaisesSettingsRequested()
    {
        var taskRepository = new Mock<ITaskRepository>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        var raised = false;
        sut.SettingsRequested += (_, _) => raised = true;
        sut.OpenSettingsCommand.Execute(null);

        Assert.True(raised);
    }

    /// <summary>
    /// Closes a previously-documented test gap: the midnight-rollover-follows-today logic
    /// depended on <c>DateTime.Now</c> directly, with no way to fake "now" in a test —
    /// fixed by threading every "what day is it" query through an injected
    /// <see cref="TimeProvider"/> (see <c>WidgetViewModel.Today()</c>), and making
    /// <c>OnDayRolloverTick</c> internal (<c>InternalsVisibleTo</c>, see
    /// src/DeskTodo.App/AssemblyInfo.cs) so it can be invoked directly here instead of
    /// waiting on the real 30-second <c>DispatcherTimer</c>.
    /// </summary>
    [Fact]
    public async Task OnDayRolloverTick_WhenFollowingToday_AdvancesPlanDateAndReloads()
    {
        var day1 = new DateOnly(2026, 1, 15);
        var day2 = new DateOnly(2026, 1, 16);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 23, 0, 0, TimeSpan.Zero));
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), timeProvider, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        Assert.Equal(day1, sut.PlanDate);

        timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 16, 0, 5, 0, TimeSpan.Zero));
        sut.OnDayRolloverTick(null, EventArgs.Empty);
        await Task.Yield(); // NavigateToAsync's reload is fire-and-forget from the tick handler.

        Assert.Equal(day2, sut.PlanDate);
        Assert.True(sut.IsToday);
        taskRepository.Verify(r => r.GetByDateAsync(day2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OnDayRolloverTick_WhenViewingADifferentDay_DoesNotChangePlanDate()
    {
        var viewedDay = new DateOnly(2026, 1, 10);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 23, 0, 0, TimeSpan.Zero));
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), timeProvider, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        sut.SelectedDate = viewedDay.ToDateTime(TimeOnly.MinValue); // Navigate away from "today" — planning ahead/reviewing history.
        Assert.Equal(viewedDay, sut.PlanDate);

        // Midnight passes in the real world while the widget is parked on a different day.
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 16, 0, 5, 0, TimeSpan.Zero));
        sut.OnDayRolloverTick(null, EventArgs.Empty);

        Assert.Equal(viewedDay, sut.PlanDate);
        taskRepository.Verify(r => r.GetByDateAsync(new DateOnly(2026, 1, 16), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void OnDayRolloverTick_WhenTheDayHasNotChanged_IsANoOp()
    {
        var day1 = new DateOnly(2026, 1, 15);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), timeProvider, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 15, 12, 0, 30, TimeSpan.Zero)); // 30s later, same day — one poll tick.
        sut.OnDayRolloverTick(null, EventArgs.Empty);

        Assert.Equal(day1, sut.PlanDate);
        taskRepository.Verify(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckForOverdueTaskNotificationsAsync_NotifiesOnceForANewlyOverdueTask()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var overdueTask = CreateTask(today, 0, "Pay rent", dueDate: DateTime.Now.AddHours(-1));
        var taskRepository = CreateRepositoryWithTasks(today, [overdueTask]);
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.CheckForOverdueTaskNotificationsAsync();
        await sut.CheckForOverdueTaskNotificationsAsync(); // A second poll tick shouldn't re-notify.

        notificationService.Verify(n => n.NotifyAsync("Task overdue", It.Is<string>(m => m.Contains("Pay rent")), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckForOverdueTaskNotificationsAsync_SkipsCompletedAndNotYetDueTasks()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var completedOverdue = CreateTask(today, 0, "Already done", dueDate: DateTime.Now.AddHours(-1));
        completedOverdue.Complete();
        var notYetDue = CreateTask(today, 1, "Later today", dueDate: DateTime.Now.AddHours(1));
        var noDueDate = CreateTask(today, 2, "No deadline");
        var taskRepository = CreateRepositoryWithTasks(today, [completedOverdue, notYetDue, noDueDate]);
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync(); // Also fires the daily summary (2 incomplete tasks) — expected, not what this test is about.

        await sut.CheckForOverdueTaskNotificationsAsync();

        notificationService.Verify(n => n.NotifyAsync("Task overdue", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckForOverdueTaskNotificationsAsync_SkipsATaskSnoozedIntoTheFuture()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var snoozedTask = CreateTask(today, 0, "Snoozed", dueDate: DateTime.Now.AddHours(-1));
        snoozedTask.Snooze(DateTime.Now.AddHours(1));
        var taskRepository = CreateRepositoryWithTasks(today, [snoozedTask]);
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.CheckForOverdueTaskNotificationsAsync();

        notificationService.Verify(n => n.NotifyAsync("Task overdue", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckForOverdueTaskNotificationsAsync_ReNotifies_OnceASnoozeHasPassed()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var task = CreateTask(today, 0, "Pay rent", dueDate: DateTime.Now.AddHours(-2));
        task.Snooze(DateTime.Now.AddMinutes(-1)); // Already in the past — the snooze has expired.
        var taskRepository = CreateRepositoryWithTasks(today, [task]);
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        await sut.CheckForOverdueTaskNotificationsAsync();

        notificationService.Verify(n => n.NotifyAsync("Task overdue", It.Is<string>(m => m.Contains("Pay rent")), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckForOverdueTaskNotificationsAsync_PassesTheNotificationSoundSetting()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var overdueTask = CreateTask(today, 0, "Pay rent", dueDate: DateTime.Now.AddHours(-1));
        var taskRepository = CreateRepositoryWithTasks(today, [overdueTask]);
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { NotificationSoundEnabled = false });
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadSettingsAsync();
        await sut.LoadTasksAsync();

        await sut.CheckForOverdueTaskNotificationsAsync();

        notificationService.Verify(n => n.NotifyAsync("Task overdue", It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckWellnessRemindersAsync_ImmediatelyAfterLoadSettings_DoesNotFire()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
        var taskRepository = new Mock<ITaskRepository>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { BreakReminderEnabled = true, BreakReminderIntervalMinutes = 30 });
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, notificationService.Object, timeProvider, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadSettingsAsync();

        await sut.CheckWellnessRemindersAsync();

        notificationService.Verify(n => n.NotifyAsync("Break Reminder", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckWellnessRemindersAsync_AfterTheConfiguredIntervalElapses_Fires()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
        var taskRepository = new Mock<ITaskRepository>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { BreakReminderEnabled = true, BreakReminderIntervalMinutes = 30 });
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, notificationService.Object, timeProvider, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadSettingsAsync();

        timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 15, 9, 31, 0, TimeSpan.Zero));
        await sut.CheckWellnessRemindersAsync();

        notificationService.Verify(n => n.NotifyAsync("Break Reminder", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckWellnessRemindersAsync_FiringOnce_DoesNotFireAgainUntilTheNextInterval()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
        var taskRepository = new Mock<ITaskRepository>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { WaterReminderEnabled = true, WaterReminderIntervalMinutes = 45 });
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, notificationService.Object, timeProvider, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadSettingsAsync();
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 15, 9, 46, 0, TimeSpan.Zero));
        await sut.CheckWellnessRemindersAsync();

        timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero)); // 14 more minutes, still under 45
        await sut.CheckWellnessRemindersAsync();

        notificationService.Verify(n => n.NotifyAsync("Water Reminder", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckWellnessRemindersAsync_WhenDisabled_NeverFires()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
        var taskRepository = new Mock<ITaskRepository>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { StretchReminderEnabled = false, StretchReminderIntervalMinutes = 5 });
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, notificationService.Object, timeProvider, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadSettingsAsync();

        timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
        await sut.CheckWellnessRemindersAsync();

        notificationService.Verify(n => n.NotifyAsync("Stretch Reminder", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckForOverdueTaskNotificationsAsync_WhenNotificationsDisabled_DoesNothing()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var overdueTask = CreateTask(today, 0, "Pay rent", dueDate: DateTime.Now.AddHours(-1));
        var taskRepository = CreateRepositoryWithTasks(today, [overdueTask]);
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { NotificationsEnabled = false });
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadSettingsAsync();
        await sut.LoadTasksAsync();

        await sut.CheckForOverdueTaskNotificationsAsync();

        notificationService.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadTasksAsync_SendsDailySummary_WhenViewingTodayWithIncompleteTasks()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem> { CreateTask(today, 0, "A"), CreateTask(today, 1, "B") };
        var taskRepository = CreateRepositoryWithTasks(today, tasks);
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.LoadTasksAsync();

        notificationService.Verify(n => n.NotifyAsync("Today's tasks", It.Is<string>(m => m.Contains("2 tasks")), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadTasksAsync_DoesNotSendDailySummaryTwice_OnTheSameDay()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = CreateRepositoryWithTasks(today, [CreateTask(today, 0, "A")]);
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.LoadTasksAsync();
        await sut.LoadTasksAsync(); // e.g. a drag-reorder reload later the same day.

        notificationService.Verify(n => n.NotifyAsync("Today's tasks", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoToPreviousDayCommand_DoesNotSendDailySummary_ForANonTodayDay()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([CreateTask(today.AddDays(-1), 0, "Yesterday's task")]);
        var notificationService = new Mock<INotificationService>();
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), notificationService.Object, TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

        await sut.GoToPreviousDayCommand.ExecuteAsync(null);

        notificationService.Verify(n => n.NotifyAsync("Today's tasks", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void OpenCalendarViewCommand_RaisesCalendarViewRequested()
    {
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        var raised = false;
        sut.CalendarViewRequested += (_, _) => raised = true;

        sut.OpenCalendarViewCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void OpenPlannerViewCommand_RaisesPlannerViewRequested()
    {
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        var raised = false;
        sut.PlannerViewRequested += (_, _) => raised = true;

        sut.OpenPlannerViewCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public async Task CreateTaskFromDropAsync_CreatesATaskOnThePlanDate_AndReloadsTheList()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tasks = new List<TaskItem>();
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).Returns(() => Task.FromResult<IReadOnlyList<TaskItem>>(tasks));
        taskRepository.Setup(r => r.GetMaxDayOrderAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync(-1);
        taskRepository.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => tasks.Add(t))
            .Returns(Task.CompletedTask);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        var created = await sut.CreateTaskFromDropAsync("report.pdf");

        Assert.Equal("report.pdf", created.Title);
        Assert.Equal(today, created.PlanDate);
        taskRepository.Verify(r => r.AddAsync(It.Is<TaskItem>(t => t == created), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(sut.Tasks, t => t.Id == created.Id);
    }

    [Fact]
    public async Task CreateTaskFromDropAsync_WithADescription_PersistsIt()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var taskRepository = new Mock<ITaskRepository>();
        taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        taskRepository.Setup(r => r.GetMaxDayOrderAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync(-1);
        using var sut = new WidgetViewModel(new TaskService(taskRepository.Object), CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
        await sut.LoadTasksAsync();

        var created = await sut.CreateTaskFromDropAsync("https://example.com/article", "https://example.com/article");

        Assert.Equal("https://example.com/article", created.Description);
    }

    // Pins LocalTimeZone to UTC so GetLocalNow() == GetUtcNow() exactly — otherwise these
    // tests' chosen date/time literals could land on a different calendar date depending on
    // the machine's local timezone (WidgetViewModel.Today() calls GetLocalNow(), matching
    // what a real desktop widget should show: the user's local day, not UTC's).
    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
    }
}
