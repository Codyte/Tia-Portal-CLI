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
- `Scripts_Siemens/FINAIS/` = referência read-only. `Scripts_Siemens/OLD/` = não tocar.
- Verbos de escrita: dry-run por padrão, `--apply` explícito.
- **Compile entre etapas**: todo import deixa o alvo (e quem o referencia) inconsistente, e o
  Openness recusa exportar bloco inconsistente. `clone`, `diff-block`, `explain-block` e os 4
  geradores exportam por baixo — sem `compile --apply` antes, quebram com essa mensagem.
  **Exceção: os verbos que editam bloco por XML** (`add`/`edit`/`delete-db-member`, `add-call`,
  `delete-network`, `set-retain`) **compilam e conferem sozinhos** — importam com `Override`,
  compilam o alvo e re-exportam para provar que o patch entrou. Sem essa prova, duas escritas
  seguidas no mesmo bloco (mesmo `run --script`) faziam a segunda ler um export defasado e a
  primeira sumia com `ok: true` (FP-03, tropeço 6).
- Nunca rodar `tia` em paralelo (Openness single-session).
- Nunca commitar `Siemens.Engineering.dll`.
- Testes só contra projeto TIA de teste, nunca produção.

## Build / run (a partir da F1)

- Solução em `src/`, target net48 x64. Binário oficial = Debug (`src\Tia.Cli\bin\Debug\net48\tia.exe`).
- **Máquina nova: `pwsh scripts/init.ps1`** = gates (grupo `Siemens TIA Openness`, .NET SDK,
  `lib/*.dll` copiadas da instalação local do Portal) + tasks (1 UAC) + rebuild + shim `tia` no
  PATH/`TIA_CLI_HOME`. Idempotente — re-rodar depois de `git pull`. **`-Check`** = relatório
  read-only dos 9 pontos (exit 1 se faltar algo).
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
    Vale em qualquer verbo que receba caminho de pasta (a regra é do `Ops.SplitPath`), inclusive o
    `--folder` do `list-blocks` — que até a FP-04 ignorava o escape e devolvia `count: 0` com
    `ok: true`, falso-negativo silencioso. Sem o escape,
    longest-match só resolve nome com `/` que **já existe** — na criação, `1. I/OS` virava duas pastas.
    `create-folder --path` é repetível: árvore inteira num attach, caminho que falha vira
    `{path, error}` e os outros seguem.
    **Dentro de `run --script` escreva `\\/`**: `\/` é escape válido de JSON e o parser o come antes
    de o argumento chegar no CLI — o verbo recebe `/` cru e cai no longest-match (que só acha pasta
    que já existe). Na linha de comando, `\/` direto.
  - **`list-io-map [--device X] [--io Input|Output]`** = todo endereço de I/O do projeto + próximo
    byte livre por tipo. Varre item **e** descendentes, que é onde os `Address` moram, **e os
    telegramas dos drive objects SINAMICS** — telegrama não vive em `DeviceItem.Addresses` e sim em
    `Telegram.Addresses` (outra composição, assembly Startdrive), que é por que `--device <drive>`
    devolvia `{addresses: 0}` até 2026-08-11 (FP-04, T7). Sem essa varredura o `nextFreeByte` mentia:
    no projeto-molde real ele dizia `Input: 156` com 34 drives ocupando de `%IB256` pra cima
    (com a varredura: `Input: 664`, `Output: 392`, 45 → 113 endereços). O mesmo endereço sai por
    drive em `list-telegrams --device X`. Quem precisa do telegrama **no programa** continua usando o
    `HWID` da constante `<drive>~PROFINET_interface~Standard_telegram_20`, não o endereço.
    **`nextFreeByte` é piso, não garantia** — sai só do que o mapa leu, e o mapa não lê os
    `unassigned`. Na FP-05 ele disse `Input: 664` e o Portal recusou com `Next free address: 1062`.
    Desde 2026-08-12 o verbo declara isso: `nextFreeByteExact: false` + `nextFreeByteNote` quando há
    `unassigned` ou filtro, e com `--device` o campo se chama **`nextFreeByteInDevice`** (é o próximo
    livre daquele device, não do projeto). A autoridade é o `Next free address: N` do erro do Portal.
  - **`audit` são 10 checks** e o R2 **exporta a DB global** para `--out` (só o export mostra o
    datatype dos membros) — não é mais 100% read-only. Check que não pode rodar devolve `skipped`
    com o motivo e **não reprova** o projeto. `--db "DB GLOBAL"` nomeia a DB quando a heurística
    (`GlobalDB` com "global" no nome) não acha.
    **`scanned` diz o tamanho da população** (`folders`, `blocks`, `callBlocks`, `tagTables`) — é
    como se distingue check conforme de check cego: `callBlocks: 0` reprova o R8 por vazio, não por
    conformidade. No projeto-molde real: 96 pastas, 475 blocos, 46 blocos de chamada, 195 tabelas,
    10 checks verdes.
  - **`--folder` de import é sempre o caminho completo.** `import-block`/`import-tags`/`import-source`
    criam a árvore que faltar **a partir da raiz**: caminho parcial (`5.2 Totalizadores` em vez de
    `5. Instrumentação / Atuadores/5.2 Totalizadores`) cria uma pasta paralela homônima, e o gerador
    seguinte morre em colisão de nome. O dry-run avisa em **`folderAction: create|reuse`** — `create`
    num caminho que você esperava existir é o sinal de que o caminho está errado.
  - **`move-block --name X | --pattern P* --folder A/B [--apply]`** — o Openness não move bloco; o
    verbo faz `export` (de todos primeiro) → `delete` → `import --folder`. Fazer isso na mão custa
    3 chamadas por bloco e falha se a ordem inverter.
  - **Chamada em LAD (R8) = `add-call`, nunca FlgNet na mão.** A ordem que funciona:
    `list-interface --folder "1. FB Bibliotecas"` (todas as assinaturas numa chamada; é o que se lê
    antes de escrever a chamada) → `clone` do molde → `delete-network --index N` para as redes que
    não servem → `add-call --block X --fb "FB Y|FC Y" [--inst iDB] --param P=<tag|DB.caminho.membro|const>`
    (rede LAD com EN no powerrail; pino de entrada sem valor é erro). `--inst` é **exigido para FB e
    recusado para FC** — o `CHAMADA_*` do padrão é sequência de chamadas de FC. **FB sem pino
    nenhum é chamável** (só o `<Instance>` no `Call`) desde 2026-08-12: era erro
    `has no Input/Output/InOut` e obrigava a inventar pino em bloco de área que só usa tag global e
    estática retentiva (FP-05, T5). Só o pino **com
    valor** entra declarado: o Portal recusa import de `<Parameter>` sem fio, então Output que você
    não usa fica de fora. Constante sai tipada pelo pino (`TRUE` num Bool vira
    `LiteralConstant`+`ConstantType`; `T#5S` se basta). `--after 0` põe na frente, omitido põe no
    fim. Rede **com condição em série** continua sendo clone de um molde que já a tenha.
  - **`clone --replace` é troca de texto no XML exportado, não caminho de membro.** Caminho de DB
    lá é cadeia de `<Component Name="…"/>`: `A.B.C=X.Y` casa zero vezes — trocar **um componente por
    vez**, e a estrutura de destino tem que ter a **mesma profundidade** da origem, senão o clone
    compila com dezenas de erros de membro inexistente (FP-04, T2: undo de 26 steps).
  - **`clone --with-instances`** cria os iDBs que o XML clonado passa a referenciar (sem eles o
    compile morre em `Missing instance DB` e o nome do iDB tem que ser deduzido de cabeça).
  - **Retentividade se declara no FB: `set-retain --block <FB> --member M [--off]`.** O Openness
    recusa `Remanence` em iDB (`The attribute 'Remanence' cannot be set`) e o `import-source` não
    expressa retentividade — sem o verbo, horímetro retentivo virava export + patch + import na mão.
  - **`create-instance-db --of` aceita nome aproximado**: acento, caixa, underscore e espaço duplo
    não contam (`FB FILTRO DE AMOSTRAGEM  ANALÍTICA`). Casou um só, resolve e devolve `resolvedFrom`;
    casou mais de um, falha listando.
  - **`run --script ops.json --summary`** = `{steps,failed,errors[]}` em vez do resultado de cada
    step (98 steps × JSON completo é dump de contexto).
  - **Saída grande já vem cortada por padrão.** Acima de 60 000 chars (`TIA_MAX_STDOUT`) qualquer
    verbo derrama sozinho em `workspace/auto-<verbo>.json` e o stdout recebe
    `{file,bytes,count,head,autoSpill}` — `find --pattern "*" --kind tag` (821 KB) não cai mais no
    contexto por esquecimento. Depois é grep no arquivo.
    - **`--out-file F.json`** = mesma coisa, no caminho que você escolher (vale pra qualquer verbo).
    - **`--full`** = desliga o corte e dumpa tudo no stdout. É o que script de PowerShell que faz
      `ConvertFrom-Json` precisa: sem ele o pipe recebe o stub, que parseia sem erro e devolve
      `$null`. Os macros do repo já passam.
    - O teto fica acima do `tree` (39 KB, leitura de orientação legítima) e abaixo do `snapshot`
      (251 KB). `TIA_MAX_STDOUT` **não atravessa a rota da task** (sessão 0): lá vale o default.
  - **Orientação num projeto novo = `tia tree` → `plc-navi.md`, e só isso.** Outline do PLC inteiro
    (blocos + tabelas de tag + UDTs) agrupado por pasta: 39 KB / 309 linhas p/ 476 blocos + 194
    tabelas + 13 UDTs, contra ~150 KB do JSON equivalente, em 4s. Depois vem verbo que responde
    pergunta (`trace`, `xref`, `explain-block`, `find --pattern`). `snapshot` (251 KB) e
    `find --kind tag` (821 KB) são volume bruto: sempre `--out-file` + grep, nunca leitura direta.
  - **Telegrama de drive SINAMICS = `insert-telegram`, nunca `plug-module`.** Não é submódulo de
    catálogo: o drive object tem `TelegramComposition` própria, então não existe TypeIdentifier de
    "Standard telegram 20" pra procurar. `list-telegrams --device X` mostra os drive objects e o
    que já está posto; o dry-run devolve `canInsert`. **G120 novo já nasce com `MainTelegram #1`**,
    então o caso real é trocar: `--change` (`conflict (pass --change to replace)` sem ele, e o
    Portal recusa apagar telegrama Main — a troca é in-place). Vale só pra família System (Startdrive) — o
    G120X **GSD** carrega telegrama como submódulo plugado de verdade, e aí é `plug-module`.
    **Telegrama posto ainda não cria a constante `X~PROFINET_interface~Standard_telegram_20`**: ela
    só nasce quando o drive é IO device daquele controlador — dois `connect-subnet` na ordem, PLC
    (`--io-system NOME`, cria) e depois o drive (junta). Nome de IO system por PLC, senão o drive
    entra no controlador errado quando duas CPUs dividem a subnet.
  - `tia doctor` = preflight dos 6 verbos antes de qualquer smoke.
