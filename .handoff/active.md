# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
Usar `proj/Software de ETE Insular_Inicial_V21` (projeto-molde da casa, conforme ao padrão que
gerou os scripts FINAIS) como régua executável da CLI. Regra: **CLI diverge do projeto → a CLI
está errada.** Estrutura completa em `docs/PADRAO.md`.

## State
- HEAD: 48ad388 — working tree limpo. `rebuild.ps1` ALL PASS (mas whitelist stale, ver blockers).
- Projeto de referência importado, compilado (Success/0 erros) e salvo. Banho `raio-x` completo em
  `workspace/Software de ETE Insular_Inicial_V21/`.
- `doctor` neste projeto: `standardize-tags` ok · `gen-alarm-fc` 8/8 ok · `gen-fault-ob` ok após
  correção · `gen-profinet`/`replicate-fc`/`replicate-instruments` = `skipped` (faltam configs).
- In progress: nada mid-flight. Próxima ação exige o portal (bloqueado por whitelist).

## Decisions (and why)
- Fixtures de teste são **sintéticas** e essa é a dívida principal: `docs/examples/ModuleErrorMolde.xml`
  = 4.8K com `ALARMES_MODULOS.QA-01.WORD_1`; o molde real = 14K com
  `DB GLOBAL.HARDWARE_INTERRUPT.ALARMES_MODULOS.QA-00.WORD_1.x0`. A suíte offline concordava com o
  código errado. Trocar por exports reais é pré-requisito de tudo, inclusive de um futuro `scaffold`.
- `Doctor.cs` — removido o check `alarm DB 'ALARMES_MODULOS'` (`FindBlock`): alvo é membro da DB
  global, não bloco; reprovava até em projeto 100% conforme.
- `FaultOb.RewireNetwork` — lança quando o template não tem acesso ao `AlarmDb`. Antes seguia em
  silêncio e **todo** OB gerado saía com o bit de alarme do molde.
- `--script-ps1` no `taskrun.ps1` — macro-verbo roda inteiro na sessão 1; um verbo por vez via
  taskio seria 8 round-trips por raio-x.
- `scripts/tia-task.ps1` sem `param` block de propósito: qualquer param faz o PS engolir `--out`
  (`parameter name 'out' is ambiguous`). E apaga `exit.txt` ANTES de disparar — sem isso o poll lê
  o resultado da rodada anterior (caí nisso).
- `setup-tasks.ps1` — `TiaWhitelist` sem trigger: `/SC ONCE` com hora passada é apagada pelo Windows
  depois de rodar, e o fallback `RunAs` do `rebuild.ps1` não mostra UAC nenhum na sessão 0.
- **`replicate-fc --apply` é proibido neste projeto**: replica a 1ª pasta populada de cada tipo por
  cima de **todas** as irmãs, e aqui as 34 pastas de equipamento já estão completas. Só dry.

## Next steps (ordered)
1. Desbloquear whitelist (elevado, 1x): `pwsh -File scripts\setup-tasks.ps1`.
2. **Fixtures reais** (maior alavanca). Exportar do projeto de referência para `docs/examples/`:
   `FC_Modelo`, `OB_MOLDE_ALARMES`, `OB_MOLDE_PARTIDAS`, `MOLDE_ANALOGS`, `MOLDE TOT1`,
   `DB GLOBAL`, os 6 blocos de `Soprador 1 (S-01A)`, e uma tabela de 29 tags. Já exportado:
   `workspace/padrao/MODULE_ERROR_MOLDE.xml` (14K, real). Repontar `Fixture()` e rodar `rebuild.ps1`
   — o que quebrar é bug real que a fixture sintética escondia.
3. **Fechar `doctor` 6/6**: escrever os 3 configs contra este projeto. Campos já confirmados:
   `BlocksFolder: "4. Motores/Bombas"`, UDTs `MotorDados`/`MotorPrincipal`/`ValvDados`,
   `GlobalDb: "DB GLOBAL"`, `TagTable: "DISPOSITIVOS_PROFINET"`, `TagFolder: "4. Comm"`.
   Falta ler os FINAIS para `SourceNumbersToReplace` e para o `replicate-instruments` (o exemplo
   pede `DB INSTRUMENTOS`, que aqui não existe — o padrão usa `DB GLOBAL`).
4. Dry-run dos 3 replicadores aqui (as pastas `Equipamento (TAG)` que o `replicate-fc` exige
   existem neste projeto; o AsBuilt não tinha).
5. Guard no `replicate-fc --apply`: recusar alvo que já tem blocos, salvo `--force`.
6. **`import-ladder` contra a verdade** — FlgNet escrito de memória, nunca validado. Gerar e comparar
   com `diff-block` contra um `PARTIDA_*` real.
7. Só então avaliar `scaffold`/`init` (projeto novo nascendo com as 34 FBs da biblioteca + moldes +
   árvore de pastas + `DB GLOBAL`) — ele consome os exports do passo 2.
8. Ideia aberta: verbo `audit` — conferir projeto qualquer contra a lei de nomenclatura (`(TAG)` na
   folha, 6 blocos por equipamento, 1 tabela por acionamento, N de área consistente entre
   `2.N`/`3.N`/`3.1.N`/`5.1.N`). É auditar AsBuilt contra o molde = o problema real do usuário.

## Key files
- `docs/PADRAO.md` — estrutura do projeto de referência + lei de nomenclatura + divergências.
- `docs/PLANO.md:186` — seção "Projeto de referência"; adiante, clone de acionamento (AsBuilt).
- `src/Tia.Core/FaultOb.cs:22` — comentário do `AlarmDb`; `:219` o throw novo.
- `src/Tia.Core/Doctor.cs:97` — onde o check bogus foi removido.
- `src/Tia.Core/Replicate.cs:15` — `ReplicateFcConfig` (campos do passo 3).
- `src/Tia.Tests/Program.cs:40` — `Fixture()` aponta para `docs/examples/` (passo 2).
- `scripts/tia-task.ps1` — driver sessão 0→1 (`--script-ps1 scripts\raio-x.ps1 "<Projeto>"`).
- `workspace/Software de ETE Insular_Inicial_V21/` — raio-x completo (plc-navi, snapshot, tags).

## Open / blockers
- **Whitelist stale + task `TiaWhitelist` inexistente** → qualquer `tia` recusa com
  `EngineeringSecurityException`. Passo 1 destrava; nada que toque o portal anda antes disso.
- Projeto importado chega inconsistente: `export-*` morre com `Inconsistent blocks and PLC data
  types (UDT) cannot be exported`. `prep-project` resolve (já rodado aqui).
- Openness single-session: um verbo por vez; `run --script` não isola steps.
