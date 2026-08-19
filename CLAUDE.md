<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L12    TIA Portal Openness API — instruções do repo -->
<!--   L26    Regras duras -->
<!--   L72    Build / run (a partir da F1) -->
<!--   L315   Antes de escrever programa de PLC: `--study` -->
<!--   L337   Não sabe como a API se comporta? Consulte a ajuda oficial, não deduza -->
<!--   L369   Sessão 0 × sessão 1 (por que `tia` às vezes não roda direto) -->
<!--   L417   Economia de tokens -->
<!-- ======================= END NAV INDEX ======================= -->

# TIA Portal Openness API — instruções do repo

**Toda sessão: ler `docs/PLANO.md` (decisões + fase atual) e `__navi__.md` antes de qualquer coisa.**
O histórico datado das sagas já fechadas saiu do PLANO inteiro para `docs/DIARIO.md` — ler só
quando a pergunta for "como chegamos nisso".

Mapas de navegação: `__navi__.md` na raiz (árvore do repo) e por pasta, todos do mesmo comando —
`python ~/.claude/skills/navindex/scripts/navindex.py <pasta|.>` da raiz do repo, idempotente.
C# entra desde 2026-08-11 (`scripts/navi-cs.ps1` morreu): o mapa de `src/Tia.Core/` e
`src/Tia.Cli/` sai por pasta, e a **lista completa dos `case "verbo"` fica no header NAV INDEX
no topo de `src/Tia.Cli/Program.cs`** — o mapa da pasta mostra só os 24 primeiros símbolos.
Payload gitignored (`proj/`, `workspace/`, `Scripts_Siemens/`) fica fora dos mapas por construção,
que é o que impede nome de projeto de cliente de voltar pra árvore commitada.

## Regras duras

- Decisões D1–D9 do PLANO valem — não rediscutir sem motivo novo.
- **Escrever programa de PLC = seguir `docs/BOAS-PRATICAS.md`** (R1–R9: UDT obrigatório, DB global
  como agregado de UDTs, ≤8 parâmetros escalares por FB, nome auto-descritivo sem prefixo de tipo,
  bloco nasce na pasta certa, numeração de pasta é a do molde, chamada em LAD e lógica pesada em
  SCL dentro de FB). Compilar não é o aceite — o aceite é `audit` + R1–R9.
- **`run --script --fail-fast` para no 1º step que falha (2026-08-18)**, e o que sobrou sai em
  `aborted`. Isolar step continua sendo o default (bateria de diagnóstico quer todos os erros de
  uma vez); numa corrente de escrita, o step seguinte trabalha em cima do que o anterior não fez.
- **`sim-run` não segue com download que falhou (2026-08-18).** `Download` devolve contagem em vez
  de levantar: `errors > 0` ou `state != Success` agora viram `error` de topo e os passos **não**
  rodam (rodavam contra programa que não subiu e "passavam" lendo lixo). Na falha as mensagens do
  download saem inteiras. E o `GoOffline()` automático só acontece com alvo PLCSIM — sob
  `--allow-physical`, projeto online vira erro pedindo que o humano desconecte.
- **`--force` exporta antes de apagar (2026-08-18).** O que morre sob `--force` (master copy,
  pasta = pacote inteiro, blocos do alvo do `replicate-fc`, tags da tabela do `standardize`) vai
  primeiro para `workspace/recovery/<verbo>-<timestamp>/`, e o caminho volta em **`recoveryDir`**.
  É **fail-closed**: export que falha aborta o delete. Apagar sem rede é `--no-backup`, explícito.
  Rollback automático não existe — o XML salvo é o que o `import-block` relê.
- `Scripts_Siemens/FINAIS/` = referência read-only. `Scripts_Siemens/OLD/` = não tocar.
- Verbos de escrita: dry-run por padrão, `--apply` explícito.
- **Opção desconhecida = exit 2 antes do attach (2026-08-18).** `--ara` por `--area` junto de
  `--apply` rodava o gerador no projeto todo, porque o parser lê a opção que conhece e ignora o
  resto. A lista fica em `Program.KnownOptions`; **opção nova exige entrada lá**, senão o teste
  offline `Cli.KnownOptions` reprova (ele grepa os literais `"--x"` dos fontes).
- **Erro no resultado agora é exit ≠ 0 (2026-08-18).** Verbo que embute a falha num campo `error` e
  volta normal saía 0 e o batch marcava `ok: true` — falso sucesso. `error` de topo = exit 1, e o
  step do batch conta como falha. Campo `error` de item, dentro de lista, continua parcial.
- **`--timeout` é recusado com `--apply`.** O timeout abandona a chamada no meio (sem cancelamento
  nem rollback): em leitura custa o resultado, em escrita deixa o projeto em estado desconhecido.
