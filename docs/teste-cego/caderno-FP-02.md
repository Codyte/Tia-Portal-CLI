<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L17    Automação da Elevatória de Esgoto Bruto e do Tratamento Preliminar — EEB-02 -->
<!--   L28    1. Descrição do processo -->
<!--   L46    2. Equipamentos -->
<!--   L68    3. Hardware de controle -->
<!--   L84    4. Lista de I/O -->
<!--   L159   5. Modos de operação -->
<!--   L171   6. Operação da Área 1 — Elevatória -->
<!--   L192   7. Operação da Área 2 — Preliminar -->
<!--   L203   8. Diagnóstico de periferia -->
<!--   L210   9. Intertravamentos -->
<!--   L228   10. Alarmes -->
<!--   L265   11. Entregável -->
<!-- ======================= END NAV INDEX ======================= -->

# Automação da Elevatória de Esgoto Bruto e do Tratamento Preliminar — EEB-02

**Cliente:** SAAE Vila Nova (fictícia) · **Obra:** ampliação da elevatória final e do preliminar
**Documento:** memorial descritivo + lista de I/O, revisão 0
**O que se pede:** programa do CLP e configuração de hardware da EEB-02, prontos para comissionamento.

> Este é um caderno fictício, escrito para servir de entrada de um teste. Nada aqui corresponde a
> uma instalação real, e os nomes de equipamento e de área foram inventados.

---

## 1. Descrição do processo

A obra tem **duas áreas de processo**, no mesmo CLP e no mesmo painel de comando:

**Área 1 — Elevatória de Esgoto Bruto (`EEB-01`).** O esgoto bruto chega por gravidade ao poço de
sucção. Duas bombas submersíveis idênticas recalcam para o tratamento preliminar, com rodízio a cada
partida para igualar horas de operação. A linha de recalque tem uma válvula motorizada de bloqueio
que precisa estar aberta antes de qualquer bomba partir. O nível do poço é medido por ultrassom e a
vazão recalcada por medidor eletromagnético, com volume acumulado.

**Área 2 — Tratamento Preliminar (`TP-01`).** O esgoto recalcado passa por uma peneira rotativa que
retém sólidos grosseiros; os resíduos caem no transportador helicoidal, que descarrega na caçamba.
A peneira é lavada periodicamente com água de serviço. Transportador e peneira são intertravados:
a peneira não gira sem o transportador rodando.

As duas áreas rodam desassistidas. O operador acompanha pela IHM e só intervém para falha ou
manutenção.

## 2. Equipamentos

### 2.1 Área 1 — Elevatória de Esgoto Bruto (`EEB-01`)

| TAG | Equipamento | Acionamento |
|---|---|---|
| `BG-01A` | Bomba submersível de esgoto bruto 1, 15 kW | inversor SINAMICS G120 CU240E-2 PN, `6SL3244-0BB12-1FA0/4.7.13`, PROFINET (telegrama padrão 20) |
| `BG-01B` | Bomba submersível de esgoto bruto 2, 15 kW | inversor SINAMICS G120 CU240E-2 PN, `6SL3244-0BB12-1FA0/4.7.13`, PROFINET (telegrama padrão 20) |
| `MV-01` | Válvula motorizada de bloqueio do recalque, DN 200, atuador elétrico multivoltas | contatores abrir/fechar, fins de curso e limitadores de torque |
| `LIT-01` | Medidor de nível ultrassônico do poço, 0–6 m | 4–20 mA, 2 fios |
| `FIT-01` | Medidor eletromagnético de vazão do recalque, 0–250 m³/h | 4–20 mA, 2 fios, **com volume acumulado** |

### 2.2 Área 2 — Tratamento Preliminar (`TP-01`)

| TAG | Equipamento | Acionamento |
|---|---|---|
| `PN-01` | Peneira rotativa de sólidos grosseiros, 3 kW | inversor SINAMICS G120 CU240E-2 PN, `6SL3244-0BB12-1FA0/4.7.13`, PROFINET (telegrama padrão 20) |
| `TR-01` | Transportador helicoidal de resíduos, 2,2 kW | inversor SINAMICS G120 CU240E-2 PN, `6SL3244-0BB12-1FA0/4.7.13`, PROFINET (telegrama padrão 20) |
| `SV-10` | Válvula solenoide de água de lavagem da peneira | bobina 24 Vcc |
| `FIT-02` | Medidor de vazão da água de lavagem, 0–20 m³/h | 4–20 mA, 2 fios, **com volume acumulado** |
| `PIT-10` | Transmissor de pressão da água de serviço, 0–10 bar | 4–20 mA, 2 fios |

