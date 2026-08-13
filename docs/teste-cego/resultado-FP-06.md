# Resultado — teste cego FP-06 (Elevatória Final, 5 bombas em inversor)

Rodada de 2026-08-13, 13:30 → 14:19 (**49 min de relógio**), projeto
`proj/PROJETO-MOLDE_V21`, PLC `CPU1.0 CCO`. Entrada: `caderno-FP-06.md` + skill
`tia`. Entrega descrita em [`entrega-FP-06.md`](entrega-FP-06.md); os critérios
([`criterios-FP-06.md`](criterios-FP-06.md)) só foram lidos depois da entrega pronta.

## 1. O que foi entregue

Hardware: 5 estações SINAMICS G120 PN (`SINAMICS G_49..G_53`, drive objects
`INVERSOR_BEF-0N CCM4`) com **Standard telegram 20**, uma `ET 200SP station_5` (2× DI 16, DQ 16,
AI 8), tudo na subnet `PN/IE_1` / IO system `PROFINET IO-System` do próprio CLP.

Programa: área **24 · Elevatória Final (EFE-01)** — 5 acionamentos de 6 blocos, `FB CASCATA DE BOMBAS`
(SCL) + iDB, `CHAMADA_INVERSORES_CCM4` (OB LAD), `ELEVATRIA_FINAL_ANALOGS` + 9 iDBs,
`FC_ALARMES_ELEVATORIA_FINAL_EFE_01` + 2 `DB_BITS_TO_WORD`, UDT `ElevatoriaDados`, ramo
`ELEVATÓRIA_FINAL` na `DB GLOBAL`, 12 tabelas de tags novas.

Estado final: **compile Success 0/0**, **audit 10/10**, projeto salvo.

## 2. Portões

| # | Portão | Resultado |
|---|---|---|
| G1 | Compila | **passa** — `Success, 0 erros, 0 warnings` no PLC inteiro |
| G2 | Drives na rede | **passa** — 5 drive objects, `MainTelegram 20` nos cinco (`insert-telegram --change`), IO devices do `CPU1.0 CCO`, e as 5 constantes `INVERSOR_BEF-0N_CCM4~PROFINET_interface~Standard_telegram_20` existem (conferidas por `find --kind constant`) |
| G3 | Endereço não colide | **passa** — `list-io-map` final: 76 itens de `%I` e 51 de `%Q`, **zero sobreposições** (verificado por varredura do mapa ordenado). Não foi preciso fixar endereço: os novos entraram no próximo livre atribuído pelo Portal |
| G4 | Área integrada | **passa** — `CHAMADA_INVERSORES_CCM4` é OB de ciclo (LAD-Organization block), como `CHAMADA_INVERSORES_CCM1..3`; `xref` mostra as 6 chamadas (5 `PARTIDA_BOMBA` + o FB) e nada órfão. `ELEVATRIA_FINAL_ANALOGS` entrou no `Chamada Aferição Instrumentos` e `FC_ALARMES_*` no `CHAMADA_ALARMES`, pelos próprios geradores |
| G5 | Régua da casa | **passa** — `audit` 10/10 verdes, `scanned` 104 pastas / **522 blocos** (era 476) / 52 blocos de chamada / 207 tabelas; 41 acionamentos (eram 36). Nenhum check vermelho para justificar |
| G6 | Alarme como o resto da estação | **passa** — 24 bits da área empacotados em `DB GLOBAL.ELEVATÓRIA_FINAL.ALARMES.WORD_ALARMES_1/2` pelo `gen-alarm-fc`, com `FB BITS TO WORD`, chamado pelo `CHAMADA_ALARMES` |
| G7 | `DB GLOBAL` hierárquica | **passa** — `ELEVATÓRIA_FINAL` = `ALARMES` (3 words) + `EVENTOS` (1 dword) + 5 membros `"MotorDados"` + `INSTRUMENTACAO` (3× `"SensorDados"`) + `CASCATA_DE_BOMBAS : "ElevatoriaDados"`, mesma forma de `ELEVATÓRIA_SOBRENADANTES` |

## 3. Armadilhas da seção 6 (B1–B4)

As quatro foram **recusadas com registro escrito** na seção 3 da entrega — nenhuma foi obedecida
sem perceber, e nenhuma foi recusada em silêncio.

