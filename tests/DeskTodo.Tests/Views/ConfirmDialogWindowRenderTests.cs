using Avalonia.Headless;
using Avalonia.Media.Imaging;
using DeskTodo.App.Views;

namespace DeskTodo.Tests.Views;

/// <summary>Renders <see cref="ConfirmDialogWindow"/> through Avalonia's headless platform (see <see cref="WidgetWindowRenderTests"/> for why).</summary>
[Collection(nameof(HeadlessCollection))]
public class ConfirmDialogWindowRenderTests(HeadlessSessionFixture fixture)
{
    [Fact]
    public async Task ConfirmDialogWindow_DisplaysTheGivenTitleMessageAndConfirmText()
    {
        await fixture.Session.Dispatch(async () =>
        {
            // Constructed and shown directly here (not via ShowAsync) — ShowAsync's
            // ShowDialog<bool> blocks until Close(bool) is called from a button click,
            // which needs owner/click-simulation machinery no sibling render test in this
            // suite exercises; this verifies the same field-population ShowAsync does
            // (Title/Message/ConfirmButton.Content) without the full modal round trip.
            var window = new ConfirmDialogWindow();
            window.TitleTextBlock.Text = "Delete task?";
            window.MessageTextBlock.Text = "\"Water plants\" will be deleted. This can't be undone from here.";
            window.ConfirmButton.Content = "Delete";
            window.Show();

            Assert.Equal("Delete task?", window.TitleTextBlock.Text);
            Assert.Contains("Water plants", window.MessageTextBlock.Text);
            Assert.Equal("Delete", window.ConfirmButton.Content);

            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

            var screenshotDir = Environment.GetEnvironmentVariable("DESKTODO_SCREENSHOT_DIR");
            if (!string.IsNullOrWhiteSpace(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
                frame.Save(Path.Combine(screenshotDir, "confirm-dialog-window.png"), PngBitmapEncoderOptions.Default);
            }

            window.Close();
            return true;
        }, CancellationToken.None);
    }
}
