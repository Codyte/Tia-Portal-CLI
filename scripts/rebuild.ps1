# Macro "rebuild": build Debug + testes offline + whitelist elevada SÓ se tia.exe mudou.
# Uso: pwsh scripts/rebuild.ps1 [-SkipTests]
param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

$before = (Test-Path $exe) ? (Get-FileHash $exe).Hash : ''
dotnet build (Join-Path $repo 'src') -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) { exit 1 }

if (-not $SkipTests) {
    & (Join-Path $repo 'src\Tia.Tests\bin\Debug\net48\Tia.Tests.exe')
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

if ($before -ne (Get-FileHash $exe).Hash) {
    # UAC 1 clique — schtasks sem elevação falha (ver CLAUDE.md)
    Start-Process pwsh -Verb RunAs -Wait -ArgumentList '-NoProfile','-File',(Join-Path $repo 'scripts\whitelist.ps1')
    Write-Host 'rebuild ok — whitelist refeita (tia.exe mudou)'
} else {
    Write-Host 'rebuild ok — tia.exe inalterado, whitelist mantida'
}
