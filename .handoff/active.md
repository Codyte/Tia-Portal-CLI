# Handoff · TIA Portal Openness API · 2026-08-10 (3ª sessão do dia)

## Goal
Rodada FP-02 do teste cego — **encerrada**. Oráculo batido: `audit` 6/6, `compile` Success 0/0,
projeto salvo. Nada mid-flight.

## State
- HEAD: `69154d6`, **pushado** (`origin/main` igual). Working tree limpo fora do `.handoff/`.
- Live state: **TIA Portal aberto** (2 processos, sessão 1) com `workspace/blind/FP02/FP02.ap21`
  aberto e salvo. Shell do agente na sessão 0 (rota da task `TiaSmokeRun`). `tia.exe` rebuildado
  nesta sessão, whitelist refeita — novo `rebuild.ps1` com o Portal aberto pode pedir clique no
  diálogo modal de autorização.
- Done: os 7 verbos `--apply` exercitados; 14 defeitos corrigidos (12 de gerador + 2 de infra);
  `docs/teste-cego/resultado-2026-08-10.md` e a tabela de rodadas do `PLANO.md` fechados.
- In progress: nada.

## Decisions (and why)
- **Os passos 1 e 2 do handoff anterior estavam obsoletos** — os dois configs já traziam o fix
  (`Structs` mapeando área → `AREA_01`/`AREA_02`; `MoldInstrumentId: INSTR_01`) e os blocos já
  estavam aplicados. `diff-block` deu `identical: true` contra o bloco no projeto, então o
  `action: "in-sync"` do dry é honesto. **Nada de renomear ramo da DB global** — a suposição mora no
  config, que é o padrão de correção da rodada inteira.
- **`Start-Process -Wait` fora do runner da task** ([taskrun.ps1:49](../scripts/taskrun.ps1#L49)):
  o `-Wait` espera o processo **e os descendentes**, e o Portal iniciado pelo `tia.exe` é
  descendente — pendurava até o timeout de 600 s com o projeto já aberto e correto na tela. Agora
  `-PassThru` + `$p.WaitForExit()`. O fix de `213dae4` tinha tratado só o pipe de stdout.
- **`list-blocks --folder` passou a casar fragmento de caminho** com barras nas pontas
  ([Inventory.cs:90](../src/Tia.Core/Inventory.cs#L90)), em vez de prefixo da raiz. Era `count: 0`
  silencioso para nome de folha, e foi o que me fez suspeitar do `in-sync` errado.
- **Descartado matar a task com `Stop-ScheduledTask`** para destravar a rodada pendurada: o Portal
  vive na árvore de processos da task e morreria junto. `Stop-Process` só no pwsh do runner libera
  a task (`State: Ready`) e o Portal sobrevive.
- Nada de teste offline para o filtro novo nem para `WalkFolders`: os dois precisam de `PlcSoftware`
  vivo. Validados em runtime (folha → 9 blocos com subpastas; `"3.1"` sozinho → 0).

## Next steps (ordered)
1. **Gate de BOM no `import-source`** — sem BOM o XML/SCL entra com mojibake silencioso
   (`AferiÃ§Ã£o CMD`) e o erro só aparece no compile, longe da causa. É o defeito aberto mais
   antigo da rodada.
2. `run --script` exige projeto já aberto — batch não pode começar com `create-project` /
   `open-project`. Decidir se o `run` passa a aceitar esses dois verbos (attach lazy) ou se fica
   documentado como limitação.
3. **FP-03 de verdade cega**: o veredito "um agente sem contexto consegue" segue não provado — duas
   rodadas, nenhuma cega (as duas herdaram handoff de quem escreveu o caderno). Precisa de sessão
   nova, sem handoff, só com o caderno e o `SKILL.md`.

## Key files
- `docs/teste-cego/resultado-2026-08-10.md` — os 11 achados numerados da rodada + a seção "Aberto".
- `docs/PLANO.md:986-1001` — tabela de rodadas do teste cego (linha FP-02 já fechada).
- `workspace/fp02-alarm.json`, `fp02-instr.json`, `fp02-stdtags-cfg.json`, `fp02-faultob.json` —
  os configs provados dos geradores; `fp02-final.json` = o batch do oráculo (standardize dry +
  audit + compile --apply + save).
- `scripts/taskrun.ps1` — runner da rota da sessão 0 (o fix do `-Wait`).
- `src/__navi__.md` — **regenerado nesta sessão** (`pwsh scripts/navi-cs.ps1`).

## Open / blockers
- `import-source` sem BOM = mojibake silencioso (passo 1).
- `run --script` não pode abrir/criar projeto (passo 2).
- Filtro de `list-blocks --folder` e `WalkFolders` sem teste offline (precisam de `PlcSoftware`).

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo** para o passo 1 — é um gate de encoding num verbo só, e a causa já está diagnosticada; o
piso é ler quem chama `import-source` antes de mexer (`Ops`/`Program.cs` e os macros). Sobe pra
**médio** se o gate exigir reescrever o arquivo em vez de recusar. O relógio não é gargalo: cada
chamada `tia` custa ~10-20 s, e um `rebuild.ps1` com o Portal aberto pede clique na tela.
