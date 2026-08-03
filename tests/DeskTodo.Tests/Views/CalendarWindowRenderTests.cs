using Avalonia.Headless;
using Avalonia.Media.Imaging;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Views;

/// <summary>Renders <see cref="CalendarWindow"/> through Avalonia's headless platform (see <see cref="WidgetWindowRenderTests"/> for why).</summary>
[Collection(nameof(HeadlessCollection))]
public class CalendarWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task CalendarWindow_LoadsAndDisplaysAMonthGrid()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Task" };
            var taskService = new Mock<ITaskService>();
            taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

            var viewModel = new CalendarViewModel(taskService.Object, TimeProvider.System, NullLogger<CalendarViewModel>.Instance);
            await viewModel.LoadAsync(new DateOnly(2026, 8, 1));

            Assert.Equal(42, viewModel.Days.Count);

            var window = new CalendarWindow { DataContext = viewModel };
            window.Show();

            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
                frame.Save(Path.Combine(screenshotDir, "calendar-window.png"), PngBitmapEncoderOptions.Default);
            }

            window.Close();
            return true;
        }, CancellationToken.None);
    }
}
