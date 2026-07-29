# Macro "new-plc": CPU nova + biblioteca instalada num comando.
# Junta add-device (MLFB do 1500 que a biblioteca exige — molde usa DisableENO, 1200 recusa o
# import) com install-lib (base + pacotes + DB GLOBAL + tags + iDBs + clock byte + compile).
# Uso: pwsh scripts/new-plc.ps1 PLC_X "1.3 Instrumentação,0 Moldes" [-Portal P] [-Apply]
#      pwsh scripts/new-plc.ps1 PLC_X            → só a CPU; sem pacote não chama o install-lib
param(
    [Parameter(Mandatory, Position = 0)][string]$Name,
    [Parameter(Position = 1)][string[]]$Package,
    # CU do S7-1515 usada em todas as medições de CPU virgem; trocar exige família 1500
    [string]$Mlfb = '6ES7 515-2AM02-0AB0/V2.9',
    [string]$Station,
    [string]$Portal,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$portalArgs = if ($Portal) { @('--portal', $Portal) } else { @() }
$add = @('add-device', '--mlfb', $Mlfb, '--name', $Name)
if ($Station) { $add += @('--station', $Station) }

Write-Host "CPU: $Name ($Mlfb)"
if (-not $Apply) {
    Invoke-Tia @add @portalArgs
    if ($Package) { & (Join-Path $PSScriptRoot 'install-lib.ps1') $Package -Plc $Name -Portal $Portal }
    Write-Host 'dry-run — repita com -Apply'
    exit 0
}

Invoke-Tia @add @portalArgs --apply
if ($LASTEXITCODE) { throw "add-device falhou para $Name" }
if ($Package) {
    & (Join-Path $PSScriptRoot 'install-lib.ps1') $Package -Plc $Name -Portal $Portal -Apply
    $rc = $LASTEXITCODE
} else {
    Write-Host 'sem pacote — CPU criada vazia; pacotes disponíveis:'
    & (Join-Path $PSScriptRoot 'install-lib.ps1') -Plc $Name -Portal $Portal
    $rc = 0
}
# CPU nova não salva se perde no fechamento do portal; install-lib já compilou antes daqui
Invoke-Tia save-project @portalArgs
exit $rc
