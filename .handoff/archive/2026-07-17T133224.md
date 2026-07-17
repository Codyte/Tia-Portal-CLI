# Handoff · TIA Portal Openness API · 2026-07-17 (2)

## Goal
CLI .NET `tia` (verbos JSON) expondo Openness TIA V19+ p/ agentes IA + engenheiros.
Extração dos scripts provados em `Scripts_Siemens/FINAIS/` (read-only).

## State
- HEAD: b2284fc
- Done: F0-F2 código+build; **F3 código 100%** — 6 verbos portados:
  gen-profinet, standardize-tags (Standardize.cs), gen-fault-ob (FaultOb.cs),
  replicate-fc (Replicate.cs), gen-alarm-fc (AlarmFc.cs), replicate-instruments (InstrumentFc.cs).
  Build 0 erros (net48/x64, .NET SDK 8). Tudo commitado (f0fd0fa foi commit manual "Update"
  do user no meio; b2284fc completou).
- In progress: nada mid-flight. F3 fecha após code-review + smoke.
- NENHUM smoke real ainda: TIA V19 desinstalado; user vai reinstalar nesta máquina (titanxnexus).

## Decisions (and why)
- D1–D9 do docs/PLANO.md travadas — não rediscutir.
- **3 variantes "Replicador de FC" ≠ 1 verbo**: algoritmos distintos →
  replicate-fc (replicação por pasta, V3), gen-alarm-fc (bits-to-word + OB chamadas +
  comentários DB GLOBAL), replicate-instruments (replicação por instrumento + comandos 8888/9999).
- Desvios deliberados dos originais (registrados no PLANO): sem compile automático (verbo
  `compile` separado), sem menu interativo (dry-run + --apply), sem cache de template em disco
  (template deve existir no projeto; comentários `ponytail:` marcam upgrade path).
- Configs JSON chaves EN, valores/keywords PT (domínio CASAN); defaults = valores dos scripts.
- standardize-tags/gen-alarm-fc rodam sem --config (defaults completos);
  replicate-fc/replicate-instruments exigem --config (exemplos em docs/examples/).

## Next steps (ordered)
1. **/code-review** (previsto no PLANO p/ fim de F3) — foco: os 5 ports novos
   (Standardize, FaultOb, Replicate, AlarmFc, InstrumentFc) vs originais em FINAIS;
   riscos: fidelidade de regex/ordem de dicionários, dry-run sem mutação, XML rewiring.
2. Corrigir findings, commit.
3. Quando TIA V19 instalado (confirmar com user + projeto de TESTE): smoke F1/F2
   (tia info, list-blocks, export→import roundtrip, compile), depois smokes F3 dry-run.
4. Marcar fases ✅ no PLANO conforme smokes passem.
5. F4: README EN, licença (MIT provável), polish GitHub.

## Key files
- docs/PLANO.md — decisões, fases, desvios F3 (LER PRIMEIRO)
- src/Tia.Core/{Standardize,FaultOb,Replicate,AlarmFc,InstrumentFc,Profinet,Ops,TiaSession}.cs
- src/Tia.Cli/Program.cs — switch de verbos, WriteLock, AssemblyResolve
- docs/examples/{profinet,replicate-fc,replicate-instruments}.json
- Scripts_Siemens/FINAIS/*.txt — originais read-only (comparação no review)

## Open / blockers
- TIA V19 não instalado (bloqueia smokes).
- Build: PATH da sessão PowerShell pode precisar de
  `$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine")+";"+...("Path","User")`.
- Verbos HMI fora do v1.
