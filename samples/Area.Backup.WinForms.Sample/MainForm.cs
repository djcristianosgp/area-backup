using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.WinForms.Sample.Theme;
using Area.Backup.WinForms.Sample.Views;

namespace Area.Backup.WinForms.Sample;

public sealed class MainForm : Form
{
    private readonly BackupEngine _engine = new();

    // Navigation & Layout
    private Panel _pnlSidebar = null!;
    private Panel _pnlContent = null!;

    private readonly List<Button> _navButtons = [];
    private DashboardView _dashboardView = null!;
    private ConfigView _configView = null!;
    private TestLabView _testLabView = null!;
    private CatalogView _catalogView = null!;
    private LogsView _logsView = null!;

    public MainForm()
    {
        InitializeApp();
        SetupViews();
        SwitchView(0); // Open Dashboard by default
    }

    private void InitializeApp()
    {
        Text = "Area Backup Engine — Central de Gestão & Testes (.NET 10)";
        Size = new Size(1240, 800);
        MinimumSize = new Size(1080, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = ModernTheme.BodyFont;
        BackColor = ModernTheme.CanvasBg;
        Icon = SystemIcons.Shield;

        // Base Layout: Sidebar (Left) + Content (Fill)
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = ModernTheme.SidebarBg
        };

        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.CanvasBg
        };

        // Add content first, then sidebar to ensure proper z-index and docking
        Controls.Add(_pnlContent);
        Controls.Add(_pnlSidebar);

        BuildSidebar();
    }

    private void BuildSidebar()
    {
        _pnlSidebar.Controls.Clear();

        // 1. Brand Header (Top)
        var pnlBrand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(11, 17, 32),
            Padding = new Padding(18, 16, 18, 12)
        };

        var lblBrand = new Label
        {
            Text = "AREA BACKUP",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 14)
        };

        var lblSubtitle = new Label
        {
            Text = "Engine & Test Studio  .NET 10",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.SidebarText,
            AutoSize = true,
            Location = new Point(17, 42)
        };

        pnlBrand.Controls.Add(lblBrand);
        pnlBrand.Controls.Add(lblSubtitle);

        // 2. Footer (Bottom)
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            BackColor = Color.FromArgb(11, 17, 32),
            Padding = new Padding(16, 12, 16, 12)
        };

        var lblStatusDot = new Label
        {
            Text = "● Engine Online",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.Success,
            AutoSize = true,
            Location = new Point(16, 12)
        };

        var lblCopyright = new Label
        {
            Text = "Atual Sistemas © 2026",
            Font = ModernTheme.MonoSmallFont,
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(16, 34)
        };

        pnlFooter.Controls.Add(lblStatusDot);
        pnlFooter.Controls.Add(lblCopyright);

        // 3. Navigation Container (Middle Fill)
        var pnlNav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.SidebarBg,
            Padding = new Padding(10, 15, 10, 10),
            AutoScroll = false
        };

        string[] menuTitles =
        [
            "⚡  Dashboard",
            "⚙️  Configuração",
            "🧪  Laboratório Testes",
            "🗄️  Catálogo Restore",
            "📋  Console de Logs"
        ];

        _navButtons.Clear();
        int y = 10;
        for (int i = 0; i < menuTitles.Length; i++)
        {
            int index = i;
            var btn = new Button
            {
                Text = menuTitles[i],
                Font = ModernTheme.BodyBold,
                ForeColor = ModernTheme.SidebarText,
                BackColor = ModernTheme.SidebarBg,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = ModernTheme.SidebarHover },
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Size = new Size(210, 42),
                Location = new Point(10, y),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            btn.Click += (_, _) => SwitchView(index);
            _navButtons.Add(btn);
            pnlNav.Controls.Add(btn);
            y += 48;
        }

        // Add to sidebar in correct docking order
        _pnlSidebar.Controls.Add(pnlNav);
        _pnlSidebar.Controls.Add(pnlBrand);
        _pnlSidebar.Controls.Add(pnlFooter);
        pnlNav.BringToFront();
    }

    private void SetupViews()
    {
        // Initial configuration object
        var sampleDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample");
        var initialConfig = new BackupConfiguration
        {
            RepositoryPath = Path.Combine(sampleDir, "Repository"),
            Sources = [new BackupSource(Path.Combine(sampleDir, "ERP"), "ERP_PRINCIPAL", "Sistema ERP")],
            Exclusions = [new BackupExclusion("*.tmp"), new BackupExclusion("*.log"), new BackupExclusion("Temp")],
            Incremental = new IncrementalOptions { Enabled = true, MaxIncrementalBackups = 7, MaxDaysSinceFull = 7, UseUsnJournal = true },
            Compression = new CompressionOptions { Algorithm = CompressionAlgorithm.Zip, Level = CompressionLevel.Optimal },
            Validation = new ValidationOptions { ValidateAfterBackup = true, Mode = ValidationMode.Quick },
            Retention = new RetentionPolicy { Enabled = true, KeepFullBackups = 4, KeepIncrementalBackups = 30 }
        };

        // Create Views with initial size matching content area
        var initialContentSize = new Size(Width - 230, Height);

        _logsView = new LogsView { Size = initialContentSize };
        _configView = new ConfigView(initialConfig, LogMessage) { Size = initialContentSize };
        _dashboardView = new DashboardView(_engine, () => _configView.Configuration, LogMessage) { Size = initialContentSize };
        _testLabView = new TestLabView(_engine, LogMessage) { Size = initialContentSize };
        _catalogView = new CatalogView(_engine, () => _configView.Configuration, LogMessage) { Size = initialContentSize };

        // Wire engine global events to log view
        _engine.ProgressChanged += (_, p) =>
        {
            if (p.Percentage % 25 < 1)
            {
                LogMessage("INFO", $"[{p.Stage}] {p.Percentage:F0}% - Processados: {p.FilesProcessed}/{p.FilesTotal} arquivos");
            }
        };

        _engine.StageChanged += (_, stage) =>
        {
            LogMessage("INFO", $"Transição de Etapa: {stage}");
        };

        _engine.Completed += (_, res) =>
        {
            LogMessage("SUCCESS", $"Backup finalizado com sucesso! ID: {res.BackupId} | Tipo: {res.Type} | Tamanho: {res.CompressedSize / (1024.0 * 1024.0):F2} MB");
        };

        _engine.Error += (_, err) =>
        {
            LogMessage("ERROR", $"Erro na engine: {err.Message}");
        };

        LogMessage("SUCCESS", "Area Backup Studio inicializado (.NET 10 Engine ativa).");
    }

    private void SwitchView(int viewIndex)
    {
        _pnlContent.Controls.Clear();

        UserControl targetView = viewIndex switch
        {
            0 => _dashboardView,
            1 => _configView,
            2 => _testLabView,
            3 => _catalogView,
            4 => _logsView,
            _ => _dashboardView
        };

        targetView.Dock = DockStyle.Fill;
        _pnlContent.Controls.Add(targetView);
        targetView.BringToFront();

        // Update sidebar button active state
        for (int i = 0; i < _navButtons.Count; i++)
        {
            if (i == viewIndex)
            {
                _navButtons[i].BackColor = ModernTheme.SidebarHover;
                _navButtons[i].ForeColor = ModernTheme.SidebarTextActive;
            }
            else
            {
                _navButtons[i].BackColor = ModernTheme.SidebarBg;
                _navButtons[i].ForeColor = ModernTheme.SidebarText;
            }
        }
    }

    private void LogMessage(string level, string message)
    {
        _logsView.AppendLog(level, message);
    }
}
