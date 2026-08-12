using Avalonia.Headless;
using Avalonia.Media.Imaging;
using DeskTodo.App.ViewModels;
using DeskTodo.App.Views;
using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DeskTodo.Tests.Views;

/// <summary>Renders <see cref="LockScreenWindow"/> through Avalonia's headless platform (see <see cref="WidgetWindowRenderTests"/> for why).</summary>
[Collection(nameof(HeadlessCollection))]
public class LockScreenWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task LockScreenWindow_RendersWithoutError_AndShowsAnErrorMessageAfterAFailedUnlock()
    {
        await fixture.Session.Dispatch(async () =>
        {
            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { PinLockEnabled = true, PinSalt = "salt", PinHash = "hash" });
            var viewModel = new LockScreenViewModel(settingsService.Object, NullLogger<LockScreenViewModel>.Instance);

            var window = new LockScreenWindow { DataContext = viewModel };
            window.Show();

            viewModel.EnteredPin = "0000";
            await viewModel.UnlockCommand.ExecuteAsync(null);

            Assert.NotEqual(string.Empty, viewModel.ErrorMessage);

            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
                frame.Save(Path.Combine(screenshotDir, "lock-screen-window.png"), PngBitmapEncoderOptions.Default);
            }

            window.Close();
            return true;
        }, CancellationToken.None);
    }
}
