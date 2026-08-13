# Critérios de aprovação — teste cego FP-07

Escritos **antes** da rodada, em 2026-08-13. Não editar depois que a execução começar; critério mal
formulado vira resultado do teste, não régua reescrita no meio da prova.

**Este arquivo não vai para a sessão que executa.** O que vai é `caderno-FP-07.md` e a skill `tia`.

## Por que esta rodada existe: é rodada de dívida

As seis rodadas anteriores produziram 30 consertos de CLI. Nem todos foram exercitados depois de
prontos — alguns já acumulam dois ciclos sem uso. Esta rodada foi desenhada para que o caminho
natural da entrega **passe por cima da dívida**, sem citar verbo nenhum no caderno:

| Dívida | O que no caderno força o uso | Ciclos parado |
|---|---|---|
| `replicate-fc --template/--target-folder` só provado em dry | área nova (sem irmã populada) com **três acionamentos idênticos em partida direta** — replicar exige molde de outra área | 1 (nasceu na FP-06) |
| `set-io-address` + `conflictCheck` nunca exercitado | seção 3 dá **endereço fixo por cartão** e proíbe mudar: não dá para deixar o Portal atribuir | **2** |
| `plug-module --apply` nunca aplicado (só dry) | os cartões da estação remota vêm **por MLFB na lista de compra**, não a critério do integrador | 1 |
| `gen-fault-ob` nunca usado em rodada cega | seção 6 pede alarme de falha de estação/cartão e CPU que não vai a STOP | **4** (existe desde a F3) |
| `audit` — os checks só foram vistos passando | seção 8 pede o relatório de conformidade **ao fim da etapa 1**, com o programa ainda ausente | **3** |
| `replicate-instruments` com `_PV_` fora da pasta da área (conserto T3 da FP-06) | três medidores novos, e o projeto guarda PV em `1. I/OS/QA-0N` | 0 |

`install-lib` e `import-master-copy --force --apply` **continuam fora**: são verbos de CPU virgem e
esta rodada é adendo em projeto existente. Ficam como dívida declarada, para uma rodada de projeto
novo.

## Portões objetivos (passa/não passa)

| # | Portão | Aprova se |
|---|---|---|
| G1 | Compila | `compile` do PLC inteiro: **0 erros**. Warnings permitidos, contados e registrados |
| G2 | Hardware presente | Estação remota nova na rede PROFINET do CLP, com os **quatro módulos da lista de compra** (MLFB conferido), IO device do controlador certo |
| G3 | **Endereço fiel ao diagrama** | Os cartões começam **exatamente** em `%IB1100`, `%QB420`, `%IB1110`. Endereço diferente = reprova, mesmo que compile — o caderno diz que o diagrama já foi para obra |
| G4 | Endereço não colide | Varredura do mapa de I/O final: zero sobreposições em `%I` e `%Q`, telegramas de drive incluídos |
| G5 | Área integrada | O bloco de chamada da área é alcançado por OB cíclico; nenhum bloco da área órfão; os alarmes da área entram no mecanismo de alarme que a estação já usa |
| G6 | Régua da casa | `audit` da **etapa 2** sem check vermelho, ou com justificativa escrita por check que reprove. `scanned` registrado (é o que separa check conforme de check cego) |
| G7 | Diagnóstico de hardware | Existe OB de falha de rack/estação e de falha de módulo, e ele publica qual estação falhou. CPU configurada para não parar por falha de periferia |

## Inspeção (não reprova sozinha, mas é metade do resultado)

- **I1 — a lógica está lá.** Os 8 itens da seção 4 e o item 6 do caderno, um a um, mapeados para
  bloco e rede. Anotar o que ficou de fora e por quê.
- **I2 — retentividade.** Horímetro e contador de partidas dos **três** acionamentos preservados na
  falta de energia, declarados onde a ferramenta permite declarar.
