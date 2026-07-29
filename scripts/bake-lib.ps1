# Macro "bake-lib": grava a biblioteca do PLC numa global library (.al21) como master copies.
# Pacote = pasta: cada subpasta de $Root vira 1 master copy (com suas subpastas); cada bloco
# solto na raiz de $Root vira 1 master copy avulso (é o que todo pacote depende — instalar sempre).
# -Prune apaga da .al21 o que nao existe mais no PLC (master copy renomeado vira orfao no rebake).
# Opt-in porque a .al21 pode ter pacote assado de outro PLC — bake so enxerga o -Plc desta rodada.
# Uso: pwsh scripts/bake-lib.ps1 [-Plc PLC_GEN] [-Portal Project1] [-Prune] [-Apply]
param(
    [string]$Plc = 'PLC_GEN',
    [string]$File,   # default = a única .al21 sob src/Tia.Lib (Resolve-LibFile)
    [string]$Root = '1. FB Bilbiotecas',
    [string]$Portal,
    [switch]$Prune,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$portalArgs = if ($Portal) { @('--portal', $Portal) } else { @() }
if (-not $File) { $File = Resolve-LibFile }
$blocks = (Invoke-Tia list-blocks --plc $Plc --folder $Root @portalArgs | ConvertFrom-Json).blocks
if (-not $blocks) { throw "nenhum bloco em '$Root' (plc $Plc)" }

# subpasta direta de $Root = pacote; bloco com folder == $Root = dependência comum
$packages = $blocks.folder | Where-Object { $_ -ne $Root } |
    ForEach-Object { ($_ -replace [regex]::Escape("$Root/"), '') -split '/' | Select-Object -First 1 } |
    Sort-Object -Unique
$loose = $blocks | Where-Object { $_.folder -eq $Root } | Select-Object -ExpandProperty name

$ops = @()
foreach ($p in $packages) { $ops += , @('add-master-copy', '--plc', $Plc, '--file', $File,
    '--folder', "$Root/$p", '--lib-folder', $Root, '--apply') }
foreach ($b in $loose)    { $ops += , @('add-master-copy', '--plc', $Plc, '--file', $File,
    '--name', $b, '--lib-folder', $Root, '--apply') }

$orphans = @()
if ($Prune -and (Test-Path (Join-Path $script:Repo $File))) {
    $want = @($packages) + @($loose)
    $orphans = @((Invoke-Tia list-library --file $File @portalArgs | ConvertFrom-Json).masterCopies |
        Where-Object { $_.folder -eq $Root -and $_.name -notin $want } |
        Select-Object -ExpandProperty name)
    foreach ($o in $orphans) { $ops += , @('delete-master-copy', '--file', $File, '--name', $o, '--apply') }
}

Write-Host "pacotes: $($packages -join ', ')"
Write-Host "blocos soltos (nível 1): $($loose -join ', ')"
if ($orphans) { Write-Host "órfãos a apagar da library: $($orphans -join ', ')" }
if (-not $Apply) { Write-Host "dry-run — repita com -Apply para gravar em $File"; exit 0 }

$script = Join-Path $script:Repo 'workspace\bake-lib.json'
[IO.File]::WriteAllText($script, ($ops | ConvertTo-Json -Depth 3 -Compress), [Text.UTF8Encoding]::new($false))
Invoke-Tia run --script $script --summary @portalArgs
exit $LASTEXITCODE
