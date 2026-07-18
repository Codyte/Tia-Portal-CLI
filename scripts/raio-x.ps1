# Macro "raio-x": banho read-only do projeto → workspace/<projeto>/.
# doctor + snapshot + list-devices + list-tags + list-types + tree (plc-navi.md)
# + export-cax + xref de todos os OBs. Replica o banho manual do ETE SG em 1 comando.
# Uso: pwsh scripts/raio-x.ps1 SmokeTest_01
param([Parameter(Mandatory)][string]$Name)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'use-project.ps1') $Name
if ($LASTEXITCODE) { exit $LASTEXITCODE }

$proj = (& $exe info | ConvertFrom-Json).project
$out = Join-Path $repo "workspace\$proj"
New-Item -ItemType Directory -Force $out | Out-Null

foreach ($v in 'doctor', 'snapshot', 'list-devices', 'list-tags', 'list-types') {
    & $exe $v | Set-Content (Join-Path $out "$v.json")
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
& $exe tree --out $out | Out-Null
& $exe export-cax --out $out | Set-Content (Join-Path $out 'export-cax.json')
if ($LASTEXITCODE) { exit $LASTEXITCODE }

# ponytail: xref de TODOS os OBs (projetos reais têm poucos); filtro se doer
$obs = (& $exe list-blocks | ConvertFrom-Json) | Where-Object type -eq 'OB'
$xref = foreach ($ob in $obs) { & $exe xref --name $ob.name | ConvertFrom-Json }
$xref | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out 'xref-obs.json')
Write-Host "raio-x ok → $out"
