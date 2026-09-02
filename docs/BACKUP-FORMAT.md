# Formato do Pacote e Manifesto (`BACKUP-FORMAT`)

Os arquivos de backup gerados possuem a extensão `.backup` e utilizam o padrão ZIP como formato físico de encapsulamento com compactação por streaming.

## 1. Estrutura Interna do Arquivo `.backup`

```text
20260902-143000-full.backup (ZIP)
│
├── manifest.json                             # Metadados, formato e hashes SHA-256
├── database/                                 # Dumps de banco (quando aplicável)
│   └── firebird_backup_20260902143000.fbk
└── files/                                    # Arquivos das fontes indexados por SourceId
    ├── SRC_ERP/
    │   ├── system.ini
    │   └── Data0001/
    │       └── clientes.dat
    └── SRC_DOCS/
        └── contratos/
            └── 2026_001.pdf
```

---

## 2. Estrutura do `manifest.json`

```json
{
  "formatVersion": 1,
  "engineVersion": "1.0.0",
  "backupId": "20260902-143000",
  "type": "Incremental",
  "createdAtUtc": "2026-09-02T14:30:00Z",
  "parentBackupId": "20260901-140000",
  "rootFullBackupId": "20260901-140000",
  "sources": [
    {
      "sourceId": "SRC_ERP",
      "originalPath": "C:\\ERP",
      "description": "Sistema ERP"
    }
  ],
  "files": [
    {
      "sourceId": "SRC_ERP",
      "relativePath": "Data0001\\clientes.dat",
      "size": 1048576,
      "lastWriteTimeUtc": "2026-09-02T14:28:10Z",
      "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "archiveEntryPath": "files/SRC_ERP/Data0001/clientes.dat",
      "changeType": "Modified"
    }
  ],
  "deletedFiles": [
    {
      "sourceId": "SRC_ERP",
      "relativePath": "Temp\\old_cache.tmp",
      "deletedAtUtc": "2026-09-02T14:30:00Z"
    }
  ],
  "databaseDumpEntryPath": "database/firebird_backup_20260902143000.fbk"
}
```

---

## 3. Versionamento do Formato

- `formatVersion`: número inteiro incremental que dita as regras de serialização do manifesto e layout de arquivos.
- A engine valida o `formatVersion` na abertura e rejeita versões superiores não suportadas com erro tipado `BackupIntegrityException`.
