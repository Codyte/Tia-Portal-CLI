# DIÁRIO — sagas fechadas do TIA Portal Openness CLI

Histórico datado extraído de [`PLANO.md`](PLANO.md) em 2026-08-10. Nada foi resumido nem
apagado: cada seção está aqui na íntegra, na ordem original. O PLANO ficou só com o que
ainda decide alguma coisa (decisões, arquitetura, fases, backlog, pendências).

## Índice

- [Otimização de tokens do CLI — ✅ 2026-07-28](#otimização-de-tokens-do-cli-2026-07-28)
- [Biblioteca de blocos ("arsenal") — ✅ ciclo fechado 2026-08-07 (`library/`)](#biblioteca-de-blocos-arsenal-ciclo-fechado-2026-08-07-library)
- [Master copy de pasta = pacote — ✅ medido 2026-07-28 (0 erros em CPU virgem)](#master-copy-de-pasta-pacote-medido-2026-07-28-0-erros-em-cpu-virgem)
- [`DB GLOBAL` composto + tags genéricas + iDBs — ✅ 2026-07-28: 87 erros → 2](#db-global-composto-tags-genéricas-idbs-2026-07-28-87-erros-2)
- [Instalação em 1 comando — ✅ 2026-07-28: CPU virgem → 4 erros (só hardware)](#instalação-em-1-comando-2026-07-28-cpu-virgem-4-erros-só-hardware)
- [Bake real da `.al21` + bug do `--force` — ✅ 2026-07-29, fix validado 2026-08-07](#bake-real-da-al21-bug-do---force-2026-07-29-fix-validado-2026-08-07)
- [Ciclo completo da biblioteca fechado (2026-08-07)](#ciclo-completo-da-biblioteca-fechado-2026-08-07)
- [Hardware do molde: o G120 (2026-07-28)](#hardware-do-molde-o-g120-2026-07-28)
- [Telegrama do G120: nunca foi `plug-module` — ✅ 2026-08-07](#telegrama-do-g120-nunca-foi-plug-module-2026-08-07)
- [Lint de camada no `audit` — ✅ 2026-07-28](#lint-de-camada-no-audit-2026-07-28)
- [Bugs abertos (smoke 2026-07-27)](#bugs-abertos-smoke-2026-07-27)
- [Clonar acionamento — fluxo real validado (2026-07-27, AsBuilt)](#clonar-acionamento-fluxo-real-validado-2026-07-27-asbuilt)
- [Biblioteca em um comando: extras da `.al21` + hardware declarado (2026-08-07)](#biblioteca-em-um-comando-extras-da-al21-hardware-declarado-2026-08-07)
- [F8 fechada: `replicate-instruments --apply` real (2026-08-07)](#f8-fechada-replicate-instruments---apply-real-2026-08-07)
- [`delete-db-member` — o contrário que faltava (2026-08-07)](#delete-db-member-o-contrário-que-faltava-2026-08-07)
- [Migração do repo para skill (2026-08-06)](#migração-do-repo-para-skill-2026-08-06)
- [Gate de máquina limpa exercitado (2026-08-07)](#gate-de-máquina-limpa-exercitado-2026-08-07)
- [Rodadas executadas](#rodadas-executadas)
- [F6 — Endurecer os scripts PS (✅ executada 2026-07-27)](#f6-endurecer-os-scripts-ps-executada-2026-07-27)
- [Plano original (referência)](#plano-original-referência)
- [F6.1 — Bugs pontuais (independentes, fazer primeiro)](#f61-bugs-pontuais-independentes-fazer-primeiro)
- [F6.2 — `scripts/_common.ps1` + `Invoke-Tia` (o núcleo)](#f62-scripts_commonps1-invoke-tia-o-núcleo)
- [F6.3 — Migrar os macros](#f63-migrar-os-macros)
- [F6.4 — Robustez menor](#f64-robustez-menor)
- [F6.5 — CLI (opcional, C#)](#f65-cli-opcional-c)
- [Verificação](#verificação)
- [Ordem](#ordem)

## Otimização de tokens do CLI — ✅ 2026-07-28

Levantada pelo custo real da reorganização da `1. FB Bibliotecas` (6 chamadas de ferramenta e um
gerador de batch em PowerShell pro que devia ser uma linha). Seis pontos, todos fechados:

| ponto | antes | agora |
|---|---|---|
| `list-blocks` sem filtro | dump de ~480 blocos | `--folder A/B` (inclui subpastas) · `--type FB\|FC\|OB\|GlobalDB\|InstanceDB` · `--count` (total por pasta, ~10 linhas) |
| não existia move | `export`+`delete`+`import` por bloco, na ordem certa | `move-block --name X \| --pattern P* --folder A/B [--apply]` ([Ops.cs:290](../src/Tia.Core/Ops.cs#L290)) |
| regra do nome de arquivo (`/` → `_`) reimplementada fora | PowerShell replicando `ExportPath` | interno ao `move-block` |
| acento virava `?` na rota da task | round-trip por arquivo pra qualquer saída com acento | `[Console]::OutputEncoding` UTF-8 em `taskrun.ps1` e `_common.ps1` |
| assinatura de verbo | ~5 greps em `Program.cs` por sessão | `docs/VERBS.md`, gerado do help por `scripts/gen-verbs.ps1` dentro do `rebuild.ps1` |
| `run --script` | resultado completo de cada step (98 steps = dump) | `--summary` → `{steps,failed,errors[]}` |

Junto: `create-folder`/`delete-folder --types` (pasta de UDT era o único dos três tipos de pasta
sem verbo) e `delete-type`. Smoke no projeto de referência: `move-block --apply` + `compile` +
`create/delete-folder --types` + `list-blocks --count` num batch, `{steps:6, failed:0}`.

**Regra do `move-block`, que o verbo agora encapsula**: exporta **todos** os alvos antes de apagar
o primeiro. O `delete` deixa quem referencia inconsistente, e bloco inconsistente não exporta.

## Biblioteca de blocos ("arsenal") — ✅ ciclo fechado 2026-08-07 (`library/`)

Problema que resolve: os 4 geradores só rodam se o projeto do cliente **já tiver** os moldes e a
lei de pastas (`doctor` checa `FC_Modelo`, `OB_MOLDE_ALARMES`, `DB GLOBAL`, `2. Alarmes`,
`3. Partidas`, UDTs `MotorDados`/`ValvDados`). Sem isso, `doctor` vermelho e acabou. Com biblioteca
instalável, vira um comando.

**Empacotamento decidido**: `.scl` como padrão, `.xml` só pro que precisa nascer em LAD.
- `.scl` via `import-source` — texto diffável, **linguagem SCL inteira** (compilador da Siemens),
  gera FC/FB/OB/DB/UDT ([Ops.cs:311](../src/Tia.Core/Ops.cs#L311) faz `GenerateBlocksFromSource` e
  apaga a fonte). Imune à versão do Engineering. Limitação: bloco nasce na raiz (verbo não tem
  `--folder`); contorno com verbos já validados = `export-block` → `import-block --folder` →
  `delete-block`.
- `.xml` via `import-block --folder` — escolhe pasta, preserva LAD e comentários multilíngues.
  Custo: `<Number>` colide (foi preciso reescrever no teste de 2026-07-28) e o `<Engineering
  version="V21">` prende à versão.
- `.al19` via `import-master-copy` — **descartado**: binário, não diffa, só se produz na mão.
- `import-ladder` (subset nosso) **não serve** pra escrever a biblioteca: sem timer nem aritmética.

**Instalação**: `tia scaffold --manifest library/library.json --apply` — sem verbo novo,
`scaffold` já é "árvore de pastas + moldes num projeto". A ordem de import por tipo já está certa
(UDT → tabela → FB → DB → iDB → FC → OB, [`Scaffold.Rank`](../src/Tia.Core/Scaffold.cs#L58)) —
a anotação anterior de "falta ordenar UDT antes de DB/FC" estava obsoleta, `Rank` sempre teve
`SW.Types` = 0.

**Fatia 1 ✅ 2026-07-28 (offline, sem Portal)** — `library/` na raiz:
- `library/library.json` (versionado) = o antigo `docs/examples/scaffold-padrao.json` com
  `Source: "blocks"` (relativo ao manifesto, então manifesto + payload viajam juntos pra
  qualquer pasta). 20 pastas de bloco, 6 de tag, 66 itens.
- `library/blocks/` (gitignored) = o antigo `workspace/padrao/`, 66 XMLs / 3,3 MB.
- `library/export-all.json` (versionado) = batch inverso, gerado do manifesto: 66 exports com o
  verbo certo por tipo, `--out library/blocks`, 1 attach. Substituiu `scripts/export-fixtures.ps1`
  (cobria 15 dos 66) e o `workspace/export-padrao.json` (gitignored, caminho absoluto da máquina).
- `library/README.md` (versionado) = por que o payload não viaja, inventário dos 66 por pasta,
  o que cada gerador exige, como repor, como instalar, limitação do `Folder` de UDT, e as duas
  pegadinhas do export (compile antes; `ExportPath` troca `/` do nome por `_` no arquivo, caso
  `FB_LIGA/DESLIGA MODO AUTO`).
- **Testado contra o Portal ✅ 2026-07-28** (projeto `Software de ETE Insular_Inicial_V21`):
  `scaffold --manifest library/library.json` dry = 26 pastas `none (exists)` + **66/66
  `skip (exists)`**, zero item não encontrado (manifesto casa 1:1 com o payload e com o projeto);
  `run --script library/export-all.json` = **66/66 `ok`** num attach. Duas rodadas de export
  seguidas dão 66 arquivos byte-idênticos exceto `<DocumentInfo><Created>` (timestamp) — hash muda
  sempre, conteúdo não; anotado no README pra ninguém caçar diff fantasma.

**Gap do `scaffold` — corrigido ✅ 2026-07-28**: item UDT ignorava `Folder` (todo `SW.Types.*` caía
na raiz do `TypeGroup`). Agora passa por [`ResolveTypePath`](../src/Tia.Core/Scaffold.cs#L188),
análogo a `ResolveBlockPath`/`ResolveTagPath` — cria a subpasta de tipo se faltar. Validado no
Portal: manifesto com `"Folder": ["ClaudeTest","Tipos"]` e `--apply` → `find --kind type` mostra o
UDT em `ClaudeTest/Tipos`. `rebuild.ps1` ALL PASS.
Junto veio **`delete-type --name X [--apply]`** ([Ops.cs:205](../src/Tia.Core/Ops.cs#L205)) — não
existia jeito de tirar UDT pela CLI, e teste de biblioteca cria UDT descartável. Os 4 UDTs de teste
(`*_T`, `MotorDados_LIB`) foram apagados com ele, compile 0 erros. Sobra: `delete-folder` não
apaga pasta de **tipo** (só bloco e tag) — `ClaudeTest/Tipos` ficou vazia no projeto de teste.

**Fatia 2 — parte SCL ✅ 2026-07-28** (`library/core/`, autoral e versionado, com README próprio):
`MotorDados.scl`, `ValvDados.scl`, `MotorPrincipal.scl` (composto de dois `MotorDados`, não campos
`CMD_&_*` duplicados), `DB GLOBAL.scl` (esqueleto: `AREA_01.ALARMES.WORD_ALARMES_1..8`,
`HARDWARE_INTERRUPT.ALARMES_MODULOS.QA-00/QA-01.WORD_1..2`) e `FB BITS TO WORD.scl` (slice access
`#BITS_TO_WORD.%X0..15`, pinos `SIGNAL_Bit0..15`). Importados na ordem UDT → DB/FB no projeto de
referência com nomes sufixados `_T` (pra não sobrescrever os homônimos do cliente) — **compile
0 erros / 0 warnings**. Os 2 blocos de teste foram apagados; os 3 UDTs `*_T` ficaram (não existe
verbo `delete-type`). Falta: os 4 moldes em LAD e assar `.scl` → `.xml` num projeto vazio pra
instalar via `scaffold` (`Scaffold.Plan` lê o tipo do XML, [:84](../src/Tia.Core/Scaffold.cs#L84),
e `import-source` não tem `--folder`).

**Instalação num PLC virgem, medida 2026-07-28** (`Project1`, S7-1515 adicionado por `add-device`
`6ES7 515-2AM02-0AB0/V2.9`): os 4 moldes **já existem** em `docs/examples/` (`ModuleErrorMolde.xml`,
`FcModeloAlarmes.xml`, `ObMoldeAlarmes.xml`, `InstrumentTemplateFc.xml`) — não falta desenhá-los,
falta a camada de dependência. Num S7-1200 o import morre em `The property 'DisableENO' is not
supported for this instruction by the CPU used`: **molde é dependente da família da CPU**, exige
1500. Orçamento de erro no 1500: só os 4 moldes = 65 erros → + biblioteca (51 blocos, `library.json`,
árvore de pastas) = 33 → + `set-memory-bytes` = **25**, todos de tag de projeto e iDB de molde.
~~`scaffold --force` não resolve colisão (não apaga antes; falha com *"already exists in this CPU"*)~~
✅ **corrigido 2026-07-28**: `ImportOptions.Override` só sobrescreve **na mesma pasta**, e nome de
bloco é único no PLC — o mesmo nome noutra pasta faz o import recusar. `--force` agora tenta o
import e, só se a exceção for *"already exists"*, apaga o objeto antigo e importa no lugar pedido
(`action: "deleted+imported"`). O caminho comum (mesma pasta) continua no `Override`, que preserva
o vínculo chamada↔iDB. Medido no `PLC_ZERO`: `move-block` de `FB BITS TO WORD` pra `ZZ Force` →
`scaffold --force --apply` → bloco de volta em `1. FB Bibliotecas`, `ZZ Force` vazia, compile nos
mesmos 4 erros (sem cicatriz).

**Núcleo genérico (fatia 2, autoral e publicável — desenho original)**. Os 66 itens de
hoje são exports do cliente e nunca vão pro Git; o que fecha `doctor` verde num projeto qualquer
são ~10 itens, escritos do zero:

| item | tipo | formato | por que |
|---|---|---|---|
| `MODULE_ERROR_MOLDE` | OB molde de erro de módulo | `.xml` | LAD; template de `gen-fault-ob` ([FaultOb.cs:19](../src/Tia.Core/FaultOb.cs#L19)) |
| `FC_Modelo` | FC modelo de alarmes | `.xml` | LAD; template de `gen-alarm-fc` ([AlarmFc.cs:20](../src/Tia.Core/AlarmFc.cs#L20)) |
| `OB_MOLDE_ALARMES` | OB molde de chamada | `.xml` | LAD; `AlarmFc.ObTemplate` |
| `MOLDE_ANALOGS` | molde de instrumento | `.xml` | LAD; template de `gen-instrument-fc` |
| `FB BITS TO WORD` | FB 16 bits → word | `.scl` | `AlarmFc.MasterFb`; lógica pura, sem LAD |
| `MotorDados` / `MotorPrincipal` / `ValvDados` | UDT | `.scl` (`TYPE`) | estrutura por equipamento que `replicate-fc` espera |
| `DB GLOBAL` **esqueleto** | GlobalDB | `.scl` (`DATA_BLOCK`) | `GlobalDb` dos 3 geradores; só a casca, **não** os 869 KB do cliente |
| árvore de pastas | — | manifesto | `2. Alarmes`, `3. Partidas`, `3.1 Alarmes Words`, `3.1.0 Modelo` são nome default nos configs |

`.scl` onde a lógica é aritmética/estrutura (diffável, imune à versão do Engineering, nasce na
raiz → contorno `export-block` → `import-block --folder` → `delete-block`); `.xml` só nos 4 moldes,
que precisam nascer em LAD legível porque o engenheiro edita e os geradores clonam rede a rede.

**Conteúdo, por valor**: (1) os pré-requisitos dos geradores — é onde está o retorno;
(2) utilitários genéricos (escala raw↔EU + clamp, debounce de falha, borda + selo com falha/reset,
horímetro, contador de partidas, bits→word e inverso, first-out, watchdog de comunicação, rampa de
setpoint); (3) diagnóstico (OB de erro de módulo já existe em `ModuleErrorMolde.xml`).

**Procedência** — resolvida pelo gate de publicação da F4: payload de cliente fica gitignored
(`library/blocks/`), o que for pro Git é autoral ou sanitizado com `clone --replace OLD=NEW`.
Vale também pros XMLs de `docs/examples/`: **sanitizados ✅ 2026-07-28** por substituição de texto
(não deu pra usar `clone --replace`, que exige o bloco no projeto) — `CASA_DE_SOPRADORES` → `AREA_01`,
`SOPRADORES_DESARENADOR` → `AREA_01_MOTORES`, `SOPRADOR_DESARENADOR_S-01A` → `MOTOR_S-01A`,
`PARTIDA_SOPRADOR_1` → `PARTIDA_MOTOR_1`, e as duas tags `..._STS_SOPRADOR_DESANERADOR_MODO_*`.
Tocou `BombaTemplateFc.xml`, `StdBombaA.xml` e as asserções de `Tia.Tests/Program.cs` que citavam
esses nomes; `rebuild.ps1` **ALL PASS**. Sobra proposital: `library/library.json` e
`library/export-all.json` citam `SOPRADOR_DESARENADOR (S-01A)` porque o nome tem que casar com o
objeto no projeto do cliente pra repor o payload — nome de objeto, não payload.

**Ainda por resolver antes de tornar o repo visível de fato**: nome de projeto de cliente em prosa
(`Insular`, `ETE SG`, `AsBuilt`) aparece em `docs/PLANO.md`, `docs/PADRAO.md`,
`docs/projeto-real-fase-A.md`, `library/README.md`, `scripts/raio-x.ps1`, `__navi__.md` e em todo
o histórico de `.handoff/` — sanitizar isso é reescrever histórico já commitado, decisão do user.

**Fatia 3** (utilitários genéricos: escala raw↔EU + clamp, debounce, bits→word e inverso,
first-out, watchdog, rampa de setpoint) só depois da fatia 2. Teste das fatias 2/3 = instalar em
`ClaudeTest/` e `compile` 0 erros, um `run --script`.

### Master copy de pasta = pacote — ✅ medido 2026-07-28 (0 erros em CPU virgem)

A `.al19/.al21` estava descartada como forma de *escrever* a biblioteca (binário, "só se produz na
mão"). **A parte "só na mão" está errada**: a ajuda oficial
(`Create master copy from a project in library`) lista `PlcBlockUserGroup` entre os
`IMasterCopySource`, então uma **pasta inteira vira um master copy só**, com subpastas. Verbo novo
`add-master-copy --file X.al21 (--name BLOCO | --folder A/B) [--lib-folder L] [--apply]`
([Library.cs](../src/Tia.Core/Library.cs)) — abre a global library em `ReadWrite`, cria e
`UserGlobalLibrary.Save()` (sem Save nada vai pra disco; `GlobalLibrary` não tem `Save`).
O Portal batiza o master copy de `"Copy of Function blocks in X"` — o verbo renomeia pro nome da
fonte. `import-master-copy` agora usa `Groups.CreateFrom` quando o `ContentType` é
`PlcBlockUserGroup`, e `Blocks.CreateFrom` no resto.

Isso **não** revoga "`.al21` é artefato, não fonte" (segue fora do git, `.gitignore`): fonte é
`.scl`/`.xml`, a library sai de um build. O que muda é o *instalador*: pacote inteiro em 1 chamada,
com hierarquia, sem os 63 imports do manifesto.

**Medição em CPU virgem** (`PLC_LIBT`, `6ES7 515-2AM02-0AB0/V2.9`, criada por `add-device`):
`import-master-copy "1.1 Acionamento"` = 12 blocos (7 + 4 na subpasta `1.1.1 Inversores`) →
**9 erros**, todos dependência de `FB CONTADOR`/`FB_HORÍMETRO` (nível 1). Somando os 5 utilitários
soltos de `1. FB Bibliotecas` → **2 erros**, ambos `Tag "Clock_1Hz" not defined`;
`set-memory-bytes --clock 0 --apply` → **compile Success, 0 erros**. Contra 82–88 erros da
instalação do manifesto inteiro. Confirma a lei de escopo: dependência de pacote só aponta pra
cima, e clock/system memory byte é parte do core (não é bloco).

**Critério de aceite batido — cada pacote sozinho numa CPU virgem = 0 erros.** `scripts/bake-lib.ps1`
grava a biblioteca do PLC na `.al21` (1 master copy por subpasta de `1. FB Bibliotecas` + 1 por bloco
solto do nível 1; `-Apply` pra valer, dry-run mostra o que faria). Instalação de um pacote = os 5
blocos de nível 1 + o pacote + `set-memory-bytes --clock 0`. Medido em 5 CPUs `6ES7 515-2AM02-0AB0`
recém-criadas: `1.1 Acionamento`, `1.3 Instrumentação`, `1.4 Alarmes e Falhas`,
`1.6 Comunicação Modbus` e `1.5 Diagnóstico` → **Success, 0 erros** em todas.

`1.5 Diagnóstico` deu 1 erro no primeiro teste (`FB DIAG MODULES: Missing instance DB`) — bug do
manifesto, não do desenho: `DIAG to STRING_DB.xml` existia em `library/blocks/` mas ficou fora dos
63 itens, e a chamada de `DIAG to STRING` dentro do FB é single-instance. Manifesto agora tem 64
itens e o pacote foi re-bakeado. Esse mesmo erro estava entre os 88 do `PLC_GEN` desde sempre —
o teste por pacote é que o isolou.

`scripts/install-lib.ps1 "<Pacote>[,<Pacote>]" -Plc X [-Apply]` faz a instalação inteira (base +
pacotes + clock byte + compile) e devolve o compile já resumido; sem pacote, lista o que a library
oferece. "Já instalado" = existe bloco com esse nome ou a pasta do pacote existe — repetir é no-op
(medido: 6 pulados, `Success`), e 3 pacotes de uma vez numa CPU virgem também deram
`Success, 0 erros`.

`delete-master-copy --file X.al21 --name M [--apply]`: rebake deixa lixo quando o master copy muda
de nome (o primeiro probe ficou como `Copy of Function blocks in ...`).

**`--portal PROJETO|PID` (opção global).** `TiaSession.Attach` fazia
`TiaPortal.GetProcesses().FirstOrDefault()` — com dois portais abertos isso attacha num projeto ao
acaso, inclusive o do cliente. Agora: 1 instância → attacha; mais de uma sem `--portal` → falha
listando `PID:projeto`; `--portal` casa por substring (nome do projeto ou PID) e recusa ambíguo.
Vale pra `open-project`/`create-project` também.

### `DB GLOBAL` composto + tags genéricas + iDBs — ✅ 2026-07-28: 87 erros → 2

O que sobrava depois dos pacotes 1.x zerarem era **a planta**, não os blocos. Três peças, medidas
numa CPU virgem (`PLC_FULL`, manifesto inteiro via `scaffold`):

| passo | erros |
|---|---|
| `scaffold --manifest library/generic.json` | 81 |
| `+ DB GLOBAL` composto (`import-source`) | 18 |
| `+ library/tags/Genericos.xml` (`import-tags`) | 7 → 6 |
| `+ 4 `create-instance-db`` | **2** |

Os 2 finais são `Tag "INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20" not defined`
— exige o G120 no hardware do projeto **e ligado ao IO system do PLC** (ver "Ciclo completo da
biblioteca fechado"), não tem lado de software. Receita reproduzível em
[`docs/examples/install-full.json`](examples/install-full.json) (`tia run --script`).

**`DB GLOBAL` por fragmentos.** SCL não tem include e o DB é um arquivo só, então a composição é
textual: `scripts/compose-db.ps1 motores,instrumentacao,afericao` = cabeçalho + `00-core.scl`
(sempre) + os ramos pedidos + rodapé → `workspace/db-global.scl`. Fragmentos em
`library/db-global/`; o ramo é do *molde* que o consome (`motores` ← `PARTIDA_MOTOR_1`,
`instrumentacao`/`afericao` ← `MOLDE_ANALOGS`/`MOLDE TOT1`), e cada um é meia dúzia de linhas
porque o tipo já existe como UDT (`"MotorDados"`, `"SensorDados"`, `"Aferição CMD"`).

**`import-source` exige UTF-8 com BOM.** Sem BOM o acento chega corrompido, a referência a
`"Aferição CMD"` não resolve e o erro é só
`Error when calling method 'GenerateBlocksFromSource'` — nada sobre encoding. `compose-db.ps1`
grava com BOM por isso.

**`create-instance-db --name X --of FB [--folder A/B]`.** Molde exportado em XML chega sem os DBs de
instância das suas chamadas (o Portal os cria no editor; o export não os leva) → `Missing instance
DB`. O nome esperado está no próprio XML do molde, em `<Instance Scope="GlobalVariable"><Component
Name="...">`, já com o mapa `Replace` aplicado (`FQIT-01` → `INSTR_01`).

`library/tags/Genericos.xml` = 11 tags que os moldes usam e nenhuma tabela do manifesto trazia,
alocadas em `%M` a partir de 5520 (`free-memory` achou o buraco). Tipo errado aparece como
`The data type Real of the actual parameter does not match ... Bool` — foi assim que
`INSTR_01_TOTALIZACAO_MEDIDOR_VAZAO` virou `Bool`.

**Movida da árvore feita** (manifesto = fonte, `library/generic.json`): `1.7 Utilitários` dissolvida
(5 blocos soltos em `1. FB Bibliotecas`), `1.2 Inversores` → `1. FB Bibliotecas/1.1 Acionamento/
1.1.1 Inversores`. `move-block` refletiu no `PLC_GEN` (9 blocos).
**Cicatriz do move in-place**: mover bloco *chamado* deixa +6 erros nos chamadores
(`Block call was invalid because interface was changed`, `The block call or the associated instance
data block could not be updated`) que dois `compile --apply` não limpam — é o vínculo
chamada↔instance DB, que o `delete`+`import` do `move-block` quebra. Em CPU virgem o problema não
existe; num PLC já instalado, reimportar o chamador.

### Instalação em 1 comando — ✅ 2026-07-28: CPU virgem → 4 erros (só hardware)

`install-lib.ps1` deixou de instalar só os pacotes `1.x`: agora `library/packages.json` diz, por
master copy, **o que o import sozinho não traz** — `requires` (dependência entre pacotes), `db`
(ramos do `DB GLOBAL`), `tags` (tabelas) e `instances` (iDBs de molde). Medido numa CPU criada na
hora (`PLC_ZERO`, `add-device` + o comando abaixo, nada mais):

```
pwsh scripts/install-lib.ps1 -Plc PLC_ZERO -Portal Project1 -Apply `
  "0 Moldes,Motor 1 (MOTOR_01),3.1.0 Modelo,3.5 Barramento de Módulos,1.6 Comunicação Modbus"
```

→ 5 blocos base + 8 pacotes (4 puxados por `requires`) + 5 UDTs + `DB GLOBAL` + 2 tabelas de tag +
4 iDBs + 2 compiles = **4 erros**, todos o mesmo `PARTIDA_MOTOR_1`: falta o G120 no hardware
(`INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`), os outros 2 são a cascata
disso (`Block call was invalid...`). Sem `--apply` lista o que faria; repetir é no-op.

**Master copy leva bloco, não leva UDT nem tabela de tag.** `PlcType`/`PlcTagTable` *são*
`IMasterCopySource` (ajuda oficial, `85077725323.htm`), mas a master copy de uma *pasta de blocos*
carrega só os blocos — sem os UDTs o compile acusa `Data type 'X' no longer exists`. Continuam vindo
de XML (`import-type` de `library/blocks/<UDT>.xml`, `import-tags`), como já era com o `DB GLOBAL`:
quem o DB precisa sai do próprio SCL composto (`: "MotorDados"` etc., regex no `install-lib`), o
resto é `types[]` no `packages.json`. A tabela `MOTOR_AREA_01 (MOTOR_01)` (29 tags do motor) foi pra
`library/tags/`.

**Nome de pasta pode conter `/`** (`3. Alarmes/Eventos/Falhas`, `4. Motores/Bombas`,
`5. Instrumentação / Atuadores`): `Ops.ResolveFolder` fatiava o caminho por `/` e nunca achava
(`Block folder not found: '3. Alarmes'`). Agora casa o **prefixo mais longo** primeiro em cada
nível — só na leitura (`create=false`); criar continua um segmento por vez.

**Master copy de pasta leva os iDBs junto** — a dúvida que fechava a fatia. `Motor 1 (MOTOR_01)`
(5 iDBs + 1 FC) importou os 6 num PLC de teste; chegam inconsistentes até os FBs-base existirem.

### Bake real da `.al21` + bug do `--force` — ✅ 2026-07-29, fix validado 2026-08-07

Primeira assada de verdade da biblioteca, a partir do projeto base `Base_tia_cli`
(`Software de ETE Insular_Inicial_V21` salvo com esse nome; 1 PLC `CPU1.0 CCO`, 476 blocos,
compila 0 erros):

- `bake-lib.ps1 -Plc "CPU1.0 CCO" -Prune -Apply` → `src/Tia.Lib/tia-cli/tia-cli.al21` = **148 KB**,
  10 master copies: **5 blocos base** soltos (BITS TO WORD, BITS TO DOUBLE WORD, CONTADOR,
  TOTALIZADOR, HORÍMETRO) + **5 pacotes** (`1.1 Acionamento` — leva a `1.1.1 Inversores` junto —,
  `1.3 Instrumentação`, `1.4 Alarmes e Falhas`, `1.5 Diagnóstico`, `1.6 Comunicação Modbus`).
  Script gerado em `workspace/bake-lib.json`. Backup automático em `src/Tia.Lib/tia-cli.backup/`.
- `install-lib.ps1 -Plc PLC_TESTE -Apply` (CPU virgem) → **falhou**: com `--force`, o
  `CreateFrom` de pasta que já existe no alvo **não levanta exceção** — o Portal batiza
  `1.5 Diagnóstico_1` em silêncio e o PLC fica com o pacote duplicado (**medido: 34 → 68 blocos,
  compile Error**). O `catch` de `AlreadyInAnotherFolder` nunca disparava nesse caminho.
- Fix em [`Library.cs`](../src/Tia.Core/Library.cs) (commit `a0df2f7`): com `--force`, apaga
  **antes** de criar (`deleted+created`). O `catch` segue valendo pra colisão de nome em *outra*
  pasta, que aí sim levanta. `rebuild.ps1` rodado (tia.exe reassinado na whitelist).

**Fix validado contra o Portal — ✅ 2026-08-07.** `PLC_TESTE` apagado e recriado virgem
(`add-device --mlfb "6ES7 515-2AN03-0AB0/V3.1"`), depois `install-lib -Apply` dos 5 pacotes:

| rodada | resultado |
|---|---|
| 1ª instalação (CPU virgem) | 35 blocos, **nenhuma pasta `_1`**, compile Success 0 erros |
| 2ª instalação por cima | `já presentes (pulados): 10`, nada tocado, 0 erros |
| `-Update` em `1.5 Diagnóstico` (= `import-master-copy --force`) | 35 blocos, sem `_1`, 0 erros |

Contra a régua antiga (**34 → 68 blocos, compile Error**), fecha os dois buracos que a F8 registra:
`import-master-copy` real e `--force --apply` real. A 3ª linha é a que importa — o skip da 2ª
rodada acontece *antes* do `--force`, então só o `-Update` exercita o `deleted+created`.

**0 erros, não os 4 do G120**: a `.al21` daquela medição tinha só os 5 pacotes de biblioteca. Quem
referencia `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20` são os **moldes**
(`0 Moldes`, `Motor 1 (MOTOR_01)`…), declarados no `packages.json` mas então ausentes da `.al21`.
Fechado em 2026-08-07 — ver "Ciclo completo" abaixo.

### Ciclo completo da biblioteca fechado (2026-08-07)

**Os moldes moram no `PLC_ZERO` do `Project1`**, não no `Base_tia_cli`. Foi o `scaffold --manifest
library/generic.json` que os criou, com os pares `Replace` do manifesto (`S-01A`→`MOTOR_01`,
`Desarenador`→`AREA_01`, `CCM1`→`CCM_01`) — por isso o caminho de cada molde é exatamente
`<target>/<nome>` do `packages.json`. O `CPU1.0 CCO` do `Base_tia_cli` é CPU de cliente (475 blocos):
tem `3.1.0 Modelo` e `3.5 Barramento de Módulos` com **nome de cliente**, e nem `0 Moldes` nem
`Motor 1 (MOTOR_01)`. Assar dali levaria payload de cliente pra `.al21`.

**`bake-lib.ps1 -MoldsOnly`** assa os 4 moldes + as UDT/tabelas do `packages.json` a partir desse
outro PLC, sem re-assar os 5 pacotes de `$Root` com a versão velha do `PLC_ZERO`. `-Prune` é
recusado junto com `-MoldsOnly`: a rodada só enxerga a fatia dos moldes e apagaria os pacotes como
órfãos. As extras vão pra `--lib-folder extras`, **não** pra `$Root` — o `install-lib` trata master
copy não-pasta em `$Root` como base e instalaria UDT/tabela como se fosse bloco.

`.al21` completa = 5 pacotes + 5 blocos soltos + 4 moldes + 3 extras (`Diag_Hardware` `PlcStruct`,
`Genericos` e `MOTOR_AREA_01 (MOTOR_01)` `PlcTagTable`).

**`Library.Open` reusava a library já aberta ignorando o modo.** Se um `list-library` (ou a UI)
tinha aberto a `.al21` `ReadOnly`, todo verbo de escrita morria com
`Cannot write to read-only libraries` — erro do `MasterCopyComposition.Create`, que não diz nada
sobre quem abriu antes. Agora, quando `write` e `IsReadOnly`, faz `UserGlobalLibrary.Close()` e
reabre `ReadWrite`.

**Régua final, no `PLC_TESTE`** (já com os 5 pacotes; `install-lib` pulou os 9 presentes):

| passo | erros |
|---|---|
| `install-lib` dos 4 moldes (`-Apply`) | **4** — todos a constante do G120 |
| `add-device 6SL3244-0BB12-1FA0/4.7.13` + `insert-telegram --number 20 --change --apply` | 4 |
| `connect-subnet` do PLC e do drive no mesmo `--io-system` | **0 · Success** |

**O telegrama sozinho não cria a constante de hardware.** Depois do `insert-telegram` o compile
continuava nos mesmos 4 erros: `..~PROFINET_interface~Standard_telegram_20` só existe quando o drive
é **IO device daquele controlador**. São dois `connect-subnet` na ordem — primeiro o PLC
(`--io-system X` com `IoController` = `CreateIoSystem`), depois o drive (`IoConnector` =
`ConnectToIoSystem`, e o verbo levanta se o IO system ainda não existir). Nome de IO system próprio
por PLC (`PROFINET IO-System_TESTE`) evita pendurar o drive no controlador errado quando outra CPU
divide a mesma subnet.

### Hardware do molde: o G120 (2026-07-28)

Os 4 erros que sobram na CPU virgem são a constante de hardware
`INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20` — o inversor não existe no
projeto. O que a arqueologia dos AML já responde, sem sondar a API:

- **Inversor certo = `OrderNumber:6SL3244-0BB12-1FA0/4.7.13`** (CU240E-2 PN). `add-device` com esse
  MLFB monta a estação inteira: `System:Device.G120-2` + rack `System:Rack.G120PN-2` + head com o
  nome pedido + `PROFINET interface` (é esse nome que a constante exige) + 2 portas.
  O nome do *item* é o do `--name`; a estação sai do `--station`.
- O G120X do AsBuilt é outra família (**GSD**, `GSDML-V2.34-SIEMENS-SINAMICS_G120X-20180814.XML`),
  interface chamada `PN-IO` — não serve pro molde, a constante não bateria.
- Telegrama do lado GSD: `.../SM/IDS_TEL20` plugado em `DRIVE_1` posição 2. Do lado System (G120-2)
  o identificador do `Standard telegram 20, PZD-2/6` **não está na ajuda** nem em nenhum AML/XML do
  repo (Insular tem 30 G120 e nenhum telegrama plugado) — daí o verbo `plug-module`.
  **⚠️ Conclusão revista em 2026-08-07 — ver "Telegrama do G120" abaixo. O identificador não está
  em lugar nenhum porque não existe: telegrama de drive System não é submódulo de catálogo.**

**Verbos novos** (`src/Tia.Core/Hardware.cs`):
- `plug-module --device X [--item I] [--type TID] [--name N] [--pos P] [--apply]` — `PlugNew` da
  ajuda `87727705355.htm`. Sem `--item` e sem `--type` = **sonda**: varre os itens do device e
  devolve os slots livres de cada um (`GetPlugLocations`), porque nome de item se repete no G120
  (head e drive object têm o mesmo nome). Com `--type`, o dry-run devolve `canPlug`
  (`CanPlugNew`) — é como se confirma um identificador de catálogo antes de escrever.
- `delete-device --name X [--apply]` — `Project1` acumulou ~15 CPUs de teste; limpar era manual.
- `set-attr --device X [--item I] --name A --value V [--apply]` — escreve **qualquer** atributo que
  o `list-attrs` mostrar, sem verbo novo por atributo. O tipo sai do valor atual (enum inclusive):
  o Portal recusa `int` onde espera byte e `"True"` onde espera bool. Atributo desconhecido falha
  antes de escrever, apontando o `list-attrs`. Smoke no `PLC_ZERO`: `PlantDesignation` do G120
  `""` → `CCM_01`, 2º apply = `none (already set)`, revertido no fim.
- `add-tag --table T --name N --type Bool --address %M10.0 [--comment C]` / `delete-tag --table T
  --name N` — acrescentar **uma** tag exigia montar o XML da tabela inteira e reimportar (foi assim
  que a `Genericos.xml` nasceu). `PlcTagComposition.Create` **exige endereço** (não tem overload de
  2 args) — o buraco livre sai do `free-memory`. Idempotente: tag existente é `skip`, com tipo e
  endereço atuais no resultado. Smoke: `ZZ_SMOKE_BIT` criada em `%M5600.0`, achada pelo `find`,
  apagada.
- `list-attrs --device X [--item I] [--like SUB]` — read-only, `GetAttributeInfos` + valor atual.
  Usada pra descartar a hipótese "telegrama é atributo": a `PROFINET interface` do G120 tem 20
  atributos (`InterfaceOperatingMode`, `PnSubslotNumber`, `PrioritizedStartup`…) e o head 16
  (`PlantDesignation`, `LocationIdentifier`…) — **nenhum de telegrama**. É submódulo plugável, e o
  identificador de catálogo dele só sai plugando um na GUI e lendo o `TypeIdentifier`.
  (A última frase estava errada — ver a seção seguinte.)

### Telegrama do G120: nunca foi `plug-module` — ✅ 2026-08-07

A busca pelo TypeIdentifier de `Standard telegram 20` falhou em três frentes (ajuda do F1, AML do
AsBuilt, `list-attrs`) por um motivo só: **telegrama de drive System não é submódulo de catálogo**.
O drive object expõe uma composição própria, e é ela que insere o telegrama:

```
Siemens.Engineering.MC.Drives.TelegramComposition.CanInsertTelegram(Int32, TelegramType)
Siemens.Engineering.MC.Drives.TelegramComposition.InsertTelegram(Int32, TelegramType)
```

Caminho: `deviceItem.GetService<DriveObjectContainer>()` → `.DriveObjects` → `driveObj.Telegrams`.
`TelegramType` = Main / Supplementary / Additional / Safety / Torque / Edge.

**Por que ficou invisível tantas sessões**: a API mora em `Siemens.Engineering.Startdrive.dll`, e o
gate 3 do `init.ps1` copiava só 3 das 14 assemblies do `PublicAPI` (Base, Step7, WinCCUnified). Não
era limite de Openness nem de documentação — era assembly que o projeto nunca referenciou. A pista
veio de `Czarnak/totally-integrated-claude` (MIT), e foi confirmada direto no
`Siemens.Engineering.Startdrive.xml` da instalação local antes de virar código.

**O que mudou**:
- `init.ps1` `$dllNames` e `Tia.Core.csproj` agora incluem `Startdrive`. O resolver de runtime do
  `Program.cs` já era genérico por nome — nenhuma mudança lá.
- `src/Tia.Core/Drives.cs`: `list-telegrams --device X` (read-only, drive objects + telegramas de
  cada um) e `insert-telegram --device X --number N [--type Main] [--item I] [--drive-object D]
  [--apply]`. Dry devolve `canInsert`; telegrama igual já presente é `skip`, telegrama diferente do
  mesmo tipo é `conflict` + `canChangeTelegram` (trocar é outra decisão, não implícita).
- O caminho GSD **continua sendo `plug-module`**: o G120X do AsBuilt carrega `.../SM/IDS_TEL20`
  como submódulo plugado de verdade. São duas famílias com dois mecanismos, não um erro de um lado.

**Smoke em `Base_tia_cli` (2026-08-07), 3 ramos verdes** contra `INVERSOR_S-01A CCM1`
(station `SINAMICS G_46`), que já tem o telegrama posto:

| chamada | resultado |
|---|---|
| `list-telegrams` | `MainTelegram #20`, 12 bytes in / 4 out |
| `insert-telegram --number 20` | `skip (already present)` |
| `insert-telegram --number 1` | `conflict`, `presentNumber: 20`, `canChangeTelegram: true` |

**`DriveObjectNumber` pode estourar em vez de devolver valor**: nesses G120 responde
`Drive object number could not be retrieved` (`EngineeringTargetInvocationException`) e derrubava o
verbo inteiro. Toda leitura de atributo de drive passa por `Try()` e degrada pra
`"unavailable: <msg>"` — um drive ilegível não pode matar a listagem. Identificação real é o caminho
do item, não o número.

**Ramo que escreve fechado — e o caso normal é `--change`, não insert** (2026-08-07). Um G120-2
recém-criado (`add-device --mlfb "OrderNumber:6SL3244-0BB12-1FA0/4.7.13"`) **já vem com
`MainTelegram #1`**: drive sem telegrama nenhum não existe na prática, então o ramo puro de
`InsertTelegram` para Main é inalcançável e o que o ciclo da biblioteca precisa é trocar 1 → 20.

- `EraseTelegram(MainTelegram)` **não serve**: o Portal recusa com `Main telegram can not be
  deleted.` A troca é in-place, `telegram.TelegramNumber = N` (a propriedade tem setter;
  `CanChangeTelegram(N)` é o gate).
- `insert-telegram` ganhou `--change`. Sem ele, telegrama diferente continua `conflict (pass
  --change to replace)` — trocar joga fora o telegrama atual, então não é implícito. Com
  `--change` sem `--apply` = `would change`; `canChangeTelegram: false` = `cannot change`.
- `--apply` agora respeita `canInsert: false` (`status: cannot insert`) em vez de deixar o
  `InsertTelegram` estourar com `An error occurred while setting the attribute Telegram (750) is
  not supported on this DriveObject.` — o dry-run já sabia a resposta.

Smoke num `ZZ_TG_TEST` criado e apagado no `Base_tia_cli`: `#1` → `--change --apply` → `changed`,
12 bytes in / 4 out, `list-telegrams` confirma `#20`, e repetir sem `--change` volta
`skip (already present)`. Os 4 erros da régua (`..~PROFINET_interface~Standard_telegram_20`) são
`insert-telegram --number 20 --change --apply` por drive.

### Lint de camada no `audit` — ✅ 2026-07-28

Sexto check: **`1. FB Bibliotecas` não pode depender de bloco de área**. Se um FB de biblioteca
chama bloco de área, a biblioteca deixa de ser instalável sozinha — é exatamente o que o
`install-lib` sofre. Camada = 1º segmento da pasta; nome de bloco é único no PLC, então o mapa
nome → pasta resolve a camada do chamado, e o xref dá a chamada (`Inventory.AllSources`).

**O xref traz os dois sentidos no mesmo saco.** Sem filtrar, o check acusou 21 falsos positivos no
`PLC_ZERO` (`FB FALHA → PARTIDA_MOTOR_1` — é o contrário, o FC é que chama o FB). A direção está em
`Location.ReferenceType`: `Uses` = src chama r, `UsedBy` = o inverso (a ajuda lista a propriedade em
`148785557643.htm`, sem dizer os valores). Com o filtro: **0 no `PLC_ZERO` e 0 no projeto de
referência** (`CPU1.0 CCO`), que é a régua.

**Achado de brinde**: o projeto do cliente tem uma pasta `ClaudeTest/Sub` (cicatriz de teste antigo)
— só o `audit` a pegou, no check do `(TAG)`. Apagar exige o OK do usuário (projeto de produção).

**Diálogo de aceite volta depois do `rebuild.ps1`**: hash novo do `tia.exe` → o Portal *já aberto*
mostra `Openness access (0033:000666)` e a chamada fica pendurada com CPU ~0. Achar a janela:
`EnumWindows` filtrando pelo PID do portal (título `Openness access`). Só o clique resolve.

## Bugs abertos (smoke 2026-07-27)

- ~~**`import-block` dry-run dá falso positivo em XML que não é bloco.**~~ ✅ corrigido
  2026-07-27. `Ops.RequireRootType` valida o root object antes de reportar `action`:
  `SW.Blocks.*` (`import-block`), `SW.Tags.PlcTagTable` (`import-tags`), `SW.Types.*`
  (`import-type`). Dry agora sai 1 com
  `XML root object is 'SW.Tags.PlcTagTable', expected 'SW.Blocks.*'`. Teste offline
  `Ops.RequireRootType`; smoke real: 4 combinações (2 aceitas, 2 recusadas) no AsBuilt.

## Clonar acionamento — fluxo real validado (2026-07-27, AsBuilt)

Objetivo do usuário ("mais uma bomba igual à BH-01A") fechado ponta-a-ponta clonando
**BH-01B → BH-01C** na Elevatória de Gordura. Verbos novos desta rodada:

- `add-db-member --db X --name M [--path A.B] [--type T | --like SIBLING] [--apply]` — a lacuna
  registrada antes (nenhum verbo *criava* instância de UDT na DB global). `--like` clona o nó do
  irmão e insere logo depois. Idempotente (`action: exists`). `ResolveSection` cobre as duas
  formas do XML: Struct nativo aninha `<Member>` direto, instância de UDT expande em
  `<Sections><Section>`.
- `clone --block N | --table T --replace OLD=NEW [--at %M432.0] [--folder A/B] [--apply]` —
  export → substituição textual → import. Um `--replace BH-01B=BH-01C` reescreve de uma vez nome
  do bloco, símbolos de tag, caminhos do DB global e instance DBs. `--at` reendereça as tags Bool
  em sequência; tag de largura maior aborta em vez de sobrepor endereço.
- `free-memory [--bytes N] [--from B]` — read-only, buracos livres da área %M (2588 tags,
  605 bytes usados, topo %M9001). Foi ele que apontou o bloco usado no teste (`%M432.0`, 8 bytes).

**Sequência que funciona** (cada passo um verbo, nunca `run --script`):
`free-memory` → `add-db-member` (instância + struct de comando do par) → `compile --block "DB GLOBAL"`
→ `clone --table` → `clone --block` (5 instance DBs, depois o FC) → `compile --apply` → `save-project`
→ `diff-block`. Resultado: PLC inteiro compila Success/0 erros, `diff-block` do FC clonado `identical`.

**Ordem é obrigatória, não estilo**: todo import deixa o alvo inconsistente e
`Inconsistent blocks and PLC data types (UDT) cannot be exported` derruba o *próximo* export —
inclusive de blocos que só *referenciam* o DB alterado. Compilar entre etapas.

`replicate-fc` **não** serve para este projeto: exige pasta nomeada `... (ID)` (AsBuilt usa
`Bomba Reserva BH-01B`) e é replicador em massa — sobrescreve todas as pastas irmãs a partir do
molde, não clona um equipamento. Dry no AsBuilt: 0 grupos, 61 pastas puladas.

**Limite conhecido**: tags de IO físico (`BOMBA_2_ELEVATORIA_DE_GORDURA_*`) não são clonadas —
uma bomba nova de verdade precisa de %I/%Q próprios, que dependem de hardware novo. `free-memory`
cobre só %M; endereço físico continua manual, de propósito.

### Biblioteca em um comando: extras da `.al21` + hardware declarado (2026-08-07)

**`install-lib` não lê mais XML solto de `library/`.** UDT e tabela de tag agora entram por
`import-master-copy --name "extras/<nome>"` da própria `.al21` — `Library.ImportMasterCopy` já
ramificava `isType`/`isTable` com `ResolveTypePath`/`ResolveTagPath`, então foi troca de script, sem
C# novo. `bake-lib -MoldsOnly` passou a assar também os UDT citados pelos ramos do DB GLOBAL
(regex `: "X"` sobre `library/db-global/*.scl`, a mesma que o `install-lib` usa em runtime): sem
isso a troca quebraria em 4 dos 5 UDTs, porque o `packages.json` só declarava `Diag_Hardware`.
`.al21` agora = 5 pacotes + 5 blocos soltos + 4 moldes + **7 extras**.

**Bloco `devices` no `packages.json`** fecha o "instalar biblioteca" em um comando: `install-lib`
emite `add-device` → `insert-telegram --change` → `connect-subnet` do PLC → `connect-subnet` do
drive, **antes** dos blocos (o primeiro `compile` já cobra a constante). Device já presente não é
recriado; o par `connect-subnet` vai mesmo assim, porque o drive pode existir ligado em outro
controlador.

**Régua numa CPU virgem (`PLC_LIB2`, criada só pra isso): compile Success / 0 erros**, biblioteca
inteira instalada em um comando. Reinstalar em cima = 13 pulados, 0 a instalar, Success/0.

Três bugs que essa régua expôs — todos de idempotência, todos silenciosos:

- **`connect-subnet` achava IO system pelo nome na subnet inteira.** Um IO system de *outra* CPU
  com o mesmo nome fazia o verbo responder `exists` sem ligar nada, e o drive virava IO device do
  controlador errado — em silêncio, aparecendo só como
  `..~PROFINET_interface~Standard_telegram_20 not defined` no compile. Agora procura em
  `controller.IoSystem` (é **um por controlador**) e levanta dizendo o nome real se o controlador
  já tem outro. O default do macro virou `PROFINET IO-System_<PLC>`.
- **`ConnectedToIoSystem == io` nunca era verdade**: wrapper EOM não é estável por referência.
  Religava o drive toda vez e, quando ele já estava em outro IO system, o Openness recusava
  (`already connected to an io system`). Agora compara por nome e, se for outro, `DisconnectFromIoSystem`
  antes de ligar (`ioSystemAction: moved`).
- **`Get-Existing`**: `list-blocks` sem `--folder` devolve array cru, e `$json.blocks` num array é
  *enumeração de membro* — devolve `@()`, não `$null`. A raiz parecia sempre vazia, então pacote de
  `target` vazio (`0 Moldes`) era reimportado toda rodada sem `--force` e o Portal batizava
  `0 Moldes_1` calado (10 blocos duplicados, medido).

`library/blocks/` deixou de ser dependência do `install-lib`; a `.al21` (também gitignored) é o
único artefato de payload, e `bake-lib` continua sendo como cada máquina repõe o seu.

### F8 fechada: `replicate-instruments --apply` real (2026-08-07)

O `in-sync` eterno não era bug: o projeto de teste **foi gerado pelo mesmo algoritmo**, então as 7
áreas batem com o que o gerador produz. Nem deletar a FC alvo destrava — a OB de chamada fica
inconsistente e o gerador precisa exportá-la (`Inconsistent blocks ... cannot be exported`). O que
destrava é **um instrumento novo**, que é o caso de uso real:

`clone --table "MEDIDOR_DE_VAZÃO_ULTRASSÔNICO (FQIT-01)" --replace FQIT-01=FIT-99` para uma pasta
de área nova + 3 `add-tag` (as tags de I/O que o molde referencia e que não moram na tabela da área)
+ `add-db-member --like` para o ramo do DB. Depois disso:
`created | TESTE_TOTALIZADOR`, `addedCalls: ["TESTE_TOTALIZADOR"]`, as outras 7 áreas intactas em
`in-sync`, **compile Success / 0 erros / 0 warnings**. Conteúdo conferido por `explain-block`: a
chamada do `FB TOTALIZADOR` com iDB `FB TOTALIZADOR FIT-99` e todos os pinos remapeados de `FQIT-01`
para `FIT-99` (tags e caminho no `DB GLOBAL`). Artefatos do teste removidos depois (FC, iDB, pastas,
tabela, OB restaurada do `_ob_cache.xml`) e o projeto **fechado sem salvar** — o `.ap21` em disco
nunca mudou.

Duas cicatrizes anotadas no caminho: `--at` do `clone` só realoca tag de bit (`%MD1021` recusa, com
mensagem certa), e `import-block --folder` **cria a árvore que faltar a partir da raiz** — passar
`5.2 Totalizadores` em vez de `5. Instrumentação / Atuadores/5.2 Totalizadores` cria uma pasta
paralela de mesmo nome, e aí o gerador não acha a FC existente e morre em colisão de nome.

### `delete-db-member` — o contrário que faltava (2026-08-07)

`add-db-member` e `edit-db-member` existiam sem inverso: tirar um membro era trabalho manual no
GUI. Mesma coreografia dos dois (export → edição do XML → import Override), mesma idempotência
(membro ausente = `missing (no-op)`), e o mesmo aviso do rename — **apagar não corrige quem
referencia o membro**, o `xref --name DB` mostra quem é.

**Bug de raiz que o teste do delete achou**: `ResolveSection` decidia "isto é um struct?" contando
membros aninhados. Struct esvaziado pelo delete deixava de ser navegável — e o `add-db-member` não
conseguia mais repor nada nele. Agora pergunta se o membro expande em `<Sections><Section>` ou
declara `Datatype="Struct"`. O caso "path através de membro não-struct falha" continua reprovando.

Régua real no `PLC_TESTE`/`DB_DUMMY`, batch de 6 steps, `failed: 0`: no-op ×2, create, compile
Success/0, delete `--apply`, compile Success/0 — DB de volta ao estado original.
A primeira rodada teve 2 falhas que **não** eram do verbo: faltou `compile --apply` entre o import
e o export seguinte, e o Openness devolveu `Inconsistent blocks ... cannot be exported`. É a regra
"compile entre etapas" do CLAUDE.md cobrando, num verbo novo.

## Migração do repo para skill (2026-08-06)

O repo inteiro vira a skill `tia`: `SKILL.md` na raiz e o checkout em `~/.claude/skills/tia`,
como submódulo de `Codyte/skills` (que já carrega `navindex`, `caveman`, `handoff`, `ponytail`).

Por quê: `~/.claude/skills/` já é um repo com submódulos; a cópia que o gate 6 fazia vivia lá
como pasta untracked, divergindo em silêncio. O *tracked* do repo são 1,36 MB / 159 arquivos e
nenhum caminho fixo `C:\Scripts` no código — cabe. O pesado é untracked e não viaja
(`proj/` 1,9 GB, `workspace/` 36 MB, `src/Tia.Lib` 8,1 MB).

Invariantes que a migração encosta:
- **Um checkout só** — a whitelist do Openness é gravada por caminho do exe; dois clones brigam.
- **A task `TiaSmokeRun` grava o caminho absoluto do `taskrun.ps1`** — mover o repo mata a rota da
  sessão 0 com o sintoma `No running TIA Portal instance found`, idêntico ao de portal fechado.
  `init.ps1` gate 4 agora compara e re-registra sozinho (1 UAC).
- **Teto do padrão**: "tudo vira skill" vale enquanto o tracked for pequeno e a instalação couber
  num script. Projeto que versione payload pesado volta pro padrão skill fina + repo separado,
  ligados por `TIA_CLI_HOME`.

### Gate de máquina limpa exercitado (2026-08-07)

`init.ps1 -Check` nunca tinha rodado contra um checkout virgem. Rodado agora sem precisar de outra
máquina: `git clone --local` do repo pra uma pasta temporária e `-Check` lá dentro — o clone não
tem `lib/*.dll`, nem `tia.exe`, nem `.al21`, e as tasks/whitelist/PATH apontam pro repo real, que é
exatamente o estado de máquina nova. Saem os 6 `FALTA` com o comando de correção em cada um e
`exit 1`; o repo real continua 9/9 e `exit 0`.

Dois textos corrigidos por causa disso:
- **Whitelist sem `tia.exe`**: mandava `Start-ScheduledTask -TaskName TiaWhitelist`, que não tem o
  que whitelistar antes do build. Agora o hint depende do exe existir — sem ele, aponta pro
  `rebuild.ps1`, que builda e whitelista de uma vez.
- **`.al21` ausente**: dizia só "assar com bake-lib.ps1", sem dizer que é gitignored, que só o
  `install-lib` depende dela, nem que assar exige um projeto que **já tenha** a biblioteca. Clone
  limpo não tem como produzir a `.al21` do nada — a linha agora diz isso.

### Rodadas executadas

| Rodada | Caderno | Resultado | Veredito |
|---|---|---|---|
| FP-01 | filtro prensa (sequência autoral) | [`resultado-2026-08-07.md`](teste-cego/resultado-2026-08-07.md) | compile 0/0; **não foi cega** (a sessão herdou o handoff de quem escreveu o caderno) |
| FP-02 | elevatória + preliminar, 2 áreas, zero SCL autoral | [`resultado-2026-08-10.md`](teste-cego/resultado-2026-08-10.md) | **encerrada**: compile 0/0 + `audit` 6/6, projeto salvo; 12 defeitos de gerador + 2 de infraestrutura (`-Wait` do runner da task esperava o Portal; `list-blocks --folder` era prefixo da raiz); a rota da sessão 0 com o **Portal fechado** ficou provada; **também não foi cega** |

A FP-02 foi desenhada para o outro extremo da FP-01: **nada autoral**, só os 7 verbos `--apply`,
para medir a engine e não a redação de SCL. Todos os 4 geradores tiveram seu **primeiro `--apply`
de verdade fora do projeto de referência** — e os 12 defeitos são a mesma família: o gerador
confundia "o que este projeto tem" com "o que todo projeto tem" (nome da área repetido na DB, molde
sendo um instrumento real, ID sempre com hífen, sufixo da tag de PV sempre igual, molde nunca
mudando). O padrão de correção também é um só: mover a suposição para o config e deixar o código
exigir só o que é estrutural.

O veredito "um agente sem contexto consegue" segue **não provado** — duas rodadas, nenhuma cega.

## F6 — Endurecer os scripts PS (✅ executada 2026-07-27)

**Resultado.** `scripts/_common.ps1` + `scripts/tia.ps1` entregues como planejado; `tia-task.ps1`
removido (o wrapper o substitui). Bugs: **1 já estava corrigido** na máquina *e* no script
(`setup-tasks.ps1:37` já era `-LogonType Interactive` — a auditoria leu o principal da
`TiaWhitelist`, que é S4U de propósito); 2, 3, 4 e 5 fechados.

Dois achados novos, ambos de PowerShell:
- **`$raw -is [pscustomobject]` é verdadeiro até pra `[string]`** (tudo vira PSObject). Era como o
  runner distinguia `{"id","args"}` de array cru — a forma legada `["doctor"]` virava `args` vazio e
  o CLI cuspia o help com exit 1. Correto é testar a propriedade: `if ($null -ne $raw.args)`.
- **Splat de array vazio vira argumento `""`**: `Invoke-Tia close-project @($Save ? @('--save') : @())`
  passa string vazia pro CLI. Trocado por `if/else` explícito em `use-project`/`clone-hw`.

**O shell do agente pode nascer na sessão 1** (VSCode na sessão do usuário — foi o caso na
verificação: `SessionId=1`, `UserInteractive=True`). A premissa "nenhum macro roda do agente"
vale só quando ele nasce na sessão 0. O roteamento cobre os dois casos e `TIA_VIA_TASK=1` força a
rota da task — sem esse knob o ramo da sessão 0 seria código morto até falhar em produção.

### Plano original (referência)

Auditoria dos 11 scripts em `scripts/`. Problema central: **nenhum macro roda a partir do
agente**. `use-project`/`prep-project`/`raio-x`/`clone-hw` chamam `& $exe` no processo local;
na sessão 0 isso é sempre `"No running TIA Portal instance found"` (confirmado nesta máquina:
`[Environment]::UserInteractive=False`, `SessionId=0`; portal em `SessionId=1`). Hoje eles só
servem se o **usuário** rodar à mão numa janela da sessão 1 — o agente refaz o protocolo taskio
na unha todo turno.

### F6.1 — Bugs pontuais (independentes, fazer primeiro)

| # | Arquivo | Defeito | Correção |
|---|---------|---------|----------|
| 1 | `setup-tasks.ps1:19` | Registra `TiaSmokeRun` com `LogonType S4U`. S4U cai na sessão 0 e nunca attacha. A task viva na máquina está `Interactive` (corrigida à mão), o script ficou pra trás. | `-LogonType Interactive`. **Não re-rodar o script depois de corrigir** — é `-Force`, recria a task e derruba o canal que hoje funciona. Corrigir e deixar quieto; vale só pra máquina nova. |
| 2 | `taskrun.ps1:15` | `& $tia @tiaArgs *> out.txt` funde stdout e stderr. Contrato do CLI é stdout=JSON / stderr=log humano; fundido, `ConvertFrom-Json` engasga. | Redirects separados (`1>` / `2>`), arquivos distintos. |
| 3 | `taskrun.ps1:11` | Quem apaga `exit.txt` é o runner, depois da task já ter arrancado. Entre `Start-ScheduledTask` e esse `Remove-Item` o `exit.txt` da rodada anterior ainda está no disco → poller lê e conclui que terminou. | Resolvido de graça pelo run-id da F6.2 (nome único por chamada = sem arquivo velho pra ler). Runner para de apagar. |
| 4 | `rebuild.ps1` `Get-RegHash` | `Select-Object -First 1` sobre os filhos de `...\Openness`: com V19 e V21 no registro compara contra a que vier primeiro, enquanto `whitelist.ps1` grava em todas. Só olha a chave `Entry`, ignora a `EntryLocal` que o próprio whitelist escreve. | Comparar contra **todas** as versões/chaves; stale = qualquer uma divergente. |
| 5 | `smokeloop.ps1` × `taskrun.ps1` | Nomes de saída divergentes (`result.txt` vs `out.txt`); CLAUDE.md documenta só `out.txt`. Poll no arquivo errado dependendo da rota de pé. | Mesmo protocolo run-id nas duas rotas (F6.2). |

### F6.2 — `scripts/_common.ps1` + `Invoke-Tia` (o núcleo)

Um arquivo novo, dot-sourced pelos macros. Mata as três duplicações de hoje
(caminho do exe em 5 arquivos, `c:\Scripts\TIA Portal` hardcoded em 5, `TITANXNEXUS\Carlos_Ortiz`
em 2 — e o repo é público como `tia-cli`, nada disso roda em clone de terceiro).

```powershell
$script:Repo = Split-Path $PSScriptRoot
$script:Exe  = Join-Path $script:Repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

function Invoke-Tia {
    param([int]$TimeoutSec = 600, [Parameter(ValueFromRemainingArguments)][string[]]$TiaArgs)
    if ((Get-Process -Id $PID).SessionId -ne 0) { & $script:Exe @TiaArgs; return }   # sessão 1: direto
    # sessão 0: rotear pela task TiaSmokeRun (que roda na 1)
}
```

Regras de projeto (cada uma resolve um problema conhecido):

- **Roteamento por sessão.** `SessionId -ne 0` → invoca direto. Senão → canal taskio. Callers
  não sabem a diferença.
- **`$global:LASTEXITCODE`.** Uma função PS não seta `$LASTEXITCODE` sozinha. No caminho da task,
  ler o código de `exit-<id>.txt` e atribuir a `$global:LASTEXITCODE` — assim todos os
  `if ($LASTEXITCODE) { exit }` dos macros continuam valendo **sem edição**.
- **Run-id único por chamada.** `cmd.json` passa a aceitar as duas formas: array (`["doctor"]`,
  compatível com o uso manual documentado) ou objeto `{"id":"...","args":[...]}`. Com id, o runner
  escreve `out-<id>.txt` / `err-<id>.txt` / `exit-<id>.txt`. Isso resolve **dois** problemas de
  uma vez: a race do item 3 (não existe arquivo velho com aquele nome) e o lock que forçou o
  `smokeloop` a rotacionar pra `result.txt` — quando um verbo inicia o portal, o portal herda o
  handle do arquivo de saída e o mantém aberto enquanto viver; nome fixo = próxima rodada não
  consegue redirecionar pra ele. Nome único contorna sem depender de o portal morrer.
  Prune de `out-*`/`err-*`/`exit-*` com mais de 1 dia na entrada, erro ignorado (podem estar
  travados pelo portal). `workspace/` é gitignored.
- **Ordem do protocolo** (cliente): escreve `cmd.json` → `Start-ScheduledTask TiaSmokeRun` →
  poll de `exit-<id>.txt` → emite `out-<id>` em stdout, `err-<id>` em stderr → seta
  `$global:LASTEXITCODE`.
- **Timeout.** Default 600s (`open-project` leva 2-4 min; compile de projeto real também demora).
  Estouro = erro claro, não trava. Cobre também o gap do `smokeloop`, que hoje faz
  `Start-Process -Wait` sem limite e prende o loop pra sempre num `open-project` travado.
- **Guard de concorrência (D9).** `cmd.json` já existente na entrada = outra chamada em andamento
  → falha alto em vez de clobber.

Depois disso, `scripts/tia.ps1` vira wrapper de 3 linhas (`. _common.ps1; Invoke-Tia @args`) —
o comando único que hoje não existe e que o CLAUDE.md descreve em prosa como 3 passos manuais.

### F6.3 — Migrar os macros

`use-project.ps1`, `prep-project.ps1`, `raio-x.ps1`, `clone-hw.ps1`: trocar `& $exe` por
`Invoke-Tia` e o `& pwsh -NoProfile -File use-project.ps1` (spawn de pwsh ~1s) por dot-source.
Zero mudança de lógica — os checks de `$LASTEXITCODE` seguem funcionando pelo `$global:`.
Ganho: os quatro passam a rodar do agente.

### F6.4 — Robustez menor

- `prep-project.ps1` é o único macro que muta (`compile --apply` + `save-project`) **sem gate** —
  `clone-hw.ps1` tem `-Apply`, esse não. Apontar projeto errado grava nele. Adicionar `-Apply`
  com o mesmo contrato (dry = só `doctor`).
- `use-project.ps1:21`: `open-project` é a última linha, sem checar exit — propaga pelo exit do
  script, mas sem mensagem própria de "abriu e falhou".
- `clone-hw.ps1`: sem check de exit no `save-project` final; salva sem confirmar que o
  `import-cax` aplicou.
- `raio-x.ps1`: `ConvertTo-Json -Depth 8` no agregado de xref pode truncar em silêncio.

### F6.5 — CLI (opcional, C#)

`raio-x.ps1` faz **um Attach por OB** no loop de xref (segundos cada). `xref --name` aceitar
lista de nomes (ou `--all-obs`) resolve na raiz. Só vale se o raio-x doer no projeto real.

### Verificação

- F6.1: `pwsh scripts/rebuild.ps1` ALL PASS; diff do `setup-tasks.ps1` conferido **sem re-rodar**.
- F6.2: o check é end-to-end e vale mais que teste unitário — `pwsh scripts/tia.ps1 doctor`
  **do shell do agente** (sessão 0) tem que devolver JSON e sair 0. Hoje isso é impossível.
- F6.3: `pwsh scripts/raio-x.ps1 <Projeto>` do agente, read-only, contra o AsBuilt.
- F6.4: `prep-project` sem `-Apply` não pode salvar nada.

### Ordem

F6.1 → F6.2 → F6.3 (F6.4 junto com a 3, mesma edição de arquivo) → F6.5 só se necessário.
Commit por bloco.

