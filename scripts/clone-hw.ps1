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
. (Join-Path $PSScriptRoot '_common.ps1')
$use = Join-Path $PSScriptRoot 'use-project.ps1'

& $use $From
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$aml = (Invoke-Tia export-cax --out (Join-Path $script:Repo 'workspace\clone-hw') | ConvertFrom-Json).file
if (-not $aml) { Write-Error 'export-cax não retornou arquivo'; exit 1 }
Write-Host "AML exportado: $aml"

& $use $To
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if ($Apply) { Invoke-Tia import-cax --file $aml --apply } else { Invoke-Tia import-cax --file $aml }
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if ($Apply) {
    Invoke-Tia save-project
    if ($LASTEXITCODE) { Write-Host "save-project falhou (exit $LASTEXITCODE) — import aplicado mas NÃO salvo" -ForegroundColor Red }
}
exit $LASTEXITCODE
