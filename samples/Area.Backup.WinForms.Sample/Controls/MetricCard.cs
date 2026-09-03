using System.Drawing.Drawing2D;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Controls;

/// <summary>
/// Metric/KPI widget card displaying a key stat, title, and optional badge or status.
/// </summary>
public class MetricCard : UserControl
{
    private string _kpiValue = "0";
    private string _kpiLabel = "Métrica";
    private string _kpiSub = "";
    private string? _badgeText;
    private Color _badgeColor = ModernTheme.SuccessLight;
    private Color _badgeTextColor = ModernTheme.Success;
    private Color _valueColor = ModernTheme.TextPrimary;

    public string Value
    {
        get => _kpiValue;
        set { _kpiValue = value; Invalidate(); }
    }

    public string Title
    {
        get => _kpiLabel;
        set { _kpiLabel = value; Invalidate(); }
    }

    public string Subtitle
    {
        get => _kpiSub;
        set { _kpiSub = value; Invalidate(); }
    }

    public string? BadgeText
    {
        get => _badgeText;
        set { _badgeText = value; Invalidate(); }
    }

    public Color ValueColor
    {
        get => _valueColor;
        set { _valueColor = value; Invalidate(); }
    }

    public MetricCard()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Size = new Size(200, 95);
        MinimumSize = new Size(160, 90);
    }

    public void SetBadge(string text, Color bg, Color textColor)
    {
        _badgeText = text;
        _badgeColor = bg;
        _badgeTextColor = textColor;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        ModernTheme.DrawCard(g, rect, radius: 10, bg: ModernTheme.CardBg, border: ModernTheme.CardBorder);

        // Label / Title
        using (var labelBrush = new SolidBrush(ModernTheme.TextMuted))
        {
            g.DrawString(_kpiLabel.ToUpperInvariant(), ModernTheme.SmallFont, labelBrush, new PointF(14, 12));
        }

        // Main KPI value
        using (var valBrush = new SolidBrush(_valueColor))
        {
            g.DrawString(_kpiValue, ModernTheme.KpiNumberFont, valBrush, new PointF(12, 28));
        }

        // Subtitle / helper note
        if (!string.IsNullOrEmpty(_kpiSub))
        {
            using var subBrush = new SolidBrush(ModernTheme.TextSecondary);
            g.DrawString(_kpiSub, ModernTheme.SmallFont, subBrush, new PointF(14, 68));
        }

        // Badge in top right corner if present
        if (!string.IsNullOrEmpty(_badgeText))
        {
            var badgeSize = TextRenderer.MeasureText(_badgeText, ModernTheme.SmallFont);
            int bx = Width - badgeSize.Width - 22;
            int by = 12;
            ModernTheme.DrawBadge(g, _badgeText, bx, by, _badgeColor, _badgeTextColor);
        }
    }
}
