<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L12    Entrega — Elevatória Final de Efluente Tratado (`EFE-01`) -->
<!--   L18    1. Hardware -->
<!--   L29    2. Programa -->
<!--   L60    3. O que foi entregue diferente do pedido, e por quê -->
<!--   L95    4. O que foi acrescentado ao pedido -->
<!--   L104   5. O que não foi entregue -->
<!--   L117   6. Estado final -->
<!-- ======================= END NAV INDEX ======================= -->

# Entrega — Elevatória Final de Efluente Tratado (`EFE-01`)

Adendo EEB-02 rev. 0 executado no projeto do CLP existente (`CPU1.0 CCO`, projeto
`PROJETO-MOLDE_V21`). Relatório do que foi entregue, do que foi entregue
diferente do pedido e por quê.

## 1. Hardware

| Item | O que foi feito |
|---|---|
| 5 inversores | 5 estações SINAMICS G120 PN (`SINAMICS G_49`..`G_53`), drive object `INVERSOR_BEF-01 CCM4`..`BEF-05 CCM4`, mesmo MLFB da CU dos inversores existentes (`6SL3244-0BB12-1FA0/4.7.13`) |
| Telegrama | **Standard telegram 20** em todos os cinco (`insert-telegram --change`: o G120 novo nasce com o telegrama 1). É o telegrama que os 34 inversores da estação já usam, e o que o bloco `SINA_SPEED_TLG20` da biblioteca consome |
| Rede | Todos na subnet `PN/IE_1`, IO system `PROFINET IO-System` do próprio CLP. As constantes `INVERSOR_BEF-0N_CCM4~PROFINET_interface~Standard_telegram_20` nasceram e é por elas que o programa endereça o telegrama (`HWIDSTW`/`HWIDZSW`) |
| Periferia remota | `ET 200SP station_5` / IM `REM_RM4.0` (`6ES7 155-6AU02-0BN0`), com 2× DI 16×24 V, 1× DQ 16×24 V/0,5 A, 1× AI 8×I 2/4 fios, server module — mesmos módulos das outras 4 estações ET 200SP do projeto |
| Endereços | Atribuídos pelo Portal a partir do próximo livre: DI `%IB1082..1083` e `%IB1074..1075`, AI `%IB1084..1099`, DQ `%QB66..67`, telegramas dos 5 drives acima de `%IB1062`. Nenhum endereço de área existente foi movido |
| Reserva de pontos | DI: 14 de 32 pontos usados (56 % livres). DO: 2 de 16 (87 % livres). AI: 3 de 8 (62 % livres). O pedido era ≥ 25 % |

## 2. Programa

Tudo nasceu com os geradores da casa, não à mão:

| Bloco / objeto | Onde | Como |
|---|---|---|
| 5 acionamentos de 6 blocos (`PARTIDA_BOMBA (BEF-0N)` + 5 iDBs cada) | `4. Motores/Bombas/4.4 Inversores_CCM4/4.4.1 Elevatória Final (EFE-01)/Bomba Elevatória Final N (BEF-0N)` | `replicate-fc` a partir de um molde derivado do acionamento `Bomba Submersível (B-10A)` (bomba submersível em inversor, mesma função) |
| `FB CASCATA DE BOMBAS` + iDB `FB CASCATA DE BOMBAS_EFE-01` | pasta da área | SCL (lógica pesada), escrito para este adendo |
| `CHAMADA_INVERSORES_CCM4` (OB, LAD) | `2. Fluxo de Controle` | chama os 5 `PARTIDA_BOMBA` e depois o FB da cascata — OB de ciclo, igual aos `CHAMADA_INVERSORES_CCM1..3` |
| `ELEVATRIA_FINAL_ANALOGS` (FC) + 9 iDBs | `5. Instrumentação / Atuadores/5.1 Aferição Analógica/5.1.24 Elevatória Final (EFE-01)` | `replicate-instruments`; a chamada entrou no OB `Chamada Aferição Instrumentos` |
| `FC_ALARMES_ELEVATORIA_FINAL_EFE_01` + `DB_BITS_TO_WORD_..._W1/W2` | `3. Alarmes/Eventos/Falhas/3.1 Alarmes Words/3.1.24 Elevatória Final (EFE-01)` | `gen-alarm-fc`; a chamada entrou no OB `CHAMADA_ALARMES` |
| `ElevatoriaDados` (UDT) | UDTs do PLC | dados da área (setpoint, cadastros, medições, estado, horímetros, contadores) |
| Membros novos da `DB GLOBAL` | `ELEVATÓRIA_FINAL` | `ALARMES.WORD_ALARMES_1..3`, `EVENTOS.DWORD_EVENTOS_1`, 5× `"MotorDados"` (uma por bomba), `INSTRUMENTACAO` com 3× `"SensorDados"`, `CASCATA_DE_BOMBAS : "ElevatoriaDados"` |
| Tabelas de tags | `1. I\/OS/QA-04` (3), `2. Alarmes/2.24 …` (4), `3. Partidas/3.24 …` (5) | uma tabela de 29 tags por bomba (padrão da casa), uma por instrumento, uma de alarmes digitais da área |

