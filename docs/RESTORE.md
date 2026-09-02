# Mecanismo e Estratégia de Restauração (`RESTORE`)

O mecanismo de restauração do **Area Backup Engine** é capaz de reconstruir com fidelidade absoluta o estado do sistema em qualquer ponto no tempo histórico.

## 1. Resolução da Cadeia de Restauração

Ao solicitar a restauração de um backup incremental:
1. O engine lê o `manifest.json` do backup alvo.
2. Consulta o catálogo SQLite (`catalog.db`) para rastrear a cadeia cronológica completa:
   `FULL Base` $\to$ `INC 1` $\to$ `INC 2` $\to$ `...` $\to$ `INC Alvo`.
3. Executa a sobreposição sequencial ordenada:
   - Extrai a base FULL.
   - Aplica as atualizações e novos arquivos de cada incremental em ordem cronológica.
   - Exclui arquivos que foram marcados como deletados (`DeletedFiles`) nos pontos intermediários.
4. Caso configurado, realiza a verificação de hash SHA-256 em todos os arquivos extraídos após a operação.

```mermaid
graph LR
    Full[FULL 01/09] --> Inc1[INC 02/09]
    Inc1 --> Inc2[INC 03/09]
    Inc2 --> Inc3[INC 04/09]
    Inc3 --> State[Estado Reconstruído em 04/09]
```

---

## 2. Opções de Restauração (`RestoreOptions`)

```csharp
var options = new RestoreOptions
{
    // Diretório base para extração
    DestinationPath = @"C:\ERP_Restaurado",

    // Restaurar apenas uma fonte específica (ex: apenas ERP)
    SourceId = "SRC_ERP",

    // Filtrar apenas subpasta (ex: "Bitmaps\")
    RelativePathFilter = "Bitmaps",

    // Sobrescrever arquivos existentes no destino
    OverwriteExisting = true,

    // Aplicar registros de exclusão dos incrementais
    ApplyDeletions = true,

    // Verificar hashes SHA-256 pós-restauração
    VerifyChecksumsAfterRestore = true
};
```