## 3. Hardware de controle

- CPU **S7-1515-2 PN**, `6ES7 515-2AN03-0AB0/V3.1`, nome `CPU_EEB02`, IP `192.168.0.10/24`.
- Periferia remota **ET200SP** da Área 1, nome `ET200_EEB`, IP `192.168.0.11/24`. Cartões:
  16 entradas digitais 24 Vcc, 16 saídas digitais 24 Vcc/0,5 A, 8 entradas analógicas de corrente
  a 2 fios.
- Periferia remota **ET200SP** da Área 2, nome `ET200_TP`, IP `192.168.0.12/24`, mesmos cartões.
- Inversores, todos como dispositivos IO da CPU na mesma sub-rede PROFINET:
  `BG-01A` em `192.168.0.21/24`, `BG-01B` em `.22`, `PN-01` em `.23`, `TR-01` em `.24`.
- Os dois painéis remotos são de fabricação recente e **devem ser diagnosticados pelo CLP**: perda
  de um módulo, módulo removido ou defeituoso tem que aparecer na IHM identificando qual periferia
  e qual módulo, não só "falha de rede".

Os códigos exatos dos módulos das periferias ficam com o integrador, desde que a contagem de pontos
seja atendida.

## 4. Lista de I/O

### 4.1 Entradas digitais — `ET200_EEB` (Área 1)

| Endereço | TAG | Descrição | Estado ativo |
|---|---|---|---|
| `%I0.0` | `ES-01` | Cadeia de emergência rearmada | 1 = liberado |
| `%I0.1` | `QM-BG01A` | Disjuntor motor `BG-01A` ok | 1 = ok |
| `%I0.2` | `QM-BG01B` | Disjuntor motor `BG-01B` ok | 1 = ok |
| `%I0.3` | `QM-MV01` | Disjuntor do atuador `MV-01` ok | 1 = ok |
| `%I0.4` | `ZSO-MV01` | `MV-01` — fim de curso aberto | 1 = aberta |
| `%I0.5` | `ZSC-MV01` | `MV-01` — fim de curso fechado | 1 = fechada |
| `%I0.6` | `ZSTO-MV01` | `MV-01` — torque na abertura | 1 = atuado |
| `%I0.7` | `ZSTC-MV01` | `MV-01` — torque no fechamento | 1 = atuado |
| `%I1.0` | `HS-MV01` | `MV-01` — seletor Local / Remoto do atuador | 1 = remoto |
| `%I1.1` | `LSH-01` | Boia de nível muito alto do poço (segurança) | 1 = atuada |
| `%I1.2` | `LSL-01` | Boia de nível muito baixo do poço (segurança) | 1 = atuada |
| `%I1.3` | `HS-01` | Seletor Local / Remoto do painel | 1 = remoto |
| `%I1.4` | `HS-02` | Botão de partida no campo | 1 = acionado |
| `%I1.5` | `HS-03` | Botão de parada no campo | 1 = acionado |
| `%I1.6` | `GS-01` | Tampa do poço fechada | 1 = fechada |
| `%I1.7` | — | Reserva | — |

### 4.2 Saídas digitais — `ET200_EEB` (Área 1)

| Endereço | TAG | Descrição |
|---|---|---|
| `%Q0.0` | `KM-MV01O` | Contator de abertura de `MV-01` |
| `%Q0.1` | `KM-MV01C` | Contator de fechamento de `MV-01` |
| `%Q0.2` | `HL-01` | Sinaleiro verde — elevatória em operação |
| `%Q0.3` | `HL-02` | Sinaleiro vermelho — falha na elevatória |
| `%Q0.4` | `HA-01` | Sirene da elevatória |
| `%Q0.5` a `%Q1.7` | — | Reserva |

### 4.3 Entradas analógicas — `ET200_EEB` (Área 1)

| Endereço | TAG | Descrição | Faixa |
|---|---|---|---|
| `%IW64` | `LIT-01` | Nível do poço de sucção | 0–6 m (4–20 mA) |
| `%IW66` | `FIT-01` | Vazão recalcada | 0–250 m³/h (4–20 mA) |

### 4.4 Entradas digitais — `ET200_TP` (Área 2)

