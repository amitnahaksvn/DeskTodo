namespace DeskTodo.App.ViewModels;

/// <summary>One shortcut as shown in <see cref="KeyboardShortcutsViewModel"/>'s list, with its effective (override-or-default) combo already resolved.</summary>
public sealed record KeyboardShortcutOption(string CommandId, string DisplayName, string Scope, string EffectiveCombo, bool IsCustomized, bool HasConflict);
