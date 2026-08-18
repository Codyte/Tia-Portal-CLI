<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L18    Verbos do `tia` (gerado por `scripts/gen-verbs.ps1` — nao editar a mao) -->
<!--   L22    session -->
<!--   L28    read -->
<!--   L57    structure -->
<!--   L67    hardware -->
<!--   L83    write -->
<!--   L110   library -->
<!--   L118   multiuser -->
<!--   L121   bulk -->
<!--   L126   batch -->
<!--   L129   sim -->
<!--   L133   meta -->
<!--   L136   notas -->
<!-- ======================= END NAV INDEX ======================= -->

# Verbos do `tia` (gerado por `scripts/gen-verbs.ps1` — nao editar a mao)

`tia <verb> [--plc NAME] [--portal PROJETO|PID] [--apply]  (--portal obrigatório se houver mais de um TIA Portal aberto)`

## session
- `open-project --file X.ap21 [--no-ui]`
- `create-project --dir D --name N [--no-ui]`
- `save-project`
- `close-project [--save]`

## read
- `info`
- `list-devices`
- `list-blocks [--folder A/B] [--type FB|FC|OB|GlobalDB|InstanceDB] [--count]  (sem filtro = ~500 blocos num projeto real; --count = só o total por pasta)`
- `list-tags [--table T]  (sem --table: uma linha por tabela; com --table: as tags dela)`
- `list-types`
- `tree [--out DIR]  ← COMECE AQUI: outline do PLC inteiro (blocos + tabelas de tag + UDTs) em plc-navi.md, ~26 KB num projeto de 476 blocos (o mesmo em JSON: 117 KB)`
- `find --pattern P* [--kind block|table|tag|type|constant]  (constant = constantes de sistema e de usuário; é como se confere <drive>~PROFINET_interface~Standard_telegram_20 sem ler o compile)`
- `xref --name X  (bloco, tag, tabela ou UDT → o que ele usa)`
- `trace --equipment AG-01  (símbolos do equipamento + quem referencia; ~9s em projeto grande)`
- `list-hmi [--device X]  (WinCC clássico e Unified: telas + tag tables; `api` diz qual)`
- `export-hmi-tags --table "Pasta/Tabela" [--device X]  (SimaticML da tabela de tags da IHM; é onde aparece a conexão e a tag do PLC por trás de cada tag de tela)`
- `import-hmi-tags --file F.xml [--device X] [--folder "Pasta/Sub"] [--replace OLD=NEW ...] [--apply]  (par do export-hmi-tags; --folder é caminho completo a partir da raiz de tags e o nome da tabela sai do XML)`
- `hmi-tree  (outline de todas as IHMs → hmi-navi.md, agrupado por pasta; irmão do `tree`)`
- `export-screen --screen "Pasta/Sub/Tela" [--device X]  (SimaticML da tela; só WinCC clássico — Unified não exporta tela)`
- `import-screen --file F.xml [--device X] [--folder "Pasta/Sub"] [--replace OLD=NEW ...] [--apply]  (--folder é caminho completo a partir da raiz de telas, como no import-block; --replace troca texto no XML antes do import — é assim que se replica tela de área, porque a tela liga tag por NOME (TargetID="@OpenLink"), sem ID a remapear)`
- `delete-screen --screen "Pasta/Sub/Tela" [--device X] [--apply]  (par do import-screen; sem ele tela de smoke só sai pela GUI)`
- `list-screen-items --screen "Pasta/Sub/Tela" [--device X] [--like P] [--group]  (um objeto por linha: nome, tipo, x, y, w, h, tag — 150 objetos cabem em 7 KB, contra 800 KB do XML da tela. --group agrega por equipamento lido do nome da tag e devolve a `region` de cada um, que é o recorte pronto p/ copy-screen-items; a coluna `group` diz de que Hmi.Screen.Group o objeto faz parte; o bbox é só dos objetos COM tag, então fundo e rótulo pedem alargar a região)`
- `audit-screen [--screen "Pasta/Sub/Tela"] [--device X] [--max N]  (cruza a tag de cada objeto de tela com as tags da própria IHM: tag que não existe, e tag sem código de equipamento (o placeholder `tag1` do editor). Sem --screen varre TODA tela do device — um export por tela. Feitio do `audit`: checks com ok/findings/detail e `scanned`. Cruzar com a tag do PLC sai `skipped`: a tag de HMI clássica só expõe Name e o SimaticML da tabela traz só a Connection)`
- `set-screen-items --screen "Pasta/Sub/Tela" [--set "Nome:x=530,y=356"] [--remove Nome] [--rename Velho=Novo] [--rename-from-tag] [--group NOME=x,y,w,h] [--device X] [--apply]  (todos repetíveis, um export e um import para N edições — import de tela custa 20-170 s. --set move/redimensiona (x,y,w,h em qualquer combinação); --remove apaga; --rename dá nome auto-descritivo no lugar do contador do editor (Switch_18 -> BF-01-EC-01_CMD_LIGA); --rename-from-tag faz isso na tela inteira, tirando o nome da própria tag a partir do 1º código de equipamento (objeto SEM tag fica com o nome do editor: batizar seria adivinhação; é idempotente e o que não dá vai p/ `skippedRename` com o motivo); --group embrulha num Hmi.Screen.Group os objetos INTEIRAMENTE contidos na região, sem mexer em geometria (coordenada de filho é absoluta). Ordem fixa: set, remove, rename, group. Nome ausente vai p/ `missing` e os outros seguem; nome repetido na tela é erro)`
- `copy-screen-items --from-screen "<molde>" --region x,y,w,h --screen "<destino>" --at x,y [--replace BF-01=BF-05] [--device X] [--apply]  (estampa: copia os objetos INTEIRAMENTE contidos na região, deslocados, renumerando ID e desduplicando ObjectName. Não há catálogo de estampas no CLI — cada tela da casa tem seu dialeto, então o grupo sai da tela que serve de molde)`
- `list-motion [--like X] [--params]  (objetos tecnológicos: eixo, came, cinemática — nome, tipo (TO_PositioningAxis...) e versão; --params traz os parâmetros, centenas por eixo. Read-only: o Openness não cria TO)`
- `export-block --name X [--out DIR]`
- `export-tags --table X [--out DIR]`
- `explain-block --name X | --file F.xml  (LAD/FBD → texto compacto; --file roda sem TIA)`
- `list-interface [--folder A/B] [--name X] [--file F.xml] [--out DIR]  (assinatura Input/Output/InOut dos FB/FC da pasta numa chamada só — é o que se lê antes de escrever qualquer chamada; --file roda sem TIA)`
- `export-type --name X [--out DIR]`
- `free-memory [--bytes N] [--from B] [--count K]  (buracos livres na área %M; length -1 = até o fim)`

