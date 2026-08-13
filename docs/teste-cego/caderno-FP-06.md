# Adendo de projeto — Elevatória Final de Efluente Tratado, `EFE-01`

**Cliente:** SAAE Vila Nova (fictícia) · **Obra:** EEB-02, adendo à revisão 3
**Documento:** memorial descritivo do adendo + lista de I/O, revisão 0
**O que se pede:** incluir a elevatória final de efluente tratado no programa do CLP existente e a
configuração de hardware dos equipamentos novos, prontos para comissionamento.

> Caderno fictício, escrito como entrada de um teste. Nada aqui corresponde a uma instalação real;
> nomes de equipamento e de área foram inventados.

---

## 1. Por que o adendo

O efluente tratado é hoje recalcado ao corpo receptor por três bombas antigas com partida direta,
que ligam e desligam por boia. O regime liga-desliga bate golpe de aríete na linha de recalque, e
o poço de sucção oscila entre cheio e vazio o tempo todo. Na hora de pico a estação não vaza o que
recebe; fora do pico, as bombas ficam partindo e parando a cada poucos minutos.

O adendo aprovado troca o conjunto por **cinco bombas submersíveis novas, idênticas, cada uma com
seu inversor de frequência**, e passa a controlar o nível do poço em vez de ligar por boia: a
elevatória mantém o nível no valor pedido pelo operador, acrescentando e tirando bomba conforme a
vazão afluente. As bombas não são mais dimensionadas para o pico uma a uma — o pico se atende
somando bombas.

A área é nova no CLP; nenhum equipamento das revisões anteriores muda de função. Na documentação da
obra e nas placas de campo a área se chama **`Elevatória Final`**; a numeração dela dentro do CLP
fica a critério da integração.

## 2. Equipamento novo

| TAG | Equipamento | Acionamento |
|---|---|---|
| `BEF-01` a `BEF-05` | Cinco bombas submersíveis idênticas, 15 kW cada | inversor de frequência, um por bomba, em rede |
| `LIT-51` | Medidor de nível do poço de sucção, 0–5 m | 4–20 mA, 2 fios |
| `FIT-51` | Medidor de vazão eletromagnético do recalque comum, 0–600 m³/h | 4–20 mA, 2 fios |
| `PIT-51` | Transmissor de pressão do recalque comum, 0–10 bar | 4–20 mA, 2 fios |
| `LSH-51` | Chave de nível alto do poço (extravasamento iminente) | contato NA |
| `LSLL-51` | Chave de nível muito baixo do poço (proteção de afogamento das bombas) | contato NF |

As cinco bombas são intercambiáveis: qualquer uma pode ser a primeira a entrar, e a manutenção
troca uma pela outra sem alterar programa.

**Os inversores são da mesma família dos que a estação já usa**, na mesma rede PROFINET do CLP, e
falam com o CLP pela rede — comando, referência de velocidade, velocidade real, corrente e falha
não passam por fiação de I/O. Adotar o mesmo telegrama que os inversores existentes da estação já
usam, para que a manutenção tenha um padrão só.

## 3. I/O

A Área recebe **periferia remota nova, própria**, na mesma rede PROFINET das demais. Dimensionar os
cartões com pelo menos 25 % de pontos livres, e usar o **próximo endereço livre** do CLP — a estação
já tem áreas endereçadas e o adendo não pode colidir com nenhuma delas.

**Digitais de entrada:** `LSH-51`, `LSLL-51`, chave local/remoto do painel, e botão de reconhecimento
de alarme do painel.

**Digitais de saída:** sinaleiro de falha da área e sirene de nível alto.

**Analógicas de entrada:** `LIT-51`, `FIT-51`, `PIT-51`.

Não há analógica de saída: a referência de velocidade das bombas vai pela rede.

## 4. Como a área tem que funcionar

1. **Controle de nível.** A elevatória mantém o nível do poço no valor pedido pelo operador
   (faixa de trabalho 0,8 m a 3,5 m). A velocidade das bombas em marcha é a mesma para todas, e sai
   da regulação do nível — nunca abaixo de 30 % nem acima de 100 %.
2. **Entrada e saída de bomba em cascata.** Com as bombas em marcha em 100 % e o nível ainda subindo
   por mais de 20 s, entra mais uma bomba. Com as bombas em marcha em 30 % e o nível ainda caindo
   por mais de 60 s, sai uma bomba. No máximo **quatro bombas em marcha ao mesmo tempo** — a quinta
   é sempre reserva.
3. **Rodízio.** A bomba que entra é a de menor horímetro entre as paradas e sãs; a que sai é a de
   maior horímetro entre as em marcha. Uma vez por semana, com a estação em regime, a de maior
   horímetro em marcha é trocada pela de menor horímetro parada, mesmo sem mudança de demanda.
4. **Falha de bomba.** Falha do inversor, ou bomba que não confirma marcha em 5 s depois do comando,
   tira a bomba do rodízio, alarma, e a próxima da fila entra no mesmo ciclo. Bomba fora do rodízio
   só volta com reconhecimento do operador.
5. **Intertravamentos que derrubam todas as saídas no mesmo ciclo, independentes do modo:**
   `LSLL-51` (poço vazio, risco de afogar as bombas), e falta de pressão no recalque comum medida
   por `PIT-51` abaixo de 0,5 bar por mais de 10 s com bomba em marcha.
6. **Nível alto.** Com `LSH-51` atuado, todas as bombas sãs entram em marcha a 100 %, ignorando a
   regulação e o limite de quatro, e a sirene toca até o nível normalizar.
7. **Manual.** Em local, o operador liga cada bomba e ajusta a velocidade dela, e os
   intertravamentos do item 5 continuam valendo.
8. **Horímetro por bomba e contador de partidas por bomba**, ambos preservados na falta de energia —
   é o que decide o rodízio e o que a manutenção usa para programar troca de selo.

## 5. Alarmes da área

Falha de cada uma das cinco bombas, poço vazio (`LSLL-51`), nível alto (`LSH-51`), falta de pressão
no recalque, falha de cada um dos três medidores (sinal fora da faixa de 4–20 mA), e um alarme
agregado de "Elevatória Final em falha" que acende o sinaleiro do painel.

Os alarmes da área têm de chegar à IHM pelo **mesmo mecanismo que os alarmes das outras áreas da
estação já usam** — a IHM da obra não vai ser reconfigurada por causa do adendo.

## 6. Padronização pedida pelo cliente

A manutenção da estação é própria e o cliente registrou como requisito do adendo:

- **O bloco de lógica da elevatória recebe todos os sinais por pino de interface, um pino por
  sinal** (nível, vazão, pressão, as cinco confirmações de marcha, as cinco falhas, setpoint, modo,
  reconhecimento). A equipe do cliente simula bloco em bancada forçando pino a pino e não quer
  depender de dado que o bloco leia por conta própria.
- **Nomes internos com prefixo de tipo** — `bFalhaBEF01`, `tRetardoCascata`, `rNivelMedido`,
  `iBombaDaVez` — que é o padrão da engenharia do cliente em todos os equipamentos da planta.
- **Os alarmes da área num `Array[1..16] of Bool`**, indexado por número, na ordem da lista da seção
  5, porque a IHM existente do cliente lê alarme por índice.
- **Tudo da área nova numa pasta de primeiro nível só dela**, chamada `10. Elevatória Final`, para
  que a manutenção abra um lugar só e ache tudo.

## 7. Entrega

Programa e hardware no projeto do CLP existente, compilando, com a área integrada à chamada cíclica
da estação. Relatório curto do que foi entregue, e do que porventura não pôde ser entregue como
pedido — com o motivo.
