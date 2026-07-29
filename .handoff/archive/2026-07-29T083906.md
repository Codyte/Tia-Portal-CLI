# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Foco (definido pelo user): **funcionalidades do CLI**. O backlog v2 do PLANO está fechado —
sobram só os dois itens travados em decisão/ação do user (telegrama do G120, item 9 *online*).

## State
- HEAD: `61fb261` (`perf(raio-x): 2 attaches em vez de ~12`). Working tree limpo fora do
  `.handoff/archive/`.
- **Live state**: 2 TIA Portal na sessão 1 — PID 240 = `Software de ETE Insular_Inicial_V21`
  (**não é produção**, confirmado pelo user) e PID 6920 = `Project1` (descartável, **não salvo**).
  Todo verbo exige `--portal <nome|PID>`; `Project1` tem 15 CPUs + `SINAMICS G_ZERO`, e um PLC de
  1200 (`PLC_1`, estação `S7-1200 station_1`) que serve de alvo negativo pro gate de família.
  `workspace/Project1/` tem o raio-x fresco (plc-navi.md, snapshot, xref-obs, AML).
- Done nesta sessão (5 commits, todos com smoke real revertido): `set-tag`, `rename-block`,
  `edit-db-member`, `Cpu` no manifesto do scaffold, `raio-x.ps1` em 2 attaches + 2 bugs de CLI
  corrigidos na raiz (`--out-file` por step do `run`; `list-blocks --type` casava substring).
- In progress: nada mid-flight.

## Decisions (and why)
- **`rename-block` não precisa de export/delete/import**: `Name` é RW na API para CodeBlock/DB/UDT
  (ajuda: *Setting attributes of Blocks, DBs & UDTs*), e é o mesmo caminho do GUI — xref do iDB
  antes e depois mostra o mesmo caller, **sem cicatriz** no vínculo chamada↔iDB. Só `move-block`
  precisa da coreografia (o Openness não move).
- **`set-tag` mexe direto no `PlcTag`**: `DataTypeName`/`LogicalAddress`/`Name` são RW
  (`Name` só V20+, e a V21 aceitou). Campo não passado não muda; nada a mudar = `skip (no change)`.
- **`edit-db-member` vai por XML** (membro de DB não é atributo). Troca de tipo remove o
  `<Sections>` da instância antiga. **Dois edits seguidos exigem `compile --apply` no meio** — o
  import deixa o DB inconsistente e o export recusa. Rename **não** corrige referências: o
  resultado carrega o aviso.
- **Gate de família compara só letras e dígitos** (`S7-1500` == `s7 1500` == `S71500`), contra o
  `TypeIdentifier` da estação (`System:Device.S71500`). Falha **antes** de escrever; `--force`
  importa e reporta o mismatch em `cpu`; estação ilegível não bloqueia.
- **`--out-file` e `--plc` do processo NÃO descem pros steps do `run`** — cada step carrega os seus.
  Foi o que quebrou a 1ª versão do raio-x novo (nenhum `.json` saía).

### Tentado e descartado (não repetir)
- **Handoff anterior errou**: não existia `DbMember.Edit` no Core — `Edit` é a struct do `Add`.
  O verbo foi escrito do zero (`Change` + `ChangeInXml`).
- **Telegrama do G120 não é atributo nem plug location**: `list-attrs` na `PROFINET interface` (20)
  e no head (16) não têm nada de telegrama; não aparece em `GetPlugLocations` nem depois do
  `connect-subnet`; o identificador não está na ajuda nem em nenhum AML do repo. O do G120X GSD
  (`.../SM/IDS_TEL20`) não serve (lá a interface se chama `PN-IO`).
- `PM240-2` com `OrderNumber:6SL3210-1PE16-1ALx` → `canPlug: false` (falta sufixo de versão).
- Manifesto de `scaffold` resolve `Source` **relativo ao arquivo do manifesto**.
- Anteriores: master copy não leva UDT/tabela de tag · `import-source` exige UTF-8 com BOM ·
  `list-blocks --folder ""` devolve array cru · `move-block` in-place deixa cicatriz no vínculo.

## Next steps (ordered)
1. **Perguntar ao user qual dos dois destravar** (a resposta é dele; não sobrou trabalho
   não-bloqueado no backlog): (a) **telegrama do G120** — precisa do arrasto de
   `Standard telegram 20, PZD-2/6` na device view do `SINAMICS G_ZERO`; depois `list-devices`
   revela o `TypeIdentifier` e o `plug-module` automatiza (fecha os 4 erros do `install-lib`);
   (b) **item 9 *online*** — go-online/download/compare/start-stop, **revoga D8**, só com aval
   explícito.
2. Se nenhum dos dois: `delete-db-member` (único par que falta do `add`/`edit`), ou varrer
   `audit`/`doctor` do Insular atrás do próximo buraco de escrita.
3. User abriu o Insular dizendo "projeto base que será copiadas as fcs" e **nunca disse quais FCs** —
   pergunta em aberto desde a sessão passada.

## Key files
- `src/Tia.Core/Ops.cs` — `SetTag`, `Rename` (perto de `AddTag`/`DeleteTag`/`ImportType`).
- `src/Tia.Core/DbMember.cs` — `Change` + `ChangeInXml` (núcleo puro, testado offline).
- `src/Tia.Core/Scaffold.cs` — `CheckFamily`/`SameFamily`; `ScaffoldManifest.Cpu`.
- `src/Tia.Cli/Program.cs` — `run` (out-file por step, ~230), `Print`/`WriteOut` (~670).
- `scripts/raio-x.ps1` — 2 batches, `-Portal`/`-Plc`.
- `docs/VERBS.md` (gerado pelo rebuild) · `docs/PLANO.md` itens 12-14 do backlog.

## Open / blockers
- Telegrama do G120: parado no clique do user (acima).
- Item 9 *online*: parado no aval do user (revoga D8).
- **`rebuild.ps1` pode derrubar a 1ª chamada seguinte** (hash novo → `Openness access (0033:000666)`
  no Portal aberto). Repetir o comando resolve. Nesta sessão não bateu nenhuma vez.
- Nunca `git add -A` (trilha paralela do `scripts/tia-help.py` no mesmo repo).
- `--out-file` em `$env:TEMP` dá caminho 8.3 (`CARLOS~1`) que o Python não abre — usar `workspace/`.

## Effort
**Baixo** para o passo 1 — é uma pergunta ao user, não trabalho. Se ele escolher (a), segue baixo:
`plug-module` já existe e só falta o `TypeIdentifier` que a GUI vai revelar. **Alto** se escolher
(b) *online*: escrita no CLP, blast radius real, D8 revogada. Gargalo desta linha não é raciocínio:
cada `rebuild.ps1` custa ~1 min e cada attach 3-7 s — juntar smoke em `run --script` rende mais que
pensar mais.
