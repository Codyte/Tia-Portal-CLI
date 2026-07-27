# Driver da sessao 0 -> sessao 1: escreve cmd.json, dispara TiaSmokeRun, espera, imprime out.txt.
# Uso: pwsh scripts/tia-task.ps1 export-block --name X --out DIR
#      pwsh scripts/tia-task.ps1 --script-ps1 scripts\raio-x.ps1 "Nome do Projeto"
# Sem param block de proposito: qualquer bloco faria o PS tentar casar --out/--name com
# parametros dele ("parameter name 'out' is ambiguous"). $args passa tudo cru.
# Timeout via $env:TIA_TASK_TIMEOUT (segundos, default 1800).
$ErrorActionPreference = 'Stop'
$dir = Join-Path (Split-Path $PSScriptRoot) 'workspace\taskio'
$timeout = [int]($env:TIA_TASK_TIMEOUT ?? 1800)
New-Item -ItemType Directory -Force $dir | Out-Null
# apagar ANTES de disparar: sem isso o poll le o exit/out da rodada anterior
Remove-Item "$dir\exit.txt", "$dir\out.txt" -ErrorAction SilentlyContinue
$args | ConvertTo-Json -AsArray | Set-Content "$dir\cmd.json" -Encoding utf8
Start-ScheduledTask -TaskName TiaSmokeRun
$deadline = (Get-Date).AddSeconds($timeout)
while (-not (Test-Path "$dir\exit.txt")) {
    if ((Get-Date) -gt $deadline) { Write-Error "timeout ${timeout}s: $args" }
    Start-Sleep -Seconds 2
}
Get-Content "$dir\out.txt" -Raw -ErrorAction SilentlyContinue
exit ([int](Get-Content "$dir\exit.txt" -Raw).Trim())
