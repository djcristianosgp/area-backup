# Atual Backup Engine (.NET 10)

Uma biblioteca de classe e engine profissional de **backup incremental e recuperação de desastres** em C# / .NET 10, projetada com foco inegociável em **integridade criptográfica, capacidade de restauração e alta performance** para sistemas ERP desktop (Windows Forms, WPF) e serviços em segundo plano.

---

## 🚀 Principais Recursos

- **Backup FULL e INCREMENTAL**: backups completos e incrementais reais que processam exclusivamente arquivos novos, alterados e excluídos, sem reprocessar gigabytes de dados estáticos.
- **Modo Automático (`Auto`)**: decide inteligentemente quando executar Incremental ou forçar um novo Full com base em idade máxima (`MaxDaysSinceFull`) e contagem da cadeia (`MaxIncrementalBackups`).
- **Múltiplas Fontes Sem Duplicação**: suporte simultâneo a múltiplas pastas (`C:\ERP`, `D:\Documentos`, `E:\XML`, `\\Servidor\Share`) com leitura e compactação em *streaming direto*, sem copiar previamente para pastas temporárias (`temp`).
- **Detecção de Alterações em Camadas**:
  1. Comparação ultrarrápida: `RelativePath` + `Size` + `LastWriteTimeUtc`.
  2. Validação criptográfica opcional de SHA-256 para confirmação de integridade.
  3. Abstração e suporte ao **NTFS USN Journal** com fallback transparente para varredura tradicional.
- **Catálogo Persistente SQLite**: histórico completo de pontos de recuperação, versões de arquivos e relações de cadeia armazenados em `catalog.db` indexado.
- **Manifesto JSON Versionado (`manifest.json`)**: cada pacote contém um manifesto declarando fontes, arquivos, hashes SHA-256, exclusões e metadados.
- **Atomicidade e Segurança de Escrita**: pacotes são gerados como `.tmp` e promovidos atomicamente a `.backup` somente após validação de integridade. Falhas ou cancelamentos eliminam imediatamente resíduos temporários.
- **Validação de Integridade Criptográfica**:
  - `ValidationMode.Quick`: validação estrutural do pacote, cabeçalhos e integridade do manifesto.
  - `ValidationMode.Full`: validação de cada entrada com recomputação de SHA-256 em streaming e conferência da cadeia de dependências.
  - *Simulated Restore Test*: extração em sandbox temporário para comprovar capacidade real de restauração.
- **Restauração Ponto no Tempo (Full + Incrementais)**: reconstrução exata do estado do sistema em qualquer ponto histórico, com suporte a filtros por fonte ou diretório e aplicação correta de exclusões de arquivos.
- **Política de Retenção com Preservação de Invariantes**: expurgo inteligente que **nunca** exclui um backup FULL se houver backups incrementais ativos dependendo dele.
- **Provedores de Banco de Dados**: suporte nativo a dumps de banco (Firebird via `gbak` e PostgreSQL via `pg_dump`) sem travamento de arquivos `.fdb` ativos.
- **Acompanhamento de Progresso e Cancelamento**: eventos ricos (`ProgressChanged`, `StageChanged`, `Completed`, `Error`) e suporte a `IProgress<BackupProgress>` com `CancellationToken`.
- **Prevenção de Concorrência**: bloqueio exclusivo no repositório (`RepositoryLock`) impedindo múltiplos backups simultâneos no mesmo diretório.

---

## 📂 Estrutura da Solução

```text
Atual.Backup.slnx
│
├── src/
│   ├── Atual.Backup.Core/            # Interfaces, modelos, enums, contratos e exceções
│   ├── Atual.Backup.Infrastructure/  # Scanner, Change Detection, Catálogo SQLite, Manifest, Zip, Storage
│   ├── Atual.Backup.Database/        # Provedores de banco (Firebird, PostgreSQL)
│   ├── Atual.Backup/                 # Fachada pública (BackupEngine) e injeção de dependência
│   └── Atual.Backup.Cli/             # Ferramenta CLI de linha de comando
│
├── tests/
│   ├── Atual.Backup.UnitTests/       # Testes unitários (Scanner, Exclusões, Hash, SQLite, Retenção, Manifest)
│   └── Atual.Backup.IntegrationTests/# Testes de integração (Full, Incremental, Restore, Corrupção, Concorrência)
│
├── samples/
│   ├── Atual.Backup.WinForms.Sample/ # Exemplo real de integração com Windows Forms
│   └── config.sample.json            # Exemplo de configuração JSON
│
└── docs/                             # Documentação técnica completa
```

---

## 🛠️ Como Compilar e Testar

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ou superior.
- Windows 10 / 11 / Server (x64).

### Compilação da Solução
```powershell
dotnet build Atual.Backup.slnx -c Release
```

