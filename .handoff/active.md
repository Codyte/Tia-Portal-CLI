# Handoff · TIA Portal Openness API · 2026-07-18 (pós-F3.5 · plano de macros aprovado p/ execução)

## Modo de trabalho
- `/ponytail full` + `/caveman` + navindex (ler `__navi__.md` antes de busca ampla).
- **Macros novos — usar SEMPRE** (nunca coreografia manual):
  - `pwsh scripts/rebuild.ps1` = build + 31 testes offline + whitelist UAC só se tia.exe mudou.
  - `pwsh scripts/use-project.ps1 <Nome|.ap21> [-Save]` = no-op se aberto; senão close+open (2-4 min, background).
- Binário: `src\Tia.Cli\bin\Debug\net48\tia.exe` (Debug only). D9: nunca 2 tia em paralelo.
- Projeto reaberto volta ao ÚLTIMO SAVE — `save-project` após applies que importam.

## Goal
Executar a lista de macros aprovada (abaixo). Depois F4 GitHub (aguarda decisões do user:
licença MIT?, nome do repo, escopo README).

## State
- HEAD: 148f1bb (tree limpo). Handoff anterior arquivado em .handoff/archive/.
- Sessão de hoje: callOb=in-sync validado ✅; smokes mutação import-type/import-cax ✅ (achado 9:
  CAx import não pode ExclusiveAccess — fix commitado); Tia.Tests offline 31 asserts ALL PASS ✅;
  cosméticos backlog 7 ✅; FindGroup/FindTagGroup→Ops ✅; macros rebuild/use-project criados ✅.
- TIA: aberto com SmokeTest_01 (mutações de smoke NÃO salvas — de propósito, reopen limpa).

## Decisions (and why)
- D1–D9 valem. ETE SG fora da padronização → adaptações (achados 3-5/7) só se projeto-alvo pedir.
- Macro-verbos > prosa de ritual (pedido explícito do user; memória salva: coreografia 3+ passos vira script).
- Lista de macros apresentada; user: "executar tudo isso na próxima execução".

## Next steps (ordered) — EXECUTAR NESTA ORDEM
1. **`scripts/prep-project.ps1 <nome>`**: use-project → doctor → `tia compile --apply` → save-project.
   Mata achado 1 (projeto real chega sem compilar, exports morrem).
2. **`scripts/raio-x.ps1 <nome>`**: doctor + snapshot + export-tags + list-types + xref (OBs principais)
   + export-cax → `workspace/<proj>/`. Banho de projeto read-only em 1 comando (replica o que foi
   feito manual no ETE SG).
3. **`docs/examples/gen-all.json`** (batch pro `tia run --script`): fluxo FINAIS canônico —
   gen-profinet → standardize-tags → gen-fault-ob → replicate-fc → gen-alarm-fc →
   replicate-instruments, dry por padrão. Zero código novo. Smoke no SmokeTest_01.
4. **`scripts/clone-hw.ps1 <origem> <destino>`**: export-cax de A + import-cax em B (fluxo validado
   hoje com AML 1.7MB). Atenção: trocar projeto entre export e import (use-project 2x).
5. Testar 1-4 contra SmokeTest_01 (raio-x também contra ETE SG se couber), commitar, atualizar
   CLAUDE.md (seção macros) e PLANO.
6. **Só se user pedir**: macros 5-7 da lista (new-area — precisa `--template-file` achado 5;
   sync-check — precisa in-sync nos outros geradores; adopt-project — relatório de aderência).
7. F4 GitHub quando user responder: licença, nome do repo, escopo README.

## Key files
- scripts/rebuild.ps1, scripts/use-project.ps1 — macros existentes (padrão a seguir nos novos)
- docs/projeto-real-fase-A.md — 9 achados + tabelas de smoke (base da lista de macros)
- docs/PLANO.md — fases; docs/examples/batch.json — formato do `tia run --script`
- src/Tia.Core/Doctor.cs — preflight (componente dos macros 1-2)
- proj/SmokeTest_01/SmokeTest_01.ap21; proj/Automação ETE SG AsBuilt_1_V21 (cópia offline)

## Open / blockers
- Nenhum blocker. Smoke dos macros exige TIA aberto (confirmar com user antes).
- gen-all.json no SmokeTest: gen-profinet precisa config (docs/examples/profinet.json existe).
