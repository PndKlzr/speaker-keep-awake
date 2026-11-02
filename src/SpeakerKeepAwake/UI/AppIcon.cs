using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SpeakerKeepAwake.UI;

public static class AppIcon
{
    public static Icon Create() => Create(32);

    public static Icon Create(int size)
    {
        if (size is < 16 or > 256) throw new ArgumentOutOfRangeException(nameof(size));

        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        var scale = size / 32f;
        var tile = new RectangleF(1.5f * scale, 1.5f * scale, 29f * scale, 29f * scale);
        using var tilePath = RoundedRectangle(tile, 7f * scale);
        using var background = new LinearGradientBrush(
            tile,
            Color.FromArgb(15, 108, 189),
            Color.FromArgb(72, 184, 255),
            LinearGradientMode.ForwardDiagonal);
        graphics.FillPath(background, tilePath);
        using var highlight = new Pen(Color.FromArgb(90, 255, 255, 255), Math.Max(1f, scale));
        graphics.DrawPath(highlight, tilePath);

        PointF[] speaker =
        [
            new(6.5f * scale, 13f * scale), new(11.5f * scale, 13f * scale), new(17f * scale, 8.5f * scale),
            new(17f * scale, 23.5f * scale), new(11.5f * scale, 19f * scale), new(6.5f * scale, 19f * scale)
        ];
        using var foreground = new SolidBrush(Color.White);
        graphics.FillPolygon(foreground, speaker);

        using var wave = new Pen(Color.White, Math.Max(1.45f, 2.2f * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawArc(wave, 15f * scale, 10.5f * scale, 8f * scale, 11f * scale, -55, 110);
        if (size >= 24)
            graphics.DrawArc(wave, 15.5f * scale, 6.5f * scale, 14f * scale, 19f * scale, -55, 110);

        var handle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var diameter = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
