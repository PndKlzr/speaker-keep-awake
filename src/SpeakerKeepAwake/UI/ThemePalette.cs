using Microsoft.Win32;

namespace SpeakerKeepAwake.UI;

public sealed record ThemePalette(
    bool IsDark,
    Color WindowBackground,
    Color CardBackground,
    Color FieldBackground,
    Color PrimaryText,
    Color SecondaryText,
    Color Border,
    Color Accent,
    Color ToggleOff)
{
    public static ThemePalette Create(bool isDark) => isDark
        ? new(
            true,
            Color.FromArgb(31, 31, 31),
            Color.FromArgb(43, 43, 43),
            Color.FromArgb(51, 51, 51),
            Color.FromArgb(245, 245, 245),
            Color.FromArgb(200, 200, 200),
            Color.FromArgb(67, 67, 67),
            Color.FromArgb(96, 205, 255),
            Color.FromArgb(112, 112, 112))
        : new(
            false,
            Color.FromArgb(243, 243, 243),
            Color.White,
            Color.White,
            Color.FromArgb(32, 32, 32),
            Color.FromArgb(96, 96, 96),
            Color.FromArgb(225, 225, 225),
            Color.FromArgb(0, 103, 192),
            Color.FromArgb(145, 145, 145));

    public static ThemePalette FromSystem()
    {
        const string key = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        var value = Registry.GetValue(key, "AppsUseLightTheme", 1);
        return Create(value is int intValue && intValue == 0);
    }
}
