# NAV INDEX
# 1-6    header
# 7-16   runner: reads cmd.json (array of tia args), runs tia.exe, writes out.txt/exit.txt
# Runs under scheduled task "TiaSmokeRun" (LogonType Interactive = sessao 1, onde o TIA vive;
# de S4U/sessao 0 o Attach() nunca enxerga o portal — ver CLAUDE.md).
# Protocol: write cmd.json -> Start-ScheduledTask TiaSmokeRun -> poll exit.txt.

$dir = "c:\Scripts\TIA Portal\workspace\taskio"
New-Item -ItemType Directory -Force $dir | Out-Null
Remove-Item "$dir\exit.txt" -ErrorAction SilentlyContinue
$tia = "c:\Scripts\TIA Portal\src\Tia.Cli\bin\Debug\net48\tia.exe"
# @() obrigatorio: cmd.json de 1 verbo vira string, e splat de string enumera CHARS
# ("doctor" -> 'd','o',...). Nao usar $args (automatica) como nome.
$tiaArgs = @(Get-Content "$dir\cmd.json" -Raw | ConvertFrom-Json)
Set-Location "c:\Scripts\TIA Portal"
if ($tiaArgs[0] -eq '--script-ps1') {
    # macro-verbo (raio-x/prep-project/...) precisa rodar INTEIRO na sessao 1: ele chama tia varias vezes
    $rest = @($tiaArgs | Select-Object -Skip 2)
    & pwsh -NoProfile -File $tiaArgs[1] @rest *> "$dir\out.txt"
} else {
    & $tia @tiaArgs *> "$dir\out.txt"
}
$LASTEXITCODE | Out-File "$dir\exit.txt" -Encoding ascii
