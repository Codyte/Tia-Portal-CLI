# Handoff · TIA Portal Openness API · 2026-08-10 (2ª sessão do dia)

## Goal
FP-02 pelo caminho da casa (zero SCL autoral): exercitar os 7 verbos `--apply` que nunca
construíram planta nenhuma. Oráculo: `audit` 6/6 + `compile` 0/0. Faltam 3 verbos.

## State
- HEAD: `2f3896a` (6 commits locais **não pushados**).
- Live state: **TIA Portal aberto** (2 processos, sessão 1) com `workspace/blind/FP02/FP02.ap21`
  aberto e salvo, compile 0/0. Shell do agente na sessão 0 (rota da task). O `tia.exe` foi
  rebuildado 2x nesta sessão — **o hash mudou e o diálogo modal de autorização já foi aceito**;
  novo `rebuild.ps1` com o Portal aberto pede outro clique.
- Done: tabelas de I/O do caderno (6, em `1. I/OS/QA-00` e `QA-01`); tabelas de instrumento
  (4, `MEDIDOR_* (<TAG>)` em `2. Alarmes/2.1` e `2.2`); 5 moldes movidos de `0 Moldes` para a
  pasta da lei; **`gen-fault-ob --apply` exercitado pela 1ª vez** (OB_DIAG_QA_00 e _QA_01, 11
  módulos cada); 2 defeitos corrigidos e commitados.
- In progress: nada mid-flight. `gen-alarm-fc` só rodou em dry, e o dry mostra um problema (ver
  Next steps 1).

## Decisions (and why)
- **`ResolveTagFolder`/`ResolveTypeFolder` ganharam o longest-match que `ResolveFolder` já tinha**
  (`WalkFolders`, [Ops.cs:68](../src/Tia.Core/Ops.cs#L68)). Sem isso, `--folder "1. I/OS/QA-00"`
  criava as pastas `1. I` + `OS`. Foi o que permitiu mover molde para
  `5. Instrumentação / Atuadores/5.1 Aferição Analógica/5.1.0 Molde` (nome de pasta com barra E
  espaços).
- **`gen-fault-ob` ganhou `Devices` no config** (mapa QA → nome do device,
  [FaultOb.cs:129](../src/Tia.Core/FaultOb.cs#L129)). Ele só varria `DeviceUserGroup` `HW_QA-*`, e
  o Openness não move device para grupo depois de criado — projeto montado pelo CLI nunca teria
  grupo. Config em `workspace/fp02-faultob.json`.
- **Chave do mapa é `QA_00`/`QA_01` (underscore), não `QA-00`** — o `qaName` vira `Component` do
  FlgNet e tem que casar o membro da DB global (`HARDWARE_INTERRUPT.ALARMES_MODULOS.QA_00`). O nome
  do OB sai igual nos dois casos (o gerador troca `-` por `_`).
- **`TargetBlocksFolder` do `replicate-instruments` é nome simples, não caminho** — `Ops.FindGroup`
  busca por nome recursivamente. `"5.1 Aferição Analógica"`, não o caminho inteiro. Mesma coisa para
  `TemplateFolder` do `gen-alarm-fc`.
- **Sem comentário nas tags importadas** — o aceite é `audit` + `compile`; `<Comment>` multilíngue
  seria risco de import por zero ganho.
- Descartado fechar o Portal por `Stop-Process` (classifier bloqueia) — pedir ao user.

## Next steps (ordered)
1. **`gen-alarm-fc`: o dry casa a área errada na DB global.** Área 2 → `struct: "PRELIMINAR"` (que
   é o ramo de *instrumentação*, herdado do molde) e área 1 → `ELEVATORIA_DE_ESGOTO_BRUTO`, que não
   existe na DB (os ramos são `AREA_01`/`AREA_02`). Decidir antes de aplicar: ou renomear os ramos
   da DB para o nome da área, ou ensinar o config a mapear área → struct. **Não aplicar como está.**
2. **`replicate-instruments` reprova com "Could not identify the template's mold instrument".**
   Causa achada: o `MOLDE_ANALOGS` do FP-02 referencia `INSTR_01` (a lib genérica renomeou o
   `FQIT-01` do projeto real), e nenhuma tag fonte tem esse prefixo. Opções: (a) tabela de tags
   `INSTR_01_*` numa das áreas, (b) reescrever o molde trocando `INSTR_01` por um instrumento real
   (`LIT-01`), como o `replicate-fc` faz. `ExtractId` para no `_`, então o Id de `INSTR_01_x` é
   `INSTR` — casa no DB por substring.
3. `standardize-tags` (doctor já passa, 12 memory sets).
4. `audit` 6/6 + `compile` 0/0 final + `save-project`; registrar a rodada em
   `docs/teste-cego/resultado-2026-08-10.md` (2 achados desta sessão já estão no commit `2f3896a`).
5. `git push` dos 6 commits.
6. Passo nunca provado: `use-project.ps1` com o Portal **fechado** (fix do `taskrun` do commit
   `213dae4`). A tentativa desta sessão morreu antes, em `use-project.ps1:8` — `Test-Path` com
   caminho relativo falha porque o pwsh filho não nasce no repo; **passar caminho absoluto**.

## Key files
- `workspace/fp02-faultob.json` / `fp02-instr.json` — configs dos geradores (o do fault-ob está
  provado; o de instrumentos ainda reprova no passo 2).
- `workspace/fp02-io/*.xml`, `workspace/fp02-instr/*.xml` — as 10 tabelas importadas.
- `workspace/fp02-io-tables.json`, `fp02-moldes.json`, `fp02-molde2.json`, `fp02-faultob-run.json`
  — batches já rodados; reaproveitar o formato.
- `docs/teste-cego/caderno-FP-02.md` — o memorial da rodada.
- `docs/PADRAO.md:36-58` — a árvore de pastas da lei (onde cada molde tem que morar).
- `src/Tia.Core/AlarmFc.cs` — o do passo 1. `src/Tia.Core/InstrumentFc.cs:125-193` — descoberta de
  área/instrumento e escolha do molde (passo 2).
- `src/__navi__.md` — **desatualizado** há 2 sessões; regenerar com `pwsh scripts/navi-cs.ps1`.

## Open / blockers
- **`import-source` sem BOM = mojibake silencioso** (`AferiÃ§Ã£o CMD`), erro de compile longe da
  causa. Vale um gate no verbo; segue aberto.
- **`run --script` exige projeto já aberto** — batch não pode começar com `create-project`/
  `open-project`. Não corrigido.
- `WalkFolders` não tem teste offline (precisa de `PlcSoftware` vivo); foi validado só em runtime,
  no import das tabelas e no move dos moldes.

## Skills
- tia
- ponytail
- caveman

## Effort
**Médio** para o passo 1 — é decisão de design (renomear ramo da DB × mapear no config), com blast
radius na DB global que todos os FCs referenciam; ler `AlarmFc.cs` antes de escolher. Sobe pra
**alto** se o passo 2 exigir reescrever o molde de instrumentos. O relógio não é gargalo aqui: cada
chamada `tia` custa ~10-20 s, não os 2-4 min do open.
