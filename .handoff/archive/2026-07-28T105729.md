# Handoff · TIA Portal Openness API · 2026-07-28 (4ª sessão do dia)

## Goal
Caminho de escrita validado contra projeto real (F8). Primitivas e `import-ladder` fechados.
**Próximo alvo: os 4 verbos de escrita que ainda nunca rodaram `--apply`** — todos precisam
escrever fora de `ClaudeTest/`, e é isso que falta decidir com o user.

## State
- HEAD: 383c074 — working tree limpo. 4 commits: 9736d52, 0268c04, dd045b6, 383c074.
- Portal aberto na sessão 1 com **Software de ETE Insular_Inicial_V21** (projeto de teste, com
  backup, dano liberado pelo user). **Compila Success / 0 erros / 0 warnings** — os 26 erros que
  o handoff anterior citava não existem mais.
- Tudo que foi escrito hoje vive em `ClaudeTest/` e `ClaudeTest/Sub|Gen` (+ `DB INSTRUMENTOS` na
  raiz, do `import-source`). **User pediu para deixar lá e continuar usando essas pastas.**
- **F8 — primitivas 11/11 ✅** (`create-folder`, `import-block` c/ FC de 90 KB, `import-tags`,
  `clone`, `export-type`→`import-type`, `import-source`, `add-db-member`, `delete-block`,
  `compile`): pasta compila 0 erros.
- **`import-ladder --apply` ✅** — round-trip fechado: `ladder.scl` → import → compile 0 erros →
  `export-block` → `explain-block` reproduz o SCL original.
- **6 geradores ✅ em dry** (`gen-all.json`, 0 falhas). Payload de `gen-fault-ob` (OB 88 KB) e
  `gen-alarm-fc` renomeado e importado em `ClaudeTest/Gen` → compile 0 erros → explain ok.
- In progress: nada rodando.

## Decisions (and why)
- **Otimizar verbo é alvo errado agora** (veredito dado ao user): nada está lento (attach = 2,9s
  fixo, amortizado por `run --script`). O que faltava era **primeira execução** do caminho de
  escrita — e ela achou 3 bugs em 1 hora. Critério de sucesso = `compile` 0 erros, não tempo.
- **Geradores testados por dry + import do payload no sandbox**, não por `--apply` direto: eles
  escrevem nas pastas da lei, não aceitam `--folder`. Renomear o bloco gerado (`<Name>` + `<Number>`)
  e importar em `ClaudeTest/Gen` valida o FlgNet sem sujar a árvore de produção.
- **`--apply` de `replicate-fc`/`replicate-instruments` NÃO foi rodado** — sobrescreve ~30 blocos
  de equipamento reais e não tem pasta para isolar. Esperando decisão do user (ver Open).
- **Guard de bloco inconsistente ficou só em `Ops.ExportBlock`** (1 dos 12 `.Export(`) — é o ponto
  por onde passam os 4 verbos que o user chama. Os outros 11 são internos dos geradores e param
  de falhar com `compile --apply` do PLC.

## Next steps (ordered)
**DECIDIDO 2026-07-28 pelo user: rodar no projeto ouro mesmo (opção a), escopado.** Projeto de
teste separado via `scaffold` foi descartado — dados sintéticos já cobertos pelo SmokeTest_01
(F3), e o `scaffold` tem bug próprio, então a sessão viraria depuração de fixture.
Correção de premissa: os 6 geradores **já rodaram `--apply` completo no SmokeTest_01** (PLANO F3,
dry→apply→compile→idempotente). O que falta é `--apply` contra **dados reais**, não 1ª execução.

1. `save-project` antes de tudo (ponto de retorno junto do backup do user).
2. Copiar `docs/examples/replicate-fc.json` para `workspace/sandbox/` com
   `"EquipmentTypes": ["Soprador"]` — corta de 4 tipos p/ 1: **5 pastas alvo, 6 blocos cada**,
   todas irmãs do mesmo molde. Rodar dry: plano tem que listar 5 alvos `overwrite` e nada fora de
   `4.1.1 Desarenador`.
3. `replicate-fc --apply` → `compile --apply` do PLC inteiro. Critério: **0 erros**. O projeto já
   compila 0 erros hoje, então qualquer erro novo é do verbo, não herdado.
4. `diff-block` de 1 bloco replicado contra o molde — prova conteúdo, não só que compilou.
5. Re-rodar `--apply`: tem que dar idempotente. É o passo que pega bug de replicador.
6. Se verde: `gen-profinet --apply` e `standardize-tags --apply` na mesma sessão — dry mostrou
   `action: exists`/`ok`, quase no-op, custo marginal ~zero.
- **Cortado de propósito**: `replicate-instruments --apply`. Dá `in-sync`, não escreveria nada;
  cobertura já veio do SmokeTest_01. Só vale com alvo dessincronizado.
3. **`scaffold` + `add-device`**: bug conhecido dos bytes de system/clock memory faltando
   (dava 8 dos 26 erros de compile num projeto scaffoldado). Escopo pequeno, offline-ish.
4. `import-master-copy` — sem `.al19` de teste; achar/gerar um ou marcar como não testável.
5. Só então F7 4-5 (`checkpoint`/`restore`, `apply-spec --file plant.json`).

Backlog parado: multiuser 3b/3c (falta host/porta do TIA Project Server).

## Key files
- `src/Tia.Core/LadConverter.cs:355-397` — `Compile`: pinos do comparador (`pre`/`in1`/`in2`) e
  paralelo como parte `O` com `Card` + `in1..inN`. Verdade de referência:
  `docs/examples/BombaTemplateFc.xml:346` (parte O) e `:1044-1058` (comparador).
- `src/Tia.Core/Ops.cs:213` — guard de bloco inconsistente (mensagem + comando a rodar).
- `src/Tia.Cli/Program.cs:88` — dry-run de `import-ladder` curto-circuita antes do switch (por isso
  o `case` só roda com `--apply`); `:318` o case.
- `docs/examples/LadderTags.xml` — tags do fixture `ladder.scl` (faixa %M26, livre via `free-memory`).
- `workspace/sandbox/*.json` — scripts do `run --script` da bateria (prims, gen-import, probe).
- `docs/PLANO.md` — linha F8 na tabela de fases + item 1b do backlog v2 (ambos atualizados hoje).

## Open / blockers
- Sem blocker: a escrita fora de `ClaudeTest/` foi autorizada para os passos 1-6 acima (projeto é
  cópia de teste com backup). A regra "tudo em pasta de teste" segue valendo para o resto.
- `checkpoint`/`restore` (F7 item 4) continua não existindo — o ponto de retorno do passo 1 é
  `save-project` + o backup do user, nada mais.
- Rebuild com Portal aberto → 1ª chamada pode abrir diálogo de aceite na tela e pendurar. Se não
  retornar com `tia.exe` vivo e CPU ~0, pedir o clique antes de investigar código.
