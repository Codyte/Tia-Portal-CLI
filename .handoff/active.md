# Handoff · TIA Portal Openness API · 2026-07-17 (6)

## Modo de trabalho (base — ativar no início da sessão)
- `/ponytail full` + `/caveman:caveman ultra` (estilo/economia) e `/navindex` p/ navegação
  (ler `__navi__.md` da pasta antes de busca ampla; regen após mudança estrutural).

## Goal
Smoke V21 do core: **FEITO**. Próximo: smoke dos 6 ports F3 (gen-profinet, standardize-tags,
gen-fault-ob, replicate-fc, gen-alarm-fc, replicate-instruments) + code-review dos ports vs FINAIS.

## State
- HEAD: 9bfc4ef (1fec820 = smoke completo; proj/ untracked do git; fixtures em docs/examples).
- 15 verbos ok contra TIA V21 real (SmokeTest_01, CPU 1214C 6ES7 214-1AG40-0XB0/V4.4):
  add-device, set-address (192.168.0.10), connect-subnet (PN1), create-folder, import-tags,
  import-source (FC1), import-ladder (FC2, LAD ok — risco FlgNet NÃO estourou), compile 0 err,
  export-block, diff-block identical, snapshot, find, xref, delete-block/folder, save-project.
- **Ambiente V21 resolvido de vez** (gates documentados no PLANO §Ambiente):
  grupo "Siemens TIA Openness" + logon novo (user já relogou — token bom em qualquer shell),
  whitelist.ps1 CORRETO (mistério era grupo/sessão, não formato; re-rodar após rebuild — hash),
  mesma sessão interativa, licença STEP 7 ativada pelo user.
- `tia` roda direto de qualquer terminal agora. smokeloop.ps1/taskrun/task S4U = obsoletos
  (mantidos como referência).

## Decisions (and why)
- D1–D9 valem; D9 (nunca 2 clients Openness em paralelo) crítico.
- proj/ fora do git — binário TIA regenerável, ruído gigante por smoke.
- Fixtures de smoke em docs/examples (SmokeTags.xml, FC_SmokeSrc.scl, FC_SmokeLad.scl).

## Next steps (ordered)
1. Smoke ports F3, um por vez (D9), contra SmokeTest_01: gen-profinet, standardize-tags,
   gen-fault-ob, replicate-fc, gen-alarm-fc, replicate-instruments. Paridade com FINAIS.
2. Smoke restante: export-cax/import-cax, export-type/import-type, export-tags, list-hmi
   (precisa device HMI — criar via add-device se catálogo permitir).
3. /code-review dos 5-6 ports vs Scripts_Siemens/FINAIS (pendência antiga).
4. Atualizar PLANO (F3 → ✅ quando ports passarem).
5. Item 9 (online) segue bloqueado por D8.

## Key files
- src/Tia.Cli/Program.cs — dispatcher verbos; usage completo rodando `tia` sem args
- src/Tia.Core/ — Ops.cs (blocos/tags/folders), Hardware.cs, LadConverter.cs (SCL→LAD),
  Profinet.cs/Standardize.cs/Replicate.cs/AlarmFc.cs/InstrumentFc.cs (ports F3)
- docs/PLANO.md — fases atualizadas + §Ambiente com gates V21
- docs/examples/ — fixtures do smoke
- proj/SmokeTest_01/SmokeTest_01.ap21 — projeto teste (PLC_1 + SmokeTags + FC_SmokeSrc)

## Open / blockers
- TIA V21 aberto com SmokeTest_01 ao fim da sessão — se fechado, `tia open-project --file
  "C:\Scripts\TIA Portal\proj\SmokeTest_01\SmokeTest_01.ap21"` (~2min).
- Ports F3 nunca rodaram contra TIA real — bugs prováveis.
- PATH PowerShell se dotnet sumir: `$env:Path=[Environment]::GetEnvironmentVariable("Path","Machine")+";"+[Environment]::GetEnvironmentVariable("Path","User")`.
