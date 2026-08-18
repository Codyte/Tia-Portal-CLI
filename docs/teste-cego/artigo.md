<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L14    Um agente escreveu um programa de PLC ponta a ponta — e o que interessa são os tropeços -->
<!--   L22    O problema com a demo -->
<!--   L33    A régua -->
<!--   L68    As três rodadas -->
<!--   L94    Os tropeços que viraram verbo -->
<!--   L131   O padrão que as três rodadas desenharam -->
<!--   L149   O que virou código -->
<!--   L170   O que ainda não está provado -->
<!--   L184   Reproduzir -->
<!-- ======================= END NAV INDEX ======================= -->

# Um agente escreveu um programa de PLC ponta a ponta — e o que interessa são os tropeços

Três rodadas de teste cego contra o TIA Portal: um caderno de obra entra, um projeto que compila
sai, e no meio fica um registro de tudo que a ferramenta atrapalhou. Este texto é sobre o registro,
não sobre o programa.

---

## O problema com a demo

Toda demonstração de "IA escreve automação" tem o mesmo formato: alguém pede um programa, o modelo
devolve texto que parece ST, e o vídeo acaba. Ninguém compila. Ninguém pergunta se o bloco nasceu na
pasta certa, se o intertravamento derruba a saída no mesmo ciclo ou se o horímetro é retentivo. E
ninguém repete o teste depois de mexer na ferramenta, porque não existe régua para comparar contra.

O que a demo mede é o modelo. O que a obra cobra é a ferramenta: se falta o verbo, o agente mais
capaz do mundo escreve XML na mão por meia sessão. Foi isso que os testes cegos foram desenhados
para medir.

## A régua

Os critérios de aprovação são escritos **antes** da rodada, num arquivo que a sessão executora não
recebe ([`criterios.md`](criterios.md)). A regra que sustenta o resto: se um critério se mostrar mal
formulado no meio da prova, isso é **resultado do teste** — não se reescreve a régua durante a
partida.

**Quatro portões objetivos**, cada um verificável por um comando, sem julgamento. Reprovar em
qualquer um reprova a rodada:

| # | Portão | Aprova se |
|---|---|---|
| G1 | o projeto compila | 0 erros; warnings permitidos, mas contados |
| G2 | hardware presente e conectado | CPU, periferia e inversor na mesma sub-rede, inversor como IO device |
| G3 | endereçamento fiel à lista de I/O | os pontos existem com **exatamente** os endereços do caderno |
| G4 | a lógica roda | o bloco principal é chamado por um OB cíclico; a chamada não está órfã |

**Quatro inspeções de julgamento**, que não reprovam sozinhas mas capturam o caso pior de todos —
*compila e não serve*: a lógica está de fato implementada ou o programa é uma casca vazia; o padrão
de pastas da casa foi respeitado; a segurança não foi diluída; e quanto veio de gerador contra
quanto foi escrito à mão (uma máquina que caísse inteira na biblioteca não testaria nada).

**Condução:** projeto novo e vazio, um TIA Portal só, sem toque no GUI. Cada clique que o operador
precisar dar é registrado — inclusive diálogo de autorização do Openness —, porque cada clique é um
furo na alegação de "ponta a ponta".

E, no fim, o produto de verdade:

> Mais importante que o veredito são **os tropeços**. Para cada um: onde a sessão travou e por
> quantos turnos; o que ela adivinhou porque o *caderno* não dizia (de propósito — obra real também
> não diz) e o que ela adivinhou porque a *ferramenta* não dizia (esse é defeito nosso); que verbo
> faltou; e que linha de documentação teria evitado o tropeço.

Travou por falta de documentação = defeito da skill, não da sessão.

## As três rodadas

| | FP-01 · filtro prensa | FP-02 · elevatória + preliminar | FP-03 · agitador `AG-05` |
|---|---|---|---|
| Data | 2026-08-07 | 2026-08-10 | 2026-08-10 |
| Escopo | planta nova do zero, com hardware | 2 áreas montadas só por verbos | equipamento novo em projeto existente |
| Compile | 0 erros / 0 warnings | 0 erros / 0 warnings | 0 erros / 1 aviso |
| `audit` | 3/5 | **6/6** | 5/6, com justificativa escrita |
| Autoria | 100 % autoral | zero SCL autoral | 8 de 10 blocos vindos da biblioteca |
| Cega de verdade? | não | não | **sim** |
| Tropeços | 8 | 13 | 10 |

**FP-01** foi a rodada de hardware: CPU 1515-2 PN, ET200SP com quatro cartões, inversor G120 com
telegrama padrão 20, tudo endereçado à mão contra uma lista de 27 pontos — **0 divergências de
endereço**. Saíram 22 blocos em 13 pastas e 35 tags em 4 tabelas, com os 9 passos da sequência, os
8 intertravamentos e os 12 alarmes do caderno implementados de fato. Zero cliques no GUI.

**FP-02** inverteu o exercício: nada de autoral, tudo pelos verbos de geração. Era o primeiro
`--apply` da vida de quatro deles. Os quatro deixaram defeito no caminho — e é aí que a rodada
pagou.

**FP-03** foi a primeira rodada de fato cega: os cadernos anteriores, os resultados datados e o
diário do projeto ficaram fechados até o fim da execução. Entregou 10 blocos, um UDT novo de 31
membros com todos os cadastros já preenchidos (o programa chega comissionável, não chega em branco)
e 26 tags. E entregou o achado mais caro da série.

## Os tropeços que viraram verbo

Três exemplos, escolhidos porque cada um representa uma família diferente.

### 1. O silencioso (FP-01)