| Endereço | TAG | Descrição | Estado ativo |
|---|---|---|---|
| `%I8.0` | `ES-10` | Cadeia de emergência do preliminar rearmada | 1 = liberado |
| `%I8.1` | `QM-PN01` | Disjuntor motor `PN-01` ok | 1 = ok |
| `%I8.2` | `QM-TR01` | Disjuntor motor `TR-01` ok | 1 = ok |
| `%I8.3` | `ZS-TR01` | Sensor de rotação do transportador `TR-01` | 1 = girando |
| `%I8.4` | `LSH-10` | Caçamba de resíduos cheia | 1 = cheia |
| `%I8.5` | `GS-10` | Guarda-corpo da peneira fechado | 1 = fechado |
| `%I8.6` | `HS-10` | Seletor Local / Remoto do preliminar | 1 = remoto |
| `%I8.7` | `HS-11` | Botão de partida no campo | 1 = acionado |
| `%I9.0` | `HS-12` | Botão de parada no campo | 1 = acionado |
| `%I9.1` a `%I9.7` | — | Reserva | — |

### 4.5 Saídas digitais — `ET200_TP` (Área 2)

| Endereço | TAG | Descrição |
|---|---|---|
| `%Q8.0` | `SV-10` | Válvula solenoide de água de lavagem da peneira |
| `%Q8.1` | `HL-10` | Sinaleiro verde — preliminar em operação |
| `%Q8.2` | `HL-11` | Sinaleiro vermelho — falha no preliminar |
| `%Q8.3` | `HA-10` | Sirene do preliminar |
| `%Q8.4` a `%Q9.7` | — | Reserva |

### 4.6 Entradas analógicas — `ET200_TP` (Área 2)

| Endereço | TAG | Descrição | Faixa |
|---|---|---|---|
| `%IW80` | `FIT-02` | Vazão da água de lavagem | 0–20 m³/h (4–20 mA) |
| `%IW82` | `PIT-10` | Pressão da água de serviço | 0–10 bar (4–20 mA) |

Comando, velocidade e realimentação dos quatro inversores trafegam pelo telegrama, não por cartão.

## 5. Modos de operação

- **Automático** — a lógica dos itens 6 e 7 roda sozinha, com o seletor da área em Remoto.
- **Manual** — cada acionamento e cada válvula podem ser comandados individualmente pela IHM,
  **sempre** respeitando os intertravamentos do item 9. Manual não desabilita segurança.
- **Local** — com o seletor da área em Local, a IHM só monitora; no campo valem os botões de partida
  e parada daquela área.

Cada bomba, a peneira e o transportador têm, na IHM, seleção individual de disponibilidade
(em serviço / fora de serviço para manutenção). Equipamento fora de serviço não entra no rodízio nem
é exigido pelos intertravamentos.

## 6. Operação da Área 1 — Elevatória

Controle por nível do poço, lido em `LIT-01`:

| Evento | Nível | Ação |
|---|---|---|
| Liga 1ª bomba | ≥ 2,50 m | parte a bomba da vez do rodízio |
| Liga 2ª bomba | ≥ 4,00 m | parte também a outra bomba |
| Desliga 2ª bomba | ≤ 3,00 m | para a última que entrou |
| Desliga todas | ≤ 1,20 m | para tudo e fecha `MV-01` |

- A frequência das bombas é modulada entre **30 e 55 Hz** para manter o nível em **3,00 m**; com as
  duas bombas ligadas as duas recebem a mesma referência.
- **Rodízio:** a cada partida da 1ª bomba, alterna qual das duas entra primeiro. Se uma estiver
  indisponível ou em falha, a outra assume sem alternar.
- `MV-01` abre antes de qualquer bomba partir, e só fecha 60 s depois da última parar. Tempo máximo
  de curso da válvula: **90 s** em cada sentido — estourou, alarme e a válvula é parada.
- Tempo mínimo entre partidas da mesma bomba: **180 s**. Tempo mínimo em operação: **60 s**.
- `FIT-01` acumula o volume recalcado em m³, com totalização contínua e reset pela IHM. O acumulado
  do dia anterior é preservado para relatório.

## 7. Operação da Área 2 — Preliminar

- Com o preliminar em Automático, a peneira `PN-01` roda sempre que houver bomba de esgoto ligada,
  e permanece 300 s ligada depois da última parar.
- `PN-01` gira a **frequência fixa de 40 Hz**; `TR-01` a **35 Hz**.
- `TR-01` parte **10 s antes** de `PN-01` e para **60 s depois** dela.
- **Lavagem da peneira:** com `PN-01` ligada, `SV-10` abre por 30 s a cada 10 min de operação.
  Durante a lavagem, `PIT-10` tem que indicar ≥ 2,0 bar e `FIT-02` tem que indicar vazão; se em 10 s
  não indicar, alarme de falta de água de lavagem e a lavagem é abortada.
