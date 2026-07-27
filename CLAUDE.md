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
  - `pwsh scripts/prep-project.ps1 <Nome>` = use-project + doctor + compile --apply + save
    (projeto real chega sem compilar — rodar antes de qualquer export).
  - `pwsh scripts/raio-x.ps1 <Nome>` = banho read-only → `workspace/<proj>/` (doctor, snapshot,
    devices, tags, types, plc-navi.md, AML, xref dos OBs).
  - `pwsh scripts/clone-hw.ps1 <Origem> <Destino> [-Apply]` = copia hardware via CAx/AML.
  - `tia run --script ops.json` = batch de verbos, attach 1x. Fluxo FINAIS completo em dry:
    `tia run --script docs/examples/gen-all.json`.
    **Não isola steps**: sem try/catch por item, a 1ª exceção aborta o batch e descarta os
    resultados já obtidos. Pra bateria onde falha é esperada, rodar um verbo por vez.
  - `tia doctor` = preflight dos 6 verbos antes de qualquer smoke.
- Smoke test exige TIA Portal aberto com projeto de teste — confirmar com o usuário antes.

## Sessão 0 × sessão 1 (por que `tia` não roda direto do agente)

O agente vive na **sessão 0** do Windows (isolada de serviços, `UserInteractive=False`). TIA Portal
e o desktop vivem na **sessão 1**. `TiaPortal.GetProcesses()` não enxerga processo de outra sessão,
então `Attach()` a partir do shell do agente **sempre** devolve
`"No running TIA Portal instance found"` — mesmo com o portal aberto na tela. É fronteira do SO,
não configuração; `--no-ui` não resolve (só troca o erro de modo pelo de whitelist).

**Canal correto — task `TiaSmokeRun`** (`LogonType Interactive` = executa na sessão 1):

1. escrever os args em `workspace/taskio/cmd.json` (array JSON, ex. `["doctor"]`);
2. `Start-ScheduledTask -TaskName TiaSmokeRun` — funciona do shell do agente, sem UAC;
3. poll de `workspace/taskio/exit.txt`; saída em `workspace/taskio/out.txt`.

O runner é `scripts/taskrun.ps1`. Não exige janela interativa aberta pelo user
(`scripts/smokeloop.ps1` é rota alternativa, útil só pra ver a saída ao vivo).
O portal só morre junto com a task se tiver sido *iniciado por ela* (fica na árvore de processos);
portal aberto à mão pelo user sobrevive.

Whitelist stale = `EngineeringSecurityException`. Refazer com
`Start-ScheduledTask -TaskName TiaWhitelist` (SYSTEM, sem UAC); `rebuild.ps1` já compara contra
o hash gravado no registro e falha alto se continuar divergente.

## Economia de tokens

- Sem spawn de agentes por padrão (repo pequeno; navi resolve). Sem workflows.
- `/handoff` + `/clear` no fim de cada fase ou >~150k de contexto.
- Atualizar tabela de fases do PLANO ao encerrar sessão de trabalho.
