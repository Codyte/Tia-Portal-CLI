# ====================== BEGIN NAV INDEX ======================
# NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
#   L48    Get-ExeHash
#   L53    Test-Whitelisted
#   L65    Resolve-RealPath
#   L87    Test-SkillInstalled
#   L93    Test-TasksCurrent
#   L122   Show
# ======================= END NAV INDEX =======================

# NAV INDEX
# 26-38   header / caminhos / descoberta do Portal instalado
# 40-72   helpers (hash do exe, whitelist do registro, repo==skill, task apontando pro repo)
# 74-109  -Check: relatorio read-only dos 8 gates + estado vivo, exit 1 se faltar gate
# 111-116 gate 1: grupo Windows Siemens TIA Openness (nao automatizavel — admin + logoff/logon)
# 118-124 gate 2: .NET SDK presente
# 126-143 gate 3: lib/*.dll (build-time) — copia da instalacao local do TIA Portal
# 146-149 gates falharam -> para com instrucoes
# 151-163 gate 4: tasks TiaWhitelist/TiaSmokeRun/TiaSimHost (setup-tasks elevado, 1 UAC) — re-registra se o
#         caminho gravado na task diverge deste repo (repo movido mata a rota da sessao 0)
# 165-166 rebuild.ps1 (build + testes offline + whitelist)
# 168-180 gate 5: shim tia no PATH do usuario + TIA_CLI_HOME
# 182-188 gate 6: o repo *e* a skill (~/.claude/skills/tia) — so verifica, nao copia
#
# Macro "init": bootstrap de 1a vez numa maquina nova. Verifica os 2 gates que so um humano
# resolve (grupo + logon, TIA Portal instalado) e automatiza o resto (lib/, build, whitelist,
# PATH). Idempotente — re-rodar depois de git pull.
# Uso: pwsh scripts/init.ps1  |  pwsh scripts/init.ps1 -Check
# -DotSourceOnly: define as funcoes e sai sem tocar em nada (o rebuild testa Resolve-RealPath).
param([switch]$Check, [switch]$DotSourceOnly)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
$skillDst = Join-Path $HOME '.claude\skills\tia'
$ok = $true
# Instalacao a partir do zip de release: tia.exe ja vem compilado e o fonte nao vem junto. Sem
# fonte nao ha o que buildar, entao os gates que so existem pro build (.NET SDK, lib/*.dll do
# Openness) nao se aplicam -- o exe resolve as DLLs da instalacao local do Portal em runtime.
$prebuilt = -not (Test-Path (Join-Path $repo 'src\Tia.Cli\Tia.Cli.csproj'))

$libDir = Join-Path $repo 'lib'
$dllNames = @('Siemens.Engineering.Base.dll', 'Siemens.Engineering.Step7.dll', 'Siemens.Engineering.WinCCUnified.dll',
              'Siemens.Engineering.WinCC.dll', 'Siemens.Engineering.Startdrive.dll')
$programFiles = [Environment]::GetFolderPath('ProgramFiles')
# INST-02: so' V19+ conta como gate. V17/V18 instalados passavam como "TIA Portal ok" e o build
# quebrava depois, na copia das DLLs (o layout PublicAPI/Vxx/net48 e' de V19+).
$allPortalDirs = Get-ChildItem (Join-Path $programFiles 'Siemens\Automation') -Directory -Filter 'Portal V*' -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending
$portalDirs = @($allPortalDirs | Where-Object { [int](($_.Name -replace '[^0-9]', '')) -ge 19 })
$unsupportedPortals = @($allPortalDirs | Where-Object { [int](($_.Name -replace '[^0-9]', '')) -lt 19 })

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

