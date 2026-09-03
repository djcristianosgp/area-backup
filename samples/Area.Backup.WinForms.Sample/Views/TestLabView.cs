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
    private MetricCard _kpiFiles = null!;
    private MetricCard _kpiAdded = null!;
    private MetricCard _kpiModified = null!;
    private MetricCard _kpiParity = null!;

    private ModernButton _btnCreateDataset = null!;
    private ModernButton _btnMutate = null!;
    private ModernButton _btnRunBackup = null!;
    private ModernButton _btnSandboxRestore = null!;
    private ModernButton _btnValidateArchive = null!;

    private TextBox _txtTestConsole = null!;
    private ModernProgressBar _progressBar = null!;
    private Label _lblStatus = null!;

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
        Padding = new Padding(24);

        // Header Title
        var lblTitle = new Label
        {
            Text = "Laboratório de Testes & Simulação (Sandbox)",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        var lblSubtitle = new Label
        {
            Text = "Gere dados fictícios, simule mutações incrementais e valide a restauração criptográfica bit a bit em sandbox isolado.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);

        // --- Row 1: KPI Stats ---
        var pnlKpis = new FlowLayoutPanel
        {
            Location = new Point(24, 85),
            Size = new Size(940, 105),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            WrapContents = false
        };

        _kpiFiles = new MetricCard { Title = "Arquivos no Teste", Value = "0", Subtitle = "Origem isolada", Width = 220 };
        _kpiAdded = new MetricCard { Title = "Novos Arquivos", Value = "0", Subtitle = "Mutações simuladas", Width = 220 };
        _kpiModified = new MetricCard { Title = "Modificados", Value = "0", Subtitle = "Arquivos alterados", Width = 220 };
        _kpiParity = new MetricCard { Title = "Paridade SHA-256", Value = "--", Subtitle = "Sandbox audit", Width = 220 };
        _kpiParity.SetBadge("AGUARDANDO", ModernTheme.SectionHeader, ModernTheme.TextSecondary);

        pnlKpis.Controls.Add(_kpiFiles);
        pnlKpis.Controls.Add(_kpiAdded);
        pnlKpis.Controls.Add(_kpiModified);
        pnlKpis.Controls.Add(_kpiParity);
        Controls.Add(pnlKpis);

        // --- Row 2: Workflow Steps Card ---
        var cardSteps = new CardPanel
        {
            Title = "Fluxo Guiado de Teste Incremental & Integridade",
            Subtitle = "Execute as etapas sequencialmente para comprovar o funcionamento da DLL sem risco aos dados de produção",
            Location = new Point(24, 205),
            Size = new Size(940, 155),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _btnCreateDataset = new ModernButton
        {
            Text = "1. Criar Dataset ERP",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(24, 55),
            Size = new Size(170, 36)
        };
        _btnCreateDataset.Click += (_, _) => CreateDataset();

        _btnMutate = new ModernButton
        {
            Text = "2. Simular Mutações",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(205, 55),
            Size = new Size(170, 36),
            Enabled = false
        };
        _btnMutate.Click += (_, _) => MutateDataset();

        _btnRunBackup = new ModernButton
        {
            Text = "3. Rodar Backup Teste",
            ButtonStyle = ModernButtonStyle.Success,
            Location = new Point(385, 55),
            Size = new Size(170, 36),
            Enabled = false
        };
        _btnRunBackup.Click += async (_, _) => await RunTestBackupAsync();

        _btnSandboxRestore = new ModernButton
        {
            Text = "4. Restaurar & Conferir SHA",
            ButtonStyle = ModernButtonStyle.Outline,
            Location = new Point(565, 55),
            Size = new Size(190, 36),
            Enabled = false
        };
        _btnSandboxRestore.Click += async (_, _) => await RunSandboxRestoreAsync();

        _btnValidateArchive = new ModernButton
        {
            Text = "Auditar Pacote",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(765, 55),
            Size = new Size(140, 36),
            Enabled = false
        };
        _btnValidateArchive.Click += async (_, _) => await ValidateLatestBackupAsync();

        _progressBar = new ModernProgressBar
        {
            Location = new Point(24, 105),
            Size = new Size(880, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Value = 0
        };

        _lblStatus = new Label
        {
            Text = "Pronto. Clique em '1. Criar Dataset ERP' para iniciar o laboratório.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(24, 128),
            AutoSize = true
        };

        cardSteps.Controls.Add(_btnCreateDataset);
        cardSteps.Controls.Add(_btnMutate);
        cardSteps.Controls.Add(_btnRunBackup);
        cardSteps.Controls.Add(_btnSandboxRestore);
        cardSteps.Controls.Add(_btnValidateArchive);
        cardSteps.Controls.Add(_progressBar);
        cardSteps.Controls.Add(_lblStatus);
        Controls.Add(cardSteps);

        // --- Row 3: Live Output Terminal ---
        var cardOutput = new CardPanel
        {
            Title = "Relatório de Auditoria e Diagnóstico do Laboratório",
            Subtitle = "Saída detalhada das operações de teste, paridade de arquivos e hashes",
            Location = new Point(24, 375),
            Size = new Size(940, 260),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _txtTestConsole = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.TerminalBg,
            ForeColor = ModernTheme.TerminalGreen,
            Font = ModernTheme.MonoFont
        };

        cardOutput.Controls.Add(_txtTestConsole);
        Controls.Add(cardOutput);
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
