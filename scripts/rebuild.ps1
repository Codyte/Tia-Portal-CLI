# Macro "rebuild": build Debug + testes offline + whitelist elevada SÓ se tia.exe mudou.
# Uso: pwsh scripts/rebuild.ps1 [-SkipTests]
param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

function Get-ExeHash64 {
    [Convert]::ToBase64String(
        [System.Security.Cryptography.SHA256]::Create().ComputeHash([IO.File]::ReadAllBytes($exe)))
}
# Verdade = o hash gravado no registro, nao o delta do build: um build que nao mudou tia.exe
# mas cujo whitelist anterior falhou deixava o registro stale indefinidamente (aconteceu, 9 dias).
function Get-RegHash {
    $k = 'HKLM:\SOFTWARE\Siemens\Automation\Openness'
    Get-ChildItem $k -ErrorAction SilentlyContinue | ForEach-Object {
        (Get-ItemProperty (Join-Path $_.PSPath 'Whitelist\tia.exe\Entry') -Name FileHash -ErrorAction SilentlyContinue).FileHash
    } | Select-Object -First 1
}

dotnet build (Join-Path $repo 'src') -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) { exit 1 }

if (-not $SkipTests) {
    & (Join-Path $repo 'src\Tia.Tests\bin\Debug\net48\Tia.Tests.exe')
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

if ((Get-ExeHash64) -ne (Get-RegHash)) {
    # Task TiaWhitelist roda como SYSTEM/Highest: sem UAC, disparavel por qualquer sessao.
    if (Get-ScheduledTask -TaskName TiaWhitelist -ErrorAction SilentlyContinue) {
        Start-ScheduledTask -TaskName TiaWhitelist
        $limit = (Get-Date).AddSeconds(30)
        while ((Get-ExeHash64) -ne (Get-RegHash) -and (Get-Date) -lt $limit) { Start-Sleep -Milliseconds 500 }
    } else {
        Start-Process pwsh -Verb RunAs -Wait -ArgumentList '-NoProfile','-File',(Join-Path $repo 'scripts\whitelist.ps1')
    }
    if ((Get-ExeHash64) -ne (Get-RegHash)) {
        Write-Host 'ATENCAO: whitelist AINDA stale — Openness vai recusar tia.exe' -ForegroundColor Red
        exit 1
    }
    Write-Host 'rebuild ok — whitelist refeita'
} else {
    Write-Host 'rebuild ok — whitelist ja bate com tia.exe'
}
