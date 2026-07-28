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

$help = & $exe --help | ConvertFrom-Json
$sb = [Text.StringBuilder]::new()
[void]$sb.AppendLine('# Verbos do `tia` (gerado por `scripts/gen-verbs.ps1` — nao editar a mao)')
[void]$sb.AppendLine()
[void]$sb.AppendLine("``$($help.usage)``")
foreach ($p in $help.PSObject.Properties) {
    if ($p.Name -in 'usage', 'notes') { continue }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## $($p.Name)")
    foreach ($line in @($p.Value)) { [void]$sb.AppendLine("- ``$line``") }
}
[void]$sb.AppendLine()
[void]$sb.AppendLine('## notas')
[void]$sb.AppendLine($help.notes)

$out = Join-Path $repo 'docs\VERBS.md'
Set-Content $out ($sb.ToString().TrimEnd() + "`n") -Encoding utf8
"gen-verbs: $out"