- **Compile entre etapas: o CLI faz sozinho (2026-08-13).** Todo import deixa o alvo inconsistente e
  o Openness recusa exportar bloco inconsistente. Os **16 exports do repo** passam por
  `Ops.ExportFresh`, que compila **só o alvo** e segue — `clone`, `diff-block`, `explain-block`,
  `list-interface` e os 4 geradores não exigem mais `compile --apply` do PLC inteiro antes
  (eram ~20 dos 49 min da FP-06). Sobra o caso raro: inconsistência **de fora** (UDT ou DB que o
  bloco usa) o compile do bloco não limpa, e aí a mensagem manda `compile --apply`.
  Os verbos que editam por XML (`add`/`edit`/`delete-db-member`, `add-call`, `delete-network`,
  `set-retain`) continuam **provando** o patch: importam com `Override`, compilam e re-exportam.
  Sem essa prova, duas escritas seguidas no mesmo bloco faziam a segunda ler export defasado e a
  primeira sumia com `ok: true` (FP-03, tropeço 6).
- Nunca rodar `tia` em paralelo (Openness single-session) — desde 2026-08-18 o próprio `tia.exe`
  recusa a 2ª chamada simultânea (mutex por sessão de logon), além do lock da rota da task.
- Nunca commitar `Siemens.Engineering.dll`.
- Testes só contra projeto TIA de teste, nunca produção.

## Build / run (a partir da F1)

- Solução em `src/`, target net48 x64. Binário oficial = Debug (`src\Tia.Cli\bin\Debug\net48\tia.exe`).
- **Máquina nova: `pwsh scripts/init.ps1`** = gates (grupo `Siemens TIA Openness`, .NET SDK,
  `lib/*.dll` copiadas da instalação local do Portal) + tasks (1 UAC) + rebuild + shim `tia` no
  PATH/`TIA_CLI_HOME`. Idempotente — re-rodar depois de `git pull`. **`-Check`** = relatório
  read-only dos **8 gates** (exit 1 se faltar um) + o estado vivo, que não é gate e não muda o
  exit — inclusive "repo em `~/.claude/skills/tia`", que o CLI não precisa e só a skill usa.
  **O repo é a skill**: `SKILL.md` na raiz, e o checkout tem que ficar em `~/.claude/skills/tia`
  (submódulo de `Codyte/skills`) — nada é copiado. Um checkout só: a whitelist do Openness é
  gravada por caminho do exe, e a task `TiaSmokeRun` guarda o caminho absoluto do `taskrun.ps1`
  (mover o repo mata a rota da sessão 0 até `init.ps1` re-registrar).
  Scripts não têm caminho nem usuário fixo — tudo sai de `$PSScriptRoot`/`$env:USERNAME`, e a versão
  do Portal (V19–V21) é descoberta em runtime.