function Resolve-RealPath([string]$p) {
    # Junction/symlink no meio do caminho faz comparacao por string mentir: aqui ~/.claude/skills
    # e Junction pra ~/.agents/skills, entao o MESMO checkout tem dois nomes e os gates 7-9
    # acusavam falta numa maquina correta -- mandando "mover o checkout", que quebraria tudo.
    # .NET so resolve link na folha; o junction pode estar em qualquer segmento, dai a subida.
    if (-not $p) { return $p }
    $leaves = @()
    $cur = [IO.Path]::GetFullPath($p)
    while ($cur) {
        $item = Get-Item -LiteralPath $cur -Force -ErrorAction SilentlyContinue
        if ($item -and $item.LinkType -in 'Junction', 'SymbolicLink' -and $item.Target) {
            $cur = [IO.Path]::GetFullPath(@($item.Target)[0])
            continue                              # o alvo tambem pode ser link
        }
        $parent = Split-Path $cur -Parent
        if (-not $parent) { break }               # chegou na raiz do volume
        $leaves = , (Split-Path $cur -Leaf) + $leaves
        $cur = $parent
    }
    if ($leaves) { [IO.Path]::GetFullPath((Join-Path $cur ($leaves -join '\'))) } else { $cur }
}

function Test-SkillInstalled {
    # O repo *e* a skill (submodulo de Codyte/skills): Claude Code le ~/.claude/skills/tia/SKILL.md.
    # Nada e copiado — checkout fora desse caminho deixa a skill desatualizada em silencio.
    (Resolve-RealPath $repo).TrimEnd('\') -ieq (Resolve-RealPath $skillDst).TrimEnd('\')
}

function Test-TasksCurrent {
    # A task grava o caminho absoluto do taskrun.ps1. Mover o repo mata a rota da sessao 0
    # ("No running TIA Portal instance found") ate re-registrar — sintoma identico ao de portal fechado.
    $here = Resolve-RealPath $PSScriptRoot
    foreach ($n in 'TiaSmokeRun', 'TiaSimHost') {
        $t = Get-ScheduledTask -TaskName $n -ErrorAction SilentlyContinue
        if (-not $t) { return $false }
        # a task guarda o caminho como foi registrado; comparar depois de resolver os dois lados
        $arg = ([regex]::Match($t.Actions.Arguments, '"([^"]+\\scripts)\\[^"\\]+\.ps1"')).Groups[1].Value
        if (-not $arg -or (Resolve-RealPath $arg) -ine $here) { return $false }
    }
    # TiaWhitelist e' o caso separado: roda elevada sem UAC, entao a acao aponta pra COPIA em
    # %ProgramData%\tia-cli (ACL de admin), nao pro script do repo — que o usuario poderia
    # reescrever para ganhar execucao elevada de graca. Aqui conferem-se as duas metades: a task
    # recebe ESTE repo em -Repo, e a copia nao ficou pra tras de um git pull que mudou o original.
    $t = Get-ScheduledTask -TaskName TiaWhitelist -ErrorAction SilentlyContinue
    if (-not $t) { return $false }
    $repoArg = ([regex]::Match($t.Actions.Arguments, '-Repo\s+"([^"]+)"')).Groups[1].Value
    if (-not $repoArg -or (Resolve-RealPath $repoArg) -ine (Resolve-RealPath $repo)) { return $false }
    $copy = Join-Path $env:ProgramData 'tia-cli\whitelist.ps1'
    if (-not (Test-Path $copy)) { return $false }
    (Get-FileHash $copy).Hash -eq (Get-FileHash (Join-Path $PSScriptRoot 'whitelist.ps1')).Hash
}

if ($DotSourceOnly) { return }

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
    if ($prebuilt) { Write-Host "ok     .NET SDK / lib -- nao se aplica (instalacao de release, tia.exe ja compilado)" -ForegroundColor Green }
    else {
        Show '.NET SDK' ([bool](dotnet --version 2>$null)) 'instalar .NET SDK 8'
        Show 'lib/*.dll (build-time)' (-not ($dllNames | Where-Object { -not (Test-Path (Join-Path $libDir $_)) })) `
            'rodar init.ps1 sem -Check (copia da instalacao local)'
    }
    Show "TIA Portal V19+ instalado ($(if ($portalDirs) { $portalDirs.Name -join ', ' } else { 'nenhum' })$(if ($unsupportedPortals) { " | fora de suporte: $($unsupportedPortals.Name -join ', ')" }))" `
        ([bool]$portalDirs) 'TIA Portal V19+ com Openness'
    Show "tia.exe$(if (Test-Path $exe) { ' (' + (Get-Item $exe).LastWriteTime.ToString('yyyy-MM-dd HH:mm') + ')' })" `
        (Test-Path $exe) 'pwsh scripts/rebuild.ps1'
    Show 'whitelist do registro bate com o hash atual' (Test-Whitelisted) `
        "$(if (Test-Path $exe) { 'Start-ScheduledTask -TaskName TiaWhitelist' } else { 'sai junto com o build: pwsh scripts/rebuild.ps1' })"
    Show 'tasks TiaWhitelist/TiaSmokeRun/TiaSimHost apontando pra este repo' (Test-TasksCurrent) `
        'rodar init.ps1 sem -Check (1 UAC)'
    Show 'shim tia no PATH do usuario' `
        (([Environment]::GetEnvironmentVariable('Path', 'User') -split ';' |
            Where-Object { $_ } | ForEach-Object { Resolve-RealPath $_ }) -contains (Resolve-RealPath $PSScriptRoot)) `
        'rodar init.ps1 sem -Check'
    Write-Host ""
    Write-Host "estado vivo (nao e gate):"
    # Lugar do checkout nao e gate: o proprio init.ps1 so avisa e instala assim mesmo. O CLI roda
    # de qualquer diretorio; o que depende do lugar e o Claude Code carregar a skill.
    Write-Host "  repo em ~/.claude/skills/tia (= a skill): $(if (Test-SkillInstalled) { 'sim' } `
        else { "nao ($repo) -- o CLI roda, mas o Claude Code nao carrega a skill deste checkout" })"
    Write-Host "  sessao do shell: $((Get-Process -Id $PID).SessionId)  (0 = roteia pela task TiaSmokeRun)"
    $portal = @(Get-Process -Name 'Siemens.Automation.Portal' -ErrorAction SilentlyContinue)
    Write-Host "  TIA Portal rodando: $(if ($portal) { "$($portal.Count) (sessao $($portal.SessionId -join ','))" } else { 'nenhum' })"
    $al = @(Get-ChildItem (Join-Path $repo 'src\Tia.Lib') -Recurse -Filter '*.al2?' -File -ErrorAction SilentlyContinue)
    Write-Host "  biblioteca .al21: $(if ($al) { "$($al[0].Name) ($([math]::Round($al[0].Length / 1KB)) KB)" } else { 'ausente (gitignored) -- so install-lib precisa dela; assar com bake-lib.ps1 -Plc X a partir de um projeto que ja tenha a biblioteca' })"
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

if ($prebuilt) {
    if (-not (Test-Path $exe)) {
        Write-Warning "Nem fonte nem tia.exe neste diretorio. O zip de release foi extraido pela metade?"
        exit 1
    }
    Write-Host "gates 2 e 3: pulados (instalacao de release -- tia.exe ja compilado, sem build)"
    if (-not $portalDirs) {
        Write-Warning "TIA Portal V19+ com Openness nao encontrado em Program Files. O CLI so roda com o Portal instalado."
        $ok = $false
    }
}

$dotnetVer = if ($prebuilt) { $null } else { (dotnet --version 2>$null) }
if (-not $prebuilt) {
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
# PLCSIM Advanced nao mora com o Openness: a API fica em Common Files (x86), versionada por pasta.
# Sem ela o `sim-run` nao compila; com ela, o build copia a DLL pro lado do exe (Private=true no csproj).
$simDest = Join-Path $libDir 'Siemens.Simatic.Simulation.Runtime.Api.x64.dll'
if (-not (Test-Path $simDest)) {
    $simFound = Get-ChildItem (Join-Path ${env:ProgramFiles(x86)} 'Common Files\Siemens\PLCSIMADV\API') -Directory -ErrorAction SilentlyContinue |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName 'Siemens.Simatic.Simulation.Runtime.Api.x64.dll' } |
        Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($simFound) { Copy-Item $simFound $simDest; Write-Host "lib/: copiado API do PLCSIM Advanced de $simFound" }
    else { $missing += 'Siemens.Simatic.Simulation.Runtime.Api.x64.dll (S7-PLCSIM Advanced instalado?)' }
}

if ($missing.Count -gt 0) {
    Write-Warning "DLLs Siemens.Engineering nao encontradas (TIA Portal V21+ com Openness instalado?): $($missing -join ', ')"
    $ok = $false
} else {
    Write-Host "gate 3 ok: lib/ populada"
}
}

if (-not $ok) {
    Write-Host "init incompleto -- resolva os gates acima e rode 'pwsh scripts/init.ps1' de novo." -ForegroundColor Yellow
    exit 1
}

# tasks TiaWhitelist/TiaSmokeRun/TiaSimHost: unico passo que exige elevacao (HKLM + registro de task).
# rebuild.ps1 depende da TiaWhitelist; sem ela cai no fallback RunAs, que da sessao 0 nao mostra UAC.
# Re-registra tambem quando a task aponta pro caminho de um checkout antigo (repo movido).
if (-not (Test-TasksCurrent)) {
    Write-Host "registrando tasks TiaWhitelist/TiaSmokeRun/TiaSimHost para $PSScriptRoot — vai pedir UAC uma vez"
    Start-Process pwsh -Verb RunAs -Wait -ArgumentList `
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'setup-tasks.ps1')
    # INST-07: conferir as tres tasks (acao, ACL e caminho), nao so' a existencia da TiaWhitelist --
    # setup parcial passava e a linha seguinte anunciava as tres como registradas.
    if (-not (Test-TasksCurrent)) {
        Write-Warning "setup-tasks nao deixou as tasks no estado esperado — ver workspace\setup-log.txt"
        exit 1
    }
}
Write-Host "gate 4 ok: tasks TiaWhitelist/TiaSmokeRun/TiaSimHost registradas"

& (Join-Path $repo 'scripts\rebuild.ps1') -WhitelistOnly:$prebuilt
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

# gate 6: o repo *e* a skill — nada a copiar, so verificar o lugar.
if (Test-SkillInstalled) {
    Write-Host "gate 6 ok: repo em $skillDst = skill tia"
} else {
    Write-Warning "repo esta em $repo, nao em $skillDst -- o Claude Code nao vai carregar a skill deste checkout."
    Write-Host "  mover: git clone/submodule add em ~/.claude/skills/tia e rodar init.ps1 la (1 checkout so: a whitelist e por caminho do exe)."
}

Write-Host ""
Write-Host "init ok. Abra TIA Portal com um projeto (sessao interativa) e rode: tia doctor" -ForegroundColor Green
Write-Host "conferir a instalacao a qualquer momento: pwsh scripts/init.ps1 -Check"
