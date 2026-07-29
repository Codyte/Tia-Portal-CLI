# Handoff · TIA Portal Openness API · 2026-07-29

## Goal
Fechar o ciclo da biblioteca: **projeto base** (cópia do atual, só arsenal de funções e moldes) →
`bake-lib` gera a `.al21` a partir dele → `install-lib` instala o arsenal completo em qualquer PLC
virgem. Hoje a biblioteca mora na máquina (`.al21` e `library/blocks/` gitignored), não no repo.

## State
- HEAD: `5444a21`. Working tree limpo (só o archive novo em `.handoff/`).
- **Live state**: 1 TIA Portal aberto, projeto `Software de ETE Insular_Inicial_V21`
  (`proj/`, 1 PLC `CPU1.0 CCO`, 62 devices) — **não é produção**, confirmado pelo user. Esse projeto
  tem a árvore **antiga** (`1.2 Inversores`, `1.7 Utilitários`). O user está montando a global
  library **à mão pela GUI** em `src/Tia.Lib/tia-cli/tia-cli.al21`: o conteúdo mudou a cada leitura
  (1 → 8 → 5 → 7 master copies), e a última bake tem a árvore **nova** (`1.1/1.1.1 Inversores`),
  fonte não identificada. `Project1` (tinha `PLC_GEN`/`PLC_ZERO`) está fechado.
- Done nesta sessão (3 commits, todos com `rebuild.ps1` ALL PASS + smoke dry real):
  `import-master-copy --force`, `install-lib -Update`, `bake-lib -Prune`, `new-plc.ps1`,
  `Resolve-LibFile` (glob da `.al21`), roteamento por `ContentType` (UDT/tabela de tag) e recusa de
  nome ambíguo entre níveis da library.
- In progress: nada mid-flight.

## Decisions (and why)
- **Atualizar biblioteca instalada = apagar e recriar.** `CreateFrom` não tem `Override` (ao
  contrário do `Blocks.Import`): nome repetido é recusado. `--force` apaga o de mesmo nome e recria
  (`action: deleted+created`), mesmo desenho do `scaffold --force`. Custo: quem chamava o bloco
  apagado fica `Block call was invalid...` — cicatriz do `move-block`, o chamador precisa ser
  reimportado. Em CPU virgem não existe.
- **`.al21` achada por glob** (`Resolve-LibFile` em `_common.ps1`): o Portal renomeou
  `tia_cli` → `tia-cli` (pasta e arquivo) e o caminho fixo nos dois macros quebrou tudo de uma vez.
- **`bake-lib -Prune` é opt-in.** A `.al21` pode ter pacote assado de outro PLC; bake só enxerga o
  `-Plc` da rodada. Provado no dry: assar do `CPU1.0 CCO` acusaria os 5 blocos base como órfãos.
- **UDT e tabela de tag são master copy válido** (`PlcStruct "Aferição CMD"` apareceu na library).
  Cada um mora na sua composition — `Types.CreateFrom` / `TagTables.CreateFrom`, nunca
  `Blocks.CreateFrom`. **Isso mata a dependência de `library/blocks/<UDT>.xml`** (payload gitignored)
  que hoje impede um clone limpo de instalar.
- **Nome ambíguo recusa, não escolhe.** Mesmo nome em níveis diferentes da library era resolvido
  pelo 1º achado, em silêncio, e o de baixo ficava inalcançável. Agora lista as pastas e exige
  `--name "PASTA/NOME"`. Mesma política do `--portal`.

### Tentado e descartado (não repetir)
- **Preflight de UDT ausente no `install-lib` era falso alarme**: todo o `$ops` é montado antes do
  único `run`, então o `throw` acontece com zero escrita. O buraco real era só a `.al21` ausente.
- **Bake manual pela GUI não substitui `bake-lib`**: o Portal batiza `Copy of Function blocks in X`
  (o script renomeia pro nome da fonte) e o arrasto não cria os master copies dos blocos soltos do
  nível 1 — os 5 FBs de que **todo** pacote depende (instalá-los junto foi o que levou 9 erros → 0).
- Anteriores: telegrama do G120 não é atributo nem plug location · master copy de *pasta de blocos*
  não leva UDT nem tabela (mas UDT solto **é** master copy) · `import-source` exige UTF-8 com BOM ·
  `--out-file`/`--plc` do processo não descem pros steps do `run`.

