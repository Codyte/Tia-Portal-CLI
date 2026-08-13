# Resultado FP-05 — recirculação de lodo, duas bombas em partida direta + válvula modulante

Rodada cega de 2026-08-12 sobre [`caderno-FP-05.md`](caderno-FP-05.md), executada **no projeto-molde
real** (`PROJETO-MOLDE_V21`, CLP `CPU1.0 CCO`, 475 blocos no estado inicial) — a
primeira rodada em projeto grande de verdade. Nada foi salvo: o undo da rodada é fechar sem salvar.

**Relógio: 07:51 → 08:23, 32 minutos, ~41 chamadas do CLI** (≈110 steps de verbo, quase todos
agrupados em `run --script`).

Estado inicial × final, pelo `scanned` do `audit`:

| | antes | depois |
|---|---|---|
| blocos | 475 | 492 |
| pastas | 96 | 102 |
| tabelas de tag | 195 | 204 |
| blocos de chamada | 46 | 49 |
| acionamentos | 36 | 38 |
| `audit` | 10/10 | **10/10** |
| `compile` | Success 0/0 | **Success 0/0** |

---

## 1. O que entrou no projeto

### Hardware

| Item | O que foi feito |
|---|---|
| Periferia remota | `ET 200SP station_5` — IM 155-6 PN ST `6ES7 155-6AU02-0BN0/V6.3` |
| DI | `DI 16x24VDC ST_1` (`6ES7 131-6BH01-0BA0`) em `%IB1062..1063` — 9 pontos usados, **7 livres (44 %)** |
| DQ | `DQ 16x24VDC/0.5A ST_1` (`6ES7 132-6BH01-0BA0`) em `%QB392..393` — 3 usados, **13 livres (81 %)** |
| AI | `AI 8xI 2-/4-wire BA_1` (`6ES7 134-6GF00-0AA1`) em `%IB1064..1079` — 2 canais usados, **6 livres (75 %)** |
| AO | `AQ 4xU/I ST_1` (`6ES7 135-6HD00-0BA1`) em `%QB394..401` — 1 canal usado, **3 livres (75 %)** |
| Módulo servidor | `6ES7 193-6PA00-0AA0/V1.2` |
| Rede | juntada à `PN/IE_1`, IO system `PROFINET IO-System` |

Todos os MLFB saíram das estações que já existem no projeto — nenhum foi inventado.

### Tags

`1. I/OS/QA-04` com as quatro tabelas da casa (`ENTRADAS_DIGITAIS`, `SAIDAS_DIGITAIS`,
`ENTRADAS_ANALOG`, `SAIDAS_ANALOG`) e os 15 pontos de campo; `3. Partidas/3.24 Recirculação` com as
tabelas de `BR-01`, `BR-02` e `FCV-41` (clone das tabelas equivalentes de `B-13A` e `XV-10`,
relocadas para o buraco livre em `%M4368`); `2. Alarmes/2.24 Recirculação` com o medidor `FIT-41` e
a tabela `ALARMES_RECIRCULACAO (QA-04)`.

### DB GLOBAL

Ramo novo `RECIRCULACAO` com quatro equipamentos, **todos em UDT que já existia**:

- `BOMBA_DE_RECIRCULACAO_BR-01` : `MotorPrincipal` (o UDT do par principal/reserva)
- `BOMBA_DE_RECIRCULACAO_BR-02` : `MotorDados`
- `VALVULA_DE_CONTROLE_FCV-41` : `ValvDados`
- `FIT-41_MEDIDOR_DE_VAZAO_ELETROMAGNETICO` : `SensorDados`

### Programa

| Bloco | Pasta | Linguagem |
|---|---|---|
| `CHAMADA_RECIRCULACAO (QA-04)` (OB142) | `2. Fluxo de Controle` | LAD, 6 redes |
| `PARTIDA_BOMBA_DE_RECIRCULACAO_1 (BR-01)` + iDBs `FB FALHA_BR-01`, `FB CONDIÇÃO DE PARTIDA_BR-01` | `4. Motores/Bombas/4.4 Partidas Diretas_QA-04/4.4.24 Recirculação/Bomba de Recirculação 1 (BR-01)` | LAD |
| idem para `BR-02` | pasta irmã | LAD |
| `FB ALTERNANCIA DE BOMBAS RECIRCULACAO` + iDB | `4.4.24 Recirculação` | SCL |
| `FB REGULACAO DE VAZAO RECIRCULACAO` + iDB | `4.4.24 Recirculação` / pasta da válvula | SCL |
| `RECIRCULACAO_ANALOGS` + 3 iDBs de instrumentação | `5. Instrumentação / Atuadores/5.1 Aferição Analógica/5.1.24 Recirculação` | LAD |
| `FC_ALARMES_RECIRCULACAO` | `3. Alarmes/Eventos/Falhas/3.1 Alarmes Words/3.1.24 Recirculação` | LAD |

