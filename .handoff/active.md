# Handoff · TIA Portal Openness API · 2026-08-13

## Goal
Fila de 6 tropeços da FP-06 fechada e conferida contra projeto real, mais o verbo que encurta o
caminho da área nova. Trabalho entregue e pushado — não há tarefa em voo.

## State
- HEAD: `d404327` (`fix(cli): fecha a fila de 6 tropecos da FP-06 e destrava area nova no
  replicate-fc`), pushado pelo usuário. Working tree limpo fora de `workspace/` (gitignored).
- Live state: **TIA Portal aberto na sessão 1 com 3 processos** — todo verbo exige
  `--portal "Software de ETE Insular_Inicial_V21"`. O projeto está como a FP-06 o deixou (área 24
  completa, compile 0/0, audit 10/10) e **nada desta sessão foi salvo**: o único write
  (`clone` do `FB ZZ_TESTE_RETAIN` + `set-retain`) foi apagado junto com a pasta `ZZ_TESTE`.
  `rebuild.ps1` rodou 4×, então o hash do `tia.exe` mudou — o Portal aberto pediu autorização
  modal 2× (`EngineeringSecurityException: operation has timed out` = ninguém clicou; retry
  depois do clique resolve).
- Done: T1–T6 da FP-06 + `replicate-fc --template/--target-folder`; validação ao vivo de cada um;
  `CLAUDE.md`, `PLANO.md` e `resultado-FP-06.md §6.1` atualizados; navindex regenerado; commit+push.
- In progress: nada.

## Decisions (and why)
- **T5 foi consertado em `BlockEdit.Patch`, não em `SetRetain`** — o pre-compile
  (`if (!block.IsConsistent) GetService<ICompilable>().Compile()`, cópia do `DbMember.ExportFresh`)
  vale de uma vez para `set-retain`, `add-call` e `delete-network`. Um guard no lugar
  compartilhado é diff menor que três.
- **Fallback do `_PV_` procura o prefixo `<ID>_`, não `<ID>`** — sem o separador, o id `LIT-5`
  casaria `LIT-51_PV_...` e o FC sairia apontando para o instrumento errado.
- **`gen-alarm-fc --area` não mexe no OB de chamada**: o `CHAMADA_ALARMES` continua saindo com
  todas as FCs sob a pasta-raiz (vem de `CollectFcs`, não da lista de áreas geradas). Escopo é do
  que se escreve, não do que se chama — escopar o OB apagaria as outras 19 chamadas.
- **`--template` que não casa com `EquipmentTypes` falha em vez de cair no molde antigo** — replicar
  com molde errado sobrescreve pasta populada; melhor erro alto.
- **`plug-module`: o `name` do módulo continua o MLFB pedido**, só o `typeIdentifier` é normalizado —
  senão o item plugado nasceria chamado `OrderNumber:6ES7 …`.
- Tentado e descartado: teste offline para `TemplateFor`/`FindFolderByName`/`IncludeFolders` — todos
  recebem tipo do Openness (`PlcBlockUserGroup`, `PlcTagTableUserGroup`) e só rodam com o Portal.
  O único núcleo puro novo (`BlockEdit.StripTypePrefix`) ganhou teste.
- Tentado e descartado: `FB BITS TO WORD` como cobaia do repro do T5 — não tem membro `Static`, e
  `set-retain` só marca Static. Quem serviu foi `clone` do `FB CASCATA DE BOMBAS`.

## Next steps (ordered)
1. Nada obrigatório. A próxima rodada natural é a **FP-07** (caderno novo em
   `docs/teste-cego/`), que é o que mede se estes 7 consertos seguram — como a FP-06 mediu os da
   FP-05.
2. Se a FP-07 for de área nova, **exercitar `replicate-fc --template/--target-folder` com `--apply`**:
   até agora só foi provado em dry-run (o dry-run declarou os 5 alvos certos, mas nenhuma escrita).
3. `set-io-address --conflictCheck` (conserto da FP-05) segue **não exercitado** em projeto real —
   segundo ciclo sem ser tocado.

## Key files
- `docs/teste-cego/resultado-FP-06.md` §6.1 — tabela dos 6 consertos com a prova de cada um.
- `docs/PLANO.md` (bloco FP-06, ~L508) — fila fechada + o verbo novo.
- `src/Tia.Core/__navi__.md` — mapa da pasta (regenerado); os 5 arquivos tocados são
  `Replicate.cs` (`TemplateFor` L270, `FindFolderByName` L282), `BlockEdit.cs` (`Patch` L235,
  `StripTypePrefix` L283), `InstrumentFc.cs` (PvTag ~L220), `AlarmFc.cs` (`IncludeFolders` L78),
  `Hardware.cs` (`PlugModule` L143).
- `workspace/val/` (gitignored) — batches A–E da validação ao vivo; `batch-d.json` é o repro do T5
  (clone + set-retain no mesmo batch), útil para reproduzir a classe de erro.

## Open / blockers
- Nada bloqueando.
- `rebuild.ps1` muda o hash do `tia.exe` → o Portal aberto abre diálogo modal. Chamada pendurada
  com CPU ~0 é clique pendente, não bug: aceitar na tela e repetir a chamada.
- Compile do PLC inteiro leva ~5 min: sempre em background.

## Skills
- tia

## Effort
**Baixo** — não há passo obrigatório; o passo 1 é preparar caderno de teste cego, trabalho de
escrita, não de investigação. Suba para **médio** se a FP-07 for executada (decisão de engenharia de
PLC ao vivo) e para **alto** só se algum dos 7 consertos falhar contra o Portal contradizendo o que
foi medido hoje. Reasoning não é o gargalo em nenhum caso: o relógio é do `rebuild.ps1`, do compile
do Portal e do diálogo modal de autorização.
