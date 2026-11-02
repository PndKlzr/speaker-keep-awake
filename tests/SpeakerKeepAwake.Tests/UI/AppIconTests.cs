using SpeakerKeepAwake.UI;

namespace SpeakerKeepAwake.Tests.UI;

public sealed class AppIconTests
{
    [Fact]
    public void CreatesCrispIconsForTrayAndWindow()
    {
        using var trayIcon = AppIcon.Create(16);
        using var windowIcon = AppIcon.Create(32);

        Assert.Equal(16, trayIcon.Width);
        Assert.Equal(16, trayIcon.Height);
        Assert.Equal(32, windowIcon.Width);
        Assert.Equal(32, windowIcon.Height);

        using var trayBitmap = trayIcon.ToBitmap();
        using var windowBitmap = windowIcon.ToBitmap();
        Assert.Equal(0, trayBitmap.GetPixel(0, 0).A);
        Assert.Equal(0, windowBitmap.GetPixel(0, 0).A);
        Assert.True(windowBitmap.GetPixel(16, 16).A > 0);
    }
}
