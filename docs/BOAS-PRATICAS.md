# Boas práticas de construção de programa

Auditoria do projeto `FP01` (rodada de 2026-08-07) contra o projeto-molde de
[`PADRAO.md`](PADRAO.md) e contra a ajuda oficial do TIA Portal, e a lei de construção que sai
disso. Vale para qualquer programa que a CLI escrever daqui em diante.

O `FP01` **compila 0/0 e cumpre o memorial** — o que está aqui não é bug de lógica, é dívida de
engenharia: o programa funciona e é caro de manter.

## 1. Achados

Evidência de cada item: `tia list-blocks --type FC` no projeto aberto, `tia tree`, e os fontes
`workspace/blind/fp01-{a,b}-*.scl`.

### A. Zero UDTs (o molde tem 13)

`tia tree` fecha `0 UDTs`. O único agrupamento de dados do programa é uma `Struct` **anônima**
declarada dentro da DB global:

```scl
INTL : Struct           // DB_FP01, linha 57
   SEGURANCA_OK : Bool;
   LIB_BH01 : Bool;
   ...
END_STRUCT;
```

A ajuda da Siemens descreve exatamente esse padrão como o procedimento do **STEP 7 V5.x** que a
recomendação para S7-1200/1500 substitui: *"The declaration in the data blocks was mostly
implemented as an anonymous structure (…) The number of parameters that you had to supply was often
very large"* (`ProgTIATIPPS1215enUS/…/68852306827.htm`, "Using PLC data types (UDT)"). O que se
perde sem UDT, segundo o mesmo tópico: herança automática da mudança em todos os pontos de uso,
endereçamento indireto, símbolo legível no editor, e passagem da estrutura inteira na chamada.

### B. Interface inchada — consequência direta de (A)

`FB SEQUENCIA_FP-01`: 10 `VAR_INPUT` + 13 `VAR_OUTPUT` = **23 parâmetros escalares**, e a chamada
em `CICLO_FILTRO_PRENSA (FP-01)` são 23 linhas de amarração membro a membro. A própria ajuda manda
o contrário, e ainda linka a FAQ: *"you can combine several parameters in a PLC data type (UDT) (…)
Why should whole structures instead of many single components be transferred for the S7-1500 when a
block is called?"* (`ProgPLCInterfaceenUS/…/10866504075.htm` → FAQ 67585079).

Com três UDTs (`UDT_FP01_COMANDO`, `UDT_FP01_PROCESSO`, `UDT_FP01_SAIDAS`) a mesma chamada tem 3
parâmetros e a IHM ganha um endereço estável por grupo.

### C. DB global plana

`DB_FP01` tem ~50 membros no mesmo nível, organizados só por comentário (`// ---- comandos da IHM
----`, `// ---- modos ----`, `// ---- sequencia ----`…). Comentário não é estrutura: não impede
colisão de nome, não aparece na referência cruzada e não se replica para o próximo equipamento.
**Cada bloco de comentário ali é um UDT que não foi criado.**

Uma DB global só está certa (o molde também tem uma, `DB GLOBAL`) — o que falta é ela ser um
agregado de UDTs em vez de uma lista de escalares.

### D. Nomes internos misturam três convenções

No mesmo arquivo: interface em `MAIÚSCULA_UNDERSCORE` (`FALHA_DISJUNTOR`), estáticas em `camelCase`
com prefixo de tipo (`fDisjuntor`, `tRetorno`, `passoAnterior`, `bombaPressao`), temporárias em
minúscula (`yv01`, `i`, `limite`, `ligar`, `setpoint`).

Além da inconsistência, os nomes descrevem o **tipo**, não a função: `f` = falha, `t` = timer.
`limite` é limite de quê? (é o tempo máximo do passo corrente). A ajuda vende símbolo justamente
como substituto do comentário: *"You do not have to write detailed comments"*
(`ProgTIATIPPS1215enUS/…/69410500747.htm`, "Symbolic addressing") — o que só vale se o símbolo for
auto-descritivo.

| hoje | deveria ser |
|---|---|
| `fDisjuntor` | `FALHA_DISJUNTOR_TRAVADA` |
| `fPartida` | `FALHA_PARTIDA_TRAVADA` |
| `limite` | `TEMPO_MAX_DO_PASSO` |
| `tPasso` | `TMR_TEMPO_MAX_DO_PASSO` |
| `ligar` | `PEDIDO_DE_MARCHA` |
| `yv01` | `SAIDA_YV01_CALCULADA` |

