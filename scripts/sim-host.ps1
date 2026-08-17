# NAV INDEX
# 1-10    header + params
# 12-24   -Status  (instancias registradas + log)
# 26-36   -Stop    (arquivo .stop, host desliga a instancia sozinho)
# 38-52   -Start   (sessao 0 -> task TiaSimHost; sessao 1 -> processo destacado)
# 54-76   host loop (register + PowerOn + dorme ate .stop)
#
# Segura uma instancia do S7-PLCSIM Advanced viva para o `tia sim-run` dar attach.
#
# Por que existe: o Runtime Manager sobe in-proc, nao ha servico — instancia registrada dentro do
# `tia.exe` morre quando ele sai. Precisa de um processo longevo. O control panel da Siemens fazia
# esse papel a mao; este script faz igual, sem GUI e sem clique. Ele sobe o Runtime Manager sozinho:
# medido 2026-08-17 com control panel e manager mortos, `RegisterInstance` ressuscitou o manager.
#
# Tem que rodar na SESSAO 1. Da sessao 0 a API nao enxerga o manager (`Version` volta vazio,
# `RegisterInstance` da `-1, InvalidErrorCode`) — mesma parede do attach do Openness. Por isso
# -Start roteia pela task `TiaSimHost` (LogonType Interactive) quando o shell nasce na sessao 0.
param(
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status,
    [string]$Name = 'plc_1500_1',
    [string]$Article = '6ES7 515-2AN03-0AB0'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$ws   = Join-Path $repo 'workspace'
New-Item -ItemType Directory -Force $ws | Out-Null
$log  = Join-Path $ws 'sim-host.log'
$flag = Join-Path $ws 'sim-host.stop'
$dll  = Join-Path $repo 'lib\Siemens.Simatic.Simulation.Runtime.Api.x64.dll'

function Write-Log($m) { "$(Get-Date -f 'HH:mm:ss') $m" | Add-Content $log }

if ($Status) {
    # a lista vem vazia quando este shell esta na sessao 0, mesmo com a instancia ligada
    try {
        Add-Type -Path $dll
        $mgr = [Siemens.Simatic.Simulation.Runtime.SimulationRuntimeManager]
        $names = @($mgr::RegisteredInstanceInfo | Where-Object { $_ } | ForEach-Object { $_.Name })
        $ver = "$($mgr::Version)"
        if (-not $ver) { $ver = '(invisivel desta sessao — o log abaixo e a fonte)' }
        $tail = @()
        if (Test-Path $log) { $tail = Get-Content $log -Tail 3 }
        [pscustomobject]@{
            session   = (Get-Process -Id $PID).SessionId
            manager   = $ver
            instances = $names
            log       = $tail
        } | Format-List
    } catch { "status falhou: $($_.Exception.Message)" }
    return
}

if ($Stop) {
    $last = Get-Content $log -Tail 1 -ErrorAction SilentlyContinue
    if (-not $last -or $last -match 'done|ERRO') { "host nao esta rodando ($last)"; return }
    New-Item -ItemType File -Force $flag | Out-Null
    # o host acorda a cada 2 s; 20 s cobre PowerOff(30s) comecando
    for ($i = 0; $i -lt 10; $i++) {
        Start-Sleep -Seconds 2
        if ((Get-Content $log -Tail 1) -match 'done|ERRO') { break }
    }
    Get-Content $log -Tail 2
    return
}

if ($Start) {
    Remove-Item $log, $flag -ErrorAction SilentlyContinue
    if ((Get-Process -Id $PID).SessionId -eq 0) {
        Start-ScheduledTask -TaskName TiaSimHost   # registrada pelo setup-tasks.ps1
    } else {
        Start-Process powershell -WindowStyle Hidden `
            -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Name', $Name
    }
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Seconds 2
        if ((Get-Content $log -Tail 1 -ErrorAction SilentlyContinue) -match 'powerOn|ERRO') { break }
    }
    Get-Content $log -ErrorAction SilentlyContinue
    return
}

# --- host: sem switch nenhum, este processo E o host e so volta no -Stop ---
try {
    Add-Type -Path $dll
    $mgr = [Siemens.Simatic.Simulation.Runtime.SimulationRuntimeManager]
    Write-Log "session=$((Get-Process -Id $PID).SessionId) pid=$PID mgrVersion=$($mgr::Version)"
    $inst = $mgr::RegisterInstance($Article, $Name)
    Write-Log "registered $Name"
    Write-Log "powerOn=$($inst.PowerOn(60000)) state=$($inst.OperatingState)"
    while (-not (Test-Path $flag)) { Start-Sleep -Seconds 2 }
    Write-Log 'stop requested; powering off'
    $inst.PowerOff(30000)
    $inst.UnregisterInstance()
    Write-Log 'done'
} catch {
    Write-Log "ERRO: $($_.Exception.GetType().Name) $($_.Exception.Message)"
}