- **Macro-verbos — usar SEMPRE em vez da coreografia manual:**
  - `pwsh scripts/tia.ps1 <verbo> [args]` = chamar o CLI de qualquer sessão (ver seção abaixo).
  - `pwsh scripts/rebuild.ps1` = build + testes offline + whitelist (UAC só se tia.exe mudou).
    Nunca rodar dotnet build/whitelist/testes soltos.
  - `pwsh scripts/use-project.ps1 <Nome|caminho.ap21> [-Save]` = garante projeto aberto
    (no-op se já aberto; fecha o atual sem save por padrão; open leva 2-4 min → background).
  - `pwsh scripts/prep-project.ps1 <Nome> [-Apply]` = use-project + doctor (+ compile --apply +
    save só com `-Apply`; projeto real chega sem compilar — rodar antes de qualquer export).
  - `pwsh scripts/raio-x.ps1 <Nome>` = banho read-only → `workspace/<proj>/` (doctor, snapshot,
    devices, tags, types, plc-navi.md, AML, xref dos OBs).
  - `pwsh scripts/clone-hw.ps1 <Origem> <Destino> [-Apply]` = copia hardware via CAx/AML.
  - `pwsh scripts/install-lib.ps1 "<Pacote>[,<Pacote>]" -Plc X [-Apply] [-IoSystem N]` = instala
    pacotes da `.al21` num PLC (clock byte + hardware do bloco `devices` + blocos-base do nível 1 +
    pacote + UDT/tabelas da pasta `extras` da própria `.al21` + iDBs + compile). Sem pacote = lista
    os disponíveis. Pula o que já existe, então repetir é no-op. `scripts/bake-lib.ps1` faz o inverso
    (PLC → library). **Com mais de um TIA Portal aberto, todo verbo exige `--portal <projeto|PID>`.**
  - `tia run --script ops.json` = batch de verbos, attach 1x (~7s por chamada solta). Fluxo FINAIS
    completo em dry: `tia run --script docs/examples/gen-all.json`.
    **Isola steps**: step que falha vira `{ok:false,error,type}` e o batch segue; `exit 1` se algum
    falhou. Bateria onde falha é esperada roda de uma vez só.
    **Exige projeto já aberto** — o attach é 1x, antes do 1º step, então `open-project` /
    `create-project` não podem ser step (fail-fast, o batch inteiro nem começa): abrir/criar numa
    chamada antes (ou `use-project.ps1`) e o batch trabalha em cima. Não vale mudar: abrir projeto
    custa 2-4 min, e a chamada solta a mais custa ~7 s.
  - **Assinatura de verbo → `docs/VERBS.md`** (gerado do help pelo `rebuild.ps1`). Uma leitura de
    ~80 linhas em vez de grep em `Program.cs`.
  - **Nunca `list-blocks` sem filtro** — são ~480 blocos. `--folder A/B` (pega subpastas),
    `--type FB|FC|OB|GlobalDB|InstanceDB`, `--count` (só o total por pasta, ~10 linhas).
  - **Pasta com `/` no nome (`1. I/OS`, `4. Motores/Bombas`) se escreve `\/`**: `--path "1. I\/OS/QA-01"`.
    Vale em qualquer verbo que receba caminho de pasta (regra do `Ops.SplitPath`). Sem o escape,
    longest-match só resolve nome com `/` que **já existe** — na criação, `1. I/OS` vira duas pastas.
    **Dentro de `run --script` escreva `\\/`**: `\/` é escape de JSON e o parser o come antes de o
    argumento chegar no CLI. Na linha de comando, `\/` direto.
    `create-folder --path` é repetível: árvore inteira num attach; caminho que falha vira
    `{path, error}` e os outros seguem.
  - **`list-io-map [--device X] [--io Input|Output]`** = todo endereço de I/O do projeto + próximo
    byte livre por tipo. Varre item, descendentes **e os telegramas dos drive objects SINAMICS**
    (`Telegram.Addresses`, assembly Startdrive — telegrama não vive em `DeviceItem.Addresses`).
    O mesmo endereço sai por drive em `list-telegrams --device X`. Quem precisa do telegrama **no
    programa** usa o `HWID` da constante `<drive>~PROFINET_interface~Standard_telegram_20`, não o
    endereço.
    **`nextFreeByte` é piso, não garantia** — sai só do que o mapa leu, e o mapa não lê os
    `unassigned`; o verbo declara isso em `nextFreeByteExact: false` + `nextFreeByteNote`. Com
    `--device` o campo se chama **`nextFreeByteInDevice`** (próximo livre daquele device, não do
    projeto). A autoridade é o `Next free address: N` do erro do Portal.
  - **`set-io-address` dry-run confere o `--start` contra o mapa** (`conflictCheck: occupied` +
    `conflictsWith`, ou `free (pelo mapa)`). É conferência de leitura: `free` não é garantia, é
    ausência de conflito no que o mapa enxerga.
  - **`audit` são 10 checks** e o R2 **exporta a DB global** para `--out` (só o export mostra o
    datatype dos membros) — não é 100% read-only. **`complete: false` + `skippedChecks[]`** dizem
    que algum check não pôde rodar: `ok: true` com `complete: false` é conformidade **não provada**,
    não projeto aprovado. Check que não pode rodar devolve `skipped` com o
    motivo e **não reprova**. `--db "DB GLOBAL"` nomeia a DB quando a heurística não acha.
    **`scanned`** (`folders`, `blocks`, `callBlocks`, `tagTables`) diz o tamanho da população — é
    como se distingue check conforme de check cego. No projeto-molde real: 96 / 475 / 46 / 195,
    10 verdes.
  - **`--folder` de import é sempre o caminho completo.** `import-block`/`import-tags`/`import-source`
    criam a árvore que faltar **a partir da raiz**: caminho parcial cria pasta paralela homônima e o
    gerador seguinte morre em colisão de nome. O dry-run avisa em **`folderAction: create|reuse`** —
    `create` num caminho que você esperava existir é o sinal de que o caminho está errado.
  - **`move-block --name X | --pattern P* --folder A/B [--apply]`** — o Openness não move bloco; o
    verbo faz `export` (de todos primeiro) → `delete` → `import --folder`.
  - **Chamada em LAD (R8) = `add-call`, nunca FlgNet na mão.** A ordem que funciona:
    `list-interface --folder "1. FB Bibliotecas"` (todas as assinaturas numa chamada) → `clone` do
    molde → `delete-network --index N` nas redes que não servem → `add-call --block X --fb NOME
    [--inst iDB] --param P=<tag|DB.caminho.membro|const>`.
    `--fb` aceita o nome com ou sem prefixo `FB `/`FC `. `--inst` é **exigido para FB e recusado
    para FC** (o `CHAMADA_*` do padrão é sequência de chamadas de FC). **FB sem pino nenhum é
    chamável** (só o `<Instance>` no `Call`). Só o pino **com valor** entra declarado — o Portal
    recusa `<Parameter>` sem fio. Constante sai tipada pelo pino (`TRUE` num Bool vira
    `LiteralConstant`+`ConstantType`). `--after 0` põe na frente, omitido põe no fim.
    **Pino de entrada sem valor é aviso**, fica solto na rede como no molde da casa; **`InOut` sem
    valor é erro**. Rede **com condição em série** continua sendo clone de um molde que já a tenha.
    `add-call`/`delete-network` devolvem **`networksBefore`/`networksAfter`**, `clone` devolve
    `networks`: planejar `--index` de cabeça apaga a rede errada, porque **rede vazia não sobrevive
    ao export** e o clone chega com uma rede a menos.
  - **Área nova = `replicate-fc --template "<pasta molde>" --target-folder "<pasta da área>"`.**
    Sem os dois, o molde é "a 1ª pasta irmã populada" e os alvos são "todas as irmãs" — área nova
    não tem irmã com blocos. O `--template` tem que conter a palavra-chave de `EquipmentTypes`
    (senão falha em vez de replicar com o molde errado); nome ambíguo falha listando os candidatos.
    Os dois valem no config (`TemplateFolder`/`TargetFolder`).
  - **Área nova tem 5 pré-requisitos, e só o 5º se diagnostica sozinho** (medido ponta a ponta
    2026-08-18, F14 do PLANO): (1) pasta do equipamento com `(ID)` no nome; (2) **membro UDT no DB
    global** — sem ele o `replicate-fc` pula o alvo com `Target 'X' has no global-DB instance.
    Skipped.`; (3) tags `<ID>*MODO_LOCAL/MODO_REMOTO`, cuja falta é só **aviso depois** de os 6
    blocos terem sido escritos (daí a 2ª passada); (4) **para área de inversor, o drive SINAMICS no
    hardware** — a FC replicada usa a constante de HWID e o compile reprova com `Tag
    "<drive>~PROFINET_interface~Standard_telegram_20" not defined.`; a sequência é `add-device`
    (mesmo MLFB dos outros) → `insert-telegram --number 20 --change` → `connect-subnet --io-system`,
    e a constante **só nasce quando o drive vira IO device**; (5) pasta de tags em `2. Alarmes` de
    mesmo nome-base da de `3. Partidas` + `<AREA>.ALARMES.WORD_ALARMES_1 : Word`. Área sem
    instrumento é **no-op limpo** no `replicate-instruments` (nem aparece no dry). A tela replica por
    `import-screen --replace`, mas **as tags da IHM não têm verbo de import** — o `audit-screen`
    lista uma a uma o que falta.
  - **`gen-alarm-fc --area NOME` (repetível) limita a geração à área.** Sem escopo, criar 1 área
    regenera todas. O OB `CHAMADA_ALARMES` continua saindo com **todas** as FCs sob a pasta-raiz.
    `--area` que não casa falha listando as pastas de `2. Alarmes`.
  - **`replicate-instruments` acha o `_PV_` no PLC inteiro** quando a tag não está na pasta de alarme
    da área (o projeto real guarda os PV em `1. I/OS/QA-0N`). O dry-run declara **`pvTag`** por
    instrumento e avisa quando o molde usa PV e o alvo não tem tag.
  - **`add-db-member --path` cria o ramo que faltar**, como Struct, já com o membro-folha dentro
    (`structsCreated` lista o que nasceu). `--type Struct` continua recusado: struct vazio deixa o
    DB inconsistente e trava todo verbo que exporta.
  - **Edição por XML é plural desde 2026-08-19 (F16): N edições, um round-trip.** O custo é do
    tamanho do bloco, não do número de edições — 5 membros na `DB GLOBAL` numa chamada custam 23,9 s,
    os mesmos 23,4 s de 1 (medido, `docs/BENCHMARKS.md`). `add-db-member --member "A.B.NOME:Tipo"`
    (repetível, caminho+nome+tipo no mesmo argumento porque listas pareadas por posição desalinham
    em silêncio; `--name/--path/--type/--like` seguem valendo), `delete-network --index N --index M`
    (índices são os do bloco **antes** da chamada; o verbo apaga do maior para o menor) e
    **`add-call --fb A ... --fb B ...`**, onde cada `--inst/--param/--after/--title/--comment`
    pertence ao `--fb` que veio antes dele. Membro repetido na mesma chamada é erro antes do export.
    **Patch que falha aborta antes do import** — nada entra pela metade. **XML igual depois dos
    patches = nenhum import** (`changed: false`, 1,3 s em vez de 23 s): a idempotência era funcional,
    agora é real. A prova (compile + re-export) continua inteira, e o erro nomeia qual edição não
    entrou.
  - **Todo verbo devolve `ms` e `attachMs`**, não só step de `run --script`. **`ms` é só o
    trabalho** (a mesma conta do `ms` de step, que nunca incluiu attach) e **`attachMs` é o attach,
    que é onde o diálogo modal de autorização pendura**. Total = `ms + attachMs`; somar é trivial,
    subtrair exigiria lembrar do modal — e foi esse esquecimento que fez 48 s virarem ">600 s" no
    registro. Medido no 1º `info` depois de um `rebuild.ps1` com o Portal aberto:
    `ms: 507, attachMs: 33465`; na chamada seguinte, `attachMs: 310`. **`attachMs` alto = ambiente**
    (modal ou portal ocupado), nunca API. Relógio de parede muito acima da soma = espera do wrapper.
  - **`connect-subnet` com nome que não existe lista as subnets do projeto** (`existingSubnets`).
  - **Biblioteca da Siemens chega arquivada: `retrieve-library --file X.zal19 [--dir D] [--upgrade]`.**
    O SIOS entrega `.zal1x` e todo o resto do CLI (`list-library`, `import-master-copy`,
    `install-lib.ps1`) só abre `.al2x`. O Portal monta `<dir>/<nome>/<nome>.al2x` e **recusa destino
    já existente** — por isso destino ocupado volta `action: exists` em vez de estourar. `--upgrade`
    (`RetrieveWithUpgrade`) sobe a versão da library no mesmo passo, que é o caso de `.zal19` em V21.
  - **`list-motion [--like X] [--params]`** = objetos tecnológicos (eixo, came, cinemática, PID):
    nome, tipo (`TO_PositioningAxis`…) e versão. **Read-only por limite da API**, não por escolha:
    `TechnologicalInstanceDBComposition` não tem `Create` — TO nasce na GUI ou vem no import de
    projeto. `--params` traz os parâmetros, que são centenas por eixo.
  - **`clone --replace` é troca de texto no XML exportado, não caminho de membro.** Caminho de DB lá
    é cadeia de `<Component Name="…"/>`: `A.B.C=X.Y` casa zero vezes — trocar **um componente por
    vez**, e o destino tem que ter a **mesma profundidade** da origem.
  - **`clone --with-instances`** cria os iDBs que o XML clonado passa a referenciar (sem eles o
    compile morre em `Missing instance DB`).
  - **Retentividade se declara no FB: `set-retain --block <FB> --member M [--off]`.** O Openness
    recusa `Remanence` em iDB e o `import-source` não expressa retentividade.
  - **`create-instance-db --of` aceita nome aproximado**: acento, caixa, underscore e espaço duplo
    não contam. Casou um, resolve e devolve `resolvedFrom`; casou vários, falha listando.
  - **`run --script ops.json --summary`** = `{steps,failed,ms,slowest[3],errors[]}` em vez do
    resultado de cada step. **Todo step traz `ms`** e o batch traz o total — é a medida de onde o
    tempo foi, sem `Measure-Command` por fora.
  - **Saída grande já vem cortada por padrão.** Acima de 60 000 chars (`TIA_MAX_STDOUT`) o verbo
    derrama sozinho em `workspace/auto-<verbo>.json` e o stdout recebe
    `{file,bytes,count,head,autoSpill}`. Depois é grep no arquivo.
    - **`--out-file F.json`** = mesma coisa, no caminho que você escolher.
    - **`--full`** = desliga o corte. É o que script de PowerShell com `ConvertFrom-Json` precisa:
      sem ele o pipe recebe o stub, que parseia sem erro e devolve `$null`. Os macros já passam.
    - `TIA_MAX_STDOUT` **não atravessa a rota da task** (sessão 0): lá vale o default.
  - **Orientação num projeto novo = `tia tree` → `plc-navi.md`, e só isso.** 39 KB / 309 linhas p/
    476 blocos + 194 tabelas + 13 UDTs, contra ~150 KB do JSON equivalente, em 4s. Depois vem verbo
    que responde pergunta (`trace`, `xref`, `explain-block`, `find --pattern`). `snapshot` (251 KB)
    e `find --kind tag` (821 KB) são volume bruto: sempre `--out-file` + grep.
  - **Telegrama de drive SINAMICS = `insert-telegram`, nunca `plug-module`.** Não é submódulo de
    catálogo: o drive object tem `TelegramComposition` própria. `list-telegrams --device X` mostra
    os drive objects e o que já está posto; o dry-run devolve `canInsert`. **G120 novo já nasce com
    `MainTelegram #1`**, então o caso real é trocar: **`--change`** (o Portal recusa apagar telegrama
    Main — a troca é in-place). Vale só para a família System (Startdrive); o G120X **GSD** carrega
    telegrama como submódulo plugado, e aí é `plug-module`.
    **Telegrama posto ainda não cria a constante `X~PROFINET_interface~Standard_telegram_20`**: ela
    só nasce quando o drive é IO device daquele controlador — dois `connect-subnet` na ordem, PLC
    (`--io-system NOME`, cria) e depois o drive (junta). Nome de IO system por PLC, senão o drive
    entra no controlador errado quando duas CPUs dividem a subnet.
  - **Rodar o programa e observar = `pwsh scripts/sim-host.ps1 -Start` e depois `sim-run`.** O host
    segura uma instância do **S7-PLCSIM Advanced** viva (`-Start`/`-Stop`/`-Status`, task
    `TiaSimHost`) e o verbo faz attach nela, baixa o programa por Openness e roda os passos do
    `--script` (`write`/`read`/`wait`/`run`/`stop`/`state`/`tags`; tag de DB vai com as aspas do
    Portal: `"\"DB GLOBAL\".AREA.EQUIP.CMD_LIGA"`). Provado no projeto-molde: download `Success`,
    41550 tags, Bool escrito e relido.
    **Instância registrada dentro do `tia.exe` morre com o processo** (o Runtime Manager sobe
    in-proc, não há serviço) — daí um host longevo separado. O **control panel da Siemens não é
    necessário**: o host sobe o Runtime Manager sozinho (medido 2026-08-17 com control panel e
    manager mortos). O host **tem que rodar na sessão 1** — da sessão 0 a API do PLCSIM tem a mesma
    parede do Openness (`SimulationRuntimeManager.Version` vazio, `RegisterInstance` = `-1,
    InvalidErrorCode`), por isso o `-Start` roteia pela task quando o shell nasce na sessão 0.
    **O PLCSIM clássico tem que estar fechado** — ele toma o mesmo canal (`-48,
    CommunicationInterfaceNotAvailable`) e sequestra o access point `PLCSIM` do S7ONLINE, onde o
    download sai `Success` com a instância Advanced vazia. Fechado o clássico, é esse mesmo access
    point que serve o Advanced: daí o default `--pc-interface PLCSIM`.
    **Instância com 0 tags = erro, não sucesso**: com download, é o clássico sequestrando o access
    point (o Portal diz `Success` e o programa foi para outro canal); com `--no-download`, é
    programa que nunca chegou lá. O verbo também devolve `programCheck` (nome do controller × PLC
    do projeto) — prova origem, não versão: assinatura de programa a API do PLCSIM não expõe.
    **O download é ~91% do verbo** (45-52 s de 49-57 s medidos): iterar observação no mesmo programa
    vai com **`--no-download`**, que pula direto pros passos. `download.ms` e `ms` saem no JSON.
    **O host desfaz só o que fez**: instância que já existia é reusada (`-5, AlreadyExists` vira
    `CreateInterface`) e continua ligada no `-Stop`; só a que ele registrou/ligou é que ele
    desliga. `-Start` com host de pé é no-op. **`-Ui`** abre o control panel junto — é a mesma
    vista do mesmo Runtime Manager, e fechar a janela não desliga nada.
  - **Objeto de dentro da tela: `list-screen-items` → `copy-screen-items` → `set-screen-items`.**
    A lista é 1 linha por objeto (150 objetos = 7,4 KB contra 798 KB do XML); **`--group`** agrega
    pelo 1º código de equipamento da tag e devolve a `region` pronta pra estampar (bbox só dos
    objetos **com tag** — fundo e rótulo pedem alargar). `copy-screen-items` copia o que está
    **inteiramente contido** na região, desloca, renumera `ID` e desduplica `ObjectName`.
    **Objeto de tela vive dentro de `Hmi.Screen.ScreenLayer`, não na `ObjectList` da tela** — colar
    no nível errado passa no XML e o Portal recusa no import (`'ScreenItems' composition … is not
    supported`); a cola vai na camada de mesmo índice. **Não há catálogo de estampas**: cada tela da
    casa tem seu dialeto (Biofiltro usa `Switch` 94x36, Gradeamento `Button` 93x39 para o mesmo
    motor), então o grupo sai sempre da tela-molde em runtime.
    **`set-screen-items` também apaga, renomeia e agrupa**: `--remove Nome`, `--rename Velho=Novo`,
    `--group NOME=x,y,w,h` (todos repetíveis, um export/import para tudo — import de tela custa
    58-123 s). Ordem fixa **set → remove → rename → group**, porque a região do grupo se confere
    contra a geometria já corrigida pelos `--set`. `--group` embrulha num `Hmi.Screen.Group` o que
    está **inteiramente contido** na região, **sem mexer em geometria** (coordenada de filho é
    absoluta no SimaticML). `--rename` para destino ocupado é erro (o Portal recusa `ObjectName`
    duplicado) — e o nome bom sai da própria tag (`Switch_18` → `BF-01-EC-01_CMD_LIGA`), o que só
    dá para fazer em objeto **com** tag; sem tag, batizar é adivinhação.
    **`--rename-from-tag` faz isso na tela inteira** e é idempotente: o que não dá vai p/
    `skippedRename` com motivo, então tag placeholder (`tag1`) aparece nominalmente — o comando que
    padroniza é o mesmo que denuncia a pendência. A lista traz **`group`** (de que
    `Hmi.Screen.Group` o objeto faz parte), que é como se confere agrupamento sem abrir o XML.
    **Não há `--align`, e é decisão**: derivar os `--set` comparando coluna contra coluna pede
    parear objeto com objeto, e par errado move o objeto errado. Coluna torta se **regenera** com
    `copy-screen-items --replace`, onde o molde é a coluna boa; `--set` é para a exceção.
  - **Tag de tela quebrada = `audit-screen`**, que hoje só aparecia no compile do HMI (e o compile não
    diz qual objeto a usa). Cruza tag do objeto × tags **da IHM**: existe, e tem código de equipamento
    (é assim que o placeholder `tag1` do editor sai nominalmente). Sem `--screen` varre toda tela do
    device — um export por tela, **~9,5 s cada** (medido: IHM_2.1, 9 telas, 591 objetos, 86 s), então
    varrer as 4 IHMs é ~12 min de banho, e iterar vai com `--screen`. Cruzar com a tag **do PLC** sai
    `skipped`: a tag de HMI clássica só expõe `Name` e o SimaticML da tabela traz só a `Connection`
    (`docs/LIMITES.md`).
  - **`plug-module --type` aceita o MLFB sem o prefixo `OrderNumber:`** e, quando `canPlug` é
    `false`, devolve **`reason`**. Sem versão, `plugAs` sai com o prefixo e o firmware sondado
    (`OrderNumber:6ES7 131-6BH00-0BA0/V1.0`). Slot é do rack: `--item Rack_0`, posição em **`--pos`**.
  - `tia doctor` = preflight dos 6 verbos antes de qualquer smoke.
