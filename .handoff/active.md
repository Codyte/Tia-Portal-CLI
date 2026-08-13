# Handoff · TIA Portal Openness API · 2026-08-13

## Goal
Fila da revisão FP-01→FP-06 atacada: 5 de 7 itens fechados. O que resta é **executar a FP-07**,
agora com o caderno conferido contra o projeto (a conferência achou e corrigiu 3 erros de fato).

## State
- HEAD: `7d7fffd` (`docs(teste-cego): regua-base fixa, e a conferencia do caderno FP-07 pega tres
  erros de fato`). **`origin/main` == HEAD** — nada pendente de push (os commits anteriores foram
  pushados durante a sessão).
- Live state: **TIA Portal aberto na sessão 1, instância única** (`--portal` desnecessário), projeto
  `Software de ETE Insular_Inicial_V21` como a FP-06 o deixou, **nada salvo** nesta sessão. Só
  leitura foi feita contra ele (`list-io-map`, `find`, `list-tags`, `doctor`). `rebuild.ps1` rodou
  1× (hash do `tia.exe` mudou; nenhum diálogo modal apareceu). Shell do agente na sessão 1 → `tia`
  roda direto.
- Done: itens 1, 3, 4, 5 e 6 da fila da revisão. Régua-base criada, telemetria do `ImportAndProve`
  no código, caderno + critérios da FP-07 corrigidos, PLANO atualizado, navi regenerado, commitado.
- In progress: nada.
- Não commitado: `.handoff/archive/2026-08-13T160751.md` (untracked, do `--archive`).

## Decisions (and why)
- **Régua-base fixa (`docs/teste-cego/regua-base.md`), arquivo por rodada vira anexo.** A régua era
  reescrita inteira a cada rodada, então não havia série comparável. A base fixa carrega condução,
  portões `G-A`…`G-D` e as seis métricas `M1`…`M6`; `criterios-FP-07.md` é o primeiro anexo. As
  rodadas anteriores **ficam como estão** — são o registro do que valia na época, não se reescreve
  história.
- **`M3`/`M4` só se leem junto com uma linha sobre o terreno.** Era o defeito do único número que
  se comparava (contorno de CLI, 32 % → 12 %).
- **Telemetria do ramo caro do `ImportAndProve` = linha em `workspace/telemetry.log`, não contador
  em memória.** Cada `tia` é processo novo; a contagem por rodada tem que sobreviver em disco
  (`wc -l`). Escrita engolida em `catch {}` de propósito — telemetria não derruba verbo.
- **Descartado: teste offline do `LogFallback`/`ExportFresh`** — dependem de objeto do Openness, só
  rodam com o Portal.
- **A conferência do caderno (item 4 da fila) pagou sozinha:** 4 verbos de leitura, 16 s, três erros
  de fato. `%IB1100`/`%IB1110` caíam **em cima dos telegramas dos SINAMICS `BEF-01/02/04`**
  (`%IB1100..1135`; `%I` ocupado até 1147) → portão `G2` era inalcançável, passaram para
  `%IB1200`/`%IB1210`. `%QB420` livre, mantido. "Área nova no CLP" era falso: existem
  `3. Partidas/3.19 Adensadores de Lodo` (dois SKID populados), `3.21 Elevatória Lodo Adensado` e a
  pasta de alarme homônima → a unidade nova virou **`Adensador por Gravidade`**.
- **Não conferido de propósito:** os 5 MLFB da lista de compra contra o catálogo — exigiria criar a
  estação, que é trabalho da rodada. Fica como risco declarado no anexo.
- **Esta sessão não podia executar a FP-07** (handoff carregado = contaminada). Rodada cega exige
  sessão nova recebendo só o caderno + a skill.

## Next steps (ordered)
1. **Executar a FP-07** — sessão nova e cega, recebendo só `docs/teste-cego/caderno-FP-07.md` + a
   skill `tia`. `criterios-FP-07.md` e `regua-base.md` **não** vão junto; a busca da rodada exclui
   `docs/teste-cego/`.
2. Commitar `.handoff/archive/2026-08-13T160751.md` (caminho explícito, nunca `git add -A`).
3. Fila que resta (`PLANO.md`, seção "Revisão da série FP-01→FP-06"): **item 2** re-teste do
   `import-master-copy --force --apply` em CPU virgem (dívida mais antiga do repo) e **item 7**
   terreno da série sempre igual — os dois exigem rodada de **projeto novo**, que a FP-07 não é.

## Key files
- `docs/teste-cego/regua-base.md` — parte fixa da régua (condução, `G-A`…`G-D`, `M1`…`M6`).
- `docs/teste-cego/caderno-FP-07.md` — a rodada, já corrigida (endereços §3, área §1).
- `docs/teste-cego/criterios-FP-07.md` — anexo: dívida perseguida, portões de terreno, armadilhas
  B1–B3, e a seção "Conferência do caderno contra o projeto" com os três achados.
- `docs/PLANO.md` (seção "Revisão da série FP-01→FP-06") — tabela dos 7 itens com estado.
- `src/Tia.Core/Ops.cs` — `LogFallback` logo antes de `Prove`, chamado no `catch` do
  `ImportAndProve`; ver `src/Tia.Core/__navi__.md` (regenerado).
- `workspace/fp07-iomap.json`, `fp07-adensador.json`, `fp07-pv.json` (gitignored) — as leituras da
  conferência, se alguém quiser reconferir sem chamar o Portal.

## Open / blockers
- Nada bloqueando.
- `workspace/telemetry.log` ainda não tem nenhuma linha — o ramo caro não disparou desde que a
  telemetria entrou. A primeira medida real sai da FP-07.

## Skills
- tia

## Effort
**Médio** para o passo 1 — rodada de engenharia de PLC ao vivo, com decisão de projeto e três
armadilhas para recusar por escrito; não é trabalho mecânico. Suba para **alto** só se um export
falhar com `Inconsistent blocks` mesmo depois do pré-compile do `ExportFresh` (aí é dependência
externa e o diagnóstico muda), ou se `set-io-address --apply` recusar `%IB1200`/`%IB1210` apesar da
conferência. Reasoning não é o gargalo: o relógio é do Portal (compile, abertura de projeto) e do
diálogo modal de autorização depois de qualquer `rebuild.ps1`.
