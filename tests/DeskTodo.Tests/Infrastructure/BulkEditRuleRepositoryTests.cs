using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Infrastructure.Repositories;

namespace DeskTodo.Tests.Infrastructure;

public class BulkEditRuleRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture = new();
    private readonly BulkEditRuleRepository _sut;

    public BulkEditRuleRepositoryTests()
    {
        _sut = new BulkEditRuleRepository(_fixture.ContextFactory);
    }

    public void Dispose() => _fixture.Dispose();

    private static BulkEditRule MakeRule(string name) => new()
    {
        Name = name,
        Conditions =
        [
            new BulkEditCondition { Field = BulkEditConditionField.Priority, Operator = BulkEditConditionOperator.Equals, Value = "High" },
            new BulkEditCondition { Field = BulkEditConditionField.DueDate, Operator = BulkEditConditionOperator.LessThan, Value = "Today" },
        ],
        Actions =
        [
            new BulkEditAction { Type = BulkEditActionType.SetPriority, Value = "Critical" },
            new BulkEditAction { Type = BulkEditActionType.AddTag, Value = "overdue" },
        ],
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsConditionsAndActions()
    {
        var rule = MakeRule("Escalate overdue high-priority tasks");

        await _sut.AddAsync(rule);
        var loaded = await _sut.GetByIdAsync(rule.Id);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Conditions.Count);
        Assert.Equal(BulkEditConditionField.DueDate, loaded.Conditions[1].Field);
        Assert.Equal(2, loaded.Actions.Count);
        Assert.Equal("overdue", loaded.Actions[1].Value);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRulesOrderedByName()
    {
        await _sut.AddAsync(MakeRule("Zeta Rule"));
        await _sut.AddAsync(MakeRule("Alpha Rule"));

        var all = await _sut.GetAllAsync();

        Assert.Equal(["Alpha Rule", "Zeta Rule"], all.Select(r => r.Name));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRule()
    {
        var rule = MakeRule("Rule");
        await _sut.AddAsync(rule);

        await _sut.DeleteAsync(rule.Id);

        Assert.Null(await _sut.GetByIdAsync(rule.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenRuleMissing_DoesNotThrow()
    {
        await _sut.DeleteAsync(Guid.NewGuid());
    }
}
