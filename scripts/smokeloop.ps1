# NAV INDEX
# 1-9    header
# 10-16  setup: dirs, banner com grupos do token
# 17-31  loop: poll cmd.json -> roda tia -> out.txt/exit.txt -> apaga cmd.json
# Iniciado UMA vez pelo user via runas (logon fresco = grupo Openness + sessao interativa
# = attach no TIA UI + popup whitelist visivel). Mesmo protocolo taskio do taskrun.ps1.
# Parar: fechar a janela ou criar workspace\taskio\stop.txt.

$repo = Split-Path $PSScriptRoot
$dir = Join-Path $repo 'workspace\taskio'
$tia = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
New-Item -ItemType Directory -Force $dir | Out-Null
Write-Host "=== smokeloop ativo. Token:" -ForegroundColor Green
whoami /groups | Select-String Openness
Write-Host "Aguardando cmd.json em $dir (stop.txt encerra)..." -ForegroundColor Green

while ($true) {
    if (Test-Path "$dir\stop.txt") { break }
    if (Test-Path "$dir\cmd.json") {
        $raw = Get-Content "$dir\cmd.json" -Raw | ConvertFrom-Json
        Remove-Item "$dir\cmd.json"
        # mesmo protocolo do taskrun.ps1: {id,args} do Invoke-Tia, ou array cru (uso manual)
        # testar a propriedade, nao o tipo: `-is [pscustomobject]` e verdadeiro ate pra string
        if ($null -ne $raw.args) { $cmdArgs = @($raw.args); $sfx = "-$($raw.id)" }
        else { $cmdArgs = @($raw); $sfx = '' }
        Write-Host ">> tia $($cmdArgs -join ' ')" -ForegroundColor Cyan
        # Start-Process com redirect p/ arquivo: filho TIA herda handle mas -Wait espera
        # so tia.exe (nao EOF de pipe) — sem travar quando open-project deixa TIA aberto.
        # Nome unico por rodada: com nome fixo o handle herdado pelo TIA lockava o arquivo.
        $quoted = $cmdArgs | ForEach-Object { '"' + $_ + '"' }
        try {
            $p = Start-Process -FilePath $tia -ArgumentList $quoted `
                -WorkingDirectory $repo -NoNewWindow -Wait -PassThru `
                -RedirectStandardOutput "$dir\out$sfx.txt" -RedirectStandardError "$dir\err$sfx.txt" `
                -ErrorAction Stop
            $p.ExitCode | Out-File "$dir\exit$sfx.txt" -Encoding ascii
            Write-Host "<< exit $($p.ExitCode)" -ForegroundColor Cyan
        } catch {
            $_.Exception.Message | Out-File "$dir\err$sfx.txt" -Encoding utf8
            "99" | Out-File "$dir\exit$sfx.txt" -Encoding ascii
            Write-Host "<< loop error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    Start-Sleep -Milliseconds 500
}
