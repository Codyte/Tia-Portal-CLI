<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L11    LIMITES — o que Openness e PLCSIM não fazem, e qual é a saída -->
<!--   L28    Online e runtime -->
<!--   L41    Simulação -->
<!--   L51    Objetos do projeto -->
<!--   L66    HMI -->
<!--   L83    Como acrescentar aqui -->
<!-- ======================= END NAV INDEX ======================= -->

# LIMITES — o que Openness e PLCSIM não fazem, e qual é a saída

Este arquivo existe para o agente **orientar em vez de sondar**. Cada limite abaixo custou tempo de
sessão para ser descoberto no braço; a coluna "saída" é o que se faz no lugar.

Distinção que importa em toda a lista:

- **Limite de API** — não existe membro que faça. Sondar de novo não vai achar.
- **Decisão do repo** — a API tem, e nós escolhemos não expor. Reabre com motivo novo.
- **Limite do checkout** — falta uma DLL em `lib/`, não falta API. Resolve-se acrescentando ela.

Método de verificação: `python scripts/tia-help.py --sdk "<termo>"` indexa os 31448 membros
documentados das 14 assemblies do Openness. "0 hits" num termo bem escolhido é a evidência de
ausência usada aqui.

---

## Online e runtime

| Limite | Natureza | Saída |
|---|---|---|
| **Valor de tag online.** `PlcTag` expõe nome, tipo de dado, endereço e comentário. Não tem `Value`. Não existe API de monitoring, watch ou snapshot de valores. | Limite de API | (a) `sim-run` contra **S7-PLCSIM Advanced**, que tem `Read`/`Write` de tag; (b) montar uma watch table por `PlcWatchTableComposition.Import` e deixar o humano ver os valores no TIA; (c) fora do Openness, S7 comms (Snap7/Sharp7). |
| **Buffer de diagnóstico da CPU.** Não existe. As duas únicas ocorrências de "diagnostic buffer" no SDK são configuração de controle de HMI (`AlarmViewBasicSource.S7Diagnosis`, `HmiSystemDiagnosisControl`), não leitura. | Limite de API | GUI do TIA (Online & Diagnostics); ou, fora do Openness, SZL por S7 comms / servidor OPC UA da CPU. |
| **Estado operacional (RUN/STOP) e comando de partida/parada.** Zero hits no SDK. | Limite de API | `IInstance.OperatingState` do PLCSIM Advanced — só serve para PLC virtual: `sim-diag` (retrato) e `sim-run` (passos `run`/`stop`/`state`). Em CPU real, é a GUI. |
| **LEDs, alarmes de rack, eventos de falha.** | Limite de API | Idem: existem no PLCSIM Advanced (`WaitForOnLedChangedEvent`, `AlarmNotification`, `RackOrStationFaultEvent`), não no Openness. `sim-diag --watch SEG` entrega os três — **estado de LED só por evento**, `IInstance` não tem getter de LED (medido 2026-08-17). |
| **Portal ocupado não tem tipo de exceção nem HResult.** `--sdk "busy"` devolve **0 hits** nas 14 assemblies: não há `EngineeringBusyException`, código ou propriedade de estado. O que chega é a mensagem, e ela vem **localizada** na língua do Portal instalado. | Limite de API | `Ops.IsBusyMessage` casa por radical em 6 línguas (`busy`, `ocupad`, `beschäftigt`, `occup*`, `занят`) e percorre a cadeia de `InnerException`; é o que o `--retry N` usa. Portal numa língua fora da lista = mais um radical lá, não uma sondagem nova. |
| **Assinatura/fingerprint do programa carregado no PLCSIM.** Não existe getter. | Limite de API | `sim-run` devolve `programCheck` (nome do controller × PLC do projeto) — prova origem, não versão — e recusa instância com 0 tags. Gravar uma assinatura própria mudaria o projeto do usuário. |
| **Rollback de escrita.** Não há transação: nada de `BeginEdit`/`Rollback` num import parcial. | Limite de API | Backup antes do delete: o que morre sob `--force` vai para `workspace/recovery/<verbo>-<timestamp>/` (`recoveryDir` no JSON, fail-closed, `--no-backup` é o opt-out), e o XML salvo é o que o `import-block` relê. |
| **`go-online`, `download`, `compare online/offline` como verbos.** A API **tem** (`OnlineProvider.GoOnline`, `DownloadProvider.Download`, `PlcSoftware.CompareTo(ISoftwareCompareTarget)`). | Decisão do repo (D8, 2026-08-07) | Download em PLC de campo é risco operacional que o CLI não carrega: `--apply` protege projeto, não protege processo. O humano faz no TIA, vendo a planta. Sinal para reabrir: PLCSIM/PLC de teste dedicado **e** um caso de uso que não seja download. |

