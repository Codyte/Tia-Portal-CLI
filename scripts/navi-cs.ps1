# Mapa de símbolos C# -> src/__navi__.md (navindex.py só faz py/js/ps1).
# Uso: pwsh scripts/navi-cs.ps1
$ErrorActionPreference = 'Stop'
$src = Join-Path (Split-Path $PSScriptRoot -Parent) 'src'
$rx = '^\s*(public|internal)\s+.*?\b(class|struct|enum)\s+(?<n>\w+)|^\s*public\s+(static\s+)?[\w<>,\[\]\.\? ]+?\s+(?<n>\w+)\s*\(|^\s*case\s+"(?<n>[\w-]+)":'
$out = [Text.StringBuilder]::new()
[void]$out.AppendLine("# __navi__ · src/ (C#) — símbolos públicos por arquivo")
[void]$out.AppendLine("<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->")
foreach ($f in Get-ChildItem $src -Recurse -Filter *.cs | Where-Object FullName -notmatch '\\(obj|bin)\\' | Sort-Object FullName) {
    $rel = $f.FullName.Substring($src.Length + 1) -replace '\\', '/'
    $lines = Get-Content $f.FullName
    $syms = for ($i = 0; $i -lt $lines.Count; $i++) {
        $m = [regex]::Match($lines[$i], $rx)
        if ($m.Success) { "$($m.Groups['n'].Value)($($i + 1))" }
    }
    [void]$out.AppendLine("")
    [void]$out.AppendLine("## ``$rel`` ($($lines.Count) linhas)")
    [void]$out.AppendLine(($syms -join '  '))
}
$dest = Join-Path $src '__navi__.md'
Set-Content -Path $dest -Value $out.ToString() -Encoding utf8NoBOM
"navi-cs: $dest"
