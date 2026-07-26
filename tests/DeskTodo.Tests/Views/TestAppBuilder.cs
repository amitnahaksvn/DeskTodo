using Avalonia;
using Avalonia.Headless;

namespace DeskTodo.Tests.Views;

// Entry point for HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder)) (see
// HeadlessSessionFixture) — driven manually rather than via Avalonia.Headless.XUnit's
// [AvaloniaFact], since that package requires xunit.v3, which conflicts with the xunit v2
// packages the rest of this test project is built on.
public static class TestAppBuilder
{
    // Reuses the real App class (not a minimal stand-in) specifically so FluentTheme and
    // the rest of App.axaml's styling apply — otherwise standard controls like CheckBox
    // and ProgressBar would render with no template at all, defeating the point.
    //
    // Fully qualified rather than "using DeskTodo.App;" + bare "App": DeskTodo.Tests.Views
    // sits under the same "DeskTodo" root as DeskTodo.App itself, so C# resolves the
    // sibling namespace DeskTodo.App before the using-imported type gets a chance to —
    // the same class of collision documented for DeskTodo.App vs DeskTodo.Application
    // in docs/ARCHITECTURE.md, here between the DeskTodo.Tests.Views namespace and the
    // DeskTodo.App namespace itself.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<global::DeskTodo.App.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
