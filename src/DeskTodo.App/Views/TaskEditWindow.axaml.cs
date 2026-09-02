using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DeskTodo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeskTodo.App.Views;

public partial class TaskEditWindow : Window
{
    public TaskEditWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is TaskEditViewModel viewModel)
        {
            viewModel.AttachmentOpenRequested += OnAttachmentOpenRequested;
            viewModel.StartTimerRequested += OnStartTimerRequested;
            viewModel.HistoryRequested += OnHistoryRequested;
            viewModel.VersionsRequested += OnVersionsRequested;
            viewModel.RelationshipsGraphRequested += OnRelationshipsGraphRequested;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            RefreshNotesPreview(viewModel);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is TaskEditViewModel viewModel)
        {
            viewModel.AttachmentOpenRequested -= OnAttachmentOpenRequested;
            viewModel.StartTimerRequested -= OnStartTimerRequested;
            viewModel.HistoryRequested -= OnHistoryRequested;
            viewModel.VersionsRequested -= OnVersionsRequested;
            viewModel.RelationshipsGraphRequested -= OnRelationshipsGraphRequested;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        base.OnClosed(e);
    }

    /// <summary>Phase 23 — preselects this task in the DI-singleton <see cref="FocusTimerViewModel"/> before showing/activating the shared Focus Timer window (see <see cref="FocusTimerWindow.ShowOrActivate"/>).</summary>
    private void OnStartTimerRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not TaskEditViewModel viewModel)
        {
            return;
        }

        var focusTimerViewModel = App.Services.GetRequiredService<FocusTimerViewModel>();
        focusTimerViewModel.PreselectTask(viewModel.TaskId, viewModel.Title);
        FocusTimerWindow.ShowOrActivate(focusTimerViewModel);
    }

    private async void OnHistoryRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not TaskEditViewModel viewModel)
        {
            return;
        }

        var historyViewModel = App.Services.GetRequiredService<TaskHistoryViewModel>();
        var historyWindow = new TaskHistoryWindow { DataContext = historyViewModel };
        await historyViewModel.LoadAsync(viewModel.TaskId, viewModel.Title);
        await historyWindow.ShowDialog(this);
    }

    /// <summary>Feature 44, Roadmap-39-100.md — same DI-resolved-child-window split as <see cref="OnHistoryRequested"/>. Reloads this editor's own fields afterward, since a restore may have just changed them.</summary>
    private async void OnVersionsRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not TaskEditViewModel viewModel)
        {
            return;
        }

        var versionViewModel = App.Services.GetRequiredService<TaskVersionViewModel>();
        var versionWindow = new TaskVersionWindow { DataContext = versionViewModel };
        await versionViewModel.LoadAsync(viewModel.TaskId, viewModel.Title);
        await versionWindow.ShowDialog(this);
        await viewModel.LoadAsync(viewModel.TaskId);
    }

    /// <summary>Feature 48, Roadmap-39-100.md — same DI-resolved-child-window split as <see cref="OnHistoryRequested"/>.</summary>
    private async void OnRelationshipsGraphRequested(object? sender, EventArgs e)
    {
        if (App.Services is null || DataContext is not TaskEditViewModel viewModel)
        {
            return;
        }

        var graphViewModel = App.Services.GetRequiredService<TaskGraphViewModel>();
        var graphWindow = new TaskGraphWindow { DataContext = graphViewModel };
        await graphViewModel.LoadAsync(viewModel.TaskId);
        await graphWindow.ShowDialog(this);
    }

    private void OnNewChecklistItemKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TaskEditViewModel viewModel)
        {
            viewModel.AddChecklistItemCommand.Execute(null);
        }
    }

    private void OnNewTagKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TaskEditViewModel viewModel)
        {
            viewModel.AddTagCommand.Execute(null);
        }
    }

    private void OnNewSubtaskKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TaskEditViewModel viewModel)
        {
            viewModel.AddSubtaskCommand.Execute(null);
        }
    }

    /// <summary>
    /// Opens the OS file picker and attaches each chosen file. The picker/local-path
    /// resolution needs a live <c>TopLevel</c>/<c>IStorageProvider</c>, which only this
    /// code-behind (not <see cref="TaskEditViewModel"/>) has — see
    /// <see cref="TaskEditViewModel.AddAttachmentAsync"/>'s doc comment.
    /// </summary>
    private async void OnAttachFileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TaskEditViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Attach a file",
            AllowMultiple = true,
        });

        foreach (var file in files)
        {
            var localPath = file.TryGetLocalPath();
            if (localPath is not null)
            {
                await viewModel.AddAttachmentAsync(localPath);
            }
        }
    }

    private async void OnAttachmentOpenRequested(object? sender, string absolutePath)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.TryGetFileFromPathAsync(new Uri(absolutePath));
        if (file is not null)
        {
            await topLevel.Launcher.LaunchFileAsync(file);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TaskEditViewModel viewModel)
        {
            return;
        }

        if (e.PropertyName is nameof(TaskEditViewModel.IsNotesPreview) or nameof(TaskEditViewModel.Notes))
        {
            RefreshNotesPreview(viewModel);
        }
    }

    /// <summary>
    /// Hand-rolled minimal Markdown (bold/italic/bullet lines) — Avalonia has no bundled
    /// Markdown renderer, and pulling in a third-party one just for **bold**/*italic*/"-
    /// item" wasn't worth the added dependency. <see cref="TextBlock.Inlines"/> can't be
    /// data-bound to a converter's output the normal way (it's a mutable collection
    /// property assigned once, not driven by a value converter), so this rebuilds it by
    /// hand whenever <see cref="TaskEditViewModel.Notes"/> or the preview toggle changes.
    /// </summary>
    private void RefreshNotesPreview(TaskEditViewModel viewModel)
    {
        if (!viewModel.IsNotesPreview)
        {
            return;
        }

        var inlines = new InlineCollection();
        var lines = (viewModel.Notes ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                inlines.Add(new Run("• " + line[2..]));
            }
            else
            {
                AppendFormattedRuns(inlines, line);
            }

            if (lineIndex < lines.Length - 1)
            {
                inlines.Add(new LineBreak());
            }
        }

        NotesPreviewBlock.Inlines = inlines;
    }

    /// <summary>Alternates plain segments with **bold**/*italic* spans — a small hand-rolled scanner, not a general Markdown parser.</summary>
    private static void AppendFormattedRuns(InlineCollection inlines, string line)
    {
        var position = 0;
        while (position < line.Length)
        {
            var boldStart = line.IndexOf("**", position, StringComparison.Ordinal);
            var italicStart = line.IndexOf('*', position);

            if (boldStart >= 0 && boldStart == italicStart)
            {
                var boldEnd = line.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
                if (boldEnd < 0)
                {
                    break;
                }

                if (boldStart > position)
                {
                    inlines.Add(new Run(line[position..boldStart]));
                }

                inlines.Add(new Bold { Inlines = new InlineCollection { new Run(line[(boldStart + 2)..boldEnd]) } });
                position = boldEnd + 2;
                continue;
            }

            if (italicStart >= 0)
            {
                var italicEnd = line.IndexOf('*', italicStart + 1);
                if (italicEnd < 0)
                {
                    break;
                }

                if (italicStart > position)
                {
                    inlines.Add(new Run(line[position..italicStart]));
                }

                inlines.Add(new Italic { Inlines = new InlineCollection { new Run(line[(italicStart + 1)..italicEnd]) } });
                position = italicEnd + 1;
                continue;
            }

            break;
        }

        if (position < line.Length)
        {
            inlines.Add(new Run(line[position..]));
        }
    }
}
