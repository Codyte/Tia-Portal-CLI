# Handoff · TIA Portal Openness API · 2026-07-17 (4)

## Goal
CLI .NET `tia` expondo Openness V19+ p/ agentes IA. Backlog v2 em andamento:
itens 1 (import-source) e 1b (import-ladder SCL→LAD) prontos. Próximo: item 2 (estrutura).

## State
- HEAD: b3b8b70
- Done: F0-F3 (9 verbos + sessão). Backlog v2: **import-source** (SCL/AWL/DB/UDT via
  GenerateBlocksFromSource, regex de nomes p/ dry-run) e **import-ladder** (conversor SCL→LAD).
- import-ladder: `LadConverter.cs` — lexer + parser recursivo-descendente (bool AND/OR/NOT/
  parênteses, comparadores = <> < > <= >=, IF→SCoil/RCoil, IF/ELSE→Coil, MOVE de literal),
  De Morgan empurra NOT pras folhas, emitter FlgNet v4 com UId wiring automático.
  Sem tipos Siemens de propósito: dry-run gera XML **sem TIA** (Program.Main desvia pra
  RunLadderDryRun ANTES de Run() — JIT de Run puxa a DLL Siemens; não mover pra dentro).
  Testado offline: 6 redes do docs/examples/ladder.scl, wiring conferido (selo paralelo+NC ok).
- Build 0 erros. Commits: 3bfeb58 (import-source), b3b8b70 (import-ladder).
- SEM smoke real: TIA V19 não instalado (user vai instalar em titanxnexus).

## Decisions (and why)
- D1-D9 PLANO valem; D8 (sem online) de pé.
- LAD não tem API de montar network no Openness → conversor próprio compila SCL subset
  pra SimaticML; lógica complexa fica em SCL via import-source (loops/CASE rejeitados
  com erro "sem equivalente LAD").
- import-ladder V1 só tags globais (sem #locais/interface) e FC only — corta parser de interface.
- RISCO ASSUMIDO: detalhes FlgNet escritos de memória (portas operand1/operand2 de comparador,
  en/in/out1 do MOVE, SrcType DInt default, MLT en-US) — validar no 1º smoke; ajustes ficam
  localizados no Emitter.

## Next steps (ordered)
1. Backlog v2 item 2 — estrutura: create/delete pasta de blocos e tag tables, delete-block,
   UDT export/import.
2. Item 3 — hardware: add-device (MLFB), set-address, subnet/IO-system; AML (CAx) em massa.
3. Item 4 — compile granular (--block/--folder) + diff-block (extrair BlocksIdentical de
   AlarmFc/InstrumentFc pra Ops).
4. Itens 5-10 do PLANO (inspeção, batch, robustez, libraries, online, HMI).
5. Pendente F3: /code-review dos 5 ports vs FINAIS.
6. Quando TIA instalar: smokes (projeto de teste, confirmar com user) — prioridade
   import-ladder --apply + round-trip export/compare, e import-source.
7. import-ladder V2 (se smoke ok): FB calls TON/CTU + instance DB, edge R_TRIG, copy tag→tag.

## Key files
- docs/PLANO.md — decisões + backlog v2 (itens 1/1b marcados feitos)
- src/Tia.Core/LadConverter.cs — conversor inteiro (lexer/parser/Normalize/Emitter/BuildBlockXml)
- src/Tia.Cli/Program.cs — RunLadderDryRun antes de Run(); cases import-ladder/import-source
- src/Tia.Core/Ops.cs:137 — ImportSource; ImportBlock reutilizado pelo apply do import-ladder
- docs/examples/ladder.scl, example.scl — exemplos dos dois verbos

## Open / blockers
- TIA V19 não instalado (bloqueia smokes).
- XML FlgNet não validado contra TIA real (risco listado acima).
- PATH PowerShell: `$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine")+";"+...("Path","User")` se dotnet sumir.
