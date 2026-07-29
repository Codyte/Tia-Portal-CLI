# Handoff · TIA Portal Openness API · 2026-07-29

## Goal
Fechar o ciclo da biblioteca **contra o Portal**, não em dry: `bake-lib` (PLC → `.al21`) →
`install-lib`/`new-plc` (`.al21` → CPU virgem) → medir contra a régua conhecida da F8
(**4 erros, todos o G120 ausente**). É a única espinha da ferramenta sem medição real.

## State
- HEAD: `048c118`. Working tree limpo (só o archive novo em `.handoff/`).
- **Live state**: 1 TIA Portal aberto, projeto **`Base_tia_cli`** (o user salvou o
  `Software de ETE Insular_Inicial_V21` com esse nome — é o projeto base). 1 PLC `CPU1.0 CCO`,
  476 blocos, **compila 0 erros**, salvo. O user já apagou os devices duplicados (1 exemplar cada).
- **A `.al21` está VAZIA** — `src/Tia.Lib/tia-cli/tia-cli.al21`, o user deletou todo o conteúdo à
  mão. Não há trabalho manual a preservar; `bake-lib -Apply` pode sobrescrever à vontade. O aviso
  de "fazer backup antes de assar" do handoff anterior **caducou**.
- Done nesta sessão: `1. FB Bibliotecas` reorganizada e commitada (`refactor(lib)`, 9 arquivos).
- In progress: nada mid-flight.

## Decisions (and why)
- **`move-block` em bloco chamado NÃO deixa cicatriz** — medido, não deduzido: movi
  `FB STATUS ECSX` (5 refs) e o compile deu **0 erros**. Contraria a nota do `PLANO.md:478`
  (`Block call was invalid because interface was changed`). O que morde de verdade é a
  **inconsistência do vizinho durante o batch**: 4 de 41 steps falharam com
  `Block 'X' is inconsistent (imported or edited, never compiled)` e passaram com
  `compile --apply` no meio. Padrão: rodar o batch → compilar → re-rodar (move é idempotente,
  `alreadyThere`); precisou de 2 rodadas porque mover o iDB reinconsistencia o FB dono.
  **Isso libera reorganizar em projeto povoado, não só em CPU virgem.**
- **Layout final da `1. FB Bibliotecas`** (typo corrigido no projeto e em 9 arquivos do repo,
  inclusive os `-Root` de `install-lib.ps1` e `bake-lib.ps1`):
  raiz 5 (**camada base**: BITS TO WORD, BITS TO DOUBLE WORD, CONTADOR, TOTALIZADOR, HORÍMETRO —
  a `1.7 Utilitários` dissolvida) · `1.1 Acionamento` 3 · `1.1 Acionamento/1.1.1 Inversores` 7 ·
  `1.3 Instrumentação` 6 · `1.4 Alarmes e Falhas` 3 (ganhou `FB INTERTRAVAMENTO_PAINEL`) ·
  `1.5 Diagnóstico` 6 · `1.6 Comunicação Modbus` 4. Total 34. `1.2` vazio de propósito
  (Inversores virou `1.1.1`). `generic.json`/`packages.json` batem — nenhum `requires` quebrado.
- **Terceiro nível (`x.x.x`) só quando o pacote passar de ~10 blocos E o subgrupo nunca for
  instalado sozinho.** `x.x.x` não renomeia pacote (o `requires` aponta pro nível 1), então é
  barato — mas os clusters de hoje têm 2-3 blocos cada, e pasta pra 3 blocos é ruído. Candidatos
  se a biblioteca crescer: `1.5 Diagnóstico` (Profinet × Módulos), `1.3 Instrumentação`
  (medida × setpoint).
- **A poda NÃO é pré-requisito do bake** — eu tinha assumido que sim; é falso. O bake só leva o
  que é nomeado. Dá pra validar o ciclo inteiro com o `Base_tia_cli` cheio (476 blocos) e podar
  depois, sem pressa e sem risco.

### Tentado e descartado (não repetir)
- **Renomear `FB_LIGA/DESLIGA MODO AUTO` (tem `/`) e `FB FILTRO DE AMOSTRAGEM  ANALÍTICA`
  (espaço duplo)**: os dois nomes estão gravados nos XML de `library/blocks/`, em `generic.json`,
  `packages.json`, `library.json`, `export-all.json` e nos moldes — 9 arquivos desincronizam, e o
  nome precisa sobreviver ao `import-master-copy --force` da atualização futura. São bombas
  latentes, não bugs ativos. **Deixar como está.**
- **Renumerar pra fechar o `1.2`, renomear `1.3`→"Instrumentação e Controle" (por causa do
  `AUX_PID`), separar `SINA_SPEED_TLG20` numa pasta Siemens**: cada um renomeia um **nome de
  pacote** já gravado em `packages.json`/`generic.json`/`.al21`. Custo > estética.
- **`rebuild.ps1` não foi rodado**: a mudança em `Audit.cs`/`Inventory.cs` é só comentário
  (zero delta de comportamento) e custaria um UAC pra rewhitelist. Rodar só se mexer em código.