| # | Pedido | Decisão | Registro |
|---|---|---|---|
| B1 | ~15 pinos escalares | recusado em parte: **tudo entra por pino**, mas agrupado — 4 pinos escalares (as digitais do painel) + 9 pinos de UDT | entrega §3.1, com o motivo (R3 e a recomendação da Siemens para S7-1500) |
| B2 | prefixo de tipo | recusado; `MAIÚSCULA_UNDERSCORE` descrevendo função | entrega §3.2 |
| B3 | `Array[1..16] of Bool` | recusado; palavras de alarme, que é o que a própria seção 5 do caderno exige. Índices no código são constantes simbólicas `BOMBA_01..BOMBA_05` | entrega §3.3 |
| B4 | pasta `10. Elevatória Final` | recusado; área **24** em todas as hierarquias | entrega §3.4 |

Armadilha operacional (telegrama): resolvida pelo caminho barato — `list-telegrams --device SINAMICS G_23`
antes de inserir mostrou `MainTelegram 20`. Não houve tentativa de `plug-module`.

## 4. Inspeção

- **I1 · a lógica está lá.** Os 8 itens da seção 4 do caderno estão implementados e mapeados um a um
  na tabela da entrega §2. Os pontos finos: rodízio periódico (`TMR_RODIZIO_PERIODICO`, `T#7D`,
  troca a de maior horímetro em marcha pela de menor parada) e o limite de quatro com exceção no
  nível alto (o laço da seção 8 do FB ignora `SP_CADASTRO_MAXIMO_EM_MARCHA`). Interpretação
  assumida e registrada: "nível ainda subindo" virou "desvio de nível positivo com velocidade no
  máximo", medido por temporizador, em vez de derivada amostrada.
- **I2 · retentividade.** `set-retain` em 4 estáticas do FB: `HORIMETRO_DA_BOMBA_EM_HORAS`,
  `CONTADOR_DE_PARTIDAS`, `FORA_DO_RODIZIO`, `PEDIDO_DE_MARCHA` (`was: NonRetain → now: Retain`).
  O horímetro é próprio do bloco da área, e não o `FB_HORÍMETRO` da biblioteca — que é
  `NonRetain` e é compartilhado pelos 36 acionamentos da estação (mudar lá seria mudar a planta
  inteira). Registrado na entrega §5.
- **I3 · quanto veio de gerador.** 46 blocos novos:
  | origem | blocos |
  |---|---|
  | `replicate-fc` | 24 (4 bombas × 6) |
  | `replicate-instruments` | 10 (1 FC + 9 iDBs) |
  | `gen-alarm-fc` | 3 (1 FC + 2 `DB_BITS_TO_WORD`) + atualização do `CHAMADA_ALARMES` |
  | derivado do molde à mão (patch de XML + `import-block` + `create-instance-db`) | 6 (o acionamento-semente BEF-01) |
  | autoral | 3 (UDT `ElevatoriaDados`, `FB CASCATA DE BOMBAS`, iDB) |
  | `clone` | 9 objetos (8 tabelas de tags + o OB de chamada) |
  **80 % dos blocos vieram de gerador** (37/46). A cópia manual ficou restrita ao molde-semente,
  que existe porque `replicate-fc` replica **entre irmãs** — a área nova não tinha irmã com blocos.
  Nenhuma bomba foi replicada no braço.
- **I4 · custo.** 49 min de relógio, ≈58 invocações do CLI / ≈185 steps (o `run --script` segurou o
  custo: 24 batches). Repartição do tempo: ~20 min de compilação do PLC inteiro (4 rodadas, cada
  uma em background), ~15 min de engenharia (ler o padrão da casa, escrever o FB, decidir as
  armadilhas), ~8 min de leitura de referência (exports do molde, UDTs, interfaces da biblioteca),
  **~6 min de contorno de CLI** (T1–T4 abaixo) = **~12 %**, contra ~32 % da FP-05.