## Next steps (ordered)
1. **Decidir a fonte da árvore** antes de copiar: o projeto aberto tem a árvore antiga
   (`1.2 Inversores`, `1.7 Utilitários`); o PLANO registra a movida para `1.1/1.1.1 Inversores` +
   `1.7` dissolvida, feita no `PLC_GEN` (projeto fechado). O base deve nascer com a árvore **nova**,
   ou re-aplicar a movida com `move-block` depois de copiar.
2. **Copiar o projeto com o Portal fechado** (cópia da pasta `.ap21` no disco). O CLI não tem
   `save-as`; `use-project.ps1` só abre/fecha.
3. **Podar** até sobrar só arsenal + moldes: `tree` → `plc-navi.md` para inventariar, depois batch de
   `delete-block`/`delete-folder` via `run --script` (`delete-block` não tem `--pattern`; montar a
   lista do `list-blocks --folder`). Guardar `1. FB Bilbiotecas`, `0 Moldes`, os 4 moldes LAD, UDTs,
   `DB GLOBAL` esqueleto e as tabelas de tag genéricas.
4. `compile --apply` até 0 erros → `save-project` → **sanitizar nome de equipamento de cliente**
   (mesmo gate da F4: `AREA_01`, `Motor 1 (MOTOR_01)` — `clone --replace OLD=NEW`).
5. `bake-lib -Plc <PLC do base> -Prune -Apply` → `.al21` com nomes certos, 1 master copy por pacote +
   os blocos soltos. **Acrescentar UDTs e tabelas de tag como master copy** (`add-master-copy
   --name`), agora que o import sabe instalá-los.
6. Validar: `new-plc.ps1 PLC_TESTE "<pacotes>" -Apply` numa CPU virgem → medir contra a régua
   conhecida (4 erros, todos o G120 ausente).
7. Só então: `install-lib` ler o layout final (hoje "base" = bloco solto cuja pasta é exatamente
   `-Root`; master copy na raiz é ignorado) e `packages.json` refletir os nomes novos.

## Key files
- `src/Tia.Core/Library.cs` — `ImportMasterCopy` (roteamento por ContentType + `--force`),
  `FindMasterCopy`/`Collect` (ambiguidade), `AddMasterCopy`, `DeleteMasterCopy`.
- `scripts/install-lib.ps1` — `-Update`, classificação pacote × base (~60-85).
- `scripts/bake-lib.ps1` — `-Prune`. `scripts/new-plc.ps1` — add-device + install-lib + save.
- `scripts/_common.ps1` — `Resolve-LibFile`, `Invoke-Tia`.
- `library/packages.json` — `requires`/`db`/`tags`/`types`/`instances` por master copy.
- `docs/PLANO.md:259-556` — biblioteca (medições por pacote, DB GLOBAL, G120).
- `library/core/README.md` — o padrão `.scl` → `xml/` → `scaffold`, que só cobre 5 itens.

## Open / blockers
- **Quais dos 51 blocos de `1. FB Bilbiotecas` são autorais** e podem ser versionados como
  `.scl`/`.xml`? Decide se o arsenal viaja no Git ou só na `.al21` local.
- **Layout final da library** (UDT na raiz? pacotes dentro de `1. FB Bilbiotecas`?) — o user estava
  testando níveis; `install-lib` precisa do layout decidido para ler certo.
- `--force --apply` **nunca foi exercitado contra o Portal**, só dry. Exige projeto descartável.
- Telegrama do G120 segue parado no clique do user (device view do `SINAMICS G_ZERO`) — são os 4
  erros residuais da CPU virgem. Item 9 (*online*) segue parado no aval (revoga D8).
- Nunca `git add -A` (trilha paralela do `scripts/tia-help.py` no mesmo repo).
- `rebuild.ps1` pode derrubar a 1ª chamada seguinte (`Openness access (0033:000666)` no Portal
  aberto — só o clique resolve). Nesta sessão não bateu.
- `--out-file` em `$env:TEMP` dá caminho 8.3 (`CARLOS~1`) que o Python não abre — usar `workspace/`.

## Effort
**Médio** para os passos 1-3. Raciocínio não é difícil, mas a poda é irreversível no projeto copiado
e "o que é arsenal" não está escrito em lugar nenhum — ler o `plc-navi.md` inteiro e conferir com o
user **é** o trabalho. **Alto** se a poda quebrar dependência que só o compile revela (bloco de área
chamando FB de biblioteca e vice-versa; o lint de camada do `audit` cobre um sentido só). O gargalo
aqui não é pensar: `rebuild.ps1` ~1 min, attach 3-7 s, compile de projeto real em minutos — juntar
operação em `run --script` rende mais que pensar mais.
