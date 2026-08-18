<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L10    Critérios de aprovação — teste cego FP-01 -->
<!--   L16    Portões objetivos (passa/não passa) -->
<!--   L28    Inspeção (julgamento do observador, registrado por escrito) -->
<!--   L45    Condução -->
<!--   L54    O que se registra (é este o produto do teste) -->
<!-- ======================= END NAV INDEX ======================= -->

# Critérios de aprovação — teste cego FP-01

Escritos **antes** da rodada, em 2026-08-07. Não editar depois que a sessão cega começar; se um
critério se mostrar mal formulado, registrar isso como resultado do teste em vez de reescrever a
régua no meio da prova.

## Portões objetivos (passa/não passa)

Cada um é verificável por um comando, sem julgamento. Reprovar em qualquer um dos quatro reprova a
rodada.

| # | Portão | Como verificar | Aprova se |
|---|---|---|---|
| G1 | O projeto compila | `tia compile --plc CPU_FP01 --errors --apply` | 0 erros. Warnings permitidos, mas contar e registrar quantos |
| G2 | Hardware presente e conectado | `tia list-devices`, `tia list-telegrams --device BL-01` | CPU 1515-2 PN, ET200SP com 16 DI / 16 DO / ≥2 AI, inversor G120 com telegrama 20, os três na mesma sub-rede e o inversor como IO device da CPU |
| G3 | Endereçamento fiel à lista de I/O | `tia find --pattern "*" --kind tag --out-file workspace/fp01-tags.json` e conferir contra o item 4 do caderno | Os 28 pontos endereçados (16 DI, 10 DO usadas, 2 AI) existem, com **exatamente** os endereços do caderno |
| G4 | Sequência existe e roda | `tia xref --name <bloco da sequência>` | O bloco da sequência é chamado por um OB cíclico; a chamada não está órfã |

## Inspeção (julgamento do observador, registrado por escrito)

Não reprovam sozinhos, mas um resultado ruim aqui com G1–G4 verdes significa "compila mas não
serve" — que é uma informação tão importante quanto o compile.

- **I1 — Lógica de fato implementada.** `tia explain-block` na sequência e nos blocos de
  intertravamento. Os 9 passos e os 8 intertravamentos do caderno estão lá, ou o programa é uma
  casca que compila vazia? Anotar quais itens ficaram de fora.
- **I2 — Padrão de pastas da casa.** `tia audit --plc CPU_FP01` + `tia tree`. Pastas de equipamento
  com `(TAG)` no nome, blocos sufixados pelo TAG, alarmes agrupados. Registrar a contagem de
  violações do `audit`.
- **I3 — Segurança não foi diluída.** Emergência, grade e `PSH-01` derrubam saída no mesmo ciclo,
  sem depender do passo da sequência. Manual respeita intertravamento.
- **I4 — Quanto veio de gerador e quanto foi escrito à mão.** Contar blocos vindos de
  `install-lib` / `replicate-fc` / `gen-alarm-fc` contra blocos autorais. Uma máquina que caísse
  inteira na biblioteca não testaria nada.

## Condução

- **Quem escreveu o caderno não executa.** A sessão que rodar recebe o `caderno-FP-01.md`, a skill
  `tia` e nada mais desta conversa. Este arquivo de critérios **não** vai para a sessão cega.
- Projeto novo e vazio (`create-project`), nunca um projeto existente. Um TIA Portal só aberto.
- Sem toque no GUI. Se o operador precisar clicar em alguma coisa (diálogo de autorização do
  Openness conta), registrar o quê e por quê — cada clique é um furo na alegação de ponta a ponta.
- Registrar o relógio: início, fim, e onde o tempo foi.

## O que se registra (é este o produto do teste)

Mais importante que o veredito: **os tropeços**. Para cada um, uma linha em
`docs/teste-cego/resultado-<data>.md`:

1. Onde a sessão travou, e por quantos turnos.
2. O que ela teve que adivinhar porque o caderno não dizia (é de propósito — obra real também não
   diz) e o que ela teve que adivinhar porque a **ferramenta** não dizia (esse é defeito nosso).
3. Que verbo faltou, ou qual existente devolveu a coisa errada.
4. Que parte do `SKILL.md` / `VERBS.md` / `CLAUDE.md` teria evitado o tropeço se estivesse escrita
   diferente. Travou por falta de documentação = defeito da skill, não da sessão.
