# Referência de Configuração (`BackupConfiguration`)

O objeto `BackupConfiguration` centraliza todas as diretrizes de execução do backup.

```json
{
  "repositoryPath": "C:\\Backup\\Atual",
  "backupType": "Auto",
  "sources": [
    {
      "id": "ERP",
      "path": "C:\\ERP",
      "description": "Sistema ERP Principal"
    },
    {
      "id": "DOCUMENTOS",
      "path": "D:\\Documentos\\ERP",
      "description": "Documentos e PDF"
    }
  ],
  "exclusions": [
    { "pattern": "*.tmp", "type": "Pattern" },
    { "pattern": "*.log", "type": "Pattern" },
    { "pattern": "Temp", "type": "DirectoryName" },
    { "pattern": "Cache", "type": "DirectoryName" }
  ],
  "incremental": {
    "enabled": true,
    "maxIncrementalBackups": 7,
    "maxDaysSinceFull": 7,
    "useUsnJournal": true,
    "useHashValidation": true
  },
  "compression": {
    "algorithm": "Zip",
    "level": "Optimal",
    "deduplicationEnabled": false
  },
  "validation": {
    "mode": "Quick",
    "validateAfterBackup": true,
    "performTestRestore": false
  },
  "retention": {
    "enabled": true,
    "keepFullBackups": 4,
    "keepIncrementalBackups": 30,
    "maxDays": 30,
    "maxStorageSizeBytes": 0
  },
  "performance": {
    "maxDegreeOfParallelism": 8,
    "bufferSize": 65536,
    "enableParallelScanning": true,
    "enableParallelHashing": true,
    "failOnLockedFile": false
  },
  "database": {
    "enabled": false,
    "providerType": "Firebird",
    "databasePath": "C:\\ERP\\Data0001\\DADOS.FDB",
    "username": "SYSDBA",
    "password": "masterkey"
  }
}
```

---

## Detalhamento das Seções

### `sources` (Lista de Origens)
- `id`: Identificador estável e único da fonte (usado internamente no manifesto e na restauração).
- `path`: Caminho absoluto ou caminho de rede UNC (`\\servidor\compartilhamento`).

### `exclusions` (Regras de Exclusão)
- Tipos de exclusão suportados:
  - `Pattern`: Coringa simples (`*.tmp`, `*.bak`, `log_*.txt`).
  - `DirectoryName`: Nome de pasta em qualquer profundidade (`Temp`, `Cache`, `Logs`).
  - `Extension`: Extensão de arquivo (`.tmp`, `.log`).
  - `ExactPath`: Caminho exato absoluto no disco.

### `incremental` (Diretrizes de Incrementais)
- `maxIncrementalBackups`: Quantidade máxima de backups incrementais em sequência antes de disparar um novo Full no modo `Auto`.
- `maxDaysSinceFull`: Número máximo de dias desde o último Full antes de forçar um novo Full no modo `Auto`.

### `retention` (Política de Retenção)
- `keepFullBackups`: Quantidade de backups Full a preservar.
- `keepIncrementalBackups`: Quantidade máxima de incrementais a reter.
- `maxDays`: Idade limite em dias para purga.
- *Garantia*: Nunca exclui um Full que ainda tenha incrementais ativos dependendo dele.
