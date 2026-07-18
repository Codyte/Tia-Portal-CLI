# Handoff · TIA Portal Openness API · 2026-07-18 (pós code-review F3)

## Modo de trabalho
- `/ponytail full` + `/caveman` + navindex (ler `__navi__.md` antes de busca ampla).
- Binário: `src\Tia.Cli\bin\Debug\net48\tia.exe` (Release stale — NÃO usar). D9: nunca 2 tia em paralelo.
- Rebuild: `dotnet build src -c Debug`. TIA V21 com UI + SmokeTest_01 (--no-ui morre com o CLI).
- Se TIA fechar: `tia open-project --file "C:\Scripts\TIA Portal\proj\SmokeTest_01\SmokeTest_01.ap21"`.
  Whitelist pós-rebuild: `schtasks /Run /TN TiaWhitelist`.

## Goal
F3 ✅ COMPLETA (6 smokes + code-review vs FINAIS: 0 bugs de paridade, commit f7f653b).
Próxima etapa: **melhorias pré-projeto-real** (backlog abaixo), depois validar contra CÓPIA de
projeto real (nunca produção — regra dura).

## State
- HEAD: f7f653b (tree limpo). PLANO tabela F3 fechada.
- Code-review 2026-07-18: 6 ports paridade ok; divergências deliberadas todas documentadas.

## Backlog de melhorias (ordenado valor/custo) — decidir com usuário quais aplicar
1. **Robustez por-item (do review):** Standardize.cs:419-437 rebuild sem try/catch por tabela
   (1 falha aborta run com tabela meio deletada; original avisava e continuava). Idem
   FaultOb.cs:83-88 import por OB. ~10 linhas; exige re-smoke.
2. **Idempotência apply do gen-alarm-fc:** AlarmFc.cs:186 reimporta DB GLOBAL e AlarmFc.cs:224
   deleta/reimporta call OB TODO apply, mesmo sem mudança. Comparar antes (Ops.BlocksIdentical /
   diff dos comments no XML) → re-apply vira no-op de verdade. Reduz risco no DB central.
3. **`tia doctor` (novo verbo):** valida pré-requisitos de cada verbo F3 sem mutar nada
   (templates existem? pastas? DB GLOBAL? cultures? UDTs?). Barato e é o primeiro comando a
   rodar num projeto novo — transforma erro no meio do run em checklist upfront.
4. **in-sync no replicate-fc:** único verbo sem detecção — dry re-run sempre diz "overwrite".
   Ops.BlocksIdentical já existe; comparar bloco principal por target (molde do dead-code
   GetFolderStatus do original V3).
5. **Heurísticas hardcoded → config (expor no *Config, defaults atuais):**
   - InstrumentFc: `"FQIT-01"` primeiro (l.150), `"Preliminar"` primeiro (l.129)
   - Replicate: `CCM(\d+)` → `QA-0n` (FindCcmInfo l.360), prefixo `PARTIDA_` (l.344),
     length-checks mágicos +15/+16 (l.254-257)
   - FaultOb: prefixo componente `QA-`/`WORD_` (l.210-214)
   - AlarmFc: sufixo `_FALHA`, filtros `_CMD_`/`_RESET_` (l.112-113), struct `ALARMES`/
     `WORD_ALARMES_` (l.459-461)
   Não fazer big-bang: expor só o que o projeto real quebrar (YAGNI).
6. **Testes offline dos rewires:** Rewire*/Build*Xml operam em XDocument puro — testável sem TIA.
   1 runner assert-based (net48 console) contra fixtures docs/examples/ → valida refactors sem
   TIA aberto. Maior alavanca de velocidade p/ iterações futuras.
7. Cosmético: Standardize.cs:365 StandardizeName aplicado 2x (idempotente, remover 2ª);
   Replicate.cs:115 comentar por quê template não é reescrito (preserva placeholders);
   build Release stale (corrigir ou documentar Debug-only).

## Decisions (and why)
- D1–D9 valem. Fixtures smoke em docs/examples/ (commitadas).
- Cultures XML sempre filtradas por LanguageSettings.ActiveLanguages (senão import falha).
- Ops.BlocksIdentical ordena filhos de ObjectList (export TIA reordena Title).
- InstrumentFc: template = raiz PRIMEIRO; IsTaskComplete com lookup global.
- Ressalvas 1-2 do review NÃO aplicadas ainda — mudam comportamento pós-smoke, exigem re-smoke.

## Next steps (ordered)
1. Usuário escolhe itens do backlog (recomendação: 1+2+3 antes do projeto real; 4-7 conforme dor).
2. Aplicar escolhidos + rebuild + re-smoke rápido contra SmokeTest_01 (dry→apply→re-run).
3. Projeto real (CÓPIA offline, nunca produção): Fase A read-only (info/snapshot/list-blocks/
   xref/export) → Fase B dry-run dos 6 verbos F3 → relatório do que quebrou/pulou vira backlog.
4. Smokes v2 faltantes: export-tags, export-type/import-type, export-cax/import-cax, list-hmi.
5. F4 GitHub (README EN, licença, exemplos) — só depois do banho de projeto real.
6. Item 9 (online) segue bloqueado por D8.

## Key files
- src/Tia.Core/{Profinet,Standardize,FaultOb,Replicate,AlarmFc,InstrumentFc}.cs — os 6 ports
- src/Tia.Core/Ops.cs:359 — BlocksIdentical (base p/ itens 2 e 4)
- Scripts_Siemens/FINAIS/ — referência read-only
- docs/PLANO.md — tabela de fases; docs/examples/ — fixtures

## Open / blockers
- Nenhum blocker. Aguardando: (a) escolha dos itens do backlog, (b) cópia de projeto real.
