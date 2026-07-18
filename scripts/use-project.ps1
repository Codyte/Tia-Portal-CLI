# Macro "use-project": garante que o projeto pedido está aberto no TIA.
# Já aberto → no-op. Outro aberto → close (sem save por padrão) + open (2-4 min).
# Uso: pwsh scripts/use-project.ps1 SmokeTest_01 [-Save]   (nome curto em proj\ ou caminho .ap21)
param([Parameter(Mandatory)][string]$Name, [switch]$Save)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

$file = if (Test-Path $Name -PathType Leaf) { (Resolve-Path $Name).Path }
        else { Get-ChildItem (Join-Path $repo "proj\$Name") -Filter *.ap2* -ErrorAction Stop |
               Select-Object -First 1 -ExpandProperty FullName }
$target = [IO.Path]::GetFileNameWithoutExtension($file)

$info = try { & $exe info 2>$null | ConvertFrom-Json } catch { $null }
if ($info.project -eq $target) { Write-Host "já aberto: $target"; exit 0 }
if ($info.project) {
    Write-Host "fechando: $($info.project)$($Save ? ' (com save)' : ' (sem save)')"
    & $exe close-project @($Save ? @('--save') : @()) | Out-Null
}
Write-Host "abrindo: $target (2-4 min)..."
& $exe open-project --file $file