- Smoke test exige TIA Portal aberto com projeto de teste — confirmar com o usuário antes.

## Antes de escrever programa de PLC: `--study`

`python scripts/tia-help.py --study "<o que se vai fazer>"` é a **primeira parada de qualquer
tarefa de engenharia** — antes de `tree`, antes de qualquer verbo de escrita. Roda **sem Portal
aberto**. Devolve, para o tema: tópicos do F1 (`--topic` lê), membros do Openness, **a biblioteca
oficial da Siemens que já resolve**, a restrição de hardware que afunda o projeto se descoberta
tarde, as regras R1–R9 aplicáveis e o verbo seguinte. Tema sem domínio casado ainda devolve o
`catalog` da plataforma — o ponto é saber que existe e onde procurar, não saber fazer tudo.

O conhecimento curado é **dados**, não código: `docs/study-map.json`. Domínio novo = mais um objeto
lá. O casamento é por palavra inteira e sem acento (`ob` não casa dentro de `robótico`,
`código de barras` casa a chave `codigo de barras`).

Guia oficial da Siemens, bibliotecas gratuitas (LGF 109479728, DriveLib 206539) e onde o padrão da
casa é deliberadamente mais estrito: `docs/GUIA-SIEMENS.md`. **Biblioteca oficial antes de código
autoral** — reescrever escala, filtro de média ou função de string é dívida sem ganho.