## structure
- `create-folder --path A/B [--path C/D ...] [--tags|--types] [--apply]  (repetir --path cria a árvore toda num attach; '\/' é barra literal no nome da pasta: "1. I\/OS/QA-01")`
- `delete-folder --path A/B [--tags|--types] [--apply]`
- `delete-block --name X [--apply]`
- `create-instance-db --name X --of FB [--folder A/B] [--apply]  (molde importado por XML chega sem iDB → 'Missing instance DB')`
- `move-block --name X | --pattern P* --folder A/B [--out DIR] [--apply]  (export→delete→import; o Openness não move bloco)`
- `delete-type --name X [--apply]  (UDT)`
- `import-type --file F.xml [--apply]`
- `scaffold --manifest F.json [--replace OLD=NEW ...] [--apply] [--force]  (árvore da lei + moldes num projeto novo; --replace troca no XML e nas pastas antes do import; "Cpu" no manifesto barra família errada, --force ignora)`

## hardware
- `add-device --mlfb "6ES7 ..." --name X [--station S] [--group G] [--apply]`
- `delete-device --name X [--apply]`
- `list-attrs --device X [--item I] [--like SUB]  (read-only: atributos e valores do device item)`
- `set-attr --device X [--item I] --name A --value V [--apply]  (qualquer atributo que o list-attrs mostrar; tipo vem do valor atual)`
- `plug-module --device X [--item I] [--type TID] [--name N] [--pos P] [--apply]  (sem --type: lista slots livres; com --type: canPlug e, com --apply, pluga. Alvo de plug é o rack: --item Rack_0, não o device. MLFB sem versão devolve plugAs com a 1ª versão que o slot aceita)`
- `list-telegrams --device X  (read-only: drive objects SINAMICS, telegramas de cada um e o endereço de cada telegrama — %IB/%QB, que não aparece em DeviceItem.Addresses)`
- `insert-telegram --device X --number N [--type Main|Supplementary|Safety|Torque|Edge] [--item I] [--drive-object D] [--change] [--apply]  (--change troca o telegrama presente: G120 novo já vem com o 1)  (telegrama de drive NÃO é submódulo de catálogo — plug-module não coloca)`
- `set-address --device X [--ip A.B.C.D] [--mask M] [--pn-name N] [--item X1] [--apply]  (device com mais de uma interface exige --item)`
- `set-io-address --device X [--item I] [--io Input|Output] [--start N] [--apply]  (endereço inicial do módulo de I/O; não é atributo — set-attr não alcança, e o import-cax ignora. Sem --item: varre o device (sonda). Sem --start: só lista)`
- `list-io-map [--device X] [--io Input|Output]  (read-only: todo endereço de I/O do projeto — device/item, %IB..%QB e o próximo byte livre por tipo; inclui o telegrama de drive SINAMICS, que não vive em DeviceItem.Addresses e sem isso deixava o nextFreeByte entregar byte já ocupado)`
- `connect-subnet --device X --subnet S [--io-system IO] [--apply]`
- `set-memory-bytes --device X [--system 1] [--clock 0] [--apply]  (habilita FirstScan/AlwaysTRUE/Clock_1Hz na CPU)`
- `export-cax [--out DIR]`
- `import-cax --file F.aml [--apply]`

