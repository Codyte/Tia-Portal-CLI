# ====================== BEGIN NAV INDEX ======================
# NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
#   L41    Write-Log
#   L104   host: sem switch nenhum, este processo E o host e so volta no -Stop --
# ======================= END NAV INDEX =======================

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
    [switch]$Ui,          # abre o control panel da Siemens junto (o usuario quer VER a instancia)
    [string]$Name = 'plc_1500_1',
    [string]$Article = '6ES7 515-2AN03-0AB0'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$ws   = Join-Path $repo 'workspace'
New-Item -ItemType Directory -Force $ws | Out-Null
$log  = Join-Path $ws 'sim-host.log'
$flag = Join-Path $ws 'sim-host.stop'
$uiOn = Join-Path $ws 'sim-host.ui'
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
    # host ja de pe: -Start de novo so criaria um 2o processo dormindo (a task tem
    # MultipleInstancesPolicy IgnoreNew, a rota da sessao 1 nao tem). No-op e o certo.
    $last = Get-Content $log -Tail 1 -ErrorAction SilentlyContinue
    if ($last -and $last -notmatch 'done|ERRO') { "host ja rodando ($last)"; return }
    Remove-Item $log, $flag, $uiOn -ErrorAction SilentlyContinue
    # marcador em vez de argumento: Start-ScheduledTask nao passa parametro pra task
    if ($Ui) { New-Item -ItemType File -Force $uiOn | Out-Null }
    if ((Get-Process -Id $PID).SessionId -eq 0) {
        Start-ScheduledTask -TaskName TiaSimHost   # registrada pelo setup-tasks.ps1
    } else {
        # -Article junto: sem ele, host iniciado com CPU diferente do default caia no 6ES7 515
        # em silencio. Pela rota da sessao 0 (task) nao ha' como passar parametro — la' valem os
        # defaults, e por isso -Ui vai por arquivo-marcador em vez de argumento.
        # ArgumentList em STRING, nao array: com array o Start-Process junta por espaco sem citar
        # nada e o MLFB ('6ES7 515-2AN03-0AB0') chegaria repartido em dois argumentos.
        Start-Process powershell -WindowStyle Hidden -ArgumentList (
            '-NoProfile -ExecutionPolicy Bypass -File "{0}" -Name "{1}" -Article "{2}"' -f `
                $PSCommandPath, $Name, $Article)
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
    # instancia com o mesmo nome ja registrada (control panel aberto, host anterior que nao saiu
    # limpo) devolve `-5, AlreadyExists`: reusar e melhor que falhar — o dono continua sendo quem
    # registrou, entao este host nao a desliga na saida.
    # Regra: desfazer no fim exatamente o que este host fez, nada mais. Instancia que ja estava
    # ligada continua ligada no -Stop; a que este host ligou, ele desliga.
    $registered = $true
    try {
        $inst = $mgr::RegisterInstance($Article, $Name)
        Write-Log "registered $Name"
    } catch {
        $registered = $false
        $inst = $mgr::CreateInterface($Name)
        Write-Log "reusando $Name ja registrada (state=$($inst.OperatingState))"
    }
    $poweredOn = "$($inst.OperatingState)" -eq 'Off'
    if ($poweredOn) { Write-Log "powerOn=$($inst.PowerOn(60000))" }
    Write-Log "state=$($inst.OperatingState)"
    # o control panel e so uma vista do mesmo Runtime Manager: ele lista a instancia que este host
    # registrou, e fechar a janela nao desliga nada (quem segura a instancia e este processo).
    if ($Ui -or (Test-Path $uiOn)) {
        $cp = "${env:ProgramFiles(x86)}\Siemens\Automation\PLCSIMADV\bin\Siemens.Simatic.PlcSim.Advanced.UserInterface.exe"
        if (Test-Path $cp) { Start-Process $cp; Write-Log 'control panel aberto' }
        else { Write-Log "control panel nao encontrado em $cp" }
    }
    while (-not (Test-Path $flag)) { Start-Sleep -Seconds 2 }
    Write-Log "stop requested (poweredOn=$poweredOn registered=$registered)"
    if ($poweredOn) { $inst.PowerOff(30000) }
    if ($registered) { $inst.UnregisterInstance() }
    Write-Log 'done'
} catch {
    Write-Log "ERRO: $($_.Exception.GetType().Name) $($_.Exception.Message)"
}