## Simulação

| Limite | Natureza | Saída |
|---|---|---|
| **PLCSIM clássico não tem API pública.** É GUI. Não há como acionar, ler ou observar por código. | Limite de produto | Nenhuma dentro do Openness. Fora: NetToPLCSim (expõe o clássico em TCP/102) + Snap7/Sharp7. |
| **PLCSIM clássico e Advanced brigam pelo mesmo canal.** O clássico sequestra o access point `PLCSIM` do S7ONLINE; o download sai `Success` com a instância Advanced vazia — falso positivo caro. Com o clássico aberto, `RegisterInstance` dá `-48, CommunicationInterfaceNotAvailable`. | Limite de produto | Fechar o clássico. É pré-requisito de `sim-host.ps1`, não recomendação. |
| **Famílias que o PLCSIM Advanced V8 não simula:** S7-1200 de 1ª geração (1211C/1215C/1217C), S7-300, S7-400. O enum `ECPUType` do header oficial cobre S7-1500 completo, ET200SP, ET200PRO, 1500 R/H, Software Controller, 1504D/1507D TF e **S7-1200 G2** (1212C/1214C/1216/1217). | Limite de produto | Para 1200 G2, `sim-host.ps1 -Article "<MLFB do G2>"` — já parametrizado, não testado aqui. Para as demais, só PLCSIM clássico, e aí vale a linha acima. |
| **Instância registrada dentro do `tia.exe` morre com o processo.** O Runtime Manager sobe in-proc; não há serviço (`Get-Service *PLCSIM*` vazio). | Limite de produto | `scripts/sim-host.ps1 -Start` — host longevo separado, task `TiaSimHost`. |
| **A API do PLCSIM não atravessa a sessão 0**, igual ao Openness: `SimulationRuntimeManager.Version` volta vazio e `RegisterInstance` dá `-1, InvalidErrorCode` mesmo com o manager vivo na sessão 1. | Limite do SO | Rota da task (`LogonType Interactive`), que é o que `Invoke-Tia` e `sim-host.ps1 -Start` já fazem. |

## Objetos do projeto

