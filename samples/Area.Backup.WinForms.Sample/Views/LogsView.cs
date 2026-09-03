using Area.Backup.WinForms.Sample.Controls;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Views;

public sealed class LogsView : UserControl
{
    private RichTextBox _rtbLogs = null!;
    private ComboBox _cboFilter = null!;
    private TextBox _txtSearch = null!;
    private ModernButton _btnClear = null!;
    private ModernButton _btnExport = null!;
    private CheckBox _chkAutoScroll = null!;

    private readonly List<(DateTime time, string level, string message)> _allLogs = [];

    public LogsView()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        Dock = DockStyle.Fill;
        BackColor = ModernTheme.CanvasBg;
        Padding = new Padding(24);

        // Header Title
        var lblTitle = new Label
        {
            Text = "Console de Logs & Auditoria",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        var lblSubtitle = new Label
        {
            Text = "Registro cronológico detalhado de eventos da engine, validações de hash e diagnóstico de execução.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);

        // --- Toolbar ---
        var pnlToolbar = new Panel
        {
            Location = new Point(24, 85),
            Size = new Size(940, 42),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
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
            Location = new Point(710, 5),
            Size = new Size(105, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnClear.Click += (_, _) => ClearLogs();

        _btnExport = new ModernButton
        {
            Text = "💾  Exportar Log",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(825, 5),
            Size = new Size(115, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnExport.Click += (_, _) => ExportLog();

        pnlToolbar.Controls.Add(lblF);
        pnlToolbar.Controls.Add(_cboFilter);
        pnlToolbar.Controls.Add(lblS);
        pnlToolbar.Controls.Add(_txtSearch);
        pnlToolbar.Controls.Add(_chkAutoScroll);
        pnlToolbar.Controls.Add(_btnClear);
        pnlToolbar.Controls.Add(_btnExport);
        Controls.Add(pnlToolbar);

        // --- Terminal Container ---
        var cardTerminal = new CardPanel
        {
            CardBackground = ModernTheme.TerminalBg,
            BorderColor = Color.FromArgb(30, 41, 59),
            Location = new Point(24, 135),
            Size = new Size(940, 485),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
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

        cardTerminal.Controls.Add(_rtbLogs);
        Controls.Add(cardTerminal);
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
