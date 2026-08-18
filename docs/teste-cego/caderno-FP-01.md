<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L15    Automação do Filtro Prensa FP-01 — Sala de Desidratação -->
<!--   L26    1. Descrição do processo -->
<!--   L37    2. Equipamentos -->
<!--   L51    3. Hardware de controle -->
<!--   L60    4. Lista de I/O -->
<!--   L107   5. Modos de operação -->
<!--   L119   6. Sequência automática -->
<!--   L139   7. Intertravamentos -->
<!--   L152   8. Alarmes -->
<!--   L172   9. Entregável -->
<!-- ======================= END NAV INDEX ======================= -->

# Automação do Filtro Prensa FP-01 — Sala de Desidratação

**Cliente:** ETE Rio Claro (fictícia) · **Obra:** reforma da sala de desidratação de lodo
**Documento:** memorial descritivo + lista de I/O, revisão 0
**O que se pede:** programa do CLP e configuração de hardware do FP-01, prontos para comissionamento.

> Este é um caderno fictício, escrito para servir de entrada de um teste. Nada aqui corresponde a
> uma instalação real, e os nomes de equipamento e de área foram inventados.

---

## 1. Descrição do processo

O lodo adensado é bombeado do tanque pulmão para dentro do filtro prensa de placas FP-01. Um
conjunto hidráulico fecha o pacote de placas antes da alimentação; a bomba de lodo pressuriza a
câmara até a torta se formar; o filtrado sai por gravidade pelo dreno; ao fim do ciclo a linha de
alimentação é soprada com ar comprimido, o pacote é despressurizado e aberto, a torta cai na caçamba
e a tela é lavada com água de serviço antes do ciclo seguinte.

O ciclo é longo (60 a 90 minutos) e roda desassistido. O operador acompanha pela IHM e só intervém
para descarga travada ou falha.

## 2. Equipamentos

| TAG | Equipamento | Acionamento |
|---|---|---|
| `BL-01` | Bomba de lodo de alimentação, 7,5 kW | inversor SINAMICS G120 CU240E-2 PN, `6SL3244-0BB12-1FA0/4.7.13`, comandado por PROFINET (telegrama padrão 20) |
| `BH-01` | Unidade hidráulica de fechamento, 4 kW | partida direta |
| `BW-01` | Bomba de água de lavagem de tela, 3 kW | partida direta |
| `YV-01` | Solenoide hidráulica — fechar pacote | bobina 24 Vcc |
| `YV-02` | Solenoide hidráulica — abrir pacote | bobina 24 Vcc |
| `SV-01` | Válvula de dreno de filtrado | solenoide 24 Vcc |
| `SV-02` | Válvula de sopro de core (ar comprimido) | solenoide 24 Vcc |
| `PIT-01` | Transmissor de pressão de alimentação, 0–10 bar | 4–20 mA, 2 fios |
| `PIT-02` | Transmissor de pressão hidráulica, 0–250 bar | 4–20 mA, 2 fios |

## 3. Hardware de controle

- CPU **S7-1515-2 PN**, `6ES7 515-2AN03-0AB0/V3.1`, nome `CPU_FP01`, IP `192.168.0.10/24`.
- Periferia remota **ET200SP** no painel do filtro, nome `ET200_FP01`, IP `192.168.0.11/24`,
  na mesma sub-rede PROFINET da CPU. Cartões: 16 entradas digitais 24 Vcc, 16 saídas digitais
  24 Vcc/0,5 A, 8 entradas analógicas de corrente a 2 fios. O painel já está montado; a escolha dos
  códigos exatos dos módulos fica com o integrador, desde que a contagem de pontos seja atendida.
- Inversor `BL-01` no IP `192.168.0.20/24`, mesma sub-rede, como dispositivo IO da CPU.

## 4. Lista de I/O

### 4.1 Entradas digitais (ET200_FP01)

| Endereço | TAG | Descrição | Estado ativo |
|---|---|---|---|
| `%I0.0` | `ES-01` | Cadeia de emergência rearmada | 1 = liberado |
| `%I0.1` | `GS-01` | Grade de proteção fechada | 1 = fechada |
| `%I0.2` | `QM-BL01` | Disjuntor motor `BL-01` ok | 1 = ok |
| `%I0.3` | `QM-BH01` | Disjuntor motor `BH-01` ok | 1 = ok |
| `%I0.4` | `KM-BH01` | Retorno do contator `BH-01` | 1 = ligado |
| `%I0.5` | `QM-BW01` | Disjuntor motor `BW-01` ok | 1 = ok |
| `%I0.6` | `KM-BW01` | Retorno do contator `BW-01` | 1 = ligado |
| `%I0.7` | `ZSC-01` | Fim de curso: pacote fechado | 1 = fechado |
| `%I1.0` | `ZSO-01` | Fim de curso: pacote aberto | 1 = aberto |
| `%I1.1` | `LSL-01` | Nível baixo do tanque de lodo | 1 = nível baixo |
| `%I1.2` | `PSH-01` | Pressostato de segurança hidráulica (> 240 bar) | 1 = atuado |
| `%I1.3` | `HS-01` | Seletor Local / Remoto | 1 = remoto |
| `%I1.4` | `HS-02` | Botão de partida no campo | 1 = acionado |
| `%I1.5` | `HS-03` | Botão de parada no campo | 1 = acionado |
| `%I1.6` | `FS-01` | Chave de fluxo da água de lavagem | 1 = há fluxo |
| `%I1.7` | — | Reserva | — |

### 4.2 Saídas digitais (ET200_FP01)

