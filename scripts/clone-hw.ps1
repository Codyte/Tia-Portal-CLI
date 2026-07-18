# Macro "clone-hw": copia hardware (CAx/AML) do projeto origem pro destino.
# export-cax na origem → use-project destino → import-cax (dry por padrão, -Apply grava+salva).
# Fluxo validado 2026-07-18 (AML 1.7MB, 21 devices). Import CAx NÃO usa ExclusiveAccess (achado 9).
# Uso: pwsh scripts/clone-hw.ps1 Origem Destino [-Apply]
param(
    [Parameter(Mandatory)][string]$From,
    [Parameter(Mandatory)][string]$To,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
$use = Join-Path $PSScriptRoot 'use-project.ps1'

& pwsh -NoProfile -File $use $From
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$aml = (& $exe export-cax --out (Join-Path $repo 'workspace\clone-hw') | ConvertFrom-Json).file
if (-not $aml) { Write-Error 'export-cax não retornou arquivo'; exit 1 }
Write-Host "AML exportado: $aml"

& pwsh -NoProfile -File $use $To
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $exe import-cax --file $aml @($Apply ? @('--apply') : @())
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if ($Apply) { & $exe save-project }
