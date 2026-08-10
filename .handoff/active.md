# Handoff · TIA Portal Openness API · 2026-08-10

## Goal
FP-02 pelo caminho da casa (zero SCL autoral): exercitar os 7 verbos `--apply` que nunca
construíram planta nenhuma. Oráculo: `audit` 6/6 + `compile` 0/0. Metade do caminho feita.

## State
- HEAD: `213dae4` (5 commits locais **não pushados**, contando os 3 da sessão anterior).
- Live state: **TIA Portal aberto (PID 14676)** com `workspace/blind/FP02/FP02.ap21` aberto e
  **salvo** (compile 0/0, audit 6/6). O PID 20068 é processo filho, não é sessão — `info --portal
  20068` reprova; só existe uma sessão, então `--portal` não é obrigatório. Shell do agente na
  sessão 0 (rota da task).
- Done: caderno FP-02 escrito; projeto criado; hardware inteiro (CPU 1515 + 2 ET200SP com
  DI/DQ/AI/servidor nos endereços do caderno + 4 G120 com telegrama 20, todos IO device do PLC);
  `install-lib` dos 4 pacotes; DB GLOBAL com os ramos das 2 áreas; **`replicate-fc --apply`
  exercitado pela 1ª vez** (5 acionamentos × 6 blocos); tabelas de tag por acionamento via
  `clone --table --at`. 4 defeitos achados e corrigidos (commits `4289164` e `213dae4`).
- In progress: nada mid-flight. Falta o teste final do fix do `taskrun` (ver Open).

## Decisions (and why)
- **`EquipmentTypes: ["("]` no `replicate-fc`** — o campo é filtro por substring no nome da pasta, e
  o único molde populado de um projeto novo é o genérico `Motor 1 (MOTOR_01)`. `"("` casa toda pasta
  de equipamento, que é a forma de dizer "todos" quando os alvos têm substantivos diferentes
  (Bomba/Peneira/Transportador). Sem isso seria um molde por substantivo.
- **Nome do drive tem que ser `INVERSOR_<TAG>_CCM_01`** — a constante do telegrama que o molde
  referencia é `<nome do device>~PROFINET_interface~Standard_telegram_20`, e o `replicate-fc` só faz
  troca textual do ID. Nome livre = constante não resolve.
- **`replicate-fc --force` com molde inconsistente destrói os alvos**: apaga os blocos do alvo antes
  de exportar o molde, e o Openness recusa exportar bloco inconsistente — os 4 alvos ficaram sem o
  FC e o estado foi salvo assim. Recuperado tornando o próprio `BG-01A` consistente (2 tags que
  faltavam) e replicando dele. **Compilar 0/0 antes de qualquer `--force`.**
- **Instrumentos ficam sob `PRELIMINAR.INSTRUMENTACAO` na DB global** mesmo os da área 1 — o nome do
  ramo é herdado do molde `MOLDE_ANALOGS`; renomear exige reimportar o molde.
- **`use-project.ps1` estava certo** — o "abre e não devolve feedback" era o `taskrun.ps1` (ver
  commit `213dae4`). Descartado mexer no macro.
- Descartado `Start-Process -ArgumentList` com **array**: não cita nada, `"X (Y)"` viraria 2
  argumentos. Linha montada no padrão `CommandLineToArgvW`, testada com espaço, aspas e barra final.

## Next steps (ordered)
1. **Provar o fix do `taskrun`**: fechar o TIA Portal e rodar
   `pwsh scripts/use-project.ps1 workspace/blind/FP02/FP02.ap21` — tem que devolver o JSON
   `{opened, path, portal:"started-with-ui"}` em 2-4 min, e não pendurar até o timeout.
2. **Tabelas de I/O do caderno** (item 4): 2 racks × (16 DI + 16 DO + 2 AI) em
   `1. I/OS/<rack>`, endereços exatos do caderno.
3. **Tabelas de instrumento** em `2. Alarmes/2.1 Elevatoria de Esgoto Bruto` e `2.2 Tratamento
   Preliminar` (padrão da casa: `MEDIDOR_<TIPO> (<TAG>)`, 8-10 tags) — é o que o
   `replicate-instruments` varre. Instrumentos: `LIT-01`, `FIT-01` (totalizado), `FIT-02`
   (totalizado), `PIT-10`.
4. **`gen-alarm-fc`** (2 áreas → `3.1.1`/`3.1.2`), **`replicate-instruments`** (aferição + os 2
   totalizadores), **`gen-fault-ob`** (QA_00 = ET200_EEB, QA_01 = ET200_TP), **`standardize-tags`**.
   Compile entre cada um.
5. `audit` 6/6 + `compile` 0/0 final + `save-project`, e registrar a rodada em
   `docs/teste-cego/resultado-2026-08-10.md` (os 6 achados desta sessão já estão nos commits).
6. `git push` dos 5 commits locais.

## Key files
- `docs/teste-cego/caderno-FP-02.md` — o memorial da rodada (2 áreas, válvula motorizada 17 pontos,
  2 totalizadores, diagnóstico das 2 periferias).
- `workspace/fp02-*.json` / `workspace/fp02-db.scl` — os batches já rodados (hw, io, blocks, tags,
  fix) e o SCL da DB global. Reaproveitar o formato para os próximos.
- `src/Tia.Core/Replicate.cs` — `ProposedBlockName` (só o bloco de chamada vira `PARTIDA_*`),
  `RewireXml` (busca de tag MODO_LOCAL no PLC inteiro), `FindCcmInfo` (`CCM_?\d+`).
- `src/Tia.Core/Clone.cs:109` — `Readdress`: bits densos, tabela mista deslocada em bloco.
- `scripts/taskrun.ps1` — `Start-Process` + citação `CommandLineToArgvW`.
- `docs/PADRAO.md` / `docs/BOAS-PRATICAS.md` — a régua de pasta/nome e a lei R1–R9.
- `src/__navi__.md` — **desatualizado** desde a sessão passada; regenerar com
  `pwsh scripts/navi-cs.ps1`.

## Open / blockers
- **`import-source` sem BOM = mojibake silencioso** (`AferiÃ§Ã£o CMD`) e erro de compile longe da
  causa. Vale um gate no verbo — hoje é armadilha aberta.
- **`run --script` exige projeto já aberto**, então batch não pode começar com
  `create-project`/`open-project` (o verbo solto funciona). Não corrigido.
- O fix do `taskrun` foi provado na citação e na não-regressão da rota da task, **não** no caso que o
  motivou (portal não rodando) — é o passo 1.

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo** para o passo 1 — é fechar o Portal e rodar um comando; o gargalo é o relógio (2-4 min de
open), não raciocínio. Sobe pra **médio** a partir do passo 3: `replicate-instruments` e
`gen-fault-ob` nunca rodaram `--apply` e dependem de tabela de tag no formato certo. Alto só se o
`gen-fault-ob` reclamar do `AlarmDb` — aí é o caso documentado em `PADRAO.md`.
