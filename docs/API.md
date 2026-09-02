# Guia da API Pública — Atual Backup Engine

A fachada principal de utilização da biblioteca é a classe `BackupEngine`, que implementa a interface `IBackupEngine`.

## 1. Interface Principal (`IBackupEngine`)

```csharp
namespace Atual.Backup.Core.Interfaces;

public interface IBackupEngine
{
    BackupStatus Status { get; }

    event EventHandler<BackupProgress>? ProgressChanged;
    event EventHandler<BackupStage>? StageChanged;
    event EventHandler<BackupResult>? Completed;
    event EventHandler<BackupError>? Error;

    Task<BackupResult> CreateBackupAsync(
        BackupConfiguration configuration,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ValidationResult> ValidateBackupAsync(
        string backupPath,
        ValidationOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<RestoreResult> RestoreBackupAsync(
        string backupPath,
        RestoreOptions options,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    BackupInfo GetBackupInfo(string backupPath);

    Task<BackupCatalog> GetCatalogAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default);
}
```

---

## 2. Injeção de Dependência (DI)

Caso a aplicação utilize `Microsoft.Extensions.DependencyInjection`:

```csharp
using Atual.Backup.Extensions;

services.AddAtualBackup();
```

---

## 3. Exemplos de Código

### Criando um Backup com Progresso e Cancelamento
```csharp
var engine = new BackupEngine();
using var cts = new CancellationTokenSource();

var progress = new Progress<BackupProgress>(p =>
{
    Console.WriteLine($"Etapa: {p.Stage} | {p.Percentage:F1}% | Arquivo: {p.CurrentFile}");
});

var result = await engine.CreateBackupAsync(configuration, progress, cts.Token);
```

### Validando a Integridade Criptográfica
```csharp
var validation = await engine.ValidateBackupAsync(
    @"C:\Backup\Atual\2026\09\20260902-143000-full.backup",
    new ValidationOptions { Mode = ValidationMode.Full });

if (validation.IsValid)
{
    Console.WriteLine($"Backup íntegro com {validation.ValidFiles} arquivos validados.");
}
```

### Restaurando para uma Pasta Específica
```csharp
var restoreResult = await engine.RestoreBackupAsync(
    @"C:\Backup\Atual\2026\09\20260902-150000-incremental.backup",
    new RestoreOptions
    {
        DestinationPath = @"C:\ERP_Restaurado",
        OverwriteExisting = true
    });
```
