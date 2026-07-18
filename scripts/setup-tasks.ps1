# NAV INDEX
# 1-6    header
# 7-25   creates scheduled tasks TiaWhitelist (SYSTEM) + TiaSmokeRun (S4U user token) and runs whitelist
# Run elevated, once. Idempotent.

Start-Transcript 'c:\Scripts\TIA Portal\workspace\setup-log.txt' -Force
$pwsh = (Get-Command pwsh).Source
$wl = 'c:\Scripts\TIA Portal\scripts\whitelist.ps1'
$tr = 'c:\Scripts\TIA Portal\scripts\taskrun.ps1'

schtasks /Create /F /TN TiaWhitelist /SC ONCE /ST 00:00 /RU SYSTEM /RL HIGHEST `
    /TR "`"$pwsh`" -NoProfile -ExecutionPolicy Bypass -File `"$wl`""

$action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$tr`""
$principal = New-ScheduledTaskPrincipal -UserId "TITANXNEXUS\Carlos_Ortiz" -LogonType S4U -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 2) -AllowStartIfOnBatteries
Register-ScheduledTask -TaskName TiaSmokeRun -Action $action -Principal $principal -Settings $settings -Force

schtasks /Query /TN TiaSmokeRun /FO LIST | Out-File 'c:\Scripts\TIA Portal\workspace\tasks-check.txt'
Stop-Transcript