**O que Openness/PLCSIM NÃO fazem: `docs/LIMITES.md`.** Cada linha traz a evidência (termo sondado,
mensagem exata), a natureza (limite de API × decisão do repo × falta de DLL em `lib/` × limite de
produto/SO) e a saída. **Ler antes de sondar API que "devia existir"** — diagnóstico online, valor
de tag, RUN/STOP, criação de TO, PLCSIM clássico e as duas APIs de HMI já estão lá com medição.

## Não sabe como a API se comporta? Consulte a ajuda oficial, não deduza

`python scripts/tia-help.py --search "termo"` → busca nos **45518 tópicos** da ajuda do TIA Portal
(a mesma do F1), dos quais **1083 são de Openness**; `--topic "PKG/TOC/ID.htm"` devolve o texto
limpo. Sobe o serviço e monta o índice sozinho na 1ª vez (`--ensure`).

**Use antes de sondar a API por tentativa e erro** — nome de atributo, o que o import recusa,
diferença entre famílias de CPU, assinatura de instrução. O custo é ~1 s e uns poucos KB; o de
descobrir no braço foi metade de uma sessão. Busca casa por **AND de palavras no título** (o índice
não tem corpo): termo que só existe no texto dá 0 hits — achar o tópico plausível e ler com
`--topic`.

