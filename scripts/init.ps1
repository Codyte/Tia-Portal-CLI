# ====================== BEGIN NAV INDEX ======================
# NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
#   L35    Get-ExeHash
#   L40    Test-Whitelisted
#   L52    Test-SkillInstalled
#   L61    Show
# ======================= END NAV INDEX =======================

# NAV INDEX
# 21-33   header / caminhos / descoberta do Portal instalado
# 35-56   helpers de verificacao (hash do exe, whitelist do registro, skill instalada)
# 58-93   -Check: relatorio read-only dos 9 pontos + estado vivo, exit 1 se faltar algo
# 95-101  gate 1: grupo Windows Siemens TIA Openness (nao automatizavel — admin + logoff/logon)
# 103-109 gate 2: .NET SDK presente
# 111-128 gate 3: lib/*.dll (build-time) — copia da instalacao local do TIA Portal
# 130-133 gates falharam -> para com instrucoes
# 135-148 gate 4: tasks TiaWhitelist/TiaSmokeRun (setup-tasks elevado, 1 UAC) — idempotente
# 150-151 rebuild.ps1 (build + testes offline + whitelist)
# 153-165 gate 5: shim tia no PATH do usuario + TIA_CLI_HOME
# 167-174 gate 6: skill do agente copiada pra ~/.claude/skills/tia
#
# Macro "init": bootstrap de 1a vez numa maquina nova. Verifica os 2 gates que so um humano
# resolve (grupo + logon, TIA Portal instalado) e automatiza o resto (lib/, build, whitelist,
# PATH, skill). Idempotente — re-rodar depois de git pull.
# Uso: pwsh scripts/init.ps1  |  pwsh scripts/init.ps1 -Check
param([switch]$Check)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
$skillSrc = Join-Path $repo 'skills\tia'
$skillDst = Join-Path $HOME '.claude\skills\tia'
$ok = $true

$libDir = Join-Path $repo 'lib'
$dllNames = @('Siemens.Engineering.Base.dll', 'Siemens.Engineering.Step7.dll', 'Siemens.Engineering.WinCCUnified.dll')
$programFiles = [Environment]::GetFolderPath('ProgramFiles')
$portalDirs = Get-ChildItem (Join-Path $programFiles 'Siemens\Automation') -Directory -Filter 'Portal V*' -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending

function Get-ExeHash($path) {
    if (-not (Test-Path $path)) { return $null }
    [Convert]::ToBase64String([Security.Cryptography.SHA256]::Create().ComputeHash([IO.File]::ReadAllBytes($path)))
}

function Test-Whitelisted {
    # Whitelist stale (hash != o do exe atual) = EngineeringSecurityException na 1a chamada.
    $h = Get-ExeHash $exe
    if (-not $h) { return $false }
    foreach ($ver in Get-ChildItem 'HKLM:\SOFTWARE\Siemens\Automation\Openness' -ErrorAction SilentlyContinue) {
        $key = Join-Path $ver.PSPath 'Whitelist\tia.exe\Entry'
        if (-not (Test-Path $key)) { continue }
        if ((Get-ItemProperty $key).FileHash -eq $h) { return $true }
    }
    return $false
}

function Test-SkillInstalled {
    $dst = Join-Path $skillDst 'SKILL.md'
    if (-not (Test-Path $dst)) { return $false }
    (Get-FileHash $dst).Hash -eq (Get-FileHash (Join-Path $skillSrc 'SKILL.md')).Hash
}

if ($Check) {
    # Read-only: nao copia, nao registra, nao builda. Exit 1 se faltar algo.
    $allOk = $true
    function Show($name, $good, $hint) {
        if ($good) { Write-Host "ok     $name" -ForegroundColor Green }
        else { Write-Host "FALTA  $name -- $hint" -ForegroundColor Yellow; $script:allOk = $false }
    }
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    Show 'grupo Siemens TIA Openness' $principal.IsInRole('Siemens TIA Openness') `
        "admin roda: net localgroup ""Siemens TIA Openness"" $env:USERNAME /add -- depois LOGOFF/LOGON"
    Show '.NET SDK' ([bool](dotnet --version 2>$null)) 'instalar .NET SDK 8'
    Show "TIA Portal instalado ($($portalDirs.Name -join ', '))" ([bool]$portalDirs) 'TIA Portal V19+ com Openness'
    Show 'lib/*.dll (build-time)' (-not ($dllNames | Where-Object { -not (Test-Path (Join-Path $libDir $_)) })) `
        'rodar init.ps1 sem -Check (copia da instalacao local)'
    Show "tia.exe$(if (Test-Path $exe) { ' (' + (Get-Item $exe).LastWriteTime.ToString('yyyy-MM-dd HH:mm') + ')' })" `
        (Test-Path $exe) 'pwsh scripts/rebuild.ps1'
    Show 'whitelist do registro bate com o hash atual' (Test-Whitelisted) 'Start-ScheduledTask -TaskName TiaWhitelist'
    Show 'tasks TiaWhitelist/TiaSmokeRun' `
        (-not (@('TiaWhitelist', 'TiaSmokeRun') | Where-Object { -not (Get-ScheduledTask -TaskName $_ -ErrorAction SilentlyContinue) })) `
        'rodar init.ps1 sem -Check (1 UAC)'
    Show 'shim tia no PATH do usuario' `
        (([Environment]::GetEnvironmentVariable('Path', 'User') -split ';') -contains $PSScriptRoot) `
        'rodar init.ps1 sem -Check'
    Show 'skill em ~/.claude/skills/tia' (Test-SkillInstalled) 'rodar init.ps1 sem -Check (copia/atualiza)'

    Write-Host ""
    Write-Host "estado vivo (nao e gate):"
    Write-Host "  sessao do shell: $((Get-Process -Id $PID).SessionId)  (0 = roteia pela task TiaSmokeRun)"
    $portal = @(Get-Process -Name 'Siemens.Automation.Portal' -ErrorAction SilentlyContinue)
    Write-Host "  TIA Portal rodando: $(if ($portal) { "$($portal.Count) (sessao $($portal.SessionId -join ','))" } else { 'nenhum' })"
    $al = @(Get-ChildItem (Join-Path $repo 'src\Tia.Lib') -Recurse -Filter '*.al2?' -File -ErrorAction SilentlyContinue)
    Write-Host "  biblioteca .al21: $(if ($al) { "$($al[0].Name) ($([math]::Round($al[0].Length / 1KB)) KB)" } else { 'ausente -- assar com bake-lib.ps1' })"
    if (-not $allOk) { Write-Host "`ninit incompleto." -ForegroundColor Yellow; exit 1 }
    Write-Host "`ninit ok. Com o TIA Portal aberto num projeto: tia doctor" -ForegroundColor Green
    exit 0
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole('Siemens TIA Openness')) {
    Write-Warning "Grupo 'Siemens TIA Openness' ausente no token atual. Peca a um admin: net localgroup ""Siemens TIA Openness"" $env:USERNAME /add -- depois faca LOGOFF/LOGON (token antigo nao carrega o grupo novo)."
    $ok = $false
} else {
    Write-Host "gate 1 ok: grupo Siemens TIA Openness"
}

