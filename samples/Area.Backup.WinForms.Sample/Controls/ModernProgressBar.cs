using System.Drawing.Drawing2D;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Controls;

/// <summary>
/// Modern Web SaaS-styled progress bar with rounded corners, smooth gradient and optional text.
/// </summary>
public class ModernProgressBar : Control
{
    private int _value = 0;
    private int _maximum = 100;
    private int _minimum = 0;
    private Color _progressColor = ModernTheme.Primary;
    private Color _gradientEndColor = Color.FromArgb(59, 130, 246); // Blue 500
    private Color _trackColor = Color.FromArgb(226, 232, 240);     // Slate 200
    private bool _showPercentage = true;
    private string? _customText;

    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, _minimum, _maximum);
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = Math.Max(1, value); Invalidate(); }
    }

    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; Invalidate(); }
    }

    public Color ProgressColor
    {
        get => _progressColor;
        set { _progressColor = value; Invalidate(); }
    }

    public bool ShowPercentage
    {
        get => _showPercentage;
        set { _showPercentage = value; Invalidate(); }
    }

    public string? CustomText
    {
        get => _customText;
        set { _customText = value; Invalidate(); }
    }

    public ModernProgressBar()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Size = new Size(300, 20);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        int radius = Height / 2;

        // Draw track
        using (var trackPath = ModernTheme.CreateRoundedRectangle(rect, radius))
        using (var trackBrush = new SolidBrush(_trackColor))
        {
            g.FillPath(trackBrush, trackPath);
        }

        // Draw filled progress
        float fraction = (float)(_value - _minimum) / (_maximum - _minimum);
        int fillWidth = (int)(rect.Width * fraction);

        if (fillWidth > 4)
        {
            var fillRect = new Rectangle(0, 0, fillWidth, rect.Height);
            using var fillPath = ModernTheme.CreateRoundedRectangle(fillRect, radius);
            using var brush = new LinearGradientBrush(fillRect, _progressColor, _gradientEndColor, LinearGradientMode.Horizontal);
            g.FillPath(brush, fillPath);
        }

        // Overlay text if requested
        string? text = _customText;
        if (string.IsNullOrEmpty(text) && _showPercentage)
        {
            text = $"{fraction * 100:F0}%";
        }

        if (!string.IsNullOrEmpty(text))
        {
            using var textBrush = new SolidBrush(fillWidth > Width / 2 ? Color.White : ModernTheme.TextPrimary);
            TextRenderer.DrawText(g, text, ModernTheme.SmallFont, rect, textBrush.Color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
