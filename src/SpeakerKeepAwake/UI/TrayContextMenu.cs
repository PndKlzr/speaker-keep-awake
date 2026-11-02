using System.Drawing.Drawing2D;

namespace SpeakerKeepAwake.UI;

public sealed class TrayContextMenu : ContextMenuStrip
{
    private readonly ThemePalette _theme;

    public TrayContextMenu(ThemePalette theme, bool protectionEnabled)
    {
        _theme = theme;
        AutoSize = true;
        MinimumSize = new Size(198, 0);
        Padding = new Padding(4);
        BackColor = theme.CardBackground;
        ForeColor = theme.PrimaryText;
        Font = new Font(SystemFonts.MenuFont!, FontStyle.Regular);
        ShowCheckMargin = false;
        ShowImageMargin = true;
        ImageScalingSize = new Size(16, 16);
        Renderer = new ToolStripProfessionalRenderer(new FluentMenuColorTable(theme)) { RoundedEdges = true };

        OpenItem = CreateItem("Abrir configurações", MenuGlyph.Open(theme));
        ProtectionItem = CreateItem(string.Empty, MenuGlyph.Protection(theme, protectionEnabled));
        ExitItem = CreateItem("Sair", MenuGlyph.Exit(theme));

        Items.Add(OpenItem);
        Items.Add(ProtectionItem);
        Items.Add(CreateSeparator());
        Items.Add(ExitItem);
        SetProtectionEnabled(protectionEnabled);
    }

    public ToolStripMenuItem OpenItem { get; }
    public ToolStripMenuItem ProtectionItem { get; }
    public ToolStripMenuItem ExitItem { get; }

    public void SetProtectionEnabled(bool enabled)
    {
        ProtectionItem.Text = enabled ? "Proteção ligada" : "Proteção desligada";
        ProtectionItem.Image?.Dispose();
        ProtectionItem.Image = MenuGlyph.Protection(_theme, enabled);
    }

    private ToolStripMenuItem CreateItem(string text, Image image) => new(text, image)
    {
        AutoSize = false,
        Size = new Size(188, 30),
        Padding = new Padding(4, 0, 4, 0),
        ForeColor = _theme.PrimaryText,
        ImageScaling = ToolStripItemImageScaling.None,
        ImageAlign = ContentAlignment.MiddleCenter,
        TextAlign = ContentAlignment.MiddleLeft,
        TextImageRelation = TextImageRelation.ImageBeforeText
    };

    private ToolStripSeparator CreateSeparator() => new()
    {
        Margin = new Padding(6, 2, 6, 2),
        ForeColor = _theme.Border
    };

    private sealed class FluentMenuColorTable(ThemePalette theme) : ProfessionalColorTable
    {
        private readonly Color _surface = theme.CardBackground;
        private readonly Color _hover = theme.IsDark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(232, 240, 248);

        public override Color ToolStripDropDownBackground => _surface;
        public override Color ImageMarginGradientBegin => _surface;
        public override Color ImageMarginGradientMiddle => _surface;
        public override Color ImageMarginGradientEnd => _surface;
        public override Color MenuItemSelected => _hover;
        public override Color MenuItemBorder => _hover;
        public override Color MenuBorder => theme.Border;
        public override Color SeparatorDark => theme.Border;
        public override Color SeparatorLight => theme.Border;
    }

    private static class MenuGlyph
    {
        public static Bitmap Open(ThemePalette theme) => Draw(graphics =>
        {
            using var pen = Stroke(theme.PrimaryText);
            graphics.DrawRectangle(pen, 4.5f, 5.5f, 11f, 9f);
            graphics.DrawLine(pen, 7f, 3.5f, 17f, 3.5f);
            graphics.DrawLine(pen, 17f, 3.5f, 17f, 12f);
        });

        public static Bitmap Protection(ThemePalette theme, bool enabled) => Draw(graphics =>
        {
            var color = enabled
                ? (theme.IsDark ? Color.FromArgb(108, 203, 95) : Color.FromArgb(16, 124, 65))
                : theme.SecondaryText;
            using var pen = Stroke(color, 1.8f);
            using var path = new GraphicsPath();
            path.AddLines([new PointF(10f, 2.5f), new PointF(16.5f, 5f), new PointF(15.5f, 12.5f), new PointF(10f, 17f), new PointF(4.5f, 12.5f), new PointF(3.5f, 5f)]);
            path.CloseFigure();
            graphics.DrawPath(pen, path);
            if (enabled)
            {
                graphics.DrawLines(pen, [new PointF(6.5f, 9.8f), new PointF(9f, 12.2f), new PointF(13.8f, 7.2f)]);
            }
            else
            {
                graphics.DrawLine(pen, 7f, 10f, 13f, 10f);
            }
        });

        public static Bitmap Exit(ThemePalette theme) => Draw(graphics =>
        {
            using var pen = Stroke(theme.PrimaryText);
            graphics.DrawArc(pen, 4f, 4f, 12f, 13f, -45, 270);
            graphics.DrawLine(pen, 10f, 2.5f, 10f, 10f);
        });

        private static Bitmap Draw(Action<Graphics> draw)
        {
            var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.ScaleTransform(0.8f, 0.8f);
            draw(graphics);
            return bitmap;
        }

        private static Pen Stroke(Color color, float width = 1.6f) => new(color, width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
    }
}
