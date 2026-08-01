using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A file attached to the task being edited, as shown/opened/removed in the editor's
/// Attachments section. "Open" can't be handled here — launching a file with the OS
/// default handler needs a <c>TopLevel</c> (a live <c>Visual</c>), which a ViewModel
/// shouldn't hold — so it bubbles up via <see cref="_requestOpen"/> the same way
/// <c>WidgetViewModel.TaskEditRequested</c> bubbles view-construction requests up to
/// its Window instead of a ViewModel doing it directly.
/// </summary>
public sealed partial class AttachmentRowViewModel : ObservableObject
{
    private readonly IAttachmentService _attachmentService;
    private readonly ILogger _logger;
    private readonly Action<AttachmentRowViewModel> _requestRemove;
    private readonly Action<AttachmentRowViewModel> _requestOpen;

    public AttachmentRowViewModel(Attachment attachment, string absolutePath, IAttachmentService attachmentService, ILogger logger, Action<AttachmentRowViewModel> requestRemove, Action<AttachmentRowViewModel> requestOpen)
    {
        Id = attachment.Id;
        FileName = attachment.FileName;
        AbsolutePath = absolutePath;
        SizeDisplay = FormatSize(attachment.FileSizeBytes);
        _attachmentService = attachmentService;
        _logger = logger;
        _requestRemove = requestRemove;
        _requestOpen = requestOpen;
    }

    public Guid Id { get; }

    public string FileName { get; }

    public string AbsolutePath { get; }

    public string SizeDisplay { get; }

    [RelayCommand]
    private void Open() => _requestOpen(this);

    [RelayCommand]
    private async Task RemoveAsync()
    {
        try
        {
            await _attachmentService.RemoveAttachmentAsync(Id);
            _requestRemove(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove attachment {AttachmentId}", Id);
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };
}
