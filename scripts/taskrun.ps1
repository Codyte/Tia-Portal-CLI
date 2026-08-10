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
    # Start-Process e nao `& exe 1> arquivo`: com o operador de redirecionamento o PowerShell le o
    # stdout do filho por PIPE e copia pro arquivo, e o TIA Portal que o tia.exe INICIA herda a
    # ponta de escrita desse pipe e vive depois que o tia.exe sai — o PS nunca ve EOF, fica
    # pendurado e o exit-<id>.txt (sinal de fim) nunca e gravado: open-project sem portal rodando
    # so terminava no timeout de 600s, sem devolver a saida (medido 2026-08-10, run f3547442).
    # Com -RedirectStandardOutput o filho recebe um handle de ARQUIVO; o portal ate o herda, mas
    # handle de arquivo nao segura ninguem — por isso o nome carrega o run-id.
    # separados de proposito: contrato do CLI e stdout=JSON / stderr=log humano
    $exe, $rest = if ($tiaArgs[0] -eq '--script-ps1') {
        # macro-verbo (raio-x/prep-project/...) precisa rodar INTEIRO na sessao 1: chama tia varias vezes
        'pwsh', (@('-NoProfile', '-File', $tiaArgs[1]) + @($tiaArgs | Select-Object -Skip 2))
    } else { $tia, @($tiaArgs) }
    # -ArgumentList com ARRAY nao cita nada: "MOTOR_AREA_01 (MOTOR_01)" chegaria como 2 argumentos.
    # Citacao no padrao CommandLineToArgvW (aspas dobram as barras que as precedem).
    $line = ($rest | ForEach-Object {
        $a = [string]$_
        if ($a -match '[\s"]') { '"' + (($a -replace '(\\+)"', '$1$1"' -replace '"', '\"') -replace '(\\+)$', '$1$1') + '"' }
        else { $a }
    }) -join ' '
    $p = Start-Process -FilePath $exe -ArgumentList $line -NoNewWindow -Wait -PassThru `
        -RedirectStandardOutput "$dir\out$sfx.txt" -RedirectStandardError "$dir\err$sfx.txt"
    $code = $p.ExitCode
} catch {
    # sem isto uma falha antes do exe (exe ausente, redirect invalido) nunca grava exit e o
    # cliente so descobre no timeout
    $_.Exception.Message | Out-File "$dir\err$sfx.txt" -Encoding utf8
    $code = 99
}
$code | Out-File "$dir\exit$sfx.txt" -Encoding ascii