## write
- `import-block --file F [--folder A/B] [--replace OLD=NEW ...] [--apply]`
- `import-source --file F.scl [--folder A/B] [--apply]  (bloco nasce na pasta, sem move-block; fonte só de TYPE vai pra pasta de UDT. KeepOnError: bloco inválido entra inconsistente em vez de derrubar o lote — compile depois pra ver o erro. Fonte com acento exige UTF-8 com BOM: sem BOM o dry-run recusa)`
- `import-ladder --file F.scl [--name N] [--folder A/B] [--apply]  (SCL subset → LAD; dry-run works without TIA)`
- `import-tags --file F [--folder A/B] [--replace OLD=NEW ...] [--apply]  (--replace reescreve o XML antes de importar — nome da tabela e das tags; tag de PLC é única no CPU, então derivar tabela de outra exige trocar todos os nomes)`
- `add-tag --table T --name N --type Bool --address %M10.0 [--comment C] [--apply]  (uma tag em tabela existente; endereço livre em %M sai do free-memory)`
- `delete-tag --table T --name N [--apply]`
- `rename-block --name X --to NEW [--apply]  (bloco ou UDT; refs seguem, igual ao GUI)`
- `set-tag --table T --name N [--type T] [--address %M10.0] [--comment C] [--rename NEW] [--apply]  (só o que for passado muda; --rename exige Openness V20+)`
- `clone --block N | --table T --replace OLD=NEW [--replace ...] [--at %M432.0] [--folder A/B] [--with-instances] [--apply]  (--replace é troca de TEXTO no XML exportado: caminho de membro de DB lá é cadeia de <Component>, então troque um componente por vez e mantenha a mesma profundidade da origem. --with-instances cria os iDBs que o clone passa a referenciar; sem eles o compile morre em 'Missing instance DB')`
- `add-call --block X --fb NOME [--inst iDB] [--param P=<tag|DB.caminho.membro|const>] [--after N] [--title T] [--comment C] [--out DIR] [--apply]  (rede LAD com a chamada, EN no powerrail; os pinos saem da interface do bloco chamado. --fb aceita o nome com ou sem o prefixo 'FB '/'FC '. --inst é exigido para FB e recusado para FC. --after 0 = primeira rede, omitido = no fim)`
- `delete-network --block X --index N [--out DIR] [--apply]  (N é 1-based, a numeração do explain-block)`
- `set-retain --block FB --member M [--off] [--out DIR] [--apply]  (Remanence na declaração do FB; o Openness recusa em iDB e o import-source não expressa)`
- `add-db-member --db X --name M [--path A.B] [--type T | --like SIBLING] [--out DIR] [--apply]`
- `edit-db-member --db X --name M [--path A.B] [--type T] [--rename NEW] [--out DIR] [--apply]  (rename não corrige quem referencia o membro)`
- `delete-db-member --db X --name M [--path A.B] [--out DIR] [--apply]  (não corrige quem referencia o membro)`
- `compile [--block X | --folder A/B] [--errors] [--apply]  (--errors = lista plana {where,message,count} em vez da árvore)`
- `diff-block --file F.xml [--name X]  (read-only, normalized compare)`
- `doctor [--verb V] [--config F]  (read-only preflight dos verbos geradores)`
- `audit [--plc N] [--max 50] [--db "DB GLOBAL"]  (projeto × lei do PADRAO/BOAS-PRATICAS; o check R2 exporta a DB global para --out, o resto é read-only)`
- `gen-profinet --config F [--apply]`
- `standardize-tags [--config F] [--apply]`
- `gen-fault-ob [--config F] [--out DIR] [--apply]`
- `replicate-fc --config F [--template PASTA] [--target-folder PASTA] [--out DIR] [--apply] [--force]  (--template: molde de outra área; --target-folder: só escreve sob ela; --force: sobrescreve pasta populada)`
- `gen-alarm-fc [--config F] [--area NOME]* [--out DIR] [--apply]`
- `replicate-instruments --config F [--out DIR] [--apply]`

