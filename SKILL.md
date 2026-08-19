---
name: tia
description: >-
  Dirigir o TIA Portal (Siemens) pela linha de comando via Openness — CLI `tia`, 92 verbos com
  JSON na entrada e na saída: ler projeto, exportar/importar bloco, tags, hardware, compilar,
  replicar FC de acionamento/alarme/instrumento, instalar biblioteca de blocos num PLC.
  Use sempre que a conversa envolver TIA Portal, Openness, PLC S7-1500, bloco FB/FC/OB/DB, UDT,
  tabela de tag, projeto .ap21/.al21 — e também quando o user pedir para instalar, atualizar ou
  verificar o tia-cli numa máquina. Traz o protocolo de instalação e as invariantes que, ignoradas,
  custam uma sessão inteira.
---

<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L25    tia — CLI do TIA Portal Openness -->
<!--   L27    1. Achar o CLI -->
<!--   L45    2. Instalar numa máquina nova -->
<!--   L79    3. Chamar -->
<!--   L91    4. Antes de escrever código: estude -->
<!--   L118   5. Invariantes (ignorar custa sessão) -->
<!--   L181   6. Orçamento de contexto (o CLI devolve volume que estoura sessão) -->
<!--   L191   7. Referência (ler no repo, não deduzir) -->
<!-- ======================= END NAV INDEX ======================= -->

# tia — CLI do TIA Portal Openness

## 1. Achar o CLI

Nesta ordem, pare no primeiro que responder:

```powershell
$env:TIA_CLI_HOME                                          # variável de usuário, gravada pelo init
[Environment]::GetEnvironmentVariable('TIA_CLI_HOME','User')   # shell velho não vê a de cima
Get-Command tia -ErrorAction SilentlyContinue              # shim tia.cmd no PATH
```

Achou → `$repo = ...`. Não achou → seção 2.

**A 2ª linha não é redundância.** `init.ps1` grava a variável no perfil do usuário, e processo já
rodando **não recebe** — o shell persistente do agente (e qualquer terminal aberto antes da
instalação) enxerga `$env:TIA_CLI_HOME` vazio e `tia` fora do PATH, com tudo instalado
corretamente. Ler o escopo `User` direto resolve sem reiniciar nada; chamar pelo caminho completo
(`& "$repo\scripts\tia.cmd" <verbo>`) sempre funciona.

## 2. Instalar numa máquina nova

Este repo **é** a skill: o checkout tem que ficar em `~/.claude/skills/tia`, como submódulo do
repo de skills.

```powershell
cd "$HOME\.claude\skills"
git submodule add https://github.com/Codyte/Tia-Portal-CLI.git tia   # ou git clone, se não for repo
pwsh "$HOME\.claude\skills\tia\scripts\init.ps1"
```

`init.ps1` é idempotente e faz tudo: confere os gates que só um humano resolve (grupo Windows
`Siemens TIA Openness` + logoff/logon, .NET SDK 8, TIA Portal instalado), copia as DLLs do
Openness da instalação local, registra as tasks (**1 UAC**), builda, whitelista o `tia.exe` e
põe o shim no PATH. Rodar de novo depois de `git pull` — reinstala o que mudou e não mexe no resto.

**Um checkout só.** A whitelist do Openness é gravada por caminho do exe e a task `TiaSmokeRun`
guarda o caminho absoluto do `taskrun.ps1`: dois clones brigam pela whitelist, e mover o clone
mata a rota da sessão 0 até rodar `init.ps1` de novo (ele detecta e re-registra a task).

**Verificar o que está instalado** (read-only, não escreve nada):

```powershell
pwsh "$env:TIA_CLI_HOME\scripts\init.ps1" -Check
```

Sai a lista dos **8 gates** (grupo, dotnet, Portal, `lib/`, `tia.exe`, whitelist, tasks, PATH) +
o estado vivo (lugar do checkout, sessão do shell, Portal rodando, `.al21` presente). Exit 1 se
faltar **gate** — o estado vivo é informativo e não muda o exit.

