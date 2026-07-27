# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
Tirar a CLI do "funciona na máquina do Carlos": rodar em **qualquer computador**, contra
**TIA Project Server** (vários engenheiros no mesmo projeto ao mesmo tempo), e ter um `init` que
prepara a base inteira — máquina e projeto — sem coreografia manual.

## State
- HEAD: d6eee46 — working tree limpo. `rebuild.ps1` ALL PASS. Whitelist ok, canal sessão 0→1 ok.
- Done nesta sessão: `doctor` **6/6** contra o projeto de referência; 3 bugs reais corrigidos
  (`gen-profinet` nome de tag, `FcSuffix` do `replicate-instruments`, guard do `replicate-fc`);
  fixtures offline agora são **exports reais** (`scripts/export-fixtures.ps1`, 15 blocos/tabelas).
- In progress: nada mid-flight.

## Decisions (and why)
- **Régua**: CLI diverge do projeto de referência → a CLI está errada. Pegou 2 bugs que o próprio
  script FINAL também tem (nome de tag Profinet, sufixo `_ANALOGS` hardcoded).
- Fixture sintética esconde bug: a suíte offline concordava com o código errado. Toda fixture nova
  sai de `export-fixtures.ps1`, nunca escrita à mão.
- `TiaWhitelist` = task **do usuário** com `RunLevel Highest` + SDDL `FRFX` pro SID dele. Task de
  SYSTEM só aceita `Start-ScheduledTask` de token elevado → o agente levava "Acesso negado".
  Enfraquece o UAC de propósito (o usuário já é admin da máquina); revertível.
- `TiaSmokeRun` **tem** que ser `LogonType Interactive`. S4U roda em sessão própria e
  `TiaPortal.GetProcesses()` não enxerga o portal da sessão 1.
- `replicate-fc --apply` continua proibido neste projeto — 32 pastas completas viram `overwrite`.
  O guard novo bloqueia; `--force` é a válvula consciente.

## Next steps (ordered)
1. **Portabilidade (bloqueia todo o resto).** 14 caminhos `c:\Scripts\TIA Portal` hardcoded em 6
   scripts (`whitelist`, `setup-tasks`, `taskrun`, `tia-task`, `smokeloop`, `export-fixtures`) —
   derivar de `$PSScriptRoot`. Idem usuário (`$env:USERDOMAIN\$env:USERNAME`, já feito em
   setup-tasks) e versão do Portal (`Portal V21`, `lib\*.dll`): descobrir em runtime, não fixar.
2. **`scripts/init.ps1` = init da MÁQUINA** (hoje é folclore no CLAUDE.md): checa .NET + versão do
   TIA, resolve `lib\` a partir da instalação local do Openness, `setup-tasks`, whitelist,
   `rebuild`, `doctor`. Um comando, idempotente, saída "pronto / falta X". É o pré-requisito de
   "usável em todos os computadores".
3. **Multiuser / Project Server.** A API existe na DLL: namespace `Siemens.Engineering.Multiuser`
   com `LocalSession`, `ServerProjectInfo`, `GetServerProjects`, `OpenServerProject`,
   `CreateLocalSession`, `DeleteLocalSessionFromServer`, `MultiuserProject`, `MultiuserException`.
   Ordem sugerida: (a) `tia list-server-projects --server <host>` read-only pra provar o attach;
   (b) `open-session` / `close-session` (local session = cópia de trabalho); (c) refresh/commit com
   `MultiuserException` tratado como conflito, não como crash. **Toda escrita passa a ser commit
   numa sessão local, nunca no projeto do servidor direto.**
4. **Concorrência real.** D9 assume "um `tia` por vez, single-session" — vale por máquina, mas com
   N engenheiros: `--out workspace/` colide se dois rodarem no mesmo share; o guard do
   `replicate-fc` vira crítico; `doctor` deveria rodar **antes de cada commit**. Decidir se `tia`
   ganha lock por projeto e o que fazer quando o servidor já mudou o bloco que vamos importar.
5. **`tia init` / `scaffold` = init do PROJETO** ("preparar a base de forma completa"). Consome os
   exports de `workspace/padrao/`: árvore de pastas da lei de nomenclatura, `DB GLOBAL` com
   `HARDWARE_INTERRUPT`/`ALARMES_MODULOS`, os moldes (`MODULE_ERROR_MOLDE`, `OB_MOLDE_ALARMES`,
   `OB_MOLDE_PARTIDAS`, `MOLDE_ANALOGS`, `MOLDE TOT1`, `FC_Modelo`), o acionamento-modelo de 6
   blocos, tabelas `1. I/OS`…`5. Instrumentação`. Só depois disso os replicadores têm o que
   replicar num projeto novo.
6. **Verbo `audit`** — projeto qualquer contra a lei de nomenclatura: `(TAG)` na folha, 6 blocos por
   equipamento, 1 tabela por acionamento, N de área consistente entre `2.N`/`3.N`/`3.1.N`/`5.1.N`.
   Com multiusuário vira gate de commit, não só relatório.
7. `import-ladder` contra a verdade (FlgNet escrito de memória, nunca validado): gerar e comparar
   com `diff-block` contra um `PARTIDA_*` real de `workspace/padrao/`.
8. `replicate-fc --apply` nunca exercitado — testar num projeto vazio (aqui o guard barra, certo).

## Key files
- `docs/PADRAO.md` — estrutura do projeto de referência, lei de nomenclatura, os 3 bugs achados.
- `docs/PLANO.md` — decisões D1–D9 (D9 = single-session, revisar para multiuser).
- `scripts/setup-tasks.ps1` — tasks + ACL + whitelist; base do futuro `init.ps1`.
- `scripts/export-fixtures.ps1` — regenera as 15 fixtures reais (roda na sessão 1).
- `src/Tia.Core/Replicate.cs:37` — `Run(..., bool force)` e o guard de alvo populado.
- `src/Tia.Core/InstrumentFc.cs:24` — `FcSuffix`; `src/Tia.Core/Profinet.cs` — `TagName`.
- `src/Tia.Cli/Program.cs:193` — parsing de flags (`--apply`, `--force`).
- `lib/Siemens.Engineering.Base.dll` — contém o namespace `Multiuser` (verificado por strings).

## Open / blockers
- Nada bloqueando. Portal aberto no projeto de referência; smoke via
  `pwsh scripts/tia-task.ps1 <verbo>` funciona sem intervenção.
- A decidir com o usuário: host do Project Server para teste, e se há projeto de teste no servidor
  (nunca contra produção).
- Openness single-session continua valendo por máquina — não paralelizar `tia` local.
