# Macro "pack": monta o zip de release a partir do build local.
#
# Por que nao roda no CI: o build referencia as DLLs do Openness (lib/), que sao licenciadas e nao
# podem ser distribuidas -- so existem numa maquina com TIA Portal instalado. O zip, esse, NAO
# carrega nenhuma DLL da Siemens: tia.exe resolve o Openness da instalacao local em runtime.
#
# O layout dentro do zip e o mesmo do repo (scripts/ + src/Tia.Cli/bin/Debug/net48/) de proposito:
# whitelist.ps1, init.ps1 e o shim derivam todos os caminhos do proprio $PSScriptRoot, entao o zip
# extraido se comporta como um checkout -- sem nenhum caminho especial.
#
# Uso: pwsh scripts/pack.ps1 [-SkipBuild] [-Publish]
param([switch]$SkipBuild, [switch]$Publish)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot

$version = ([xml](Get-Content (Join-Path $repo 'src\Directory.Build.props'))).Project.PropertyGroup.Version
if (-not $version) { throw "Version nao encontrada em src/Directory.Build.props" }
$tag = "v$version"

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'rebuild.ps1')
    if ($LASTEXITCODE -ne 0) { throw "rebuild falhou -- nao empacota build quebrado" }
}

$exe = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
if (-not (Test-Path $exe)) { throw "tia.exe ausente: rodar sem -SkipBuild" }

$stamped = (& $exe --version | ConvertFrom-Json).version
if ($stamped -notlike "$version*") {
    throw "tia.exe diz '$stamped' mas Directory.Build.props diz '$version' -- build defasado"
}

$dist = Join-Path $repo 'workspace\dist'
$stage = Join-Path $dist "tia-cli-$tag"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stage | Out-Null

# bin: tudo menos pdb (simbolo de debug nao serve pra quem so usa)
$binDst = Join-Path $stage 'src\Tia.Cli\bin\Debug\net48'
New-Item -ItemType Directory -Force -Path $binDst | Out-Null
Get-ChildItem (Split-Path $exe) -File | Where-Object { $_.Extension -ne '.pdb' } |
    Copy-Item -Destination $binDst

# Fonte da verdade do que entra: o que o Git ja rastreia. Copiar pasta inteira arriscaria levar
# junto payload gitignored (library/blocks, workspace, Scripts_Siemens) para um artefato publico.
Push-Location $repo
try { $tracked = git ls-files -- scripts docs library SKILL.md README.md LICENSE CHANGELOG.md CLAUDE.md }
finally { Pop-Location }
if ($LASTEXITCODE -ne 0 -or -not $tracked) { throw "git ls-files nao devolveu nada -- rodar de dentro do repo" }
foreach ($rel in $tracked) {
    $dst = Join-Path $stage ($rel -replace '/', '\')
    New-Item -ItemType Directory -Force -Path (Split-Path $dst) | Out-Null
    Copy-Item (Join-Path $repo $rel) $dst -Force
}

# Guarda de licenca: nenhuma DLL da Siemens pode entrar no artefato publicado.
$siemens = @(Get-ChildItem $stage -Recurse -Filter 'Siemens.*' -File)
if ($siemens) { throw "ABORTADO: DLL da Siemens no pacote -- $($siemens.Name -join ', ')" }

$zip = Join-Path $dist "tia-cli-$tag.zip"
Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Remove-Item $stage -Recurse -Force

$sha = (Get-FileHash $zip -Algorithm SHA256).Hash
Write-Host "pack ok: $zip ($([math]::Round((Get-Item $zip).Length / 1KB)) KB)"
Write-Host "sha256: $sha"

if ($Publish) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "gh CLI ausente -- publicar a mao no GitHub" }
    $notes = "Ver [CHANGELOG.md](https://github.com/Codyte/Tia-Portal-CLI/blob/main/CHANGELOG.md).`n`n" +
             "``sha256: $sha```n`n" +
             "Instalar: extrair e rodar ``pwsh scripts/init.ps1`` (sem .NET SDK -- o binario ja vem pronto)."
    gh release create $tag $zip --title "tia-cli $tag" --notes $notes
    if ($LASTEXITCODE -ne 0) { throw "gh release create falhou" }
    Write-Host "release $tag publicada"
}