O que **não** vem no clone (gitignored, cada máquina repõe o seu): `lib/*.dll` (licença Siemens,
o `init` copia da instalação local), `library/blocks/` e `src/Tia.Lib/*.al21` (payload de projeto
de cliente — assar com `bake-lib.ps1` a partir de um projeto que já tenha a biblioteca).

## 3. Chamar

**Sempre pelo `tia.ps1`/`tia.cmd`, nunca `tia.exe` direto.** O shim roteia por sessão do Windows:
se o shell nasceu na sessão 0 (isolada de serviços), o Openness não enxerga o Portal da sessão 1 e
todo attach devolve `No running TIA Portal instance found` — é fronteira do SO, não configuração.
`Invoke-Tia` esconde isso passando pela task `TiaSmokeRun`.

```powershell
tia doctor                                    # preflight, se o shim está no PATH
pwsh "$env:TIA_CLI_HOME\scripts\tia.ps1" tree --plc "CPU1"      # de qualquer diretório
```

Pela rota da task aparece uma janela de console na sessão 1. Ela **reabre onde foi deixada e mostra
o comando que está rodando, sem configuração nenhuma** — a geometria vive em
`workspace/taskio/console-rect.txt`, escrita pelo próprio runner. Para sair disso (`hidden`,
posição fixa, sem texto) existe `workspace/console.json`; chaves e porquês no `CLAUDE.md`.

## 4. Antes de escrever código: estude

**Toda tarefa de engenharia de PLC começa aqui, antes do primeiro verbo de escrita:**

```powershell
python "$env:TIA_CLI_HOME\scripts\tia-help.py" --study "<o que se vai fazer>"
```

Devolve, para o tema: tópicos do F1 para ler (`--topic <ItemId>`), membros da API Openness,
**a biblioteca oficial da Siemens que já resolve** (LGF, DriveLib — não reescrever escala,
filtro, string), a restrição de hardware que afunda o projeto se descoberta tarde, as regras
R1–R9 que se aplicam e o verbo seguinte. Roda **sem TIA Portal aberto** — estudar não custa attach.

Tema que não casa com domínio nenhum ainda devolve o `catalog` da plataforma. O princípio é o de
engenharia: não é preciso saber fazer tudo, é preciso saber que existe, que é possível e onde
procurar.

Três coisas que só se descobrem lendo, e que já custaram projeto:

- **Trajetória coordenada (braço, pórtico, delta) exige S7-1500T.** CPU 1500 comum faz eixo isolado.
- **Posicionamento pode viver no drive** (EPos + telegrama 111 + `SINA_POS`), sem objeto tecnológico.
- **Safety não é escopo do Openness.** F-runtime group e assinatura são GUI: recusar por escrito.

Depois do `--study`, quando faltar detalhe: `--sdk` (assinatura exata, casa no corpo), `--search`
(título dos 45518 tópicos), `--deep` (baixa e grepa o corpo dos tópicos mais plausíveis; é o que
responde pergunta em prosa).

## 5. Invariantes (ignorar custa sessão)

- **Verbo de escrita é dry por padrão.** `--apply` explícito. Nunca contra projeto de produção.
- **`sim-run` para se o download falhar.** `errors > 0` ou `state != Success` = `error` de topo,
  passos não rodam. O `GoOffline()` automático só vale para alvo PLCSIM: sob `--allow-physical`,
  projeto online é erro pedindo ação humana, não desconexão silenciosa.
- **`run --script --fail-fast`** para no 1º step que falha (`aborted` conta o resto). Sem ele o
  batch isola os steps e segue — que é o certo para dry, e o errado para corrente de escrita.
- **`--force` exporta antes de apagar.** Vai para `workspace/recovery/<verbo>-<timestamp>/`, o
  caminho volta em `recoveryDir`, e export que falha **aborta o delete**. `--no-backup` apaga sem
  rede, e tem que ser dito. Não há rollback automático: o XML salvo é o que o `import-block` relê.
