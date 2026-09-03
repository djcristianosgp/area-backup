using Area.Backup.WinForms.Sample.Controls;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Views;

public sealed class LogsView : UserControl
{
    private Label _lblTitle = null!;
    private Label _lblSubtitle = null!;

    private Panel _pnlToolbar = null!;
    private ComboBox _cboFilter = null!;
    private TextBox _txtSearch = null!;
    private ModernButton _btnClear = null!;
    private ModernButton _btnExport = null!;
    private CheckBox _chkAutoScroll = null!;

    private CardPanel _cardTerminal = null!;
    private RichTextBox _rtbLogs = null!;

    private readonly List<(DateTime time, string level, string message)> _allLogs = [];

    public LogsView()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        Dock = DockStyle.Fill;
        BackColor = ModernTheme.CanvasBg;
        AutoScroll = true;

        // Header Title
        _lblTitle = new Label
        {
            Text = "Console de Logs & Auditoria",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        _lblSubtitle = new Label
        {
            Text = "Registro cronológico detalhado de eventos da engine, validações de hash e diagnóstico de execução.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(_lblTitle);
        Controls.Add(_lblSubtitle);

        // --- Toolbar ---
        _pnlToolbar = new Panel
        {
            Location = new Point(24, 85),
            Height = 42
        };

        var lblF = new Label { Text = "Filtrar:", Font = ModernTheme.BodyBold, Location = new Point(0, 10), AutoSize = true };
        _cboFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(50, 7),
            Size = new Size(130, 26),
            Font = ModernTheme.BodyFont
        };
        _cboFilter.Items.AddRange(["Todos", "Informação", "Sucesso", "Avisos", "Erros"]);
        _cboFilter.SelectedIndex = 0;
        _cboFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var lblS = new Label { Text = "Buscar:", Font = ModernTheme.BodyBold, Location = new Point(200, 10), AutoSize = true };
        _txtSearch = new TextBox
        {
            Location = new Point(255, 7),
            Size = new Size(200, 26),
            Font = ModernTheme.BodyFont,
            PlaceholderText = "Pesquisar no log..."
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        _chkAutoScroll = new CheckBox
        {
            Text = "Auto-scroll",
            Font = ModernTheme.BodyFont,
            Location = new Point(475, 9),
            Size = new Size(100, 24),
            Checked = true
        };

        _btnClear = new ModernButton
        {
            Text = "🗑️  Limpar",
            ButtonStyle = ModernButtonStyle.Secondary,
            Size = new Size(100, 30)
        };
        _btnClear.Click += (_, _) => ClearLogs();

        _btnExport = new ModernButton
        {
            Text = "💾  Exportar Log",
            ButtonStyle = ModernButtonStyle.Secondary,
            Size = new Size(120, 30)
        };
        _btnExport.Click += (_, _) => ExportLog();

        _pnlToolbar.Controls.Add(lblF);
        _pnlToolbar.Controls.Add(_cboFilter);
        _pnlToolbar.Controls.Add(lblS);
        _pnlToolbar.Controls.Add(_txtSearch);
        _pnlToolbar.Controls.Add(_chkAutoScroll);
        _pnlToolbar.Controls.Add(_btnClear);
        _pnlToolbar.Controls.Add(_btnExport);
        Controls.Add(_pnlToolbar);

        // --- Terminal Container ---
        _cardTerminal = new CardPanel
        {
            CardBackground = ModernTheme.TerminalBg,
            BorderColor = Color.FromArgb(30, 41, 59),
            Location = new Point(24, 135),
            Padding = new Padding(12)
        };

        _rtbLogs = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.TerminalBg,
            ForeColor = ModernTheme.TerminalText,
            Font = ModernTheme.MonoFont,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        _cardTerminal.Controls.Add(_rtbLogs);
        Controls.Add(_cardTerminal);

        PerformCustomLayout();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PerformCustomLayout();
    }

    private void PerformCustomLayout()
    {
        if (_pnlToolbar == null || _cardTerminal == null) return;

        int cardWidth = Math.Max(600, ClientSize.Width - 48);

        _pnlToolbar.Location = new Point(24, 85);
        _pnlToolbar.Width = cardWidth;
        _btnExport.Location = new Point(cardWidth - 120, 5);
        _btnClear.Location = new Point(cardWidth - 230, 5);

        _cardTerminal.Location = new Point(24, 135);
        _cardTerminal.Width = cardWidth;
        _cardTerminal.Height = Math.Max(300, ClientSize.Height - 160);
    }

    public void AppendLog(string level, string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string, string>(AppendLog), level, message);
            return;
        }

        var entry = (time: DateTime.Now, level: level.ToUpperInvariant(), message);
        _allLogs.Add(entry);

        if (MatchesFilter(entry))
        {
            RenderLogLine(entry);
        }
    }

    private void RenderLogLine((DateTime time, string level, string message) entry)
    {
        _rtbLogs.SelectionStart = _rtbLogs.TextLength;
        _rtbLogs.SelectionLength = 0;

        // Timestamp
        _rtbLogs.SelectionColor = Color.FromArgb(100, 116, 139); // Slate 500
        _rtbLogs.AppendText($"[{entry.time:HH:mm:ss}] ");

        // Level Badge
        _rtbLogs.SelectionColor = entry.level switch
        {
            "SUCCESS" => ModernTheme.TerminalGreen,
            "INFO" => ModernTheme.TerminalBlue,
            "WARN" => ModernTheme.TerminalYellow,
            "ERROR" => ModernTheme.TerminalRed,
            _ => ModernTheme.TerminalText
        };
        _rtbLogs.AppendText($"[{entry.level,-7}] ");

        // Message
        _rtbLogs.SelectionColor = ModernTheme.TerminalText;
        _rtbLogs.AppendText($"{entry.message}\r\n");

        if (_chkAutoScroll.Checked)
        {
            _rtbLogs.ScrollToCaret();
        }
    }

    private void ApplyFilter()
    {
        _rtbLogs.Clear();
        foreach (var entry in _allLogs)
        {
            if (MatchesFilter(entry))
            {
                RenderLogLine(entry);
            }
        }
    }

    private bool MatchesFilter((DateTime time, string level, string message) entry)
    {
        int filterIndex = _cboFilter.SelectedIndex;
        bool levelMatch = filterIndex switch
        {
            1 => entry.level == "INFO",
            2 => entry.level == "SUCCESS",
            3 => entry.level == "WARN",
            4 => entry.level == "ERROR",
            _ => true
        };

        if (!levelMatch) return false;

        var search = _txtSearch.Text.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            return entry.message.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   entry.level.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private void ClearLogs()
    {
        _allLogs.Clear();
        _rtbLogs.Clear();
    }

    private void ExportLog()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Arquivo de Log (*.log)|*.log|Arquivo de Texto (*.txt)|*.txt",
            FileName = $"area-backup-{DateTime.Now:yyyyMMdd-HHmmss}.log",
            Title = "Exportar Log de Execução"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dlg.FileName, _rtbLogs.Text);
            MessageBox.Show("Log exportado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
