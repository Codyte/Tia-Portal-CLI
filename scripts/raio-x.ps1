# Macro "raio-x": banho read-only do projeto → workspace/<projeto>/.
# doctor + snapshot + list-devices + list-tags + list-types + tree (plc-navi.md)
# + export-cax + xref de todos os OBs. Replica o banho manual do ETE SG em 1 comando.
# Uso: pwsh scripts/raio-x.ps1 SmokeTest_01
param([Parameter(Mandatory)][string]$Name)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

& (Join-Path $PSScriptRoot 'use-project.ps1') $Name
if ($LASTEXITCODE) { exit $LASTEXITCODE }

$proj = (Invoke-Tia info | ConvertFrom-Json).project
$out = Join-Path $script:Repo "workspace\$proj"
New-Item -ItemType Directory -Force $out | Out-Null

foreach ($v in 'doctor', 'snapshot', 'list-devices', 'list-tags', 'list-types') {
    Invoke-Tia $v | Set-Content (Join-Path $out "$v.json")
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
Invoke-Tia tree --out $out | Out-Null
Invoke-Tia export-cax --out $out | Set-Content (Join-Path $out 'export-cax.json')
if ($LASTEXITCODE) { exit $LASTEXITCODE }

# ponytail: xref de TODOS os OBs (projetos reais têm poucos); filtro se doer
$obs = (Invoke-Tia list-blocks | ConvertFrom-Json) | Where-Object type -eq 'OB'
$xref = foreach ($ob in $obs) { Invoke-Tia xref --name $ob.name | ConvertFrom-Json }
# Depth alto: xref aninha fundo e -Depth estourado trunca em SILENCIO (vira "System.Object[]")
$xref | ConvertTo-Json -Depth 32 | Set-Content (Join-Path $out 'xref-obs.json')
Write-Host "raio-x ok → $out"
