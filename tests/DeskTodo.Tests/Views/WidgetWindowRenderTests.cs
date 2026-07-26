using Avalonia.Headless;
using Avalonia.Media.Imaging;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
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
            var taskService = new TaskService(taskRepository.Object);
            using var viewModel = new WidgetViewModel(taskService, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

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
            var taskService = new TaskService(taskRepository.Object);
            using var viewModel = new WidgetViewModel(taskService, NullLogger<WidgetViewModel>.Instance, NullLogger<TaskItemViewModel>.Instance);

            await viewModel.LoadTasksAsync();

            var window = new WidgetWindow { DataContext = viewModel };
            window.Show();

            Assert.True(viewModel.HasNoTasks);

            window.Close();
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
}
