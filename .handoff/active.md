# Handoff · TIA Portal Openness API · 2026-08-10 (7ª sessão do dia)

## Goal
Rodar a **FP-03: teste cego**. A próxima sessão executa um caderno de teste sem saber nada desta —
o valor está em não saber. Este handoff é curto de propósito.

## State
- HEAD: `bb65516`, pushado (push manual do user). Working tree limpo fora do `.handoff/`.
- Live state: **TIA Portal aberto** (sessão 1) com `workspace/newlib/LIB_TESTE/LIB_TESTE.ap21`.
  Nada foi aplicado nesta sessão — tudo dry. Shell do agente na sessão 0 (rota da task).
- Done: 4 commits (split do PLANO, auto-spill de saída, 2 defeitos corrigidos, contador de verbos).
- In progress: nada.

## Decisions (and why)
- **Não ler nada além do caderno e do `SKILL.md`** na próxima sessão. Contexto de fora do caderno
  contamina o teste cego — se você já sabe onde está a armadilha, a FP-03 não mede nada.
- **`caveman-compress` descartado para os `.md` do repo** — medido 3,4% no `CLAUDE.md`: os arquivos
  já nasceram em modo caveman, e o compressor ainda reflowou 164 linhas em 76 (diff pior). O ganho
  de contexto veio de *o que carrega*, não de estilo.

## Next steps (ordered)
1. **FP-03 cega**: escolher **um** caderno em `docs/teste-cego/` (os da FP-01/FP-02 já rodaram —
   escrever um novo, de uma tarefa só), abrir sessão limpa e executar. Cegar pequeno.
2. Pendências antigas de baixo retorno: baseline manual dos benchmarks e os 21 warnings da FP-01
   nunca lidos um a um.

## Key files
- `docs/teste-cego/` — cadernos FP-01 e FP-02 (referência de formato, não de conteúdo).
- `docs/PLANO.md` — seções `## Decisões`, `## Fases`, `## Pendências`. O histórico datado saiu
  inteiro para `docs/DIARIO.md`: só ler se a pergunta for "como chegamos nisso".

## Open / blockers
- Uma sessão que já leu este arquivo **não pode** executar a FP-03: ela deixou de ser cega.

## Skills
- tia

## Effort
**Alto** para o passo 1 — teste cego paga descoberta do zero e cada erro dele é dado. Mas o relógio
é do Portal: cada chamada `tia` custa 10-20 s, então raciocínio não é o gargalo do laço.
