# Handoff · TIA Portal Openness API · 2026-08-13

## Goal
Executar a **rodada cega FP-06** no projeto-molde: elevatória final com cinco bombas em inversor
SINAMICS. O caderno e os critérios já estão escritos e commitados; falta a execução e o
`resultado-FP-06.md`.

## State
- HEAD: `cb3caa6` (`docs(teste-cego): caderno e criterios da FP-06`). Working tree limpo fora de
  `workspace/` (gitignored).
- Live state: **TIA Portal aberto na sessão 1** com `proj/Software de ETE Insular_Inicial_V21`
  (PLC `CPU1.0 CCO`), no estado de antes da FP-05 — a Área 24 morreu num reboot e os blocos
  `ZZ_TESTE_*` foram apagados, com compile Success 0/0 depois. **Havia 2 processos
  `Siemens.Automation.Portal` vivos** ao fim desta sessão (PIDs 15620 e 20852): conferir antes de
  chamar qualquer verbo — com mais de um Portal aberto todo verbo exige `--portal <projeto|PID>`.
  O shell do agente nasceu na sessão 1, então `tia` roda direto.
- Done: fila da FP-05 fechada (7 itens + T2(b), 5 commits, tudo com aceite ao vivo). Caderno e
  critérios da FP-06 escritos.
- In progress: nada. A execução da FP-06 ainda não começou.

## Decisions (and why)
- **FP-06 pressiona R3, R4, R5 e R7** — as regras da `BOAS-PRATICAS.md` que **nenhum dos 10 checks
  do `audit` pega**. A FP-05 já provou que a régua automática funciona; o que falta medir é se a
  doutrina escrita sozinha segura a decisão sob pressão de requisito de cliente.
- **Tema é inversor SINAMICS** porque toda rodada anterior foi partida direta: exercita
  `insert-telegram --change`, a ordem dos dois `connect-subnet` e o nascimento da constante
  `~Standard_telegram_NN` — cadeia que hoje só existe descrita no `CLAUDE.md`.
- **Cinco equipamentos idênticos de propósito**: a FP-05 fechou com zero uso de
  `replicate-fc`/`gen-alarm-fc`/`install-lib` (duas bombas em partida direta não têm molde na casa).
  Se a rodada replicar no braço com cinco `clone`, o achado é da ferramenta.
- **A execução tem de ser sessão nova e cega**: só `caderno-FP-06.md` + skill `tia` entram.
  `criterios-FP-06.md` **não se lê antes nem durante** — se ler, o teste vira gabarito.

## Next steps (ordered)
1. **Executar a FP-06** lendo apenas `docs/teste-cego/caderno-FP-06.md`. Antes de tudo:
   `pwsh scripts/prep-project.ps1 "Software de ETE Insular_Inicial_V21"` e conferir quantos Portais
   estão abertos.
2. Ao terminar a entrega, aí sim ler `docs/teste-cego/criterios-FP-06.md` e escrever
   `docs/teste-cego/resultado-FP-06.md` no formato das rodadas anteriores (entregue / tropeços
   medidos / portões / fila).
3. Atualizar a tabela de fases do `PLANO.md` e abrir a fila de conserto que sair da rodada.

## Key files
- `docs/teste-cego/caderno-FP-06.md` — a entrada da rodada (o único que a sessão executora lê).
- `docs/teste-cego/criterios-FP-06.md` — portões G1–G7 e inspeções I1–I5. **Só depois da execução.**
- `docs/teste-cego/resultado-FP-05.md` — formato do relatório e os 7 tropeços que o I5 remede.
- `docs/BOAS-PRATICAS.md` — R1–R9, a régua que as armadilhas da seção 6 do caderno pressionam.
- `src/Tia.Core/__navi__.md` — mapa da pasta, se a fila exigir conserto de verbo.

## Open / blockers
- Nada bloqueando a execução.
- **`rebuild.ps1` invalida a autorização do Portal já aberto**: a chamada seguinte trava ~10 min e
  morre com `EngineeringSecurityException: Security error. The operation has timed out.` Conserto:
  `Start-ScheduledTask -TaskName TiaWhitelist` e repetir. Não rebuildar no meio da rodada.
- Compile do PLC inteiro passa de 10 min: batch com `compile --apply` vai em background, nunca em
  foreground com timeout de 600 s.

## Skills
- tia

## Effort
**Médio** para o passo 1 — é engenharia de programa de PLC contra projeto grande, com decisão de
padrão a tomar (as quatro exigências da seção 6 do caderno). **Alto** só se o telegrama do drive
resistir: se `insert-telegram` recusar ou a constante `~Standard_telegram_NN` não nascer depois dos
dois `connect-subnet`, é sondagem de API e aí `tia-help.py --sdk` vem antes de tentar no braço.
Fora disso, reasoning não é o gargalo: attach, compile e import do Portal dominam o relógio.
