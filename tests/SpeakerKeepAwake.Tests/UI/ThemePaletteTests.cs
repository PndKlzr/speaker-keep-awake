using SpeakerKeepAwake.UI;

namespace SpeakerKeepAwake.Tests.UI;

public sealed class ThemePaletteTests
{
    [Fact]
    public void DarkThemeUsesDarkSurfacesAndLightText()
    {
        var theme = ThemePalette.Create(isDark: true);

        Assert.True(theme.WindowBackground.GetBrightness() < 0.2f);
        Assert.True(theme.CardBackground.GetBrightness() < 0.3f);
        Assert.True(theme.PrimaryText.GetBrightness() > 0.8f);
    }

    [Fact]
    public void LightThemeUsesLightSurfacesAndDarkText()
    {
        var theme = ThemePalette.Create(isDark: false);

        Assert.True(theme.WindowBackground.GetBrightness() > 0.8f);
        Assert.True(theme.CardBackground.GetBrightness() > 0.9f);
        Assert.True(theme.PrimaryText.GetBrightness() < 0.2f);
    }
}
