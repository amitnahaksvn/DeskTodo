using Avalonia.Headless;
using Avalonia.Media.Imaging;
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

/// <summary>Renders <see cref="GridWindow"/> through Avalonia's headless platform (see <see cref="WidgetWindowRenderTests"/> for why).</summary>
[Collection(nameof(HeadlessCollection))]
public class GridWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task GridWindow_LoadsAndDisplaysTasksAcrossMultipleDays()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var category = new Category { Name = "Work", ColorHex = "#3B82F6" };
            var tasks = new List<TaskItem>
            {
                new() { PlanDate = new DateOnly(2026, 7, 27), Title = "Read System Design", Priority = TaskPriority.High, CategoryId = category.Id, Category = category },
                new() { PlanDate = new DateOnly(2026, 7, 28), Title = "Team Meeting", Priority = TaskPriority.Critical, DueDate = new DateTime(2026, 7, 28, 14, 0, 0) },
            };

            var taskService = new Mock<ITaskService>();
            taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
            var categoryRepository = new Mock<ICategoryRepository>();
            categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([category]);
            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());

            var projectService = new Mock<IProjectService>();
            projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

            var viewModel = new GridViewModel(taskService.Object, categoryRepository.Object, projectService.Object, settingsService.Object, TimeProvider.System, NullLogger<GridViewModel>.Instance);
            await viewModel.LoadAsync();

            Assert.Equal(2, viewModel.Rows.Count);

            var window = new GridWindow { DataContext = viewModel };
            window.Show();

            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
                frame.Save(Path.Combine(screenshotDir, "grid-window.png"), PngBitmapEncoderOptions.Default);
            }

            window.Close();
            return true;
        }, CancellationToken.None);
    }

    [Fact]
    public async Task GridWindow_WithNoTasks_RendersWithoutError()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var taskService = new Mock<ITaskService>();
            taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
            var categoryRepository = new Mock<ICategoryRepository>();
            categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());

            var projectService = new Mock<IProjectService>();
            projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

            var viewModel = new GridViewModel(taskService.Object, categoryRepository.Object, projectService.Object, settingsService.Object, TimeProvider.System, NullLogger<GridViewModel>.Instance);
            await viewModel.LoadAsync();

            var window = new GridWindow { DataContext = viewModel };
            window.Show();

            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);

            window.Close();
            return true;
        }, CancellationToken.None);
    }
}
