# Handoff · TIA Portal Openness API · 2026-07-28 (4ª sessão do dia)

## Goal
Fechar o último buraco do caminho de escrita: `replicate-fc --apply` contra **dados reais**
(nunca rodou fora de projeto scaffoldado). Tudo já preparado — o próximo agente executa, não decide.

## State
- HEAD: 26d1ac4 + 1 commit de prep. Working tree limpo.
- Portal aberto na sessão 1 com **Software de ETE Insular_Inicial_V21** (cópia de teste, backup do
  user, dano liberado). **PLC compila Success / 0 erros / 0 warnings** — qualquer erro depois do
  apply é do verbo, não herdado. Confirmar com `tia info` antes de começar (3s).
- **F8 fechado hoje**: primitivas 11/11 ✅, `import-ladder --apply` ✅ (2 bugs de FlgNet
  corrigidos), 6 geradores ✅ em dry + payload de `gen-fault-ob`/`gen-alarm-fc` importado no
  sandbox → compile 0 erros → `explain-block` round-trip.
- Tudo que foi escrito hoje vive em `ClaudeTest/`, `ClaudeTest/Sub`, `ClaudeTest/Gen` (+
  `DB INSTRUMENTOS` na raiz). User mandou **deixar lá e continuar usando essas pastas**.
- In progress: nada rodando.

## Decisions (and why)
- **`replicate-fc --apply` roda no projeto ouro mesmo, escopado a 1 tipo** — projeto separado via
  `scaffold` foi descartado: dados sintéticos já cobertos pelo SmokeTest_01 (PLANO F3) e o
  `scaffold` tem bug próprio, então a sessão viraria depuração de fixture.
- **Premissa corrigida**: os 6 geradores **já rodaram `--apply` completo** no SmokeTest_01
  (dry→apply→compile→idempotente). O que falta é robustez contra **dados reais**, não 1ª execução.
- **`replicate-instruments --apply` cortado de propósito** — dá `action: in-sync`, não escreveria
  nada. Só vale com alvo dessincronizado.
- **Otimizar verbo é alvo errado** (veredito dado ao user): attach = 2,9s fixo, amortizado por
  `run --script`. Critério de sucesso é `compile` 0 erros, não tempo.

## Next steps (ordered)
Arquivos **já criados e commitados** — é só executar:

1. **Dry primeiro** (`replicate-fc-soprador.json` é *config*, não script de batch):
   `pwsh scripts/tia.ps1 replicate-fc --config docs/examples/replicate-fc-soprador.json --out-file workspace/rep-dry.json`
   Esperado: 1 grupo (`Soprador`), molde `Soprador 1 (S-01A)`, **5 alvos `overwrite`**
   (S-01B..S-01F), 6 blocos cada, nada fora de `4. Motores/Bombas`. Se listar outro tipo, parar.
2. `pwsh scripts/tia.ps1 run --script docs/examples/replicate-soprador-run.json --out-file workspace/rep-run.json`
   = `save-project` → `--apply` → `compile --apply` → `--apply` de novo (idempotência) →
   `compile --apply` → `save-project`. Steps isolados: falha vira `{ok:false,error}` e o batch segue.
   Critério: **os dois compiles 0 erros** e o 2º apply sem reescrever nada.
3. `diff-block --file <xml gerado em workspace/exports> --name "PARTIDA_SOPRADOR_2 (S-01B)"` —
   prova conteúdo, não só que compilou.
4. Se verde, emendar `gen-profinet --apply` e `standardize-tags --apply` (dry mostrou
   `action: exists`/`ok`, quase no-op, custo marginal ~zero).
5. Depois disso F8 fecha. Pendentes menores: `scaffold`/`add-device` (bug dos bytes de
   system/clock memory), `import-master-copy` (sem `.al19` de teste).
6. **Ideia nova do user, análise pronta, execução não começou**: biblioteca de blocos instalável
   ("arsenal") — seção inteira em `docs/PLANO.md` (empacotamento `.scl`, instalação via `scaffold`,
   conteúdo, procedência). **Falta o user responder**: fatia 1 = 3 utilitários (escala, debounce,
   bits→word) ou os moldes que os geradores exigem?

## Key files
- `docs/examples/replicate-fc-soprador.json` — config escopado (`EquipmentTypes: ["Soprador"]`).
- `docs/examples/replicate-soprador-run.json` — o batch do passo 2, pronto.
- `docs/PLANO.md` — linha **F8** na tabela de fases; item **1b** do backlog v2 (LAD validado);
  seção **"Biblioteca de blocos"** (proposta nova).
- `src/Tia.Core/LadConverter.cs:355-397` — comparador `pre`/`in1`/`in2`, paralelo = parte `O` com
  `Card` + `in1..inN`. Verdade: `docs/examples/BombaTemplateFc.xml:346` e `:1044-1058`.
- `src/Tia.Core/Ops.cs:213` — guard de bloco inconsistente; `:311` — `ImportSource` gera blocos.
- `src/Tia.Cli/Program.cs:88` — dry de `import-ladder` curto-circuita antes do switch.

## Open / blockers
- Sem blocker técnico. Escrita fora de `ClaudeTest/` **autorizada** para os passos 1-4.
- Sem `checkpoint`/`restore` (F7 item 4): o ponto de retorno é o `save-project` do passo 2 + backup
  do user. Se quiser rede real, fechar o Portal e copiar a pasta `.ap21` antes.
- Rebuild com Portal aberto → 1ª chamada pode abrir diálogo de aceite na tela e pendurar. Se não
  retornar com `tia.exe` vivo e CPU ~0, pedir o clique antes de investigar código.
- Nunca rodar `tia` em paralelo (Openness single-session). `pwsh scripts/tia.ps1` é o comando único.
