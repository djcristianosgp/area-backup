using System.Drawing.Drawing2D;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Controls;

public enum ModernButtonStyle
{
    Primary,
    Secondary,
    Outline,
    Success,
    Danger
}

/// <summary>
/// Web SaaS-styled button with rounded corners, subtle transitions and clean typography.
/// </summary>
public class ModernButton : Button
{
    private ModernButtonStyle _style = ModernButtonStyle.Primary;
    private bool _isHovered;
    private bool _isPressed;
    private int _borderRadius = 8;

    public ModernButtonStyle ButtonStyle
    {
        get => _style;
        set { _style = value; Invalidate(); }
    }

    public int BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Invalidate(); }
    }

    public ModernButton()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = ModernTheme.BodyBold;
        Cursor = Cursors.Hand;
        Size = new Size(130, 36);
        UseVisualStyleBackColor = false;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ModernTheme.CreateRoundedRectangle(rect, _borderRadius);

        var (bg, border, text) = GetColors();

        // Fill background
        using (var brush = new SolidBrush(bg))
        {
            g.FillPath(brush, path);
        }

        // Draw border if defined
        if (border != Color.Transparent)
        {
            using var pen = new Pen(border, 1f);
            g.DrawPath(pen, path);
        }

        // Draw text
        TextRenderer.DrawText(g, Text, Font, rect, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private (Color bg, Color border, Color text) GetColors()
    {
        if (!Enabled)
        {
            return (Color.FromArgb(241, 245, 249), Color.FromArgb(226, 232, 240), Color.FromArgb(148, 163, 184));
        }

        return _style switch
        {
            ModernButtonStyle.Primary => (
                _isPressed ? Color.FromArgb(30, 64, 175) : (_isHovered ? ModernTheme.PrimaryHover : ModernTheme.Primary),
                Color.Transparent,
                Color.White
            ),
            ModernButtonStyle.Success => (
                _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? Color.FromArgb(5, 150, 105) : ModernTheme.Success),
                Color.Transparent,
                Color.White
            ),
            ModernButtonStyle.Danger => (
                _isPressed ? Color.FromArgb(185, 28, 28) : (_isHovered ? ModernTheme.DangerHover : ModernTheme.Danger),
                Color.Transparent,
                Color.White
            ),
            ModernButtonStyle.Secondary => (
                _isPressed ? Color.FromArgb(226, 232, 240) : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252)),
                ModernTheme.CardBorder,
                ModernTheme.TextPrimary
            ),
            ModernButtonStyle.Outline => (
                _isPressed ? ModernTheme.PrimaryLight : (_isHovered ? Color.FromArgb(240, 247, 255) : Color.Transparent),
                ModernTheme.Primary,
                ModernTheme.Primary
            ),
            _ => (ModernTheme.CardBg, ModernTheme.CardBorder, ModernTheme.TextPrimary)
        };
    }
}
