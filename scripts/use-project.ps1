# Macro "use-project": garante que o projeto pedido está aberto no TIA.
# Já aberto → no-op. Outro aberto → close (sem save por padrão) + open (2-4 min).
# Uso: pwsh scripts/use-project.ps1 SmokeTest_01 [-Save]   (nome curto em proj\ ou caminho .ap21)
param([Parameter(Mandatory)][string]$Name, [switch]$Save)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$file = if (Test-Path $Name -PathType Leaf) { (Resolve-Path $Name).Path }
        else { Get-ChildItem (Join-Path $script:Repo "proj\$Name") -Filter *.ap2* -ErrorAction Stop |
               Select-Object -First 1 -ExpandProperty FullName }
$target = [IO.Path]::GetFileNameWithoutExtension($file)

$info = try { Invoke-Tia info | ConvertFrom-Json } catch { $null }
if ($info.project -eq $target) { Write-Host "já aberto: $target"; exit 0 }
if ($info.project) {
    Write-Host "fechando: $($info.project)$($Save ? ' (com save)' : ' (sem save)')"
    # sem ternario/splat aqui: array vazio splatado vira argumento "" pro CLI
    if ($Save) { Invoke-Tia close-project --save | Out-Null } else { Invoke-Tia close-project | Out-Null }
}
Write-Host "abrindo: $target (2-4 min)..."
Invoke-Tia open-project --file $file
if ($LASTEXITCODE) { Write-Host "open-project falhou (exit $LASTEXITCODE): $target" -ForegroundColor Red }
exit $LASTEXITCODE
