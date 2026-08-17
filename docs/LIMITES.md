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
| **Objeto tecnológico (eixo, came, cinemática, PID) não pode ser criado.** `TechnologicalInstanceDBComposition` não tem `Create`. | Limite de API | TO nasce na GUI ou vem no import do projeto. `list-motion` lê o que existe. |
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
| **WinCC Unified: tela não tem export SimaticML.** O que exporta é tag (`HmiTagComposition.Export`) e script (`HmiScriptModule.Export`). Tela é modelo de objetos tipado — `HmiSoftware.Screens`, `HmiScreenBase`, `HmiScreenItemBase` com propriedades individuais. | Limite de API | Construir tela objeto a objeto, propriedade a propriedade. Funciona, mas é outra engine — não reaproveita o gerador de XML. |

---

## Como acrescentar aqui

Limite novo entra com as quatro coisas: **o que não dá**, **a evidência** (termo sondado e resultado,
ou mensagem de erro exata), **a natureza** (API / decisão / checkout / produto / SO) e **a saída**.
Sem a evidência a linha vira boato e alguém vai sondar de novo.
