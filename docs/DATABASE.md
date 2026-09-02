# Provedores de Banco de Dados (`DATABASE`)

O **Atual Backup Engine** possui uma camada de abstração dedicada para bancos de dados relacionais (`Atual.Backup.Database`), evitando o erro crítico de tentar copiar arquivos `.fdb` ou bases ativas em execução com `File.Copy`.

## 1. Firebird (`FirebirdBackupProvider`)

Utiliza o utilitário nativo `gbak` ou serviços do Firebird para gerar um arquivo de backup consistente `.fbk`.

```json
"database": {
  "enabled": true,
  "providerType": "Firebird",
  "databasePath": "C:\\ERP\\Data0001\\DADOS.FDB",
  "username": "SYSDBA",
  "password": "masterkey",
  "gbakPath": "C:\\Program Files\\Firebird\\Firebird_5_0\\gbak.exe"
}
```

O comando executado internamente é:
```bash
gbak -b -v -user SYSDBA -password masterkey "C:\ERP\Data0001\DADOS.FDB" "dump.fbk"
```

---

## 2. PostgreSQL (`PostgreSqlBackupProvider`)

Utiliza o utilitário oficial `pg_dump` para gerar um arquivo no formato de arquivo customizado (`-F c`).

```json
"database": {
  "enabled": true,
  "providerType": "PostgreSQL",
  "databaseName": "erp_production",
  "host": "localhost",
  "port": 5432,
  "username": "postgres",
  "password": "secretpassword"
}
```