### E. Índice mágico em array de alarme

`ALARME : Array[1..12] of Bool`, escrito como `#ALARME[7] := #FALHA_PARTIDA_BH01`. Quem for
configurar a IHM ou depurar em campo precisa do memorial aberto para saber o que é o 7. UDT com
membros nomeados, ou constantes simbólicas para os índices.

### F. Pastas — 5 divergências do molde

| # | O que está | O que o molde manda | Causa |
|---|---|---|---|
| 1 | `1. I-OS` | `1. I/OS` | `create-folder --path` usava `/` como separador — **resolvido**: `--path "1. I\/OS"` (§3.4) |
| 2 | `4. Motores` | `4. Motores/Bombas` | idem |
| 3 | `CHAMADA_ALARMES_DESIDRATACAO` dentro de `3. Alarmes/3.1 Desidratacao` | `CHAMADA_*` fica no nível acima, junto do `OB_MOLDE_ALARMES` (`3.1 Alarmes Words`); a pasta de área só tem `FC_ALARMES_<AREA>` + DBs | decisão da sessão |
| 4 | `5. Instrumentacao/5.1 Desidratacao` | `5. Instrumentação/5.1 Aferição Analógica/5.1.N <Área>` — falta um nível | decisão da sessão |
| 5 | `6. Sequencia`, `7. Intertravamentos` | no molde `6.` = Comm Serial 485 e `7.` = Comm Skids — números **já ocupados** | decisão da sessão |

Os itens 1 e 2 são defeito de ferramenta; 3, 4 e 5 são o programa inventando taxonomia onde já
havia uma.

### G. Toda chamada em SCL

`list-blocks --type FC` devolve `"language": "SCL"` nos 8 FCs, e o `Main (OB1)` é SCL. O molde faz o
contrário: bloco de chamada (`OB_MOLDE_PARTIDAS`, `CHAMADA_INVERSORES_CCM*`, `CHAMADA_ALARMES`,
`PARTIDA_*`) é gráfico, e a lógica pesada mora no FB.

O argumento aqui não é de gosto:

- **Os geradores da própria CLI só enxergam LAD.** `replicate-fc`, `gen-alarm-fc` e `gen-fault-ob`
  reescrevem `FlgNet` (rede gráfica). Um `CHAMADA_*` em SCL está fora do alcance deles — o programa
  entregue não é replicável pela ferramenta que o gerou.
- **A "call structure" do Portal** (`ProgRef2MenUS/…/10866807819.htm`), que é o que o manutentor
  abre em campo, mostra bloco gráfico com o estado ao vivo por rede; texto estruturado ele lê como
  bloco único.

Contrapartida honesta: LAD para chamada custa mais raciocínio para montar (é por isso que o molde
mantém a lógica dentro dos FBs, e é por isso que a sessão caiu em SCL — ver §3).

### H. `tia audit` fecha 3/5

3 acionamentos com 2 blocos em vez de 6, 2 sem tabela de tag própria. Já registrado em
[`teste-cego/resultado-2026-08-07.md`](teste-cego/resultado-2026-08-07.md) como consequência de não
usar a biblioteca da casa — a régua funcionou.

## 2. Lei de construção

Regra e como verificar. Vale para qualquer programa que a CLI escrever.

| # | Regra | Verificação |
|---|---|---|
| R1 | Todo agrupamento de dados usado por mais de um bloco é **UDT**. `Struct` anônima dentro de DB: proibida. | `tia tree` → `0 UDTs` reprova |
| R2 | DB global é agregado de UDTs, um por área funcional. Comentário separador de seção = UDT faltando. | membro escalar solto na raiz da DB global |
| R3 | Interface de FB: **até ~8 parâmetros escalares**. Acima disso, agrupar em UDT. | contagem no `explain-block` |
| R4 | Nome descreve função, nunca tipo. Sem prefixo húngaro (`f`, `t`, `b`). **Uma** convenção no projeto inteiro — `MAIÚSCULA_UNDERSCORE`, como o molde. | leitura da interface |
| R5 | Array indexado por número só com UDT nomeado por trás ou constante simbólica. | `[<número literal>]` no código |
| R6 | Bloco **nasce** na pasta certa. `import-source` na raiz + `move-block` depois é caminho errado (34 steps e uma janela de inconsistência). | `list-blocks` com `folder` vazio ou raiz |
| R7 | Numeração de 1º nível é a do molde (`0..9` já têm dono). Categoria nova entra como sub-nível. | comparar com `PADRAO.md` §Program blocks |
| R8 | **Linguagem:** chamada (OB1, `CHAMADA_*`, `PARTIDA_*`, `MOLDE_*`) em **LAD**; lógica pesada (sequência, escalonamento, aferição, alarme) em **SCL dentro de FB**. | `list-blocks` → `language` |
| R9 | Acionamento = 6 blocos + 1 tabela de tag com o `(TAG)`, ou justificativa escrita. | `tia audit` |

