# Handoff · TIA Portal Openness API · 2026-07-18 (F3.5 completa + banho projeto real)

## Modo de trabalho
- `/ponytail full` + `/caveman` + navindex (ler `__navi__.md` antes de busca ampla).
- Binário: `src\Tia.Cli\bin\Debug\net48\tia.exe` (Debug only; Release stale). D9: nunca 2 tia em paralelo.
- Rebuild: `dotnet build src -c Debug`. **Pós-rebuild whitelist**: `schtasks /Run /TN TiaWhitelist`
  FALHA sem elevação → usar `Start-Process pwsh -Verb RunAs ... scripts/whitelist.ps1` (UAC 1 clique).
- tia direto do shell funciona (token tem grupo Openness); protocolo TiaSmokeRun só como fallback.
- Se TIA fechar: `tia open-project --file <caminho .ap21>` (background, ~2-4 min). S4U não abre UI.
- Projeto reaberto volta ao ÚLTIMO SAVE — rodar `save-project` após applies que importam.

## Goal
F3.5 ✅ (melhorias 1+2+3 + doctor + tree + banho de projeto real). Próximo: fechar pendências
de smoke, depois decidir F4 (GitHub) vs backlog 4-7.

## State
- HEAD: 2d8f96d (tree limpo). PLANO atualizado (linha F3.5 + smokes v2).
- Feito hoje: robustez por-item (Standardize/FaultOb), gen-alarm-fc idempotente (globalDb/callOb
  in-sync via BlocksIdentical), BlocksIdentical normaliza namespace+Informative (validado:
  diff-block roundtrip identical no projeto real), verbo `doctor` (preflight 6 verbos), verbo
  `tree` (outline navindex do PLC), fix pastas TIA com '/' literal, warning pastas sem '(ID)'.
- Banho projeto real (ETE SG AsBuilt, 1011 blocos): 8 achados em docs/projeto-real-fase-A.md.
  Smokes v2 read-only ok: export-tags/list-types/export-type/xref/export-cax; list-hmi erro
  claro (projeto sem Unified).
- TIA atual: aberto com projeto REAL (Automação ETE SG AsBuilt_1_V21, PLC "CPU CCO").

## Decisions (and why)
- D1–D9 valem. ETE SG NÃO segue padronização dos scripts → achados 3-5 (regex ID, tabela-por-área,
  template externo) são adaptação de convenção: YAGNI até projeto-alvo padronizado pedir.
- Hardening genuíno (vale sempre): compile antes de export; nomes de pasta com '/' literal;
  warnings em vez de no-op silencioso.
- Commit a1bc882 "update" (user) já continha versão anterior dos itens 1-3 de sessão perdida —
  507ada8+2d8f96d são o delta validado. Não investigar mais.

## Next steps (ordered) — BEM CLARO
1. **Validar callOb=in-sync no SmokeTest_01** (única validação pendente da idempotência):
   fechar projeto real no TIA → `tia open-project --file "C:\Scripts\TIA Portal\proj\SmokeTest_01\SmokeTest_01.ap21"`
   → `tia gen-alarm-fc --apply` → `tia gen-alarm-fc` (dry) → esperar `callOb=in-sync`
   (globalDb já validou). Depois `save-project`.
2. **Smokes v2 com mutação no SmokeTest_01** (nunca no real): `import-type` (usar
   workspace\real-A\MotorDados.xml) e `import-cax` (dry primeiro; AML do real é grande — se
   falhar, gerar cax do próprio SmokeTest e reimportar).
3. **Backlog 6 — testes offline dos rewires** (maior alavanca): runner net48 console assert-based
   contra fixtures docs/examples/, cobrindo Rewire*/Build*Xml (XDocument puro, sem TIA).
4. **Backlog 7 cosmético** (15 min): Standardize.cs StandardizeName 2x; comentar por quê template
   não é reescrito em Replicate.cs:115; documentar build Debug-only (ou corrigir Release).
5. **Refactor barato (da revisão de estrutura)**: mover FindTagGroup (Profinet) e FindGroup
   (ReplicateFc) pra Ops — mecânico, 3+ consumidores cada.
6. **F4 GitHub** (README EN, licença MIT?, exemplos) — só quando 1-3 fecharem.
7. Itens 4-5 do backlog antigo (in-sync no replicate-fc, heurísticas→config) e achados 3-5 do
   projeto real: SÓ se projeto-alvo padronizado pedir.

## Key files
- src/Tia.Core/Doctor.cs — preflight; Inventory.cs:Tree — outline navindex
- src/Tia.Core/Ops.cs:~359 BlocksIdentical — normalização nova (ns + Informative)
- docs/projeto-real-fase-A.md — 8 achados + tabela smokes v2
- docs/PLANO.md — fases (F3.5 fechada); workspace/real-A/ — inventários do projeto real (gitignored)

## Open / blockers
- Nenhum blocker. TIA está com projeto real aberto — passo 1 exige trocar pro SmokeTest.
