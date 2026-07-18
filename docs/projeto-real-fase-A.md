# Banho de projeto real — Fase A/B (2026-07-18)

Projeto: `Automação ETE SG AsBuilt_1_V21` (cópia offline em `proj/`). PLC `CPU CCO`, 21 devices,
1011 blocos, 102 tabelas de tags. Inventários completos em `workspace/real-A/*.json`.

**Contexto (user, 2026-07-18):** este projeto NÃO foi criado com a padronização dos scripts
originais — por padrão os verbos geradores não servem nele; vale como experiência/estresse.
Logo: achados 1-2 e o warning do 3 são hardening genuíno (valem pra qualquer projeto);
adaptações às convenções do ETE (regex de ID, tabela-por-área, template externo — achados 3-5)
só entram se um projeto-alvo real precisar (YAGNI).

## Achados (viram backlog)

1. **Projeto chega sem compilar** (todos os blocos `isConsistent: false`) e qualquer verbo que
   exporta (replicate-fc, gen-alarm-fc, diff) morre com `Inconsistent blocks and PLC data types
   (UDT) cannot be exported`. Mitigação: `tia compile --apply` antes. Candidato: check de
   consistência no `doctor` + mensagem de erro dirigida.
2. **Nomes de pasta TIA contêm `/` literal** — `4. Motores/Bombas`, `3. Alarmes/Eventos/Falhas`,
   `5. Instrumentação / Atuadores` são UMA pasta cada. `Ops.ResolveFolder` splitta por `/` e
   falhava. **Corrigido**: Replicate/Doctor tentam nome literal (FindGroup) antes de path.
   Pendente: mesmo tratamento nos demais consumidores de `--folder` (create-folder,
   import-block, compile --folder) se doer.
3. **Pastas de equipamento sem `(ID)`**: `Misturadores1 Zona1 Tanque1 MS-01A` — sem parênteses.
   `ExtractId`/heurísticas `(ID)` do replicate-fc e standardize (`VALVULA (VP-02B)` existe, mas
   é minoria) não casam com o padrão real `... TAG-NN`. Quantificado no dry: 61 pastas casam
   keyword e caem no filtro (Soprador 11, Bomba 37, Misturador 13 — agora com warning; antes
   era no-op silencioso). Backlog: extrator de ID configurável (regex no config).
4. **`2. Alarmes` real = 26 tabelas por área (2.1 PRELIMINAR … 2.26), sem subpastas.**
   `gen-alarm-fc` itera SUBPASTAS de área → 0 áreas no projeto real. Backlog: suportar
   tabela-por-área além de pasta-por-área.
5. **Sem blocos template no projeto real** (`FC_Modelo`, `OB_MOLDE_ALARMES`, `MODULE_ERROR_MOLDE`,
   `ALARMES_MODULOS` inexistentes; 0 grupos `HW_QA-*`). Original tinha fallback de template em
   disco; ports exigem bloco no projeto. Backlog: aceitar `--template-file X.xml` como fonte.
6. **`standardize-tags` dry funciona**: 102 tabelas → 91 rebuild, 2 generate, 8 memorize, 1 skip
   (`VALVULA (VP-02B)` sem template compatível). Apply seria reescrita em massa de endereços —
   NÃO aplicar sem revisar molds/AlarmOrder contra a convenção real.
7. **Tag folders reais**: `1.I/OS`, `2. Alarmes`, `3. Partidas/3.x Área[/SubÁrea]`, `4. Comm`.
   Não existe `5. Instrumentos` (tags de instrumento vivem nas áreas) → replicate-instruments
   não roda como está.
8. `doctor` pagou o investimento no primeiro uso: diagnosticou tudo acima sem mutar nada.

## Smokes v2 read-only (contra o projeto real, 2026-07-18)

| verbo | resultado |
|---|---|
| export-tags | ok (`--table` obrigatório; 55 tags) |
| list-types / export-type | ok — 25 UDTs; `MotorDados.xml` exportado |
| xref | ok — 313 linhas p/ `CHAMADA_ALARMES` |
| export-cax | ok — AML dos 21 devices + log |
| list-hmi | erro claro: projeto sem HMI Unified (Comfort fora da API — limitação Siemens) |

## Smokes v2 com mutação (no SmokeTest_01, 2026-07-18)

| verbo | resultado |
|---|---|
| import-type | ok — `MotorDados.xml` do real; dry detectou `override`, apply ok |
| import-cax | ok — AML 1.7MB do real (21 devices) importado; dry + apply |
| gen-alarm-fc | idempotência total validada: apply e dry retornam `in-sync` (áreas, globalDb, **callOb**) |

Achado 9 (fix aplicado): `CaxProvider.Import` falha com "Action is not supported within
ExclusiveAccess" — Openness proíbe CAx import dentro de ExclusiveAccess. Removido o
WriteLock do verbo `import-cax` em Program.cs. Hardening genuíno.

Projeto SmokeTest NÃO foi salvo após os imports — mutações eram só validação; reopen volta
ao estado limpo.

## Status dry-runs

| verbo | resultado |
|---|---|
| standardize-tags | dry ok (91 rebuild) |
| gen-fault-ob | bloqueado: sem HW_QA-*/templates (achado 5) |
| gen-alarm-fc | bloqueado: sem templates + achado 4 |
| replicate-fc | dry ok: 0 grupos (achado 3 — 61 pastas sem `(ID)`, warnings explícitos) |
| replicate-instruments | bloqueado: achado 7 |
| gen-profinet | precisa config de mapeamento (usuário) |
