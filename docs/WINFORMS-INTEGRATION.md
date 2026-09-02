# Integração com Windows Forms e ERP (`WINFORMS-INTEGRATION`)

Este documento orienta como acoplar o `Atual.Backup` diretamente na interface de um ERP Windows Forms.

## 1. Passo a Passo de Integração

1. Adicione a referência ao projeto `Atual.Backup` ou à DLL compilada.
2. Crie uma instância de `BackupEngine` no formulário.
3. Conecte o evento de progresso com `Progress<BackupProgress>` para atualizar os controles da tela na thread de UI do Windows Forms sem travamentos:

```csharp
private async void btnIniciarBackup_Click(object sender, EventArgs e)
{
    var config = new BackupConfiguration
    {
        RepositoryPath = @"C:\Backup\Atual",
        BackupType = BackupType.Auto,
        Sources = [new BackupSource(@"C:\ERP", "ERP_SRC")]
    };

    var engine = new BackupEngine();
    using var cts = new CancellationTokenSource();

    var progress = new Progress<BackupProgress>(p =>
    {
        lblStatus.Text = $"Status: {p.Stage}";
        progressBar.Value = (int)p.Percentage;
        lblPorcentagem.Text = $"{p.Percentage:F1}%";
        lblArquivos.Text = $"{p.FilesProcessed} / {p.FilesTotal}";
        lblArquivoAtual.Text = p.CurrentFile;
        lblTempo.Text = $"Tempo: {p.Elapsed:hh\\:mm\\:ss}";
    });

    btnIniciar.Enabled = false;
    btnCancelar.Enabled = true;

    try
    {
        var result = await engine.CreateBackupAsync(config, progress, cts.Token);
        MessageBox.Show("Backup concluído com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (OperationCanceledException)
    {
        lblStatus.Text = "Backup cancelado.";
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Erro: {ex.Message}", "Falha", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        btnIniciar.Enabled = true;
        btnCancelar.Enabled = false;
    }
}
```

Consulte o código completo no projeto de exemplo:
[samples/Atual.Backup.WinForms.Sample](file:///c:/AtualDev/Prototipo/area-backup/samples/Atual.Backup.WinForms.Sample)
