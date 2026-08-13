# Handoff · TIA Portal Openness API · 2026-08-13

## Goal
Revisão da série de testes cegos FP-01→FP-06 (handoffs × cadernos × código), e as 5 primeiras metas
que saíram dela. Entregue e commitado — não há tarefa em voo. O próximo passo natural é **executar a
FP-07**, que já está escrita.

## State
- HEAD: `5886e8e` (`docs(plano): 78 verbos, e a regra do grep em rodada cega sai do resultado`);
  antes dele `9897b38` (o conserto grande). **Não pushado** — os dois commits estão só locais.
- Live state: **TIA Portal aberto na sessão 1, um processo só** (`--portal` desnecessário agora),
  projeto `Software de ETE Insular_Inicial_V21` como a FP-06 o deixou. O smoke desta sessão
  (`ZZ_TESTE_FRESH` + `ZZ_TESTE_FRESH_FB`) foi apagado no mesmo batch e **nada foi salvo**.
  `rebuild.ps1` rodou 2× (hash do `tia.exe` mudou; nenhum diálogo modal apareceu, as chamadas
  responderam normal). Shell do agente na sessão 1 → `tia` roda direto.
- Done: metas 1–5 da revisão + 2 itens de fila (contagem de verbos, regra do grep).
- In progress: nada.

## Decisions (and why)
- **O pré-compile virou um helper só (`Ops.ExportFresh`, sobrecarga `PlcBlock` + `PlcType`), não um
  guard por chamador.** Havia 3 políticas para o mesmo estado em 16 exports (2 compilavam, 1 lançava
  erro traduzido, 13 exportavam cru). Um lugar compartilhado é diff menor que treze.
- **Compila só o alvo, nunca o PLC no caminho normal.** Compile de bloco é de segundos; do PLC
  inteiro foram ~20 dos 49 min da FP-06. O caro sobrou para o ramo raro: inconsistência vinda de
  fora (UDT/DB usado pelo bloco) não é limpa pelo compile do bloco, e aí a mensagem manda
  `compile --apply`. O casamento é pela frase `Inconsistent blocks` percorrendo `InnerException`.
- **`ms` no `run --script`, não verbo de benchmark.** A medida tem que sobreviver no resultado da
  rodada; `Measure-Command` por fora nunca sobreviveu.
- **FP-07 desenhada por dívida, não por terreno novo.** O caderno força o uso de cada conserto que
  nunca foi exercitado, sem citar verbo: endereço fixo do diagrama (`set-io-address`), cartão por
  MLFB de compra (`plug-module --apply`), 3 acionamentos idênticos em área sem irmã populada
  (`replicate-fc --template --apply`), diagnóstico de estação (`gen-fault-ob`), e **entrega em duas
  etapas com `audit` ao fim de cada uma** — é a primeira chance real de ver check reprovando.
- **`CLAUDE.md` enxugado tirando arqueologia, não regra.** O histórico já mora nos
  `resultado-FP-*.md`; nada foi migrado para o `DIARIO.md` porque não havia o que salvar.
- Tentado e descartado: teste offline para o `ExportFresh` — recebe `PlcBlock`/`PlcType` do Openness,
  só roda com o Portal. A prova é a ao vivo (batch em `workspace/val-exportfresh.json`).
- Errata do meu próprio diagnóstico: `clone` usa `--block` + `--replace OLD=NEW`, não `--to`.
  Dois steps queimados até ler o `VERBS.md`.

## Next steps (ordered)
1. **Executar a FP-07** — sessão nova, cega, recebendo só `docs/teste-cego/caderno-FP-07.md` + a
   skill `tia`. `criterios-FP-07.md` **não** vai junto, e a busca da rodada exclui
   `docs/teste-cego/`.
2. Antes disso (ou como parte do relatório), decidir se o `git push` dos dois commits sai agora.
3. Fila que sobrou da revisão, em `PLANO.md` (seção "Revisão da série FP-01→FP-06"): régua-base fixa
   + anexo por rodada · re-teste do `import-master-copy --force --apply` (dívida mais antiga do
   repo) · conferência do caderno contra o projeto antes da rodada · telemetria do ramo caro do
   `ImportAndProve` · terreno da série sempre igual (4 rodadas cegas, todas ETE/acionamento).

## Key files
- `docs/teste-cego/caderno-FP-07.md` + `criterios-FP-07.md` — a próxima rodada, já escrita.
- `docs/PLANO.md` (seção "Revisão da série FP-01→FP-06", antes de "Histórico fechado") — os 5 itens
  feitos e os 7 da fila.
- `src/Tia.Core/Ops.cs` — `ExportFresh` (2 sobrecargas) + `IsInconsistentExport`, logo depois de
  `ExportBlock`; ver o `__navi__.md` da pasta, regenerado.
- `src/Tia.Cli/Program.cs` — o cronômetro do batch fica no ramo `run` do `Run` (~L400).
- `workspace/val-exportfresh.json` (gitignored) — o batch da prova ao vivo: clone → explain-block →
  list-interface sem compile no meio, 6 steps / 0 falhas / 7,7 s.

## Open / blockers
- Nada bloqueando.
- Os dois commits estão locais; o push é decisão do usuário.
- Compile do PLC inteiro ainda leva ~5 min quando for mesmo necessário: sempre em background.

## Skills
- tia

## Effort
**Médio** para o passo 1 — é rodada de engenharia de PLC ao vivo, com decisão de projeto e três
armadilhas para recusar por escrito; não é trabalho mecânico. Suba para **alto** só se algum dos
consertos exercitados contradisser o que foi medido hoje (em especial o `ExportFresh`: se um export
falhar com `Inconsistent blocks` depois do pré-compile, é dependência externa e o diagnóstico muda).
Reasoning não é o gargalo: o relógio é do Portal (compile, abertura de projeto) e do diálogo modal
de autorização depois de qualquer `rebuild.ps1`.
