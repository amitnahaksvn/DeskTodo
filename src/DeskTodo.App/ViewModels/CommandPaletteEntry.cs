using System.Windows.Input;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// One entry in the Command Palette (Phase 28) — a label plus the existing
/// <see cref="WidgetViewModel"/> <c>[RelayCommand]</c> it runs. Deliberately wraps the
/// already-defined commands rather than inventing a second command layer: the palette is a
/// second way to invoke actions that already exist, not a new feature surface of its own.
/// </summary>
public sealed record CommandPaletteEntry(string Label, ICommand Command);
