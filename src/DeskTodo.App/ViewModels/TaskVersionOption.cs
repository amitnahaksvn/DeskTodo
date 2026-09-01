using DeskTodo.Domain.Entities;

namespace DeskTodo.App.ViewModels;

/// <summary>One <see cref="TaskVersion"/> row as shown in <see cref="TaskVersionViewModel"/>'s list — Feature 44 (Roadmap-39-100.md).</summary>
public sealed record TaskVersionOption(Guid Id, string Title, string CapturedAtDisplay, int VersionNumber)
{
    public static TaskVersionOption FromEntity(TaskVersion version) =>
        new(version.Id, version.Title, version.CapturedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt"), version.VersionNumber);
}
