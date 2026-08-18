# NAV INDEX
# 1-8    header
# 10-16  caminhos
# 18-34  copia protegida do whitelist.ps1 em %ProgramData%\tia-cli (ACL admin) — ver comentario la
# 36-48  registra TiaWhitelist (dono = usuario, RunLevel Highest -> agente pode dispara-la)
# 50-58  ACL da task: read+execute pro usuario, senao o agente nao consegue dispara-la
# 60-72  registra TiaSmokeRun / TiaSimHost (token do usuario, Interactive)
# 74-78  confere e roda o whitelist
# Run elevated, once. Idempotent.

$repo = Split-Path $PSScriptRoot
New-Item -ItemType Directory -Force (Join-Path $repo 'workspace') | Out-Null
Start-Transcript (Join-Path $repo 'workspace\setup-log.txt') -Force
$pwsh = (Get-Command pwsh).Source
$wl = Join-Path $PSScriptRoot 'whitelist.ps1'
$tr = Join-Path $PSScriptRoot 'taskrun.ps1'

# A TiaWhitelist roda com token ELEVADO e o usuario pode dispara-la sem UAC (e' o que evita um
# prompt por rebuild). Se a acao apontasse pro whitelist.ps1 de dentro do repo -- que vive no
# perfil do usuario e e' gravavel sem admin -- qualquer processo rodando como o usuario reescreveria
# o arquivo, dispararia a task e teria execucao elevada de graca: a ACL da task protege a ACAO,
# nao o SCRIPT que a acao executa. Por isso a task executa uma COPIA em %ProgramData%\tia-cli com
# heranca cortada e escrita so' para Administradores/SYSTEM (usuario le e executa, nao altera).
# O caminho do repo vai por argumento (-Repo), gravado dentro da acao, que o usuario tambem nao
# reescreve. init.ps1 (Test-TasksCurrent) confere que a copia continua identica ao original.
$secureDir = Join-Path $env:ProgramData 'tia-cli'
$secureWl = Join-Path $secureDir 'whitelist.ps1'
New-Item -ItemType Directory -Force $secureDir | Out-Null
# O:BA nao e' enfeite: o DONO de um objeto sempre pode reescrever a DACL dele (WRITE_DAC
# implicito), entao deixar o usuario como dono devolveria de graca a escrita que a ACL tirou.
# Dono = Administradores; P = heranca cortada; BU (usuarios) so' le e executa.
# Pasta E arquivo: o arquivo copiado nasce com o usuario como CREATOR OWNER.
$sddl = 'O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)(A;OICI;FRFX;;;BU)'
$sd = New-Object Security.AccessControl.DirectorySecurity
$sd.SetSecurityDescriptorSddlForm($sddl)
Set-Acl -Path $secureDir -AclObject $sd
Copy-Item $wl $secureWl -Force
$fsd = New-Object Security.AccessControl.FileSecurity
$fsd.SetSecurityDescriptorSddlForm('O:BAG:BAD:PAI(A;;FA;;;BA)(A;;FA;;;SY)(A;;FRFX;;;BU)')
Set-Acl -Path $secureWl -AclObject $fsd
Write-Host "copia protegida: $secureWl (escrita so' para Administradores/SYSTEM)"

# sem trigger: /SC ONCE com hora passada some sozinho depois de rodar (o Windows apaga tarefa
# ONCE expirada) e o rebuild caia no fallback RunAs, que da sessao 0 nao mostra UAC nenhum.
# Dona = o proprio usuario (nao SYSTEM): task de SYSTEM so aceita Start-ScheduledTask de um token
# elevado, entao o shell do agente levava "Acesso negado" e todo rebuild exigia terminal do usuario.
# RunLevel Highest usa o token elevado do usuario sem prompt de UAC — o whitelist precisa de HKLM.
Register-ScheduledTask -TaskName TiaWhitelist -Force `
    -Action (New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$secureWl`" -Repo `"$repo`"") `
    -Principal (New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType S4U -RunLevel Highest)

# task criada por processo elevado nasce com SD que so admins leem/executam — sem isto o shell do
# agente (token filtrado) leva "Acesso negado" ate no Get-ScheduledTask. FRFX = read+execute apenas:
# o usuario dispara, mas nao reescreve a acao da task sem elevar.
$sid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$svc = New-Object -ComObject Schedule.Service
$svc.Connect()
$svc.GetFolder('\').GetTask('TiaWhitelist').SetSecurityDescriptor(
    "D:P(A;;FA;;;BA)(A;;FA;;;SY)(A;;FRFX;;;$sid)", 0)

$action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$tr`""
# Interactive (nao S4U): S4U roda numa sessao propria e TiaPortal.GetProcesses() nao enxerga o
# portal da sessao 1 -> "No running TIA Portal instance found" mesmo com o portal aberto na tela.
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 2) -AllowStartIfOnBatteries
Register-ScheduledTask -TaskName TiaSmokeRun -Action $action -Principal $principal -Settings $settings -Force

# TiaSimHost: segura a instancia do PLCSIM Advanced viva na sessao 1 (o `tia sim-run` da attach
# nela). Mesmo principal Interactive, pelo mesmo motivo — da sessao 0 a API do PLCSIM nao enxerga
# o Runtime Manager. Sem limite de tempo: o host so sai no `sim-host.ps1 -Stop`.
# powershell.exe (nao pwsh): o assembly do PLCSIM e net48, e o caminho nao tem espaco.
$sh = Join-Path $PSScriptRoot 'sim-host.ps1'
Register-ScheduledTask -TaskName TiaSimHost -Force `
    -Action (New-ScheduledTaskAction -Execute "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" `
        -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$sh`"") `
    -Principal $principal `
    -Settings (New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries)

schtasks /Query /TN TiaSmokeRun /FO LIST | Out-File (Join-Path $repo 'workspace\tasks-check.txt')

# ja estamos elevados: roda o whitelist agora, senao o proximo tia morre com EngineeringSecurityException
& $secureWl -Repo $repo
Stop-Transcript
