using System.Data;
using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;

namespace Area.Backup.WinForms.Sample;

public sealed class MainForm : Form
{
    private readonly BackupEngine _engine = new();
    private CancellationTokenSource? _cts;

    // Controls - Backup Tab
    private TabControl _tabControl = null!;
    private TabPage _tabBackup = null!;
    private TabPage _tabCatalog = null!;

    private TextBox _txtRepository = null!;
    private Button _btnBrowseRepo = null!;
    private CheckedListBox _lstSources = null!;
    private Button _btnAddSource = null!;
    private Button _btnRemoveSource = null!;

    private RadioButton _rbAuto = null!;
    private RadioButton _rbIncremental = null!;
    private RadioButton _rbFull = null!;

    private Label _lblStage = null!;
    private ProgressBar _progressBar = null!;
    private Label _lblPercentage = null!;
    private Label _lblFiles = null!;
    private Label _lblBytes = null!;
    private Label _lblCurrentFile = null!;
    private Label _lblTime = null!;

    private Button _btnStartBackup = null!;
    private Button _btnCancel = null!;
    private TextBox _txtLogs = null!;

    // Controls - Catalog & Restore Tab
    private TextBox _txtCatRepo = null!;
    private Button _btnRefreshCatalog = null!;
    private DataGridView _gridCatalog = null!;
    private TextBox _txtRestoreDest = null!;
    private Button _btnBrowseRestoreDest = null!;
    private Button _btnValidateSelected = null!;
    private Button _btnRestoreSelected = null!;

    public MainForm()
    {
        InitializeComponents();
        WireEvents();
    }

    private void InitializeComponents()
    {
        Text = "Area Backup Engine — ERP Integration Sample";
        Size = new Size(950, 720);
        MinimumSize = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        _tabControl = new TabControl { Dock = DockStyle.Fill };
        _tabBackup = new TabPage("Criar Backup");
        _tabCatalog = new TabPage("Catálogo & Restauração");

        _tabControl.TabPages.Add(_tabBackup);
        _tabControl.TabPages.Add(_tabCatalog);
        Controls.Add(_tabControl);

        BuildBackupTab();
        BuildCatalogTab();
    }