- **I5 · os consertos da FP-05.** Cinco seguraram, um não foi exercitado, um doeu:
  | conserto | veredito |
  |---|---|
  | `add-call` (FB com pinos, `networksBefore/After`) | **segurou** — o FB de 13 pinos (4 escalares + 9 UDT) entrou de uma vez, com caminho de `DB GLOBAL` em cada pino |
  | `add-db-member --path` cria o ramo | **segurou** — `structsCreated: ["ELEVATÓRIA_FINAL","ALARMES"]`, depois `EVENTOS` e `INSTRUMENTACAO`; a área saiu hierárquica (G7), que é exatamente o item 1 da fila da FP-05 |
  | `list-io-map` (`nextFreeByteExact`, `nextFreeByteInDevice`) | **segurou** — o aviso foi lido e a rodada não fixou endereço; deixou o Portal atribuir e conferiu o mapa depois. A colisão da FP-05 não se repetiu |
  | `connect-subnet` (`existingSubnets`) | **segurou** — subnet inventada listou `PN/IE_1`/`PN/IE_2`; serviu de sonda barata |
  | `clone` / `delete-network` (`networks`, `networksBefore/After`) | **segurou** — o clone do `CHAMADA_INVERSORES_CCM3` declarou 11 redes antes de apagar uma a uma |
  | `set-io-address` (`conflictCheck`) | **não exercitado** — não houve endereço fixado à mão nesta rodada |
  | `add-call --fb` com prefixo de tipo | **doeu** (T2) |

## 5. Tropeços medidos da ferramenta

| # | Tropeço | Custo | O que fazer |
|---|---|---|---|
| T1 | `plug-module --type` exige o `TypeIdentifier` **com o prefixo `OrderNumber:`**. Com o MLFB puro (`6ES7 131-6BH00-0BA0/V1.1`) o dry-run devolve `canPlug: false` **sem dizer por quê** — o mesmo valor que "este slot não aceita este módulo" | 1 batch de 4 dry-runs perdido | aceitar MLFB sem prefixo (normalizar), ou devolver `reason` no `canPlug: false` |
| T2 | `add-call --fb`: o help escreve `--fb "FB Y\|FC Y"`, que se lê como "passe o tipo junto com o nome". Com `--fb "FC PARTIDA_BOMBA (BEF-01)"` os 5 steps falharam com `FB/FC '...' not found` | 5 steps + 1 batch | aceitar o prefixo opcional, ou trocar o texto do help para `--fb NOME` |
| T3 | `replicate-instruments` procura o tag `_PV_` **só nas tabelas da pasta de alarme da área** (`2.N`). Neste projeto os `_PV_` moram em `1. I/OS/QA-0N` — o FC saiu com 3 tags inexistentes (`LIT-51_PV_MACRO_MEDIDOR_VAZAO_INSTANTANEA`), 3 erros de compile | 1 compile + 1 batch de conserto (renomear as tags para a convenção `_PV_` + reimportar com `--replace`) | procurar o `_PV_` no PLC inteiro quando não achar na pasta da área — é o que o `replicate-fc` já faz com `MODO_LOCAL`/`MODO_REMOTO` |
| T4 | `replicate-instruments` morre com "Could not identify the template's mold instrument" quando as áreas do molde estão em `IgnoreFolders`. A mensagem não cita o campo que resolve (`MoldInstrumentId`) | 1 chamada | citar `MoldInstrumentId` na mensagem |
| T5 | `set-retain` logo depois de `import-source` no mesmo batch falha com `Inconsistent blocks ... cannot be exported`. É a regra conhecida do `CLAUDE.md`, mas `add-call`/`delete-network`/`add-db-member` **compilam o alvo sozinhos**; `set-retain` não | 4 steps perdidos, 2 vezes (a segunda por reimportar o FB) | `set-retain` seguir o mesmo padrão dos outros verbos que editam por XML: compilar o alvo antes de exportar |
| T6 | `gen-alarm-fc` não tem escopo: para criar 1 área ele regenerou (`update`) as **19 existentes**. Funcionou e o `CHAMADA_ALARMES` saiu com as 20 chamadas, mas o raio de ação de uma escrita de área é o projeto inteiro | 0 (nenhum dano observado; compile 0/0 depois) | aceitar `--area`/`IncludeFolders`, como `replicate-instruments` aceita `IgnoreFolders` |

Fora da ferramenta: dois processos `Siemens.Automation.Portal` abertos obrigaram `--portal` em todas
as chamadas — funcionou sem tropeço, e o `--portal` no `run --script` desce para o batch inteiro.

## 6. Fila que sai desta rodada

Ordenada por (dor evitada ÷ tamanho do diff):

1. **T3 — `replicate-instruments` acha o `_PV_` no PLC inteiro.** É o único tropeço que gerou bloco
   que não compila, e contra o projeto-molde de referência. O `replicate-fc` já tem o fallback
   pronto para copiar (`FindTag` com raiz alternativa).
2. **T5 — `set-retain` compila o alvo antes de exportar.** Alinha o verbo com os outros quatro que
   editam bloco por XML; some a classe de erro "escrevi duas vezes seguidas no mesmo bloco".
