# Handoff · TIA Portal Openness API · 2026-08-10 (6ª sessão do dia)

## Goal
Exercitar o CLI num projeto criado do zero: biblioteca instalada num PLC virgem, ciclo completo de
acionamento e os 4 geradores produzindo bloco. Tudo fechou. Sobraram 8 defeitos achados e corrigidos
em 4 commits; o próximo passo é a FP-03 cega, que exige sessão sem contexto.

## State
- HEAD: `3ea2e12`, pushado. Working tree limpo fora do `.handoff/`.
- Live state: **TIA Portal aberto** (sessão 1) com `workspace/newlib/LIB_TESTE/LIB_TESTE.ap21`,
  **salvo**. Dois PLCs: `PLC_ZERO` (biblioteca + MOTOR_01/02 + alarmes + fault OB + totalizador) e
  `PLC_RT` (35 blocos, só o round-trip da library). Mais `ET200SP_QA` no grupo `HW_QA-01` e 2 G120.
  Compile Success/0 no último apply. `FP02.ap21` foi fechado no início — o user já tinha salvo.
  Shell do agente na **sessão 0** (rota da task `TiaSmokeRun`). `tia.exe` rebuildado 4× aqui, cada
  rebuild reabre o diálogo modal de autorização (bateu 3×, user clicou).
- Biblioteca nova assada em `workspace/roundtrip/rt/rt.al21` (10 master copies) — artefato de teste,
  gitignored; a oficial `src/Tia.Lib/tia-cli/tia-cli.al21` não foi tocada.
- Done: install-lib em PLC zerado (35 blocos, sem `_1`, compile 0), `-Update` (62→62, sem `_1`),
  round-trip bake→install, ciclo de acionamento completo, os 4 geradores com apply e compile 0.
- In progress: nada.

## Decisions (and why)
- **Projeto novo em vez de escrever no FP02** — o passo 1 do handoff anterior pedia seu ok para
  escrever no FP02; criar `LIB_TESTE` do zero testa o mesmo caminho sem tocar no resultado da FP-02.
- **Sem verbo de catálogo de hardware.** O Openness só expõe `CatalogEntry.ArticleNumber`, sem
  composição de busca — um `list-catalog` seria sondagem às cegas. Em vez disso o `plug-module` com
  MLFB inválido passou a dizer de onde tirar o `typeIdentifier` (item igual já plugado) e a lembrar
  do sufixo de versão. 4 MLFBs de módulo ET200SP tentados, todos `Unknown TypeIdentifer`.
- **`gen-alarm-fc` não cria o membro do DB**, só comenta o que existe — mantido assim: criar ramo de
  DB é trabalho do fragmento/molde. O que faltava era o aviso (agora nominal, com o `add-db-member`
  que resolve), porque ele reportava `globalDb: in-sync` e o compile seguinte é que quebrava.
- **`add-db-member --type Struct` passou a ser recusado na entrada** — struct vazio é inválido, deixa
  o DB inconsistente e trava até o `add-db-member` seguinte (que criaria o primeiro membro). Bateu de
  verdade aqui; só saí reimportando o DB por `import-source`.

## Next steps (ordered)
1. **FP-03 cega** — sessão nova, sem handoff, só com o caderno e o `SKILL.md`. Não é executável de
   uma sessão que já tem contexto. Cegar pequeno: caderno de uma tarefa só.
2. **Varrer os defeitos menores que ficaram anotados e não viraram issue**: `list-blocks --folder`
   com caminho parcial vs. `FindGroup` (agora aceita caminho — conferir se os outros verbos que
   recebem pasta seguem a mesma regra), e `set-attr` aceitando no dry atributo read-only.
3. Pendências antigas de baixo retorno: baseline manual dos benchmarks (só você cronometrando) e os
   21 warnings da FP-01 nunca lidos um a um.

## Key files
- `src/Tia.Core/Library.cs:42` — `Create` (verbo `create-library`, novo).
- `src/Tia.Core/Inventory.cs` — `TagTables(plc, table)`, `FindTagTable`, `--kind constant` no `Find`.
- `src/Tia.Core/Ops.cs:29` — `FindGroup` (nome literal primeiro, depois caminho) + `FindGroupByName`.
- `src/Tia.Core/Hardware.cs:147` — `FindItem` aceita `<device>/<item>`; erro de `CanPlugNew` em :129.
- `src/Tia.Core/AlarmFc.cs` — fallback do molde (:69) e warning do word ausente (`WriteDbComments`).
- `src/Tia.Core/DbMember.cs:38` — guard do `--type Struct`.
- `src/__navi__.md` — regenerado (`pwsh scripts/navi-cs.ps1`); `docs/VERBS.md` idem, pelo rebuild.
- `workspace/instr.json`, `workspace/rep-motor.json`, `workspace/*prep*.json` — os configs e batches
  que montaram os pré-requisitos dos geradores (gitignored, reexecutáveis contra o `LIB_TESTE`).

## Open / blockers
- Régua do `install-lib` em PLC virgem agora tem dois pontos: 35 blocos (só os 5 pacotes de
  `1. FB Bibliotecas`) e 56 blocos (os 9 pacotes, incluindo moldes/área). Comparar com o número
  certo antes de gritar regressão.
- `rebuild.ps1` com o Portal aberto reabre o diálogo modal: chamada pendurada com CPU ~0 = alguém
  precisa clicar. Não é bug.
- MLFB de módulo de I/O continua sem fonte dentro do CLI (ver Decisions).

## Skills
- tia
- ponytail
- caveman

## Effort
**Alto** para o passo 1 — a FP-03 é teste cego: o valor está em não saber, então a sessão paga
descoberta do zero, e cada erro dela é dado. Para os passos 2 e 3, **baixo**: são varreduras
mecânicas com alvo nomeado. Em qualquer um deles raciocínio não é o gargalo — cada chamada `tia`
custa 10-20 s e o Portal domina o relógio.