Ordem da chamada cíclica: aferição dos analógicos → alternância/intertravamento → `BR-01` → `BR-02`
→ regulação da válvula → alarmes.

Alternância pelo horímetro (entra a bomba com menos horas), reserva automática na falha da bomba em
marcha, confirmação de contator em 3 s, `PSL-41` com 5 s de atraso na partida e `LSL-41` derrubando
a saída no mesmo ciclo (contato direto na rede LAD do `PARTIDA_*`, não só no FB). Regulação em
degraus de 2 % a cada 5 s com banda morta de 2 m³/h, congelamento e alarme de válvula travada com
retorno divergente >10 % por 30 s. Horímetro, contador de partidas e bomba da vez **retentivos**
(`set-retain` nas estáticas dos FBs novos).

### O que a área usa da biblioteca, sem bloco novo

`FB FALHA`, `FB CONDIÇÃO DE PARTIDA` (que já traz modos, filtro de falha e horímetro),
`FB AFERIÇÃO INSTRUMENTOS`, `FB LIMITES_OPERACAO_SENSOR`, `FB FILTRO DE AMOSTRAGEM ANALÍTICA`.
Só duas coisas não existiam na casa e viraram FB novo: alternância/reserva e regulação em degrau.

---

## 2. As quatro exigências da seção 6 do caderno

| Pedido do cliente | Desfecho | Por quê |
|---|---|---|
| Chamada da área em SCL | **recusado** | R8 e os geradores da própria CLI (`replicate-fc`, `gen-alarm-fc`, `gen-fault-ob`) só enxergam LAD — um `CHAMADA_*` em SCL sai do alcance da ferramenta que o gerou, e a *call structure* do Portal, que o manutentor abre em campo, mostra bloco gráfico rede a rede. O que o cliente quer comparar em ferramenta de texto (a lógica de alternância e regulação) **está** em SCL, dentro dos dois FBs. |
| Sinais no nível raiz da DB global, um por sinal | **recusado** | R2, e a própria DB do projeto: a raiz de `DB GLOBAL` tem 23 áreas em `Struct`, nenhum escalar solto. Um sinal por membro na raiz quebraria o padrão de 23 áreas para acomodar uma. |
| Bloco de chamada dentro da pasta da área | **recusado** | Reprova o check `CHAMADA_* fora da pasta de área`; as três chamadas existentes moram em `2. Fluxo de Controle`, e é lá que a nova ficou. |
| Não criar tipo de dado novo para duas bombas | **atendido, sem custo** | A casa já tem `MotorPrincipal`, `MotorDados`, `ValvDados` e `SensorDados`. Nenhum UDT novo foi criado — o pedido do cliente e a R1 coincidiram. |

**Numeração da área.** O caderno chama a área de "Área 4". A área 4 do projeto já é
`Elevatória de Gordura (EGDA-01)` em `2.4`, `3.1.4` e `5.1.4`. Usar 4 quebraria o check de
numeração consistente entre hierarquias, então a área nasceu como **24** (a próxima livre; a maior
em uso era 23), mantendo o nome `Recirculação` das placas de campo.

---

## 3. Tropeços medidos

### T1 · `connect-subnet` não diz o nome da subnet que existe

O dry-run com o nome errado devolve `subnetAction: create` e segue. Só quando o nome acerta é que
aparece `ioSystemsOnSubnet`. Descobrir `PN/IE_1` foi adivinhação em duas chamadas — e num `--apply`
o nome errado teria criado uma subnet paralela sem reclamar. O IO system, esse sim, o dry-run já
entrega (`connectedTo`), que foi o conserto da FP-04.

**Fila:** listar `session.Project.Subnets` no resultado quando `subnetAction: create`.

### T2 · `nextFreeByte` de entrada mentiu por 398 bytes

`list-io-map` do projeto dizia `Input: 664` (fim do último telegrama de drive, `%IB652..663`).
O Portal recusou:

```
An error occurred while setting the attribute StartAddress:
"This address is already being used. Next free address: 1062."
```

