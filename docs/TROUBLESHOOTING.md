# Diagnóstico e Solução de Problemas (`TROUBLESHOOTING`)

Guia para solução de cenários excepcionais e resolução de dúvidas operacionais.

---

## 1. Erro: `BackupAlreadyRunningException`
- **Causa**: Outro processo ou rotina de backup está em execução no mesmo diretório de repositório (o arquivo `.backup.lock` está ativo).
- **Solução**: Aguarde a conclusão da operação anterior. Caso um processo anterior tenha sido finalizado abruptamente pelo gerenciador de tarefas do Windows, o arquivo `.backup.lock` é liberado automaticamente pelo sistema operacional no fechamento da handle.

---

## 2. Arquivos Bloqueados / Em Uso (`FILE_LOCKED`)
- **Comportamento Padrão**: Caso `Performance.FailOnLockedFile = false` (padrão), o motor registra um aviso `BackupWarning` no resultado e continua o processamento dos demais arquivos.
- **Caso Deseje Abortar**: Configure `Performance.FailOnLockedFile = true` para que qualquer bloqueio de arquivo lance imediatamente uma exceção e limpe os arquivos temporários.

---

## 3. Erro: `BackupIntegrityException`
- **Causa**: O manifesto foi alterado indevidamente ou os bytes do arquivo `.backup` foram corrompidos no disco (falha de hardware/bit-rot).
- **Diagnóstico**: Execute `Atual.Backup.Cli validate --backup <caminho> --mode Full` para listar exatamente quais arquivos divergiram em relação ao checksum SHA-256 original.

---

## 4. Limpeza de Resíduos Temporários
- Caso uma máquina seja desligada durante o backup, arquivos com extensão `.tmp` no repositório são automaticamente detectados e eliminados na próxima inicialização do motor através do método `IBackupStorage.CleanupTempFiles()`.
