using System.Text.Json;
using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.WinForms.Sample.Controls;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Views;

public sealed class ConfigView : UserControl
{
    private BackupConfiguration _config;
    private readonly Action<string, string> _logAction;

    // UI Controls
    private TextBox _txtRepository = null!;
    private ModernButton _btnBrowseRepo = null!;

    private ListView _lvSources = null!;
    private ModernButton _btnAddSource = null!;
    private ModernButton _btnRemoveSource = null!;

    private ListBox _lstExclusions = null!;
    private TextBox _txtNewExclusion = null!;
    private ModernButton _btnAddExclusion = null!;
    private ModernButton _btnRemoveExclusion = null!;

    private NumericUpDown _numMaxInc = null!;
    private NumericUpDown _numMaxDays = null!;
    private CheckBox _chkUsnJournal = null!;

    private ComboBox _cboCompressionLevel = null!;

    private CheckBox _chkEnableDb = null!;
    private ComboBox _cboDbType = null!;
    private TextBox _txtDbHost = null!;
    private NumericUpDown _numDbPort = null!;
    private TextBox _txtDbUser = null!;
    private TextBox _txtDbPass = null!;
    private TextBox _txtDbPath = null!;

    private NumericUpDown _numKeepFull = null!;
    private NumericUpDown _numKeepInc = null!;
    private CheckBox _chkRetentionEnabled = null!;

    private ModernButton _btnSaveJson = null!;
    private ModernButton _btnLoadJson = null!;
    private ModernButton _btnResetDefaults = null!;

    public BackupConfiguration Configuration => GetCurrentConfiguration();

    public ConfigView(BackupConfiguration initialConfig, Action<string, string> logAction)
    {
        _config = initialConfig;
        _logAction = logAction;

        InitializeUI();
        LoadConfigurationIntoForm(_config);
    }

