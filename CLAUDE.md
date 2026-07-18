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

- Solução em `src/`, target net48 x64.
- **Binário oficial = Debug** (`src\Tia.Cli\bin\Debug\net48\tia.exe`): a whitelist do Openness
  registra esse caminho/hash. Release compila, mas exige rodar `scripts/whitelist.ps1` de novo
  se for usado — na prática, usar sempre Debug. Pós-rebuild: whitelist elevada (UAC).
- Testes offline (sem TIA): `src\Tia.Tests\bin\Debug\net48\Tia.Tests.exe` — assert-based,
  cobre os geradores XML puros contra `docs/examples/`.
- Smoke test exige TIA Portal V19 aberto com projeto de teste — confirmar com o usuário antes.

## Economia de tokens

- Sem spawn de agentes por padrão (repo pequeno; navi resolve). Sem workflows.
- `/handoff` + `/clear` no fim de cada fase ou >~150k de contexto.
- Atualizar tabela de fases do PLANO ao encerrar sessão de trabalho.
