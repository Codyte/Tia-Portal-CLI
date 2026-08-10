# Handoff · TIA Portal Openness API · 2026-08-10 (7ª sessão do dia)

## Goal
Executar a **FP-03 — teste cego**: fazer o que o caderno `docs/teste-cego/caderno-FP-03.md` pede,
sem procurar nada desta sessão. O caderno já está escrito e commitado; não foi lido por quem vai
executá-lo, e é assim que ele mede alguma coisa.

## State
- HEAD: `14933a4`. Working tree limpo.
- Live state: **TIA Portal aberto** (sessão 1) com `workspace/newlib/LIB_TESTE/LIB_TESTE.ap21`.
  Nada aplicado na sessão anterior — tudo dry. Shell do agente na sessão 0 (rota da task).

## Next steps (ordered)
1. Ler `docs/teste-cego/caderno-FP-03.md` e entregar o que ele pede. Trabalhar como se o caderno
   tivesse chegado de um cliente: as decisões de projeto são suas.
2. Anotar no fim da sessão o que o CLI atrapalhou — é esse o resultado do teste, não o programa.

## Key files
- `docs/teste-cego/caderno-FP-03.md` — a tarefa.
- `docs/BOAS-PRATICAS.md` — R1–R9, o aceite do item 7 do caderno.

## Open / blockers
- Não abrir os cadernos FP-01/FP-02 nem o `docs/DIARIO.md` antes de terminar: os dois entregam
  respostas que a FP-03 deveria custar.

## Skills
- tia

## Effort
**Alto** — teste cego paga descoberta do zero e cada tropeço é o dado que se quer colher. Mas o
relógio é do Portal: cada chamada `tia` custa 10-20 s, então mais raciocínio não acelera o laço.
