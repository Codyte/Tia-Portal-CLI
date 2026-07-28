# Macro "install-lib": instala pacotes da global library (.al21) num PLC.
# Pacote = pasta = 1 master copy; junto vão os blocos soltos do nível 1 (todo pacote depende deles)
# e o clock memory byte (senão Clock_1Hz não existe e o compile acusa).
# Uso: pwsh scripts/install-lib.ps1 -Plc PLC_X "1.3 Instrumentação" [-Apply]
#      pwsh scripts/install-lib.ps1 -Plc PLC_X            → lista os pacotes disponíveis
param(
    # posicional, mas nomeado: com ValueFromRemainingArguments o PS engole -Portal como "resto"
    [Parameter(Position = 0)][string[]]$Package,
    [string]$Plc = 'PLC_GEN',
    [string]$File = 'src/Tia.Lib/tia_cli/tia_cli.al21',
    [string]$Root = '1. FB Bilbiotecas',
    [string]$Portal,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$portalArgs = if ($Portal) { @('--portal', $Portal) } else { @() }
$lib = Invoke-Tia list-library --file $File @portalArgs | ConvertFrom-Json
$copies = $lib.masterCopies | Where-Object { $_.folder -eq $Root }
$packages = $copies | Where-Object { $_.contentType -like '*PlcBlockUserGroup' } | Select-Object -ExpandProperty name
$base     = $copies | Where-Object { $_.contentType -notlike '*PlcBlockUserGroup' } | Select-Object -ExpandProperty name

if (-not $Package) {
    Write-Host "pacotes em $($lib.library):"; $packages | ForEach-Object { Write-Host "  $_" }
    Write-Host "base (vai junto sempre): $($base -join ', ')"
    exit 0
}
# "a,b" num token só (é o que chega quando o caller é bash/cmd) vale como lista
$Package = $Package -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
$unknown = $Package | Where-Object { $_ -notin $packages }
if ($unknown) { throw "pacote não existe na library: $($unknown -join ', ') — disponíveis: $($packages -join ', ')" }

# ponytail: "já instalado" = existe bloco/pasta com esse nome. Não compara versão nem conteúdo;
# quando a library ganhar types versionados, trocar por comparação de versão.
$have = (Invoke-Tia list-blocks --plc $Plc --folder $Root @portalArgs | ConvertFrom-Json).blocks
$haveNames = @($have.name)
$haveFolders = @($have.folder | Sort-Object -Unique)

$todo = @()
$todo += $base    | Where-Object { $_ -notin $haveNames }
$todo += $Package | Where-Object { "$Root/$_" -notin $haveFolders }
$skipped = $base.Count + $Package.Count - $todo.Count

$ops = @(, @('set-memory-bytes', '--device', $Plc, '--system', '1', '--clock', '0', '--apply'))
foreach ($n in $todo) { $ops += , @('import-master-copy', '--plc', $Plc, '--file', $File,
    '--name', $n, '--folder', $Root, '--apply') }
$ops += , @('compile', '--plc', $Plc, '--apply', '--errors')

Write-Host "instalar em ${Plc}: $($todo -join ', ')"
if ($skipped) { Write-Host "já presentes (pulados): $skipped" }
if (-not $Apply) { Write-Host 'dry-run — repita com -Apply'; exit 0 }

$script = Join-Path $script:Repo 'workspace\install-lib.json'
[IO.File]::WriteAllText($script, ($ops | ConvertTo-Json -Depth 3 -Compress), [Text.UTF8Encoding]::new($false))
$out = Invoke-Tia run --script $script @portalArgs | ConvertFrom-Json
$compile = ($out.results | Where-Object { $_.verb -eq 'compile' }).result
Write-Host "compile: $($compile.state) — $($compile.errors) erro(s)"
$compile.list | Where-Object { $_.message -notlike 'Compiling finished*' } |
    ForEach-Object { Write-Host "  $($_.count) | $($_.where) | $($_.message)" }
exit $(if ($compile.errors) { 1 } else { 0 })
