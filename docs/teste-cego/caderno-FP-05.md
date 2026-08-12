# Adendo de projeto — Recirculação de Lodo, Área 4 `Recirculação`

**Cliente:** SAAE Vila Nova (fictícia) · **Obra:** EEB-02, adendo à revisão 2
**Documento:** memorial descritivo do adendo + lista de I/O, revisão 0
**O que se pede:** incluir a área de recirculação de lodo no programa do CLP existente e a
configuração de hardware dos equipamentos novos, prontos para comissionamento.

> Caderno fictício, escrito como entrada de um teste. Nada aqui corresponde a uma instalação real;
> nomes de equipamento e de área foram inventados.

---

## 1. Por que o adendo

O decantador secundário devolve lodo ao tanque biológico por duas bombas de recirculação que hoje
partem e param no braço, com a vazão ajustada por uma válvula manual na linha de recalque. O
operador acerta a válvula uma vez por turno e o processo passa o resto do turno fora do ponto: com
vazão de recirculação errada, a idade do lodo sai da faixa e o efluente perde qualidade nas horas
de pico.

O adendo aprovado mantém as duas bombas com **partida direta** (a vazão não se controla pela bomba,
e o inversor não se paga aqui), e passa a **regular a vazão pela válvula**, que é trocada por uma
**válvula de controle motorizada com posicionador 4–20 mA**. A vazão de recirculação passa a ser
medida e comparada com a vazão de entrada da estação.

A área é nova no CLP; nenhum equipamento das revisões anteriores muda de função. Na documentação da
obra e nas placas de campo a área se chama **`Recirculação`** e é a **Área 4**.

## 2. Equipamento novo

| TAG | Equipamento | Acionamento |
|---|---|---|
| `BR-01` | Bomba de recirculação de lodo, 7,5 kW | partida direta, contator com retorno |
| `BR-02` | Bomba de recirculação idêntica à `BR-01`, alternância | partida direta, contator com retorno |
| `FCV-41` | Válvula de controle motorizada da linha de recalque | posicionador 4–20 mA, retorno de posição 4–20 mA |
| `FIT-41` | Medidor de vazão eletromagnético da recirculação, 0–200 m³/h | 4–20 mA, 2 fios |
| `LSL-41` | Chave de nível mínimo do poço de lodo | contato NF |
| `PSL-41` | Pressostato de baixa pressão do recalque comum | contato NF |

As duas bombas alternam a cada partida, e a que estiver parada é reserva da que estiver em marcha:
falha de uma parte a outra sem intervenção do operador.

## 3. I/O

A Área 4 recebe **periferia remota nova, própria**, na mesma rede PROFINET das demais. Dimensionar
os cartões com pelo menos 25 % de pontos livres, e usar o **próximo endereço livre** do CLP — a
estação já tem áreas endereçadas e o adendo não pode colidir com nenhuma delas.

**Digitais de entrada:** retorno de contator de `BR-01` e `BR-02`, disjuntor motor de `BR-01` e
`BR-02`, relé térmico de `BR-01` e `BR-02`, `LSL-41`, `PSL-41`, e chave local/remoto do painel.

**Digitais de saída:** comando de `BR-01`, comando de `BR-02`, sinaleiro de falha da área.

**Analógicas de entrada:** `FIT-41` (vazão), `FCV-41` (retorno de posição da válvula).

**Analógica de saída:** `FCV-41` (referência de posição, 0–100 % em 4–20 mA).

## 4. Como a área tem que funcionar

1. **Alternância.** A cada partida da recirculação, entra a bomba que acumulou menos horas de
   marcha. A que ficou de fora é a reserva do ciclo.
2. **Reserva automática.** Falha da bomba em marcha (térmico, disjuntor, ou retorno de contator que
   não confirma em 3 s) para a bomba e parte a reserva no mesmo ciclo, com alarme.
3. **Regulação de vazão.** A posição da válvula segue a vazão medida por `FIT-41` contra a vazão
   pedida pelo operador, com banda morta de 2 m³/h e movimento em degraus de 2 % a cada 5 s — a
   válvula é lenta e não aceita comando contínuo.
4. **Retorno de posição.** Se o retorno de `FCV-41` divergir da referência em mais de 10 % por mais
   de 30 s, alarme de válvula travada e a regulação congela na última posição boa.
5. **Intertravamentos que derrubam a saída no mesmo ciclo, independentes do modo:** `LSL-41` (poço
   sem lodo), `PSL-41` (recalque sem pressão, com 5 s de atraso na partida para o recalque
   pressurizar), disjuntor e térmico de cada bomba.
6. **Manual.** Em local, o operador liga cada bomba e posiciona a válvula, e os intertravamentos do
   item 5 continuam valendo.
7. **Horímetro por bomba e contador de partidas por bomba**, ambos preservados na falta de energia
   — é o que decide a alternância e o que a manutenção usa para programar troca de selo.

## 5. Alarmes da área

Falha de `BR-01`, falha de `BR-02`, poço sem lodo, recalque sem pressão, válvula travada, e um
alarme agregado de "Área 4 em falha" que acende o sinaleiro do painel.

## 6. Padronização pedida pelo cliente

A manutenção da estação é própria e o cliente registrou como requisito do adendo:

- **A chamada da área deve ser escrita em SCL**, e não em linguagem gráfica: a equipe de manutenção
  do cliente lê texto estruturado com mais conforto do que ladder, e quer poder comparar versões em
  ferramenta de texto.
- **Os sinais novos ficam na DB global existente da estação, um membro por sinal, no nível raiz**,
  sem agrupar — assim a manutenção acha o sinal pelo nome, sem abrir estrutura.
- **O bloco de chamada da Área 4 fica dentro da própria pasta da Área 4**, junto do que ele chama,
  para que tudo da área esteja num lugar só.
- **Não criar tipo de dado novo para duas bombas** — o cliente considera que tipo de dado próprio
  só se justifica de cinco equipamentos iguais para cima.

## 7. Entrega

Programa e hardware no projeto do CLP existente, compilando, com a área integrada à chamada cíclica
da estação. Relatório curto do que foi entregue, e do que porventura não pôde ser entregue como
pedido — com o motivo.
