# Handoff · TIA Portal Openness API · 2026-07-18

## Modo de trabalho (ativar no início da sessão)
- `/ponytail full` + `/caveman ultra` + navindex (ler `__navi__.md` antes de busca ampla).
- Binário: `C:\Scripts\TIA Portal\src\Tia.Cli\bin\Debug\net48\tia.exe` (Release é stale/V19 — NÃO usar).
- PATH fix se preciso: `$env:Path=[Environment]::GetEnvironmentVariable("Path","Machine")+";"+[Environment]::GetEnvironmentVariable("Path","User")`.

## Goal
Smoke dos 6 ports F3 contra SmokeTest_01. **4/6 FEITOS** (gen-profinet, standardize-tags,
gen-fault-ob, replicate-fc). Em curso: gen-alarm-fc (fixture pronta, TIA caiu no meio).
Depois: replicate-instruments, PLANO update, code-review ports vs FINAIS.

## State
- HEAD: 52c8ae5 + working tree sujo (fixes de smoke não commitados — commitar cedo na próxima sessão).
- **TIA caiu/fechou no meio do gen-alarm-fc.** `open-project` com UI ficou pendurado em background
  (task bn39dilry) — checar se TIA abriu; se não, matar processo TIA órfão e reabrir:
  `tia open-project --file "C:\Scripts\TIA Portal\proj\SmokeTest_01\SmokeTest_01.ap21"` (SEM --no-ui:
  headless morre quando o CLI sai; attach seguinte não acha nada — limitação conhecida agora).
- **Incerto o que sobreviveu ao crash** (último save-project = sessão anterior). Verificar com
  list-devices / find antes de re-rodar fixtures: devices INV-BH01A/B + RIO-QA01 (grupo HW_QA-01),
  tabelas "3. Partidas"/BOMBA (BH-01A/B), DB GLOBAL + MotorDados + PARTIDA_BOMBA (BH-01B),
  MODULE_ERROR_MOLDE + OB_DIAG_QA_01 + ALARMES_MODULOS. O que faltar: re-rodar passos abaixo.

### Smokes feitos (como reproduzir cada fixture)
1. **gen-profinet ✅** — precisa IO devices: `add-device --mlfb "6ES7 155-6AU01-0BN0/V4.1" --name INV-BH01A --apply`
   (idem B). Config: docs/examples/profinet.json. Dry→apply→re-run "exists" (idempotente).
2. **standardize-tags ✅** — tabelas em "3. Partidas" via `import-tags --folder "3. Partidas"` com
   docs/examples/StdBombaA.xml (populada) + StdBombaB.xml (vazia). Apply converge em 1 rebuild.
   **FIX aplicado** em Standardize.cs (AuditTable + GenerateFromTemplate): padronizar nomes ANTES de
   ordenar/alocar layout — senão nunca converge (audit seguinte ordena por nome novo).
3. **gen-fault-ob ✅** — fixture: `add-device ... --name RIO-QA01 --group HW_QA-01 --apply` (--group é
   NOVO), DB via import-source docs/examples/AlarmesModulosDb.scl, molde via import-block
   docs/examples/ModuleErrorMolde.xml (OB LAD hand-crafted: Contact slice WORD_1.%X0 + Move 999→Smoke_Int).
   Dry→apply→compile 0 err; re-run dry "override" (exige compile --apply antes: export falha em bloco
   inconsistente). **FIX aplicado** em FaultOb.cs: filtrar config.CommentCultures pelas
   LanguageSettings.ActiveLanguages do projeto (pt-BR ausente quebrava import inteiro).
4. **replicate-fc ✅** — fixture: import-source docs/examples/ReplicateFixture.scl (UDT MotorDados +
   DB GLOBAL), create-folder "4. Motores/Bombas/Bomba (BH-01A)" e "(BH-01B)", import-block
   docs/examples/BombaTemplateFc.xml --folder ".../Bomba (BH-01A)", compile. Config exemplo
   replicate-fc.json. **FIX aplicado** em Replicate.cs: BlocksFolder com "/" resolve como path
   (Ops.ResolveFolder), não nome literal. Verificado: PARTIDA_BOMBA (BH-01B) com symbols Motor_BH-01B.

