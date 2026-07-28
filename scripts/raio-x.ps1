# Macro "raio-x": banho read-only do projeto → workspace/<projeto>/.
# doctor + snapshot + list-devices + list-tags + list-types + tree (plc-navi.md)
# + export-cax + xref de todos os OBs. Replica o banho manual do ETE SG em 1 comando.
# Uso: pwsh scripts/raio-x.ps1 SmokeTest_01
#
# Tudo passa por `tia run --script` (2 attaches: um pro banho, outro pros xrefs), porque cada
# chamada solta custa ~7 s de attach. Cada step escreve com --out-file: o JSON vai direto pro
# disco, sem passar por ConvertTo-Json (que truncava xref em silêncio no -Depth estourado).
param([Parameter(Mandatory)][string]$Name, [string]$Portal, [string]$Plc)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

# -Portal <projeto|PID>: projeto ja aberto naquele Portal (obrigatorio com mais de um aberto);
# sem ele, use-project abre/garante o projeto pedido.
$sel = if ($Portal) { @('--portal', $Portal) } else { @() }
if (-not $Portal) {
    & (Join-Path $PSScriptRoot 'use-project.ps1') $Name
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

$proj = (Invoke-Tia info @sel | ConvertFrom-Json).project
$out = Join-Path $script:Repo "workspace\$proj"
New-Item -ItemType Directory -Force $out | Out-Null
$script = Join-Path $out '_raio-x.steps.json'

# --plc do `run` NAO desce pros steps: cada step carrega o seu (projeto com varios PLCs)
$plcArgs = if ($Plc) { @('--plc', $Plc) } else { @() }

function Invoke-Batch($steps, $label) {
    $steps | ConvertTo-Json -Depth 4 | Set-Content $script -Encoding utf8
    $summary = Invoke-Tia run --script $script --summary @sel | ConvertFrom-Json
    if ($LASTEXITCODE) {
        $summary.errors | ForEach-Object { Write-Warning "$label step $($_.step) ($($_.verb)): $($_.error)" }
        exit $LASTEXITCODE
    }
}

# tree primeiro: plc-navi.md e' a leitura de orientacao (26 KB p/ 476 blocos + tabelas + UDTs).
# O resto e' volume bruto — vai pro disco pra grep, ninguem le inteiro (snapshot = 251 KB).
$steps = @(, (@('tree', '--out', $out) + $plcArgs))
foreach ($v in 'doctor', 'snapshot', 'list-devices', 'list-tags', 'list-types') {
    $steps += , (@($v, '--out-file', (Join-Path $out "$v.json")) + $plcArgs)
}
$steps += , @('export-cax', '--out', $out)
$steps += , (@('list-blocks', '--type', 'OB', '--out-file', (Join-Path $out 'obs.json')) + $plcArgs)
Invoke-Batch $steps 'banho'

# ponytail: xref de TODOS os OBs (projetos reais têm poucos); filtro se doer
$obs = @((Get-Content (Join-Path $out 'obs.json') -Raw | ConvertFrom-Json).blocks)
$files = @{}
$steps = foreach ($i in 0..($obs.Count - 1)) {
    $f = Join-Path $out ("xref-{0:d3}.json" -f $i)
    $files[$obs[$i].name] = $f
    , (@('xref', '--name', $obs[$i].name, '--out-file', $f) + $plcArgs)
}
if ($steps) { Invoke-Batch $steps 'xref' }

# um arquivo só, como antes: nome do OB → xref (os parciais somem)
$xref = [ordered]@{}
foreach ($k in $files.Keys) { $xref[$k] = Get-Content $files[$k] -Raw | ConvertFrom-Json }
$xref | ConvertTo-Json -Depth 32 | Set-Content (Join-Path $out 'xref-obs.json')
Remove-Item @($files.Values) -ErrorAction SilentlyContinue
Remove-Item $script -ErrorAction SilentlyContinue

Write-Host "raio-x ok -> $out"
Write-Host "leia primeiro: $(Join-Path $out 'plc-navi.md') — o resto e' volume bruto p/ grep"
