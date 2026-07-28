# Handoff · TIA Portal Openness API · 2026-07-28

Split de 2 tracks **fundido** — sessão única a partir daqui. Histórico dos dois em
`.handoff/archive/2026-07-28T125409.md`.

## Goal
F8 (caminho de escrita contra projeto real) está fechado. Próximo alvo: **fatia 2 da biblioteca de
blocos** — escrever os ~10 itens autorais do núcleo genérico (tabela em `docs/PLANO.md`, seção
"Biblioteca de blocos"), e o teste da biblioteca contra o Portal.

## State
- HEAD: 2849fca. Working tree limpo.
- Portal aberto na sessão 1 com **Software de ETE Insular_Inicial_V21** (cópia de teste, backup do
  user, dano autorizado). PLC compila **Success / 0 erros / 0 warnings** + `save-project` feito.
- **F8 fechado.** Primitivas 11/11 ✅, `import-ladder --apply` ✅, 6 geradores ✅ em dry + payload
  importado, e nesta rodada: `replicate-fc --apply` no tipo `Soprador` da árvore de produção
  (2 alvos S-01B/C, batch 0 falhas, 2 compiles 0 erros, conteúdo conferido — 5 linhas de 1993
  diferem do molde, todas devidas), `gen-profinet --apply` (no-op) e `standardize-tags --apply`
  (5 tabelas rebuilt). Detalhe completo na linha F8 do PLANO.
- **Biblioteca fatia 1 fechada** (offline): `library/library.json` (manifesto, `Source: "blocks"`),
  `library/export-all.json` (batch inverso, 66 exports, 1 attach), `library/README.md` (inventário
  + como repor + o que cada gerador exige), `library/blocks/` = 66 XMLs / 3,3 MB **fora do Git**
  (`.gitignore`). Removido `scripts/export-fixtures.ps1` (cobria 15 dos 66).
- Tudo que foi escrito nos sandboxes vive em `ClaudeTest/`, `ClaudeTest/Sub`, `ClaudeTest/Gen`
  (+ `DB INSTRUMENTOS` na raiz). User mandou deixar lá e continuar usando essas pastas.

## Decisions (and why)
- **`replicate-fc --apply` exige `--force`** quando a pasta-alvo já tem blocos — guard correto,
  não bug. Sem ele: `2 target folder(s) already have blocks…`.
- **Idempotência do `replicate-fc` é funcional, não no-op**: o verbo não detecta in-sync, reimporta
  e recompila. Resultado idêntico, 0 erros. Não "consertar" sem motivo novo.
- **`replicate-instruments --apply` cortado** — dry dá `in-sync`, não escreveria nada.
- **Empacotamento da biblioteca**: `.scl` padrão (diffável, imune à versão do Engineering; limitação:
  bloco nasce na raiz, contorno = `export-block` → `import-block --folder` → `delete-block`);
  `.xml` só pro que precisa nascer em LAD; `.al19` descartado (binário). `import-ladder` não serve
  pra escrever biblioteca (sem timer, sem aritmética).
- **Repo é público** (`github.com/Codyte/TIA-Portal`) — nenhum payload de cliente versionado
  (gate explícito na linha F4 do PLANO).
- Otimizar verbo é alvo errado: attach = 2,9s fixo, amortizado por `run --script`. Critério de
  sucesso é `compile` 0 erros, não tempo.

## Next steps (ordered)
1. **Teste da biblioteca contra o Portal** (nunca rodou): `scaffold --manifest library/library.json`
   em dry no projeto de referência → esperado 66/66 `skip (exists)`; depois
   `run --script library/export-all.json` → 66 arquivos de volta em `library/blocks/`
   (exige PLC compilado antes — bloco inconsistente não exporta).
2. **Fatia 2 da biblioteca**: escrever os ~10 itens autorais do núcleo genérico (tabela no PLANO,
   cada item já linkado ao default do gerador que o exige).
3. **Bug real do `scaffold`**: item UDT ignora `Folder` — `src/Tia.Core/Scaffold.cs:126` importa todo
   `SW.Types.*` na raiz do `TypeGroup`, enquanto bloco e tabela resolvem caminho. Correção =
   `ResolveTypePath` análogo aos outros dois. Exige `rebuild.ps1`.
   (O gap "`scaffold` não ordena UDT antes de DB/FC" **não existe** — `Scaffold.Rank` sempre teve
   `SW.Types` = 0, `Scaffold.cs:58`. Não reintroduzir.)
4. **Sanitizar `docs/examples/*.xml`**: são fixtures de projeto real E estão versionados num repo
   público — `clone --replace OLD=NEW` ou trocar por sintéticas.
5. Fatia 3 da biblioteca (utilitários genéricos: escala, debounce, first-out, watchdog, rampa).

## Key files
- `docs/PLANO.md` — linha **F8** (fechada), linha **F4** (gate de publicação), seção
  **"Biblioteca de blocos"** (fatias + tabela do núcleo genérico).
- `library/library.json` · `library/export-all.json` · `library/README.md`.
- `docs/examples/replicate-fc-soprador.json` (config escopado) ·
  `docs/examples/replicate-soprador-run.json` (batch, já com `--force`).
- `src/Tia.Core/Scaffold.cs:126` — bug do `Folder` do UDT · `:58` — `Rank` (correto).
- `src/Tia.Core/Ops.cs:213` guard de inconsistência · `:311` `ImportSource` gera blocos.
- `src/Tia.Core/LadConverter.cs:355-397` — pinos e parte `O`; verdade em
  `docs/examples/BombaTemplateFc.xml:346` e `:1044-1058`.

## Open / blockers
- `scaffold`/`add-device`: bug dos bytes de system/clock memory (separado do bug do `Folder`).
  `import-master-copy`: sem `.al19` de teste.
- Sem `checkpoint`/`restore` (F7 item 4): ponto de retorno = `save-project` + backup do user.
- Regra dura que continua valendo: todo import deixa o alvo **e quem o referencia** inconsistente e
  o Openness recusa exportar bloco inconsistente → `compile --apply` entre etapas.
- Chamada pendurada com `tia.exe` vivo e CPU ~0 = diálogo de aceite do Openness na tela: pedir o
  clique, não investigar código.
