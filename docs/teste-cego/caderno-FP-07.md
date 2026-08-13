# Adendo de projeto — Adensador de Lodo por Gravidade, `ADG-01`

**Cliente:** SAAE Vila Nova (fictícia) · **Obra:** EEB-02, adendo à revisão 4
**Documento:** memorial descritivo do adendo + lista de I/O do diagrama elétrico revisão 2
**O que se pede:** incluir o adensador de lodo por gravidade no programa do CLP existente e a
configuração de hardware dos equipamentos novos, prontos para comissionamento **em duas etapas**.

> Caderno fictício, escrito como entrada de um teste. Nada aqui corresponde a uma instalação real;
> nomes de equipamento e de área foram inventados.

---

## 1. Por que o adendo

O lodo de descarte dos decantadores vai hoje direto para a desidratação, com 0,8 % de sólidos. A
centrífuga trabalha o dia inteiro para tirar água que um adensador tiraria por gravidade, e o
consumo de polímero está 40 % acima do previsto em projeto.

O adendo aprovado instala um **adensador por gravidade** entre o descarte dos decantadores e a
desidratação: o lodo entra pelo centro, a manta adensa por decantação, o **raspador de fundo** gira
lentamente empurrando o lodo adensado para o poço central, e **duas bombas de lodo adensado**
recalcam para a desidratação. O sobrenadante volta por gravidade ao tratamento, sem bombeamento.

A área é nova no CLP; nenhum equipamento das revisões anteriores muda de função. Na documentação da
obra e nas placas de campo a área se chama **`Adensador`**; a numeração dela dentro do CLP fica a
critério da integração.

## 2. Equipamento novo

| TAG | Equipamento | Acionamento |
|---|---|---|
| `BLA-01`, `BLA-02` | Duas bombas de lodo adensado idênticas, 5,5 kW, uma em serviço e uma reserva | partida direta |
| `RAS-01` | Raspador de fundo do adensador, motorredutor 1,5 kW, giro contínuo lento | partida direta |
| `LIT-61` | Medidor de manta de lodo do adensador, 0–6 m | 4–20 mA, 2 fios |
| `FIT-61` | Medidor de vazão eletromagnético do recalque de lodo, 0–40 m³/h | 4–20 mA, 2 fios |
| `DIT-61` | Medidor de concentração de sólidos do lodo adensado, 0–8 % | 4–20 mA, 2 fios |
| `LSH-61` | Chave de nível alto de sobrenadante (transbordo iminente) | contato NA |
| `ZSH-61` | Chave de torque alto do raspador (lodo compactado demais) | contato NF |

As duas bombas são intercambiáveis: qualquer uma pode ser a de serviço, e a manutenção troca uma
pela outra sem alterar programa. **O raspador não tem reserva** — parou o raspador, o adensador
não descarrega.

## 3. I/O — endereços fixos do diagrama elétrico

O diagrama elétrico revisão 2 **já foi aprovado, impresso e entregue em obra**, e a fiação foi
executada por ele. O borne de cada sinal está identificado com o endereço abaixo, e o teste de laço
do comissionamento vai conferir borne contra endereço. **Os endereços têm de ser exatamente estes** —
mudar endereço agora significa reimprimir o diagrama e reidentificar 22 bornes em campo.

A periferia remota da área já foi comprada e é a da lista abaixo, na mesma rede PROFINET das demais
estações.

| Módulo | MLFB | Endereço inicial |
|---|---|---|
| Interface da estação remota | `6ES7 155-6AU02-0BN0` | — |
| Entradas digitais 16 pontos | `6ES7 131-6BH01-0BA0` | `%IB1100` |
| Saídas digitais 16 pontos | `6ES7 132-6BH01-0BA0` | `%QB420` |
| Entradas analógicas 8 canais | `6ES7 134-6GF00-0AA1` | `%IB1110` |
| Módulo servidor | `6ES7 193-6PA00-0AA0` | — |

**Digitais de entrada** (`%I1100.0` em diante, nesta ordem): confirmação de marcha `BLA-01`,
confirmação de marcha `BLA-02`, confirmação de marcha `RAS-01`, disjuntor `BLA-01`, disjuntor
`BLA-02`, disjuntor `RAS-01`, `LSH-61`, `ZSH-61`, local/remoto do painel, reconhecimento de alarme
do painel.

**Digitais de saída** (`%Q420.0` em diante, nesta ordem): comando `BLA-01`, comando `BLA-02`,
comando `RAS-01`, sinaleiro de falha da área, sirene de transbordo.

**Analógicas de entrada** (`%IW1110` em diante, nesta ordem): `LIT-61`, `FIT-61`, `DIT-61`.

