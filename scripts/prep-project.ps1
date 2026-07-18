# Macro "prep-project": deixa o projeto pronto pra verbos de export (achado 1:
# projeto real chega sem compilar e todo export morre). use-project → doctor →
# compile --apply → save-project.
# Uso: pwsh scripts/prep-project.ps1 SmokeTest_01
param([Parameter(Mandatory)][string]$Name)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'use-project.ps1') $Name
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $exe doctor
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $exe compile --apply
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& $exe save-project
