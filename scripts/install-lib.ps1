# Macro "install-lib": instala pacotes da global library (.al21) num PLC.
# Pacote = pasta = 1 master copy; junto vão os blocos soltos do nível 1 (todo pacote depende deles)
# e o clock memory byte (senão Clock_1Hz não existe e o compile acusa).
# library/packages.json diz o que o import sozinho não traz: dependência entre pacotes, ramo do
# DB GLOBAL, tabelas de tag e iDBs de molde — tudo isso entra na mesma rodada.
# Uso: pwsh scripts/install-lib.ps1 -Plc PLC_X "1.3 Instrumentação" [-Apply]
#      pwsh scripts/install-lib.ps1 -Plc PLC_X            → lista os pacotes disponíveis
param(
    # posicional, mas nomeado: com ValueFromRemainingArguments o PS engole -Portal como "resto"
    [Parameter(Position = 0)][string[]]$Package,
    [string]$Plc = 'PLC_GEN',
    [string]$File,   # default = a única .al21 sob src/Tia.Lib (Resolve-LibFile)
    [string]$Root = '1. FB Bibliotecas',
    [string]$Portal,
    # -Update reinstala o que já está lá (import-master-copy --force apaga e recria). Sem isto,
    # "já instalado" = existe bloco com esse nome, e corrigir uma FC na library nunca chegava no PLC.
    # Cuidado: apagar bloco chamado quebra o vínculo chamada↔iDB — o chamador precisa ser
    # reimportado depois (em CPU virgem não existe o problema).
    [switch]$Update,
    # nome do IO system PROFINET a usar/criar; default = o declarado no packages.json
    [string]$IoSystem,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$portalArgs = if ($Portal) { @('--portal', $Portal) } else { @() }
# .al21 e library/blocks/ sao gitignored (artefato de build / payload de cliente): clone limpo nao
# tem nenhum dos dois. Sem isto o erro sai como ConvertFrom-Json vazio, que nao diz o que fazer.
if (-not $File) { $File = Resolve-LibFile }
elseif (-not (Test-Path (Join-Path $script:Repo $File))) {
    throw "global library ausente: $File — assar a partir de um PLC que ja tenha a arvore: pwsh scripts/bake-lib.ps1 -Plc <PLC> -Apply"
}
# --full em todo consumidor de ConvertFrom-Json: sem ele a saida grande derrama pro arquivo e o
# pipe recebe o stub {file,bytes,head}, que parseia sem erro e devolve $null
$lib = Invoke-Tia list-library --file $File --full @portalArgs | ConvertFrom-Json
$packages = $lib.masterCopies | Where-Object { $_.contentType -like '*PlcBlockUserGroup' } |
    Select-Object -ExpandProperty name
$base = $lib.masterCopies |
    Where-Object { $_.folder -eq $Root -and $_.contentType -notlike '*PlcBlockUserGroup' } |
    Select-Object -ExpandProperty name

# list-library que falha (Portal pedindo autorização, library travada) devolve {error} e o pipe
# vira lista vazia — sem isto o erro sai como "pacote não existe na library", que aponta pro lado errado.
if (-not $packages) { throw "list-library não devolveu pacote nenhum de ${File} — rode 'tia list-library --file $File' e veja o erro real (autorização Openness pendente?)" }

$meta = @{}
$metaFile = Join-Path $script:Repo 'library/packages.json'
if (Test-Path $metaFile) {
    (Get-Content $metaFile -Raw -Encoding utf8 | ConvertFrom-Json).PSObject.Properties |
        Where-Object Name -notlike '_*' | ForEach-Object { $meta[$_.Name] = $_.Value }
}
function Get-Target($name) {
    if ($meta.ContainsKey($name) -and $null -ne $meta[$name].target) { $meta[$name].target } else { $Root }
}

if (-not $Package) {
    Write-Host "pacotes em $($lib.library):"; $packages | ForEach-Object { Write-Host "  $_" }
    Write-Host "base (vai junto sempre): $($base -join ', ')"
    exit 0
}
# "a,b" num token só (é o que chega quando o caller é bash/cmd) vale como lista
$Package = $Package -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
$unknown = $Package | Where-Object { $_ -notin $packages }
if ($unknown) { throw "pacote não existe na library: $($unknown -join ', ') — disponíveis: $($packages -join ', ')" }

# dependência declarada em packages.json (pacote sem entrada = sem dependência)
$want = [System.Collections.Generic.List[string]]::new()
function Add-Package($name) {
    if ($want -contains $name) { return }
    if ($meta.ContainsKey($name)) { foreach ($r in @($meta[$name].requires)) { Add-Package $r } }
    $want.Add($name)
}
foreach ($p in $Package) { Add-Package $p }

# ponytail: "já instalado" = existe bloco/pasta com esse nome. Não compara versão nem conteúdo;
# quando a library ganhar types versionados, trocar por comparação de versão.
$seen = @{}
function Get-Existing($folder) {
    if ($seen.ContainsKey($folder)) { return $seen[$folder] }
    # pasta ausente em CPU virgem: list-blocks levanta, e isso só quer dizer "nada instalado".
    # `--folder ""` também levanta — e pacote de target vazio ("0 Moldes") caía sempre no $todo,
    # reimportando sem --force: o Portal batiza "0 Moldes_1" em silêncio (medido 2026-08-07).
    $fargs = if ($folder) { @('--folder', $folder) } else { @() }
    $out = Invoke-Tia list-blocks --plc $Plc @fargs --full @portalArgs 2>$null
    # com --folder o verbo devolve {count,blocks}; na raiz devolve array cru
    $json = if ($LASTEXITCODE -eq 0) { $out | ConvertFrom-Json } else { @() }
    # array cru: `.blocks` num array é enumeração de membro e devolve @(), não $null — testar o
    # tipo, senão a raiz sempre parecia vazia e o pacote de target vazio era reimportado
    $blocks = if ($json -is [System.Array]) { $json } else { $json.blocks }
    $seen[$folder] = @{ names = @($blocks.name); folders = @($blocks.folder) }
    $seen[$folder]
}

$have = Get-Existing $Root
$todo = @()
$todo += $base | Where-Object { $Update -or $_ -notin $have.names }
foreach ($p in $want) {
    $t = Get-Target $p
    $path = if ($t) { "$t/$p" } else { $p }
    if ($Update -or $path -notin (Get-Existing $t).folders) { $todo += $p }
}
$skipped = $base.Count + $want.Count - $todo.Count

# extras dos pacotes escolhidos: ramos do DB GLOBAL, tabelas de tag, iDBs de molde
# @(...) em volta de tudo: com 1 item só o pipe devolve escalar e o += vira concatenação de string
$fragments = @($want | ForEach-Object { if ($meta.ContainsKey($_)) { $meta[$_].db } } |
    Where-Object { $_ } | Sort-Object -Unique)
$tags = @($want | ForEach-Object { if ($meta.ContainsKey($_)) { $meta[$_].tags } } | Where-Object { $_ })
$types = @($want | ForEach-Object { if ($meta.ContainsKey($_)) { $meta[$_].types } } | Where-Object { $_ })
$instances = @($want | ForEach-Object { if ($meta.ContainsKey($_)) { $meta[$_].instances } }) | Where-Object { $_ }
$devices = @($want | ForEach-Object { if ($meta.ContainsKey($_)) { $meta[$_].devices } } | Where-Object { $_ })
# Device já presente não é recriado (nem com -Update: apagar hardware derruba a rede; -Update existe
# pra corrigir bloco da library). O par connect-subnet vai mesmo assim — o drive pode existir ligado
# em OUTRO controlador, e a constante de telegrama é por controlador. connect-subnet é idempotente.
$haveDev = if ($devices) { @((Invoke-Tia list-devices @portalArgs | ConvertFrom-Json).device) } else { @() }

Write-Host "$(if ($Update) { 'atualizar' } else { 'instalar' }) em ${Plc}: $($todo -join ', ')"
if ($Update) { Write-Host 'MODO UPDATE: o que já existe é apagado e recriado (chamador de bloco apagado fica inconsistente — reimportar depois)' }
if ($skipped) { Write-Host "já presentes (pulados): $skipped" }
if ($fragments) { Write-Host "DB GLOBAL: 00-core + $($fragments -join ', ')" }
if ($instances) { Write-Host "iDBs de molde: $($instances.Count)" }
if ($devices) {
    $novos = @($devices | Where-Object { $_.name -notin $haveDev })
    Write-Host "hardware: $(($devices.name) -join ', ') — a criar: $(if ($novos) { ($novos.name) -join ', ' } else { 'nenhum (só religar no IO system)' })"
}
if (-not $Apply) { Write-Host 'dry-run — repita com -Apply'; exit 0 }

$ops = @(, @('set-memory-bytes', '--device', $Plc, '--system', '1', '--clock', '0', '--apply'))
# Hardware que o molde exige (bloco "devices" do packages.json). Vem ANTES dos blocos: a constante
# <drive>~PROFINET_interface~Standard_telegram_20 que o molde referencia só nasce quando o drive é
# IO device DESTE controlador, e o primeiro compile já cobra. Ordem provada (2026-08-07):
# add-device → insert-telegram --change (G120 novo já vem com o Main #1) → connect-subnet do PLC
# (cria o IO system) → connect-subnet do drive (junta nele).
foreach ($d in $devices) {
    if ($d.name -notin $haveDev) {
        $op = @('add-device', '--mlfb', $d.mlfb, '--name', $d.name)
        if ($d.group) { $op += @('--group', $d.group) }
        $ops += , ($op + '--apply')
        if ($d.telegram) {
            $op = @('insert-telegram', '--device', $d.name, '--number', "$($d.telegram.number)")
            if ($d.telegram.change) { $op += '--change' }
            $ops += , ($op + '--apply')
        }
    }
    if ($d.subnet) {
        # nome do IO system é por PLC: com um nome compartilhado, a CPU que chega depois acha o IO
        # system da outra e o drive vira IO device do controlador errado (medido 2026-08-07 —
        # a constante ..~Standard_telegram_20 some sem ninguém falhar). Default = sufixo do PLC.
        $io = if ($IoSystem) { $IoSystem }
              elseif ($d.ioSystem) { $d.ioSystem }
              else { "PROFINET IO-System_$Plc" }
        $sub = @('--subnet', $d.subnet) + $(if ($io) { @('--io-system', $io) } else { @() })
        $ops += , (@('connect-subnet', '--device', $Plc) + $sub + '--apply')
        $ops += , (@('connect-subnet', '--device', $d.name) + $sub + '--apply')
    }
}
foreach ($n in $todo) {
    $t = if ($n -in $base) { $Root } else { Get-Target $n }
    $op = @('import-master-copy', '--plc', $Plc, '--file', $File, '--name', $n)
    if ($t) { $op += @('--folder', $t) }
    if ($Update) { $op += '--force' }
    $ops += , ($op + '--apply')
}
# import deixa o alvo (e quem o referencia) inconsistente: compilar antes de gerar iDB em cima
$ops += , @('compile', '--plc', $Plc, '--apply')
if ($fragments) {
    & (Join-Path $PSScriptRoot 'compose-db.ps1') ($fragments -join ',') | Write-Host
    # UDT que o DB usa sai do próprio SCL (': "X"') — mapa a mais seria só cópia do fragmento
    $scl = Get-Content (Join-Path $script:Repo 'workspace/db-global.scl') -Raw -Encoding utf8
    $types += @([regex]::Matches($scl, ':\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
}
# UDT e tabela de tag saem da própria .al21 (pasta 'extras', assada pelo `bake-lib -MoldsOnly`):
# XML solto em library/ é payload gitignored, então clone limpo não teria o que importar.
# `--name "extras/X"` com pasta explícita: FindMasterCopy recusa nome homônimo em duas pastas.
$force = if ($Update) { @('--force') } else { @() }
$existing = @((Invoke-Tia list-types --plc $Plc --full @portalArgs | ConvertFrom-Json).name)   # array cru
foreach ($t in ($types | Sort-Object -Unique | Where-Object { $Update -or $_ -notin $existing })) {
    $ops += , (@('import-master-copy', '--plc', $Plc, '--file', $File, '--name', "extras/$t") + $force + '--apply')
}
if ($fragments) { $ops += , @('import-source', '--plc', $Plc, '--file', 'workspace/db-global.scl', '--apply') }
# CreateFrom recusa tabela de nome já existente (só --force apaga antes) — sem este filtro,
# reinstalar deixava de ser no-op.
$haveTables = @((Invoke-Tia list-tags --plc $Plc --full @portalArgs | ConvertFrom-Json).table)
foreach ($t in $tags) {
    $name = [IO.Path]::GetFileNameWithoutExtension($t.file)
    if (-not $Update -and $name -in $haveTables) { continue }
    $ops += , (@('import-master-copy', '--plc', $Plc, '--file', $File, '--name', "extras/$name",
        '--folder', $t.folder) + $force + '--apply')
}
foreach ($i in $instances) {
    $ops += , @('create-instance-db', '--plc', $Plc, '--name', $i.name, '--of', $i.of,
        '--folder', $i.folder, '--apply')
}
$ops += , @('compile', '--plc', $Plc, '--apply', '--errors')

$script = Join-Path $script:Repo 'workspace\install-lib.json'
[IO.File]::WriteAllText($script, ($ops | ConvertTo-Json -Depth 3 -Compress), [Text.UTF8Encoding]::new($false))
$out = Invoke-Tia run --script $script --full @portalArgs | ConvertFrom-Json
$compile = @($out.results | Where-Object { $_.verb -eq 'compile' })[-1].result
foreach ($r in $out.results | Where-Object { -not $_.ok }) { Write-Host "FALHOU $($r.verb): $($r.error)" }
Write-Host "compile: $($compile.state) — $($compile.errors) erro(s)"
$compile.list | Where-Object { $_.message -notlike 'Compiling finished*' } |
    ForEach-Object { Write-Host "  $($_.count) | $($_.where) | $($_.message)" }
exit $(if ($compile.errors) { 1 } else { 0 })