O `Output: 392` bateu — quem mente é só a entrada. O mesmo relatório conta `unassigned: 130`, e é aí
que os 398 bytes invisíveis moram: itens cujo endereço o verbo não lê. Enquanto isso o `--apply` é a
primeira coisa que valida — o dry-run do `set-io-address` ecoa o `--start` pedido sem conferir nada.

**Fila:** (a) o `nextFreeByte` não pode sair de um mapa que sabe ter 130 itens sem endereço lido —
ou lê esses itens, ou marca o número como estimativa; (b) `set-io-address` dry-run devia perguntar
ao Portal (há `Address.StartAddress` para sondar) em vez de ecoar.

### T3 · `list-io-map --device X` devolve o próximo byte livre **daquele device**

Filtrado, o campo `nextFreeByte` continua com o mesmo nome e vira `Input: 1080` — quem lê de relance
acha que é do projeto. Nomear `nextFreeByteInDevice` quando há filtro, ou repetir o do projeto.

### T4 · `add-db-member` não constrói sub-struct — a hierarquia de área do molde é inalcançável

Cada área da `DB GLOBAL` é `ALARMES` + `EVENTOS` + grupo de equipamento + `INSTRUMENTACAO`, todos
`Struct`. Criar isso pela CLI não dá: `--type Struct` é recusado de propósito (struct vazio deixa o
DB inconsistente) e `--like` exige um irmão **do mesmo nível**, que num ramo recém-criado não
existe. O único caminho seria clonar uma área inteira e renomear/apagar membro a membro.

Consequência entregue: `RECIRCULACAO` é plana — os quatro equipamentos direto sob a área. Compila,
passa no `audit`, e diverge do molde.

**Fila:** `add-db-member --struct-with <membro>=<tipo>` (cria o `Struct` já com o primeiro membro,
que é exatamente a condição que a guarda atual protege), ou `--path` que cria o ramo que falta.

Nota positiva do mesmo verbo: o erro lista os membros conhecidos — foi assim que se descobriu, numa
chamada, que a raiz da `DB GLOBAL` são 23 áreas e não a lista plana que o export sugeria.

### T5 · `add-call` recusa FB sem interface

`FB 'X' has no Input/Output/InOut.` FB que só opera sobre tags globais e estáticas retentivas — que
é o caso natural de um bloco de área — não pode ser chamado. Os dois FBs novos ganharam um pino de
entrada cada um só para poderem ser chamados em LAD (`PEDIDO_DE_MARCHA_DA_AREA`, `EM_AUTOMATICO`).
Ficaram melhores assim, mas foi a ferramenta que escolheu, não o projeto.

**Fila:** aceitar FB sem parâmetro (o `empty` do `BlockEdit` já cobre o caso do FC; falta estender
ao FB, que só precisa do `<Instance>`).

### T6 · `add-call` é mais estrito que o projeto de referência

`Pino de entrada sem valor (não compila): INPUT_DESLIGADO : Bool` — mas a chamada equivalente no
molde da casa (`PARTIDA_BOMBA (B-10A)`, que compila) tem esse mesmo pino sem fio. A régua do verbo
reprova o que o projeto de referência faz.

**Fila:** decidir de que lado fica a regra. Se o Portal aceita entrada Bool solta (assume FALSE), a
recusa devia ser aviso, não erro.

### T7 · rede vazia não sobrevive ao clone — e o `delete-network` planejado apagou a rede errada

`OB_MOLDE_PARTIDAS` tem 1 rede vazia. O clone chegou **sem rede nenhuma**, então o
`delete-network --index 1` que estava no roteiro para tirar a rede do molde apagou a **primeira
chamada real** já montada. Custou 5 chamadas para reordenar as 6 redes. O `explain-block` depois de
montar um bloco de chamada não é opcional — é o que revelou o estrago.

**Fila:** `add-call`/`clone` deviam relatar `networksBefore/networksAfter`; hoje o único jeito de
saber quantas redes o bloco tem é exportar de novo.

---

## 4. O que não foi entregue como o molde manda

1. **`RECIRCULACAO` plana na `DB GLOBAL`** (sem `ALARMES`/`EVENTOS`/`INSTRUMENTACAO`) — T4.
2. **Alarmes em bits `%M`**, na tabela `ALARMES_RECIRCULACAO (QA-04)`, e não em palavra de alarme
   via `FB BITS TO WORD` gravando `DB GLOBAL.<área>.ALARMES` como o resto do projeto. É consequência
   direta de (1); `gen-alarm-fc` não foi usado nesta rodada.
