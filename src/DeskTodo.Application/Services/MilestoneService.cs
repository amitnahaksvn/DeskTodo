using DeskTodo.Application.Abstractions;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Exceptions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IMilestoneService"/>
public sealed class MilestoneService(IMilestoneRepository milestoneRepository) : IMilestoneService
{
    public Task<IReadOnlyList<Milestone>> GetMilestonesAsync(CancellationToken cancellationToken = default) =>
        milestoneRepository.GetAllAsync(cancellationToken);

    public async Task<Milestone> CreateMilestoneAsync(string title, string? description, DateOnly? targetDate, Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        var order = 0;
        if (projectId is { } id)
        {
            var existing = await milestoneRepository.GetAllAsync(cancellationToken);
            order = existing.Where(m => m.ProjectId == id).Select(m => m.Order).DefaultIfEmpty(-1).Max() + 1;
        }

        var milestone = new Milestone
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            TargetDate = targetDate,
            ProjectId = projectId,
            Order = order,
        };
        await milestoneRepository.AddAsync(milestone, cancellationToken);
        return milestone;
    }

    public async Task UpdateMilestoneAsync(Guid milestoneId, string title, string? description, DateOnly? targetDate, CancellationToken cancellationToken = default)
    {
        var milestone = await milestoneRepository.GetByIdAsync(milestoneId, cancellationToken) ?? throw new MilestoneNotFoundException(milestoneId);
        milestone.Title = title.Trim();
        milestone.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        milestone.TargetDate = targetDate;
        await milestoneRepository.UpdateAsync(milestone, cancellationToken);
    }

    public async Task SetCompletedAsync(Guid milestoneId, bool isCompleted, CancellationToken cancellationToken = default)
    {
        var milestone = await milestoneRepository.GetByIdAsync(milestoneId, cancellationToken) ?? throw new MilestoneNotFoundException(milestoneId);
        milestone.IsCompleted = isCompleted;
        await milestoneRepository.UpdateAsync(milestone, cancellationToken);
    }

    public Task DeleteMilestoneAsync(Guid milestoneId, CancellationToken cancellationToken = default) =>
        milestoneRepository.DeleteAsync(milestoneId, cancellationToken);
}
