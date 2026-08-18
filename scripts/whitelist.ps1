# NAV INDEX
# 1-14   header / params
# 15-32  whitelist registry entry (HKLM Openness <ver>) for tia.exe — run elevated/SYSTEM
# Re-run after every rebuild (FileHash changes). Invoked via scheduled task "TiaWhitelist".
#
# -Repo: este script roda a partir de uma COPIA protegida em %ProgramData%\tia-cli (a task
# TiaWhitelist usa token elevado sem UAC; o original vive no perfil do usuario, gravavel sem
# admin — ver setup-tasks.ps1). Dai o caminho do repo chegar por argumento em vez de sair do
# $PSScriptRoot. Chamado direto de dentro do repo, o default continua valendo.
param([string]$Repo = (Split-Path $PSScriptRoot))

$exe = Join-Path $Repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
if (-not (Test-Path $exe)) { Write-Error "tia.exe ausente: $exe (rodar: pwsh scripts/rebuild.ps1)"; exit 1 }
$f = Get-Item $exe
$sha = [System.Security.Cryptography.SHA256]::Create()
try { $hash = [Convert]::ToBase64String($sha.ComputeHash([IO.File]::ReadAllBytes($f.FullName))) }
finally { $sha.Dispose() }
# TIA compares DateModified as string; UTC vs local undocumented in practice -> write both entries
$dates = @{ "Entry" = $f.LastWriteTimeUtc.ToString("yyyy'/'MM'/'dd HH:mm:ss.fff")
            "EntryLocal" = $f.LastWriteTime.ToString("yyyy'/'MM'/'dd HH:mm:ss.fff") }

foreach ($root in @("HKLM:\SOFTWARE\Siemens\Automation\Openness")) {
    foreach ($ver in Get-ChildItem $root -ErrorAction SilentlyContinue) {
        foreach ($name in $dates.Keys) {
            $key = Join-Path $ver.PSPath "Whitelist\tia.exe\$name"
            New-Item -Path $key -Force | Out-Null
            Set-ItemProperty -Path $key -Name "Path" -Value $f.FullName
            Set-ItemProperty -Path $key -Name "DateModified" -Value $dates[$name]
            Set-ItemProperty -Path $key -Name "FileHash" -Value $hash
            Write-Output "whitelisted: $($ver.PSChildName) $name $($dates[$name]) $hash"
        }
    }
}