- `FIT-02` acumula o volume de água de lavagem em m³, totalização contínua e reset pela IHM.

## 8. Diagnóstico de periferia

Falha de módulo em `ET200_EEB` ou em `ET200_TP` — módulo ausente, defeituoso ou com erro de canal —
gera alarme identificando a periferia e o módulo, e é registrado como palavra de diagnóstico na
base de dados do CLP, para a IHM listar. Perda completa da periferia leva a área correspondente para
parada segura.

## 9. Intertravamentos

1. `ES-01` = 0 ou `GS-01` = 0: todas as saídas da **Área 1** caem imediatamente; religa só após
   rearme manual pela IHM.
2. `ES-10` = 0 ou `GS-10` = 0: todas as saídas da **Área 2** caem imediatamente; religa só após
   rearme manual pela IHM.
3. `LSL-01` atuada (nível muito baixo) desliga as duas bombas no mesmo ciclo, independente da leitura
   de `LIT-01` — protege contra afogamento perdido por falha do ultrassom.
4. `LSH-01` atuada (nível muito alto) obriga as duas bombas a ligar, mesmo em Manual, e alarma.
5. Nenhuma bomba parte com `MV-01` fora da posição aberta (`ZSO-MV01` = 1).
6. `KM-MV01O` e `KM-MV01C` nunca energizados ao mesmo tempo, e a inversão de sentido só é permitida
   1 s depois de o sentido anterior cair.
7. `PN-01` não pode girar sem `ZS-TR01` = 1 (transportador confirmado em rotação).
8. `LSH-10` (caçamba cheia) por mais de 60 s para `TR-01` e `PN-01` e alarma.
9. Falha de disjuntor (`QM-*` = 0) desliga o equipamento correspondente e alarma.
10. Falha do inversor, via telegrama, desliga o equipamento correspondente e alarma.
11. `SV-10` só abre com `PN-01` ligada.

## 10. Alarmes

Todos com registro de data/hora e reconhecimento pela IHM, **agrupados por área**. Os de segurança
(1, 2, 11, 12) desligam o processo da sua área; os demais sinalizam.

### 10.1 Área 1 — Elevatória (`EEB-01`)

| # | Alarme |
|---|---|
| 1 | Emergência da elevatória atuada (`ES-01`) |
| 2 | Tampa do poço aberta (`GS-01`) |
| 3 | Falha do disjuntor `BG-01A` |
| 4 | Falha do disjuntor `BG-01B` |
| 5 | Falha do inversor `BG-01A` (via telegrama) |
| 6 | Falha do inversor `BG-01B` (via telegrama) |
| 7 | Nível muito alto do poço (`LSH-01`) |
| 8 | Nível muito baixo do poço (`LSL-01`) |
| 9 | Falha de curso de `MV-01` (90 s sem chegar ao fim de curso) |
| 10 | Torque de `MV-01` atuado fora de fim de curso (válvula travada) |
| 11 | Falha do disjuntor do atuador `MV-01` |
| 12 | Falha de módulo na periferia `ET200_EEB` |

### 10.2 Área 2 — Preliminar (`TP-01`)

| # | Alarme |
|---|---|
| 1 | Emergência do preliminar atuada (`ES-10`) |
| 2 | Guarda-corpo da peneira aberto (`GS-10`) |
| 3 | Falha do disjuntor `PN-01` |
| 4 | Falha do disjuntor `TR-01` |
| 5 | Falha do inversor `PN-01` (via telegrama) |
| 6 | Falha do inversor `TR-01` (via telegrama) |
| 7 | Transportador sem rotação com comando ligado (`ZS-TR01` = 0 por 5 s) |
| 8 | Caçamba de resíduos cheia (`LSH-10`) |
| 9 | Falta de água de lavagem (`PIT-10` < 2,0 bar ou `FIT-02` sem vazão durante a lavagem) |
| 10 | Falha de módulo na periferia `ET200_TP` |

## 11. Entregável

Projeto do CLP compilando sem erros, com o hardware do item 3 configurado e endereçado, a lista de
I/O do item 4 lançada em tabelas de tag, e o programa organizado no padrão de pastas da casa —
um conjunto de blocos por equipamento, alarmes agrupados por área, medições analógicas e
totalizadores tratados como tal, e as **duas áreas** identificadas de forma consistente em toda a
árvore do projeto (blocos, alarmes, instrumentação e tabelas de tag).
