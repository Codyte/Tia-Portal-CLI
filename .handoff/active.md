# Handoff · TIA Portal Openness API · 2026-07-28 — **dois tracks paralelos**

## Roteador (leia só o seu)
- User disse **"continue 1"** → leia `.handoff/track1.md`. Portal: `replicate-fc --apply` contra
  dados reais. **É o único track que pode chamar `tia`.**
- User disse **"continue 2"** → leia `.handoff/track2.md`. Offline: biblioteca de blocos
  (`library/`), `.gitignore`, docs. **Nunca chama `tia`, nunca roda `rebuild.ps1`.**
- Sem número → perguntar qual, ou tratar como sessão única e executar o track 1 primeiro.

Não leia o arquivo do outro track: o que você precisa saber dele já está aqui embaixo.

## Por que o paralelismo é seguro (e onde deixa de ser)
Openness é **single-session**: duas chamadas `tia` simultâneas se derrubam. Por isso o corte é
Portal × offline, não "metade das tarefas cada". Três proibições que sustentam isso:
1. Só o track 1 chama `tia` (qualquer verbo, inclusive leitura).
2. Ninguém roda `rebuild.ps1` enquanto o outro trabalha — ele substitui o `tia.exe` que o track 1
   está usando e refaz a whitelist. Track 2 não altera `src/**`, então não precisa.
3. **`git add -A` proibido nos dois.** Mesma working tree: `-A` commita o trabalho pela metade do
   outro. Sempre caminhos explícitos.

Divisão de arquivos (quem escreve o quê) está no fim de cada track. Único ponto de contato é
`docs/PLANO.md`, em seções diferentes: reler o arquivo imediatamente antes de editar.

## Estado compartilhado
- HEAD ao escrever: 740f6bc. Working tree limpo.
- Portal aberto na sessão 1 com **Software de ETE Insular_Inicial_V21** (cópia de teste, backup do
  user, dano autorizado). **PLC compila Success / 0 erros / 0 warnings.**
- Repo é **público** (`github.com/Codyte/TIA-Portal`) — pesa na decisão do track 2.
- **F8 fechado hoje** (caminho de escrita): primitivas 11/11 ✅, `import-ladder --apply` ✅ com 2
  bugs de FlgNet corrigidos (comparador `pre`/`in1`/`in2`; paralelo = parte `O` com `Card` +
  `in1..inN`), 6 geradores ✅ em dry + payload de `gen-fault-ob`/`gen-alarm-fc` importado no
  sandbox → compile 0 erros → `explain-block` round-trip.
- Tudo que foi escrito hoje vive em `ClaudeTest/`, `ClaudeTest/Sub`, `ClaudeTest/Gen` (+
  `DB INSTRUMENTOS` na raiz). User mandou deixar lá e continuar usando essas pastas.

## Decisões travadas (não rediscutir)
- **`replicate-fc --apply` no projeto ouro, escopado a 1 tipo** — projeto separado via `scaffold`
  descartado: dado sintético já coberto pelo SmokeTest_01, e o `scaffold` tem bug próprio.
- Os 6 geradores **já rodaram `--apply` completo** no SmokeTest_01 (PLANO F3). O que falta é
  robustez contra dados reais, não primeira execução.
- **`replicate-instruments --apply` cortado**: dá `in-sync`, não escreveria nada.
- **Empacotamento da biblioteca**: `.scl` padrão, `.xml` só pra LAD, `.al19` descartado.
- Otimizar verbo é alvo errado agora: attach = 2,9s fixo, amortizado por `run --script`. Critério
  de sucesso é `compile` 0 erros, não tempo.

## Regras duras que valem pros dois
- Todo import deixa o alvo **e quem o referencia** inconsistente, e o Openness recusa exportar
  bloco inconsistente → `compile --apply` entre etapas.
- `pwsh scripts/tia.ps1 <verbo>` é o comando único (roteia sessão 0 × sessão 1 sozinho).
- Verbo de escrita: dry por padrão, `--apply` explícito.
- Chamada pendurada com `tia.exe` vivo e CPU ~0 = diálogo de aceite do Openness na tela: pedir o
  clique, não investigar código.

## Arquivos-chave
- `docs/examples/replicate-fc-soprador.json` — config escopado; `replicate-soprador-run.json` — batch.
- `docs/PLANO.md` — linha **F8** (track 1) · seção **"Biblioteca de blocos"** e linha **F4** (track 2).
- `src/Tia.Core/LadConverter.cs:355-397` — pinos e parte `O`; verdade em
  `docs/examples/BombaTemplateFc.xml:346` e `:1044-1058`.
- `src/Tia.Core/Ops.cs:213` guard de inconsistência · `:311` `ImportSource` gera blocos.

## Aberto
- Fatia 3 da biblioteca (utilitários genéricos: escala, debounce, bits→word) — só depois da fatia 1.
- `scaffold`/`add-device`: bug dos bytes de system/clock memory. `import-master-copy`: sem `.al19`.
- Sem `checkpoint`/`restore` (F7 item 4): ponto de retorno = `save-project` + backup do user.
