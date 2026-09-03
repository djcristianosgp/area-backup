using Area.Backup.Core.Enums;
using Area.Backup.Core.Models;
using Area.Backup.WinForms.Sample.Controls;
using Area.Backup.WinForms.Sample.Theme;

namespace Area.Backup.WinForms.Sample.Views;

public sealed class DashboardView : UserControl
{
    private readonly BackupEngine _engine;
    private readonly Func<BackupConfiguration> _getConfigFunc;
    private readonly Action<string, string> _logAction;
    private CancellationTokenSource? _cts;

    // Controls
    private MetricCard _kpiStatus = null!;
    private MetricCard _kpiRepo = null!;
    private MetricCard _kpiLastBackup = null!;
    private MetricCard _kpiEfficiency = null!;

    private RadioButton _rbAuto = null!;
    private RadioButton _rbIncremental = null!;
    private RadioButton _rbFull = null!;
    private ModernButton _btnStart = null!;
    private ModernButton _btnCancel = null!;

    private Label _lblStageBadge = null!;
    private Label _lblStageDesc = null!;
    private ModernProgressBar _progressBar = null!;
    private Label _lblSpeed = null!;
    private Label _lblFiles = null!;
    private Label _lblBytes = null!;
    private Label _lblEta = null!;
    private Label _lblCurrentFile = null!;

