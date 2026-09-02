using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Bulk Edit Rules window (Feature 88, Roadmap-39-100.md) — build a set of AND-ed
/// conditions and actions (as compact text lines, the same approach
/// <see cref="ProjectTemplatesViewModel"/> uses for task/milestone items), preview how many
/// tasks match before touching anything, then apply. A destructive action (Delete) can't be
/// applied until <see cref="ConfirmDestructiveAction"/> is explicitly checked — this feature's
/// own "Safety" spec requires additional confirmation before a destructive bulk operation runs.
/// </summary>
public sealed partial class BulkEditRulesViewModel(
    IBulkEditRuleService ruleService,
    IProjectRepository projectRepository,
    ICategoryRepository categoryRepository,
    ILogger<BulkEditRulesViewModel> logger) : ViewModelBase
{
    private IReadOnlyList<Project> _projects = [];
    private IReadOnlyList<Category> _categories = [];

    public ObservableCollection<BulkEditRuleRow> Rules { get; } = [];

    [ObservableProperty]
    public partial string NewRuleName { get; set; } = string.Empty;

    /// <summary>One condition per line: <c>Field | Operator | Value</c> — e.g. <c>Priority | Equals | High</c>, <c>DueDate | LessThan | Today</c>, <c>Project | Equals | Website Redesign</c> (Project/Category values are matched by name, not id).</summary>
    [ObservableProperty]
    public partial string ConditionsText { get; set; } = string.Empty;

    /// <summary>One action per line: <c>Type | Value</c> — e.g. <c>SetPriority | Critical</c>, <c>AddTag | overdue</c>, <c>MoveToProject | Recovery</c>. MarkCompleted/Delete need no value.</summary>
    [ObservableProperty]
    public partial string ActionsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ConfirmDestructiveAction { get; set; }

    [ObservableProperty]
    public partial int? PreviewCount { get; set; }

    public ObservableCollection<string> PreviewSampleTitles { get; } = [];

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _projects = await projectRepository.GetAllAsync(cancellationToken);
            _categories = await categoryRepository.GetAllAsync(cancellationToken);

            var rules = await ruleService.GetRulesAsync(cancellationToken);
            Rules.Clear();
            foreach (var rule in rules)
            {
                Rules.Add(new BulkEditRuleRow(rule.Id, rule.Name, rule.Conditions.Count, rule.Actions.Count,
                    rule.Actions.Any(a => a.Type == BulkEditActionType.Delete)));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load bulk edit rules");
            ErrorMessage = "Couldn't load bulk edit rules.";
        }
    }

    internal static List<BulkEditCondition> ParseConditionsText(string text, IReadOnlyList<Project> projects, IReadOnlyList<Category> categories)
    {
        var conditions = new List<BulkEditCondition>();
        foreach (var rawLine in text.Split('\n'))
        {
            var parts = rawLine.Trim().Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !Enum.TryParse<BulkEditConditionField>(parts[0], ignoreCase: true, out var field))
            {
                continue;
            }

            var op = parts.Length > 1 && Enum.TryParse<BulkEditConditionOperator>(parts[1], ignoreCase: true, out var parsedOp) ? parsedOp : BulkEditConditionOperator.Equals;
            var rawValue = parts.Length > 2 ? parts[2] : string.Empty;
            var value = ResolveNameToId(field, rawValue, projects, categories);
            if (value is null)
            {
                continue;
            }

            conditions.Add(new BulkEditCondition { Field = field, Operator = op, Value = value });
        }

        return conditions;
    }

    internal static List<BulkEditAction> ParseActionsText(string text, IReadOnlyList<Project> projects, IReadOnlyList<Category> categories)
    {
        var actions = new List<BulkEditAction>();
        foreach (var rawLine in text.Split('\n'))
        {
            var parts = rawLine.Trim().Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || !Enum.TryParse<BulkEditActionType>(parts[0], ignoreCase: true, out var type))
            {
                continue;
            }

            var rawValue = parts.Length > 1 ? parts[1] : string.Empty;
            var value = type switch
            {
                BulkEditActionType.MoveToProject => projects.FirstOrDefault(p => string.Equals(p.Name, rawValue, StringComparison.OrdinalIgnoreCase))?.Id.ToString(),
                BulkEditActionType.SetCategory => categories.FirstOrDefault(c => string.Equals(c.Name, rawValue, StringComparison.OrdinalIgnoreCase))?.Id.ToString(),
                BulkEditActionType.MarkCompleted or BulkEditActionType.Delete => string.Empty,
                _ => rawValue,
            };

            if (value is null)
            {
                continue;
            }

            actions.Add(new BulkEditAction { Type = type, Value = value });
        }

        return actions;
    }

    private static string? ResolveNameToId(BulkEditConditionField field, string rawValue, IReadOnlyList<Project> projects, IReadOnlyList<Category> categories) => field switch
    {
        BulkEditConditionField.Project => projects.FirstOrDefault(p => string.Equals(p.Name, rawValue, StringComparison.OrdinalIgnoreCase))?.Id.ToString(),
        BulkEditConditionField.Category => categories.FirstOrDefault(c => string.Equals(c.Name, rawValue, StringComparison.OrdinalIgnoreCase))?.Id.ToString(),
        _ => rawValue,
    };

    private bool TryBuildConditionsAndActions(out List<BulkEditCondition> conditions, out List<BulkEditAction> actions)
    {
        conditions = ParseConditionsText(ConditionsText, _projects, _categories);
        actions = ParseActionsText(ActionsText, _projects, _categories);

        if (actions.Count == 0)
        {
            ErrorMessage = "Add at least one action (Type | Value, one per line).";
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        ErrorMessage = string.Empty;
        var conditions = ParseConditionsText(ConditionsText, _projects, _categories);

        try
        {
            var matches = await ruleService.PreviewAsync(conditions);
            PreviewCount = matches.Count;
            PreviewSampleTitles.Clear();
            foreach (var title in matches.Take(10).Select(t => t.Title))
            {
                PreviewSampleTitles.Add(title);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to preview a bulk edit rule");
            ErrorMessage = "Couldn't preview matching tasks.";
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        if (!TryBuildConditionsAndActions(out var conditions, out var actions))
        {
            return;
        }

        if (actions.Any(a => a.Type == BulkEditActionType.Delete) && !ConfirmDestructiveAction)
        {
            ErrorMessage = "This rule deletes tasks — check the confirmation box before applying.";
            return;
        }

        try
        {
            var count = await ruleService.ApplyAsync(conditions, actions);
            StatusMessage = $"Applied to {count} task(s).";
            ConfirmDestructiveAction = false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply a bulk edit rule");
            ErrorMessage = "Couldn't apply the rule.";
        }
    }

    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        ErrorMessage = string.Empty;
        var name = NewRuleName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Enter a name for the rule.";
            return;
        }

        if (!TryBuildConditionsAndActions(out var conditions, out var actions))
        {
            return;
        }

        try
        {
            await ruleService.CreateRuleAsync(name, conditions, actions);
            NewRuleName = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save bulk edit rule '{Name}'", name);
            ErrorMessage = "Couldn't save the rule.";
        }
    }

    [RelayCommand]
    private async Task ApplySavedRuleAsync(BulkEditRuleRow row)
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        if (row.HasDestructiveAction && !ConfirmDestructiveAction)
        {
            ErrorMessage = "This rule deletes tasks — check the confirmation box before applying.";
            return;
        }

        try
        {
            var count = await ruleService.ApplyRuleAsync(row.Id);
            StatusMessage = $"Applied \"{row.Name}\" to {count} task(s).";
            ConfirmDestructiveAction = false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply saved bulk edit rule {RuleId}", row.Id);
            ErrorMessage = "Couldn't apply the rule.";
        }
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(Guid ruleId)
    {
        try
        {
            await ruleService.DeleteRuleAsync(ruleId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete bulk edit rule {RuleId}", ruleId);
        }
    }
}
