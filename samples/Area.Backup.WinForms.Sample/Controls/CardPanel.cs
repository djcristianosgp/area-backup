using System.Drawing.Drawing2D;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Controls;

/// <summary>
/// Modern Web SaaS-styled card container panel with rounded borders.
/// </summary>
public class CardPanel : Panel
{
    private string? _title;
    private string? _subtitle;
    private int _borderRadius = 10;
    private Color _cardBg = ModernTheme.CardBg;
    private Color _borderColor = ModernTheme.CardBorder;

    public string? Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    public string? Subtitle
    {
        get => _subtitle;
        set { _subtitle = value; Invalidate(); }
    }

    public int BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Invalidate(); }
    }

    public Color CardBackground
    {
        get => _cardBg;
        set { _cardBg = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    public CardPanel()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Padding = new Padding(16);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        ModernTheme.DrawCard(g, rect, _borderRadius, _cardBg, _borderColor);

        int topOffset = 14;
        if (!string.IsNullOrEmpty(_title))
        {
            using var titleBrush = new SolidBrush(ModernTheme.TextPrimary);
            g.DrawString(_title, ModernTheme.SectionFont, titleBrush, new PointF(16, topOffset));
            topOffset += 20;

            if (!string.IsNullOrEmpty(_subtitle))
            {
                using var subBrush = new SolidBrush(ModernTheme.TextMuted);
                g.DrawString(_subtitle, ModernTheme.SmallFont, subBrush, new PointF(16, topOffset));
                topOffset += 16;
            }

            // Optional separator line
            using var linePen = new Pen(ModernTheme.SectionHeader, 1f);
            g.DrawLine(linePen, 16, topOffset, Width - 16, topOffset);
        }
    }
}