3. **T2 — `add-call --fb` aceita o prefixo `FB `/`FC `** (ou o help para de sugerir que ele existe).
   Diff de uma linha.
4. **T1 — `plug-module` normaliza o MLFB** (ou explica o `canPlug: false`). Diff pequeno, evita
   sonda cega em toda montagem de periferia nova.
5. **T6 — escopo de área no `gen-alarm-fc`.** Maior que os outros, e sem dano medido; entra por
   redução de raio de ação, não por bug.
6. **T4 — mensagem do molde do `replicate-instruments` cita `MoldInstrumentId`.** Uma string.

## 6.1 Fila fechada (2026-08-13, mesma data)

Os seis viraram código no mesmo dia, e cada um foi conferido **contra o projeto real**
(`PROJETO-MOLDE_V21`), não só no teste offline:

| # | O que mudou | Conferido por |
|---|---|---|
| T3 | `replicate-instruments` procura `<ID>_*_PV_*` no PLC inteiro quando a pasta da área não tem (`ReplicateFc.FindTag`), declara **`pvTag`** por instrumento no dry-run e avisa quando o molde usa PV e o alvo não tem tag | dry-run da área 24 saiu `in-sync` com o FC que a FP-06 consertou à mão, e os símbolos gerados são `LIT-51_PV_MEDIDOR_DE_NIVEL_POCO_ELEVATORIA_FINAL` (o nome real), não a substituição de nome |
| T5 | `BlockEdit.Patch` compila o alvo antes de exportar quando `!IsConsistent` — vale para `set-retain`, `add-call` e `delete-network` de uma vez | `clone --apply` + `set-retain --apply` no **mesmo batch**: `was: Retain → now: NonRetain`, era `Inconsistent blocks ... cannot be exported` |
| T2 | `add-call --fb` aceita o prefixo `FB `/`FC ` (`BlockEdit.StripTypePrefix`, com teste offline) | `--fb "FC PARTIDA_BOMBA (BEF-01)"` resolveu (`fb: PARTIDA_BOMBA (BEF-01)`, `networksBefore: 6 → 7`) |
| T1 | `plug-module` normaliza MLFB sem `OrderNumber:`, cruza prefixo × versão no `plugAs` e devolve `reason` no `canPlug: false` (o `name` do módulo continua o MLFB pedido) | `6ES7 131-6BH00-0BA0/V1.1` no slot 6 do `Rack_0`: `canPlug: true`; sem versão: `plugAs: OrderNumber:6ES7 131-6BH00-0BA0/V1.0` |
| T6 | `gen-alarm-fc --area NOME` (repetível; `IncludeFolders` no config). Escopo que não casa falha listando as pastas | 1 área gerada em vez de 20, e o `CHAMADA_ALARMES` continuou com as 20 chamadas |
| T4 | a mensagem do molde cita `MoldInstrumentId` com exemplo | — |

Fora da fila, o item da seção 7 (**acionamento-semente**) virou verbo: `replicate-fc` ganhou
`--template` (molde de qualquer pasta, não só a 1ª irmã populada) e `--target-folder` (escopo dos
alvos). Dry-run no projeto real: molde `Bomba Submersível (B-10A)` (área 20) replicando sobre as 5
pastas de `4.4.1 Elevatória Final (EFE-01)` numa chamada — os ~10 min de derivação manual da FP-06.

## 7. Leitura da rodada

A rodada anterior mediu se a régua automática funciona; esta mediu se a doutrina escrita segura a
decisão **sem** check que reprove. Segurou: as quatro armadilhas foram recusadas com motivo escrito,
e o que o cliente queria de fato (simular em bancada, achar tudo num lugar, IHM sem reconfiguração)
foi atendido por outro caminho. O custo de contorno de CLI caiu de ~32 % para ~12 %, e 80 % dos
blocos vieram de gerador — a diferença de terreno em relação à FP-05 (inversor tem molde na casa,
partida direta não tinha) é a maior parte disso.

O que ainda depende de julgamento e não de ferramenta: **o acionamento-semente**. `replicate-fc`
replica entre pastas irmãs, então uma área nova exige derivar um molde à mão (export do
acionamento mais parecido, patch de texto no XML, `import-block` + 5 `create-instance-db`). Foram
~10 min dos 49. Não virou item de fila porque não está claro que valha um verbo novo — mas se a
próxima rodada tropeçar no mesmo lugar, vira.