**Termo que só existe no corpo do tópico: `--deep`.** `python scripts/tia-help.py --deep "termo"`
ranqueia candidatos pelo título (quebrando `camelCase`), **baixa o corpo** dos `--scan` melhores
(default 40, ~6 s) e grepa lá, devolvendo trecho. O corpo lido fica em `workspace/help-cache/`, então
a segunda busca é de graça. É o que responde pergunta em prosa, que o `--search` de título zera.

**Para a API em si, `--sdk` vem antes do `--search`.** `python scripts/tia-help.py --sdk "termo"`
busca nos **31448 membros documentados** do IntelliSense XML das 14 assemblies do Openness
(`PublicAPI\V21\net48\*.xml`) e devolve `Assembly|assinatura|summary`. Duas coisas que o índice do
F1 não dá: **assinatura exata** de método/propriedade e **casamento no corpo** do texto, não só no
título. É local (sem serviço, sem rede) e responde "existe API pra isso?" em um comando —
`--sdk "insert main telegram"` acha `TelegramComposition.InsertMainTelegram(System.Int32)`, que meia
sessão de sondagem no braço não achou. Índice em `workspace/sdk-index.txt` (5,8 MB, montado na 1ª
busca ou com `--sdk-index`).

**Se o `--sdk` não achar, a assembly pode não estar em `lib/`.** O gate 3 do `init.ps1` copia as 4
que o build referencia (Base, Step7, WinCCUnified, Startdrive); a instalação tem 14. O `--sdk`
indexa as 14 direto da instalação, então ele enxerga API que o projeto ainda não compila — achou
lá e não compila aqui = acrescentar a DLL em `$dllNames` e a `<Reference>` no `Tia.Core.csproj`
(o resolver de runtime do `Program.cs` já acha qualquer `Siemens.Engineering.*` sozinho).

