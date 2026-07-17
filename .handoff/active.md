# Handoff · TIA Portal Openness API · 2026-07-17 (3)

## Goal
CLI .NET `tia` expondo Openness V19+ p/ agentes IA. F3 código 100%.
Próxima frente: **backlog v2** — começar por `import-source` (SCL/AWL).

## State
- HEAD: a94758b
- Done: F0-F2; F3 código 100% (9 verbos: info/list-*/export-*/import-*/compile +
  gen-profinet, standardize-tags, gen-fault-ob, replicate-fc, gen-alarm-fc,
  replicate-instruments) + sessão (open-project [--no-ui], save-project, close-project [--save]).
  Build 0 erros. Backlog v2 priorizado em docs/PLANO.md.
- In progress: nada mid-flight.
- SEM smoke: TIA V19 ainda não instalado (user vai instalar em titanxnexus).
- Pendente de F3: /code-review dos 5 ports (Standardize, FaultOb, Replicate, AlarmFc,
  InstrumentFc) vs originais em FINAIS — riscos: regex/ordem de dicionários, dry-run
  sem mutação, XML rewiring.

## Decisions (and why)
- D1-D9 PLANO travadas; D4 revisado (open suportado, headless via --no-ui);
  D8 (sem online) de pé — revogar só com decisão explícita do user.
- 3 variantes Replicador = 3 verbos (algoritmos distintos).
- Desvios dos originais: sem auto-compile, sem menu interativo, sem cache de template em disco.

## Next steps (ordered) — backlog v2 do PLANO
1. **import-source**: SCL/AWL via `plc.ExternalSourceGroup.CreateFromFile` +
   `GenerateBlocksFromSource` — IA escreve SCL texto puro. Verbo:
   `tia import-source --file X.scl [--apply]`. Maior alavanca.
2. Estrutura: create/delete pasta de blocos e tag tables, delete-block, UDT export/import.
3. Hardware: add-device (MLFB), set-address (IP/Profinet name), subnet/IO-system; AML (CAx) em massa.
4. compile granular (--block/--folder) + diff-block (comparador XML já existe em
   AlarmFc/InstrumentFc.BlocksIdentical — extrair pra Ops e expor).
5. Inspeção: find, cross-references, snapshot (inventário 1 JSON).
6. Batch: tia run --script ops.json (1 attach, N verbos).
7. Robustez: retry portal ocupado, timeout, exit codes por categoria.
8. Libraries: master copies/types.
9. Online (revoga D8 — só com ok explícito do user).
10. HMI Unified.
+ Intercalar: code-review F3 (item pendente) e smokes quando TIA instalado
  (SEMPRE projeto de teste, confirmar com user).

## Key files
- docs/PLANO.md — decisões + Backlog v2 (LER PRIMEIRO)
- src/Tia.Core/TiaSession.cs — attach, OpenProject/Save/CloseProject, ExclusiveAccess, GetPlc
- src/Tia.Core/Ops.cs — export/import XML, compile, FindBlock/ResolveFolder
- src/Tia.Core/{Standardize,FaultOb,Replicate,AlarmFc,InstrumentFc,Profinet}.cs — ports F3
- src/Tia.Cli/Program.cs — switch de verbos (open-project roda antes do Attach)
- docs/examples/*.json — configs exemplo

## Open / blockers
- TIA V19 não instalado (bloqueia todo smoke).
- PATH PowerShell: `$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine")+";"+...("Path","User")` se dotnet sumir.
- Multiuser: save/close só single-user (check-in via TIA).