| Limite | Natureza | Saída |
|---|---|---|
| **Openness não enumera as instruções do editor.** As 5 abas do painel *Instructions* (Basic, Extended, Technology, Communication, Optional packages) são catálogo do editor, não objetos do projeto: não há composição a percorrer — `--sdk "instruction"` devolve 1 membro, e é `ModuleUseFromUserProgram`. Medido 2026-08-21. | Limite de API | Escrever **SCL** e deixar o compilador resolver (`import-source`) cobre as 5 abas sem catálogo; instrução que é bloco de sistema entra por `add-call`; rede LAD pronta, por `clone`. O catálogo para consulta é a ajuda do F1: `tia-help.py --search "TON"`. |
| **Objeto tecnológico não nasce de qualquer tipo/versão.** `TechnologicalInstanceDBComposition.Create(nome, tipo, Version)` só aceita os pares da tabela *Overview of technology objects and versions* (`TOOpennessenUS/.../95673198603`) — tipo ou versão fora dela levanta, e a API não expõe catálogo para consultar antes. | Limite de API | `create-motion --name X --type PID_Compact [--version 3.0] --apply`; sem `--version` o verbo herda a de um TO do mesmo tipo já no PLC. `delete-motion` desfaz, com o XML em `workspace/recovery/`. |
| **Parâmetro de TO só escreve onde o Portal dá acesso**, e a recusa só aparece na tentativa: `Retain.CtrlParams.Gain` levanta `EngineeringNotSupportedException` — `'set_Value' is not supported ... The property 'Value' is read-only` — enquanto `Config.InputUpperLimit` grava (120 → 3, relido). `SetAttribute("Value", x)` cai no mesmo setter. Medido 2026-08-21 num `PID_Compact` V3.0. | Limite de API | `set-motion-param --name TO --param P --value V --apply`. Parâmetro de `Retain`/runtime se escreve pelo programa, no iDB do TO. |
| **`explain-block` não lê SCL.** Ele converte `FlgNet` (LAD/FBD); num bloco SCL o texto sai como literais concatenados (`T#5S0.0027648100.0`) e os membros do temporizador estático aparecem numa seção `NONE`. Medido 2026-08-21 num FB com `TON_TIME` + `NORM_X`/`SCALE_X`. | Decisão do repo | Ler SCL de volta é `export-block` + o XML (`<StructuredText>`). Escrever continua sendo `import-source`, que compilou o mesmo bloco com 0 erros. |
| **`Remanence` em iDB é recusado.** | Limite de API | Retentividade se declara no FB: `set-retain --block <FB> --member M`. `import-source` também não expressa retentividade. |
| **Openness não move bloco entre pastas.** | Limite de API | `move-block` faz export de todos → delete → import no destino. |
| **Bloco inconsistente não exporta.** Todo import deixa o alvo inconsistente. | Limite de API | `Ops.ExportFresh` compila só o alvo e segue — já embutido nos 16 exports. Sobra o caso de inconsistência **externa** (UDT/DB que o bloco usa), que exige `compile --apply`. |
| **Watch table não tem `Create`.** `PlcWatchTableComposition` só oferece `Import(FileInfo, ImportOptions)` (SimaticML). | Limite de API | Gerar o XML e importar — mesmo caminho de todo verbo de escrita do repo. |
| **Struct vazio não é aceito como membro de DB.** Deixa o DB inconsistente e trava todo verbo que exporta. | Limite de API | `add-db-member --path A.B.C` cria o ramo já com o membro-folha dentro. |
| **Programa de segurança (F).** O que existe no SDK é framework de **add-in** (`Siemens.Engineering.AddIn.Safety`, `SafetyCompileAddInProvider`), não API de escrita de bloco F. Não sondado a fundo. | Não verificado | Tratar como fora de escopo até haver caso de uso; a senha e a assinatura F são o obstáculo esperado. |

## HMI

