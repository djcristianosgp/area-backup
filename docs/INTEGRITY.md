# Integridade Criptográfica e Validação (`INTEGRITY`)

A integridade dos dados é o princípio de maior precedência no **Area Backup Engine**.

## 1. Modos de Validação (`ValidationMode`)

### `ValidationMode.Quick`
- Valida cabeçalho e estrutura do arquivo ZIP central directory.
- Extrai e valida a integridade sintática e versionamento do `manifest.json`.
- Confere a existência e quantidade de entradas declaradas no manifesto.
- Tempo de execução: **Sub-segundo**.

### `ValidationMode.Full`
- Executa todas as verificações do modo Quick.
- Abre o stream de leitura de **cada arquivo** no pacote e calcula o hash SHA-256 em tempo real.
- Compara com o hash original registrado no manifesto.
- Confere a integridade da cadeia de dependências até o Full raiz no catálogo.
- Tempo de execução: Proporcional ao tamanho dos arquivos adicionados/alterados.

---

## 2. Teste de Restauração em Sandbox (`PerformTestRestore`)

Permite simular uma extração completa para um diretório temporário isolado do sistema operacional (`%TEMP%\AtualRestoreTest_...`), validando o sucesso da escrita em disco antes de descartar o diretório temporário.

```csharp
var validation = await engine.ValidateBackupAsync(
    backupPath,
    new ValidationOptions
    {
        Mode = ValidationMode.Full,
        PerformTestRestore = true
    });
```
