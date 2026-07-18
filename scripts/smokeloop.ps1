# NAV INDEX
# 1-9    header
# 10-16  setup: dirs, banner com grupos do token
# 17-31  loop: poll cmd.json -> roda tia -> out.txt/exit.txt -> apaga cmd.json
# Iniciado UMA vez pelo user via runas (logon fresco = grupo Openness + sessao interativa
# = attach no TIA UI + popup whitelist visivel). Mesmo protocolo taskio do taskrun.ps1.
# Parar: fechar a janela ou criar workspace\taskio\stop.txt.

$dir = "c:\Scripts\TIA Portal\workspace\taskio"
$tia = "c:\Scripts\TIA Portal\src\Tia.Cli\bin\Debug\net48\tia.exe"
New-Item -ItemType Directory -Force $dir | Out-Null
Write-Host "=== smokeloop ativo. Token:" -ForegroundColor Green
whoami /groups | Select-String Openness
Write-Host "Aguardando cmd.json em $dir (stop.txt encerra)..." -ForegroundColor Green

while ($true) {
    if (Test-Path "$dir\stop.txt") { break }
    if (Test-Path "$dir\cmd.json") {
        $cmdArgs = Get-Content "$dir\cmd.json" -Raw | ConvertFrom-Json
        Remove-Item "$dir\cmd.json"
        Write-Host ">> tia $($cmdArgs -join ' ')" -ForegroundColor Cyan
        # Start-Process com redirect p/ arquivo: filho TIA herda handle mas -Wait espera
        # so tia.exe (nao EOF de pipe) — sem travar quando open-project deixa TIA aberto.
        # result.txt (nao out.txt): out.txt ficou lockado por handle herdado pelo TIA.
        $quoted = $cmdArgs | ForEach-Object { '"' + $_ + '"' }
        try {
            $p = Start-Process -FilePath $tia -ArgumentList $quoted `
                -WorkingDirectory "c:\Scripts\TIA Portal" -NoNewWindow -Wait -PassThru `
                -RedirectStandardOutput "$dir\result.txt" -RedirectStandardError "$dir\result-err.txt" `
                -ErrorAction Stop
            $p.ExitCode | Out-File "$dir\exit.txt" -Encoding ascii
            Write-Host "<< exit $($p.ExitCode)" -ForegroundColor Cyan
        } catch {
            $_.Exception.Message | Out-File "$dir\result.txt" -Encoding utf8
            "99" | Out-File "$dir\exit.txt" -Encoding ascii
            Write-Host "<< loop error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    Start-Sleep -Milliseconds 500
}
