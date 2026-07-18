# Handoff · TIA Portal Openness API · 2026-07-18

## Modo de trabalho
- `/ponytail full` + `/caveman` + navindex (ler `__navi__.md` antes de busca ampla).
- Binário: `src\Tia.Cli\bin\Debug\net48\tia.exe` (Release stale — NÃO usar). D9: nunca 2 tia em paralelo.
- Rebuild: `dotnet build src -c Debug`. TIA V21 aberto com SmokeTest_01 (manter com UI; --no-ui morre com o CLI).

## Goal
**F3 ✅ COMPLETO — 6/6 smokes ok** contra SmokeTest_01 (gen-profinet, standardize-tags,
gen-fault-ob, replicate-fc, gen-alarm-fc, replicate-instruments). Todos: dry→apply→compile 0
err→re-run idempotente ("in-sync"). Projeto salvo. PLANO atualizado. Navi regen.

## State
- HEAD: 1c085a0 (tree limpo, tudo commitado).
- Sessão de hoje: gen-alarm-fc ✅ (fix cultures AlarmFc + idempotência dry + normalização ordem
  ObjectList em Ops.BlocksIdentical) e replicate-instruments ✅ (4 fixes, ver abaixo).

## Decisions (and why)
- D1–D9 valem. Fixtures de smoke em docs/examples/ (commitadas).
- Padrão consolidado: verbos que importam XML multilingual FILTRAM cultures pelas
  LanguageSettings.ActiveLanguages do projeto (FaultOb, AlarmFc, InstrumentFc — todos com
  `_cultures` + session param).
- Ops.BlocksIdentical agora ordena filhos de ObjectList (sort estável por nome) — export TIA põe
  Title no fim, XML gerado no início; sem isso re-run nunca dava "in-sync".
- InstrumentFc: template = raiz PRIMEIRO (Insert pós-OrderBy) — senão pasta de área gerada vence
  no re-run e o molde vira FC já rewired (gerava networks duplicadas).
- IsTaskComplete (InstrumentFc): lookup GLOBAL de instance DB (Ops.FindBlock), mesmo critério do
  ImportAreaFc.

## Next steps (ordered)
1. **code-review dos 6 ports vs Scripts_Siemens/FINAIS/** (última pendência F3; PLANO diz "fim de
   F3, pontos de maior risco"). Ports: src/Tia.Core/{Profinet,Standardize,FaultOb,Replicate,
   AlarmFc,InstrumentFc}.cs vs os .txt de FINAIS. Foco: paridade de lógica, edge cases dos
   originais não portados.
2. Smokes faltantes v2 (menor prioridade): list-hmi, export-cax/import-cax, export-type/
   import-type, export-tags.
3. Item 9 (online) segue bloqueado por D8.

## Key files
- src/Tia.Core/AlarmFc.cs — _cultures (~l.42, Generate início), idempotência dry (~l.137)
- src/Tia.Core/InstrumentFc.cs — _cultures, template raiz-primeiro (~l.99), FlgNs em CallInfo
  (~l.186), IsTaskComplete global (~l.515)
- src/Tia.Core/Ops.cs — BlocksIdentical sort ObjectList (~l.380)
- docs/examples/ — fixtures replicate-instruments: InstrumentDb.scl, InstrTagsEta.xml,
  InstrumentTemplateFc.xml, ObInstrumentos.xml, replicate-instruments.json
- docs/PLANO.md — tabela de fases (F3 ✅)

## Open / blockers
- Nenhum blocker. Estado do projeto TIA: SmokeTest_01 salvo com todas as fixtures (devices
  INV-BH01A/B + RIO-QA01, área ETA alarmes + instrumentos, FCs gerados).
- Se TIA fechar: `tia open-project --file "C:\Scripts\TIA Portal\proj\SmokeTest_01\SmokeTest_01.ap21"`
  (com UI). Whitelist pode pedir de novo após rebuild+novo TIA (elevação: `schtasks /Run /TN TiaWhitelist`).
