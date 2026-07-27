# Macro "prep-project": deixa o projeto pronto pra verbos de export (achado 1:
# projeto real chega sem compilar e todo export morre). use-project → doctor →
# compile --apply → save-project.
# Dry por padrão (só use-project + doctor); -Apply compila e SALVA no projeto.
# Uso: pwsh scripts/prep-project.ps1 SmokeTest_01 [-Apply]
param([Parameter(Mandatory)][string]$Name, [switch]$Apply)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

& (Join-Path $PSScriptRoot 'use-project.ps1') $Name
if ($LASTEXITCODE) { exit $LASTEXITCODE }
Invoke-Tia doctor
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if (-not $Apply) { Write-Host 'dry: doctor ok. -Apply para compile --apply + save-project.'; exit 0 }
Invoke-Tia compile --apply
if ($LASTEXITCODE) { exit $LASTEXITCODE }
Invoke-Tia save-project
exit $LASTEXITCODE
