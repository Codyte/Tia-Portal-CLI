# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Provar a engine ponta a ponta com o caderno fictício FP-01. Esta rodada entregou o item 9 (projeto
compilando), mas **não foi cega** — a sessão herdou o handoff. Falta a rodada cega de verdade.

## State
- HEAD: `381f331` (commit local, não pushado).
- Live state: **TIA Portal aberto na sessão 1**, projeto `workspace/blind/FP01/FP01.ap21` aberto e
  **salvo** (compile Success / 0 erros / 0 warnings). Shell do agente na sessão 0 (rota da task).
  `tia.exe` rebuildado 3x nesta sessão, whitelist refeita, **sem** diálogo modal de autorização.
- Done: hardware completo (CPU 1515-2 PN, ET200SP com DI16/DQ16/AI8/servidor, G120 com telegrama
  20, subnet + IO system), 35 tags em 4 tabelas (27 pontos do caderno conferidos, 0 divergências),
  22 blocos autorais em 13 pastas (SCL via `import-source`), `Main (OB1)` →
  `CICLO_FILTRO_PRENSA (FP-01)` → `FB SEQUENCIA_FP-01`. Relatório em
  `docs/teste-cego/resultado-2026-08-07.md`. 3 correções de CLI commitadas.
- In progress: nada mid-flight.

## Decisions (and why)
- **Programa 100% autoral em SCL**, sem `install-lib`/`replicate-fc` — o caderno foi desenhado pra
  não cair na biblioteca, e usar os geradores anularia o teste. Consequência assumida: `audit` fecha
  3/5 (acionamento com 2 blocos em vez de 6, sem tabela de tag por acionamento).
- **`import-cax` descartado como caminho de endereço de módulo** — aceita o AML com `StartAddress`
  editado e ignora em silêncio. Virou o verbo `set-io-address`.
- **Endereço do telegrama do G120 achado por sonda de conflito** (mover o AI da ET200SP pelo mapa
  até `set_StartAddress` recusar): `%IB256..267` / `%QB256..259`. Nem `DeviceItem.Addresses`, nem os
  atributos do `Telegram`, nem o CAx expõem isso. 18 chamadas pro que o Portal mostra num clique.
- **Pastas de tag saíram como `1. I-OS`**, não `1. I/OS` da lei — `create-folder --path` usa `/`
  como separador e não expressa nome com barra (o `scaffold` resolve com lista de segmentos).
- `TITLE` em bloco SCL **aborta o lote inteiro** do `import-source` — virou comentário.
- `move-block` em lote só funciona intercalado com `compile --apply` (17 moves + 17 compiles).

## Next steps (ordered)
1. **Rodar o teste cego de verdade**: `/clear`, sessão nova recebe só `caderno-FP-01.md` + a skill
   `tia`, sem handoff. Projeto novo (`create-project`), nunca o `FP01` já pronto. Registrar em
   `docs/teste-cego/resultado-<data>.md` — o produto são os tropeços, não o veredito.
2. Antes disso, considerar tapar o buraco que mais custou: um `list-io-map` (ou endereço no
   `list-telegrams`) e uma nota no `CLAUDE.md` sobre `plug-module --item Rack_0` + sufixo de
   firmware obrigatório em módulo de ET200SP.
3. `git push` do `381f331`.
4. Depois: números manuais (item 1 do `BENCHMARKS.md`) e gravação de tela.

## Key files
- `docs/teste-cego/resultado-2026-08-07.md` — os 9 tropeços, com o que é defeito de ferramenta e o
  que é falta de documentação.
- `docs/teste-cego/criterios.md` — a régua; **não** vai para a sessão cega.
- `workspace/blind/*.scl`, `workspace/blind/tags/*.xml`, `workspace/blind/ops-*.json` — todo o
  material da rodada (gitignored).
- `src/Tia.Core/Hardware.cs` — `SetIoAddress`; `src/Tia.Core/Ops.cs` — `ProjectOf` + `EnsureCultures`
  nos dois imports.
- `src/__navi__.md` — **desatualizado** (verbo novo não entrou); regenerar com `pwsh scripts/navi-cs.ps1`.

## Open / blockers
- Os 21 warnings da 1ª compilação nunca foram lidos um a um — depois dos moves o Portal fecha 0/0 e
  não reemite a lista. Sem caminho conhecido pra forçar rebuild-all por Openness.
- O projeto `FP01` fica aberto no Portal; a rodada cega precisa dele fechado ou de um Portal só.

## Skills
- tia
- ponytail
- caveman

## Effort
**Alto** para o passo 1 — é a prova, e a sessão cega vai encontrar API se comportando fora do
documentado (foi o que dominou esta rodada). Não é o relógio que manda: os `compile`/`move-block`
custam segundos, o gargalo é decidir sem documentação. Se o passo 2 for feito antes, cai pra médio.