    public DashboardView(BackupEngine engine, Func<BackupConfiguration> getConfigFunc, Action<string, string> logAction)
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
            Text = "Dashboard de Operações",
            Font = ModernTheme.TitleFont,
            ForeColor = ModernTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(24, 20)
        };
        var lblSubtitle = new Label
        {
            Text = "Visão geral em tempo real, telemetria da engine e disparo de rotinas de backup.",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 52)
        };
        Controls.Add(lblTitle);
        Controls.Add(lblSubtitle);

        // --- Row 1: KPI Cards ---
        var pnlKpis = new FlowLayoutPanel
        {
            Location = new Point(24, 85),
            Size = new Size(880, 105),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            WrapContents = false,
            AutoScroll = false
        };

        _kpiStatus = new MetricCard
        {
            Title = "Status da Engine",
            Value = "Pronto",
            Subtitle = "Aguardando tarefas",
            Width = 205,
            Height = 95
        };
        _kpiStatus.SetBadge("ONLINE", ModernTheme.SuccessLight, ModernTheme.Success);

        _kpiRepo = new MetricCard
        {
            Title = "Repositório",
            Value = "Configurado",
            Subtitle = "Pronto para gravação",
            Width = 215,
            Height = 95
        };

        _kpiLastBackup = new MetricCard
        {
            Title = "Último Backup",
            Value = "--",
            Subtitle = "Nenhum nesta sessão",
            Width = 215,
            Height = 95
        };

        _kpiEfficiency = new MetricCard
        {
            Title = "Integridade",
            Value = "100%",
            Subtitle = "SHA-256 verificado",
            Width = 215,
            Height = 95
        };
        _kpiEfficiency.SetBadge("SEGURO", ModernTheme.PrimaryLight, ModernTheme.Primary);

        pnlKpis.Controls.Add(_kpiStatus);
        pnlKpis.Controls.Add(_kpiRepo);
        pnlKpis.Controls.Add(_kpiLastBackup);
        pnlKpis.Controls.Add(_kpiEfficiency);
        Controls.Add(pnlKpis);

        // --- Row 2: Action & Execution Card ---
        var cardAction = new CardPanel
        {
            Title = "Executar Rotina de Backup",
            Subtitle = "Selecione a modalidade e dispare a engine de proteção",
            Location = new Point(24, 205),
            Size = new Size(880, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _rbAuto = new RadioButton
        {
            Text = "Modo Automático (Inteligente - decide entre Incremental e Full)",
            Font = ModernTheme.BodyBold,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(24, 55),
            Size = new Size(480, 24),
            Checked = true
        };
        _rbIncremental = new RadioButton
        {
            Text = "Incremental Forçado",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(510, 55),
            Size = new Size(160, 24)
        };
        _rbFull = new RadioButton
        {
            Text = "Completo (Full)",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(680, 55),
            Size = new Size(160, 24)
        };

        _btnStart = new ModernButton
        {
            Text = "▶  Iniciar Backup",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(24, 90),
            Size = new Size(160, 36)
        };
        _btnStart.Click += async (_, _) => await RunBackupAsync();

        _btnCancel = new ModernButton
        {
            Text = "✖  Cancelar",
            ButtonStyle = ModernButtonStyle.Danger,
            Location = new Point(195, 90),
            Size = new Size(120, 36),
            Enabled = false
        };
        _btnCancel.Click += (_, _) => _cts?.Cancel();

        cardAction.Controls.Add(_rbAuto);
        cardAction.Controls.Add(_rbIncremental);
        cardAction.Controls.Add(_rbFull);
        cardAction.Controls.Add(_btnStart);
        cardAction.Controls.Add(_btnCancel);
        Controls.Add(cardAction);

        // --- Row 3: Live Telemetry Card ---
        var cardTelemetry = new CardPanel
        {
            Title = "Telemetria & Progresso em Tempo Real",
            Subtitle = "Métricas em streaming direto da engine de backup",
            Location = new Point(24, 360),
            Size = new Size(880, 240),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _lblStageBadge = new Label
        {
            Text = "IDLE",
            Font = ModernTheme.SmallFont,
            BackColor = ModernTheme.SectionHeader,
            ForeColor = ModernTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(24, 55),
            Size = new Size(110, 24)
        };

        _lblStageDesc = new Label
        {
            Text = "Aguardando inicialização da rotina...",
            Font = ModernTheme.BodyBold,
            ForeColor = ModernTheme.TextPrimary,
            Location = new Point(145, 57),
            AutoSize = true
        };

        _progressBar = new ModernProgressBar
        {
            Location = new Point(24, 88),
            Size = new Size(830, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Value = 0
        };

        _lblSpeed = new Label
        {
            Text = "Velocidade: 0.0 MB/s",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(24, 125),
            AutoSize = true
        };

        _lblFiles = new Label
        {
            Text = "Arquivos: 0 / 0",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(220, 125),
            AutoSize = true
        };

        _lblBytes = new Label
        {
            Text = "Volume: 0 MB / 0 MB",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(400, 125),
            AutoSize = true
        };

        _lblEta = new Label
        {
            Text = "Tempo: 00:00:00 | Restante: --:--:--",
            Font = ModernTheme.BodyFont,
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(620, 125),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _lblCurrentFile = new Label
        {
            Text = "Arquivo: --",
            Font = ModernTheme.MonoSmallFont,
            ForeColor = ModernTheme.TextMuted,
            Location = new Point(24, 160),
            Size = new Size(830, 22),
            AutoEllipsis = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        cardTelemetry.Controls.Add(_lblStageBadge);
        cardTelemetry.Controls.Add(_lblStageDesc);
        cardTelemetry.Controls.Add(_progressBar);
        cardTelemetry.Controls.Add(_lblSpeed);
        cardTelemetry.Controls.Add(_lblFiles);
        cardTelemetry.Controls.Add(_lblBytes);
        cardTelemetry.Controls.Add(_lblEta);
        cardTelemetry.Controls.Add(_lblCurrentFile);
        Controls.Add(cardTelemetry);
    }

    public async Task RunBackupAsync()
    {
        var config = _getConfigFunc();
        if (string.IsNullOrWhiteSpace(config.RepositoryPath))
        {
            MessageBox.Show("Por favor, configure o Repositório na aba 'Configuração' antes de iniciar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (config.Sources.Count == 0)
        {
            MessageBox.Show("Adicione pelo menos uma pasta de Origem na aba 'Configuração'.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        config.BackupType = _rbFull.Checked ? BackupType.Full : (_rbIncremental.Checked ? BackupType.Incremental : BackupType.Auto);

        _btnStart.Enabled = false;
        _btnCancel.Enabled = true;
        _progressBar.Value = 0;
        _kpiStatus.Value = "Em Execução";
        _kpiStatus.SetBadge("RUNNING", ModernTheme.WarningLight, ModernTheme.Warning);

        _cts = new CancellationTokenSource();
        _logAction("INFO", $"Disparando backup ({config.BackupType}) no repositório {config.RepositoryPath}");

        var progress = new Progress<BackupProgress>(p =>
        {
            UpdateTelemetry(p);
        });

        try
        {
            var result = await _engine.CreateBackupAsync(config, progress, _cts.Token);

            _kpiStatus.Value = "Concluído";
            _kpiStatus.SetBadge("SUCESSO", ModernTheme.SuccessLight, ModernTheme.Success);
            _kpiLastBackup.Value = $"{result.CompressedSize / (1024.0 * 1024.0):F1} MB";
            _kpiLastBackup.Subtitle = $"{result.Type} em {result.Duration.TotalSeconds:F1}s";

            _lblStageBadge.Text = "FINALIZADO";
            _lblStageBadge.BackColor = ModernTheme.SuccessLight;
            _lblStageBadge.ForeColor = ModernTheme.Success;
            _lblStageDesc.Text = $"Backup {result.BackupId} gerado com integridade 100% validada!";

            _logAction("SUCCESS", $"Backup finalizado com sucesso! ID: {result.BackupId} | Tipo: {result.Type} | Tamanho: {result.CompressedSize / (1024.0 * 1024.0):F2} MB");

            MessageBox.Show(
                $"✓ BACKUP EXECUTADO COM SUCESSO!\n\n" +
                $"• ID do Backup: {result.BackupId}\n" +
                $"• Modalidade: {result.Type}\n" +
                $"• Arquivos no Pacote: {result.FilesAdded + result.FilesModified} (+{result.FilesAdded} novos, ~{result.FilesModified} mod, -{result.FilesDeleted} del)\n" +
                $"• Tamanho Compactado: {result.CompressedSize / (1024.0 * 1024.0):F2} MB\n" +
                $"• Duração: {result.Duration:hh\\:mm\\:ss}\n" +
                $"• Integridade Criptográfica: Válida (SHA-256)",
                "Backup Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            _kpiStatus.Value = "Cancelado";
            _kpiStatus.SetBadge("CANCELADO", ModernTheme.DangerLight, ModernTheme.Danger);
            _lblStageBadge.Text = "CANCELADO";
            _lblStageBadge.BackColor = ModernTheme.DangerLight;
            _lblStageBadge.ForeColor = ModernTheme.Danger;
            _lblStageDesc.Text = "A operação de backup foi interrompida pelo usuário.";
            _logAction("WARN", "Backup cancelado pelo operador.");
        }
        catch (Exception ex)
        {
            _kpiStatus.Value = "Erro";
            _kpiStatus.SetBadge("FALHA", ModernTheme.DangerLight, ModernTheme.Danger);
            _lblStageBadge.Text = "ERRO";
            _lblStageBadge.BackColor = ModernTheme.DangerLight;
            _lblStageBadge.ForeColor = ModernTheme.Danger;
            _lblStageDesc.Text = $"Erro: {ex.Message}";
            _logAction("ERROR", $"Falha crítica no backup: {ex.Message}");
            MessageBox.Show($"Ocorreu um erro ao processar o backup:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnStart.Enabled = true;
            _btnCancel.Enabled = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void UpdateTelemetry(BackupProgress p)
    {
        _lblStageBadge.Text = p.Stage.ToString().ToUpperInvariant();
        _lblStageBadge.BackColor = ModernTheme.PrimaryLight;
        _lblStageBadge.ForeColor = ModernTheme.Primary;

        _lblStageDesc.Text = p.Stage switch
        {
            BackupStage.Initializing => "Inicializando catálogo e repositório...",
            BackupStage.Scanning => "Varrendo diretórios de origem e calculando deltas...",
            BackupStage.DetectingChanges => "Comparando hashes e detectando arquivos modificados...",
            BackupStage.Reading => "Lendo dados e gerando hashes SHA-256...",
            BackupStage.Compressing => "Compactando dados em streaming para o arquivo .tmp...",
            BackupStage.Writing => "Finalizando escrita do pacote...",
            BackupStage.Validating => "Validando integridade criptográfica pós-gravação...",
            BackupStage.Finalizing => "Promovendo .tmp para .backup e registrando no catálogo...",
            BackupStage.Completed => "Backup finalizado com sucesso!",
            _ => p.Stage.ToString()
        };

        _progressBar.Value = (int)Math.Clamp(p.Percentage, 0, 100);
        _lblSpeed.Text = $"Velocidade: {p.SpeedBytesPerSecond / (1024.0 * 1024.0):F1} MB/s";
        _lblFiles.Text = $"Arquivos: {p.FilesProcessed:N0} / {p.FilesTotal:N0}";
        _lblBytes.Text = $"Volume: {p.BytesProcessed / (1024.0 * 1024.0):F1} MB / {p.BytesTotal / (1024.0 * 1024.0):F1} MB";
        _lblEta.Text = $"Tempo: {p.Elapsed:hh\\:mm\\:ss} | Restante: {(p.EstimatedRemaining.HasValue ? p.EstimatedRemaining.Value.ToString(@"hh\:mm\:ss") : "--:--:--")}";
        _lblCurrentFile.Text = $"Arquivo: {p.CurrentFile}";
    }
}
