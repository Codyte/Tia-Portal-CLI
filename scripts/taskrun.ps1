# NAV INDEX
# 1-9    header / protocolo
# 11-17   caminhos + citacao compartilhada (_common.ps1)
# 21-58   janela do console: lembra a posicao sozinha (console.json so' pra sair do default)
# 60-105  le cmd.json (array cru OU {id,args}) e executa tia.exe (ou macro ps1), stdout/stderr SEPARADOS
# 106-111 catch (falha antes do exe ainda tem de gravar exit)
# 112-132 antes do fim: saida com show=all, posicao da janela com window=remember
# 134     exit-<id>.txt = sinal de fim, sempre por ultimo
# Runner da task "TiaSmokeRun" (LogonType Interactive = sessao 1, onde o TIA vive;
# de S4U/sessao 0 o Attach() nunca enxerga o portal — ver CLAUDE.md).
# Cliente = scripts/tia.ps1 (Invoke-Tia). Protocolo: cmd.json entra ->
# out-<id>.txt / err-<id>.txt / exit-<id>.txt saem (exit por ultimo = sinal de fim).

$repo = Split-Path $PSScriptRoot
$dir = Join-Path $repo 'workspace\taskio'
New-Item -ItemType Directory -Force $dir | Out-Null
$tia = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
. (Join-Path $PSScriptRoot '_common.ps1')   # ConvertTo-CmdLine (citacao CommandLineToArgvW)

Set-Location $repo

# --- janela do console: lembra sozinha onde ficou -------------------------------------------
# A task roda pwsh SEM -WindowStyle, entao aparece um console na sessao 1; toda a saida vai por
# redirecionamento pra arquivo, e por isso ele nascia em branco e sempre na posicao padrao do host
# (o Windows so' guarda posicao de console por atalho, e aqui nao ha atalho).
# Default = "remember" + "command": ninguem precisa configurar nada, a janela reabre onde foi
# deixada e diz o que esta rodando. A posicao e' ESTADO (console-rect.txt, escrito pelo runner),
# nao configuracao. workspace/console.json e' opcional e so' serve pra sair do default:
# {"window":"default|remember|hidden|X,Y,W,H", "show":"none|command|all"} — modelo em
# docs/examples/console.json.
$conf = $null
$confFile = Join-Path $repo 'workspace\console.json'
if (Test-Path $confFile) {
    try { $conf = Get-Content $confFile -Raw -Encoding utf8 | ConvertFrom-Json } catch { $conf = $null }
}
$window = if ($conf -and $conf.window) { "$($conf.window)" } else { 'remember' }
$show   = if ($conf -and $conf.show)   { "$($conf.show)"   } else { 'command' }
$rectFile = Join-Path $dir 'console-rect.txt'
if ($window -ne 'default') {
    # ~150 ms de Add-Type por chamada, contra ~7 s de attach do verbo mais barato. So' o
    # "default" explicito nao paga.
    Add-Type -Namespace TiaWin -Name Api -MemberDefinition @'
[DllImport("kernel32.dll")] public static extern System.IntPtr GetConsoleWindow();
[DllImport("user32.dll")] public static extern bool ShowWindow(System.IntPtr h, int cmd);
[DllImport("user32.dll")] public static extern bool SetWindowPos(System.IntPtr h, System.IntPtr after, int x, int y, int cx, int cy, uint flags);
[DllImport("user32.dll")] public static extern bool GetWindowRect(System.IntPtr h, out RECT r);
public struct RECT { public int Left, Top, Right, Bottom; }
'@
    $hwnd = [TiaWin.Api]::GetConsoleWindow()
    # rect vem do estado em "remember"; em "X,Y,W,H" vem do texto do proprio modo
    $rect = if ($window -eq 'remember') { Get-Content $rectFile -ErrorAction SilentlyContinue | Select-Object -First 1 }
            elseif ($window -match '^\s*-?\d+\s*,') { $window } else { $null }
    if ($window -eq 'hidden') { [void][TiaWin.Api]::ShowWindow($hwnd, 0) }   # SW_HIDE
    elseif ($rect) {
        $n = @("$rect" -split ',' | ForEach-Object { [int]$_.Trim() })
        # 0x14 = SWP_NOZORDER | SWP_NOACTIVATE: mover a janela nao rouba o foco de quem trabalha
        if ($n.Count -eq 4) { [void][TiaWin.Api]::SetWindowPos($hwnd, [IntPtr]::Zero, $n[0], $n[1], $n[2], $n[3], 0x14) }
    }
}