## Sessão 0 × sessão 1 (por que `tia` às vezes não roda direto)

`pwsh scripts/tia.ps1 <args>` é **o comando único** — resolve isso sozinho, use sempre.

Se o shell nascer na **sessão 0** do Windows (isolada de serviços, `UserInteractive=False`), TIA
Portal e desktop vivem na **sessão 1** e `TiaPortal.GetProcesses()` não enxerga processo de outra
sessão: `Attach()` devolve `"No running TIA Portal instance found"` mesmo com o portal na tela.
É fronteira do SO, não configuração; `--no-ui` não resolve (só troca o erro de modo pelo de
whitelist). O shell do agente **pode** nascer na sessão 1 (VSCode na sessão do usuário) — daí
tudo roda direto; checar com `(Get-Process -Id $PID).SessionId`.

`Invoke-Tia` (`scripts/_common.ps1`, dot-sourced por todos os macros) roteia: sessão ≠ 0 invoca
`tia.exe` direto; sessão 0 passa pela task `TiaSmokeRun` (`LogonType Interactive` = sessão 1).
Caller não vê diferença — `$LASTEXITCODE` e stdout/stderr valem nas duas rotas.
`TIA_VIA_TASK=1` força a rota da task (é como se testa esse ramo da sessão 1);
`TIA_TIMEOUT` = segundos (default 600).

