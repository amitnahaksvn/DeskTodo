using Avalonia.Input;
using DeskTodo.App.ViewModels;

namespace DeskTodo.Tests.ViewModels;

public class KeyboardShortcutDefinitionTests
{
    [Fact]
    public void TryParseGesture_ResolvesModToThePlatformModifier()
    {
        var gesture = KeyboardShortcutDefinition.TryParseGesture("Mod+K", KeyModifiers.Control);

        Assert.NotNull(gesture);
        Assert.Equal(Key.K, gesture!.Key);
        Assert.Equal(KeyModifiers.Control, gesture.KeyModifiers);
    }

    [Fact]
    public void TryParseGesture_WithShift_IncludesBothModifiers()
    {
        var gesture = KeyboardShortcutDefinition.TryParseGesture("Mod+Shift+Z", KeyModifiers.Meta);

        Assert.NotNull(gesture);
        Assert.Equal(Key.Z, gesture!.Key);
        Assert.Equal(KeyModifiers.Meta | KeyModifiers.Shift, gesture.KeyModifiers);
    }

    [Fact]
    public void TryParseGesture_WithAnUnrecognizedToken_ReturnsNull()
    {
        var gesture = KeyboardShortcutDefinition.TryParseGesture("Mod+NotAKey", KeyModifiers.Control);

        Assert.Null(gesture);
    }

    [Fact]
    public void TryParseGesture_WithNoKeyToken_ReturnsNull()
    {
        var gesture = KeyboardShortcutDefinition.TryParseGesture("Mod+Shift", KeyModifiers.Control);

        Assert.Null(gesture);
    }

    [Fact]
    public void TryFormatCombo_RequiresThePlatformModifier()
    {
        var combo = KeyboardShortcutDefinition.TryFormatCombo(Key.K, KeyModifiers.Shift, KeyModifiers.Control);

        Assert.Null(combo);
    }

    [Fact]
    public void TryFormatCombo_WithJustTheModifier_FormatsWithoutShift()
    {
        var combo = KeyboardShortcutDefinition.TryFormatCombo(Key.K, KeyModifiers.Control, KeyModifiers.Control);

        Assert.Equal("Mod+K", combo);
    }

    [Fact]
    public void TryFormatCombo_WithModifierAndShift_IncludesShift()
    {
        var combo = KeyboardShortcutDefinition.TryFormatCombo(Key.Z, KeyModifiers.Control | KeyModifiers.Shift, KeyModifiers.Control);

        Assert.Equal("Mod+Shift+Z", combo);
    }

    [Fact]
    public void FormatThenParse_RoundTrips()
    {
        var combo = KeyboardShortcutDefinition.TryFormatCombo(Key.OemComma, KeyModifiers.Meta | KeyModifiers.Shift, KeyModifiers.Meta);
        var gesture = KeyboardShortcutDefinition.TryParseGesture(combo!, KeyModifiers.Meta);

        Assert.Equal(Key.OemComma, gesture!.Key);
        Assert.Equal(KeyModifiers.Meta | KeyModifiers.Shift, gesture.KeyModifiers);
    }

    [Fact]
    public void All_HasNoDuplicateCommandIds()
    {
        var ids = KeyboardShortcutDefinition.All.Select(d => d.CommandId).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void All_EveryDefaultComboParsesSuccessfully()
    {
        foreach (var definition in KeyboardShortcutDefinition.All)
        {
            Assert.NotNull(KeyboardShortcutDefinition.TryParseGesture(definition.DefaultCombo, KeyModifiers.Control));
        }
    }
}
