# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Fechar o ciclo da biblioteca ponta-a-ponta: `.al21` completa (com moldes, UDT e tabelas) instalada
numa CPU virgem até compilar 0 erros, incluindo o G120 e seu telegrama.

## State
- HEAD: `db5722e` — em sincronia com `origin/main`.
- Live state: **TIA Portal aberto na sessão 1, projeto `Base_tia_cli` salvo**
  (`save-project` rodado). `PLC_TESTE` foi apagado e recriado virgem
  (`6ES7 515-2AN03-0AB0/V3.1`) e está com os 5 pacotes de biblioteca instalados: **35 blocos,
  nenhuma pasta `_1`, compile Success**. Shell do agente na **sessão 0** (rota da task
  `TiaSmokeRun`).
  **Há um diálogo modal `Openness access` pendente na tela** — o último `rebuild.ps1` mudou o hash
  do `tia.exe` e o Portal já aberto pede autorização de novo. Enquanto ninguém clicar, todo verbo
  pendura com CPU ~0 e morre no timeout (foi assim que o dry-run do `bake-lib` gastou 1800s).
  **Já clicado** — o Portal voltou a responder (`doctor` ok).
- Done nesta sessão: `insert-telegram --change` (ramo que escreve, fechado e smoke verde);
  validação do fix do `--force` do `install-lib` contra o Portal; `add-master-copy` aceitando UDT e
  tabela de tag + `bake-lib` assando os dois.
- In progress: nada mid-flight. O `bake-lib` novo já rodou em **dry** contra o Portal e lista
  `UDT/tabelas do packages.json: Diag_Hardware, Genericos, MOTOR_AREA_01 (MOTOR_01)` — o `$extras`
  funciona. O `-Apply` (que é quem chama `add-master-copy` de UDT/tabela de verdade) **ainda não**.

## Decisions (and why)
- **Telegrama Main não se insere, se troca.** Um G120-2 recém-criado já vem com `MainTelegram #1`,
  então o ramo puro de `InsertTelegram` para Main é inalcançável no uso real. `EraseTelegram`
  também não serve: o Portal recusa com `Main telegram can not be deleted.` A troca é in-place,
  `telegram.TelegramNumber = N`, com `CanChangeTelegram(N)` de gate. Daí o `--change` explícito.
- **`--apply` respeita `canInsert: false`** (`status: cannot insert`) em vez de deixar o
  `InsertTelegram` estourar com `attribute Telegram (750) is not supported on this DriveObject` —
  o dry-run já sabia a resposta.
- **Fix do `--force` validado**: 1ª instalação em CPU virgem = 35 blocos sem `_1`; 2ª por cima =
  `pulados: 10`; `-Update` em `1.5 Diagnóstico` = ainda 35, sem `_1`. A 3ª é a que prova — o skip
  da 2ª acontece *antes* do `--force`, só o `-Update` exercita o `deleted+created`.
- **0 erros, não os 4 do G120**: a `.al21` atual foi assada com `-Prune` e só tem os 5 pacotes de
  biblioteca. Quem referencia `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`
  são os moldes, declarados no `packages.json` mas ausentes da `.al21`.
- **`add-master-copy --name` só resolvia `PlcBlock`.** `PlcType` e `PlcTagTable` também são
  `IMasterCopySource`; sem eles na `.al21` o `install-lib` depende de XML solto em `library/`
  (payload gitignored) e clone limpo não instala molde nenhum.

## Next steps (ordered)
1. **Achar as pastas-fonte dos moldes.** `bake-lib` só varre `$Root = '1. FB Bibliotecas'`, então
   os moldes do `packages.json` (`0 Moldes`, `Motor 1 (MOTOR_01)`, `3.1.0 Modelo`,
   `3.5 Barramento de Módulos`) **não entram no bake** — é isso que falta pra régua dos 4 erros.
   Medido no `CPU1.0 CCO` do `Base_tia_cli` (`list-blocks --folder X --count`):
   `3. Alarmes/Eventos/Falhas/3.1 Alarmes Words/3.1.0 Modelo` = 3 blocos e
   `3. Alarmes/Eventos/Falhas/3.5 Barramento de Módulos` = 7, mas `0 Moldes` e
   `4. Motores/Bombas/4.1 Inversores_CCM_01/4.1.1 AREA_01/Motor 1 (MOTOR_01)` = **0** — caminho
   errado ou esse PLC não tem os moldes. Localizar com `tia tree --out-file` + grep (nunca leitura
   direta) ou abrir o projeto de referência `proj/Software de ETE Insular_Inicial_V21` com
   `use-project.ps1`. A fonte de cada molde é `<target>/<nome>` do `packages.json` (target vazio =
   raiz), mas isso é hipótese, não medida.
2. Estender o `bake-lib` para assar essas pastas (`add-master-copy --folder <path>`), somando os
   nomes ao `$want` do `-Prune` igual ao `$extras`.
3. Reassar completo (**sem `-Prune`**, para os moldes entrarem) com `-Apply`.
4. `install-lib.ps1 -Plc PLC_TESTE <pacotes + moldes> -Apply` → esperar a régua: **4 erros**, todos
   a constante de hardware do G120 ausente.
5. `add-device --mlfb "OrderNumber:6SL3244-0BB12-1FA0/4.7.13" --name INVERSOR_MOTOR_01_CCM_01
   --apply` + `insert-telegram --device INVERSOR_MOTOR_01_CCM_01 --number 20 --change --apply` →
   `compile --apply` → **0 erros** fecha o ciclo.

## Key files
- `scripts/bake-lib.ps1` — o `$extras` novo (UDT/tabelas do `packages.json`) é o que o passo 2 testa.
- `src/Tia.Core/Library.cs` `AddMasterCopy` — resolve bloco → UDT → tabela.
- `src/Tia.Core/Drives.cs` — `insert-telegram`/`list-telegrams`; `Try()` segura atributo que estoura.
- `library/packages.json` — declara `types`/`tags`/`db`/`instances` por molde.
- `docs/PLANO.md` §§ "Telegrama do G120" e o bloco do `install-lib` — o que já foi medido.
- `scripts/__navi__.md` e `src/__navi__.md` — mapas das duas pastas que os passos tocam.

## Open / blockers
- **Onde estão os moldes** (passo 1). Se o `CPU1.0 CCO` do `Base_tia_cli` não os tiver, trocar pro
  projeto de referência (open leva 2-4 min → background).
- Diálogo modal `Openness access` volta a cada `rebuild.ps1` com o Portal aberto: chamada pendurada
  com CPU ~0 = alguém precisa clicar, não é bug de API.

## Skills
- tia
- ponytail
- caveman

## Effort
**Médio** para o passo 2-3: a sequência é documentada, mas o `bake-lib` novo nunca rodou contra o
Portal e `add-master-copy` de tabela de tag pode falhar no `IMasterCopySource` (aí o erro é
`'X' is not a master copy source`, e a saída é exportar XML mesmo). Subir pra **alto** só se o
`MasterCopies.Create` aceitar o objeto e o `install-lib` ainda assim não achar a tabela no alvo.
O gargalo não é raciocínio: cada attach do Openness e o `install-lib` dominam o relógio, e o
diálogo modal bloqueia tudo até alguém clicar.
