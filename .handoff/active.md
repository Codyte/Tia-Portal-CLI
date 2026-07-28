# Handoff · TIA Portal Openness API · 2026-07-28 (3ª sessão do dia)

## Goal
Camada de leitura do F7 fechada (orçamento de contexto atingido). **Próximo alvo: o caminho de
escrita, que nunca rodou `--apply` de verdade.** Começar pelo bug concreto e offline do
`LadConverter` (pinos de comparador), porque ele contamina os 4 geradores que emitem FlgNet.

## State
- HEAD: 266d56c — working tree limpo. 3 commits nesta sessão (d540797, 47fd434, 266d56c).
- Portal aberto na sessão 1 com **Software de ETE Insular_Inicial_V21** (CPU1.0 CCO, 62 devices,
  476 blocos, 194 tabelas de tag, 4372 tags, 13 UDTs). Saudável, `info` em 3,0s.
- **User liberou dano ao projeto ouro**: foi construído com os scripts originais e tem backup —
  perder/danificar não é problema. Isso destrava o `--apply` real, que era o bloqueio de sempre.
- **F7 leitura fechado.** Três entregas, todas medidas no projeto real:
  - `--out-file F.json` global (guard no único `Print`): 821.210 B viraram ~950 B de stdout, com
    `count=4372` intacto. Erro e timeout nunca vão pro arquivo.
  - `run --script` isola steps: `{ok:false,error,type}` por item, batch segue, `exit 1` se algum
    falhou. Smoke: `info` / `xref` inexistente / `list-types` = steps 3, failed 1, 3º rodou.
  - `tree` virou a orientação inteira: blocos + tabelas de tag + UDTs no mesmo `plc-navi.md`,
    39 KB / 309 linhas em 4,0s (JSON equivalente ~150 KB).
- In progress: nada rodando.

## Decisions (and why)
- **`--format table` (TSV) medido e DESCARTADO.** 822 KB → 331 KB = 2x num problema que precisa de
  30x; muda o número, não a classe. O que paga é agrupar por pasta (4,5x: 117 KB → 26 KB) ou não
  devolver volume (`trace` responde a pergunta inteira em 20 KB). Código não foi escrito.
- **Attach = 2,9s fixo**, não 7s como o handoff anterior dizia (`info` solo 3,0s, `list-types` 2,9s,
  batch de 5 steps 7,0s). Deixou de ser gargalo agora que o `run` sobrevive a exceção de step.
- **Os 52,4s do 1º `tree` eram cold cache do Openness pós-rebuild**, não regressão — remedido
  4,0s / 3,9s. Regra: primeira chamada depois de `rebuild.ps1` é atípica, não medir nela.
- **`snapshot` saiu do bloco "read" do help** pro bloco novo "bulk", junto do `find --pattern "*"`:
  volume bruto, sempre `--out-file` + grep, nunca leitura de orientação.
- **Item 3 do handoff anterior fechado como "não é bug"**: chave `"block"` no xref de tag/tabela/UDT
  fica — `kind` ao lado já diz o que é, e renomear quebra `raio-x.ps1`/`xref-obs.json`.
- **`checkpoint`/`restore` (F7 item 4) adiado**: rede de segurança pra `--apply` que ainda não roda,
  e o user tem backup do projeto. YAGNI até o `--apply` existir.

## Next steps (ordered)
1. **`LadConverter` — pinos de FlgNet contra o export real.** `docs/examples/BombaTemplateFc.xml`
   (90 KB, já no repo) mostra comparador com pinos `in1`/`in2` e série no pino `pre`; o
   `LadConverter` emite `operand1`/`operand2` e `in`. Conferir os nomes reais no XML, corrigir,
   assert novo em `Tia.Tests`. **100% offline, zero Portal, zero risco** — e desbloqueia
   `gen-fault-ob`, `gen-alarm-fc`, `replicate-fc`, `replicate-instruments`, que emitem FlgNet pelo
   mesmo caminho.
2. **`import-ladder --apply` no projeto ouro** — primeiro `--apply` de verdade do repo, agora
   liberado. Alvo: os `PARTIDA_*` reais.
3. **8 dos 26 erros de compile**: bytes de system/clock memory faltando no `scaffold`/`add-device`.
   Defeito conhecido, escopo pequeno.
4. `replicate-fc --apply` no ScaffoldTest.
5. Só então F7 4-5 (`checkpoint`/`restore`, `apply-spec --file plant.json`), se ainda fizerem
   sentido depois de ver o `--apply` funcionando.

Backlog parado: multiuser 3b/3c.

## Key files
- `src/Tia.Core/LadConverter.cs:341` — `Compile`; `ToFlgNet` em 386, `Operand` em 332. Alvo do passo 1.
- `docs/examples/BombaTemplateFc.xml` — export real, fonte da verdade dos nomes de pino.
- `docs/examples/ladder.scl` — fixture do dry-run; `Tia.Tests` já cobre `LadConverter.Convert`.
- `src/Tia.Core/Inventory.cs:109` — `Tree` (orientação); `AppendGrouped` logo abaixo.
- `src/Tia.Cli/Program.cs:484` — `Print` + `_outFile`, o guard único de saída.
- `docs/PLANO.md` — F7 na tabela de fases; pista dos pinos nas linhas 143-145.

## Open / blockers
- Nenhum blocker técnico.
- **Rebuild com Portal aberto → 1ª chamada pode abrir diálogo de aceite na tela e pendurar.** Se
  não retornar com `tia.exe` vivo e CPU ~0, pedir o clique antes de investigar código.
- Falta host/porta do TIA Project Server + projeto de teste lá — trava multiuser 3b/3c.