| Endereço | TAG | Descrição |
|---|---|---|
| `%Q0.0` | `KM-BH01` | Contator da unidade hidráulica `BH-01` |
| `%Q0.1` | `KM-BW01` | Contator da bomba de lavagem `BW-01` |
| `%Q0.2` | `SV-01` | Válvula de dreno de filtrado |
| `%Q0.3` | `SV-02` | Válvula de sopro de core |
| `%Q0.4` | `YV-01` | Solenoide — fechar pacote |
| `%Q0.5` | `YV-02` | Solenoide — abrir pacote |
| `%Q0.6` | `HL-01` | Sinaleiro verde — em operação |
| `%Q0.7` | `HL-02` | Sinaleiro vermelho — falha |
| `%Q1.0` | `HA-01` | Sirene |
| `%Q1.1` a `%Q1.7` | — | Reserva |

### 4.3 Entradas analógicas (ET200_FP01)

| Endereço | TAG | Descrição | Faixa |
|---|---|---|---|
| `%IW64` | `PIT-01` | Pressão de alimentação | 0–10 bar (4–20 mA) |
| `%IW66` | `PIT-02` | Pressão hidráulica do pacote | 0–250 bar (4–20 mA) |

Velocidade e realimentação de `BL-01` trafegam pelo telegrama do inversor, não por cartão analógico.

## 5. Modos de operação

- **Automático** — a sequência do item 6 roda sozinha, partindo pela IHM ou pelo botão de campo
  `HS-02` com o seletor `HS-01` em Remoto.
- **Manual** — cada saída pode ser acionada individualmente pela IHM, **sempre** respeitando os
  intertravamentos de segurança do item 7. Manual não desabilita segurança.
- **Local** — com `HS-01` em Local, a IHM só monitora; no campo valem `HS-02` (partir) e `HS-03`
  (parar).

Parada pelo operador em qualquer ponto leva a sequência para descompressão (passo S5) antes de
repousar; nunca deixa o pacote pressurizado.

## 6. Sequência automática

| Passo | Ação | Condição para avançar | Tempo máximo |
|---|---|---|---|
| `S0` | Repouso: todas as saídas desligadas | comando de partida | — |
| `S1` | Fechar pacote: `YV-01` + `BH-01` | `ZSC-01` = 1 **e** `PIT-02` ≥ 180 bar | 120 s |
| `S2` | Manter pressão: `BH-01` desliga em 200 bar e religa em 170 bar; `SV-01` abre | pressão estabilizada por 5 s | 60 s |
| `S3` | Alimentar: `BL-01` parte em 25 Hz e sobe até 45 Hz em rampa de 30 s | `PIT-01` ≥ 7 bar mantido por 60 s | 90 min |
| `S4` | Soprar core: `BL-01` desliga, `SV-02` abre | tempo cumprido | 90 s |
| `S5` | Descomprimir: `YV-01` e `BH-01` desligam, `SV-01` fecha | `PIT-02` ≤ 20 bar | 60 s |
| `S6` | Abrir pacote: `YV-02` | `ZSO-01` = 1 | 120 s |
| `S7` | Descarga da torta | tempo cumprido (300 s) | 300 s |
| `S8` | Lavar tela: `BW-01` | tempo cumprido (180 s) | 180 s |

Ao fim de `S8` a sequência volta para `S0`. Estouro de tempo máximo em qualquer passo gera alarme,
interrompe a sequência e a leva para `S5` (descompressão) e depois `S0`.

A manutenção de pressão do passo `S2` continua ativa durante `S3` — o pacote não pode perder aperto
enquanto está sendo alimentado.

## 7. Intertravamentos

1. `ES-01` = 0 (emergência atuada) **ou** `GS-01` = 0 (grade aberta): todas as saídas caem
   imediatamente, a sequência vai para `S0` e só religa após rearme manual pela IHM.
2. `BL-01` não pode partir sem `ZSC-01` = 1 **e** `PIT-02` ≥ 150 bar.
3. `PSH-01` atuado desliga `BH-01` e `YV-01` no mesmo ciclo, com alarme.
4. `LSL-01` ativo por mais de 30 s durante `S3` encerra a alimentação e avança para `S4`.
5. `BW-01` é bloqueada enquanto `ZSC-01` = 1 (não se lava tela com pacote fechado).
6. `YV-01` e `YV-02` nunca energizadas ao mesmo tempo.
7. Falha de disjuntor (`QM-*` = 0) desliga o motor correspondente e alarma.
8. Divergência entre comando e retorno de contator (`KM-*`) por mais de 3 s alarma como falha de
   partida.

## 8. Alarmes

Todos com registro de data/hora e reconhecimento pela IHM. Os de segurança (1 a 3) desligam o
processo; os demais sinalizam.

| # | Alarme |
|---|---|
| 1 | Emergência atuada (`ES-01`) |
| 2 | Grade de proteção aberta (`GS-01`) |
| 3 | Sobrepressão hidráulica (`PSH-01`) |
| 4 | Falha do disjuntor `BL-01` |
| 5 | Falha do disjuntor `BH-01` |
| 6 | Falha do disjuntor `BW-01` |
| 7 | Falha de partida `BH-01` (comando sem retorno) |
| 8 | Falha de partida `BW-01` (comando sem retorno) |
| 9 | Falha do inversor `BL-01` (via telegrama) |
| 10 | Nível baixo do tanque de lodo (`LSL-01`) |
| 11 | Sem fluxo de água de lavagem (`FS-01` = 0 por 10 s com `BW-01` ligada) |
| 12 | Estouro de tempo de passo da sequência (com o número do passo) |

## 9. Entregável

Projeto do CLP compilando sem erros, com o hardware do item 3 configurado e endereçado, a lista de
I/O do item 4 lançada em tabelas de tag, e o programa organizado no padrão de pastas da casa —
um conjunto de blocos por equipamento, alarmes agrupados por área, e a sequência do item 6 como
bloco próprio, chamado ciclicamente.
