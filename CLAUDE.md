# TIA Portal Openness API — instruções do repo

**Toda sessão: ler `docs/PLANO.md` (decisões + fase atual) e `__navi__.md` antes de qualquer coisa.**

## Regras duras

- Decisões D1–D9 do PLANO valem — não rediscutir sem motivo novo.
- `Scripts_Siemens/FINAIS/` = referência read-only. `Scripts_Siemens/OLD/` = não tocar.
- Verbos de escrita: dry-run por padrão, `--apply` explícito.
- Nunca rodar `tia` em paralelo (Openness single-session).
- Nunca commitar `Siemens.Engineering.dll`.
- Testes só contra projeto TIA de teste, nunca produção.

## Build / run (a partir da F1)

- Solução em `src/`, target net48 x64. Binário oficial = Debug (`src\Tia.Cli\bin\Debug\net48\tia.exe`).
- **Macro-verbos — usar SEMPRE em vez da coreografia manual:**
  - `pwsh scripts/rebuild.ps1` = build + testes offline + whitelist (UAC só se tia.exe mudou).
    Nunca rodar dotnet build/whitelist/testes soltos.
  - `pwsh scripts/use-project.ps1 <Nome|caminho.ap21> [-Save]` = garante projeto aberto
    (no-op se já aberto; fecha o atual sem save por padrão; open leva 2-4 min → background).
  - `tia run --script ops.json` = batch de verbos, attach 1x.
  - `tia doctor` = preflight dos 6 verbos antes de qualquer smoke.
- Smoke test exige TIA Portal aberto com projeto de teste — confirmar com o usuário antes.

## Economia de tokens

- Sem spawn de agentes por padrão (repo pequeno; navi resolve). Sem workflows.
- `/handoff` + `/clear` no fim de cada fase ou >~150k de contexto.
- Atualizar tabela de fases do PLANO ao encerrar sessão de trabalho.