## 3. O que hoje impede cumprir (fila de correção)

Nenhuma das regras acima é gratuita com a CLI como está. Ordenado por (dor evitada ÷ tamanho do
diff):

1. ~~**`import-source --folder` + `KeepOnError`.**~~ **Feito em 2026-08-07.** Os overloads já
   existiam no Openness (`TIAPortalOpennessenUS/…/131792485771.htm`) e `Ops.ImportSource` chamava a
   versão sem argumento. Agora: `--folder` põe o bloco na pasta certa de nascença (fim dos 34 steps
   de `move-block` + `compile`, tropeço 7) e `KeepOnError` impede que um bloco inválido derrube o
   lote (tropeço 5). Medido no `FP01`: a fonte com `TITLE` gera **as duas** FCs — a ruim entra
   inconsistente, e quem acusa é o `compile` seguinte. Resolve R6.
2. ~~**`import-source` roteando `TYPE` para `plc.TypeGroup`.**~~ **Feito em 2026-08-07.** Fonte que
   só declara `TYPE` vai para a pasta de UDT (overload `PlcTypeUserGroup`); fonte mista com
   `--folder` é recusada com mensagem (um `--folder` não endereça os dois grupos). O relatório
   deixou de mentir: `generated` procura em blocos **e** em UDTs. Destrava R1/R2 — UDT em pasta,
   por fonte SCL, sem GUI.
3. ~~**`import-ladder` não converte chamada de bloco.**~~ **Descartado em 2026-08-11**, resolvido por
   outro caminho. A R8 foi destravada pelo `add-call` (FP-03, tropeço 2), que monta a rede LAD
   direto no XML do bloco a partir da interface do FB — sem passar por SCL. Ensinar `CALL` ao
   `LadConverter` seria uma segunda rota para o mesmo destino, com a parte cara do problema
   (resolver tipo de pino, montar `Access`/`Wires`) duplicada: o conversor teria que reimplementar
   o que o `add-call` já faz, e um `#local` como parâmetro continua fora do alcance dos dois.
   O `import-ladder` fica no que faz bem — lógica booleana pura vinda de fonte SCL.
4. ~~**`create-folder` aceitando lista de segmentos.**~~ **Feito em 2026-08-11.** Saiu diferente do
   proposto: em vez de lista de segmentos, `\/` no `--path` é barra literal
   (`--path "1. I\/OS/QA-01"`), o que vale para **todo** verbo que recebe caminho de pasta, não só
   o `create-folder` — a regra mora no `Ops.SplitPath`, sob o longest-match do `WalkFolders`.
   Fecha as divergências F1 e F2. No mesmo movimento, `--path` virou repetível: uma árvore inteira
   num attach, com o caminho que falha isolado em `{path, error}`.
5. ~~**`audit` com os checks novos.**~~ **Feito em 2026-08-11.** Os quatro entraram: `R1 · o PLC tem
   UDT`, `R2 · DB global sem escalar solto na raiz`, `R8 · bloco de chamada em linguagem gráfica` e
   `CHAMADA_* fora da pasta de área` — 10 checks no total. O R2 é o único que sai do read-only: só
   o export mostra o datatype dos membros, então ele exporta a DB global para `--out` e **pula sem
   reprovar** (`skipped`, com o motivo) quando não há DB global identificável, quando ela está
   inconsistente, ou quando `--db` aponta para o que não existe.
6. ~~**`list-io-map`**~~ — **Feito em 2026-08-11.** `list-io-map [--device X] [--io Input|Output]`
   devolve todo endereço de I/O do projeto (device, caminho do item, `%IB..`/`%QB..`, bits/bytes) e
   o próximo byte livre por tipo. É a resposta direta à sonda de 18 chamadas da FP-01: varre item +
   descendentes, que é onde os `Address` moram — inclusive os do drive object, que
   `list-telegrams` não traz e `list-attrs` não enxerga.
