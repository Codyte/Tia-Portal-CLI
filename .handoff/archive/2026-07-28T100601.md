# Handoff · TIA Portal Openness API · 2026-07-28 (2ª sessão do dia)

## Goal
Fechar as pontas soltas do F7 item 2 e atacar o **gargalo de consumo** da camada de leitura:
verbos que despejam JSON gigante em stdout queimam o contexto do agente que deveriam economizar.
F7 existe pra caber no orçamento de contexto — hoje `find`/`snapshot`/`list-devices` trabalham
contra isso.

## State
- HEAD: c92f68c — working tree limpo.
- Portal aberto na sessão 1 com **Software de ETE Insular_Inicial_V21** (CPU1.0 CCO, 62 devices,
  476 blocos / 131 com lógica, 4372 tags). Saudável, `info` em 3,2s.
- **F7 item 2 fechado** (`dd37d70`): `trace --equipment AG-01` = 39 símbolos + 39 usos em 10
  blocos, **10,1s total / 3,3s de xref**. Cobertura conferida contra `xref --name Resets`
  independente. `xref` agora resolve bloco → tag → tabela → UDT (`ResolveSymbol`).
- **O blocker do handoff anterior era falso.** "Xref inviável, 476 chamadas > 600s, BlockGroup 18
  min" = diálogo de autorização Openness pendurado na tela desde o rebuild. Nada a ver com API.
- Plano B (export dos 476 blocos + índice invertido + cache) **descartado** — não há problema de
  performance pra resolver. Item 3 (`index`) deixou de ser pré-requisito de qualquer coisa.
- In progress: nada rodando.

## Decisions (and why)
- **Medir antes de construir** (user pediu avaliação ponytail): a medição derrubou o plano B
  inteiro. Custo da medição: 4 chamadas. Custo do plano B: parser + índice + cache + verbo novo.
- **`tia info` é o teste de ambiente**: chamada mais barata que existe. Se ela trava, é ambiente,
  nunca custo do verbo. Documentado no PLANO e na memória do projeto.
- Deletado o branch `BlockGroup.GetService<CrossReferenceService>()` — V21 devolve null sempre,
  nunca rodou. Deleção > código morto especulativo.
- `index`/cache adiado sem data: só faz sentido se `trace` passar a rodar em loop.

## Next steps (ordered)
1. **Gargalo #1 — output de leitura sem teto.** Medido nesta sessão: `find --pattern "*" --kind
   tag` = **821 KB / 4372 hits** em stdout (~200k tokens se cair no contexto). `snapshot` do mesmo
   projeto = 7967 linhas, `list-devices` = 3109. Um agente que rode qualquer um deles sem pensar
   perde a sessão. Correção proposta: `--out FILE` (escreve no workspace, stdout devolve só
   `{count, file, sample}`) + `--limit N`. `tree` já faz isso — é o precedente a copiar, não um
   design novo. Vale pra `find`, `snapshot`, `list-devices`, `list-tags`, `list-blocks`, `xref`.
2. **Gargalo #2 — attach de ~7s por chamada.** `trace` = 10,1s dos quais só 3,3s são trabalho;
   o resto é attach. Qualquer bateria de N verbos paga N×7s. `tia run --script` já amortiza
   (attach 1x) mas **aborta o batch inteiro na 1ª exceção** e descarta os resultados já obtidos
   (CLAUDE.md registra isso). Correção: try/catch por step, resultado por item com `ok/error`.
   Isso é o que torna `run` utilizável de verdade e mata o gargalo sem verbo novo.
3. **Ponta solta cosmética:** `xref` devolve a chave `"block"` mesmo quando o alvo é tag/tabela/UDT
   (tem `"kind"` ao lado). Renomear pra `"symbol"` quebra `raio-x.ps1`/`xref-obs.json` — decidir
   se renomeia com fallback ou deixa.
4. **Sem cobertura offline pra `ResolveSymbol`/`Trace`** — dependem de projeto TIA, `Tia.Tests` é
   offline. Aceito; se virar problema, extrair a montagem do `usedBy` pra função pura.
5. Só então F7 itens 4-5: `checkpoint`/`restore` por escopo, `apply-spec --file plant.json`.

Backlog anterior intacto: `import-ladder --apply` contra `PARTIDA_*` real (pinos `in1`/`in2` de
comparador, PLANO item 1b); `replicate-fc --apply` no ScaffoldTest; bytes de system/clock memory
no `scaffold`/`add-device` (8 dos 26 erros de compile); multiuser 3b/3c.

## Key files
- `src/Tia.Core/Inventory.cs:276` — `ResolveSymbol`/`FindTag`; `Xref` em 311, `Trace` em 359.
- `src/Tia.Cli/Program.cs:30` — bloco de help "read" (onde o `--out`/`--limit` seria anunciado).
- `src/Tia.Core/Inventory.cs:106` — `Tree`, o precedente de verbo que escreve em arquivo.
- `docs/PLANO.md` — F7 na tabela de fases; "Openness pede aceite na tela" na seção Ambiente.
- `src/__navi__.md` — mapa C#; regen obrigatório após mexer no CLI (`pwsh scripts/navi-cs.ps1`).

## Open / blockers
- Nenhum blocker técnico. O anterior era ambiental e está documentado.
- **Rebuild com Portal aberto → primeira chamada abre diálogo na tela e pendura.** Se uma chamada
  não retorna com `tia.exe` vivo e CPU ~0, pedir o clique ao usuário antes de investigar código.
- Falta host/porta do TIA Project Server + projeto de teste lá (nunca produção) — trava multiuser.
