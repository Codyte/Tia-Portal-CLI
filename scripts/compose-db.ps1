# Compõe o "DB GLOBAL" a partir dos fragmentos de library/db-global/.
# SCL não tem include: o DB é um arquivo só, então a composição é textual — cabeçalho fixo +
# os ramos pedidos + rodapé. 00-core.scl entra sempre (é o contrato dos geradores).
# Uso: pwsh scripts/compose-db.ps1 motores,afericao [-Out workspace/db-global.scl]
param(
    [Parameter(Position = 0)][string[]]$Fragment,
    [string]$Out = 'workspace/db-global.scl',
    [string]$Dir = 'library/db-global'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$Dir = Join-Path $script:Repo $Dir
$names = @('00-core') + (@($Fragment) -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$files = $names | ForEach-Object {
    $f = Join-Path $Dir "$($_ -replace '\.scl$').scl"
    if (-not (Test-Path $f)) {
        $have = (Get-ChildItem $Dir -Filter *.scl).BaseName -join ', '
        throw "fragmento não existe: $_ — disponíveis: $have"
    }
    $f
}

$body = $files | ForEach-Object { "// --- $([IO.Path]::GetFileName($_)) ---"; (Get-Content $_ -Raw).TrimEnd() }
$scl = @(
    'DATA_BLOCK "DB GLOBAL"'
    "{ S7_Optimized_Access := 'TRUE' }"
    'VERSION : 0.1'
    '// Gerado por scripts/compose-db.ps1 — editar os fragmentos em library/db-global/, não este arquivo.'
    'NON_RETAIN'
    '   STRUCT'
    ($body -join "`r`n")
    '   END_STRUCT;'
    ''
    'BEGIN'
    ''
    'END_DATA_BLOCK'
) -join "`r`n"

$outPath = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $script:Repo $Out }
# BOM obrigatório: sem ele o import-source lê o acento errado e GenerateBlocksFromSource falha
# (referência a "Aferição CMD" não resolve). Medido 2026-07-28.
[IO.File]::WriteAllText($outPath, $scl, [Text.UTF8Encoding]::new($true))
Write-Host "$outPath — fragmentos: $($names -join ', ')"
