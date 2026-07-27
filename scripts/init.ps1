# NAV INDEX
# 1-10   header
# 11-20  gate 1: grupo Windows Siemens TIA Openness (nao automatizavel — precisa admin + logoff/logon)
# 21-28  gate 2: .NET SDK presente
# 29-50  gate 3: lib/*.dll (build-time) — copia da instalacao local do TIA Portal se existir
# 52-58  gates falharam -> para com instrucoes
# 59-64  gates ok -> rebuild.ps1 (build+test+whitelist) + instrucao final
#
# Macro "init": bootstrap de 1a vez numa maquina nova. Verifica os 2 gates que so um humano
# resolve (grupo + logon, TIA Portal instalado) e automatiza o resto (lib/, build, whitelist).
# Uso: pwsh scripts/init.ps1
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$ok = $true

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

$libDir = Join-Path $repo 'lib'
New-Item -ItemType Directory -Force -Path $libDir | Out-Null
$dllNames = @('Siemens.Engineering.Base.dll', 'Siemens.Engineering.Step7.dll', 'Siemens.Engineering.WinCCUnified.dll')
$programFiles = [Environment]::GetFolderPath('ProgramFiles')
$portalDirs = Get-ChildItem (Join-Path $programFiles 'Siemens\Automation') -Directory -Filter 'Portal V*' -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending
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

& (Join-Path $repo 'scripts\rebuild.ps1')
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host ""
Write-Host "init ok. Abra TIA Portal com um projeto (sessao interativa) e rode: tia doctor" -ForegroundColor Green
