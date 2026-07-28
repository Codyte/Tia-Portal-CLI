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

dotnet build (Join-Path $repo 'src') -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) { exit 1 }

if (-not $SkipTests) {
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
}

# referencia de verbo derivada do proprio help — evita grep em Program.cs pra achar assinatura
& (Join-Path $PSScriptRoot 'gen-verbs.ps1')

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