Protocolo da task, se precisar na unha: `workspace/taskio/cmd.json` recebe
`{"id":"<run>","args":[...]}` (ou array cru `["doctor"]`, forma legada) →
`Start-ScheduledTask -TaskName TiaSmokeRun` → poll de `exit-<run>.txt`; saída em
`out-<run>.txt` / `err-<run>.txt` (stdout e stderr **separados**; sem `id`, os nomes são
`out.txt`/`err.txt`/`exit.txt`). Nome único por rodada é obrigatório: verbo que inicia o portal
deixa o handle do arquivo de saída herdado e aberto enquanto o portal viver.

O runner é `scripts/taskrun.ps1`. Não exige janela interativa aberta pelo user
(`scripts/smokeloop.ps1` é rota alternativa, mesmo protocolo, útil só pra ver a saída ao vivo).
O portal só morre junto com a task se tiver sido *iniciado por ela* (fica na árvore de processos);
portal aberto à mão pelo user sobrevive.

**`EngineeringSecurityException` + `The operation has timed out` = diálogo modal de autorização
esperando clique na tela, não falha.** Acontece sempre que o hash do `tia.exe` muda (todo
`rebuild.ps1`) com o Portal já aberto. O usuário clica em OK depois de o verbo já ter estourado o
timeout, então o que chega ao agente é sempre o erro. **Rodar o mesmo verbo de novo antes de
relatar falha** — a permissão já foi dada. Só insistir num diagnóstico se a 2ª tentativa repetir.

Whitelist stale = `EngineeringSecurityException`. Refazer com
`Start-ScheduledTask -TaskName TiaWhitelist` — **token elevado do usuário** (S4U + `RunLevel
Highest`), não SYSTEM, e sem UAC; `rebuild.ps1` já compara contra o hash gravado no registro e
falha alto se continuar divergente.
**Por isso a task executa uma cópia do `whitelist.ps1` em `%ProgramData%\tia-cli`, não o script
do repo**: a ACL da task protege a *ação*, não o *arquivo* que a ação roda, e o repo vive no perfil
do usuário (gravável sem admin) — apontar pra lá daria execução elevada sem UAC a qualquer processo
rodando como o usuário. `setup-tasks.ps1` faz a cópia com dono e escrita só de
Administradores/SYSTEM — dono reescreve a
própria DACL, então deixar o usuário como dono devolveria a escrita que a ACL tirou — e passa o
repo em `-Repo`; `init.ps1 -Check` reprova se a cópia divergir do original (git pull) ou o
`-Repo` apontar pra outro checkout.

## Economia de tokens

- Sem spawn de agentes por padrão (repo pequeno; navi resolve). Sem workflows.
- `/handoff` + `/clear` no fim de cada fase ou >~150k de contexto.
- Atualizar tabela de fases do PLANO ao encerrar sessão de trabalho.
