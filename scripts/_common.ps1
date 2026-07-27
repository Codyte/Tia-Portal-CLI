# NAV INDEX
# 1-14   header: por que existe (sessao 0 x sessao 1)
# 15-18  caminhos do repo (Repo/Exe/TaskIo) — unica definicao, dot-sourced pelos macros
# 19-70  Invoke-Tia: roteia por sessao, protocolo run-id, $global:LASTEXITCODE
#
# Dot-source nos macros: . (Join-Path $PSScriptRoot '_common.ps1')
# O agente vive na sessao 0 (UserInteractive=False); TIA Portal vive na sessao 1.
# TiaPortal.GetProcesses() nao enxerga processo de outra sessao, entao & tia.exe direto da
# sessao 0 devolve sempre "No running TIA Portal instance found". Invoke-Tia esconde isso:
# sessao 1 = invoca direto; sessao 0 = roteia pela task TiaSmokeRun (LogonType Interactive).
# Caller nao sabe a diferenca e continua checando $LASTEXITCODE.

$script:Repo   = Split-Path $PSScriptRoot
$script:Exe    = Join-Path $script:Repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
$script:TaskIo = Join-Path $script:Repo 'workspace\taskio'

function Invoke-Tia {
    # Sem param block de proposito: qualquer parametro faz o PS tentar casar --name/--out com
    # parametros da funcao ("parameter name 'out' is ambiguous"). $args passa tudo cru.
    # Timeout: $env:TIA_TIMEOUT segundos (default 600 — open-project leva 2-4 min).
    $tiaArgs = @($args)
    if (-not $tiaArgs) { throw 'Invoke-Tia: sem argumentos' }

    # TIA_VIA_TASK=1 força a rota da task mesmo da sessao 1 (unico jeito de testar esse ramo
    # quando o shell do agente nasce interativo)
    if ((Get-Process -Id $PID).SessionId -ne 0 -and -not $env:TIA_VIA_TASK) {
        if ($tiaArgs[0] -eq '--script-ps1') { & $tiaArgs[1] @($tiaArgs | Select-Object -Skip 2); return }
        & $script:Exe @tiaArgs
        return
    }

    $task = Get-ScheduledTask -TaskName TiaSmokeRun -ErrorAction SilentlyContinue
    if (-not $task) { throw 'task TiaSmokeRun ausente — rodar 1x elevado: pwsh scripts/setup-tasks.ps1' }
    # D9: Openness nao aceita duas chamadas ao mesmo tempo. Task rodando = alguem esta dentro.
    if ($task.State -eq 'Running') { throw 'outra chamada tia em andamento (TiaSmokeRun rodando) — D9: uma por vez' }

    New-Item -ItemType Directory -Force $script:TaskIo | Out-Null
    # Prune do lixo de ontem. Arquivo pode estar lockado por um portal ainda vivo -> erro ignorado.
    foreach ($pat in 'out-*.txt', 'err-*.txt', 'exit-*.txt') {
        Get-ChildItem $script:TaskIo -Filter $pat -ErrorAction SilentlyContinue |
            Where-Object LastWriteTime -lt (Get-Date).AddDays(-1) |
            Remove-Item -ErrorAction SilentlyContinue
    }

    # Run-id unico resolve dois problemas: (a) o poll nunca le o exit.txt da rodada anterior;
    # (b) verbo que inicia o portal deixa o handle do arquivo de saida herdado e aberto enquanto
    # o portal viver — nome fixo travaria a proxima rodada.
    $id = [guid]::NewGuid().ToString('N').Substring(0, 8)
    $exitFile = Join-Path $script:TaskIo "exit-$id.txt"
    [pscustomobject]@{ id = $id; args = $tiaArgs } | ConvertTo-Json -Compress |
        Set-Content (Join-Path $script:TaskIo 'cmd.json') -Encoding utf8
    Start-ScheduledTask -TaskName TiaSmokeRun

    $timeout = [int]($env:TIA_TIMEOUT ?? 600)
    $deadline = (Get-Date).AddSeconds($timeout)
    while (-not (Test-Path $exitFile)) {
        if ((Get-Date) -gt $deadline) { throw "timeout ${timeout}s: tia $($tiaArgs -join ' ')" }
        Start-Sleep -Seconds 1
    }

    $err = Get-Content (Join-Path $script:TaskIo "err-$id.txt") -Raw -ErrorAction SilentlyContinue
    if ($err) { [Console]::Error.Write($err) }
    Get-Content (Join-Path $script:TaskIo "out-$id.txt") -Raw -ErrorAction SilentlyContinue
    # Funcao PS nao seta $LASTEXITCODE sozinha; sem isto todo `if ($LASTEXITCODE)` dos macros mente.
    $global:LASTEXITCODE = [int](Get-Content $exitFile -Raw).Trim()
}