$dotnetVer = (dotnet --version 2>$null)
if (-not $dotnetVer) {
    Write-Warning ".NET SDK nao encontrado no PATH. Instale .NET SDK 8: https://dotnet.microsoft.com/download"
    $ok = $false
} else {
    Write-Host "gate 2 ok: dotnet SDK $dotnetVer"
}

New-Item -ItemType Directory -Force -Path $libDir | Out-Null
$missing = @()
foreach ($dll in $dllNames) {
    $dest = Join-Path $libDir $dll
    if (Test-Path $dest) { continue }
    $found = $portalDirs |
        ForEach-Object { Join-Path $_.FullName "PublicAPI\$($_.Name -replace 'Portal ','')\net48\$dll" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    if ($found) { Copy-Item $found $dest; Write-Host "lib/: copiado $dll de $found" }
    else { $missing += $dll }
}
if ($missing.Count -gt 0) {
    Write-Warning "DLLs Siemens.Engineering nao encontradas (TIA Portal V21+ com Openness instalado?): $($missing -join ', ')"
    $ok = $false
} else {
    Write-Host "gate 3 ok: lib/ populada"
}

if (-not $ok) {
    Write-Host "init incompleto -- resolva os gates acima e rode 'pwsh scripts/init.ps1' de novo." -ForegroundColor Yellow
    exit 1
}

# tasks TiaWhitelist/TiaSmokeRun: unico passo que exige elevacao (HKLM + registro de task).
# rebuild.ps1 depende da TiaWhitelist; sem ela cai no fallback RunAs, que da sessao 0 nao mostra UAC.
$tasks = @('TiaWhitelist', 'TiaSmokeRun') | Where-Object {
    -not (Get-ScheduledTask -TaskName $_ -ErrorAction SilentlyContinue) }
if ($tasks) {
    Write-Host "registrando tasks ($($tasks -join ', ')) — vai pedir UAC uma vez"
    Start-Process pwsh -Verb RunAs -Wait -ArgumentList `
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'setup-tasks.ps1')
    if (-not (Get-ScheduledTask -TaskName TiaWhitelist -ErrorAction SilentlyContinue)) {
        Write-Warning "setup-tasks nao registrou TiaWhitelist — ver workspace\setup-log.txt"
        exit 1
    }
}
Write-Host "gate 4 ok: tasks TiaWhitelist/TiaSmokeRun registradas"

& (Join-Path $repo 'scripts\rebuild.ps1')
if ($LASTEXITCODE -ne 0) { exit 1 }

# gate 5: shim no PATH + TIA_CLI_HOME. Variavel de usuario (nao de maquina) -> sem elevacao.
# TIA_CLI_HOME e como a skill acha o repo de qualquer diretorio; o PATH e pro humano digitar `tia`.
$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if (($userPath -split ';') -notcontains $PSScriptRoot) {
    [Environment]::SetEnvironmentVariable('Path', "$userPath;$PSScriptRoot".Trim(';'), 'User')
    Write-Host "PATH: + $PSScriptRoot (abrir um shell novo pra valer)"
}
if ([Environment]::GetEnvironmentVariable('TIA_CLI_HOME', 'User') -ne $repo) {
    [Environment]::SetEnvironmentVariable('TIA_CLI_HOME', $repo, 'User')
    Write-Host "TIA_CLI_HOME = $repo"
}
$env:TIA_CLI_HOME = $repo
Write-Host "gate 5 ok: shim tia no PATH"

# gate 6: skill do agente. Copia (nao symlink: exige admin/dev mode no Windows) -> re-rodar
# init depois de um git pull atualiza. -Check acusa quando o instalado difere do repo.
if (-not (Test-SkillInstalled)) {
    New-Item -ItemType Directory -Force -Path $skillDst | Out-Null
    Copy-Item (Join-Path $skillSrc '*') $skillDst -Recurse -Force
    Write-Host "skill: instalada em $skillDst (vale na proxima sessao do Claude Code)"
}
Write-Host "gate 6 ok: skill tia instalada"

Write-Host ""
Write-Host "init ok. Abra TIA Portal com um projeto (sessao interativa) e rode: tia doctor" -ForegroundColor Green
Write-Host "conferir a instalacao a qualquer momento: pwsh scripts/init.ps1 -Check"
