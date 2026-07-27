# Comando unico: roda `tia` de qualquer sessao (ver _common.ps1).
# Uso: pwsh scripts/tia.ps1 doctor
#      pwsh scripts/tia.ps1 export-block --name X --out DIR
#      pwsh scripts/tia.ps1 --script-ps1 scripts\raio-x.ps1 "Nome do Projeto"  (macro inteiro na sessao 1)
# Sem param block: $args passa tudo cru pro CLI (senao o PS engole --out/--name).
. (Join-Path $PSScriptRoot '_common.ps1')
Invoke-Tia @args
exit $LASTEXITCODE
