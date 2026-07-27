# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
Publicação GitHub concluída (F4). Sessão seguiu pra melhorias de onboarding: README quick-start
+ `scripts/init.ps1` (bootstrap 1a vez). Próximo passo real identificado: **smoke test dos
verbos pendentes contra TIA real**, não features novas.

## State
- HEAD: fdd06fb
- Done:
  - Publicação GitHub (F4 fechado): `tia-cli` público, histórico scrubado de `Scripts_Siemens/`
    via `git-filter-repo` (verificado com clone fresh — vazio em working tree e histórico).
    Descrição + topics setados. PLANO.md atualizado.
  - README melhorado: Quick start linear (gates → clone → init → doctor), nota sobre popup
    consent Openness no 1º attach.
  - `scripts/init.ps1` criado e testado nesta máquina: checa gate 1 (grupo Windows
    `Siemens TIA Openness` via `WindowsPrincipal.IsInRole`, não `whoami /groups` — quebra em
    Git Bash), gate 2 (.NET SDK), gate 3 (copia `lib/*.dll` Siemens.Engineering da instalação
    local do TIA, busca dinâmica `Portal V*` sob Program Files, sem hardcode de versão); se
    todos passam, chama `rebuild.ps1`. Reporta e para com instrução exata se um gate falha
    (não finge sucesso). Commitado + pushado.
- In progress: nada mid-flight — ponto de parada limpo.

## Decisions (and why)
- `--init` vira macro PowerShell (`scripts/init.ps1`), não flag do `tia.exe` — ovo-galinha:
  exe não existe/não tá whitelisted antes do 1º build, não pode se auto-inicializar.
- Gate 1 (grupo Windows) e "TIA Portal instalado" ficam **reportados, não auto-corrigidos** —
  exigem admin + logoff/logon ou instalador Siemens, fora do que um script pode fazer sozinho.

## Next steps (ordered)
1. **Smoke test dos verbos marcados "smoke pendente" no `docs/PLANO.md` backlog v2** (linhas
   125-150), contra TIA Portal real com projeto de teste aberto:
   - `import-source`, `import-ladder` (SCL→LAD)
   - `create-folder`/`delete-folder`/`delete-block`, `import-type`
   - `add-device`/`set-address`/`connect-subnet`, `export-cax`/`import-cax`
   - F1 propriamente dito (linha 171 do PLANO — ainda pendência aberta)
2. Documentar achados no estilo Fase A/B (`docs/projeto-real-fase-A.md` já existe como modelo).
3. Secundário/cosmético: 2 ressalvas de error-handling do code-review F3 (Standardize rebuild,
   FaultOb import sem try/catch por item) — não bloqueiam, resolver se sobrar tempo.
4. F5 (MCP server) **não** é próximo passo — só cogitar se D1 cair (uso remoto/claude.ai surgir).

## Key files
- `docs/PLANO.md:123-163` — backlog v2, cada item com nota "smoke pendente" ou risco específico.
- `docs/PLANO.md:171` — pendência aberta: smoke F1 na máquina do TIA.
- `scripts/init.ps1` — bootstrap novo, único ponto de entrada pra máquina nova.
- `README.md` — Quick start atualizado, macros table com `init.ps1`.

## Open / blockers
- Smoke exige TIA Portal aberto com projeto de teste — sempre confirmar com o user antes
  (regra do CLAUDE.md do repo). Nenhum blocker de decisão pendente.
