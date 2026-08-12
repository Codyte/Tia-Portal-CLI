# Handoff · TIA Portal Openness API · 2026-08-12

## Goal
Executar a **FP-05** — rodada cega de teste da ferramenta, escrevendo a Área 4 `Recirculação` do
`docs/teste-cego/caderno-FP-05.md` dentro do projeto-molde real já aberto. O produto do teste são os
tropeços medidos da CLI, não o programa; o programa é só o pretexto que os revela.

## State
- HEAD: `1ca77f2`, working tree com `docs/teste-cego/caderno-FP-05.md` e `criterios-FP-05.md` novos
  (ainda não commitados) + `standing.md` editado.
- Live state: **TIA Portal aberto na sessão 1 com `proj/Software de ETE Insular_Inicial_V21`**
  (62 devices, PLC `CPU1.0 CCO`, 475 blocos, 96 pastas, 195 tabelas, 13 UDTs, 36 acionamentos).
  Nada foi escrito nele nesta sessão — as chamadas de ontem e de hoje foram todas read-only ou
  dry-run. `audit` = 10/10 verde. É o estado inicial da FP-05.
- Done: fila da FP-04 fechada, 8 de 8. Telegrama de drive no `list-io-map`/`list-telegrams`,
  `plugAs` provado ao vivo, `scanned` no `audit`.
- In progress: nada em vôo. A FP-05 começa do zero, com o caderno já escrito.

## Decisions (and why)
- **A rodada é no projeto-molde real, não em `LIB_TESTE`** — decisão do usuário. Ganho: primeira
  rodada em projeto grande de verdade (475 blocos), que é onde orientação e colisão de endereço
  doem. Custo: o molde não tem backup, então **nunca salvar** (está no `standing.md`).
- **O caderno tem 4 armadilhas plantadas na seção 6**, e obedecer ou recusar são os dois desfechos
  válidos — o que não vale é decidir sem registrar. Detalhe em `criterios-FP-05.md`, que **não deve
  ser aberto antes de executar** (é a régua, escrita antes da prova).
- **`list-io-map` mudou ontem**: agora conta os 34 telegramas de drive, então `nextFreeByte` é
  `Input: 664` / `Output: 392`, não o que era antes. Endereçar a área nova a partir daí.
- Rejeitado: rodada só para "ver os 4 checks do `audit` reprovando". Check cego já foi resolvido pelo
  `scanned` (o R8 examinou 46 blocos e aprovou os 46); forçar vermelho artificialmente não mede nada.

## Next steps (ordered)
1. **Ler `docs/teste-cego/caderno-FP-05.md` inteiro** e mais nada de `teste-cego/` — em especial,
   não abrir `criterios-FP-05.md`.
2. `pwsh scripts/tia.ps1 doctor` e `tia audit --out workspace/fp05-antes` para carimbar o estado
   inicial (o `scanned.blocks` de antes é a linha de base do G5).
3. `tia tree` → `plc-navi.md` para achar onde a Área 4 nasce, e `list-io-map --out-file` para o
   próximo byte livre de cada tipo. Orientação é uma leitura, não um `snapshot`.
4. Hardware: periferia nova (DI/DO/AI/**AO** — saída analógica é a primeira vez em qualquer rodada),
   `plug-module --item Rack_0` (o alvo é o rack; MLFB sem versão devolve `plugAs`), `set-io-address`,
   `connect-subnet`.
5. Programa: UDT → DB → blocos da área → chamada em LAD via `add-call` → `set-retain` no horímetro
   e no contador de partidas → integração na chamada cíclica.
6. `compile --apply` a cada etapa (import deixa o alvo inconsistente) e `audit --out` no fim.
7. Escrever `docs/teste-cego/resultado-FP-05.md` no formato das rodadas anteriores: entregue,
   tropeços medidos, fila que sai deles. **Anotar o relógio e o número de chamadas desde o começo** —
   é a métrica I4 e não dá para reconstruir depois.
8. Fechar: **`close-project` sem `--save`** (ou deixar aberto e avisar o usuário). Nunca salvar.

## Key files
- `docs/teste-cego/caderno-FP-05.md` — a entrada da rodada. É o que se executa.
- `docs/BOAS-PRATICAS.md` — R1–R9, o aceite de qualquer programa novo.
- `docs/VERBS.md` — assinatura dos 78 verbos.
- `CLAUDE.md` do repo — invariantes de operação (dry/`--apply`, escape `\/`, `--out-file`, macros).
- `docs/teste-cego/resultado-FP-04.md` — formato do relatório a escrever no fim.
- `src/Tia.Core/__navi__.md` e `src/Tia.Cli/__navi__.md` — só se a rodada precisar mexer no CLI.

## Open / blockers
- Commitar os dois arquivos novos de `docs/teste-cego/` antes de começar (caminhos explícitos,
  nunca `git add -A`).
- Depois de qualquer `rebuild.ps1`, o Portal já aberto pode devolver `EngineeringSecurityException`:
  `Start-ScheduledTask -TaskName TiaWhitelist` resolve sem UAC e sem reabrir o projeto.
- O gargalo da rodada é o relógio do Portal (10-20 s por chamada), não o raciocínio.

## Skills
- tia

## Effort
**Médio** para o passo 1 e para toda a rodada: é execução de sequência documentada, com decisão de
engenharia pontual (as armadilhas da seção 6 do caderno). Suba para **alto** só se um verbo se
comportar contra o que o `CLAUDE.md` promete — aí virou sonda de API e vale pensar antes de tentar.
Mais raciocínio não acelera: o laço é ditado pelo relógio do Portal, não pelo modelo.