### gen-alarm-fc (em curso — fixture PRONTA, não rodada)
Arquivos criados em docs/examples/: **AlarmFixture.scl** (FB BITS TO WORD 16 bits+word via slices,
DB_BITS_TO_WORD_MODELO instance, DB GLOBAL v2 com struct ETA.ALARMES.WORD_ALARMES_1/2),
**FcModeloAlarmes.xml** (FC_Modelo LAD: Call "FB BITS TO WORD" com 16 SIGNAL_Bitn wired em Smoke_Bit
+ BITS_TO_WORD→"DB GLOBAL".ETA.ALARMES.WORD_ALARMES_1), **ObMoldeAlarmes.xml** (OB_MOLDE_ALARMES
skeleton vazio), **AlarmTagsEta.xml** (tabela "INSTRUMENTOS_ALARMES (ETA)": NIVEL_MUITO_ALTO (ETA),
PRESSAO_ALTA (ETA)), **StartTagsEta.xml** (tabela "PARTIDAS (ETA)": ETA-P01_FALHA).
Passos restantes (D9: um tia por vez):
1. `delete-block --name "DB GLOBAL" --apply` (GenerateBlocksFromSource pode falhar se existir)
2. `import-source --file docs/examples/AlarmFixture.scl --apply`
3. `create-folder --path "3.1.0 Modelo" --apply`; `import-block --file docs/examples/FcModeloAlarmes.xml --folder "3.1.0 Modelo" --apply`
4. `import-block --file docs/examples/ObMoldeAlarmes.xml --apply`
5. `create-folder --path "2. Alarmes/2.1 ETA" --tags --apply`; `create-folder --path "3. Partidas/3.2 ETA" --tags --apply`
6. `import-tags --file docs/examples/AlarmTagsEta.xml --folder "2. Alarmes/2.1 ETA" --apply`; idem StartTagsEta.xml → "3. Partidas/3.2 ETA"
7. `compile --apply` (0 err esperado; se FB slice syntax falhar, tentar `#BITS_TO_WORD.%X0`)
8. `gen-alarm-fc` dry → conferir área ETA, FC_ALARMES_ETA, struct ETA → `--apply` → compile → re-run "in-sync"
Notas AlarmFc.cs: áreas = subgrupos de "2. Alarmes" pareados por GetBaseName com subgrupos de
"3. Partidas" (por isso os subgrupos 2.1/3.2 ETA); template FC precisa dos 16 wires COM IdentCon+Access
(OpenCon-only quebra rewire); RewireWordNetwork usa comment cultures pt-BR+en-US hardcoded (linha ~373)
— MESMO bug de culture do FaultOb, se import falhar aplicar mesmo fix lá.

## Decisions (and why)
- D1–D9 valem. D9 crítico: nunca 2 tia em paralelo.
- Debug build = binário de trabalho (V21 resolve via AssemblyResolve; Release não rebuiltado).
- Whitelist: TIA lembra permissão na sessão; após rebuild + NOVO processo TIA pode pedir de novo —
  task TiaWhitelist requer admin (schtasks /Run falhou "Acesso negado" sem elevação); se Openness
  bloquear, pedir ao usuário rodar elevado: `schtasks /Run /TN TiaWhitelist`.
- Fixtures de smoke ficam em docs/examples/ (commitá-las).
- CLI ganhou: `import-tags --folder A/B`, `add-device --group G` (gaps reais achados no smoke).

## Next steps (ordered)
1. Commitar working tree (fixes + fixtures) — antes de continuar smoke.
2. Verificar TIA aberto + estado do projeto (o que sobreviveu ao crash); repor o que faltar.
3. gen-alarm-fc: passos acima.
4. replicate-instruments (último port; config exemplo docs/examples/replicate-instruments.json;
   ler InstrumentFc.cs antes pra montar fixture).
5. save-project ao fim de cada smoke ok (aprendizado do crash: perdemos trabalho não salvo).
6. Atualizar PLANO (F3 → ✅) + regen navi.
7. /code-review dos ports vs Scripts_Siemens/FINAIS (pendência).

## Key files
- src/Tia.Core/Standardize.cs — fix idempotência (AuditTable ~l.384, GenerateFromTemplate ~l.352)
- src/Tia.Core/FaultOb.cs — fix cultures (Generate início)
- src/Tia.Core/Replicate.cs — fix path BlocksFolder (Run ~l.57)
- src/Tia.Core/Hardware.cs — add-device --group
- src/Tia.Core/Ops.cs — ImportTagTable --folder
- src/Tia.Cli/Program.cs — wiring dos novos flags
- docs/examples/ — todas as fixtures (Std*, ModuleErrorMolde, AlarmesModulosDb, ReplicateFixture,
  BombaTemplateFc, AlarmFixture, FcModeloAlarmes, ObMoldeAlarmes, AlarmTagsEta, StartTagsEta)

## Open / blockers
- TIA possivelmente fechado; task background bn39dilry (open-project UI) pendurada — matar/ignorar.
- Se whitelist bloquear no novo processo TIA: precisa elevação (ver Decisions).
- list-hmi/export-cax/import-cax/export-type/import-type/export-tags ainda sem smoke (item 2 do
  handoff anterior).
- Item 9 (online) bloqueado por D8.