$sfx = ''   # definido antes do try: o catch grava err/exit com este sufixo
# tia.exe escreve UTF-8 (Console.OutputEncoding no Program.Main). Se o host decodifica em
# codepage OEM, "Elevatória" chega "Elevat?ria" no out-<id>.txt e o cliente nunca recupera.
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
try {
    # Leitura do cmd.json DENTRO do try: cmd.json ausente ou corrompido levantava antes daqui, o
    # exit-<id>.txt nunca era gravado e o cliente so' descobria no timeout de 600 s — exatamente
    # o modo de falha que o catch existe pra evitar.
    $raw = Get-Content "$dir\cmd.json" -Raw -ErrorAction Stop | ConvertFrom-Json
    Remove-Item "$dir\cmd.json" -ErrorAction SilentlyContinue
    # @() obrigatorio: cmd.json de 1 verbo vira string, e splat de string enumera CHARS
    # ("doctor" -> 'd','o',...). Nao usar $args (automatica) como nome.
    # testar a propriedade, nao o tipo: `-is [pscustomobject]` e verdadeiro ate pra string
    if ($null -ne $raw.args) { $id = $raw.id; $tiaArgs = @($raw.args) }
    else { $id = $null; $tiaArgs = @($raw) }
    $sfx = if ($id) { "-$id" } else { '' }   # sem id = uso manual (cmd.json array), nomes antigos

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
    # A citacao (padrao CommandLineToArgvW) vive no _common.ps1 porque o smokeloop.ps1 fala o
    # MESMO protocolo taskio: enquanto morava so' aqui, la' a linha repartia em argumento com
    # aspas ou barra final.
    $line = ConvertTo-CmdLine $rest
    # "show": e' o unico texto que aparece durante a corrida — a saida real esta' redirecionada
    # pra arquivo e so' pode ser impressa no fim.
    if ($show -ne 'none') { Write-Host "$(Get-Date -f 'HH:mm:ss') $exe $line" -ForegroundColor Cyan }
    # sem -Wait: o -Wait do Start-Process espera o processo E OS DESCENDENTES (job object), e o
    # TIA Portal que o tia.exe inicia e descendente — ficava pendurado enquanto o portal vivesse,
    # mesmo com o tia.exe ja encerrado (medido 2026-08-10, run 29c5e0eb: portal aberto na tela,
    # exit-<id>.txt nunca gravado, timeout de 600s). WaitForExit() espera SO o processo.
    $p = Start-Process -FilePath $exe -ArgumentList $line -NoNewWindow -PassThru `
        -RedirectStandardOutput "$dir\out$sfx.txt" -RedirectStandardError "$dir\err$sfx.txt"
    $p.WaitForExit()
    $code = $p.ExitCode
} catch {
    # sem isto uma falha antes do exe (exe ausente, redirect invalido) nunca grava exit e o
    # cliente so descobre no timeout
    $_.Exception.Message | Out-File "$dir\err$sfx.txt" -Encoding utf8
    $code = 99
}
# ANTES do exit-<id>.txt, e nao depois: o cliente solta o busy.lock so' quando a task ja parou
# (_common.ps1), entao runner que continua trabalhando depois do sinal de fim deixa o lock preso e
# a chamada seguinte morre em "outra chamada tia em andamento" ate o coletor de orfao (10 min).
# Sao milissegundos de leitura de arquivo e uma chamada GetWindowRect — nao ha' o que adiar.
if ($show -eq 'all') {
    foreach ($f in "$dir\out$sfx.txt", "$dir\err$sfx.txt") {
        if ((Test-Path $f) -and (Get-Item $f).Length -gt 0) { Get-Content $f -Encoding utf8 | Write-Host }
    }
    Write-Host "exit=$code" -ForegroundColor Cyan
}
# "remember" (o default): grava onde a janela ficou, e a proxima chamada nasce ali. Mover a janela
# no meio de uma corrida vale pra proxima, que e' o que se espera de "ultima posicao". Vai pro
# console-rect.txt e nao pro console.json de proposito: o json e' escrito pelo humano, e runner que
# reescreve arquivo de configuracao acaba comendo comentario e chave que nao entendeu.
if ($window -eq 'remember') {
    $r = New-Object TiaWin.Api+RECT
    if ([TiaWin.Api]::GetWindowRect($hwnd, [ref]$r) -and $r.Right -gt $r.Left) {
        ("{0},{1},{2},{3}" -f $r.Left, $r.Top, ($r.Right - $r.Left), ($r.Bottom - $r.Top)) |
            Set-Content $rectFile -Encoding ascii
    }
}

$code | Out-File "$dir\exit$sfx.txt" -Encoding ascii   # sinal de fim: por ultimo, sempre
