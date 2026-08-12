using Avalonia.Headless;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;

namespace DeskTodo.Tests.Views;

/// <summary>Renders <see cref="CommandPaletteWindow"/> through Avalonia's headless platform (see <see cref="WidgetWindowRenderTests"/> for why).</summary>
[Collection(nameof(HeadlessCollection))]
public class CommandPaletteWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task CommandPaletteWindow_LoadsAndDisplaysEntries()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var viewModel = new CommandPaletteViewModel();
            viewModel.SetEntries([
                new CommandPaletteEntry("Open Grid View", new RelayCommand(() => { })),
                new CommandPaletteEntry("Open Settings", new RelayCommand(() => { })),
            ]);

            var window = new CommandPaletteWindow { DataContext = viewModel };
            window.Show();

            Assert.Equal(2, viewModel.VisibleEntries.Count);

            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
                frame.Save(Path.Combine(screenshotDir, "command-palette-window.png"), PngBitmapEncoderOptions.Default);
            }

            window.Close();
            return true;
        }, CancellationToken.None);
    }
}
