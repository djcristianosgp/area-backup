# Performance, Streaming e USN Journal (`PERFORMANCE`)

O **Atual Backup Engine** foi projetado para operar com centenas de milhares de arquivos mantendo baixo consumo de memória RAM e preservando a vida útil e velocidade do subsistema de disco.

## 1. Zero Cópia Intermediária (Direct Streaming)

Ao contrário de abordagens ingênuas que copiam pastas para `%TEMP%` antes de compactar, o motor conecta os streams de leitura da origem diretamente aos streams de escrita do compressor:

```text
[Arquivo em Disco] ──(Buffer 64KB)──> [SHA-256 Hasher] ──> [ZIP Archive Stream] ──> [Arquivo .tmp]
```

- Nenhum arquivo é carregado integralmente na memória RAM (`File.ReadAllBytes` nunca é utilizado em arquivos de dados).
- Suporte nativo para arquivos com vários gigabytes sem estouro de memória (`OutOfMemoryException`).

---

## 2. Estratégia de Detecção em Camadas

Para evitar recomputar hashes SHA-256 de centenas de milhares de arquivos estáticos a cada backup:
1. **Verificação Nível 1**: `Caminho Relativo + Tamanho + LastWriteTimeUtc`.
2. Se todos forem idênticos, o arquivo é classificado como inalterado em microssegundos.
3. Se houver divergência de tamanho ou data, o hash criptográfico é computado para o novo pacote.

---

## 3. Integração com NTFS USN Journal

Em discos locais formatados em NTFS no Windows, o provedor `UsnJournalChangeSource` pode consultar o journal de alterações do sistema de arquivos NTFS para identificar arquivos modificados de maneira quase instantânea, com fallback automático para `FileSystemScanner` em unidades de rede (UNC) ou partições FAT32/exFAT.
