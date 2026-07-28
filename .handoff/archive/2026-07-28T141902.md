# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Ciclo que está funcionando: **executar uma operação real no projeto → medir onde ela doeu →
virar verbo/flag no CLI**. Duas rodadas fechadas (reorganizar uma pasta de 34 blocos, depois as
6 otimizações que essa operação expôs). Próximo alvo do mesmo ciclo: **orientação em projeto
novo / `raio-x.ps1`** — é a operação cara que ainda não passou por essa moagem.

## State
- HEAD: f88f020. Working tree limpo.
- Portal na sessão 1 com **Software de ETE Insular_Inicial_V21** (cópia de teste, dano autorizado).
  479 blocos, 0 inconsistentes, compile Success, `save-project` feito.
- **Fatia 1 da biblioteca testada no Portal** ✅ — `scaffold` dry = 33 pastas + **65/65 skip**;
  `run --script library/export-all.json` = 65/65 ok. Export é determinístico a menos do
  `<DocumentInfo><Created>` (hash muda sempre, conteúdo não).
- **Fatia 2, parte SCL** ✅ — `library/core/`: `MotorDados`, `ValvDados`, `MotorPrincipal`
  (composto de dois `MotorDados`), `DB GLOBAL` (esqueleto) e `FB BITS TO WORD`. Importados com
  sufixo `_T` pra não colidir: compile 0 erros. Os `_T` foram apagados com o `delete-type` novo.
- **`1. FB Bilbiotecas` reorganizada** ✅ — 33 FBs em 7 subpastas por função (`1.1 Acionamento` …
  `1.7 Utilitários`); projeto, `library.json` e README em sincronia.
- **6 otimizações de contexto no CLI** ✅ (2baff96) — tabela em `docs/PLANO.md`, seção
  "Otimização de tokens do CLI": filtros do `list-blocks`, `move-block`, `run --summary`,
  `docs/VERBS.md`, UTF-8 na rota da task, `--types` em create/delete-folder.
- **Fixtures públicas sanitizadas** ✅ e **bug do `Folder` de UDT no `scaffold`** ✅ corrigido.

## Decisions (and why)
- **Openness não move bloco**: `export` (de TODOS antes) → `delete` → `import --folder`. Importar
  antes de apagar falha com *"A program element with this fully qualified name already exists in
  this CPU"*; e não dá pra exportar depois do primeiro delete, porque o delete deixa quem
  referencia inconsistente e bloco inconsistente não exporta. Regra encapsulada em `move-block`.
- **Apagar FB não leva junto as instance DBs** — verificado com um FB de 1 iDB e com os de 36.
- **`DIAG to STRING_DB` saiu dos manifestos** — o user apagou do projeto (iDB de teste em pasta
  errada). Manifesto tem **65 itens**, não 66; o XML continua em `library/blocks/`.
- **Métrica de otimização = chamada de ferramenta e KB de saída**, não tempo: attach é 2,9 s fixo e
  amortizado por `run --script`. Critério de sucesso continua sendo `compile` 0 erros.
- Repo é público (`github.com/Codyte/TIA-Portal`) — payload de cliente fica gitignored.

## Next steps (ordered)
1. **Próxima rodada do ciclo**: rodar `pwsh scripts/raio-x.ps1 <Proj>` e medir chamadas + KB de
   saída. Alvos já visíveis: `snapshot` (251 KB) e `find --kind tag` (821 KB) não ganharam filtro
   como o `list-blocks`; falta `--folder`/`--count` em `list-tags` e em `find`, e `xref`/`trace`
   devolvem tudo.
2. **Fatia 2, os 4 moldes em LAD** (`MODULE_ERROR_MOLDE`, `FC_Modelo`, `OB_MOLDE_ALARMES`,
   `MOLDE_ANALOGS`) — não dá em SCL, os geradores clonam rede a rede.
3. **Assar `library/core/*.scl` → `.xml`** num projeto vazio pra instalar via `scaffold`
   (`Scaffold.Plan` lê o tipo do XML e `import-source` não tem `--folder`).
4. Fatia 3 (utilitários genéricos: escala, debounce, first-out, watchdog, rampa).
5. Sanitizar **nome de projeto de cliente em prosa** (`Insular`, `ETE SG`, `AsBuilt`) em docs,
   `scripts/raio-x.ps1`, `__navi__.md` e no `.handoff/` — reescreve histórico já commitado,
   decisão do user.

## Key files
- `docs/VERBS.md` — assinatura de todo verbo, gerada do help pelo `rebuild.ps1`.
  **Ler isto em vez de grepar `Program.cs`.**
- `docs/PLANO.md` — "Otimização de tokens do CLI" (o que já foi otimizado e por quê) ·
  "Biblioteca de blocos" (fatias 1–3 + tabela do núcleo genérico).
- `src/Tia.Core/Ops.cs:290` `MoveBlock` · `:130`/`:155` folders com `--types` · `:205` `DeleteType`.
- `src/Tia.Core/Inventory.cs:79` `Blocks(plc, folder, type, countOnly)`.
- `src/Tia.Cli/Program.cs:216` `run --summary`.
- `library/core/README.md` — contrato de cada item SCL · `library/README.md` — inventário dos 65.
- `scripts/gen-verbs.ps1` — gera o `VERBS.md`.

## Open / blockers
- `scaffold`/`add-device`: bug dos bytes de system/clock memory. `import-master-copy`: sem `.al19`.
- Sem `checkpoint`/`restore`: ponto de retorno = `save-project` + backup do user.
- Regra dura: todo import deixa o alvo **e quem o referencia** inconsistente → `compile --apply`
  entre etapas.
- Chamada pendurada com `tia.exe` vivo e CPU ~0 = diálogo de aceite do Openness na tela: pedir o
  clique. `EngineeringSecurityException` logo depois de um `rebuild.ps1` é a mesma família
  (whitelist nova) — repetir a chamada resolveu.
