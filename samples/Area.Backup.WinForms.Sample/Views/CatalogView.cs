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

    // Header
    private Label _lblTitle = null!;
    private Label _lblSubtitle = null!;

    // UI Controls
    private CardPanel _cardRepo = null!;
    private TextBox _txtRepository = null!;
    private ModernButton _btnBrowseRepo = null!;
    private ModernButton _btnRefreshCatalog = null!;
    private MetricCard _kpiTotalPoints = null!;

    private CardPanel _cardGrid = null!;
    private DataGridView _gridCatalog = null!;

    private CardPanel _cardRestore = null!;
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

        // Header Title
        _lblTitle = new Label
        {
            Text = "Catálogo & Restauração Ponto no Tempo",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        _lblSubtitle = new Label
        {
            Text = "Consulte o histórico persistente do SQLite (catalog.db), audite cadeias e restaure pontos históricos com precisão.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(_lblTitle);
        Controls.Add(_lblSubtitle);

        // --- Row 1: Repository Selector & KPIs ---
        _cardRepo = new CardPanel
        {
            Location = new Point(24, 85),
            Height = 95
        };

        var lblRepo = new Label { Text = "Repositório:", Font = ModernTheme.BodyBold, Location = new Point(16, 16), AutoSize = true };
        _txtRepository = new TextBox
        {
            Location = new Point(16, 40),
            Font = ModernTheme.BodyFont
        };
        _btnBrowseRepo = new ModernButton
        {
            Text = "Procurar...",
            ButtonStyle = ModernButtonStyle.Secondary,
            Size = new Size(100, 28)
        };
        _btnBrowseRepo.Click += (_, _) => BrowseFolder(_txtRepository);

        _btnRefreshCatalog = new ModernButton
        {
            Text = "🔄  Carregar Catálogo",
            ButtonStyle = ModernButtonStyle.Primary,
            Size = new Size(160, 32)
        };
        _btnRefreshCatalog.Click += async (_, _) => await RefreshCatalogAsync();

        _kpiTotalPoints = new MetricCard { Title = "Total Pontos", Value = "0", Subtitle = "No catálogo", Size = new Size(170, 80) };

        _cardRepo.Controls.Add(lblRepo);
        _cardRepo.Controls.Add(_txtRepository);
        _cardRepo.Controls.Add(_btnBrowseRepo);
        _cardRepo.Controls.Add(_btnRefreshCatalog);
        _cardRepo.Controls.Add(_kpiTotalPoints);

        Controls.Add(_cardRepo);

        // --- Row 2: Data Table Grid ---
        _cardGrid = new CardPanel
        {
            Title = "Pontos de Recuperação no Repositório",
            Subtitle = "Selecione uma linha para validar integridade ou restaurar arquivos",
            Location = new Point(24, 190),
            Height = 280
        };

        _gridCatalog = new DataGridView
        {
            Location = new Point(16, 50),
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

        _cardGrid.Controls.Add(_gridCatalog);
        Controls.Add(_cardGrid);

        // --- Row 3: Restore & Action Panel ---
        _cardRestore = new CardPanel
        {
            Title = "Ações de Restauração & Auditoria",
            Subtitle = "Restaure os arquivos para uma pasta segura ou faça verificação SHA-256",
            Location = new Point(24, 480),
            Height = 150
        };

        var lblDest = new Label { Text = "Destino da Restauração:", Font = ModernTheme.BodyBold, Location = new Point(16, 52), AutoSize = true };
        _txtRestoreDest = new TextBox
        {
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupSample", "Restored"),
            Location = new Point(16, 74),
            Font = ModernTheme.BodyFont
        };
        _btnBrowseDest = new ModernButton
        {
            Text = "Procurar...",
            ButtonStyle = ModernButtonStyle.Secondary,
            Size = new Size(110, 28)
        };
        _btnBrowseDest.Click += (_, _) => BrowseFolder(_txtRestoreDest);

        _chkOverwrite = new CheckBox
        {
            Text = "Sobrescrever arquivos existentes",
            Font = ModernTheme.BodyFont,
            Size = new Size(205, 24),
            Checked = true
        };

        _btnValidateSelected = new ModernButton
        {
            Text = "🛡️  Validar Integridade",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(16, 108),
            Size = new Size(170, 32)
        };
        _btnValidateSelected.Click += async (_, _) => await ValidateSelectedAsync();

        _btnViewInfo = new ModernButton
        {
            Text = "ℹ️  Ver Metadados",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(195, 108),
            Size = new Size(150, 32)
        };
        _btnViewInfo.Click += (_, _) => ViewSelectedInfo();

        _btnRestoreSelected = new ModernButton
        {
            Text = "⚡  Restaurar Ponto Selecionado",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(355, 108),
            Size = new Size(240, 32)
        };
        _btnRestoreSelected.Click += async (_, _) => await RestoreSelectedAsync();

        _cardRestore.Controls.Add(lblDest);
        _cardRestore.Controls.Add(_txtRestoreDest);
        _cardRestore.Controls.Add(_btnBrowseDest);
        _cardRestore.Controls.Add(_chkOverwrite);
        _cardRestore.Controls.Add(_btnValidateSelected);
        _cardRestore.Controls.Add(_btnViewInfo);
        _cardRestore.Controls.Add(_btnRestoreSelected);

        Controls.Add(_cardRestore);

        PerformCustomLayout();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PerformCustomLayout();
    }

    private void PerformCustomLayout()
    {
        if (_cardRepo == null || _cardGrid == null || _cardRestore == null) return;

        int cardWidth = Math.Max(600, ClientSize.Width - 48);

        // Card 1
        _cardRepo.Location = new Point(24, 85);
        _cardRepo.Width = cardWidth;
        _txtRepository.Width = Math.Max(200, cardWidth - 480);
        _btnBrowseRepo.Location = new Point(cardWidth - 455, 38);
        _btnRefreshCatalog.Location = new Point(cardWidth - 345, 36);
        _kpiTotalPoints.Location = new Point(cardWidth - 175, 6);

        // Card 2
        _cardGrid.Location = new Point(24, 190);
        _cardGrid.Width = cardWidth;
        _gridCatalog.Location = new Point(16, 50);
        _gridCatalog.Size = new Size(cardWidth - 32, _cardGrid.Height - 66);

        // Card 3
        _cardRestore.Location = new Point(24, 480);
        _cardRestore.Width = cardWidth;
        _txtRestoreDest.Width = Math.Max(200, cardWidth - 350);
        _btnBrowseDest.Location = new Point(cardWidth - 325, 72);
        _chkOverwrite.Location = new Point(cardWidth - 210, 74);
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
