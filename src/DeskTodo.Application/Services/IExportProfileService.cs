using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;

namespace DeskTodo.Application.Services;

/// <summary>Roadmap-39-100.md Feature 91 — save and re-run a named export configuration.</summary>
public interface IExportProfileService
{
    Task<IReadOnlyList<ExportProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<ExportProfile> CreateProfileAsync(
        string name,
        ExportFormat format,
        Guid? projectId,
        ExportDateRange dateRange,
        CancellationToken cancellationToken = default);

    Task DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>Filters tasks per the profile's project/date-range configuration and writes them to <paramref name="destination"/> in the profile's format. Returns how many tasks were exported.</summary>
    Task<int> RunProfileAsync(Guid profileId, Stream destination, CancellationToken cancellationToken = default);
}