    private void BuildBackupTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(15) };
        _tabBackup.Controls.Add(panel);

        // Header Title
        var lblTitle = new Label
        {
            Text = "BACKUP DO SISTEMA ERP",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.DarkSlateBlue,
            AutoSize = true,
            Location = new Point(15, 10)
        };
        panel.Controls.Add(lblTitle);

        // GroupBox: Configuração
        var grpConfig = new GroupBox
        {
            Text = " Configurações de Origem e Destino ",
            Location = new Point(15, 45),
            Size = new Size(890, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        panel.Controls.Add(grpConfig);

        var lblRepo = new Label { Text = "Repositório de Backup:", Location = new Point(15, 25), AutoSize = true };
        _txtRepository = new TextBox
        {
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample", "Repository"),
            Location = new Point(15, 45),
            Size = new Size(740, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnBrowseRepo = new Button { Text = "Procurar...", Location = new Point(765, 43), Size = new Size(110, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnBrowseRepo.Click += (_, _) => BrowseFolder(_txtRepository);

        grpConfig.Controls.Add(lblRepo);
        grpConfig.Controls.Add(_txtRepository);
        grpConfig.Controls.Add(_btnBrowseRepo);

        var lblSources = new Label { Text = "Fontes a incluir no backup:", Location = new Point(15, 80), AutoSize = true };
        _lstSources = new CheckedListBox
        {
            Location = new Point(15, 100),
            Size = new Size(740, 85),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Add some default demo source directories
        var sampleSourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample", "ERP");
        if (!Directory.Exists(sampleSourceDir))
        {
            try
            {
                Directory.CreateDirectory(sampleSourceDir);
                File.WriteAllText(Path.Combine(sampleSourceDir, "config.ini"), "[ERP]\nVersao=2026.1\n");
                Directory.CreateDirectory(Path.Combine(sampleSourceDir, "Data0001"));
                File.WriteAllText(Path.Combine(sampleSourceDir, "Data0001", "clientes.dat"), "Sample data content");
            }
            catch { }
        }

        _lstSources.Items.Add(sampleSourceDir, true);

        _btnAddSource = new Button { Text = "Adicionar...", Location = new Point(765, 100), Size = new Size(110, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _btnRemoveSource = new Button { Text = "Remover", Location = new Point(765, 135), Size = new Size(110, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };

        _btnAddSource.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _lstSources.Items.Add(dlg.SelectedPath, true);
            }
        };

        _btnRemoveSource.Click += (_, _) =>
        {
            if (_lstSources.SelectedIndex >= 0)
                _lstSources.Items.RemoveAt(_lstSources.SelectedIndex);
        };

        grpConfig.Controls.Add(lblSources);
        grpConfig.Controls.Add(_lstSources);
        grpConfig.Controls.Add(_btnAddSource);
        grpConfig.Controls.Add(_btnRemoveSource);

        // GroupBox: Tipo de Backup
        var grpType = new GroupBox
        {
            Text = " Tipo de Backup ",
            Location = new Point(15, 255),
            Size = new Size(890, 55),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        panel.Controls.Add(grpType);

        _rbAuto = new RadioButton { Text = "Automático (Recomendado)", Location = new Point(20, 22), AutoSize = true, Checked = true };
        _rbIncremental = new RadioButton { Text = "Incremental", Location = new Point(240, 22), AutoSize = true };
        _rbFull = new RadioButton { Text = "Completo (Full)", Location = new Point(370, 22), AutoSize = true };

        grpType.Controls.Add(_rbAuto);
        grpType.Controls.Add(_rbIncremental);
        grpType.Controls.Add(_rbFull);

        // GroupBox: Progresso em Tempo Real
        var grpProgress = new GroupBox
        {
            Text = " Status da Execução em Tempo Real ",
            Location = new Point(15, 320),
            Size = new Size(890, 160),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        panel.Controls.Add(grpProgress);

        _lblStage = new Label
        {
            Text = "Status: Aguardando início...",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.MidnightBlue,
            Location = new Point(15, 22),
            AutoSize = true
        };
        _progressBar = new ProgressBar
        {
            Location = new Point(15, 48),
            Size = new Size(780, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Style = ProgressBarStyle.Continuous
        };
        _lblPercentage = new Label
        {
            Text = "0%",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Location = new Point(805, 48),
            Size = new Size(70, 24),
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _lblFiles = new Label { Text = "Arquivos: 0 / 0", Location = new Point(15, 80), AutoSize = true };
        _lblBytes = new Label { Text = "Processado: 0 MB / 0 MB (0.0 MB/s)", Location = new Point(260, 80), AutoSize = true };
        _lblTime = new Label { Text = "Tempo: 00:00:00 | Restante: --:--:--", Location = new Point(580, 80), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _lblCurrentFile = new Label
        {
            Text = "Arquivo: --",
            ForeColor = Color.Gray,
            Location = new Point(15, 105),
            Size = new Size(860, 20),
            AutoEllipsis = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _btnStartBackup = new Button
        {
            Text = "Iniciar Backup",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            BackColor = Color.ForestGreen,
            ForeColor = Color.White,
            Location = new Point(15, 128),
            Size = new Size(140, 30),
            UseVisualStyleBackColor = false
        };
        _btnCancel = new Button
        {
            Text = "Cancelar",
            BackColor = Color.Crimson,
            ForeColor = Color.White,
            Location = new Point(165, 128),
            Size = new Size(110, 30),
            Enabled = false,
            UseVisualStyleBackColor = false
        };

        grpProgress.Controls.Add(_lblStage);
        grpProgress.Controls.Add(_progressBar);
        grpProgress.Controls.Add(_lblPercentage);
        grpProgress.Controls.Add(_lblFiles);
        grpProgress.Controls.Add(_lblBytes);
        grpProgress.Controls.Add(_lblTime);
        grpProgress.Controls.Add(_lblCurrentFile);
        grpProgress.Controls.Add(_btnStartBackup);
        grpProgress.Controls.Add(_btnCancel);

        // GroupBox: Logs
        var grpLogs = new GroupBox
        {
            Text = " Registro de Atividades (Logs) ",
            Location = new Point(15, 490),
            Size = new Size(890, 160),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        panel.Controls.Add(grpLogs);

        _txtLogs = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            ForeColor = Color.LimeGreen,
            Font = new Font("Consolas", 9f)
        };
        grpLogs.Controls.Add(_txtLogs);
    }

    private void BuildCatalogTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        _tabCatalog.Controls.Add(panel);

        var lblCatRepo = new Label { Text = "Repositório:", Location = new Point(15, 15), AutoSize = true };
        _txtCatRepo = new TextBox
        {
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample", "Repository"),
            Location = new Point(95, 12),
            Size = new Size(650, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnRefreshCatalog = new Button
        {
            Text = "Carregar Catálogo",
            Location = new Point(755, 10),
            Size = new Size(140, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        panel.Controls.Add(lblCatRepo);
        panel.Controls.Add(_txtCatRepo);
        panel.Controls.Add(_btnRefreshCatalog);

        _gridCatalog = new DataGridView
        {
            Location = new Point(15, 50),
            Size = new Size(880, 480),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };
        panel.Controls.Add(_gridCatalog);

        var lblDest = new Label { Text = "Destino do Restore:", Location = new Point(15, 545), AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
        _txtRestoreDest = new TextBox
        {
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample", "Restored"),
            Location = new Point(140, 542),
            Size = new Size(605, 26),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnBrowseRestoreDest = new Button
        {
            Text = "Procurar...",
            Location = new Point(755, 540),
            Size = new Size(140, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnBrowseRestoreDest.Click += (_, _) => BrowseFolder(_txtRestoreDest);

        panel.Controls.Add(lblDest);
        panel.Controls.Add(_txtRestoreDest);
        panel.Controls.Add(_btnBrowseRestoreDest);

        _btnValidateSelected = new Button
        {
            Text = "Validar Integridade",
            Location = new Point(15, 580),
            Size = new Size(160, 32),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        _btnRestoreSelected = new Button
        {
            Text = "Restaurar Ponto Selecionado",
            BackColor = Color.DodgerBlue,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(185, 580),
            Size = new Size(220, 32),
            UseVisualStyleBackColor = false,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        panel.Controls.Add(_btnValidateSelected);
        panel.Controls.Add(_btnRestoreSelected);
    }

    private void WireEvents()
    {
        _btnStartBackup.Click += async (_, _) => await StartBackupAsync();
        _btnCancel.Click += (_, _) => _cts?.Cancel();
        _btnRefreshCatalog.Click += async (_, _) => await RefreshCatalogAsync();
        _btnValidateSelected.Click += async (_, _) => await ValidateSelectedAsync();
        _btnRestoreSelected.Click += async (_, _) => await RestoreSelectedAsync();
    }

    private async Task StartBackupAsync()
    {
        var repo = _txtRepository.Text.Trim();
        if (string.IsNullOrEmpty(repo))
        {
            MessageBox.Show("Informe o caminho do repositório.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedSources = _lstSources.CheckedItems.Cast<string>().ToList();
        if (selectedSources.Count == 0)
        {
            MessageBox.Show("Selecione pelo menos uma pasta de origem.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var backupType = _rbFull.Checked ? BackupType.Full : (_rbIncremental.Checked ? BackupType.Incremental : BackupType.Auto);

        var config = new BackupConfiguration
        {
            RepositoryPath = repo,
            BackupType = backupType,
            Sources = selectedSources.Select((s, i) => new BackupSource(s, $"SRC_{i + 1}")).ToList(),
            Incremental = new IncrementalOptions { Enabled = true, MaxIncrementalBackups = 7, MaxDaysSinceFull = 7 },
            Compression = new CompressionOptions { Algorithm = CompressionAlgorithm.Zip, Level = CompressionLevel.Optimal },
            Validation = new ValidationOptions { ValidateAfterBackup = true, Mode = ValidationMode.Quick },
            Retention = new RetentionPolicy { Enabled = true, KeepFullBackups = 4, KeepIncrementalBackups = 30 }
        };

        _btnStartBackup.Enabled = false;
        _btnCancel.Enabled = true;
        _progressBar.Value = 0;
        _lblPercentage.Text = "0%";
        _cts = new CancellationTokenSource();

        AppendLog($"[+] Iniciando backup {config.BackupType}...");

        var progress = new Progress<BackupProgress>(p =>
        {
            _lblStage.Text = $"Status: {TranslateStage(p.Stage)}";
            _progressBar.Value = (int)Math.Clamp(p.Percentage, 0, 100);
            _lblPercentage.Text = $"{p.Percentage:F1}%";
            _lblFiles.Text = $"Arquivos: {p.FilesProcessed:N0} / {p.FilesTotal:N0}";
            _lblBytes.Text = $"Processado: {FormatMb(p.BytesProcessed)} / {FormatMb(p.BytesTotal)} ({FormatSpeed(p.SpeedBytesPerSecond)})";
            _lblCurrentFile.Text = $"Arquivo: {p.CurrentFile}";
            _lblTime.Text = $"Tempo: {p.Elapsed:hh\\:mm\\:ss} | Restante: {(p.EstimatedRemaining.HasValue ? p.EstimatedRemaining.Value.ToString(@"hh\:mm\:ss") : "--:--:--")}";
        });

        try
        {
            var result = await _engine.CreateBackupAsync(config, progress, _cts.Token);

            AppendLog($"[✓] Backup concluído com sucesso!");
            AppendLog($"    ID: {result.BackupId} | Tipo: {result.Type}");
            AppendLog($"    Arquivos: +{result.FilesAdded} mod:{result.FilesModified} del:{result.FilesDeleted}");
            AppendLog($"    Tamanho Compactado: {FormatMb(result.CompressedSize)}");
            AppendLog($"    Duração: {result.Duration.TotalSeconds:F2}s");

            MessageBox.Show($"✓ BACKUP CONCLUÍDO\n\nTipo: {result.Type}\nArquivos alterados: {result.FilesAdded + result.FilesModified}\nTamanho: {FormatMb(result.CompressedSize)}\nTempo: {result.Duration:hh\\:mm\\:ss}\nIntegridade: ✓ OK",
                "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            AppendLog("[!] Backup cancelado pelo usuário.");
            _lblStage.Text = "Status: Cancelado.";
        }
        catch (Exception ex)
        {
            AppendLog($"[ERROR] Falha no backup: {ex.Message}");
            _lblStage.Text = "Status: Falha na execução.";
            MessageBox.Show($"Erro ao realizar backup: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnStartBackup.Enabled = true;
            _btnCancel.Enabled = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task RefreshCatalogAsync()
    {
        var repo = _txtCatRepo.Text.Trim();
        if (string.IsNullOrEmpty(repo) || !Directory.Exists(repo))
        {
            MessageBox.Show("Repositório não encontrado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var catalog = await _engine.GetCatalogAsync(repo);
            _gridCatalog.DataSource = catalog.Entries.Select(e => new
            {
                e.BackupId,
                Tipo = e.Type.ToString(),
                DataUtc = e.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                Arquivos = e.FileCount,
                Excluídos = e.DeletedFileCount,
                Tamanho = FormatMb(e.CompressedSizeBytes),
                Status = e.Status.ToString(),
                Arquivo = e.RelativeFilePath
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao ler catálogo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ValidateSelectedAsync()
    {
        var selected = GetSelectedBackupPath();
        if (selected == null) return;

        var validation = await _engine.ValidateBackupAsync(selected, new ValidationOptions { Mode = ValidationMode.Full });

        var msg = validation.IsValid
            ? $"✓ BACKUP ÍNTEGRO\n\nArquivos esperados: {validation.ExpectedFiles:N0}\nArquivos válidos: {validation.ValidFiles:N0}\nHashes inválidos: {validation.InvalidChecksums}\nCadeia válida: {(validation.DependencyChainValid ? "SIM" : "NÃO")}\nTempo: {validation.Duration.TotalSeconds:F2}s"
            : $"✗ FALHA DE INTEGRIDADE\n\nErros:\n{string.Join("\n", validation.ValidationErrors)}";

        MessageBox.Show(msg, "Validação de Integridade", MessageBoxButtons.OK, validation.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private async Task RestoreSelectedAsync()
    {
        var selected = GetSelectedBackupPath();
        if (selected == null) return;

        var dest = _txtRestoreDest.Text.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            MessageBox.Show("Informe a pasta de destino para restauração.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var result = await _engine.RestoreBackupAsync(selected, new RestoreOptions
            {
                DestinationPath = dest,
                OverwriteExisting = true
            });

            if (result.Success)
            {
                MessageBox.Show($"✓ RESTAURAÇÃO CONCLUÍDA COM SUCESSO!\n\nBackups na cadeia: {result.BackupsInChainCount}\nArquivos restaurados: {result.FilesRestored:N0}\nTamanho total: {FormatMb(result.BytesRestored)}\nTempo: {result.Duration:hh\\:mm\\:ss}",
                    "Restore Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Restauração finalizada com erros.\n{string.Join("\n", result.Errors.Select(e => e.Message))}", "Restore com Erros", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha na restauração: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? GetSelectedBackupPath()
    {
        if (_gridCatalog.SelectedRows.Count == 0)
        {
            MessageBox.Show("Selecione um backup na lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var row = _gridCatalog.SelectedRows[0];
        var relPath = row.Cells["Arquivo"].Value?.ToString();
        if (string.IsNullOrEmpty(relPath)) return null;

        var fullPath = Path.Combine(_txtCatRepo.Text.Trim(), relPath);
        if (!File.Exists(fullPath))
        {
            MessageBox.Show($"Arquivo de backup não encontrado no disco: {fullPath}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        return fullPath;
    }

    private void BrowseFolder(TextBox target)
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            target.Text = dlg.SelectedPath;
        }
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(AppendLog), message);
            return;
        }

        _txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
    }

    private static string TranslateStage(BackupStage stage) => stage switch
    {
        BackupStage.Initializing => "Inicializando...",
        BackupStage.Scanning => "Analisando arquivos...",
        BackupStage.DetectingChanges => "Detectando alterações...",
        BackupStage.Reading => "Lendo dados...",
        BackupStage.Compressing => "Compactando pacote...",
        BackupStage.Writing => "Gravando backup...",
        BackupStage.Validating => "Validando integridade...",
        BackupStage.Finalizing => "Finalizando...",
        BackupStage.Completed => "Concluído!",
        _ => stage.ToString()
    };

    private static string FormatMb(long bytes) => $"{bytes / (1024.0 * 1024.0):F1} MB";
    private static string FormatSpeed(double speedBps) => $"{speedBps / (1024.0 * 1024.0):F1} MB/s";
}