## library
- `list-library --file X.al19`
- `retrieve-library --file X.zal19 [--dir D] [--upgrade] [--apply]  (dearquiva .zal1x → .al2x; é como se consome biblioteca oficial da Siemens (LGF 109479728, DriveLib 206539), que o SIOS entrega arquivada; --upgrade sobe a versão da library junto)`
- `create-library --file X.al21 [--apply]  (library vazia; o Portal cria <pasta>/<nome>/<nome>.al21 — caminho real volta em "path")`
- `import-master-copy --file X.al19 --name M [--folder A/B] [--apply] [--force]  (--force: apaga o de mesmo nome e recria — é como se atualiza pacote já instalado)`
- `add-master-copy --file X.al21 (--name BLOCO | --folder A/B) [--lib-folder L] [--apply]  (PLC → library; --folder = pasta inteira = pacote; substitui se já existir)`
- `delete-master-copy --file X.al21 --name M [--apply]`

## multiuser
- `list-server-projects --server HOST [--port N] [--http] [--keep-connection]  (read-only: projetos do TIA Project Server, lock e sessões locais)`

## bulk
- `snapshot  (inventário completo: devices + blocos + tabelas + UDTs de todo PLC)`
- `find --pattern "*" --kind tag  (todas as tags)`
- `→ saída na casa das centenas de KB (snapshot = 251 KB, find de tag = 821 KB num projeto real). SEMPRE com --out-file, depois grep no arquivo. Não é leitura de orientação: pra isso é `tree``

## batch
- `run --script ops.json [--summary]  (JSON array de arg-arrays, uma sessão só; step que falha vira {ok:false,error} e o batch segue; exit 1 se algum falhou. --summary = só {steps,failed,ms,slowest[3],errors[]}, sem o resultado de cada step. Todo step traz `ms`, e o batch traz o total — é a medida de onde foi o tempo. --plc/--out-file do processo NÃO descem pros steps: cada step carrega os seus. Exige projeto JÁ aberto: o attach é 1x, antes do 1º step, então open-project/create-project (e list-server-projects, que roda sem projeto) não podem ser step — chamar antes, sozinhos)`

## sim
- `sim-run [--plc X] [--instance plc_1500_1] [--pc-interface PLCSIM] [--script sim.json] [--no-download] [--apply]  (PLC virtual do S7-PLCSIM Advanced: attach na instância ligada por 'pwsh scripts/sim-host.ps1 -Start' (ou pelo control panel), baixa o programa do projeto por Openness, roda os passos. Exige o PLCSIM CLÁSSICO FECHADO — ele toma o mesmo canal. So baixa em access point PLCSIM: nome fora disso e recusado antes do download (--allow-physical libera; nunca ha download em CPU real). --no-download pula o download e roda os passos no programa que já está na instância (o download é ~80% do tempo). Passos do script: ["write","tag","valor"], ["read","tag"], ["wait","ms"], ["run"], ["stop"], ["state"], ["tags","filtro"]; tag de DB vai com as aspas do Portal. Dry-run lista as instâncias registradas e as interfaces de PC do download)`
- `sim-diag [--instance plc_1500_1] [--watch SEG]  (retrato da instância do PLCSIM Advanced: estado, modo, CPU, IP, licença, monitoração de ciclo, tag list. NÃO precisa de TIA Portal aberto nem de projeto — a API do PLCSIM é independente do Openness. --watch SEG assina os eventos e devolve o que MUDOU na janela (LED, estado operacional, falha de rack/estação); LED não tem getter na API, só evento, então sem --watch não há estado de LED)`

## meta
- `--version  (versão do CLI + qual instalação do Openness este exe carrega; é a 1ª linha de qualquer bug report)`

## notas
write verbs are dry-run unless --apply; default --out is .\workspace\exports; saída acima de 60k chars (TIA_MAX_STDOUT) derrama SOZINHA em workspace/auto-<verbo>.json e o stdout recebe o stub {file,bytes,count,head,autoSpill} — --full desliga e dumpa tudo no stdout (use em script que faz ConvertFrom-Json); --out-file F.json (qualquer verbo: JSON completo no arquivo escolhido, stdout só o stub); o que --force apaga é exportado antes para workspace/recovery/<verbo>-<timestamp>/ (caminho no campo recoveryDir; --no-backup apaga sem rede); --retry N (busy, default 3) --timeout SEC; exit: 0 ok, 1 geral, 2 uso, 3 arquivo, 4 TIA, 5 timeout

