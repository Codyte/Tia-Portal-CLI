# NAV INDEX
# 1-14   header: por que existe (sessao 0 x sessao 1)
# 15-18  caminhos do repo (Repo/Exe/TaskIo) — unica definicao, dot-sourced pelos macros
# 19-30  Resolve-LibFile: acha a unica .al2? sob src/Tia.Lib
# 31-52  ConvertTo-CmdLine: citacao CommandLineToArgvW (usada por taskrun.ps1 e smokeloop.ps1)
# 53-120 Invoke-Tia: roteia por sessao, lock, protocolo run-id, $global:LASTEXITCODE
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

function Resolve-LibFile {
    # A .al21 é artefato de build e o Portal renomeia a pasta junto com a library (tia_cli ->
    # tia-cli quebrou todos os macros de uma vez). Caminho fixo em dois scripts era um rename de
    # distância; o glob acha a única .al21 sob src/Tia.Lib.
    $hits = @(Get-ChildItem (Join-Path $script:Repo 'src\Tia.Lib') -Recurse -Filter '*.al2?' -File -ErrorAction SilentlyContinue)
    if (-not $hits) { throw 'nenhuma .al21 em src/Tia.Lib — assar com: pwsh scripts/bake-lib.ps1 -Plc <PLC> -Apply' }
    if ($hits.Count -gt 1) { throw "mais de uma .al21 em src/Tia.Lib ($($hits.Name -join ', ')) — passar -File" }
    # relativo ao repo (nao ao cwd): e assim que os macros passam --file e montam Test-Path
    $hits[0].FullName.Substring($script:Repo.Length + 1)
}

function ConvertTo-CmdLine($items) {
    # Start-Process -ArgumentList com ARRAY nao cita nada: "MOTOR_AREA_01 (MOTOR_01)" chegaria
    # como 2 argumentos. Citacao no padrao CommandLineToArgvW: aspas dobram as barras que as
    # precedem, e a barra final tambem dobra (senao escaparia a aspa de fechamento).
    # Vive aqui porque taskrun.ps1 e smokeloop.ps1 falam o MESMO protocolo taskio: quando a
    # citacao morava so' num deles, o outro reparta a linha em argumento com aspas ou barra final.
    ($items | ForEach-Object {
        $a = [string]$_
        if ($a -eq '' -or $a -match '[\s"]') {
            '"' + (($a -replace '(\\+)"', '$1$1"' -replace '"', '\"') -replace '(\\+)$', '$1$1') + '"'
        } else { $a }
    }) -join ' '
}

function Invoke-Tia {
    # Sem param block de proposito: qualquer parametro faz o PS tentar casar --name/--out com
    # parametros da funcao ("parameter name 'out' is ambiguous"). $args passa tudo cru.
    # Timeout: $env:TIA_TIMEOUT segundos (default 600 — open-project leva 2-4 min).
    $tiaArgs = @($args)
    if (-not $tiaArgs) { throw 'Invoke-Tia: sem argumentos' }

    # mesma razao do taskrun.ps1: tia.exe fala UTF-8; sem isto acento vira '?' na rota direta
    [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
    $OutputEncoding = [Text.UTF8Encoding]::new($false)

    # TIA_VIA_TASK=1 força a rota da task mesmo da sessao 1 (unico jeito de testar esse ramo
    # quando o shell do agente nasce interativo)
    if ((Get-Process -Id $PID).SessionId -ne 0 -and -not $env:TIA_VIA_TASK) {
        if ($tiaArgs[0] -eq '--script-ps1') { & $tiaArgs[1] @($tiaArgs | Select-Object -Skip 2); return }
        & $script:Exe @tiaArgs
        return
    }

    $task = Get-ScheduledTask -TaskName TiaSmokeRun -ErrorAction SilentlyContinue
    if (-not $task) { throw 'task TiaSmokeRun ausente — rodar 1x elevado: pwsh scripts/setup-tasks.ps1' }

    New-Item -ItemType Directory -Force $script:TaskIo | Out-Null
    # D9: Openness nao aceita duas chamadas ao mesmo tempo. O teste de $task.State sozinho era
    # TOCTOU — entre ele e o Start-ScheduledTask cabem duas chamadas, e como cmd.json tem nome
    # fixo e a task ignora start concorrente (IgnoreNew), a 2a so' descobria no timeout de 600 s.
    # CreateNew falha atomicamente se o lock existir. Lock orfao (processo morto no meio) e'
    # colhido pelo mtime, com a task parada como segunda testemunha.
    $lockFile = Join-Path $script:TaskIo 'busy.lock'
    $timeout = [int]($env:TIA_TIMEOUT ?? 600)
    if ((Test-Path $lockFile) -and $task.State -ne 'Running' -and
        (Get-Item $lockFile).LastWriteTime -lt (Get-Date).AddSeconds(-$timeout)) {
        Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
    }
    # O lock nao substitui o teste de State: task disparada por fora (smokeloop.ps1, ou
    # Start-ScheduledTask na mao) roda sem lock nenhum, e sem este teste a chamada nova entrava,
    # tinha o start engolido pelo IgnoreNew e so' descobria no timeout.
    if ($task.State -eq 'Running') { throw 'outra chamada tia em andamento (TiaSmokeRun rodando) — D9: uma por vez' }
    try { $lock = [IO.File]::Open($lockFile, 'CreateNew', 'Write', 'None') }
    catch { throw 'outra chamada tia em andamento (workspace\taskio\busy.lock) — D9: uma por vez' }
    try {
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
    } finally {
        $lock.Dispose()
        # Timeout NAO libera o lock: o verbo pode continuar rodando dentro da task, e soltar aqui
        # deixaria a proxima chamada entrar em cima de uma sessao Openness viva (D9). Quem libera
        # nesse caso e' o coletor de lock orfao la' em cima, quando a task ja tiver parado.
        if ((Get-ScheduledTask -TaskName TiaSmokeRun -ErrorAction SilentlyContinue).State -ne 'Running') {
            Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
        }
    }
}