### Execução dos Testes Automatizados
```powershell
dotnet test Atual.Backup.slnx -c Release --verbosity normal
```

### Publicação da DLL
```powershell
dotnet publish src/Atual.Backup/Atual.Backup.csproj -c Release -o ./publish/lib
```

---

## 💻 Exemplo de Uso no ERP (C#)

```csharp
using Atual.Backup;
using Atual.Backup.Core.Enums;
using Atual.Backup.Core.Models;

// 1. Configurar o Backup
var configuration = new BackupConfiguration
{
    RepositoryPath = @"C:\Backup\Atual",
    BackupType = BackupType.Auto,
    Sources =
    [
        new BackupSource(@"C:\ERP", "ERP_PRINCIPAL", "Sistema ERP"),
        new BackupSource(@"D:\Documentos\ERP", "ERP_DOCS", "Documentos e PDF"),
        new BackupSource(@"E:\XML", "ERP_XML", "Notas Fiscais Eletrônicas")
    ],
    Exclusions =
    [
        new BackupExclusion("*.tmp"),
        new BackupExclusion("*.log"),
        new BackupExclusion("Temp", ExclusionType.DirectoryName)
    ],
    Incremental = new IncrementalOptions
    {
        Enabled = true,
        MaxIncrementalBackups = 7,
        MaxDaysSinceFull = 7
    },
    Compression = new CompressionOptions
    {
        Algorithm = CompressionAlgorithm.Zip,
        Level = CompressionLevel.Optimal
    },
    Validation = new ValidationOptions
    {
        ValidateAfterBackup = true,
        Mode = ValidationMode.Quick
    },
    Retention = new RetentionPolicy
    {
        Enabled = true,
        KeepFullBackups = 4,
        KeepIncrementalBackups = 30
    }
};

// 2. Instanciar a Engine
var engine = new BackupEngine();

// 3. Acompanhar o Progresso
var progress = new Progress<BackupProgress>(p =>
{
    Console.WriteLine($"[{p.Stage}] {p.Percentage:F1}% - Arquivo: {p.CurrentFile}");
});

// 4. Executar o Backup de forma Assíncrona
var result = await engine.CreateBackupAsync(configuration, progress, cancellationToken);

if (result.Success)
{
    Console.WriteLine($"Backup {result.BackupId} ({result.Type}) finalizado com sucesso!");
    Console.WriteLine($"Tamanho: {result.CompressedSize / (1024.0 * 1024.0):F2} MB");
}
```

---

## 🖥️ Utilizando a Ferramenta CLI (`Atual.Backup.Cli`)

A ferramenta de linha de comando permite testar e automatizar rotinas:

```powershell
# Executar backup com base em JSON
Atual.Backup.Cli backup --config ./samples/config.sample.json

# Validar integridade com verificação criptográfica SHA-256
Atual.Backup.Cli validate --backup C:\Backup\Atual\2026\09\20260902-143000-full.backup --mode Full

# Restaurar ponto de recuperação para uma pasta limpa
Atual.Backup.Cli restore --backup C:\Backup\Atual\2026\09\20260902-150000-incremental.backup --destination C:\Restore\Teste

# Obter informações do cabeçalho do arquivo .backup
Atual.Backup.Cli info --backup C:\Backup\Atual\2026\09\20260902-143000-full.backup

# Listar histórico de backups do repositório
Atual.Backup.Cli list --repository C:\Backup\Atual

# Executar benchmark sintético de performance (Full vs Incremental)
Atual.Backup.Cli benchmark --files 2000 --size 32
```

---

## 📚 Documentação Técnica Detalhada

- 🏛️ [Arquitetura do Sistema](docs/ARCHITECTURE.md)
- 🔌 [Guia da API Pública](docs/API.md)
- ⚙️ [Referência de Configurações](docs/CONFIGURATION.md)
- 📦 [Formato do Pacote e Manifesto](docs/BACKUP-FORMAT.md)
- 🔄 [Mecanismo e Estratégia de Restauração](docs/RESTORE.md)
- 🛡️ [Integridade e Criptografia](docs/INTEGRITY.md)
- ⚡ [Performance, Streaming e USN Journal](docs/PERFORMANCE.md)
- 🗄️ [Provedores de Banco de Dados (Firebird/PostgreSQL)](docs/DATABASE.md)
- 🖼️ [Integração com Windows Forms e ERPs](docs/WINFORMS-INTEGRATION.md)
- 🔧 [Diagnóstico e Solução de Problemas (Troubleshooting)](docs/TROUBLESHOOTING.md)

---

## 📄 Licença
Propriedade de **Atual Sistemas**. Uso e distribuição reservados para produtos do ecossistema Atual.
