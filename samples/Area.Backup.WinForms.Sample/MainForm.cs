using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.WinForms.Sample.Theme;
using Area.Backup.WinForms.Sample.Views;

namespace Area.Backup.WinForms.Sample;

public sealed class MainForm : Form
{
    private readonly BackupEngine _engine = new();

    // Navigation & Views
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
        Size = new Size(1220, 780);
        MinimumSize = new Size(1050, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = ModernTheme.BodyFont;
        BackColor = ModernTheme.CanvasBg;
        Icon = SystemIcons.Shield;

        // Base Layout: Sidebar (Left) + Content (Fill)
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = ModernTheme.SidebarBg,
            Padding = new Padding(0)
        };

        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.CanvasBg,
            Padding = new Padding(0)
        };

        Controls.Add(_pnlContent);
        Controls.Add(_pnlSidebar);

        BuildSidebar();
    }

    private void BuildSidebar()
    {
        // 1. Brand Logo & Title Area
        var pnlBrand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.FromArgb(11, 17, 32),
            Padding = new Padding(18, 18, 18, 12)
        };

        var lblBrand = new Label
        {
            Text = "AREA BACKUP",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 16)
        };

        var lblSubtitle = new Label
        {
            Text = "Engine & Test Studio  .NET 10",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.SidebarText,
            AutoSize = true,
            Location = new Point(17, 44)
        };

        pnlBrand.Controls.Add(lblBrand);
        pnlBrand.Controls.Add(lblSubtitle);
        _pnlSidebar.Controls.Add(pnlBrand);

        // 2. Navigation Menu
        var pnlNav = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 15, 10, 15)
        };

        string[] menuItems =
        [
            "⚡  Dashboard",
            "⚙️  Configuração",
            "🧪  Laboratório de Testes",
            "🗄️  Catálogo & Restore",
            "📋  Console de Logs"
        ];

        int y = 15;
        for (int i = 0; i < menuItems.Length; i++)
        {
            int index = i;
            var btn = new Button
            {
                Text = menuItems[i],
                Font = ModernTheme.BodyBold,
                ForeColor = ModernTheme.SidebarText,
                BackColor = ModernTheme.SidebarBg,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = ModernTheme.SidebarHover },
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
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

        _pnlSidebar.Controls.Add(pnlNav);

        // 3. Bottom Footer in Sidebar
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = Color.FromArgb(11, 17, 32),
            Padding = new Padding(15)
        };

        var lblStatusDot = new Label
        {
            Text = "● Engine Ativa",
            Font = ModernTheme.SmallFont,
            ForeColor = ModernTheme.Success,
            AutoSize = true,
            Location = new Point(16, 15)
        };

        var lblCopyright = new Label
        {
            Text = "Atual Sistemas © 2026",
            Font = ModernTheme.MonoSmallFont,
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(16, 36)
        };

        pnlFooter.Controls.Add(lblStatusDot);
        pnlFooter.Controls.Add(lblCopyright);
        _pnlSidebar.Controls.Add(pnlFooter);
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

        // Create Views
        _logsView = new LogsView();
        _configView = new ConfigView(initialConfig, LogMessage);
        _dashboardView = new DashboardView(_engine, () => _configView.Configuration, LogMessage);
        _testLabView = new TestLabView(_engine, LogMessage);
        _catalogView = new CatalogView(_engine, () => _configView.Configuration, LogMessage);

        // Wire engine global events to log view
        _engine.ProgressChanged += (_, p) =>
        {
            if (p.Percentage % 20 < 1)
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

        LogMessage("SUCCESS", "Area Backup Studio inicializado com sucesso (.NET 10 Engine ativa).");
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