O caderno pedia as analógicas em `%IW64`; a ET200SP nasce com o AI em `%IW2`. `list-attrs` não mostra
endereço — não é atributo do `DeviceItem` —, então `set-attr` não alcança. E o `import-cax`
**aceitou o AML com o `StartAddress` editado e ignorou a mudança em silêncio**: o export seguinte
continuava em 2.

O pior pedaço não é a falta do verbo. É o caminho que *parecia* ter funcionado. Virou
`set-io-address`, que varre o item e os descendentes, porque os endereços vivem no submódulo e não
no módulo que a pessoa nomeia.

### 2. O `ok: true` que não mudou nada (FP-03)

`edit-db-member --rename` devolveu sucesso e o passo seguinte do mesmo lote ainda enxergava o nome
antigo. O mecanismo, reconstituído depois com os artefatos do lote na mão: a DB não estava
*inconsistente* (o export funcionou) — estava **modificada-não-compilada**, porque o passo anterior
tinha importado sem compilar. Nesse estado o export devolve o conteúdo pré-import, e o patch é
calculado em cima de um XML velho. A primeira escrita sumia, com `ok: true` nas duas.

O conserto não foi um guard novo de "recusar bloco inconsistente" — esse não dispararia aqui. Foi a
coreografia `export → patch → Import Override` passar a **conferir o próprio resultado**: compila o
alvo e re-exporta para provar que o patch entrou.

### 3. O que não tinha caminho nenhum (FP-03)

A lei de construção da casa exige que a chamada de bloco seja feita em LAD. O `import-ladder` não
converte `CALL`, e não havia verbo que montasse uma rede de chamada. Montar um FC de partida foi:
exportar o molde, apagar duas `CompileUnit`, reescrever três `Access`, remover a negação de um
contato, inserir um contato em série (três wires) e **escrever uma `CompileUnit` inteira na mão** —
1 `Call`, 9 `Access`, 11 `Wires`, num script de 276 linhas de Python.

Funcionou de primeira, e o resultado é LAD legítimo. Mas isso é trabalho de verbo, não de sessão.

## O padrão que as três rodadas desenharam

**O defeito caro é sempre o silencioso.** A soma dos três: um import que engole a alteração e
devolve sucesso; um `rename` que responde `ok` sem tocar no projeto; um gate de `in-sync` que olhava
só a existência do bloco e deixava bloco gerado por molde velho preso para sempre, reportado como
sucesso; um `--folder` que devolvia `count: 0` e era lido como "pasta vazia" quando era filtro
errado; um `import-source` que aceitava fonte sem BOM e transformava `"Aferição"` em `"AferiÃ§Ã£o"`,
com o erro aparecendo no compile, longe da causa.

Erro alto e na causa custa uma chamada. Sucesso mentiroso custa a sessão inteira e, na obra, custa
depois.

**E o gerador confunde "o que este projeto tem" com "o que todo projeto tem".** Todos os defeitos
dos geradores em FP-02 são a mesma família: o nome da área sempre repetido na DB, o molde sendo
sempre um instrumento real do projeto, o ID sempre com hífen, o sufixo da tag de valor sempre igual.
Eles nasceram como port de scripts escritos *para um projeto*. O que resolveu, em todos os casos,
foi mover a suposição para o arquivo de configuração e deixar o código exigir só o que é estrutural.

## O que virou código

FP-03 fechou com uma fila de correção ordenada por dor evitada ÷ tamanho do diff. **Os seis itens
viraram verbo:**

| Tropeço | Virou |
|---|---|
| chamada em LAD escrita na mão | `add-call` + `delete-network` |
| `ok: true` sem efeito | guard de compile-e-confere em `add`/`edit`/`delete-db-member` |
| `Remanence` inalcançável (o Openness recusa em iDB, e SCL não expressa) | `set-retain --block FB --member M` |
| 12 chamadas de leitura para descobrir a assinatura dos FBs | `list-interface --folder` |
| clone que referencia iDB inexistente | `clone --with-instances` |
| `audit` assumindo que todo acionamento tem inversor | regra que reconhece partida direta |

Antes disso, FP-01 tinha rendido `set-io-address` e a correção de cultura que fazia todo
`import-tags` com comentário em `pt-BR` morrer em projeto novo — o exato caso de uso de qualquer
demonstração. FP-02 rendeu treze correções, cada uma com commit.

Essa é a resposta para "de onde vêm os verbos": não de brainstorm de features. De um agente
apanhando com o caderno na mão, e do registro do lugar exato onde apanhou.

## O que ainda não está provado

Honestidade sobre a régua vale mais que a régua:

- **Duas das três rodadas não foram cegas.** FP-01 e FP-02 foram executadas pela mesma linhagem de
  sessões que escreveu o caderno, violando o próprio critério de condução. O que vale nelas são os
  defeitos de ferramenta, que independem de quem executa; a alegação "um agente sem contexto
  consegue" só tem uma rodada por trás, a FP-03.
- **Um executor só.** Nada aqui compara modelos ou mede variância entre sessões.
- **O projeto de teste não tem periferia real.** Em FP-03 os cinco pontos de I/O foram endereçados em
  faixa livre, sem cartão plugado — é o que gera o único aviso do compile.
- **Compilar não é o aceite.** O aceite é o `audit` mais as nove regras da lei de construção. Um
  programa que compila e ignora o padrão da casa reprovou, mesmo com G1 verde.

## Reproduzir

Os cadernos, os critérios e os três resultados completos estão em
[`docs/teste-cego/`](.) — incluindo os tropeços que este texto não cobre. O caderno é a entrada, o
resultado é a saída, e o diff entre a fila de correção de uma rodada e os verbos da rodada seguinte
é a medida.

A ferramenta é o [tia-cli](https://github.com/Codyte/Tia-Portal-CLI): CLI sobre o TIA Portal
Openness, JSON na entrada e na saída, verbo de escrita em dry-run por padrão.