- Smoke test exige TIA Portal aberto com projeto de teste — confirmar com o usuário antes.

## Não sabe como a API se comporta? Consulte a ajuda oficial, não deduza

`python scripts/tia-help.py --search "termo"` → busca nos **45518 tópicos** da ajuda do TIA Portal
(a mesma do F1), dos quais **1083 são de Openness**; `--topic "PKG/TOC/ID.htm"` devolve o texto
limpo. Sobe o serviço e monta o índice sozinho na 1ª vez (`--ensure`).

**Use antes de sondar a API por tentativa e erro** — nome de atributo, o que o import recusa,
diferença entre famílias de CPU, assinatura de instrução. O custo é ~1 s e uns poucos KB; o de
descobrir no braço foi metade de uma sessão. Busca casa por **AND de palavras no título** (o índice
não tem corpo): termo que só existe no texto dá 0 hits — achar o tópico plausível e ler com
`--topic`.

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

Whitelist stale = `EngineeringSecurityException`. Refazer com
`Start-ScheduledTask -TaskName TiaWhitelist` (SYSTEM, sem UAC); `rebuild.ps1` já compara contra
o hash gravado no registro e falha alto se continuar divergente.

## Economia de tokens

- Sem spawn de agentes por padrão (repo pequeno; navi resolve). Sem workflows.
- `/handoff` + `/clear` no fim de cada fase ou >~150k de contexto.
- Atualizar tabela de fases do PLANO ao encerrar sessão de trabalho.
