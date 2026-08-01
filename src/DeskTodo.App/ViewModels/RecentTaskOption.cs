using CommunityToolkit.Mvvm.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// A recently-opened task, as shown in the widget's "Recently viewed" chip row —
/// session-only, never persisted (see <see cref="WidgetViewModel.RecentlyViewed"/>'s
/// remarks). Owns its own "open" command (mirrors <see cref="TagChip"/>/<see cref="BlockerChip"/>)
/// rather than the chip's <c>Button</c> reaching for an ambient parent-DataContext binding
/// to invoke a command on <see cref="WidgetViewModel"/> — simpler and more robust.
/// </summary>
public sealed class RecentTaskOption
{
    public RecentTaskOption(Guid id, string title, Action<RecentTaskOption> requestOpen)
    {
        Id = id;
        Title = title;
        OpenCommand = new RelayCommand(() => requestOpen(this));
    }

    public Guid Id { get; }

    public string Title { get; }

    public IRelayCommand OpenCommand { get; }
}
