# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Lapidar o modo operante: o FP-01 compila e cumpre o memorial, mas carrega dívida de engenharia.
Auditado, virou lei escrita. Próxima meta = a rodada que produz mais defeito por hora.

## State
- HEAD: `0928744` (2 commits locais nesta sessão, **não pushados**; `381f331` da anterior também não).
- Live state: **TIA Portal aberto na sessão 1**, projeto `workspace/blind/FP01/FP01.ap21` aberto.
  Probes de teste criados e deletados, `compile` limpo, **projeto não salvo** — o `.ap21` em disco
  segue igual ao entregue. Shell do agente na sessão 0 (rota da task). `tia.exe` rebuildado 2x,
  whitelist refeita, sem diálogo modal.
- Done: auditoria de engenharia do FP-01 (`docs/BOAS-PRATICAS.md`, 8 achados + lei R1–R9 + fila de
  gaps), ponteiro obrigatório no `CLAUDE.md`, e os gaps 1 e 2 corrigidos e provados no projeto vivo.
- In progress: nada mid-flight.

## Decisions (and why)
- **`import-source` passou a usar os overloads que já existiam**
  (`GenerateBlocksFromSource(PlcBlockUserGroup|PlcTypeUserGroup, GenerateBlockOption)`): `--folder`
  faz o bloco nascer na pasta (fim dos 17 `move-block`+`compile`) e fonte só de `TYPE` vai pra pasta
  de UDT — **é isto que torna UDT alcançável por fonte SCL**, sem GUI. Fonte mista com `--folder`
  é recusada: um caminho não endereça os dois grupos.
- **`KeepOnError` não rejeita bloco inválido — ele entra inconsistente.** Medido: a fonte com
  `TITLE` que antes abortava o lote agora gera **as duas** FCs. Contraria o tropeço 5 do relatório
  anterior. Consequência: `import-source ok` **não** significa "compila"; quem acusa é o `compile`
  seguinte.
- **Chamada em LAD é regra (R8) por motivo objetivo, não estético**: `replicate-fc`, `gen-alarm-fc`
  e `gen-fault-ob` reescrevem `FlgNet`. Bloco de chamada em SCL fica fora do alcance dos geradores
  da própria CLI.
- **Descartada** a ideia de outra rodada autoral em SCL: mesmo terreno, rendimento baixo — o que ela
  acharia já está no `BOAS-PRATICAS.md`.
- **Rodada cega adiada de propósito.** Continua pendente, mas hoje ela prova o *protocolo*, não caça
  defeito. Vale mais depois da FP-02 e com o caderno FP-02.
- `WebFetch` no support.industry.siemens.com dá **403** — a documentação usável é a ajuda local
  (`scripts/tia-help.py`), que tem o pacote `ProgTIATIPPS1215enUS` ("Programming recommendations").

## Next steps (ordered)
1. **`bake-lib.ps1`**: gerar a `.al21` a partir do projeto molde (`src/Tia.Lib/*.al21` **não existe**
   nesta máquina; `library/blocks/` tem os 66 XMLs). Caminho PLC→library nunca provado — o teste
   começa aí.
2. **Varredura dry dos 69 verbos** contra os 2 projetos reais, 1 `run --script --summary` (~80
   steps, 1 attach). Melhor achado/token do repo: erro besta, mensagem críptica, verbo que engasga
   em projeto de 476 blocos.
3. **FP-02 pelo caminho da casa, zero SCL autoral**: `scaffold --apply` → `install-lib` →
   `replicate-fc --apply` → `gen-alarm-fc` → `replicate-instruments` → `gen-fault-ob` →
   `standardize-tags`. É a metade da CLI que **nunca construiu planta nenhuma** (7 verbos `--apply`,
   só dry até hoje; `replicate-fc --apply` nunca exercitado). Oráculo automático: `audit` **5/5** +
   `compile` 0/0. Caderno deve forçar o que o FP-01 não tocou: válvula motorizada (17 tags),
   totalizador, diagnóstico de módulo, e **duas áreas** (aí a numeração cruzada `2.N`/`3.N`/`3.1.N`/
   `5.1.N` vira check de verdade).
4. Rodada cega de verdade, com o caderno FP-02.
5. `git push` dos 3 commits locais.

## Key files
- `docs/BOAS-PRATICAS.md` — a auditoria (8 achados com evidência + citação da ajuda oficial), a lei
  R1–R9 e a fila de gaps §3 (itens 1-2 feitos; sobram `import-ladder` sem chamada de bloco,
  `create-folder` por segmentos, checks novos no `audit`, `list-io-map`).
- `docs/PADRAO.md` — o molde da casa; é a régua de pasta/nome/6-blocos.
- `src/Tia.Core/Ops.cs:613` — `ImportSource` novo (folder + KeepOnError + `SourceDeclNames`).
- `src/Tia.Core/LadConverter.cs:11-15` — o subset que o `import-ladder` aceita (booleano puro, sem
  chamada de bloco); é o gap 3.
- `docs/teste-cego/resultado-2026-08-07.md` — os 9 tropeços da run anterior.
- `src/__navi__.md` — **desatualizado** (`set-io-address` e as mudanças desta sessão não entraram);
  regenerar com `pwsh scripts/navi-cs.ps1`.

## Open / blockers
- Nada bloqueia. O `FP01` fica aberto no Portal — a FP-02 precisa dele fechado ou de um Portal só.
- Pergunta em aberto para o agente da run anterior (opcional): por que nunca criou UDT? O
  `import-source` já aceitava `TYPE` no dry-run, então parece omissão de especificação, não
  bloqueio técnico — se for, a correção é a regra R1, não código.

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo** para o passo 1 — `bake-lib.ps1` é sequência documentada; se falhar, o erro do Openness
diz o quê. Sobe pra **médio** no passo 3 (7 verbos `--apply` nunca exercitados juntos, e o `audit`
5/5 é régua dura). O gargalo do passo 3 não é raciocínio: é `compile` e attach do Portal.
