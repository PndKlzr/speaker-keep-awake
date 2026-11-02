using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SpeakerKeepAwake.UI;

public sealed class ToggleSwitch : CheckBox
{
    public ToggleSwitch()
    {
        AutoSize = false;
        Size = new Size(46, 24);
        Cursor = Cursors.Hand;
        Text = string.Empty;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CheckedColor { get; set; } = Color.FromArgb(0, 103, 192);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color UncheckedColor { get; set; } = Color.FromArgb(145, 145, 145);

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        eventArgs.Graphics.Clear(BackColor);
        var track = new Rectangle(0, 1, Width - 1, Height - 3);
        using var trackPath = RoundedRectangle(track, track.Height / 2);
        using var trackBrush = new SolidBrush(Checked ? CheckedColor : UncheckedColor);
        eventArgs.Graphics.FillPath(trackBrush, trackPath);

        var diameter = track.Height - 6;
        var x = Checked ? track.Right - diameter - 3 : track.Left + 3;
        using var knobBrush = new SolidBrush(Color.White);
        eventArgs.Graphics.FillEllipse(knobBrush, x, track.Top + 3, diameter, diameter);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(eventArgs.Graphics, ClientRectangle);
        }
    }

    protected override void OnCheckedChanged(EventArgs eventArgs)
    {
        base.OnCheckedChanged(eventArgs);
        Invalidate();
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 90, 180);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 180);
        path.CloseFigure();
        return path;
    }
}
