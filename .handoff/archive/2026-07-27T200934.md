# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
CLI usável em **qualquer máquina**, contra **TIA Project Server** (vários engenheiros no mesmo
projeto), com `init` de máquina e de projeto — sem coreografia manual.

## State
- HEAD: 880ef5e — working tree limpo. `rebuild.ps1` ALL PASS (11 suítes + `Audit.Naming`).
- Done nesta sessão:
  - **Portabilidade** (bloqueava o resto): 14 caminhos `c:\Scripts\TIA Portal` viraram
    `$PSScriptRoot` em `whitelist`/`taskrun`/`smokeloop`/`setup-tasks`/`export-fixtures`.
    `grant-whitelist-acl.ps1` deletado (ACL com usuário fixo, obsoleto).
  - **`init.ps1` gate 4**: registra `TiaWhitelist`/`TiaSmokeRun` via `setup-tasks` elevado (1 UAC),
    idempotente, falha alto se não registrar. `init.ps1` roda limpo de ponta a ponta.
  - **`tia list-server-projects --server HOST [--port N] [--http] [--keep-connection]`** —
    multiuser read-only (passo 3a).
  - **`tia audit [--plc N] [--max 50]`** — projeto × lei de nomenclatura, 5 checks, sem config.
- In progress: nada mid-flight.

## Decisions (and why)
- API Multiuser levantada por **reflexão na DLL**, não de memória:
  `TiaPortal.ProjectServers` (`Create(name, Protocol, host, port)`), `GetServerProjects`,
  `GetLockStateProvider` (`IsProjectLocked`/`GetLockOwner`), `GetLocalSessions`,
  `CreateLocalSession(info, name, dir, SessionCreationMode)`, `LocalSessions.OpenServerProject(file)`,
  `LocalSession.CloseAndCommit(msg)`/`Save`/`IsUptoDate`/`Close`.
- `list-server-projects` roda **antes** do `TiaSession.Attach()`: precisa de portal, não de projeto
  aberto. Conexão que ele cria é removida no `finally` (`--keep-connection` mantém).
- `audit` lê o prefixo de área **por árvore** (`2.N`/`3.N` só em tags, `3.1.N`/`5.1.N` só em blocos).
  Sem isso `3.2 Comunicacao Profinet` (blocos) colide com `3.2 <Área>` (tags) e todo projeto
  conforme acusava conflito. `N=0` fora (molde/painéis).
- Régua do PADRAO aplicada nos dois sentidos: referência passa limpo (36 acionamentos, 5/5);
  AsBuilt acusa 69/69 sem `(TAG)` e 58 com ≠ 6 blocos. Check que não discrimina é check inútil.
- Escrita multiuser **não** foi escrita sem servidor pra testar — código não exercitável foi o que
  produziu os 3 bugs da sessão passada.

## Next steps (ordered)
1. **`scaffold` / `tia init` do PROJETO** (maior valor restante): consome `workspace/padrao/` —
   árvore de pastas da lei, `DB GLOBAL` com `HARDWARE_INTERRUPT`/`ALARMES_MODULOS`, os moldes
   (`MODULE_ERROR_MOLDE`, `OB_MOLDE_ALARMES`, `OB_MOLDE_PARTIDAS`, `MOLDE_ANALOGS`, `MOLDE TOT1`,
   `FC_Modelo`), acionamento-modelo de 6 blocos, tabelas `1. I/OS`…`5. Instrumentação`.
   Sem isso os replicadores não têm o que replicar num projeto novo. `audit` vira o teste de aceite.
2. `import-ladder` contra a verdade (FlgNet escrito de memória, nunca validado): gerar e comparar
   com `diff-block` contra um `PARTIDA_*` real de `workspace/padrao/`.
3. `replicate-fc --apply` nunca exercitado — testar em projeto vazio (no de referência o guard barra,
   certo; `--force` é a válvula consciente).
4. **Multiuser 3b/3c** (bloqueado por host): `open-session`/`close-session` (`CreateLocalSession` +
   `OpenServerProject`), depois refresh/commit com `MultiuserException` tratada como conflito.
   Toda escrita passa a ser commit numa sessão local, nunca no projeto do servidor direto.
5. **Concorrência** (D9 = single-session vale por máquina): com N engenheiros `--out workspace/`
   colide em share; `audit`+`doctor` deveriam rodar antes de cada commit; decidir lock por projeto.

## Key files
- `src/Tia.Core/Audit.cs` — 5 checks; `AreaConflicts` (prefixo por árvore), `NormalizeArea`.
- `src/Tia.Core/Multiuser.cs` — `ListServerProjects`, `ResolveServer` (reusa/cria+apaga conexão).
- `src/Tia.Cli/Program.cs:145` — `list-server-projects` antes do Attach; `case "audit"` no Dispatch.
- `src/Tia.Tests/Program.cs` — `Audit_Naming`, 10 asserts com strings reais do projeto.
- `scripts/init.ps1` — 4 gates (grupo Openness, dotnet, `lib/*.dll`, tasks) + rebuild.
- `docs/PADRAO.md` — lei de nomenclatura + seção nova `tia audit`.
- `docs/PLANO.md` — D1–D9 (D9 a revisar quando o multiuser escrever).

## Open / blockers
- **Falta o host do Project Server** (+ porta, http/https) e um projeto de **teste** no servidor,
  nunca produção — trava só o passo 4.
- Portal aberto no projeto de referência; smoke via `pwsh scripts/tia-task.ps1 <verbo>` funciona
  sem intervenção. Openness single-session continua valendo por máquina.
