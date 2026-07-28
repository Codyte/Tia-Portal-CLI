# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
**F7 — camada de compreensão.** Fazer a IA *ler* o projeto dentro do orçamento de contexto
(1 FC em LAD = ~200KB de XML) para diagnosticar ("problema no acionamento BH-01A") e criar
projeto a partir de documentos. 5 itens no plano; item 1 fechado nesta sessão.

## State
- HEAD: 0f1e2f0 — working tree limpo (só `.handoff/` desta escrita).
- Done: **F7 item 1 — `explain-block`** (`src/Tia.Core/BlockExplain.cs`, 320 linhas).
  Percorre Parts/Wires do FlgNet e reconstrói a expressão (série = AND, paralelo = OR, caixas
  O/A, comparadores, `Negated`); uma linha por bobina/SCoil/RCoil/MOVE/chamada de FB-FC, com
  título e comentário da rede (pt-BR quando existe) e interface do bloco. Sem FlgNet → `(rede
  vazia)`; sem CompileUnit (DB/UDT) → árvore de membros com 2 níveis.
  `--file F.xml` roda offline (mesmo atalho do `import-ladder` dry-run, antes de `Run()`);
  `--name X` = `Ops.ExportBlock` + explica. Medido: `BombaTemplateFc.xml` 92KB → 8,3KB.
  9 asserts em `Tia.Tests`, `rebuild.ps1` ALL PASS. Navi regenerado, PLANO com a linha F7.
- In progress: nada mid-flight.

## Decisions (and why)
- **`explain-block` offline por padrão** — `--file` não toca Siemens.Engineering, então o parser
  é testável em `Tia.Tests` contra o fixture real (`docs/examples/BombaTemplateFc.xml`) sem TIA.
- **Sinks dirigem a saída** (Coil/SCoil/RCoil/Move/Call); contatos e comparadores só aparecem
  dentro da expressão. Sem isso a lista repete cada parte solta e perde a semântica.
- **Comentário cortado em 200 chars, seções Temp/Constant/Static fora do cabeçalho** — ruído para
  diagnóstico; é onde o texto engordaria sem informar.
- IA escreve a *spec*, nunca o XML (item 5); leitura antes de escrita (1+2 read-only);
  `checkpoint` antes de qualquer `--apply` autônomo. (Inalteradas.)

## Next steps (ordered)
1. **Smoke do `explain-block --name`** contra projeto real (read-only, ~1 min) — só falta isso
   para o item 1 estar 100%. Portal está com **ScaffoldTest** aberto; para o de referência:
   `pwsh scripts/use-project.ps1 "Software de ETE Insular_Inicial_V21"` (2-4 min).
2. **`trace --equipment BH-01A`** (item 2) — vizinhança semântica em 1 chamada: tags %I/%Q/%M do
   símbolo, membro do DB global, iDBs, FCs que referenciam, word de alarme, OB que chama, endereço
   físico, pasta. `xref` só aceita bloco → índice invertido próprio, offline-testável.
3. `index` cacheado (`workspace/<proj>/index.json`) — só quando 2 doer no projeto real (1011 blocos).
4. `checkpoint` / `restore` via export-block/import-block por escopo.
5. `apply-spec --file plant.json` — orquestrador + schema sobre verbos existentes.

Backlog anterior: `import-ladder --apply` contra `PARTIDA_*` real (ver blocker abaixo);
`replicate-fc --apply` no ScaffoldTest; bytes de system/clock memory no `scaffold`/`add-device`
(8 dos 26 erros de compile); multiuser 3b/3c.

## Key files
- `src/Tia.Core/BlockExplain.cs` — NAV INDEX no topo; `Net.PartExpr`/`Expr` = reconstrução.
- `src/Tia.Cli/Program.cs:79` (rota offline `--file`) e `case "explain-block"` no `Dispatch`.
- `src/Tia.Tests/Program.cs` — `BlockExplain_Explain`, asserts contra o fixture real.
- `docs/examples/BombaTemplateFc.xml` — export LAD real (11 redes, FB calls, MOVE, Eq, OR).
- `docs/PLANO.md` — linha F7 na tabela; nota do FlgNet no item 1b.

## Open / blockers
- **Achado a validar: pinos de comparador.** Export real usa `in1`/`in2` e série no pino `pre`;
  o `LadConverter` emite `operand1`/`operand2`/`in`. Provável causa de falha no primeiro
  `import-ladder --apply` com comparador. Anotado no PLANO (item 1b), não corrigido.
- **Decisão pendente do user: revogar D8 só para leitura online** (diagnostic buffer, compare
  online×offline, watch de valores; download/start/stop seguem proibidos). Sem isso, "problema no
  acionamento" só cobre lógica/config offline.
- Aceite do item 2 (`trace`) como próximo passo ainda não confirmado.
- Falta host/porta do TIA Project Server + projeto de teste lá (nunca produção) — trava multiuser.
