# Macro "rebuild": build Debug + testes offline + whitelist elevada SÓ se tia.exe mudou.
# Uso: pwsh scripts/rebuild.ps1 [-SkipTests] [-WhitelistOnly]
# -WhitelistOnly: só o passo da whitelist, sem build. É o que a instalação a partir do zip de
# release usa — lá o tia.exe já vem pronto e não há .NET SDK nem lib/ para compilar.
param([switch]$SkipTests, [switch]$WhitelistOnly)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

function Get-ExeHash64 {
    [Convert]::ToBase64String(
        [System.Security.Cryptography.SHA256]::Create().ComputeHash([IO.File]::ReadAllBytes($exe)))
}
# Verdade = o hash gravado no registro, nao o delta do build: um build que nao mudou tia.exe
# mas cujo whitelist anterior falhou deixava o registro stale indefinidamente (aconteceu, 9 dias).
# TODAS as versoes (V19 e V21 coexistem) e as DUAS chaves (Entry/EntryLocal, ambas escritas pelo
# whitelist.ps1): stale = qualquer uma divergente, ou nenhuma existindo.
function Test-WhitelistStale {
    $h = Get-ExeHash64
    $reg = @(Get-ChildItem 'HKLM:\SOFTWARE\Siemens\Automation\Openness' -ErrorAction SilentlyContinue |
        ForEach-Object { $v = $_.PSPath
            foreach ($n in 'Entry', 'EntryLocal') {
                (Get-ItemProperty (Join-Path $v "Whitelist\tia.exe\$n") -Name FileHash -ErrorAction SilentlyContinue).FileHash
            } })
    return ($reg.Count -eq 0) -or [bool]@($reg | Where-Object { $_ -ne $h })
}

