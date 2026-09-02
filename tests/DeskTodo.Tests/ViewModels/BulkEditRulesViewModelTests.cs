using DeskTodo.App.ViewModels;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.ViewModels;

public class BulkEditRulesViewModelTests
{
    private readonly Mock<IBulkEditRuleService> _ruleService = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();

    private BulkEditRulesViewModel CreateSut()
    {
        _projectRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _ruleService.Setup(s => s.GetRulesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return new BulkEditRulesViewModel(_ruleService.Object, _projectRepository.Object, _categoryRepository.Object, NullLogger<BulkEditRulesViewModel>.Instance);
    }

    [Fact]
    public void ParseConditionsText_ParsesFieldOperatorAndValue()
    {
        var conditions = BulkEditRulesViewModel.ParseConditionsText("Priority | Equals | High\nDueDate | LessThan | Today", [], []);

        Assert.Equal(2, conditions.Count);
        Assert.Equal(BulkEditConditionField.Priority, conditions[0].Field);
        Assert.Equal(BulkEditConditionOperator.Equals, conditions[0].Operator);
        Assert.Equal("High", conditions[0].Value);
        Assert.Equal(BulkEditConditionOperator.LessThan, conditions[1].Operator);
    }

    [Fact]
    public void ParseConditionsText_ResolvesProjectNameToId()
    {
        var project = new Project { Name = "Website Redesign", ColorHex = "#4A90D9" };

        var conditions = BulkEditRulesViewModel.ParseConditionsText("Project | Equals | Website Redesign", [project], []);

        var condition = Assert.Single(conditions);
        Assert.Equal(project.Id.ToString(), condition.Value);
    }

    [Fact]
    public void ParseConditionsText_WithAnUnknownProjectName_SkipsTheLine()
    {
        var conditions = BulkEditRulesViewModel.ParseConditionsText("Project | Equals | Nonexistent", [], []);

        Assert.Empty(conditions);
    }

    [Fact]
    public void ParseConditionsText_SkipsUnparsableFieldLines()
    {
        var conditions = BulkEditRulesViewModel.ParseConditionsText("NotAField | Equals | X\nPriority | Equals | High", [], []);

        var condition = Assert.Single(conditions);
        Assert.Equal(BulkEditConditionField.Priority, condition.Field);
    }

    [Fact]
    public void ParseActionsText_ParsesTypeAndValue()
    {
        var actions = BulkEditRulesViewModel.ParseActionsText("SetPriority | Critical\nMarkCompleted", [], []);

        Assert.Equal(2, actions.Count);
        Assert.Equal("Critical", actions[0].Value);
        Assert.Equal(BulkEditActionType.MarkCompleted, actions[1].Type);
        Assert.Equal(string.Empty, actions[1].Value);
    }

    [Fact]
    public void ParseActionsText_ResolvesProjectNameForMoveToProject()
    {
        var project = new Project { Name = "Recovery", ColorHex = "#4A90D9" };

        var actions = BulkEditRulesViewModel.ParseActionsText("MoveToProject | Recovery", [project], []);

        var action = Assert.Single(actions);
        Assert.Equal(project.Id.ToString(), action.Value);
    }

    [Fact]
    public async Task PreviewAsync_PopulatesCountAndSampleTitles()
    {
        var sut = CreateSut();
        var task = new TaskItem { PlanDate = DateOnly.FromDateTime(DateTime.Today), Title = "Ship it" };
        _ruleService.Setup(s => s.PreviewAsync(It.IsAny<IReadOnlyList<BulkEditCondition>>(), It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        await sut.PreviewCommand.ExecuteAsync(null);

        Assert.Equal(1, sut.PreviewCount);
        Assert.Contains("Ship it", sut.PreviewSampleTitles);
    }

    [Fact]
    public async Task ApplyAsync_WhenNoActionsParse_SetsErrorMessage()
    {
        var sut = CreateSut();
        sut.ActionsText = string.Empty;

        await sut.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("at least one action", sut.ErrorMessage);
        _ruleService.Verify(s => s.ApplyAsync(It.IsAny<IReadOnlyList<BulkEditCondition>>(), It.IsAny<IReadOnlyList<BulkEditAction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_WithADeleteAction_WithoutConfirmation_SetsErrorMessage_WithoutApplying()
    {
        var sut = CreateSut();
        sut.ActionsText = "Delete";
        sut.ConfirmDestructiveAction = false;

        await sut.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("deletes tasks", sut.ErrorMessage);
        _ruleService.Verify(s => s.ApplyAsync(It.IsAny<IReadOnlyList<BulkEditCondition>>(), It.IsAny<IReadOnlyList<BulkEditAction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_WithADeleteAction_WhenConfirmed_Applies()
    {
        var sut = CreateSut();
        sut.ActionsText = "Delete";
        sut.ConfirmDestructiveAction = true;
        _ruleService.Setup(s => s.ApplyAsync(It.IsAny<IReadOnlyList<BulkEditCondition>>(), It.IsAny<IReadOnlyList<BulkEditAction>>(), It.IsAny<CancellationToken>())).ReturnsAsync(3);

        await sut.ApplyCommand.ExecuteAsync(null);

        Assert.Contains("3 task", sut.StatusMessage);
        Assert.False(sut.ConfirmDestructiveAction);
    }

    [Fact]
    public async Task SaveRuleAsync_WhenNameIsBlank_SetsErrorMessage()
    {
        var sut = CreateSut();
        sut.NewRuleName = " ";
        sut.ActionsText = "MarkCompleted";

        await sut.SaveRuleCommand.ExecuteAsync(null);

        Assert.Equal("Enter a name for the rule.", sut.ErrorMessage);
        _ruleService.Verify(s => s.CreateRuleAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<BulkEditCondition>>(), It.IsAny<IReadOnlyList<BulkEditAction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveRuleAsync_WithValidInput_SavesAndClearsTheName()
    {
        var sut = CreateSut();
        sut.NewRuleName = "  Escalate Overdue  ";
        sut.ActionsText = "SetPriority | Critical";

        await sut.SaveRuleCommand.ExecuteAsync(null);

        _ruleService.Verify(s => s.CreateRuleAsync("Escalate Overdue", It.IsAny<IReadOnlyList<BulkEditCondition>>(), It.IsAny<IReadOnlyList<BulkEditAction>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, sut.NewRuleName);
    }

    [Fact]
    public async Task LoadAsync_MapsRuleRowsIncludingWhetherTheyHaveADestructiveAction()
    {
        var sut = CreateSut();
        var rule = new BulkEditRule
        {
            Name = "Cleanup",
            Actions = [new BulkEditAction { Type = BulkEditActionType.Delete }],
        };
        _ruleService.Setup(s => s.GetRulesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([rule]);

        await sut.LoadAsync();

        var row = Assert.Single(sut.Rules);
        Assert.True(row.HasDestructiveAction);
    }

    [Fact]
    public async Task ApplySavedRuleAsync_WithADestructiveRule_WithoutConfirmation_SetsErrorMessage()
    {
        var sut = CreateSut();
        var row = new BulkEditRuleRow(Guid.NewGuid(), "Cleanup", 0, 1, HasDestructiveAction: true);

        await sut.ApplySavedRuleCommand.ExecuteAsync(row);

        Assert.Contains("deletes tasks", sut.ErrorMessage);
        _ruleService.Verify(s => s.ApplyRuleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
