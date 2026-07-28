# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
**F7 — camada de compreensão.** IA lê o projeto dentro do orçamento de contexto para diagnosticar
("problema no acionamento BH-01A") e criar projeto a partir de documentos. Item 1 (`explain-block`)
fechado; item 2 (`trace --equipment`) implementado mas **sem smoke válido** — ver blocker.

## State
- HEAD: cd4bb14 — working tree limpo (só `.handoff/`).
- Portal aberto na sessão 1 com **Software de ETE Insular_Inicial_V21** (CPU1.0 CCO, 62 devices,
  476 blocos / 123 LAD). `doctor` ok. Nenhum `tia.exe` vivo (o travado foi morto).
- Done nesta sessão:
  - **Item 1 fechado** — smoke do `explain-block --name` em projeto real: `Resets` 58KB → 4,6KB,
    `Paineis Intertravamento` 53KB → 4,9KB, `FC_ALARMES_PRELIMINAR_P_GM_01` 26KB → 2,2KB (~12x).
    Expressões série/paralelo, CALL de FB com pinos e comentário pt-BR saíram corretos.
  - **`trace --equipment X`** implementado (`Inventory.Trace`, verbo no CLI, help). Reusa
    `Find("*X*")` para tags/blocos/tabelas/UDTs e varre cross-references para o lado reverso.
    `rebuild.ps1` ALL PASS.
  - **Navegação**: `src/__navi__.md` (símbolos públicos por `.cs` + os 47 `case "verbo"` do CLI com
    linha) gerado por `scripts/navi-cs.ps1`; 13 maps + árvore raiz regenerados; NAV INDEX no topo de
    `Inventory.cs`; ponteiro no CLAUDE.md.
- In progress: nada rodando.

## Decisions (and why)
- **D8 mantida integral** (user, hoje): otimizar tudo offline primeiro; testes online (diagnostic
  buffer, compare online×offline, watch) só quando a camada offline estiver bem mais avançada.
- **Item 2 (`trace`) escolhido como próximo passo** (user, hoje), antes de index/checkpoint.
- `trace` reusa `Find` em vez de reimplementar match — pattern `*X*` pega `BH-01A_CMD_LIGA`.
- `navi-cs.ps1` próprio porque `navindex.py` não lê C#; regenerar após refatorar.

## Next steps (ordered)
1. **Trocar a fonte do xref reverso do `trace`.** O caminho Openness está descartado na prática
   (blocker abaixo). Plano B = índice invertido a partir do **export XML** (`Ops.ExportBlock` +
   parse dos `<Access>`/`Symbol`), que é o que o plano original dizia: offline, testável em
   `Tia.Tests`, e naturalmente cacheável no `workspace/<proj>/index.json` (item 3 vira pré-requisito
   do 2, não sucessor).
2. Medir o custo do export de 476 blocos uma vez — se for aceitável, `index` popula o cache e
   `trace` só lê JSON (milissegundos por equipamento).
3. `checkpoint` / `restore` via export-block/import-block por escopo.
4. `apply-spec --file plant.json` — orquestrador + schema sobre verbos existentes.

Backlog anterior: `import-ladder --apply` contra `PARTIDA_*` real (pinos de comparador, ver PLANO
item 1b); `replicate-fc --apply` no ScaffoldTest; bytes de system/clock memory no `scaffold`/
`add-device` (8 dos 26 erros de compile); multiuser 3b/3c.

## Key files
- `src/Tia.Core/Inventory.cs:316` — `Trace`; `AllSources` logo abaixo = a parte a substituir.
- `src/Tia.Cli/Program.cs:258` — `case "trace"`; help na linha 33.
- `src/Tia.Core/BlockExplain.cs` — parser XML offline, molde para o índice invertido.
- `src/__navi__.md` — mapa C#; regen: `pwsh scripts/navi-cs.ps1`.
- `docs/PLANO.md` — linha F7 com as medições do item 1.

## Open / blockers
- **Xref do Openness inviável por chamada** (medido hoje, 2 tentativas): 1 chamada por bloco (476)
  estourou 600s; a versão com uma única chamada em `plc.BlockGroup.GetService<CrossReferenceService>()`
  ficou **18 min sem retornar, com CPU ~0,2s** (bloqueado no Portal, não computando) — processo
  morto à mão. Conclusão: `trace` como está não fecha; seguir para o passo 1.
- Aceite do plano B (índice via export XML) ainda não confirmado pelo user.
- Falta host/porta do TIA Project Server + projeto de teste lá (nunca produção) — trava multiuser.
