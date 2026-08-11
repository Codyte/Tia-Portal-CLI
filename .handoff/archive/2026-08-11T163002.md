# Handoff · TIA Portal Openness API · 2026-08-11 (4)

## Goal
Executar o caderno [`docs/teste-cego/caderno-FP-04.md`](../docs/teste-cego/caderno-FP-04.md) —
área de aeração `Sopradores/Aeração` no CLP de teste, hardware novo incluído — e escrever
`docs/teste-cego/resultado-FP-04.md`.

É uma **rodada cega**: quem escreveu o caderno não é quem executa. O handoff da sessão que o
escreveu foi arquivado de propósito para esta sessão não herdar nada dela.

## Rodada cega — o que NÃO ler (é a regra do teste, não preferência)

Ler qualquer um destes entrega de graça o que a rodada deveria descobrir sozinha, e invalida o
resultado:

- as outras rodadas: `docs/teste-cego/caderno-FP-0{1,2,3}.md`, `resultado-*.md`, `criterios.md`,
  `artigo.md`;
- a seção **"FP-04 escrita"** de `docs/PLANO.md` — o resto do PLANO (decisões D1–D9, fases) pode e
  deve ser lido;
- `docs/DIARIO.md` e `.handoff/archive/`.

Tudo o mais é jogo limpo, e é o que uma obra real teria: o caderno, o `CLAUDE.md` do repo,
`docs/BOAS-PRATICAS.md`, `docs/VERBS.md`, os `__navi__.md`, a ajuda oficial (`scripts/tia-help.py`)
e o próprio projeto TIA.

**Travou por falta de documentação = defeito da ferramenta, não da sessão.** Registrar em vez de
contornar calado — o produto do teste são os tropeços, não o programa.

## State
- HEAD: `918b121`, pushado, working tree limpo.
- Live state: **TIA Portal aberto** (sessão 1) com `LIB_TESTE` — o projeto do caderno, herdado de
  sessões anteriores; nada foi tocado nele. `tia.exe` acabou de ser reconstruído
  (`rebuild.ps1` ok, whitelist refeita, 78 verbos): o hash mudou com o Portal já aberto, então a
  **primeira** chamada de verbo pode pendurar num **diálogo modal de autorização do Openness** na
  tela — CPU ~0 é isso, alguém clica e segue. Registrar o clique no resultado (é furo na alegação
  de ponta a ponta).
- Done: nada da rodada. O caderno existe e é a entrada.
- In progress: nada.

## Next steps (ordered)
1. Ler o caderno inteiro antes de tocar no projeto. Depois orientar-se no CLP
   (`tia tree` → `plc-navi.md`) e planejar; o caderno é memorial de cliente, não especificação de
   software — o que ele não diz é para decidir com engenharia, como em obra real.
2. Executar: hardware do item 2 e 3, programa dos itens 4 a 6, no padrão de
   `docs/BOAS-PRATICAS.md` (R1–R9). Verbo de escrita é dry por padrão, `--apply` explícito.
3. Escrever `docs/teste-cego/resultado-FP-04.md`: veredito do item 7 do caderno + **os tropeços**,
   um por linha — onde travou e por quantos turnos, o que teve de adivinhar porque o caderno não
   dizia (de propósito) e o que teve de adivinhar porque a **ferramenta** não dizia (defeito
   nosso), que verbo faltou ou devolveu a coisa errada, e que linha de `SKILL.md`/`VERBS.md`/
   `CLAUDE.md` escrita diferente teria evitado o tropeço. Registrar o relógio: início, fim, onde
   o tempo foi.

## Key files
- `docs/teste-cego/caderno-FP-04.md` — a entrada. Único arquivo de `teste-cego/` a abrir.
- `docs/BOAS-PRATICAS.md` (R1–R9) — o padrão da casa que o item 7 do caderno cobra.
- `docs/VERBS.md` — assinatura dos 78 verbos, ~90 linhas.
- `__navi__.md` da raiz e de `docs/` — orientação em 1 read.

## Open / blockers
- `LIB_TESTE` não tem periferia remota nem inversor: o caderno manda incluir os dois. É a parte da
  rodada sem precedente no projeto de teste.
- Openness é single-session: uma chamada `tia` por vez, sem paralelizar (nem via agente).

## Skills
- tia

## Effort
**Alto.** É programa de PLC novo com decisões de engenharia que o caderno deliberadamente não toma
(dimensionar cartão, escolher onde a lógica mora, como o rodízio conta hora), mais hardware que o
projeto de teste nunca teve. O relógio, porém, é do Portal (~10–20 s por verbo, 2–4 min por
`open-project`) — pensar mais não acelera a parte lenta. Baixar para **médio** só depois do
hardware fechado e compilando.