    private void InitializeUI()
    {
        Dock = DockStyle.Fill;
        BackColor = ModernTheme.CanvasBg;
        AutoScroll = true;
        Padding = new Padding(24);

        // Header Title
        var lblTitle = new Label
        {
            Text = "Configurações da DLL Area.Backup",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        var lblSubtitle = new Label
        {
            Text = "Gerencie origens, filtros de exclusão, compressão, políticas de retenção e provedores de banco de dados.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);

        // Action Toolbar (Save/Load JSON)
        _btnSaveJson = new ModernButton
        {
            Text = "💾  Salvar JSON",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(580, 20),
            Size = new Size(130, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnSaveJson.Click += (_, _) => SaveJsonToFile();

        _btnLoadJson = new ModernButton
        {
            Text = "📂  Carregar JSON",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(720, 20),
            Size = new Size(140, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnLoadJson.Click += (_, _) => LoadJsonFromFile();

        _btnResetDefaults = new ModernButton
        {
            Text = "↺  Padrões",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(870, 20),
            Size = new Size(95, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnResetDefaults.Click += (_, _) => ResetToDefaults();

        Controls.Add(_btnSaveJson);
        Controls.Add(_btnLoadJson);
        Controls.Add(_btnResetDefaults);

        int currentY = 85;

        // --- Card 1: Repositório Principal ---
        var cardRepo = new CardPanel
        {
            Title = "1. Repositório de Destino",
            Subtitle = "Local onde os pacotes criptográficos (.backup) e o catálogo SQLite serão salvos",
            Location = new Point(24, currentY),
            Size = new Size(940, 110),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var lblRepo = new Label { Text = "Caminho do Repositório:", Font = ModernTheme.BodyBold, Location = new Point(24, 55), AutoSize = true };
        _txtRepository = new TextBox
        {
            Location = new Point(24, 75),
            Size = new Size(760, 26),
            Font = ModernTheme.BodyFont,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnBrowseRepo = new ModernButton
        {
            Text = "Procurar...",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(795, 73),
            Size = new Size(120, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnBrowseRepo.Click += (_, _) => BrowseFolder(_txtRepository);

        cardRepo.Controls.Add(lblRepo);
        cardRepo.Controls.Add(_txtRepository);
        cardRepo.Controls.Add(_btnBrowseRepo);
        Controls.Add(cardRepo);

        currentY += 125;

        // --- Card 2: Origens & Exclusões ---
        var cardSources = new CardPanel
        {
            Title = "2. Pastas de Origem & Regras de Exclusão",
            Subtitle = "Diretórios a serem protegidos e padrões de arquivos temporários ignorados",
            Location = new Point(24, currentY),
            Size = new Size(940, 250),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Left Column: Sources
        var lblSrc = new Label { Text = "Origens (Sources):", Font = ModernTheme.BodyBold, Location = new Point(24, 50), AutoSize = true };
        _lvSources = new ListView
        {
            Location = new Point(24, 72),
            Size = new Size(460, 130),
            View = View.Details,
            FullRowSelect = true,
            Font = ModernTheme.BodyFont
        };
        _lvSources.Columns.Add("Alias / Tag", 120);
        _lvSources.Columns.Add("Caminho no Disco", 320);

        _btnAddSource = new ModernButton
        {
            Text = "+ Adicionar Origem",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(24, 208),
            Size = new Size(150, 30)
        };
        _btnAddSource.Click += (_, _) => AddSource();

        _btnRemoveSource = new ModernButton
        {
            Text = "- Remover Selecionada",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(180, 208),
            Size = new Size(160, 30)
        };
        _btnRemoveSource.Click += (_, _) => RemoveSelectedSource();

        cardSources.Controls.Add(lblSrc);
        cardSources.Controls.Add(_lvSources);
        cardSources.Controls.Add(_btnAddSource);
        cardSources.Controls.Add(_btnRemoveSource);

        // Right Column: Exclusions
        var lblExc = new Label { Text = "Padrões de Exclusão:", Font = ModernTheme.BodyBold, Location = new Point(510, 50), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _lstExclusions = new ListBox
        {
            Location = new Point(510, 72),
            Size = new Size(405, 95),
            Font = ModernTheme.MonoFont,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _txtNewExclusion = new TextBox
        {
            Text = "*.tmp",
            Location = new Point(510, 172),
            Size = new Size(240, 26),
            Font = ModernTheme.MonoFont,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _btnAddExclusion = new ModernButton
        {
            Text = "+ Regra",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(758, 170),
            Size = new Size(75, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnAddExclusion.Click += (_, _) =>
        {
            var txt = _txtNewExclusion.Text.Trim();
            if (!string.IsNullOrEmpty(txt) && !_lstExclusions.Items.Contains(txt))
            {
                _lstExclusions.Items.Add(txt);
            }
        };

        _btnRemoveExclusion = new ModernButton
        {
            Text = "- Remover",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(840, 170),
            Size = new Size(75, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnRemoveExclusion.Click += (_, _) =>
        {
            if (_lstExclusions.SelectedIndex >= 0)
                _lstExclusions.Items.RemoveAt(_lstExclusions.SelectedIndex);
        };

        cardSources.Controls.Add(lblExc);
        cardSources.Controls.Add(_lstExclusions);
        cardSources.Controls.Add(_txtNewExclusion);
        cardSources.Controls.Add(_btnAddExclusion);
        cardSources.Controls.Add(_btnRemoveExclusion);

        Controls.Add(cardSources);

        currentY += 265;

        // --- Card 3: Parâmetros Incrementais & Compressão ---
        var cardParams = new CardPanel
        {
            Title = "3. Parâmetros Incrementais & Compressão",
            Subtitle = "Controle das regras de delta e taxa de compactação dos pacotes",
            Location = new Point(24, currentY),
            Size = new Size(940, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var lblInc = new Label { Text = "Máx. Incrementais antes do Full:", Font = ModernTheme.BodyFont, Location = new Point(24, 55), AutoSize = true };
        _numMaxInc = new NumericUpDown { Location = new Point(24, 75), Size = new Size(120, 26), Minimum = 1, Maximum = 100, Value = 7 };

        var lblDays = new Label { Text = "Máx. Dias sem novo Full:", Font = ModernTheme.BodyFont, Location = new Point(210, 55), AutoSize = true };
        _numMaxDays = new NumericUpDown { Location = new Point(210, 75), Size = new Size(120, 26), Minimum = 1, Maximum = 365, Value = 7 };

        _chkUsnJournal = new CheckBox
        {
            Text = "Usar NTFS USN Journal (Ultra rápido)",
            Font = ModernTheme.BodyFont,
            Location = new Point(380, 75),
            Size = new Size(260, 26),
            Checked = true
        };

        var lblComp = new Label { Text = "Nível de Compressão:", Font = ModernTheme.BodyFont, Location = new Point(670, 55), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cboCompressionLevel = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(670, 75),
            Size = new Size(245, 26),
            Font = ModernTheme.BodyFont,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _cboCompressionLevel.Items.AddRange(["Optimal (Recomendado)", "Fastest (Mais Rápido)", "NoCompression (Sem Compactação)"]);
        _cboCompressionLevel.SelectedIndex = 0;

        cardParams.Controls.Add(lblInc);
        cardParams.Controls.Add(_numMaxInc);
        cardParams.Controls.Add(lblDays);
        cardParams.Controls.Add(_numMaxDays);
        cardParams.Controls.Add(_chkUsnJournal);
        cardParams.Controls.Add(lblComp);
        cardParams.Controls.Add(_cboCompressionLevel);

        Controls.Add(cardParams);

        currentY += 155;

        // --- Card 4: Retenção & Banco de Dados ---
        var cardDbRet = new CardPanel
        {
            Title = "4. Retenção de Histórico & Integração com Banco de Dados",
            Subtitle = "Regras de expurgo seguro e credenciais para dump de Firebird ou PostgreSQL",
            Location = new Point(24, currentY),
            Size = new Size(940, 230),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _chkRetentionEnabled = new CheckBox
        {
            Text = "Ativar Política de Retenção Automática",
            Font = ModernTheme.BodyBold,
            Location = new Point(24, 50),
            Size = new Size(300, 24),
            Checked = true
        };

        var lblKFull = new Label { Text = "Manter Backups Fulls:", Font = ModernTheme.BodyFont, Location = new Point(24, 80), AutoSize = true };
        _numKeepFull = new NumericUpDown { Location = new Point(24, 100), Size = new Size(110, 26), Minimum = 1, Maximum = 100, Value = 4 };

        var lblKInc = new Label { Text = "Manter Incrementais:", Font = ModernTheme.BodyFont, Location = new Point(160, 80), AutoSize = true };
        _numKeepInc = new NumericUpDown { Location = new Point(160, 100), Size = new Size(110, 26), Minimum = 1, Maximum = 365, Value = 30 };

        cardDbRet.Controls.Add(_chkRetentionEnabled);
        cardDbRet.Controls.Add(lblKFull);
        cardDbRet.Controls.Add(_numKeepFull);
        cardDbRet.Controls.Add(lblKInc);
        cardDbRet.Controls.Add(_numKeepInc);

        // Database Section
        _chkEnableDb = new CheckBox
        {
            Text = "Integrar Dump de Banco (Firebird / PostgreSQL)",
            Font = ModernTheme.BodyBold,
            Location = new Point(400, 50),
            Size = new Size(380, 24),
            Checked = false
        };

        var lblDbType = new Label { Text = "SGDB:", Font = ModernTheme.BodyFont, Location = new Point(400, 80), AutoSize = true };
        _cboDbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(400, 100), Size = new Size(130, 26) };
        _cboDbType.Items.AddRange(["Firebird", "PostgreSql"]);
        _cboDbType.SelectedIndex = 0;

        var lblDbHost = new Label { Text = "Host / Porta:", Font = ModernTheme.BodyFont, Location = new Point(545, 80), AutoSize = true };
        _txtDbHost = new TextBox { Text = "localhost", Location = new Point(545, 100), Size = new Size(140, 26) };
        _numDbPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 3050, Location = new Point(695, 100), Size = new Size(70, 26) };

        var lblDbUser = new Label { Text = "Usuário / Senha:", Font = ModernTheme.BodyFont, Location = new Point(400, 135), AutoSize = true };
        _txtDbUser = new TextBox { Text = "SYSDBA", Location = new Point(400, 155), Size = new Size(130, 26) };
        _txtDbPass = new TextBox { Text = "masterkey", PasswordChar = '•', Location = new Point(545, 155), Size = new Size(140, 26) };

        var lblDbPath = new Label { Text = "Caminho do Banco (.FDB / Nome):", Font = ModernTheme.BodyFont, Location = new Point(400, 190), AutoSize = true };
        _txtDbPath = new TextBox { Text = @"C:\ERP\Dados\SISTEMA.FDB", Location = new Point(620, 187), Size = new Size(295, 26) };

        cardDbRet.Controls.Add(_chkEnableDb);
        cardDbRet.Controls.Add(lblDbType);
        cardDbRet.Controls.Add(_cboDbType);
        cardDbRet.Controls.Add(lblDbHost);
        cardDbRet.Controls.Add(_txtDbHost);
        cardDbRet.Controls.Add(_numDbPort);
        cardDbRet.Controls.Add(lblDbUser);
        cardDbRet.Controls.Add(_txtDbUser);
        cardDbRet.Controls.Add(_txtDbPass);
        cardDbRet.Controls.Add(lblDbPath);
        cardDbRet.Controls.Add(_txtDbPath);

        Controls.Add(cardDbRet);
    }

    private void AddSource()
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            int index = _lvSources.Items.Count + 1;
            var item = new ListViewItem($"SRC_{index}");
            item.SubItems.Add(dlg.SelectedPath);
            _lvSources.Items.Add(item);
        }
    }

    private void RemoveSelectedSource()
    {
        if (_lvSources.SelectedItems.Count > 0)
        {
            _lvSources.Items.Remove(_lvSources.SelectedItems[0]);
        }
    }

    private void BrowseFolder(TextBox target)
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            target.Text = dlg.SelectedPath;
        }
    }

    private void LoadConfigurationIntoForm(BackupConfiguration config)
    {
        _txtRepository.Text = config.RepositoryPath;

        _lvSources.Items.Clear();
        foreach (var src in config.Sources)
        {
            var item = new ListViewItem(src.Id);
            item.SubItems.Add(src.Path);
            _lvSources.Items.Add(item);
        }

        _lstExclusions.Items.Clear();
        foreach (var exc in config.Exclusions)
        {
            _lstExclusions.Items.Add(exc.Pattern);
        }

        _numMaxInc.Value = Math.Max(1, config.Incremental.MaxIncrementalBackups);
        _numMaxDays.Value = Math.Max(1, config.Incremental.MaxDaysSinceFull);
        _chkUsnJournal.Checked = config.Incremental.UseUsnJournal;

        _cboCompressionLevel.SelectedIndex = config.Compression.Level switch
        {
            Core.Enums.CompressionLevel.Optimal => 0,
            Core.Enums.CompressionLevel.Fastest => 1,
            Core.Enums.CompressionLevel.NoCompression => 2,
            _ => 0
        };

        _chkRetentionEnabled.Checked = config.Retention.Enabled;
        _numKeepFull.Value = Math.Max(1, config.Retention.KeepFullBackups);
        _numKeepInc.Value = Math.Max(1, config.Retention.KeepIncrementalBackups);

        _chkEnableDb.Checked = config.Database.Enabled;
        _cboDbType.SelectedIndex = string.Equals(config.Database.ProviderType, "PostgreSQL", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _txtDbHost.Text = config.Database.Host ?? "localhost";
        _numDbPort.Value = config.Database.Port > 0 ? config.Database.Port : 3050;
        _txtDbUser.Text = config.Database.Username ?? "SYSDBA";
        _txtDbPass.Text = config.Database.Password ?? "";
        _txtDbPath.Text = config.Database.DatabasePath ?? config.Database.DatabaseName ?? @"C:\ERP\Dados\SISTEMA.FDB";
    }

    public BackupConfiguration GetCurrentConfiguration()
    {
        var isPostgres = _cboDbType.SelectedIndex == 1;
        var config = new BackupConfiguration
        {
            RepositoryPath = _txtRepository.Text.Trim(),
            Sources = _lvSources.Items.Cast<ListViewItem>().Select(i => new BackupSource(i.SubItems[1].Text, i.Text)).ToList(),
            Exclusions = _lstExclusions.Items.Cast<string>().Select(p => new BackupExclusion(p)).ToList(),
            Incremental = new IncrementalOptions
            {
                Enabled = true,
                MaxIncrementalBackups = (int)_numMaxInc.Value,
                MaxDaysSinceFull = (int)_numMaxDays.Value,
                UseUsnJournal = _chkUsnJournal.Checked
            },
            Compression = new CompressionOptions
            {
                Algorithm = CompressionAlgorithm.Zip,
                Level = _cboCompressionLevel.SelectedIndex switch
                {
                    0 => Core.Enums.CompressionLevel.Optimal,
                    1 => Core.Enums.CompressionLevel.Fastest,
                    2 => Core.Enums.CompressionLevel.NoCompression,
                    _ => Core.Enums.CompressionLevel.Optimal
                }
            },
            Validation = new ValidationOptions
            {
                ValidateAfterBackup = true,
                Mode = ValidationMode.Quick
            },
            Retention = new RetentionPolicy
            {
                Enabled = _chkRetentionEnabled.Checked,
                KeepFullBackups = (int)_numKeepFull.Value,
                KeepIncrementalBackups = (int)_numKeepInc.Value
            },
            Database = new DatabaseBackupOptions
            {
                Enabled = _chkEnableDb.Checked,
                ProviderType = isPostgres ? "PostgreSQL" : "Firebird",
                Host = _txtDbHost.Text.Trim(),
                Port = (int)_numDbPort.Value,
                Username = _txtDbUser.Text.Trim(),
                Password = _txtDbPass.Text,
                DatabasePath = !isPostgres ? _txtDbPath.Text.Trim() : null,
                DatabaseName = isPostgres ? _txtDbPath.Text.Trim() : null
            }
        };

        return config;
    }

    private void SaveJsonToFile()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Arquivo de Configuração JSON (*.json)|*.json",
            FileName = "backup-config.json",
            Title = "Salvar Perfil de Configuração"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var config = GetCurrentConfiguration();
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dlg.FileName, json);
            _logAction("SUCCESS", $"Perfil de configuração salvo com sucesso: {dlg.FileName}");
            MessageBox.Show("Configuração salva com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void LoadJsonFromFile()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Arquivo de Configuração JSON (*.json)|*.json",
            Title = "Carregar Perfil de Configuração"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var json = File.ReadAllText(dlg.FileName);
                var config = JsonSerializer.Deserialize<BackupConfiguration>(json);
                if (config != null)
                {
                    LoadConfigurationIntoForm(config);
                    _logAction("INFO", $"Perfil carregado a partir de: {dlg.FileName}");
                    MessageBox.Show("Configuração carregada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao ler JSON: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ResetToDefaults()
    {
        var sampleDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample");
        var defaultConfig = new BackupConfiguration
        {
            RepositoryPath = Path.Combine(sampleDir, "Repository"),
            Sources = [new BackupSource(Path.Combine(sampleDir, "ERP"), "ERP_PRINCIPAL", "Sistema ERP")],
            Exclusions = [new BackupExclusion("*.tmp"), new BackupExclusion("*.log"), new BackupExclusion("Temp")],
            Incremental = new IncrementalOptions { Enabled = true, MaxIncrementalBackups = 7, MaxDaysSinceFull = 7, UseUsnJournal = true },
            Compression = new CompressionOptions { Algorithm = CompressionAlgorithm.Zip, Level = Core.Enums.CompressionLevel.Optimal },
            Retention = new RetentionPolicy { Enabled = true, KeepFullBackups = 4, KeepIncrementalBackups = 30 }
        };

        LoadConfigurationIntoForm(defaultConfig);
        _logAction("INFO", "Configurações restauradas para o padrão recomendado.");
    }
}