- **I3 — quanto veio de gerador.** Contagem por origem (replicação, geradores, clone, autoral).
  Referência: FP-06 fez 80 % por gerador com terreno favorável; FP-05 fez 0 % sem molde na casa.
  Esta rodada tem molde na casa para partida direta — abaixo de 60 % merece explicação.
- **I4 — custo.** Relógio, número de chamadas, e a repartição: engenharia × leitura × **contorno de
  CLI**. A série é 32 % (FP-05) → 12 % (FP-06). Registrar também o `ms` que o próprio batch reporta
  agora, e quanto do relógio foi compile.
- **I5 — os consertos entram no veredito.** Para cada dívida da tabela do topo: **segurou**,
  **doeu**, ou **não exercitado**. "Não exercitado" numa rodada desenhada para exercitar é achado,
  não neutro.

## As duas fotos do `audit` (é o achado que esta rodada persegue)

A etapa 1 entrega hardware e I/O **sem lógica de processo**. Nesse estado o projeto é legitimamente
não-conforme com o padrão da casa: não há bloco de chamada da área, não há FC de alarme, não há
pasta de acionamento populada.

O que se mede comparando as duas fotos:

1. Algum check **reprova** de verdade (vermelho) na etapa 1? Se sim, é a primeira vez que se vê os
   checks acusando em projeto real — o item aberto desde a FP-04.
2. Ou todos passam por **população vazia** (`scanned` baixo, tudo verde)? Se sim, o `scanned`
   provou o que foi construído para provar, e o `audit` tem um modo de falha declarado: aprova
   projeto pela ausência do que deveria checar.
3. Ou os checks se declaram `skipped` com motivo? É o comportamento desenhado — confirmar que a
   razão dita bate com a realidade da etapa.

Qualquer um dos três é resultado. O que não pode acontecer é a rodada entregar só a foto final.

## Armadilhas da seção 7 (as três são para recusar, com registro)

| # | Pedido | Contra o quê | Se for obedecido |
|---|---|---|---|
| B1 | um bloco de lógica próprio por bomba, escrito separadamente | duplicação de lógica idêntica; o padrão da casa é molde replicado com instância por equipamento | custo de manutenção dobrado; e delata replicação feita no braço |
| B2 | dados soltos na raiz da base global, um membro por sinal | R1/R2 — a raiz da `DB GLOBAL` do projeto real são áreas em `Struct`, sem escalar solto | **o `audit` deve reprovar** — se obedecer e o `audit` passar, o check está cego |
| B3 | bloco de diagnóstico de falha dentro da pasta da área | o OB de falha é do CLP inteiro, não da área; e o check de bloco de chamada fora da pasta de área | diagnóstico duplicado quando a próxima área nascer |

Recusar **sem registro escrito** conta como recusa cega e é tão ruim quanto obedecer: o produto é a
justificativa, não o "não".

## Condução

- **Quem escreveu o caderno não executa.** A sessão que rodar recebe `caderno-FP-07.md`, a skill
  `tia`, e nada mais desta conversa.
- Projeto de teste `proj/Software de ETE Insular_Inicial_V21` (escrita liberada). Um TIA Portal
  aberto, ou `--portal` em todas as chamadas.
- **Busca em rodada cega exclui `docs/teste-cego/` explicitamente** — lista de não-ler não vale
  para `grep`, e a FP-04 vazou por aí. Vazamento que acontecer, registrar como aconteceu.
- Sem toque no GUI. Clique necessário (diálogo de autorização do Openness conta) = registrar o quê
  e por quê.
- Registrar o relógio: início, fim, e onde o tempo foi.

## O que se registra (é este o produto do teste)

1. Os tropeços, separados entre "o caderno não dizia" (esperado) e "a ferramenta não dizia"
   (defeito nosso, e provavelmente do `SKILL.md`).
2. O veredito por dívida da tabela do topo (I5).
3. As duas fotos do `audit`.
4. A fila que sai, ordenada por dor evitada ÷ tamanho do diff.