- Anteriores: bake manual pela GUI não substitui `bake-lib` · master copy de *pasta de blocos*
  não leva UDT nem tabela (mas UDT solto **é** master copy) · `import-source` exige UTF-8 com BOM ·
  `--out-file`/`--plc` do processo não descem pros steps do `run` · telegrama do G120 não é
  atributo nem plug location · preflight de UDT no `install-lib` era falso alarme (todo o `$ops`
  é montado antes do único `run`).

## Next steps (ordered)
1. **`pwsh scripts/bake-lib.ps1 -Plc "CPU1.0 CCO" -Prune`** (dry) → conferir a lista → repetir com
   `-Apply`. Sai a `.al21` com os nomes novos: 5 base soltos + 6 pacotes `1.x`. A `.al21` está
   vazia, então `-Prune` não tem órfão a acusar.
2. **Acrescentar UDTs e tabelas de tag como master copy** (`add-master-copy --name`) — o import já
   sabe roteá-los por `ContentType` (`Types.CreateFrom`/`TagTables.CreateFrom`). Isso mata a
   dependência de `library/blocks/<UDT>.xml` (payload gitignored) que impede um clone limpo.
3. **`pwsh scripts/new-plc.ps1 PLC_TESTE "<pacotes>" -Apply`** numa CPU virgem → comparar com a
   régua da F8: **4 erros, todos
   `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`**. Igual = ciclo validado
   (fecha `import-master-copy` real e `--force --apply` real, os dois buracos que a F8 registra).
   Diferente = o bug apareceu agora, não daqui a três fases.
4. Só então a poda do `Base_tia_cli` (442 blocos fora da biblioteca) — inventário já gerado em
   `workspace/base-inventory.json` (93 pastas com contagem) + keep/delete conferido com o user.
   **Irreversível**, e "o que é arsenal" ainda não está escrito em lugar nenhum.
5. Atualizar a tabela de fases do PLANO (F8: fechar os 2 buracos) e o `packages.json` se o bake
   mudar algum nome.

## Key files
- `workspace/base-inventory.json` — 93 pastas × contagem do `Base_tia_cli` (a base da poda).
- `workspace/lib-deps.md` — xref das 34 da biblioteca: 5 arestas internas, nenhum bloco órfão.
- `workspace/lib-reorg.json` / `lib-reorg-dry.json` — o batch de 41 ops que reorganizou; modelo
  reusável (`create-folder` × 7 + `move-block --name` × 34).
- `scripts/bake-lib.ps1` (`-Prune`) · `scripts/install-lib.ps1` (`-Update`, `-Root` já corrigido) ·
  `scripts/new-plc.ps1` · `scripts/_common.ps1` (`Resolve-LibFile` acha a `.al21` por glob).
- `library/packages.json` — `requires`/`db`/`tags`/`types`/`instances` por master copy.
- `src/Tia.Core/Library.cs` — `ImportMasterCopy` (roteamento por ContentType + `--force`),
  `FindMasterCopy`/`Collect` (recusa nome ambíguo entre níveis).
- `docs/PLANO.md:71` (F8, os 2 buracos) · `:259-556` (biblioteca) · `:484` (a régua dos 4 erros).

## Open / blockers
- **A biblioteca vai ser atualizada a partir de outro projeto** (ordem do user). Por isso nome de
  pacote tem que ficar estável — é o que ancora o `import-master-copy --force`.
- **Quais dos 33 FBs são autorais** e podem virar `.scl`/`.xml` versionado? Decide se o arsenal
  viaja no Git ou só na `.al21` local. `SINA_SPEED_TLG20` (FB38003) é da Siemens, não autoral.
- Telegrama do G120 segue parado no clique do user (device view do `SINAMICS G_ZERO`) — são os 4
  erros residuais da régua. Item 9 (*online*) segue parado no aval (revoga D8).
- Nunca `git add -A` (trilha paralela do `scripts/tia-help.py` no mesmo repo).
- `rebuild.ps1` pode derrubar a 1ª chamada seguinte (`Openness access (0033:000666)` no Portal
  aberto — só o clique resolve). Nesta sessão não bateu.
- `--out-file` em `$env:TEMP` dá caminho 8.3 (`CARLOS~1`) que o Python não abre — usar `workspace/`.

## Effort
**Baixo** para o passo 1, com Opus 5. `bake-lib` é sequência documentada, o layout está decidido e
commitado, a `.al21` está vazia (nada a preservar) e o dry mostra tudo antes de escrever. O gargalo
não é raciocínio: é attach (~3s), compile de projeto real (minutos) e o `-Apply` — juntar operação
em `run --script` rende mais que pensar mais. **Sobe pra alto no passo 3** se a contagem da CPU
virgem divergir dos 4 erros da régua: aí a `.al21` gerada difere do que o `install-lib` espera ler,
e o layout ("base" = bloco solto cuja pasta é exatamente `-Root`) volta pra mesa.