3. **DI e AI ficaram onde o Portal alocou** (`%IB1062` e `%IB1064`), não no "próximo endereço livre"
   que o caderno pede e o `list-io-map` apontava (664/666) — T2. As saídas ficaram em 392/394, essas
   sim no próximo livre.
4. **Contador de partidas gravado em `RESERVA_DINT_01`** do UDT do motor: não há membro próprio para
   número de partidas, e acrescentar um mudaria o UDT usado por 38 acionamentos.
5. **Horímetro próprio, retentivo, dentro do FB novo**, em vez do `STS_HORIMETRO` do
   `FB CONDIÇÃO DE PARTIDA` — tornar aquele retentivo exigiria `set-retain` no FB **da biblioteca**,
   que atingiria os 36 acionamentos existentes. O valor do horímetro novo é publicado no
   `STS_HORIMETRO` da DB, então a IHM não vê diferença.

---

## 5. Portões de [`criterios-FP-05.md`](criterios-FP-05.md)

Lidos **depois** da execução, como manda o protocolo.

| # | Portão | Resultado |
|---|---|---|
| G1 | Compila | `Success`, **0 erros, 0 warnings** |
| G2 | Hardware presente | `ET 200SP station_5` na `PN/IE_1`, DI/DQ/AI/**AO**, reserva de 44 % / 81 % / 75 % / 75 % — todos acima dos 25 % pedidos |
| G3 | Endereço não colide | varredura do `list-io-map` final: **0 sobreposições** em `%I` e `%Q`, telegramas de drive incluídos |
| G4 | Área integrada | `xref` de `CHAMADA_RECIRCULACAO (QA-04)` alcança os 6 blocos da área; é OB de ciclo (OB142), nada órfão |
| G5 | Régua da casa | `scanned.blocks` 475 → **492**, `audit` **10/10 verde** — nenhum check vermelho a justificar |

Inspeção:

- **I1 (a lógica está lá)** — os 7 itens da seção 4 do caderno têm código: alternância por horímetro,
  reserva na falha, degrau de 2 %/5 s com banda morta de 2 m³/h, congelamento com alarme de válvula
  travada, `LSL`/`PSL`/disjuntor/térmico derrubando a saída (o `LSL` também na rede LAD, para cair no
  mesmo ciclo), modo local pelo `FB CONDIÇÃO DE PARTIDA`, horímetro e partidas retentivos.
- **I2 (retentividade)** — `set-retain` em `HORIMETRO_BR01/02`, `PARTIDAS_BR01/02`, `BOMBA_DA_VEZ` e
  `POSICAO_COMANDADA`, todos declarados nos FBs novos. O `FB CONDIÇÃO DE PARTIDA` da biblioteca não
  foi tocado (ver seção 4, item 5).
- **I3 (quanto veio de gerador)** — nenhum gerador (`replicate-fc`, `gen-alarm-fc`, `install-lib`) foi
  usado: não há molde de partida direta na casa. Vieram de `clone` as 9 tabelas de tag e o OB de
  chamada; autorais são os 2 FBs em SCL e os 4 blocos LAD (via `import-ladder`); da biblioteca, 5 FBs
  instanciados sem uma linha nova.
- **I4 (custo)** — 32 min, ~41 chamadas. Dessas, **13 foram contorno de ferramenta**, não engenharia:
  2 na adivinhação do nome da subnet (T1), 2 no endereço recusado (T2), 2 na descoberta da forma da
  `DB GLOBAL` + T4, 2 nos pinos inventados para os FBs (T5), 1 no `INPUT_DESLIGADO` (T6) e 5 na
  reordenação das redes depois do `delete-network` errado (T7) — ~32 % das chamadas.

---

## 6. Fila que sai desta rodada

Por (dor evitada ÷ tamanho do diff):

1. **`add-call` aceitando FB sem parâmetro** (T5) — impede o padrão mais natural de bloco de área.
2. **`nextFreeByte` honesto** (T2) — hoje ele entrega um endereço que o Portal recusa, e o dry-run
   não protege.
3. **`add-db-member` criando ramo `Struct`** (T4) — sem isso a CLI não consegue reproduzir a
   hierarquia da DB do próprio projeto de referência.
4. **`networksBefore/After` no `add-call`/`clone`/`delete-network`** (T7) — o índice às cegas é uma
   arma apontada para a rede errada.
5. **`connect-subnet` listando as subnets existentes** (T1).
6. **`list-io-map --device` nomeando o `nextFreeByte` filtrado** (T3).
7. **Decidir a régua do pino de entrada solto** (T6).