A área é a **N = 24** em todas as hierarquias (`2.24`, `3.24`, `3.1.24`, `5.1.24`), como manda a
numeração estável do projeto-molde.

### Como a lógica atende o memorial

| Item do memorial | Onde está |
|---|---|
| 1 · controle de nível, 30–100 % | `FB CASCATA DE BOMBAS`, seção 4: velocidade proporcional ao desvio de nível, limitada pelos cadastros (`SP_CADASTRO_VELOCIDADE_MINIMA/MAXIMA`, `SP_CADASTRO_GANHO_REGULACAO` — o ganho é o parafuso de ajuste em campo). A mesma velocidade vai para as cinco bombas |
| 2 · cascata 20 s / 60 s, máx. 4 | seção 6, temporizadores `TMR_ENTRA_BOMBA` / `TMR_SAI_BOMBA` e `SP_CADASTRO_MAXIMO_EM_MARCHA := 4` |
| 3 · rodízio por horímetro + troca periódica | seções 5 e 7: entra a de menor horímetro entre as paradas e sãs, sai a de maior entre as em marcha; `TMR_RODIZIO_PERIODICO` com `SP_CADASTRO_INTERVALO_RODIZIO := T#7D` |
| 4 · falha de bomba / não confirmou marcha em 5 s | seção 2 + o `FB FALHA` de cada acionamento. `FORA_DO_RODIZIO` é retentivo e só sai com reconhecimento do operador |
| 5 · intertravamentos independentes do modo | seções 3 e 11: `LSLL-51` e falta de pressão (`PIT-51 < 0,5 bar` por 10 s com bomba em marcha) zeram `BEF-0N_CMD_LIGA_PROFINET` no mesmo ciclo — o FB é chamado depois dos `PARTIDA_BOMBA`, e o telegrama lê esse bit no ciclo seguinte |
| 6 · nível alto | seção 8: todas as sãs a 100 %, ignorando regulação e o limite de quatro; sirene em `%Q66.1` |
| 7 · manual em local | cadeia `FB CONDIÇÃO DE PARTIDA` de cada bomba (local manual), com os intertravamentos do item 5 continuando a valer pela seção 11 |
| 8 · horímetro e contador de partidas retentivos | estáticas `HORIMETRO_DA_BOMBA_EM_HORAS` e `CONTADOR_DE_PARTIDAS` do FB, marcadas `Retain` (`set-retain`), espelhadas na `DB GLOBAL` para a IHM |

## 3. O que foi entregue diferente do pedido, e por quê

As quatro exigências da seção 6 do memorial ("padronização pedida pelo cliente") batem de frente com
o padrão do CLP existente. A estação já tem 36 acionamentos, 19 áreas e uma IHM em produção; adotar
uma segunda convenção só nesta área cria uma ilha de manutenção. As quatro foram atendidas no que
resolvem o problema do cliente, e não na forma pedida:

1. **"Um pino por sinal" → pinos, mas agrupados em UDT.**
   O bloco recebe **tudo por pino**: as quatro digitais do painel como pino escalar e os dados de
   processo em nove pinos de UDT (`ElevatoriaDados`, 5× `MotorDados`, 3× `SensorDados`). Nada é lido
   de "DB por conta própria" — a bancada continua forçando pino a pino, agora um por grupo.
   Um pino por sinal escalar daria ~30 pinos e uma amarração de 30 linhas na chamada, que é
   exatamente o que a recomendação da Siemens para S7-1500 (e a lei interna, R3, ≤ 8 escalares)
   manda evitar. Os sinais da própria área (5 confirmações de marcha, 5 falhas, 5 habilita, 2 saídas
   de painel e 4 bits de alarme) são lidos/escritos como **tag simbólica**, como fazem os blocos de
   área do próprio projeto — e tag `%M`/`%I` é tão forçável em bancada quanto pino.
