using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.WinForms.Sample.Controls;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Views;

public sealed class CatalogView : UserControl
{
    private readonly BackupEngine _engine;
    private readonly Func<BackupConfiguration> _getConfigFunc;
    private readonly Action<string, string> _logAction;

    // UI Controls
    private TextBox _txtRepository = null!;
    private ModernButton _btnBrowseRepo = null!;
    private ModernButton _btnRefreshCatalog = null!;

    private DataGridView _gridCatalog = null!;
    private MetricCard _kpiTotalPoints = null!;

    private TextBox _txtRestoreDest = null!;
    private ModernButton _btnBrowseDest = null!;
    private CheckBox _chkOverwrite = null!;
    private ModernButton _btnValidateSelected = null!;
    private ModernButton _btnRestoreSelected = null!;
    private ModernButton _btnViewInfo = null!;

    public CatalogView(BackupEngine engine, Func<BackupConfiguration> getConfigFunc, Action<string, string> logAction)
    {
        _engine = engine;
        _getConfigFunc = getConfigFunc;
        _logAction = logAction;

        InitializeUI();
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
            Text = "Catálogo & Restauração Ponto no Tempo",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        var lblSubtitle = new Label
        {
            Text = "Consulte o histórico persistente do SQLite (catalog.db), audite cadeias e restaure pontos históricos com precisão.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);

        // --- Row 1: Repository Selector & KPIs ---
        var cardRepo = new CardPanel
        {
            Location = new Point(24, 85),
            Size = new Size(940, 95),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var lblRepo = new Label { Text = "Repositório:", Font = ModernTheme.BodyBold, Location = new Point(16, 16), AutoSize = true };
        _txtRepository = new TextBox
        {
            Location = new Point(16, 40),
            Size = new Size(450, 26),
            Font = ModernTheme.BodyFont,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnBrowseRepo = new ModernButton
        {
            Text = "Procurar...",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(475, 38),
            Size = new Size(100, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnBrowseRepo.Click += (_, _) => BrowseFolder(_txtRepository);

        _btnRefreshCatalog = new ModernButton
        {
            Text = "🔄  Carregar Catálogo",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(585, 36),
            Size = new Size(160, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnRefreshCatalog.Click += async (_, _) => await RefreshCatalogAsync();

        cardRepo.Controls.Add(lblRepo);
        cardRepo.Controls.Add(_txtRepository);
        cardRepo.Controls.Add(_btnBrowseRepo);
        cardRepo.Controls.Add(_btnRefreshCatalog);

        _kpiTotalPoints = new MetricCard { Title = "Total Pontos", Value = "0", Subtitle = "No catálogo", Location = new Point(755, 6), Size = new Size(170, 80), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        cardRepo.Controls.Add(_kpiTotalPoints);

        Controls.Add(cardRepo);

        // --- Row 2: Data Table Grid ---
        var cardGrid = new CardPanel
        {
            Title = "Pontos de Recuperação no Repositório",
            Subtitle = "Selecione uma linha para validar integridade ou restaurar arquivos",
            Location = new Point(24, 190),
            Size = new Size(940, 280),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _gridCatalog = new DataGridView
        {
            Location = new Point(16, 50),
            Size = new Size(908, 215),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            Font = ModernTheme.BodyFont,
            EnableHeadersVisualStyles = false
        };
        _gridCatalog.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.SectionHeader;
        _gridCatalog.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
        _gridCatalog.ColumnHeadersDefaultCellStyle.Font = ModernTheme.BodyBold;
        _gridCatalog.ColumnHeadersHeight = 32;
        _gridCatalog.RowTemplate.Height = 28;
        _gridCatalog.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

        cardGrid.Controls.Add(_gridCatalog);
        Controls.Add(cardGrid);

        // --- Row 3: Restore & Action Panel ---
        var cardRestore = new CardPanel
        {
            Title = "Ações de Restauração & Auditoria",
            Subtitle = "Restaure os arquivos para uma pasta segura ou faça verificação SHA-256",
            Location = new Point(24, 480),
            Size = new Size(940, 140),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        var lblDest = new Label { Text = "Destino da Restauração:", Font = ModernTheme.BodyBold, Location = new Point(16, 52), AutoSize = true };
        _txtRestoreDest = new TextBox
        {
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample", "Restored"),
            Location = new Point(16, 74),
            Size = new Size(580, 26),
            Font = ModernTheme.BodyFont,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnBrowseDest = new ModernButton
        {
            Text = "Procurar...",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(605, 72),
            Size = new Size(110, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnBrowseDest.Click += (_, _) => BrowseFolder(_txtRestoreDest);

        _chkOverwrite = new CheckBox
        {
            Text = "Sobrescrever arquivos existentes",
            Font = ModernTheme.BodyFont,
            Location = new Point(725, 74),
            Size = new Size(205, 24),
            Checked = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _btnValidateSelected = new ModernButton
        {
            Text = "🛡️  Validar Integridade",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(16, 104),
            Size = new Size(170, 30)
        };
        _btnValidateSelected.Click += async (_, _) => await ValidateSelectedAsync();

        _btnViewInfo = new ModernButton
        {
            Text = "ℹ️  Ver Metadados",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(195, 104),
            Size = new Size(150, 30)
        };
        _btnViewInfo.Click += (_, _) => ViewSelectedInfo();

        _btnRestoreSelected = new ModernButton
        {
            Text = "⚡  Restaurar Ponto Selecionado",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(355, 104),
            Size = new Size(240, 30)
        };
        _btnRestoreSelected.Click += async (_, _) => await RestoreSelectedAsync();

        cardRestore.Controls.Add(lblDest);
        cardRestore.Controls.Add(_txtRestoreDest);
        cardRestore.Controls.Add(_btnBrowseDest);
        cardRestore.Controls.Add(_chkOverwrite);
        cardRestore.Controls.Add(_btnValidateSelected);
        cardRestore.Controls.Add(_btnViewInfo);
        cardRestore.Controls.Add(_btnRestoreSelected);

        Controls.Add(cardRestore);
    }

    public async Task RefreshCatalogAsync()
    {
        var repo = _txtRepository.Text.Trim();
        if (string.IsNullOrEmpty(repo))
        {
            repo = _getConfigFunc().RepositoryPath;
            _txtRepository.Text = repo;
        }

        if (string.IsNullOrEmpty(repo) || !Directory.Exists(repo))
        {
            MessageBox.Show("Repositório não encontrado no disco. Execute um primeiro backup para criar o catálogo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var catalog = await _engine.GetCatalogAsync(repo);
            var entries = catalog.Entries.OrderByDescending(e => e.CreatedAtUtc).ToList();

            _kpiTotalPoints.Value = entries.Count.ToString();
            _kpiTotalPoints.Subtitle = $"{entries.Count(e => e.Type == BackupType.Full)} Fulls, {entries.Count(e => e.Type == BackupType.Incremental)} Incs";

            _gridCatalog.DataSource = entries.Select(e => new
            {
                ID = e.BackupId,
                Tipo = e.Type.ToString(),
                Data = e.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                Arquivos = e.FileCount,
                Excluídos = e.DeletedFileCount,
                Tamanho = $"{e.CompressedSizeBytes / (1024.0 * 1024.0):F2} MB",
                Status = e.Status.ToString(),
                Pai = e.ParentBackupId ?? "(Nenhum - Raiz)",
                Arquivo = e.RelativeFilePath
            }).ToList();

            _logAction("INFO", $"Catálogo atualizado: {entries.Count} pontos de restauração carregados de {repo}");
        }
        catch (Exception ex)
        {
            _logAction("ERROR", $"Falha ao ler catálogo SQLite: {ex.Message}");
            MessageBox.Show($"Erro ao ler catálogo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ValidateSelectedAsync()
    {
        var selected = GetSelectedBackupPath();
        if (selected == null) return;

        _logAction("INFO", $"Validando integridade de: {Path.GetFileName(selected)}");

        try
        {
            var validation = await _engine.ValidateBackupAsync(selected, new ValidationOptions { Mode = ValidationMode.Full });

            var msg = validation.IsValid
                ? $"✓ BACKUP ÍNTEGRO!\n\n" +
                  $"• Arquivos esperados: {validation.ExpectedFiles:N0}\n" +
                  $"• Arquivos válidos: {validation.ValidFiles:N0}\n" +
                  $"• Checksums divergentes: {validation.InvalidChecksums}\n" +
                  $"• Cadeia de dependências: {(validation.DependencyChainValid ? "Válida" : "Inválida")}\n" +
                  $"• Tempo de validação: {validation.Duration.TotalSeconds:F2}s"
                : $"✗ FALHA DE INTEGRIDADE!\n\nErros detectados:\n{string.Join("\n", validation.ValidationErrors)}";

            _logAction(validation.IsValid ? "SUCCESS" : "ERROR", $"Validação de {Path.GetFileName(selected)}: {(validation.IsValid ? "Válido" : "Inválido")}");
            MessageBox.Show(msg, "Validação Criptográfica", MessageBoxButtons.OK, validation.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha na validação: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ViewSelectedInfo()
    {
        var selected = GetSelectedBackupPath();
        if (selected == null) return;

        try
        {
            var info = _engine.GetBackupInfo(selected);
            var msg = $"METADADOS DO PACOTE\n\n" +
                      $"• ID: {info.BackupId}\n" +
                      $"• Tipo: {info.Type}\n" +
                      $"• Criado em: {info.CreatedAtUtc.ToLocalTime():dd/MM/yyyy HH:mm:ss}\n" +
                      $"• Backup Pai: {info.ParentBackupId ?? "(Nenhum)"}\n" +
                      $"• Full Raiz: {info.RootFullBackupId ?? "(Nenhum)"}\n" +
                      $"• Total de Arquivos: {info.FileCount:N0}\n" +
                      $"• Arquivos Excluídos: {info.DeletedFileCount:N0}\n" +
                      $"• Tamanho Original: {info.TotalSizeBytes / (1024.0 * 1024.0):F2} MB\n" +
                      $"• Tamanho Compactado: {info.CompressedSizeBytes / (1024.0 * 1024.0):F2} MB\n" +
                      $"• Versão da Engine: {info.EngineVersion}\n" +
                      $"• Fontes incluídas: {string.Join(", ", info.Sources)}";

            MessageBox.Show(msg, "Metadados do Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao ler metadados: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RestoreSelectedAsync()
    {
        var selected = GetSelectedBackupPath();
        if (selected == null) return;

        var dest = _txtRestoreDest.Text.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            MessageBox.Show("Informe a pasta de destino para a restauração.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _logAction("INFO", $"Iniciando restauração de {Path.GetFileName(selected)} para {dest}");

        try
        {
            var result = await _engine.RestoreBackupAsync(selected, new RestoreOptions
            {
                DestinationPath = dest,
                OverwriteExisting = _chkOverwrite.Checked
            });

            if (result.Success)
            {
                _logAction("SUCCESS", $"Restauração concluída com sucesso! {result.FilesRestored} arquivos ({result.BytesRestored / (1024.0 * 1024.0):F2} MB)");
                MessageBox.Show(
                    $"✓ RESTAURAÇÃO CONCLUÍDA COM SUCESSO!\n\n" +
                    $"• Backups processados na cadeia: {result.BackupsInChainCount}\n" +
                    $"• Arquivos restaurados: {result.FilesRestored:N0}\n" +
                    $"• Volume restaurado: {result.BytesRestored / (1024.0 * 1024.0):F2} MB\n" +
                    $"• Duração: {result.Duration:hh\\:mm\\:ss}",
                    "Restauração Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _logAction("WARN", $"Restauração concluída com {result.Errors.Count} avisos.");
                MessageBox.Show($"Restauração finalizada com avisos:\n{string.Join("\n", result.Errors.Select(e => e.Message))}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            _logAction("ERROR", $"Falha na restauração: {ex.Message}");
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

        var repo = _txtRepository.Text.Trim();
        var fullPath = Path.Combine(repo, relPath);

        if (!File.Exists(fullPath))
        {
            MessageBox.Show($"Arquivo não encontrado no disco: {fullPath}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
}
