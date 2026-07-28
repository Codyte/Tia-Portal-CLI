# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Foco atual (definido pelo user): **funcionalidades do CLI**, não mais a biblioteca/hardware.
Backlog v2 fechado exceto o item 9 (*online*, exige revogar D8). Fechar os buracos que sobraram
na superfície de escrita do `tia`.

## State
- HEAD: `95a0883` (`feat(cli): set-attr, add-tag, delete-tag`). Working tree limpo fora do
  `.handoff/archive/`.
- **Live state**: 2 TIA Portal na sessão 1 — PID 240 = `Software de ETE Insular_Inicial_V21`
  (**não é produção**, o user confirmou; salvo depois de apagar a pasta de teste `ClaudeTest`) e
  PID 6920 = `Project1` (descartável, **não salvo**). Todo verbo exige `--portal <nome|PID>`.
  `Project1` tem 15 CPUs de teste + `SINAMICS G_ZERO` (G120 criado nesta sessão, ligado ao IO
  system do `PLC_ZERO`, subnet `PN/IE_1`). User decidiu **não apagar** nenhuma CPU por ora.
- Done nesta sessão: verbos `plug-module`, `delete-device`, `list-attrs`, `set-attr`, `add-tag`,
  `delete-tag`; lint de camada no `audit`; `scaffold --force` = delete+reimport corrigido.
  Todos com smoke real no `PLC_ZERO`, revertidos no fim. `audit` do projeto de referência: 6/6 limpo.
- In progress: nada mid-flight.

## Decisions (and why)
- **`--force` não apaga sempre**: `ImportOptions.Override` já resolve na mesma pasta e preserva o
  vínculo chamada↔iDB; só a exceção *"already exists in this CPU"* (mesmo nome noutra pasta — nome
  de bloco é único no PLC) dispara delete+reimport. Try/catch na mensagem, sem calcular caminho.
- **`set-attr` tipa pelo valor atual** (enum inclusive): o Portal recusa `int` onde espera byte e
  `"True"` onde espera bool. Atributo desconhecido falha antes de escrever, apontando o `list-attrs`.
- **`add-tag` exige `--address`**: `PlcTagComposition.Create` não tem overload de 2 args. Endereço
  livre sai do `free-memory`.
- **Lint de camada**: o xref traz os dois sentidos no mesmo saco — sem filtrar
  `Location.ReferenceType == "Uses"` dava 21 falsos positivos. Régua: `CPU1.0 CCO` passa limpo.
- **G120 do molde** (thread pausada): `add-device --mlfb "OrderNumber:6SL3244-0BB12-1FA0/4.7.13"`
  monta a estação certa (`System:Device.G120-2` + rack + head + `PROFINET interface`).
  Compile do `PLC_ZERO` fica em **4 erros, só o telegrama**.

### Tentado e descartado (não repetir)
- **Telegrama do G120 não é atributo**: `list-attrs` na `PROFINET interface` (20 atributos) e no head
  (16) — nenhum de telegrama. Também **não aparece em `GetPlugLocations`** (só o slot 3, do power
  module), nem depois do `connect-subnet`. É submódulo de catálogo e o identificador **não está na
  ajuda nem em nenhum AML do repo** (Insular tem 30 G120, zero telegrama plugado). O do G120X GSD
  (`.../SM/IDS_TEL20`) não serve: lá a interface se chama `PN-IO`, a constante não bateria.
- `PM240-2` com `OrderNumber:6SL3210-1PE16-1ALx` → `canPlug: false` (falta sufixo de versão).
- Manifesto de `scaffold` resolve `Source` **relativo ao arquivo do manifesto** — manifesto em
  scratchpad precisa de `Source` absoluto.
- Anteriores: master copy não leva UDT/tabela de tag · `import-source` exige UTF-8 com BOM ·
  `list-blocks --folder ""` devolve array cru · PowerShell `@(...) | Where` com 1 item vira escalar ·
  `move-block` in-place deixa cicatriz no vínculo chamada↔iDB.

## Next steps (ordered)
1. **Escolher o próximo verbo do CLI** — pergunta feita ao user, sem resposta ainda:
   `rename-block` (export → reescreve nome no XML → delete → import; coreografia do `move-block`) ·
   `set-tag` (mudar tipo/endereço/comentário de tag existente) · `edit-db-member`
   (`DbMember.Edit` já existe no Core, falta só o `case` no `Program.cs`) · item 9 *online*
   (go-online/download/compare/start-stop — revoga D8, **só com aval explícito do user**).
2. `Cpu` no manifesto + validação de família (evidência no PLANO: molde exige 1500 —
   `not supported for this instruction by the CPU used`).
3. Otimizar `raio-x.ps1`.
4. Telegrama do G120 — parado no clique do user na GUI (ver Open).

## Key files
- `src/Tia.Core/Hardware.cs` — `PlugModule`+`CollectSlots`, `ListAttrs`, `SetAttr`, `DeleteDevice`.
- `src/Tia.Core/Ops.cs` — `AddTag`/`DeleteTag` (perto de `ImportTagTable`).
- `src/Tia.Core/Scaffold.cs` — `Run` (try/catch do `--force`), `AlreadyInAnotherFolder`, `DeleteObject`.
- `src/Tia.Core/Audit.cs` — `LayerLeaks`; `Inventory.AllSources` virou `internal` por causa dele.
- `docs/VERBS.md` — gerado pelo `rebuild.ps1`; 1 leitura em vez de grep no `Program.cs`.
- `docs/PLANO.md` → "Hardware do molde: o G120", "Lint de camada", item 11 do backlog (`--force`).

## Open / blockers
- **Telegrama do G120**: precisa do clique do user — `Project1` → device view do `SINAMICS G_ZERO`
  → arrastar `Standard telegram 20, PZD-2/6` do catálogo pro `PROFINET interface`. Aí `list-devices`
  revela o `TypeIdentifier` e o `plug-module` automatiza. Sem isso o `install-lib` fecha em 4 erros.
- **`rebuild.ps1` derruba a 1ª chamada seguinte**: hash novo do `tia.exe` → o Portal já aberto mostra
  `Openness access (0033:000666)` e a chamada morre com `EngineeringSecurityException: The operation
  has timed out`. **Repetir o comando resolve.** Achar o diálogo: `EnumWindows` pelo título.
- User abriu o Insular dizendo "projeto base que será copiadas as fcs" e **nunca disse quais FCs**.
- Nunca `git add -A` (trilha paralela do `scripts/tia-help.py` no mesmo repo).
- `--out-file` em `$env:TEMP` dá caminho 8.3 (`CARLOS~1`) que o Python não abre — usar `workspace/`.

## Effort
**Baixo** para o passo 1 — os candidatos são coreografia já conhecida (`rename-block` é o
`move-block` com um replace no XML; `edit-db-member` é só o `case` no `Program.cs`). O piso é ler o
fluxo inteiro do verbo que serve de molde antes de copiar. Gargalo real não é raciocínio: cada
`rebuild.ps1` custa ~1 min e derruba a chamada seguinte, cada attach 3-7 s — juntar smoke em
`run --script` vale mais que pensar mais. Sobe pra **médio** se a escolha for o item 9 (*online*:
escrita no CLP, blast radius real) ou se `rename-block` mostrar cicatriz de vínculo nos chamadores.
