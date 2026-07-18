# NAV INDEX
# 1-8    header
# 9-20   runner: reads cmd.json (array of tia args), runs tia.exe, writes out.txt/exit.txt
# Runs under scheduled task "TiaSmokeRun" (S4U logon = fresh token with Openness group).
# Protocol: write cmd.json -> schtasks /Run TiaSmokeRun -> poll exit.txt.

$dir = "c:\Scripts\TIA Portal\workspace\taskio"
New-Item -ItemType Directory -Force $dir | Out-Null
Remove-Item "$dir\exit.txt" -ErrorAction SilentlyContinue
$tia = "c:\Scripts\TIA Portal\src\Tia.Cli\bin\Debug\net48\tia.exe"
$args = Get-Content "$dir\cmd.json" -Raw | ConvertFrom-Json
Set-Location "c:\Scripts\TIA Portal"
& $tia @args *> "$dir\out.txt"
$LASTEXITCODE | Out-File "$dir\exit.txt" -Encoding ascii
