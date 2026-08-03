using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Views;

/// <summary>Renders <see cref="PlannerWindow"/> (all eight tabs — Week/Year/Agenda/Timeline/Kanban/Matrix/Goals/Milestones — via <see cref="PlannerViewModel"/>) through Avalonia's headless platform (see <see cref="WidgetWindowRenderTests"/> for why).</summary>
[Collection(nameof(HeadlessCollection))]
public class PlannerWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task PlannerWindow_LoadsAndDisplaysEveryTab()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var tasks = new[]
            {
                new TaskItem { PlanDate = new DateOnly(2026, 8, 15), Title = "Today task", Priority = TaskPriority.Critical, DueDate = new DateTime(2026, 8, 15, 9, 0, 0) },
                new TaskItem { PlanDate = new DateOnly(2026, 8, 16), Title = "Tomorrow task", Priority = TaskPriority.Low },
            };
            var taskService = new Mock<ITaskService>();
            taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

            var goal = new Goal { Name = "Meditate daily" };
            goal.Completions.Add(new GoalCompletion { GoalId = goal.Id, CompletedDate = new DateOnly(2026, 8, 15) });
            var goalService = new Mock<IGoalService>();
            goalService.Setup(s => s.GetGoalsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([goal]);

            var milestone = new Milestone { Title = "Ship v1", TargetDate = new DateOnly(2026, 9, 1) };
            var milestoneService = new Mock<IMilestoneService>();
            milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([milestone]);

            var week = new WeekViewModel(taskService.Object, TimeProvider.System, NullLogger<WeekViewModel>.Instance);
            var year = new YearViewModel(taskService.Object, TimeProvider.System, NullLogger<YearViewModel>.Instance);
            var agenda = new AgendaViewModel(taskService.Object, TimeProvider.System, NullLogger<AgendaViewModel>.Instance);
            var timeline = new TimelineViewModel(taskService.Object, NullLogger<TimelineViewModel>.Instance);
            var kanban = new KanbanViewModel(taskService.Object, NullLogger<KanbanViewModel>.Instance);
            var matrix = new MatrixViewModel(taskService.Object, TimeProvider.System, NullLogger<MatrixViewModel>.Instance);
            var goals = new GoalsViewModel(goalService.Object, TimeProvider.System, NullLogger<GoalsViewModel>.Instance);
            var milestones = new MilestonesViewModel(milestoneService.Object, NullLogger<MilestonesViewModel>.Instance);
            var viewModel = new PlannerViewModel(week, year, agenda, timeline, kanban, matrix, goals, milestones);

            await viewModel.LoadAsync(new DateOnly(2026, 8, 15));

            var window = new PlannerWindow { DataContext = viewModel };
            window.Show();

            var tabControl = window.GetVisualDescendants().OfType<TabControl>().Single();
            Assert.Equal(8, tabControl.ItemCount);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");

            for (var i = 0; i < tabControl.ItemCount; i++)
            {
                tabControl.SelectedIndex = i;

                var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

                if (!string.IsNullOrWhiteSpace(screenshotDir))
                {
                    Directory.CreateDirectory(screenshotDir);
                    var tabHeader = ((TabItem)tabControl.Items[i]!).Header;
                    frame.Save(Path.Combine(screenshotDir, $"planner-window-{tabHeader}.png"), PngBitmapEncoderOptions.Default);
                }
            }

            window.Close();
            return true;
        }, CancellationToken.None);
    }
}
