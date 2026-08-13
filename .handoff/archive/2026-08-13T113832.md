# Handoff · TIA Portal Openness API · 2026-08-12

## Goal
A fila de 7 tropeços da FP-05 está fechada (código + smoke + docs). Não há trabalho em vôo — a
próxima sessão escolhe o próximo alvo: a ponta solta do T2(b), uma FP-06, ou outra coisa.

## State
- HEAD: `33f8151` (`docs(plano): fila da FP-05 fechada`). Working tree limpo fora de `workspace/`.
- **Live state: TIA Portal aberto na sessão 1** com `proj/Software de ETE Insular_Inicial_V21`.
  O projeto está **como estava antes da FP-05**: a Área 24 morreu num reboot da máquina (era o undo
  previsto) e os blocos `ZZ_TESTE_*` dos smokes foram apagados, com compile Success 0/0 depois.
  O shell do agente nasceu na **sessão 1** nesta máquina, então `tia` roda direto (sem a rota da task).
- Done: os 7 itens da fila, em 4 commits. `add-call` aceita FB sem pino e trata Input solto como
  aviso; `list-io-map` declara `nextFreeByteExact`/`Note`/`InDevice`; `add-db-member --path` cria
  ramo Struct; `add-call`/`delete-network`/`clone` reportam contagem de redes; `connect-subnet`
  lista `existingSubnets`. `PLANO.md` e `CLAUDE.md` atualizados.
- In progress: nada.

## Decisions (and why)
- **`nextFreeByte` não virou verdade, virou piso declarado.** Não existe API de "next free address"
  no Openness (`tia-help.py --sdk` só acha `Address.StartAddress`), e os 398 bytes que o Portal
  enxerga a mais não saem de nenhuma composição que o verbo visita. Mentir menos > adivinhar.
- **Input solto virou `warning`, `InOut` solto continua erro.** O molde da casa
  (`PARTIDA_BOMBA (B-10A)`) tem pino de entrada sem fio e compila — a régua do verbo era mais
  estrita que o projeto de referência. InOut é referência: sem fio não compila mesmo.
- **`--type Struct` continua recusado.** O ramo agora nasce pelo `--path`, no mesmo XML do
  membro-folha, então nenhum Struct vazio chega ao Portal — a guarda protege exatamente o caso que
  o `--path` não cobre.
- Rejeitado: sondar o Portal no dry-run do `set-io-address` (T2 item b). Exigiria escrever o
  endereço e reverter; num projeto real isso é escrita disfarçada de leitura.
- Retirado do `standing.md`: a proibição de salvar o projeto-molde. O usuário esclareceu que é
  projeto de teste.

## Next steps (ordered)
1. Perguntar ao usuário o próximo alvo — a fila acabou. Candidatos abaixo, nenhum urgente.
2. T2(b): `set-io-address --apply` é a primeira coisa que valida; o dry-run só ecoa o `--start`.
   Decidir se vale sondar (escreve e reverte) ou só documentar que o dry-run não confere.
3. FP-06 (nova rodada cega) se a ideia for continuar medindo a CLI contra trabalho real.

## Key files
- `docs/PLANO.md` — seção "Fila da FP-05 executada (2026-08-12)", tabela dos 7 itens com o aceite
  de cada um. É o resumo mais denso do que esta sessão fez.
- `docs/teste-cego/resultado-FP-05.md` — a rodada que gerou a fila (seção 3 = tropeços medidos).
- `src/Tia.Core/__navi__.md` — mapa da pasta; os 4 arquivos tocados foram `BlockEdit.cs`,
  `Hardware.cs`, `DbMember.cs`, `Clone.cs`.
- `workspace/t5/` (gitignored) — `ops.json`/`ops2.json`/`cleanup*.json`: os batches de smoke que
  provaram os 7 itens ao vivo. É a receita se algum item precisar ser re-provado.

## Open / blockers
- Nada bloqueando. Ponta solta única: o dry-run do `set-io-address` não pergunta nada ao Portal.
- **`rebuild.ps1` invalida a autorização do Portal já aberto**: a chamada seguinte trava ~10 min e
  morre com `EngineeringSecurityException: Security error. The operation has timed out.` Conserto:
  `Start-ScheduledTask -TaskName TiaWhitelist` e repetir. Aconteceu duas vezes nesta sessão.
- Compile do PLC inteiro passa de 10 min: batch com `compile --apply` roda em background, não em
  foreground com timeout de 600 s.

## Skills
- tia

## Effort
**Baixo** para o passo 1 — é uma pergunta ao usuário. Se a resposta for o passo 2, suba para
**médio**: mexer em `set-io-address` toca escrita de hardware e a decisão (sondar × documentar) não
está tomada. Reasoning não é o gargalo aqui — build e compile do Portal são.
