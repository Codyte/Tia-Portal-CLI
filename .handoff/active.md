# Handoff · TIA Portal Openness API · 2026-08-13

## Goal
A fila da FP-05 está fechada por inteiro — 7 itens + o T2(b). Não há trabalho em vôo: a próxima
sessão escolhe o alvo (FP-06, ou o que o usuário quiser).

## State
- HEAD: `46ce12d` (`feat(set-io-address): dry-run confere o --start contra o mapa de I/O`).
  Working tree limpo fora de `workspace/` (gitignored).
- **Live state: TIA Portal aberto na sessão 1** com `proj/Software de ETE Insular_Inicial_V21`,
  no estado de antes da FP-05 (a Área 24 morreu num reboot; os blocos `ZZ_TESTE_*` dos smokes foram
  apagados, compile Success 0/0 depois). O shell do agente nasceu na **sessão 1**, então `tia` roda
  direto — a rota da task não foi exercitada nesta máquina hoje.
- Done: 5 commits. `add-call` (FB sem pino chamável, Input solto = aviso, `networksBefore/After`),
  `list-io-map` (`nextFreeByteExact`/`Note`/`InDevice`), `add-db-member --path` (cria ramo Struct,
  `structsCreated`), `clone` (`networks`), `delete-network` (`networksBefore/After`),
  `connect-subnet` (`existingSubnets`), `set-io-address` (dry-run com `conflictCheck`).
  `PLANO.md`, `CLAUDE.md` e `VERBS.md` atualizados; todos os 7 itens têm aceite ao vivo.
- In progress: nada.

## Decisions (and why)
- **`nextFreeByte` não virou verdade, virou piso declarado.** Não existe API de "next free address"
  no Openness (`tia-help.py --sdk` só acha `Address.StartAddress`), e os 398 bytes que o Portal
  enxerga a mais não saem de nenhuma composição que o verbo visita.
- **Dry-run confere, não sonda.** `set-io-address` cruza o `--start` com o mapa de I/O e devolve
  `conflictsWith` / `free (pelo mapa)`. Sondar de verdade exigiria escrever o endereço e reverter —
  isso deixa de ser dry-run, em qualquer projeto.
- **Input solto virou `warning`, `InOut` solto continua erro.** O molde da casa tem pino de entrada
  sem fio e compila; InOut é referência e sem fio não compila.
- **`--type Struct` continua recusado** — o ramo agora nasce pelo `--path`, no mesmo XML do
  membro-folha, então Struct vazio nunca chega ao Portal.
- Retirado do `standing.md`: a proibição de salvar o projeto-molde (é projeto de teste).

## Next steps (ordered)
1. Perguntar ao usuário o próximo alvo — não há fila pendente.
2. Candidato: FP-06, nova rodada cega, se a ideia for continuar medindo a CLI contra trabalho real.
   O método que funcionou: caderno → execução → `resultado-FP-0N.md` com os tropeços medidos → fila.

## Key files
- `docs/PLANO.md` — seção "Fila da FP-05 executada (2026-08-12)": os 7 itens e o aceite de cada um.
- `docs/teste-cego/resultado-FP-05.md` — a rodada que gerou a fila (seção 3 = tropeços medidos).
- `src/Tia.Core/__navi__.md` — mapa da pasta; os tocados foram `BlockEdit.cs`, `Hardware.cs`,
  `DbMember.cs`, `Clone.cs`.
- `workspace/t5/*.json` (gitignored) — os batches de smoke que provaram os itens ao vivo.

## Open / blockers
- Nada bloqueando.
- **`rebuild.ps1` invalida a autorização do Portal já aberto**: a chamada seguinte trava ~10 min e
  morre com `EngineeringSecurityException: Security error. The operation has timed out.` Conserto:
  `Start-ScheduledTask -TaskName TiaWhitelist` e repetir a chamada. Aconteceu 3x nesta sessão.
- Compile do PLC inteiro passa de 10 min: batch com `compile --apply` vai em background, nunca em
  foreground com timeout de 600 s.

## Skills
- tia

## Effort
**Baixo** para o passo 1 — é uma pergunta ao usuário. Se a resposta for FP-06, o esforço é da
rodada, não do planejamento. Reasoning não é o gargalo neste repo: build, compile e attach do Portal
dominam o relógio.
