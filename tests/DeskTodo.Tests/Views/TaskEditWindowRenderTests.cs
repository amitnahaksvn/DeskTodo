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

/// <summary>Renders <see cref="TaskEditWindow"/> through Avalonia's headless platform (see <see cref="WidgetWindowRenderTests"/> for why).</summary>
[Collection(nameof(HeadlessCollection))]
public class TaskEditWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task TaskEditWindow_LoadsAndDisplaysTheTasksFields()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var task = new TaskItem
            {
                PlanDate = DateOnly.FromDateTime(DateTime.Now),
                Title = "Read System Design",
                Description = "Chapter 4: caching",
                Priority = TaskPriority.High,
                Notes = "Focus on cache invalidation strategies",
            };
            var category = new Category { Name = "Learning", ColorHex = "#8B5CF6" };

            var taskRepository = new Mock<ITaskRepository>();
            taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
            taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TaskItem>());
            var categoryRepository = new Mock<ICategoryRepository>();
            categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([category]);
            var taskService = new TaskService(taskRepository.Object, Mock.Of<ITaskHistoryRepository>(), Mock.Of<ITaskVersionRepository>());
            var checklistService = new Mock<IChecklistService>();
            var tagService = new Mock<ITagService>();
            var templateService = new Mock<ITaskTemplateService>();
            var taskDependencyService = new Mock<ITaskDependencyService>();
            var attachmentService = new Mock<IAttachmentService>();
            attachmentService.Setup(s => s.GetAttachmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Attachment>());
            var milestoneService = new Mock<IMilestoneService>();
            milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Milestone>());
            var projectService = new Mock<IProjectService>();
            projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Project>());
            var viewModel = new TaskEditViewModel(
                taskService,
                categoryRepository.Object,
                checklistService.Object,
                tagService.Object,
                templateService.Object,
                taskDependencyService.Object,
                attachmentService.Object,
                milestoneService.Object,
                projectService.Object,
                NullLogger<TaskEditViewModel>.Instance,
                NullLogger<ChecklistItemRowViewModel>.Instance);

            await viewModel.LoadAsync(task.Id);

            Assert.True(viewModel.IsLoaded);
            Assert.Equal(task.Title, viewModel.Title);
            Assert.Equal(task.Description, viewModel.Description);
            Assert.Equal(TaskPriority.High, viewModel.Priority);
            Assert.Equal(2, viewModel.Categories.Count); // CategoryOption.None + the one mocked category ("Learning")
            Assert.Equal(CategoryOption.None, viewModel.SelectedCategory); // task.CategoryId is null

            var window = new TaskEditWindow { DataContext = viewModel };
            window.Show();

            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
                frame.Save(Path.Combine(screenshotDir, "task-edit-window.png"), PngBitmapEncoderOptions.Default);
            }

            window.Close();
            return true;
        }, CancellationToken.None);
    }

    [Fact]
    public async Task SaveCommand_PersistsEditedFields()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var task = new TaskItem { PlanDate = DateOnly.FromDateTime(DateTime.Now), Title = "Original title" };
            var taskRepository = new Mock<ITaskRepository>();
            taskRepository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
            taskRepository.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TaskItem>());
            var categoryRepository = new Mock<ICategoryRepository>();
            categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
            var taskService = new TaskService(taskRepository.Object, Mock.Of<ITaskHistoryRepository>(), Mock.Of<ITaskVersionRepository>());
            var checklistService = new Mock<IChecklistService>();
            var tagService = new Mock<ITagService>();
            var templateService = new Mock<ITaskTemplateService>();
            var taskDependencyService = new Mock<ITaskDependencyService>();
            var attachmentService = new Mock<IAttachmentService>();
            attachmentService.Setup(s => s.GetAttachmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Attachment>());
            var milestoneService = new Mock<IMilestoneService>();
            milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Milestone>());
            var projectService = new Mock<IProjectService>();
            projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Project>());
            var viewModel = new TaskEditViewModel(
                taskService,
                categoryRepository.Object,
                checklistService.Object,
                tagService.Object,
                templateService.Object,
                taskDependencyService.Object,
                attachmentService.Object,
                milestoneService.Object,
                projectService.Object,
                NullLogger<TaskEditViewModel>.Instance,
                NullLogger<ChecklistItemRowViewModel>.Instance);
            await viewModel.LoadAsync(task.Id);

            var saved = false;
            viewModel.Saved += (_, _) => saved = true;
            viewModel.Title = "Updated title";
            viewModel.Priority = TaskPriority.Critical;

            await viewModel.SaveCommand.ExecuteAsync(null);

            Assert.True(saved);
            taskRepository.Verify(
                r => r.UpdateAsync(It.Is<TaskItem>(t => t.Title == "Updated title" && t.Priority == TaskPriority.Critical), It.IsAny<CancellationToken>()),
                Times.Once);
            return true;
        }, CancellationToken.None);
    }
}