if (-not $WhitelistOnly) {
    dotnet build (Join-Path $repo 'src') -c Debug --nologo -v q
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

if (-not $SkipTests -and -not $WhitelistOnly) {
    & (Join-Path $repo 'src\Tia.Tests\bin\Debug\net48\Tia.Tests.exe')
    if ($LASTEXITCODE -ne 0) { exit 1 }

    # --out-file: unico check offline possivel (Print e' do Tia.Cli, Tia.Tests so' linka Tia.Core).
    # --help passa pelo mesmo Print de todo verbo.
    $tmp = Join-Path ([IO.Path]::GetTempPath()) 'tia-outfile-check.json'
    Remove-Item $tmp -ErrorAction SilentlyContinue
    $stub = & $exe --help --out-file $tmp | ConvertFrom-Json
    if (-not (Test-Path $tmp)) { Write-Host 'FAIL --out-file nao escreveu o arquivo' -ForegroundColor Red; exit 1 }
    if ($stub.file -ne $tmp -or $stub.bytes -le (($stub.head).Length)) {
        Write-Host 'FAIL --out-file: stub nao resumiu (bytes <= head)' -ForegroundColor Red; exit 1
    }
    Write-Host '  ok  Cli.--out-file (stdout vira stub, JSON completo no arquivo)'

    # auto-spill: saida acima do teto derrama sozinha, e --full desliga. Teto por env pra caber no
    # --help; se o default de 60k voltar a valer sempre, o agente leva 821 KB de find no contexto.
    $env:TIA_MAX_STDOUT = '500'
    try {
        $spill = & $exe --help | ConvertFrom-Json
        if (-not $spill.autoSpill) { Write-Host 'FAIL auto-spill nao disparou acima do teto' -ForegroundColor Red; exit 1 }
        if (-not (Test-Path $spill.file)) { Write-Host 'FAIL auto-spill nao escreveu o arquivo' -ForegroundColor Red; exit 1 }
        $whole = & $exe --help --full | ConvertFrom-Json
        if ($whole.autoSpill -or -not $whole.usage) { Write-Host 'FAIL --full nao dumpou inteiro' -ForegroundColor Red; exit 1 }
    } finally { Remove-Item Env:TIA_MAX_STDOUT }
    if ($spill.file) { Remove-Item $spill.file -ErrorAction SilentlyContinue }
    Write-Host '  ok  Cli.auto-spill (acima do teto derrama sozinho; --full desliga)'

    # run --script valida o script ANTES do attach: erro de uso nao pode custar sessao nem exigir
    # portal aberto. Se a validacao voltar pra dentro do using(Attach()), este check falha aqui.
    $bad = Join-Path ([IO.Path]::GetTempPath()) 'tia-badscript-check.json'
    '[["create-project","--dir","x","--name","y"]]' | Set-Content -LiteralPath $bad -Encoding utf8
    $out = & $exe run --script $bad 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0) { Write-Host 'FAIL run --script aceitou create-project como step' -ForegroundColor Red; exit 1 }
    if ($out -notmatch 'attaches once') {
        Write-Host "FAIL run --script: erro nao ensina a saida (veio: $out)" -ForegroundColor Red; exit 1
    }
    Write-Host '  ok  Cli.run --script (open/create-project recusado antes do attach, com a saida na mensagem)'

    # Resolve-RealPath sustenta 3 gates do init -Check. Se ele passar a devolver tudo igual, os
    # gates viram sempre-ok e param de detectar checkout no lugar errado — falso positivo silencioso.
    . (Join-Path $PSScriptRoot 'init.ps1') -Check:$false -DotSourceOnly
    # partir do caminho JA resolvido: chamar o rebuild pelo junction (~/.claude/skills/tia) fazia
    # $repo entrar com link e o gate de identidade reprovar sozinho — falso FAIL, nao regressao
    $plain = Join-Path (Resolve-RealPath $repo) 'scripts'
    if ((Resolve-RealPath $plain) -ine [IO.Path]::GetFullPath($plain)) {
        Write-Host 'FAIL Resolve-RealPath alterou caminho sem link' -ForegroundColor Red; exit 1
    }
    if ((Resolve-RealPath $plain) -ieq (Resolve-RealPath $repo)) {
        Write-Host 'FAIL Resolve-RealPath achatou caminhos diferentes' -ForegroundColor Red; exit 1
    }
    Write-Host '  ok  init.Resolve-RealPath (sem link: identidade; caminhos distintos seguem distintos)'

    # roteamento do --study: chave com acento nunca casa (o mapa e comparado dobrado) e palavra
    # curta ("ob") nao pode casar dentro de outra ("robotico"). Offline, nao precisa do Portal.
    $py = Get-Command python -ErrorAction SilentlyContinue
    if ($py) {
        $out = & python (Join-Path $PSScriptRoot 'tia-help.py') --selftest 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "FAIL tia-help --selftest`n$out" -ForegroundColor Red; exit 1
        }
        Write-Host '  ok  study.roteamento (mapa sem acento na chave; casamento por palavra inteira)'
    } else {
        Write-Host '  --  study.roteamento pulado (python fora do PATH)' -ForegroundColor DarkGray
    }
}

# referencia de verbo derivada do proprio help — evita grep em Program.cs pra achar assinatura
if (-not $WhitelistOnly) { & (Join-Path $PSScriptRoot 'gen-verbs.ps1') }

if (Test-WhitelistStale) {
    # Task TiaWhitelist roda como SYSTEM/Highest: sem UAC, disparavel por qualquer sessao.
    if (Get-ScheduledTask -TaskName TiaWhitelist -ErrorAction SilentlyContinue) {
        Start-ScheduledTask -TaskName TiaWhitelist
        $limit = (Get-Date).AddSeconds(30)
        while ((Test-WhitelistStale) -and (Get-Date) -lt $limit) { Start-Sleep -Milliseconds 500 }
    } else {
        Start-Process pwsh -Verb RunAs -Wait -ArgumentList '-NoProfile','-File',(Join-Path $repo 'scripts\whitelist.ps1')
    }
    if (Test-WhitelistStale) {
        Write-Host 'ATENCAO: whitelist AINDA stale — Openness vai recusar tia.exe' -ForegroundColor Red
        Write-Host 'Falta a task TiaWhitelist? rodar elevado 1x: pwsh -File scripts\setup-tasks.ps1' -ForegroundColor Red
        exit 1
    }
    Write-Host 'rebuild ok — whitelist refeita'
} else {
    Write-Host 'rebuild ok — whitelist ja bate com tia.exe'
}