| Limite | Natureza | Saída |
|---|---|---|
| **WinCC clássico (Comfort/Advanced/Professional): tela tem roundtrip SimaticML completo.** `Screen.Export(FileInfo, ExportOptions)` e `ScreenComposition.Import(FileInfo, ImportOptions)`; idem `ScreenTemplate`, `ScreenPopup`, `ScreenSlidein`, `ScreenOverview`, `ScreenGlobalElements`. Também `ScreenComposition.CreateFrom(MasterCopy)`. | **Sem limite** | É o mesmo padrão export→edita XML→import que o repo já usa para bloco de PLC. Caminho fácil. |
| **Tag de HMI não responde a dump de atributo: `GetAttributeInfos()` devolve só `Name`** (medido 2026-08-17 no projeto real, 20 tags). Os membros tipados de `Hmi.Tag.Tag` também não compilam sem `Siemens.Engineering.WinCC.Extension` (o tipo `ILimit`). | Limite de API | `export-hmi-tags --table "Pasta/Tabela"`: o SimaticML traz `LinkList` (`Connection`, `DataType`, `HmiDataType`, `AcquisitionCycle`), escala e `AddressAccessMode`. |
| **`HmiTarget.Connections` volta vazio mesmo com as tags apontando para uma conexão** (`HMI_Connection_2` no `LinkList` do export, projeto real, 4 IHMs). | Limite de API | O `connections: []` do `list-hmi` não é "não há conexão". Ler o nome no `export-hmi-tags`. |
| ~~**`Siemens.Engineering.WinCC.dll` (clássico) não está em `lib/`**~~ — **resolvido 2026-08-17**: entrou em `$dllNames` do `init.ps1` e como `<Reference>` no `Tia.Core.csproj`. A instalação tem 14 DLLs, o build referencia 5. | Limite do checkout | Máquina que der pull precisa re-rodar `init.ps1`. Falta ainda `WinCC.Extension` (tipos como `ILimit`) — acrescentar só quando um verbo pedir. |
| **O símbolo do PLC por trás de uma tag de HMI clássica não sai por API nem por export** (medido 2026-08-17): a tag só expõe `Name`, e o SimaticML da tabela traz do vínculo apenas o **nome da `Connection`** (`LogicalAddress` vem vazio quando `AddressAccessMode` é `Symbolic`). O nome da tag *parece* o caminho do PLC com `_` no lugar do separador (`DB GLOBAL_AFERICAO_..._CONFIG`), mas `_` também aparece dentro de nome de membro — reverter é adivinhação. | Limite de API | `audit-screen` cruza tag de tela × tag **da IHM** e devolve o check de PLC como `skipped` com este motivo. Cruzar de verdade exige convenção de nome do projeto, não API. |
| ~~**Tabela de tags da IHM não tem verbo de import no CLI**~~ — **verbo escrito 2026-08-18** (`import-hmi-tags --file F.xml [--folder "Pasta/Sub"] [--device X] [--replace OLD=NEW] [--apply]`), builda; falta o smoke contra o Portal. Contexto original (F14): tela de área nova replica por `import-screen --replace`, mas as tags trocadas não existem na IHM e o `audit-screen` lista as 5. **Não é limite de API** — `Siemens.Engineering.Hmi.Tag.TagTableComposition.Import(FileInfo, ImportOptions)` existe (`tia-help.py --sdk "TagTable Import"`), só falta o verbo. | Verbo faltando (resolvido, pendente de smoke) | Era o único elo da cadeia de área nova que pedia GUI. `Hmi.ImportTagTable` espelha o `import-tags` do PLC: `folderAction: create\|reuse` no dry, `ImportOptions.Override` no apply. |
| **Não há verbo que apague pasta de telas na IHM** (`delete-folder` é do PLC): `delete-screen` tira a tela e a pasta fica vazia. | Verbo faltando | Apagar a pasta pela GUI, ou deixar (pasta vazia não quebra compile). |
| **WinCC Unified: tela não tem export SimaticML.** O que exporta é tag (`HmiTagComposition.Export`) e script (`HmiScriptModule.Export`). Tela é modelo de objetos tipado — `HmiSoftware.Screens`, `HmiScreenBase`, `HmiScreenItemBase` com propriedades individuais. | Limite de API | Construir tela objeto a objeto, propriedade a propriedade. Funciona, mas é outra engine — não reaproveita o gerador de XML. |
| **`explain-block` não renderiza caixa de matemática** — `Calc`, `Normalize`, `Scale`, `Mul` e `Add` somem do texto (medido 2026-08-20 em `FB SETPOINT ESCALONAMENTO`, cujas 3 caixas `Calc` com fórmula `(IN1*IN3)/IN2` não aparecem, e em `FB TOTALIZADOR`). O texto mostra contatos, bobinas, `Move`, comparadores, temporizadores e chamadas. | Decisão do repo (o `explain` é resumo, não decompilador) | Saída que "não aparece escrita" no `explain` **não é saída morta**. Provar no XML: `export-block` e grepar `<Part Name="Calc">` + os `<Component Name>` da rede. |
| **Varrer `<Section Name="Input">` do XML atrás de parâmetro morto pega as instâncias de instrução** declaradas em `Static` (medido 2026-08-20: 25 falsos positivos em `FB MODBUS MASTER BLOCK` — `PORT`, `BUFFER`, `RECORD`, `ID`, `MLEN` são pinos de `RDREC`/`WRREC`/`MB_MASTER`, não da interface do bloco). | Formato do SimaticML (Section aninhada em multi-instância) | Contar só o que está na `<Interface>` de **primeiro nível**; parar a varredura no primeiro `</Interface>`. |

---

## Como acrescentar aqui

Limite novo entra com as quatro coisas: **o que não dá**, **a evidência** (termo sondado e resultado,
ou mensagem de erro exata), **a natureza** (API / decisão / checkout / produto / SO) e **a saída**.
Sem a evidência a linha vira boato e alguém vai sondar de novo.
