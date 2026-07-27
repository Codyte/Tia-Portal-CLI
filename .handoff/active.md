# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
Testar a funcionalidade real da ferramenta contra projeto TIA de verdade. Objetivo declarado
("mais uma bomba igual à BH-01A") **fechado**: clone de acionamento ponta-a-ponta funciona.

## State
- HEAD: a3b10e0 — working tree limpo. `rebuild.ps1` ALL PASS.
- Projeto de teste = o AsBuilt aberto na sessão 1 (**user autorizou `--apply` nele**).
  Clone BH-01B → BH-01C aplicado e **salvo**: 13 tags em `%M432.0-433.4`, 5 instance DBs,
  1 FC, 2 membros novos na DB GLOBAL. Compile do PLC inteiro Success/0 erros,
  `diff-block` do FC clonado `identical`.
- Verbos novos (3 commits): `add-db-member`, `clone`, `free-memory`. 3 baterias de teste
  offline novas (DbMember.AddToXml, Memory.Occupied, Clone.Rewrite).
- In progress: nada mid-flight.

## Decisions (and why)
- `clone` genérico (`--block`/`--table` + `--replace OLD=NEW`) em vez de `clone-equipment`
  monolítico — um `--replace BH-01B=BH-01C` já reescreve nome, símbolos, path do DB global e
  instance DBs; serve tags, FC e iDBs com um verbo só.
- `--at` reendereça só tags Bool e **aborta** em tag mais larga — endereço sobreposto é pior
  que erro. Reusa `BoolAddressAllocator` (Profinet.cs).
- Tags de IO físico (`BOMBA_2_ELEVATORIA_DE_GORDURA_*`) **não** são clonadas: bomba nova de
  verdade precisa de %I/%Q próprios, que dependem de hardware novo. `free-memory` cobre só %M.
- `replicate-fc` não serve neste projeto (exige pasta `... (ID)`, e é replicador em massa que
  sobrescreve todas as irmãs). Confirmado por dry: 0 grupos, 61 pastas puladas.
- Achados no `docs/PLANO.md` (seção "Clonar acionamento — fluxo real validado"), sem doc separado.

## Next steps (ordered)
1. Se quiser reverter o teste no AsBuilt: apagar a pasta de blocos
   `4. Motores/Bombas/4.4 Elevatória de Gordura/Bomba Reserva 2 BH-01C`, a tag table
   `BOMBA_01_ELEVATORIA_DE_GORDURA_RESERVA_2 (BH-01C)` e os 2 membros BH-01C da DB GLOBAL
   (`delete-block`/`delete-folder` existem; `add-db-member` não remove).
2. `--apply` ainda não exercitados: `import-ladder` (FlgNet escrito de memória, nunca validado
   contra TIA real) e hardware (`set-address`/`connect-subnet`/`add-device`).
3. F1 propriamente dito (`docs/PLANO.md:171`).
4. Ressalvas de error-handling do code-review F3 (Standardize rebuild, FaultOb import sem
   try/catch por item) — não bloqueiam.

## Key files
- `src/Tia.Core/Clone.cs` — `Rewrite` (substituição textual) + `Readdress` (%M sequencial).
- `src/Tia.Core/DbMember.cs` — `AddToXml`/`ResolveSection`: Struct nativo aninha `<Member>`
  direto, instância de UDT expande em `<Sections><Section>` — trata os dois.
- `src/Tia.Core/Memory.cs` — `Occupied`/`Gaps`: mapa de bytes %M ocupados.
- `docs/PLANO.md` — seção "Clonar acionamento" com a sequência exata que funciona.
- `workspace/real-A/` — exports e XMLs gerados do teste (DB GLOBAL original em `db/`).
- `scripts/taskrun.ps1` + `workspace/taskio/` — canal da sessão 1.

## Open / blockers
- **Ordem de compile é obrigatória**: todo import deixa o alvo inconsistente e
  `Inconsistent blocks and PLC data types (UDT) cannot be exported` derruba o *próximo* export —
  inclusive de blocos que só referenciam o DB alterado. Compilar entre etapas.
- **Whitelist**: `rebuild.ps1` refez a whitelist mas o portal já aberto continuou com o hash
  velho → `EngineeringSecurityException: The operation has timed out`.
  `Start-ScheduledTask -TaskName TiaWhitelist` resolveu sem reabrir o portal.
- Openness single-session: um verbo por vez; `run --script` não isola steps.
- Allocator de %I/%Q não existe (e não deve chutar endereço físico).