- **Opção que o CLI não conhece falha (exit 2) antes do attach.** Typo de escopo (`--ara` por
  `--area`) com `--apply` rodava o gerador no projeto inteiro. Verbo novo com opção nova pede
  entrada em `Program.KnownOptions` — o teste offline `Cli.KnownOptions` cobra.
- **Exit code é honesto: resultado com `error` de topo sai 1**, e o step do batch com erro embutido
  conta em `failed`. `--timeout` com `--apply` é recusado (timeout abandona escrita no meio).
- **`sim-run` só baixa em access point PLCSIM.** Interface com outro nome é recusada antes do
  download; `--allow-physical` é o opt-in explícito e existe para access point PLCSIM renomeado —
  nunca para CPU real.
- **Uma chamada por vez.** Openness é single-session; nada de paralelizar `tia` (nem via agentes).
  A 2ª chamada simultânea é recusada pelo próprio exe ("Another tia call is running").
- **Compile entre etapas: os verbos fazem sozinhos desde 2026-08-13.** Todo import deixa o alvo
  inconsistente e o Openness recusa exportar bloco inconsistente. Todo export do CLI passa por
  `Ops.ExportFresh`, que compila **só o bloco** e segue — não é mais preciso `compile --apply` do
  PLC inteiro entre etapas (eram ~20 dos 49 min da FP-06). O caro sobrou para o caso raro:
  inconsistência que vem **de fora** (UDT ou DB que o bloco usa) volta com a mensagem mandando
  `compile --apply`.
- **Mais de um Portal aberto** → todo verbo exige `--portal <projeto|PID>`.
- **Telegrama de drive SINAMICS é `insert-telegram`, não `plug-module`.** Família System
  (Startdrive) não tem TypeIdentifier de catálogo pra telegrama — o drive object tem
  `TelegramComposition` própria. Só o G120X **GSD** carrega telegrama como submódulo plugado.
  Procurar o identificador inexistente já custou várias sessões. **Drive novo já vem com
  `MainTelegram #1`**: trocar exige `--change` (telegrama Main não pode ser apagado, a troca é
  in-place).
- **Rodar o programa = `pwsh scripts/sim-host.ps1 -Start` e então `sim-run`, com o PLCSIM CLÁSSICO
  FECHADO.** O host segura a instância do S7-PLCSIM Advanced viva (o verbo só dá **attach**:
  instância registrada dentro do `tia.exe` morre com o processo, porque o Runtime Manager sobe
  in-proc e não há serviço). O control panel da Siemens **não é necessário** — o host sobe o manager
  sozinho. O host tem que viver na **sessão 1**: da sessão 0 a API do PLCSIM cai na mesma parede do
  Openness (`Version` vazio, `RegisterInstance` = `-1, InvalidErrorCode`), e por isso `-Start`
  roteia pela task `TiaSimHost`. O clássico toma o mesmo canal (`-48,
  CommunicationInterfaceNotAvailable`) e sequestra o access point `PLCSIM` do S7ONLINE: o download
  sai `Success` e a instância Advanced continua **vazia** — falso positivo que já custou meia sessão.
  Com o clássico fechado, esse mesmo access point é a rota do Advanced (`--pc-interface PLCSIM`, o
  default). **`--no-download`** roda os passos no programa que já está lá: o download é ~91% do verbo.
  `-Start` é idempotente e o host **desfaz só o que fez** (instância que já existia fica de pé no
  `-Stop`). Usuário quer **ver** a simulação: `-Start -Ui` abre o control panel junto — mesma vista
  do mesmo Runtime Manager, e fechar a janela não desliga nada.
- **Área nova = `replicate-fc --template "<pasta molde>" --target-folder "<pasta da área>"`.** Sem
  os dois, o molde é "a 1ª pasta irmã populada" e os alvos são "todas as irmãs" — área nova não tem
  irmã com blocos, e derivar o acionamento-semente à mão custou ~10 min da FP-06.
