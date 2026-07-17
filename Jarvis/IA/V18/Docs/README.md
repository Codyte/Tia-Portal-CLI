# Jarvis V18

Base inicial de utilitarios TIA Openness para o TIA Portal V18.

Arquivos incluidos:

- `tia-read-plc-name.cs`: conecta no TIA aberto e le nome do projeto e do CLP.
- `tia-check-session-status.cs`: conecta no TIA aberto e informa o estado da sessao Multiuser (`IsUptoDate`).
- `tia-dump-program-blocks.cs`: conecta no TIA aberto e lista a estrutura de `Program blocks`.
- `tia-create-tag-folder.cs`: conecta no TIA aberto e cria uma pasta nas tags internas do CLP.
- `tia-create-test-tags.cs`: garante uma pasta/tabela de teste e cria tags basicas no CLP.

Observacoes:

- Os utilitarios foram validados no projeto `Automação ETE Saco Grande RevIHM`.
- O acesso usa `TiaPortal.GetProcesses().First().Attach()`.
- Para executar fora do sandbox, o ambiente precisa permitir IPC do Openness.
- A base para evolucao futura do V19 pode reaproveitar a mesma estrutura, ajustando referencias de API e testes.
