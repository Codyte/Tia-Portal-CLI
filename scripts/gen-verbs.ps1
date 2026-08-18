# NAV INDEX
# 1-8    header: por que existe
# 9-20   roda `tia.exe --help` (sem TIA: o help imprime antes do Attach) e vira docs/VERBS.md
#
# Referencia de verbo em 1 leitura. Sem isto, descobrir a assinatura de um verbo custa
# grep em Program.cs (~5 chamadas por sessao). Fonte da verdade continua sendo o array de
# help do CLI — este arquivo e derivado, regenerado pelo rebuild.ps1.

$repo = Split-Path $PSScriptRoot
$exe  = Join-Path $repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'
if (-not (Test-Path $exe)) { throw "tia.exe ausente: $exe (rodar rebuild.ps1)" }

# mesma razao do _common.ps1: tia.exe fala UTF-8, e sem isto o help volta decodificado na
# codepage OEM do host — todo acento do VERBS.md vira mojibake na regeracao seguinte.
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$help = & $exe --help --full | ConvertFrom-Json
$sb = [Text.StringBuilder]::new()
[void]$sb.AppendLine('# Verbos do `tia` (gerado por `scripts/gen-verbs.ps1` — nao editar a mao)')
[void]$sb.AppendLine()
[void]$sb.AppendLine("``$($help.usage)``")
$verbs = 0
foreach ($p in $help.PSObject.Properties) {
    if ($p.Name -in 'usage', 'notes') { continue }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## $($p.Name)")
    foreach ($line in @($p.Value)) { [void]$sb.AppendLine("- ``$line``"); $verbs++ }
}
[void]$sb.AppendLine()
[void]$sb.AppendLine('## notas')
[void]$sb.AppendLine($help.notes)

$out = Join-Path $repo 'docs\VERBS.md'
Set-Content $out ($sb.ToString().TrimEnd() + "`n") -Encoding utf8
"gen-verbs: $out ($verbs verbos)"

# O numero de verbos aparece a mao no SKILL.md (2x) e no README — era o tipo de dado que envelhece
# calado (ficou em 69 com 72 no help). Aqui a verdade e' o help, entao o numero e' corrigido no
# lugar em vez de avisado: avisar so' empurrava a edicao a mao pro fim da sessao.
# CLAUDE.md fica de fora: la' "6 verbos" e' o preflight do doctor, nao o total.
foreach ($f in @('SKILL.md', 'README.md')) {
    $path = Join-Path $repo $f
    if (-not (Test-Path $path)) { continue }
    $text = Get-Content -LiteralPath $path -Raw
    $fixed = [regex]::Replace($text, '(\d+)(\s+verb)', { param($m) "$verbs$($m.Groups[2].Value)" })
    if ($fixed -ne $text) {
        Set-Content -LiteralPath $path $fixed -NoNewline -Encoding utf8
        Write-Host "  ${f}: contagem de verbos -> $verbs" -ForegroundColor Yellow
    }
}

# O arquivo nasce sem o header NAV INDEX que o navindex poe nos .md do repo; sem isto,
# todo rebuild sujava o git com as 17 linhas do header indo e voltando.
$navi = Join-Path $HOME '.claude/skills/navindex/scripts/navindex.py'
if (Test-Path $navi) { & python $navi $out *> $null }
