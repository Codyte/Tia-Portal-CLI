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
1. **Decidir com o user** (pergunta já feita, sem resposta): rodar `replicate-fc --apply` no
   projeto ouro (sobrescreve `Soprador 2..6`, 6 blocos cada) **ou** montar projeto separado via
   `scaffold` para os verbos que não cabem no sandbox.
2. Depois da decisão, fechar os 4 pendentes na ordem: `replicate-fc --apply`,
   `replicate-instruments --apply` (hoje `in-sync`, precisa de alvo fora de sincronia),
   `gen-profinet --apply` (tabela `4.1 Profinet`), `standardize-tags --apply` (131 tabelas).
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
- **Bloqueio real**: os 4 verbos do passo 2 exigem escrita na árvore de produção do projeto ouro.
  Backup existe e o user liberou dano, mas ele pediu para manter tudo em pastas de teste — as duas
  coisas se contradizem aqui. Perguntar antes de rodar.
- `replicate-instruments` devolve `action: in-sync` no projeto atual: mesmo com `--apply` não
  escreveria nada. Para testar de verdade, precisa de um alvo fora de sincronia.
- Rebuild com Portal aberto → 1ª chamada pode abrir diálogo de aceite na tela e pendurar. Se não
  retornar com `tia.exe` vivo e CPU ~0, pedir o clique antes de investigar código.