- **Escopo nos geradores.** `gen-alarm-fc --area NOME` (repetível) gera só a área pedida; sem ele,
  criar 1 área regenera todas as existentes. `replicate-instruments` aceita `IgnoreFolders`.
- **`add-call --fb` aceita o nome com ou sem o prefixo `FB `/`FC `.** `--inst` é exigido para FB e
  recusado para FC. Pino de entrada sem valor é aviso; `InOut` sem valor é erro.
- **`set-io-address` dry-run confere o `--start` contra o mapa** (`conflictCheck: occupied` +
  `conflictsWith`, ou `free (pelo mapa)`). `free` é ausência de conflito no que o mapa lê, não
  garantia: a autoridade é o `Next free address: N` do erro do Portal.
- **`run --script` cronometra**: cada step traz `ms`, o batch traz o total, e `--summary` traz
  `slowest[3]`. É a medida de onde o tempo foi, sem `Measure-Command` por fora.
- **`rebuild.ps1` muda o hash do `tia.exe`** → o Portal já aberto abre um **diálogo modal de
  autorização** na tela. Chamada pendurada com CPU ~0 = alguém precisa clicar; não é bug de API.

## 6. Orçamento de contexto (o CLI devolve volume que estoura sessão)

- **Orientação em projeto novo = `tia tree`** → `plc-navi.md` (39 KB para 476 blocos), e só isso.
  Depois vem verbo que responde pergunta: `trace`, `xref`, `explain-block`, `find --pattern`.
- **`--out-file F.json`** em qualquer verbo de leitura: o JSON completo vai pro arquivo, stdout
  devolve `{file,bytes,count,head}`. Sem isso, `find --pattern "*" --kind tag` num projeto real
  são 821 KB no contexto.
- **Nunca `list-blocks` sem filtro** (~480 blocos): use `--folder`, `--type` ou `--count`.
- **`run --script ops.json --summary`** para lote: 1 attach (~3 s) em vez de um por chamada.

## 7. Referência (ler no repo, não deduzir)

| Preciso de | Arquivo |
|---|---|
| assinatura dos 92 verbos | `$env:TIA_CLI_HOME\docs\VERBS.md` (~90 linhas, gerado do help) |
| **o que estudar antes de fazer** | `python "$env:TIA_CLI_HOME\scripts\tia-help.py" --study "tema"` — **primeira parada de qualquer tarefa de engenharia**; roda sem Portal aberto |
| guia oficial da Siemens + bibliotecas gratuitas | `$env:TIA_CLI_HOME\docs\GUIA-SIEMENS.md` (entry IDs do SIOS e onde a casa é mais estrita) |
| **o que a API NÃO faz** | `$env:TIA_CLI_HOME\docs\LIMITES.md` — limite de API × decisão do repo × DLL faltando, cada um com evidência e saída. **Ler antes de sondar API que "devia existir".** |
| domínios que o `--study` conhece | `$env:TIA_CLI_HOME\docs\study-map.json` — domínio novo é mais um objeto lá, nenhuma linha de código muda |
| decisões, fases, o que já foi medido | `$env:TIA_CLI_HOME\docs\PLANO.md` |
| regras de operação do repo | `$env:TIA_CLI_HOME\CLAUDE.md` |
| macros de fluxo | `$env:TIA_CLI_HOME\scripts\` (`prep-project`, `raio-x`, `install-lib`, `bake-lib`) |
| assinatura de uma API Openness | `python "$env:TIA_CLI_HOME\scripts\tia-help.py" --sdk "termo"` — 31448 membros do IntelliSense XML das 14 assemblies, casa no corpo do summary. **Primeira parada; local, sem serviço.** |
| como a API Openness se comporta | `python "$env:TIA_CLI_HOME\scripts\tia-help.py" --search "termo"` — 1083 tópicos da ajuda oficial do F1. **Usar antes de sondar por tentativa e erro.** |
