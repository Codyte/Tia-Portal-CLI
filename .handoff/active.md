# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
Bateria de smoke dos verbos pendentes do backlog v2 contra TIA real — **concluída**.
Próximo tema: fechar F1 e os `--apply` que ficaram fora (ladder, hardware).

## State
- HEAD: 9f3de0d — 5 arquivos modificados **não commitados** (`CLAUDE.md`, `docs/PLANO.md`,
  `scripts/rebuild.ps1`, `scripts/taskrun.ps1`, `.handoff/`).
- Done:
  - **Canal autônomo resolvido.** `Start-ScheduledTask -TaskName TiaSmokeRun` funciona do shell
    do agente e executa na sessão 1. **Não precisa da janela do `smokeloop`** — o handoff anterior
    concluiu errado; o que estava quebrado era o runner, não o acesso. Protocolo documentado no
    `CLAUDE.md` (seção "Sessão 0 × sessão 1").
  - **Bateria completa** contra `Automação ETE SG AsBuilt_1_V21` (PLC `CPU CCO`, 21 devices,
    1011 blocos): `doctor`, `import-source`, `import-ladder`, `create-folder`, `delete-folder`,
    `delete-block`, `import-type`, `export-type`, `add-device`, `set-address`, `connect-subnet`,
    `export-cax`, `import-cax`, `import-block`, `import-tags`, `xref`, `compile`, `run --script`.
  - `compile --apply` + `save-project` rodados no AsBuilt (autorizado pelo user): 0 errors,
    3 warnings. Destravou `export-type` (sem compilar, Openness recusa UDT inconsistente).
  - `create-folder`/`import-block`/`delete-block`/`delete-folder` com `--apply` exercitados contra
    alvo descartável `ZZ_Smoke` e revertidos — 1011 blocos antes e depois.
  - Fixes: `taskrun.ps1` (splat de string enumerava chars → verbo virava `d`);
    `rebuild.ps1` (comparava delta do build em vez do hash do registro → whitelist ficou stale
    9 dias sem ninguém notar; agora compara com o registro, usa a task `TiaWhitelist` sem UAC e
    sai 1 gritando se continuar divergente). Validado: `rebuild.ps1` → `ALL PASS` + refez whitelist.
  - `PLANO.md` atualizado (itens 1, 1b, 2, 3, 6 + seção "Bugs abertos").
- In progress: nada mid-flight.

## Decisions (and why)
- **Task `TiaSmokeRun` > `smokeloop.ps1`** — autônoma, sem janela do user, e o portal sobrevive
  (só morre se tiver sido iniciado pela própria task). `smokeloop` fica como rota alternativa
  pra ver saída ao vivo.
- **Um verbo por vez, não `run --script`** — o batch não isola steps: 1ª exceção aborta e
  **descarta** os resultados já colhidos (`Program.cs:148-155`). Ruim pra bateria onde falha
  é esperada.
- **Sem doc "Fase C"** — achados foram pro `PLANO.md` e `CLAUDE.md`; doc separado seria a
  terceira cópia dos mesmos fatos.
- `compile --apply` no AsBuilt autorizado pelo user (é o que `prep-project.ps1` já faz).

## Next steps (ordered)
1. Commitar os 5 arquivos (o user perguntou e a sessão acabou antes da resposta).
2. **Bug aberto**: `import-block` dry dá **falso positivo** em XML que não é bloco — aceitou
   `StdBombaA.xml` (root `SW.Tags.PlcTagTable`) com exit 0 e `action: create`; só o `--apply`
   quebrou. Fix: validar root element antes de reportar `action`. Detalhe em `docs/PLANO.md`,
   seção "Bugs abertos".
3. `--apply` não exercitados: `import-ladder` (detalhes FlgNet escritos de memória seguem
   não validados) e hardware (`set-address`/`connect-subnet`/`add-device` — atributos Node e
   CreateIoSystem). Exigem alvo descartável; considerar `SmokeTest_01`.
4. F1 propriamente dito (`docs/PLANO.md:171`).
5. Melhoria de UX achada no smoke: `--device` quer nome de estação
   (`S7-1500/ET200MP station_1`), mas `info`/`doctor` só reportam `plc` (`CPU CCO`) — o erro
   `Device 'X' not found` não sugere o nome próximo.
6. Ressalvas de error-handling do code-review F3 (Standardize rebuild, FaultOb import sem
   try/catch por item) — não bloqueiam.

## Key files
- `scripts/taskrun.ps1` — runner da task, protocolo taskio. Ponto de entrada real.
- `CLAUDE.md` (seção "Sessão 0 × sessão 1") — por que `tia` não roda direto e como disparar.
- `scripts/rebuild.ps1:8-20` — hash do exe × hash do registro.
- `docs/PLANO.md` — backlog v2 atualizado + "Bugs abertos".
- `workspace/taskio/` — `cmd.json` entra, `out.txt` + `exit.txt` saem.
- Helpers efêmeros no scratchpad (recriar se sumir): `tiacmd.ps1` (1 verbo),
  `battery.ps1 -Plan X.json -OutDir Y` (lista de verbos, 1 attach cada, resumo 1 linha/verbo).

## Open / blockers
- **Projeto AsBuilt ficou compilado e salvo** — estado diferente do que estava no disco antes
  (existe um `.backup` ao lado em `proj/`). Intencional e autorizado.
- Portal segue aberto na sessão 1 com o AsBuilt. Fechar sem salvar não perde nada (o último
  save foi antes das mutações de smoke, que já foram revertidas).
- O classifier de permissões barra planos que contenham `delete-*` mesmo em dry-run —
  contornar rodando o subconjunto sem delete e pedindo a palavra do user pros deletes.
- `tia.exe` foi rebuildado no fim da sessão; whitelist já refeita e conferida.
