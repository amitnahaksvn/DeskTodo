using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeskTodo.Infrastructure.Data;

/// <inheritdoc cref="IDataIntegrityService"/>
public sealed class DataIntegrityService(
    IDbContextFactory<DeskTodoDbContext> contextFactory,
    IOptions<AppStorageOptions> storageOptions,
    ILogger<DataIntegrityService> logger) : IDataIntegrityService
{
    private const string DanglingCategoryCategory = "Dangling category reference";
    private const string SelfParentedCategory = "Task is its own parent";
    private const string DanglingParentCategory = "Dangling parent-task reference";
    private const string MissingAttachmentFileCategory = "Missing attachment file";
    private const string NegativeMinutesCategory = "Negative time value";
    private const string SqliteCorruptionCategory = "Database file corruption";

    public async Task<IReadOnlyList<IntegrityIssue>> CheckAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<IntegrityIssue>();

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var integrityResult = await context.Database
            .SqlQueryRaw<string>("PRAGMA integrity_check")
            .ToListAsync(cancellationToken);
        if (integrityResult.Count != 1 || !string.Equals(integrityResult[0], "ok", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new IntegrityIssue(SqliteCorruptionCategory, $"SQLite reported: {string.Join("; ", integrityResult)}", IsAutoRepairable: false));
        }

        var categoryIds = await context.Categories.AsNoTracking().Select(c => c.Id).ToListAsync(cancellationToken);
        var tasks = await context.Tasks.AsNoTracking()
            .Select(t => new { t.Id, t.Title, t.CategoryId, t.ParentTaskId, t.EstimatedMinutes, t.ActualMinutes })
            .ToListAsync(cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToHashSet();

        foreach (var task in tasks)
        {
            if (task.CategoryId is { } categoryId && !categoryIds.Contains(categoryId))
            {
                issues.Add(new IntegrityIssue(DanglingCategoryCategory, $"Task \"{task.Title}\" references a category that no longer exists.", IsAutoRepairable: true));
            }

            if (task.ParentTaskId == task.Id)
            {
                issues.Add(new IntegrityIssue(SelfParentedCategory, $"Task \"{task.Title}\" lists itself as its own parent task.", IsAutoRepairable: true));
            }
            else if (task.ParentTaskId is { } parentId && !taskIds.Contains(parentId))
            {
                issues.Add(new IntegrityIssue(DanglingParentCategory, $"Task \"{task.Title}\" references a parent task that no longer exists.", IsAutoRepairable: true));
            }

            if (task.EstimatedMinutes is < 0)
            {
                issues.Add(new IntegrityIssue(NegativeMinutesCategory, $"Task \"{task.Title}\" has a negative estimated-minutes value.", IsAutoRepairable: true));
            }

            if (task.ActualMinutes is < 0)
            {
                issues.Add(new IntegrityIssue(NegativeMinutesCategory, $"Task \"{task.Title}\" has a negative actual-minutes value.", IsAutoRepairable: true));
            }
        }

        var attachments = await context.Attachments.AsNoTracking()
            .Select(a => new { a.Id, a.FileName, a.StoredRelativePath })
            .ToListAsync(cancellationToken);
        foreach (var attachment in attachments)
        {
            var fullPath = Path.Combine(storageOptions.Value.RootDirectory, attachment.StoredRelativePath);
            if (!File.Exists(fullPath))
            {
                issues.Add(new IntegrityIssue(MissingAttachmentFileCategory, $"Attachment \"{attachment.FileName}\" is recorded but its file is missing from disk.", IsAutoRepairable: true));
            }
        }

        logger.LogInformation("Data integrity check found {Count} issue(s)", issues.Count);
        return issues;
    }

    public async Task<int> RepairAsync(IReadOnlyList<IntegrityIssue> issues, CancellationToken cancellationToken = default)
    {
        var repairable = issues.Where(i => i.IsAutoRepairable).ToList();
        if (repairable.Count == 0)
        {
            return 0;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var categoryIds = await context.Categories.AsNoTracking().Select(c => c.Id).ToListAsync(cancellationToken);
        var tasks = await context.Tasks.ToListAsync(cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var fixedCount = 0;

        foreach (var task in tasks)
        {
            if (task.CategoryId is { } categoryId && !categoryIds.Contains(categoryId))
            {
                task.CategoryId = null;
                fixedCount++;
            }

            if (task.ParentTaskId == task.Id || (task.ParentTaskId is { } parentId && !taskIds.Contains(parentId)))
            {
                task.ParentTaskId = null;
                fixedCount++;
            }

            if (task.EstimatedMinutes is < 0)
            {
                task.EstimatedMinutes = 0;
                fixedCount++;
            }

            if (task.ActualMinutes is < 0)
            {
                task.ActualMinutes = 0;
                fixedCount++;
            }
        }

        var attachments = await context.Attachments.ToListAsync(cancellationToken);
        foreach (var attachment in attachments)
        {
            var fullPath = Path.Combine(storageOptions.Value.RootDirectory, attachment.StoredRelativePath);
            if (!File.Exists(fullPath))
            {
                context.Attachments.Remove(attachment);
                fixedCount++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Data integrity repair fixed {Count} issue(s)", fixedCount);
        return fixedCount;
    }
}
