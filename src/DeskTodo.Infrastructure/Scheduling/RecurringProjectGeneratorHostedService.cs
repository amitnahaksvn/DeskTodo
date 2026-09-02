using DeskTodo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Infrastructure.Scheduling;

/// <summary>
/// Feature 87's scheduler — a background service (same "started/stopped by the generic
/// <see cref="IHost"/>" and "scope-per-unit-of-work" pattern as <see cref="Api.LocalApiServer"/>
/// and <c>WebhookDispatcher</c>) that periodically calls
/// <see cref="IRecurringProjectScheduleService.GenerateDueProjectsAsync"/> so due
/// <see cref="Domain.Entities.RecurringProjectSchedule"/>s materialize their next project without
/// the user needing to open any particular window. Runs once immediately on startup (covers
/// occurrences that came due while the app was closed) and then once an hour — frequent enough
/// that a due schedule's project appears the same day, without meaningfully more overhead than a
/// desktop app already has running in the background.
/// </summary>
public sealed class RecurringProjectGeneratorHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RecurringProjectGeneratorHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scheduleService = scope.ServiceProvider.GetRequiredService<IRecurringProjectScheduleService>();
                var generated = await scheduleService.GenerateDueProjectsAsync(DateOnly.FromDateTime(DateTime.Today), stoppingToken);

                if (generated.Count > 0)
                {
                    logger.LogInformation("Generated {Count} project(s) from due recurring schedules", generated.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to generate due recurring projects");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
