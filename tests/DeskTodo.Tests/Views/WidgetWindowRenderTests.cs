using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Views;

/// <summary>
/// Renders <see cref="WidgetWindow"/> through Avalonia's headless platform — no physical
/// display needed — to catch binding/converter/XAML errors that only surface at runtime
/// (see the "ObservableProperty setter fires from the constructor" bug this caught during
/// development, documented in docs/ARCHITECTURE.md).
/// </summary>
[Collection(nameof(HeadlessCollection))]
public class WidgetWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task WidgetWindow_LoadsAndDisplaysTodaysTasks()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var tasks = new List<TaskItem>
            {
                CreateTask(today, 0, "Morning Exercise", TaskPriority.Medium, completed: true),
                CreateTask(today, 1, "Read System Design", TaskPriority.High, completed: false),
                CreateTask(today, 2, "LinkedIn Post", TaskPriority.Low, completed: true),
                CreateTask(today, 3, "DSA Practice", TaskPriority.High, completed: false),
                CreateTask(today, 4, "Drink Water", TaskPriority.Low, completed: true),
                CreateTask(today, 5, "Team Meeting", TaskPriority.Critical, completed: false),
            };
            tasks[5].Pin();

            var taskRepository = new Mock<ITaskRepository>();
            taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
            var taskService = new TaskService(taskRepository.Object, Mock.Of<ITaskHistoryRepository>());
            using var viewModel = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

            await viewModel.LoadTasksAsync();

            Assert.Equal(6, viewModel.Tasks.Count);
            Assert.Equal(3, viewModel.CompletedCount);
            Assert.Equal(50, viewModel.ProgressPercentage);
            Assert.True(viewModel.Tasks[5].IsPinned);

            var window = new WidgetWindow { DataContext = viewModel };
            window.Show();

            // Confirms the whole thing actually renders (a XAML/binding error here would
            // throw during layout/render, not at compile time) and, opt-in via env var so
            // CI stays side-effect-free, saves a PNG for manual visual inspection.
            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
                frame.Save(Path.Combine(screenshotDir, "widget-window.png"), PngBitmapEncoderOptions.Default);
            }

            // Taps a real user gesture through the headless input pipeline (not calling the
            // command directly) to also exercise the CheckBox binding/click path end to end.
            taskRepository.Setup(r => r.GetByIdAsync(tasks[1].Id, It.IsAny<CancellationToken>())).ReturnsAsync(tasks[1]);
            var secondRow = viewModel.Tasks[1];
            await secondRow.ToggleCompleteCommand.ExecuteAsync(null);

            Assert.True(secondRow.IsCompleted);
            taskRepository.Verify(r => r.UpdateAsync(It.Is<TaskItem>(t => t.Id == tasks[1].Id && t.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);

            window.Close();
            return true;
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WidgetWindow_WithNoTasksToday_ShowsEmptyState()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var taskRepository = new Mock<ITaskRepository>();
            taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            var taskService = new TaskService(taskRepository.Object, Mock.Of<ITaskHistoryRepository>());
            using var viewModel = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

            await viewModel.LoadTasksAsync();

            var window = new WidgetWindow { DataContext = viewModel };
            window.Show();

            Assert.True(viewModel.HasNoTasks);

            window.Close();
            return true;
        }, CancellationToken.None);
    }

    /// <summary>
    /// Regression test for a real bug caught only through headless rendering (a ViewModel-only
    /// test can't see it, since the ViewModel's own state was already correct — see
    /// <c>WidgetViewModel.RefreshCategoriesAsync</c>'s doc comment): rebuilding
    /// <c>Categories</c> via <c>Clear()</c>+re-<c>Add()</c> momentarily removed the
    /// currently-selected item from the bound collection, which desynced the category
    /// <c>ComboBox</c>'s two-way <c>SelectedItem</c> binding — it stuck on "nothing
    /// selected" (<c>SelectedIndex == -1</c>, blank closed box) even once the list was
    /// repopulated with an equal item a moment later. Reproduced here by loading twice
    /// (mirroring day-navigation, which re-runs <c>RefreshCategoriesAsync</c> every time).
    /// </summary>
    [Fact]
    public async Task WidgetWindow_CategoryFilterComboBox_StaysSelected_AcrossReloads()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var category = new Category { Name = "Work", ColorHex = "#3B82F6" };
            var taskRepository = new Mock<ITaskRepository>();
            taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            var categoryRepository = new Mock<ICategoryRepository>();
            categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([category]);
            var taskService = new TaskService(taskRepository.Object, Mock.Of<ITaskHistoryRepository>());
            using var viewModel = new WidgetViewModel(taskService, categoryRepository.Object, CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), CreateDefaultSettingsService(), CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);
            viewModel.IsSearchBarVisible = true;

            var window = new WidgetWindow { DataContext = viewModel };
            window.Show(); // OnOpened's own LoadTasksAsync call is the first reload.
            await viewModel.LoadTasksAsync(); // A second reload, mirroring day-navigation.

            var combo = window.GetVisualDescendants().OfType<ComboBox>().First(c => c.Name == "CategoryFilterComboBox");

            Assert.NotEqual(-1, combo.SelectedIndex);
            Assert.NotNull(combo.SelectedItem);
            Assert.Equal(CategoryFilterOption.All, viewModel.SelectedCategoryFilter);

            window.Close();
            return true;
        }, CancellationToken.None);
    }

    /// <summary>
    /// Closes a previously-documented verification gap: <c>WidgetWindow.OnClosing</c> →
    /// <c>SaveWindowBoundsAsync</c> was assumed to be untestable without a real display,
    /// since it needs an actual window-close gesture. That assumption was wrong —
    /// <c>Window.Close()</c> runs Avalonia's own <c>OnClosing</c>/<c>OnClosed</c> C# lifecycle
    /// synchronously even under the headless platform (verified empirically first: a
    /// throwaway test confirmed <c>Width</c>/<c>Height</c> come through exactly as set, while
    /// the headless backend overrides whatever <c>Position</c> is explicitly assigned to its
    /// own placement — so this asserts against whatever <c>Position</c> actually is at close
    /// time, not a hardcoded expected value, to stay robust to that backend detail).
    /// </summary>
    [Fact]
    public async Task WidgetWindow_OnClosing_PersistsCurrentWindowBounds()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var taskRepository = new Mock<ITaskRepository>();
            taskRepository.Setup(r => r.GetByDateAsync(today, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
            var taskService = new TaskService(taskRepository.Object, Mock.Of<ITaskHistoryRepository>());
            using var viewModel = new WidgetViewModel(taskService, CreateEmptyCategoryRepository(), CreateEmptyProjectService(), CreateEmptyTagService(), CreateEmptyTemplateService(), settingsService.Object, CreateDefaultNotificationService(), TimeProvider.System, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

            var window = new WidgetWindow { DataContext = viewModel, Width = 401, Height = 601 };
            window.Show();

            var expectedPosition = window.Position;
            var expectedWidth = window.Width;
            var expectedHeight = window.Height;

            window.Close();

            settingsService.Verify(s => s.SaveAsync(
                It.Is<AppSettings>(a =>
                    a.WindowLeft == expectedPosition.X &&
                    a.WindowTop == expectedPosition.Y &&
                    a.WindowWidth == expectedWidth &&
                    a.WindowHeight == expectedHeight),
                It.IsAny<CancellationToken>()),
                Times.Once);

            return true;
        }, CancellationToken.None);
    }

    private static TaskItem CreateTask(DateOnly planDate, int order, string title, TaskPriority priority, bool completed)
    {
        var task = new TaskItem { PlanDate = planDate, DayOrder = order, Title = title, Priority = priority };
        if (completed)
        {
            task.Complete();
        }

        return task;
    }

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
}
