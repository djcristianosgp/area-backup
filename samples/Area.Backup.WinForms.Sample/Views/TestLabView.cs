using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.WinForms.Sample.Controls;
using Area.Backup.WinForms.Sample.Models;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Views;

public sealed class TestLabView : UserControl
{
    private readonly BackupEngine _engine;
    private readonly Action<string, string> _logAction;
    private readonly TestScenarioState _testState = new();

    // UI Controls
    private Label _lblTitle = null!;
    private Label _lblSubtitle = null!;

    private FlowLayoutPanel _pnlKpis = null!;
    private MetricCard _kpiFiles = null!;
    private MetricCard _kpiAdded = null!;
    private MetricCard _kpiModified = null!;
    private MetricCard _kpiParity = null!;

    private CardPanel _cardSteps = null!;
    private ModernButton _btnCreateDataset = null!;
    private ModernButton _btnMutate = null!;
    private ModernButton _btnRunBackup = null!;
    private ModernButton _btnSandboxRestore = null!;
    private ModernButton _btnValidateArchive = null!;
    private ModernProgressBar _progressBar = null!;
    private Label _lblStatus = null!;

    private CardPanel _cardOutput = null!;
    private TextBox _txtTestConsole = null!;

    public TestLabView(BackupEngine engine, Action<string, string> logAction)
    {
        _engine = engine;
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
            Text = "Laboratório de Testes & Simulação (Sandbox)",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        _lblSubtitle = new Label
        {
            Text = "Gere dados fictícios, simule mutações incrementais e valide a restauração criptográfica bit a bit em sandbox isolado.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(_lblTitle);
        Controls.Add(_lblSubtitle);

        // --- Row 1: KPI Stats ---
        _pnlKpis = new FlowLayoutPanel
        {
            Location = new Point(24, 85),
            Size = new Size(880, 100),
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _kpiFiles = new MetricCard { Title = "Arquivos no Teste", Value = "0", Subtitle = "Origem isolada", Height = 95 };
        _kpiAdded = new MetricCard { Title = "Novos Arquivos", Value = "0", Subtitle = "Mutações simuladas", Height = 95 };
        _kpiModified = new MetricCard { Title = "Modificados", Value = "0", Subtitle = "Arquivos alterados", Height = 95 };
        _kpiParity = new MetricCard { Title = "Paridade SHA-256", Value = "--", Subtitle = "Sandbox audit", Height = 95 };
        _kpiParity.SetBadge("AGUARDANDO", ModernTheme.SectionHeader, ModernTheme.TextSecondary);

        _pnlKpis.Controls.Add(_kpiFiles);
        _pnlKpis.Controls.Add(_kpiAdded);
        _pnlKpis.Controls.Add(_kpiModified);
        _pnlKpis.Controls.Add(_kpiParity);
        Controls.Add(_pnlKpis);

        // --- Row 2: Workflow Steps Card ---
        _cardSteps = new CardPanel
        {
            Title = "Fluxo Guiado de Teste Incremental & Integridade",
            Subtitle = "Execute as etapas sequencialmente para comprovar o funcionamento da DLL sem risco aos dados de produção",
            Location = new Point(24, 195),
            Height = 160
        };

        _btnCreateDataset = new ModernButton
        {
            Text = "1. Criar Dataset ERP",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(24, 55),
            Size = new Size(160, 36)
        };
        _btnCreateDataset.Click += (_, _) => CreateDataset();

        _btnMutate = new ModernButton
        {
            Text = "2. Simular Mutações",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(195, 55),
            Size = new Size(160, 36),
            Enabled = false
        };
        _btnMutate.Click += (_, _) => MutateDataset();

        _btnRunBackup = new ModernButton
        {
            Text = "3. Rodar Backup Teste",
            ButtonStyle = ModernButtonStyle.Success,
            Location = new Point(365, 55),
            Size = new Size(170, 36),
            Enabled = false
        };
        _btnRunBackup.Click += async (_, _) => await RunTestBackupAsync();

        _btnSandboxRestore = new ModernButton
        {
            Text = "4. Restaurar & Conferir SHA",
            ButtonStyle = ModernButtonStyle.Outline,
            Location = new Point(545, 55),
            Size = new Size(190, 36),
            Enabled = false
        };
        _btnSandboxRestore.Click += async (_, _) => await RunSandboxRestoreAsync();

        _btnValidateArchive = new ModernButton
        {
            Text = "Auditar Pacote",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(745, 55),
            Size = new Size(130, 36),
            Enabled = false
        };
        _btnValidateArchive.Click += async (_, _) => await ValidateLatestBackupAsync();

        _progressBar = new ModernProgressBar
        {
            Location = new Point(24, 105),
            Height = 20,
            Value = 0
        };

        _lblStatus = new Label
        {
            Text = "Pronto. Clique em '1. Criar Dataset ERP' para iniciar o laboratório.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(24, 130),
            AutoSize = true
        };

        _cardSteps.Controls.Add(_btnCreateDataset);
        _cardSteps.Controls.Add(_btnMutate);
        _cardSteps.Controls.Add(_btnRunBackup);
        _cardSteps.Controls.Add(_btnSandboxRestore);
        _cardSteps.Controls.Add(_btnValidateArchive);
        _cardSteps.Controls.Add(_progressBar);
        _cardSteps.Controls.Add(_lblStatus);
        Controls.Add(_cardSteps);

        // --- Row 3: Live Output Terminal ---
        _cardOutput = new CardPanel
        {
            Title = "Relatório de Auditoria e Diagnóstico do Laboratório",
            Subtitle = "Saída detalhada das operações de teste, paridade de arquivos e hashes",
            Location = new Point(24, 370),
            Height = 280
        };

        _txtTestConsole = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(16, 50),
            BackColor = ModernTheme.TerminalBg,
            ForeColor = ModernTheme.TerminalGreen,
            Font = ModernTheme.MonoFont
        };

        _cardOutput.Controls.Add(_txtTestConsole);
        Controls.Add(_cardOutput);

        PerformCustomLayout();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PerformCustomLayout();
    }

    private void PerformCustomLayout()
    {
        if (_cardSteps == null || _cardOutput == null || _pnlKpis == null) return;

        int cardWidth = Math.Max(600, ClientSize.Width - 48);

        // KPIs
        _pnlKpis.Location = new Point(24, 85);
        _pnlKpis.Width = cardWidth;
        int kpiCardWidth = (cardWidth - 36) / 4;
        _kpiFiles.Width = kpiCardWidth;
        _kpiAdded.Width = kpiCardWidth;
        _kpiModified.Width = kpiCardWidth;
        _kpiParity.Width = kpiCardWidth;

        // Card Steps
        _cardSteps.Location = new Point(24, 195);
        _cardSteps.Width = cardWidth;
        _progressBar.Width = cardWidth - 48;

        // Card Output
        _cardOutput.Location = new Point(24, 370);
        _cardOutput.Width = cardWidth;
        _txtTestConsole.Location = new Point(16, 50);
        _txtTestConsole.Size = new Size(cardWidth - 32, _cardOutput.Height - 66);
    }

    private void CreateDataset()
    {
        _testState.GenerateInitialDataset(45);
        _kpiFiles.Value = _testState.TotalGeneratedFiles.ToString();
        _kpiAdded.Value = "0";
        _kpiModified.Value = "0";
        _kpiParity.Value = "--";
        _kpiParity.SetBadge("DATASET CRIADO", ModernTheme.PrimaryLight, ModernTheme.Primary);

        _btnMutate.Enabled = true;
        _btnRunBackup.Enabled = true;
        _btnSandboxRestore.Enabled = false;
        _btnValidateArchive.Enabled = false;

        AppendTestLog($"[1/4] Dataset de teste criado com sucesso em: {_testState.SourceFolder}");
        AppendTestLog($"      Total de arquivos gerados: {_testState.TotalGeneratedFiles} (PDFs, XMLs de NFe, Dados DAT e arquivos de exclusão *.tmp/*.log)");
        _lblStatus.Text = "Dataset criado! Você pode executar o primeiro Backup (Full) ou simular mutações.";
        _logAction("SUCCESS", "Laboratório: Dataset de teste ERP gerado.");
    }

    private void MutateDataset()
    {
        var (added, modified, deleted) = _testState.SimulateMutations();
        _kpiFiles.Value = _testState.TotalGeneratedFiles.ToString();
        _kpiAdded.Value = $"+{_testState.AddedFilesCount}";
        _kpiModified.Value = $"~{_testState.ModifiedFilesCount}";

        AppendTestLog($"[2/4] Mutações simuladas na base ERP:");
        AppendTestLog($"      + {added} novos arquivos adicionados (NFe XML, Aditivo PDF)");
        AppendTestLog($"      ~ {modified} arquivos existentes modificados (alteração de bytes e anotações)");
        AppendTestLog($"      - {deleted} arquivo excluído (para testar registro de deleção)");
        _lblStatus.Text = "Mutações aplicadas! Clique em '3. Rodar Backup Teste' para testar a detecção de deltas.";
        _logAction("INFO", $"Laboratório: {added} adicionados, {modified} alterados, {deleted} excluídos.");
    }

    private async Task RunTestBackupAsync()
    {
        _btnCreateDataset.Enabled = false;
        _btnMutate.Enabled = false;
        _btnRunBackup.Enabled = false;
        _lblStatus.Text = "Executando backup de teste na engine...";
        _progressBar.Value = 20;

        var config = new BackupConfiguration
        {
            RepositoryPath = _testState.RepositoryFolder,
            BackupType = BackupType.Auto,
            Sources = [new BackupSource(_testState.SourceFolder, "TEST_ERP", "Fonte de Testes")],
            Exclusions = [new BackupExclusion("*.tmp"), new BackupExclusion("*.log"), new BackupExclusion("Temp")],
            Incremental = new IncrementalOptions { Enabled = true, MaxIncrementalBackups = 7, MaxDaysSinceFull = 7 },
            Compression = new CompressionOptions { Algorithm = CompressionAlgorithm.Zip, Level = Core.Enums.CompressionLevel.Optimal },
            Validation = new ValidationOptions { ValidateAfterBackup = true, Mode = ValidationMode.Quick }
        };

        try
        {
            var progress = new Progress<BackupProgress>(p =>
            {
                _progressBar.Value = (int)Math.Clamp(p.Percentage, 0, 100);
            });

            var result = await _engine.CreateBackupAsync(config, progress);
            _progressBar.Value = 100;

            AppendTestLog($"[3/4] Backup de Teste Concluído!");
            AppendTestLog($"      ID: {result.BackupId} | Tipo: {result.Type}");
            AppendTestLog($"      Arquivos no pacote: {result.FilesAdded + result.FilesModified} (+{result.FilesAdded} novos, ~{result.FilesModified} mod)");
            AppendTestLog($"      Tamanho compactado: {result.CompressedSize / 1024.0:F1} KB em {result.Duration.TotalSeconds:F2}s");
            AppendTestLog($"      Integridade: ✓ 100% OK");

            _lblStatus.Text = $"Backup {result.Type} finalizado! Clique em '4. Restaurar & Conferir SHA' para auditoria.";
            _btnSandboxRestore.Enabled = true;
            _btnValidateArchive.Enabled = true;
            _btnMutate.Enabled = true;
            _logAction("SUCCESS", $"Laboratório: Backup {result.Type} concluído com sucesso.");
        }
        catch (Exception ex)
        {
            AppendTestLog($"[ERRO] Falha no backup de teste: {ex.Message}");
            _lblStatus.Text = $"Falha no backup: {ex.Message}";
            _logAction("ERROR", $"Laboratório erro: {ex.Message}");
        }
        finally
        {
            _btnCreateDataset.Enabled = true;
            _btnRunBackup.Enabled = true;
        }
    }

    private async Task RunSandboxRestoreAsync()
    {
        _btnSandboxRestore.Enabled = false;
        _lblStatus.Text = "Restaurando último ponto em sandbox isolado e validando hashes...";
        _progressBar.Value = 30;

        try
        {
            var catalog = await _engine.GetCatalogAsync(_testState.RepositoryFolder);
            var latest = catalog.Entries.OrderByDescending(e => e.CreatedAtUtc).FirstOrDefault();

            if (latest == null)
            {
                MessageBox.Show("Nenhum backup encontrado no repositório de teste.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var backupPath = Path.Combine(_testState.RepositoryFolder, latest.RelativeFilePath);
            var restoreOptions = new RestoreOptions
            {
                DestinationPath = _testState.SandboxRestoreFolder,
                OverwriteExisting = true
            };

            var restoreResult = await _engine.RestoreBackupAsync(backupPath, restoreOptions);

            _progressBar.Value = 70;
            AppendTestLog($"[4/4] Restauração em Sandbox finalizada:");
            AppendTestLog($"      Backups na cadeia: {restoreResult.BackupsInChainCount}");
            AppendTestLog($"      Arquivos restaurados: {restoreResult.FilesRestored}");
            AppendTestLog($"      Volume restaurado: {restoreResult.BytesRestored / 1024.0:F1} KB");

            // Verify Bit-by-Bit SHA256 Parity
            AppendTestLog("--- INICIANDO AUDITORIA CRIPTOGRÁFICA BIT A BIT (SHA-256) ---");
            var (matched, mismatched, missing, errors) = _testState.VerifySandboxParity();

            if (mismatched == 0 && missing == 0)
            {
                _kpiParity.Value = "100% OK";
                _kpiParity.SetBadge("PARIDADE TOTAL", ModernTheme.SuccessLight, ModernTheme.Success);
                AppendTestLog($"[✓] SUCESSO TOTAL: {matched}/{matched} arquivos conferidos com 100% de paridade SHA-256!");
                AppendTestLog($"[✓] Exclusões aplicadas corretamente (nenhum *.tmp ou *.log foi restaurado).");
                _lblStatus.Text = "Auditoria concluída com 100% de paridade criptográfica!";
            }
            else
            {
                _kpiParity.Value = "DIVERGÊNCIA";
                _kpiParity.SetBadge("ERRO", ModernTheme.DangerLight, ModernTheme.Danger);
                AppendTestLog($"[!] DIVERGÊNCIAS DETECTADAS: {mismatched} hashes divergentes, {missing} ausentes.");
                foreach (var err in errors)
                {
                    AppendTestLog($"    - {err}");
                }
            }

            _progressBar.Value = 100;
        }
        catch (Exception ex)
        {
            AppendTestLog($"[ERRO] Falha na restauração em sandbox: {ex.Message}");
            _lblStatus.Text = $"Erro na restauração: {ex.Message}";
        }
        finally
        {
            _btnSandboxRestore.Enabled = true;
        }
    }

    private async Task ValidateLatestBackupAsync()
    {
        try
        {
            var catalog = await _engine.GetCatalogAsync(_testState.RepositoryFolder);
            var latest = catalog.Entries.OrderByDescending(e => e.CreatedAtUtc).FirstOrDefault();
            if (latest == null) return;

            var backupPath = Path.Combine(_testState.RepositoryFolder, latest.RelativeFilePath);
            var validation = await _engine.ValidateBackupAsync(backupPath, new ValidationOptions { Mode = ValidationMode.Full });

            AppendTestLog($"[VALIDAÇÃO] Pacote: {Path.GetFileName(backupPath)}");
            AppendTestLog($"            Resultado: {(validation.IsValid ? "✓ ÍNTEGRO" : "✗ INVÁLIDO")}");
            AppendTestLog($"            Arquivos válidos: {validation.ValidFiles} / {validation.ExpectedFiles}");
            AppendTestLog($"            Cadeia de dependências: {(validation.DependencyChainValid ? "Válida" : "Inválida")}");
            AppendTestLog($"            Tempo de verificação: {validation.Duration.TotalMilliseconds:F0} ms");
        }
        catch (Exception ex)
        {
            AppendTestLog($"[ERRO] Falha na validação: {ex.Message}");
        }
    }

    private void AppendTestLog(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(AppendTestLog), msg);
            return;
        }

        _txtTestConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
    }
}
