# Arquitetura do Area Backup Engine

## 1. Visão Geral da Arquitetura

O **Area Backup Engine** foi concebido sob princípios rigorosos de **engenharia de software para resiliência e integridade de dados**, separando categoricamente as camadas de domínio, infraestrutura, acesso a dados, camada de banco de dados e fachadas de aplicação.

```mermaid
graph TD
    UI[ERP Desktop / WinForms / CLI] --> Facade[Area.Backup - BackupEngine]
    Facade --> Core[Area.Backup.Core - Contratos & Modelos]
    Facade --> Infra[Area.Backup.Infrastructure]
    Facade --> DB[Area.Backup.Database]

    subgraph "Camada de Infraestrutura"
        Scanner[FileSystemScanner & ExclusionMatcher]
        ChangeDetector[FileSystemChangeDetector]
        Checksum[Sha256ChecksumService]
        Storage[LocalFileSystemStorage & RepositoryLock]
        Compression[ZipCompressionProvider]
        Catalog[SqliteCatalogRepository]
        Manifest[JsonManifestService]
        Validator[IntegrityValidator]
        Restore[RestoreEngine]
        Retention[RetentionPolicyService]
    end

    subgraph "Provedores de Banco"
        FB[FirebirdBackupProvider - gbak]
        PG[PostgreSqlBackupProvider - pg_dump]
    end
```

---

## 2. Pipeline de Execução de Backup

O ciclo de criação de backup executa em 10 etapas sequenciais e assíncronas:

```mermaid
sequenceDiagram
    participant ERP as Aplicação ERP / CLI
    participant Engine as BackupEngine
    participant Lock as RepositoryLock
    participant Scanner as FileSystemScanner
    participant CD as FileSystemChangeDetector
    participant Comp as ZipCompressionProvider
    participant Val as IntegrityValidator
    participant Cat as SqliteCatalogRepository
    participant Ret as RetentionPolicyService

    ERP->>Engine: CreateBackupAsync(config, progress, token)
    Engine->>Lock: Acquire(RepositoryPath)
    Engine->>Cat: Resolver modo Auto (Full vs Incremental)
    Engine->>Scanner: ScanSourcesAsync(Fontes, Exclusões)
    Scanner-->>Engine: Lista de Arquivos Atuais
    Engine->>CD: DetectChangesAsync(Atuais, ManifestPai)
    CD-->>Engine: Deltas (Novos, Alterados, Excluídos)
    Engine->>Comp: Gravar fluxo direto em backup_id.tmp
    Comp-->>Engine: Arquivo temporário concluído
    Engine->>Val: ValidateAsync(backup_id.tmp, Quick/Full)
    Val-->>Engine: Integridade confirmada
    Engine->>Engine: Atomic Commit (rename .tmp -> .backup)
    Engine->>Cat: RegisterBackupAsync(metadados, manifest)
    Engine->>Ret: ApplyRetentionAsync(política)
    Engine->>Lock: Release
    Engine-->>ERP: BackupResult (Sucesso, Duração, Hashes)
```

---

## 3. Princípios Fundamentais de Design

1. **Prioridade Absoluta da Integridade**:
   - Integridade > Segurança > Restauração > Confiabilidade > Performance > Espaço.
   - Nenhum backup parcial é considerado válido. O backup é escrito em `.tmp` e só é promovido a `.backup` após validação completa.

2. **Zero Duplicação Intermediária de Pastas (Direct Streaming)**:
   - A leitura da origem é feita diretamente para o fluxo de compactação do arquivo de destino via buffers gerenciados (`64 KB`), sem copiar pastas para `C:\Temp` antes de compactar.

3. **Invariante de Retenção de Dependências**:
   - Um backup FULL **nunca** é excluído enquanto houver backups incrementais que dependam dele na cadeia de recuperação.

4. **Isolamento de Banco de Dados Ativo**:
   - Arquivos `.fdb` ou bases de dados em uso nunca são copiados via `File.Copy`. São acionadas as ferramentas nativas (`gbak`, `pg_dump`) para gerar dumps transacionais consistentes.
