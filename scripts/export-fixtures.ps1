# NAV INDEX
# 1-8    header
# 9-24   lista de blocos/tabelas do projeto de referencia
# 25-40  exporta um por vez (falha de um nao aborta os outros)
# Roda NA SESSAO 1: pwsh scripts/tia.ps1 --script-ps1 scripts\export-fixtures.ps1
# Saida: workspace/padrao/. Sao as fixtures reais que substituem as sinteticas de docs/examples.
$ErrorActionPreference = 'Continue'
$repo = Split-Path $PSScriptRoot
$tia = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
$out = Join-Path $repo 'workspace\padrao'

$blocks = @(
    'MODULE_ERROR_MOLDE'
    'OB_MOLDE_ALARMES'
    'OB_MOLDE_PARTIDAS'
    'MOLDE_ANALOGS'
    'MOLDE TOT1'
    'FC_Modelo'
    'DB GLOBAL'
    # os 6 blocos de um acionamento completo (Soprador 1) — a unidade que o padrao replica
    'PARTIDA_SOPRADOR_1 (S-01A)'
    'FB CONDIÇÃO DE PARTIDA_S-01A'
    'FB FALHA_S-01A'
    'FB SETPOINT ESCALONAMENTO S-01A'
    'FB SETPOINT MANUAL S-01A'
    'SINA_SPEED_TLG20_S-01A'
)
$tables = @('DISPOSITIVOS_PROFINET', 'SOPRADOR_DESARENADOR (S-01A)')

New-Item -ItemType Directory -Force $out | Out-Null
foreach ($b in $blocks) {
    & $tia export-block --name $b --out $out | Out-Null
    Write-Output "$(if ($LASTEXITCODE -eq 0) { 'ok  ' } else { 'FAIL' }) block $b"
}
foreach ($t in $tables) {
    & $tia export-tags --table $t --out $out | Out-Null
    Write-Output "$(if ($LASTEXITCODE -eq 0) { 'ok  ' } else { 'FAIL' }) table $t"
}