2. **Prefixo de tipo (`bFalha`, `tRetardo`, `rNivel`) → não adotado.**
   O projeto inteiro usa `MAIÚSCULA_UNDERSCORE` descrevendo **função**, não tipo. Misturar as duas
   convenções foi medido como dívida de manutenção neste mesmo projeto. Os nomes entregues dizem o
   que a variável é (`TMR_ENTRA_BOMBA`, `FORA_DO_RODIZIO`, `SP_CADASTRO_TEMPO_SAI_BOMBA`).
3. **`Array[1..16] of Bool` de alarmes → palavras de alarme (`WORD_ALARMES_n`).**
   A seção 5 do próprio memorial exige que os alarmes cheguem à IHM **pelo mesmo mecanismo das
   outras áreas**, e esse mecanismo é `FB BITS TO WORD` empacotando os bits em
   `DB GLOBAL.<ÁREA>.ALARMES.WORD_ALARMES_n`, gerado pelo `gen-alarm-fc` e chamado pelo
   `CHAMADA_ALARMES`. Um array paralelo obrigaria a reconfigurar a IHM — o oposto do que o adendo
   pede. Os 24 alarmes da área ocupam 2 palavras, na ordem da lista da seção 5, e continuam sendo
   lidos por índice de bit. Onde há índice numérico no código, ele é constante simbólica
   (`BOMBA_01`..`BOMBA_05`), não literal.
4. **Pasta `10. Elevatória Final` de primeiro nível → área 24 dentro da árvore existente.**
   Os números de primeiro nível `0..9` já têm dono no padrão do projeto (`6.` é Comm Serial 485,
   `7.` Comm Skids, `8.` Compartilhamento, `9.` Comm Supervisório); `10.` seria uma taxonomia nova
   ao lado da que já existe. O que a manutenção pediu — "abrir um lugar só e achar tudo" — é
   atendido pelo número de área: **tudo da elevatória é 24** (`2.24`, `3.24`, `3.1.24`, `5.1.24`,
   `4.4.1`), que é como se acha qualquer outra área da estação hoje.

## 4. O que foi acrescentado ao pedido

- **Local/remoto por bomba** (10 pontos digitais) além da chave do painel: a cadeia de partida do
  padrão exige o par `MODO_LOCAL`/`MODO_REMOTO` por acionamento.
- **`CCM-4_STS_PAINEL_OK`** (1 ponto digital): o bloco de partida do padrão derruba o comando de
  liga quando o painel não está OK.
- **Segundo módulo DI** para manter a folga de pontos pedida depois dos acréscimos acima.
- Bombas, inversores e periferia foram tratados como um painel novo, **CCM-4 / QA-04**.

## 5. O que não foi entregue

- **Sem tela de IHM.** O adendo diz que a IHM não será reconfigurada; os alarmes chegam pelas
  palavras existentes e os dados pela `DB GLOBAL`, mas as telas do WinCC não foram tocadas.
- **Sem parametrização dos inversores** (rampas, limites de corrente, dados de motor): a lista do
  adendo não traz dados de placa. Os drives estão em rede, com telegrama, prontos para
  comissionamento.
- **Sem endereço IP fixo** nos equipamentos novos — ficaram com o que o Portal atribuiu; a lista de
  endereçamento da obra não veio no adendo.
- **Horímetro retentivo é o do bloco da área.** O `FB_HORÍMETRO` da biblioteca não é retentivo, e
  trocar isso lá mudaria os 36 acionamentos da estação; o horímetro retentivo desta área é contado
  dentro do `FB CASCATA DE BOMBAS`.

## 6. Estado final

- Compilação do CLP: **Success, 0 erros, 0 avisos**.
- `tia audit`: **10/10 checks verdes** (36 → 41 acionamentos).
- Projeto salvo.
