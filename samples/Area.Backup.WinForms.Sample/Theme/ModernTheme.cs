using System.Drawing.Drawing2D;

namespace Area.Backup.WinForms.Sample.Theme;

/// <summary>
/// Modern Web/SaaS Design Tokens and GDI+ rendering helpers.
/// </summary>
public static class ModernTheme
{
    // --- Palette ---
    // Sidebar & Dark Accents (Slate)
    public static readonly Color SidebarBg = Color.FromArgb(15, 23, 42);       // Slate 900
    public static readonly Color SidebarHover = Color.FromArgb(30, 41, 59);    // Slate 800
    public static readonly Color SidebarActive = Color.FromArgb(51, 65, 85);   // Slate 700
    public static readonly Color SidebarText = Color.FromArgb(148, 163, 184);  // Slate 400
    public static readonly Color SidebarTextActive = Color.FromArgb(248, 250, 252); // Slate 50

    // Content Canvas
    public static readonly Color CanvasBg = Color.FromArgb(248, 250, 252);     // Slate 50
    public static readonly Color CardBg = Color.FromArgb(255, 255, 255);       // White
    public static readonly Color CardBorder = Color.FromArgb(226, 232, 240);   // Slate 200
    public static readonly Color CardBorderHover = Color.FromArgb(203, 213, 225); // Slate 300
    public static readonly Color SectionHeader = Color.FromArgb(241, 245, 249); // Slate 100

    // Typography Colors
    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);     // Slate 900
    public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);  // Slate 600
    public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);    // Slate 400

    // Accent Colors
    public static readonly Color Primary = Color.FromArgb(37, 99, 235);        // Blue 600
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);   // Blue 700
    public static readonly Color PrimaryLight = Color.FromArgb(239, 246, 255); // Blue 50
    
    public static readonly Color Success = Color.FromArgb(16, 185, 129);       // Emerald 500
    public static readonly Color SuccessLight = Color.FromArgb(236, 253, 245); // Emerald 50
    
    public static readonly Color Warning = Color.FromArgb(245, 158, 11);       // Amber 500
    public static readonly Color WarningLight = Color.FromArgb(254, 243, 199); // Amber 50
    
    public static readonly Color Danger = Color.FromArgb(239, 68, 68);         // Red 500
    public static readonly Color DangerHover = Color.FromArgb(220, 38, 38);    // Red 600
    public static readonly Color DangerLight = Color.FromArgb(254, 242, 242);  // Red 50

    public static readonly Color Indigo = Color.FromArgb(99, 102, 241);        // Indigo 500
    public static readonly Color Purple = Color.FromArgb(168, 85, 247);        // Purple 500

    // Terminal / Code Colors
    public static readonly Color TerminalBg = Color.FromArgb(10, 15, 29);      // Very deep slate
    public static readonly Color TerminalText = Color.FromArgb(226, 232, 240);
    public static readonly Color TerminalGreen = Color.FromArgb(52, 211, 153);
    public static readonly Color TerminalBlue = Color.FromArgb(96, 165, 250);
    public static readonly Color TerminalYellow = Color.FromArgb(251, 191, 36);
    public static readonly Color TerminalRed = Color.FromArgb(248, 113, 113);

    // --- Fonts ---
    public static readonly Font TitleFont = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font SubtitleFont = new("Segoe UI", 11.5f, FontStyle.Bold);
    public static readonly Font SectionFont = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font BodyBold = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font SmallFont = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font KpiNumberFont = new("Segoe UI", 20f, FontStyle.Bold);
    public static readonly Font MonoFont = new("Consolas", 9f, FontStyle.Regular);
    public static readonly Font MonoSmallFont = new("Consolas", 8f, FontStyle.Regular);

    // --- GDI Helpers ---
    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        int diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        // Top left
        path.AddArc(arc, 180, 90);

        // Top right
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom right
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom left
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    public static void DrawCard(Graphics g, Rectangle bounds, int radius = 10, Color? bg = null, Color? border = null)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedRectangle(bounds, radius);
        
        using (var brush = new SolidBrush(bg ?? CardBg))
        {
            g.FillPath(brush, path);
        }

        using (var pen = new Pen(border ?? CardBorder, 1f))
        {
            g.DrawPath(pen, path);
        }
    }

    public static void DrawBadge(Graphics g, string text, int x, int y, Color bgColor, Color textColor, Font? font = null)
    {
        font ??= SmallFont;
        var size = TextRenderer.MeasureText(text, font);
        var badgeRect = new Rectangle(x, y, size.Width + 12, size.Height + 4);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedRectangle(badgeRect, 6);
        using var brush = new SolidBrush(bgColor);
        g.FillPath(brush, path);

        TextRenderer.DrawText(g, text, font, badgeRect, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
