# NAV INDEX
# 1-10   header / params
# 11-30  whitelist registry entry (HKLM Openness <ver>) for tia.exe — run elevated/SYSTEM
# Re-run after every rebuild (FileHash changes). Invoked via scheduled task "TiaWhitelist".

$exe = Join-Path (Split-Path $PSScriptRoot) 'src\Tia.Cli\bin\Debug\net48\tia.exe'
$f = Get-Item $exe
$hash = [Convert]::ToBase64String(
    [System.Security.Cryptography.SHA256]::Create().ComputeHash([IO.File]::ReadAllBytes($f.FullName)))
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
