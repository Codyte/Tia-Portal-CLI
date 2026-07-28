# NAV INDEX
# 1-9    header / protocolo
# 10-20  le cmd.json (array cru OU {id,args}), resolve sufixo do run-id
# 21-33  executa tia.exe (ou macro ps1) com stdout/stderr SEPARADOS, grava exit por ultimo
# Runner da task "TiaSmokeRun" (LogonType Interactive = sessao 1, onde o TIA vive;
# de S4U/sessao 0 o Attach() nunca enxerga o portal — ver CLAUDE.md).
# Cliente = scripts/tia.ps1 (Invoke-Tia). Protocolo: cmd.json entra ->
# out-<id>.txt / err-<id>.txt / exit-<id>.txt saem (exit por ultimo = sinal de fim).

$repo = Split-Path $PSScriptRoot
$dir = Join-Path $repo 'workspace\taskio'
New-Item -ItemType Directory -Force $dir | Out-Null
$tia = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

$raw = Get-Content "$dir\cmd.json" -Raw | ConvertFrom-Json
Remove-Item "$dir\cmd.json" -ErrorAction SilentlyContinue
# @() obrigatorio: cmd.json de 1 verbo vira string, e splat de string enumera CHARS
# ("doctor" -> 'd','o',...). Nao usar $args (automatica) como nome.
# testar a propriedade, nao o tipo: `-is [pscustomobject]` e verdadeiro ate pra string
if ($null -ne $raw.args) { $id = $raw.id; $tiaArgs = @($raw.args) }
else { $id = $null; $tiaArgs = @($raw) }
$sfx = if ($id) { "-$id" } else { '' }   # sem id = uso manual (cmd.json array), nomes antigos

Set-Location $repo
# tia.exe escreve UTF-8 (Console.OutputEncoding no Program.Main). Se o host decodifica em
# codepage OEM, "Elevatória" chega "Elevat?ria" no out-<id>.txt e o cliente nunca recupera.
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
try {
    if ($tiaArgs[0] -eq '--script-ps1') {
        # macro-verbo (raio-x/prep-project/...) precisa rodar INTEIRO na sessao 1: chama tia varias vezes
        $rest = @($tiaArgs | Select-Object -Skip 2)
        & pwsh -NoProfile -File $tiaArgs[1] @rest 1> "$dir\out$sfx.txt" 2> "$dir\err$sfx.txt"
    } else {
        # separados de proposito: contrato do CLI e stdout=JSON / stderr=log humano
        & $tia @tiaArgs 1> "$dir\out$sfx.txt" 2> "$dir\err$sfx.txt"
    }
    $code = $LASTEXITCODE
} catch {
    # sem isto uma falha antes do exe (exe ausente, redirect invalido) nunca grava exit e o
    # cliente so descobre no timeout
    $_.Exception.Message | Out-File "$dir\err$sfx.txt" -Encoding utf8
    $code = 99
}
$code | Out-File "$dir\exit$sfx.txt" -Encoding ascii
