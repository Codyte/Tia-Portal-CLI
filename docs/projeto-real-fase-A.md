# Banho de projeto real — Fase A/B (2026-07-18)

Projeto: `Automação ETE SG AsBuilt_1_V21` (cópia offline em `proj/`). PLC `CPU CCO`, 21 devices,
1011 blocos, 102 tabelas de tags. Inventários completos em `workspace/real-A/*.json`.

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

## Status dry-runs

| verbo | resultado |
|---|---|
| standardize-tags | dry ok (91 rebuild) |
| gen-fault-ob | bloqueado: sem HW_QA-*/templates (achado 5) |
| gen-alarm-fc | bloqueado: sem templates + achado 4 |
| replicate-fc | dry ok: 0 grupos (achado 3 — 61 pastas sem `(ID)`, warnings explícitos) |
| replicate-instruments | bloqueado: achado 7 |
| gen-profinet | precisa config de mapeamento (usuário) |