## 4. Como a área tem que funcionar

1. **Raspador em regime.** O raspador gira continuamente enquanto a área está em automático, e é o
   primeiro a partir: bomba de lodo não parte com raspador parado.
2. **Descarga de lodo por concentração.** Com a manta acima de 1,5 m **e** concentração medida por
   `DIT-61` acima de 2,5 %, a bomba de serviço parte e recalca até a manta cair abaixo de 0,8 m ou
   a concentração cair abaixo de 1,8 %. Descarga mínima de 3 min e máxima de 25 min por ciclo.
3. **Alternância das bombas.** A bomba de serviço é a de menor horímetro entre as sãs. Falha da
   bomba de serviço durante a descarga passa o comando para a outra no mesmo ciclo, alarma, e a que
   falhou sai do rodízio até reconhecimento do operador.
4. **Torque alto do raspador.** Com `ZSH-61` atuado por mais de 5 s, o raspador para, alarma, e a
   bomba de serviço parte em descarga forçada por 10 min para aliviar o fundo. Retorno do raspador
   só com reconhecimento do operador.
5. **Intertravamentos que derrubam as saídas no mesmo ciclo, independentes do modo:** disjuntor de
   cada acionamento sobre o seu comando, e raspador parado sobre as duas bombas.
6. **Transbordo.** Com `LSH-61` atuado, a bomba de serviço parte em descarga forçada e a sirene toca
   até o nível normalizar. `LSH-61` atuado por mais de 15 min é alarme crítico.
7. **Manual.** Em local, o operador liga cada acionamento individualmente, e os intertravamentos do
   item 5 continuam valendo.
8. **Horímetro e contador de partidas por acionamento** (as duas bombas e o raspador), preservados
   na falta de energia — é o que decide a alternância e o que a manutenção usa para programar troca
   de redutor e de selo.

## 5. Alarmes da área

Falha de cada um dos três acionamentos, transbordo (`LSH-61`), torque alto (`ZSH-61`), descarga que
excede o tempo máximo, falha de cada um dos três medidores (sinal fora da faixa de 4–20 mA), e um
alarme agregado de "Adensador em falha" que acende o sinaleiro do painel.

Os alarmes da área têm de chegar à IHM pelo **mesmo mecanismo que os alarmes das outras áreas da
estação já usam** — a IHM da obra não vai ser reconfigurada por causa do adendo.

## 6. Diagnóstico de hardware — requisito novo desta revisão

A estação remota da área nova fica num painel de campo a 180 m da sala de comando, no mesmo caminho
de fibra de duas estações que já tiveram queda de link no último ano. O cliente exigiu, e vale
para a estação nova e para as existentes:

- **Falha ou perda de estação remota, e falha de cartão, têm de gerar alarme identificando qual
  estação e qual cartão**, sem parar o CLP e sem depender de alguém abrir o TIA para ver o
  diagnóstico.
- O CLP **não pode ir para STOP** por falha de módulo de periferia: a planta continua com o que
  restou e o operador decide.

## 7. Padronização pedida pelo cliente

A manutenção da estação é própria e o cliente registrou como requisito do adendo:

- **Cada bomba com o seu próprio bloco de lógica, escrito separadamente**, para que alterar o
  comportamento de uma nunca afete a outra — a equipe já foi surpreendida por bloco compartilhado
  em outra obra.
- **Os dados do adensador soltos na raiz da base de dados global, um membro por sinal**, sem
  agrupar por equipamento: a IHM da obra é configurada apontando membro a membro e quem configura
  não quer navegar hierarquia.
- **O bloco de diagnóstico de falha de estação dentro da pasta da área nova**, junto com o resto do
  adendo, para que a manutenção abra um lugar só.

## 8. Entrega, em duas etapas

O comissionamento da obra é em duas frentes e o adendo tem de acompanhar:

**Etapa 1 — laço.** Hardware da área configurado e endereçado conforme a seção 3, sinais de campo
disponíveis para o teste de laço (o eletricista aciona o borne e confere o sinal no CLP). Sem
lógica de processo ainda. **Ao fim da etapa 1, registrar o relatório de conformidade do projeto
como ele estiver** — inclusive o que ele acusar de faltando, que é o esperado nesta etapa.

**Etapa 2 — processo.** A lógica das seções 4, 5 e 6, a área integrada à chamada cíclica da estação,
o projeto compilando. **Ao fim da etapa 2, registrar o relatório de conformidade de novo.**

Relatório curto do que foi entregue em cada etapa, e do que porventura não pôde ser entregue como
pedido — com o motivo.
