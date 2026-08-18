<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L14    Adendo de projeto — Aeração do Tanque Biológico, Área 3 `Sopradores/Aeração` -->
<!--   L26    1. Por que o adendo -->
<!--   L41    2. Equipamento novo -->
<!--   L56    3. I/O -->
<!--   L74    4. Operação -->
<!--   L100   5. Proteções e alarmes -->
<!--   L122   6. Registros exigidos -->
<!--   L130   7. Aceite -->
<!--   L143   Contexto de execução (não faz parte do caderno do cliente) -->
<!-- ======================= END NAV INDEX ======================= -->

# Adendo de projeto — Aeração do Tanque Biológico, Área 3 `Sopradores/Aeração`

**Cliente:** SAAE Vila Nova (fictícia) · **Obra:** EEB-02, adendo à revisão 1
**Documento:** memorial descritivo do adendo + lista de I/O, revisão 0
**O que se pede:** incluir a área de aeração no programa do CLP existente e a configuração de
hardware dos equipamentos novos, prontos para comissionamento.

> Caderno fictício, escrito como entrada de um teste. Nada aqui corresponde a uma instalação real;
> nomes de equipamento e de área foram inventados.

---

## 1. Por que o adendo

O tanque biológico da obra estava previsto com aeração por difusores alimentados por sopradores de
velocidade fixa, comandados por temporizador. O laudo do processo condenou o arranjo: o oxigênio
dissolvido oscilava entre falta e excesso ao longo do dia, e o excesso é o que paga a conta de
energia da estação inteira.

A solução aprovada troca os dois sopradores por **sopradores de velocidade variável com inversor**,
e passa o comando a seguir a leitura de oxigênio dissolvido do próprio tanque. A área é nova no
CLP; nenhum equipamento das revisões anteriores muda de função.

A área nova se chama, na documentação da obra e nas placas de campo, **`Sopradores/Aeração`**, e é a
**Área 3**. O nome com barra é o da folha de dados aprovada e não pode ser trocado no programa —
manutenção procura pelo nome que está na placa.

## 2. Equipamento novo

| TAG | Equipamento | Acionamento |
|---|---|---|
| `SOP-01` | Soprador de lóbulos rotativos do tanque biológico, 15 kW | inversor, PROFINET |
| `SOP-02` | Soprador idêntico ao `SOP-01`, redundante | inversor, PROFINET |
| `AIT-31` | Analisador de oxigênio dissolvido do tanque, 0–10 mg/L | 4–20 mA, 2 fios |
| `PIT-31` | Transmissor de pressão da linha de ar, 0–1000 mbar | 4–20 mA, 2 fios |
| `TSH-31` | Termostato de sobretemperatura do `SOP-01` | contato NF |
| `TSH-32` | Termostato de sobretemperatura do `SOP-02` | contato NF |

Os dois inversores são de mesma família e mesma potência, ligados na **mesma rede PROFINET do CLP**,
e trocam com o CLP palavra de comando, referência de velocidade, palavra de estado e velocidade
real — o suficiente para ligar, desligar, dar referência, ler a velocidade e ler falha do inversor.

## 3. I/O

A Área 3 recebe **periferia remota nova**, própria, na mesma rede das demais. Dimensionar o cartão
de entrada analógica pelos instrumentos do item 2 e o de entrada digital pelos sinais abaixo,
deixando pelo menos 25 % de pontos livres para reserva.

| Sinal | Tipo | Descrição |
|---|---|---|
| Chave seccionadora local do `SOP-01` em "desligado" | DI | contato NF |
| Chave seccionadora local do `SOP-02` em "desligado" | DI | contato NF |
| Sobretemperatura `SOP-01` (`TSH-31`) | DI | contato NF |
| Sobretemperatura `SOP-02` (`TSH-32`) | DI | contato NF |
| Oxigênio dissolvido do tanque (`AIT-31`) | AI | 4–20 mA |
| Pressão da linha de ar (`PIT-31`) | AI | 4–20 mA |

Ligar, desligar, referência de velocidade, velocidade real e falha dos sopradores **não passam por
cartão de I/O** — vêm da rede, pelo inversor.

## 4. Operação

**Manual.** Pela IHM, com o seletor da área em Manual, o operador liga e desliga o soprador
escolhido e ajusta a velocidade dele em percentual. Todo intertravamento de proteção continua
valendo em Manual.

**Automático.** Com o seletor da área em Automático, **um soprador roda por vez** e a velocidade
segue o oxigênio dissolvido medido por `AIT-31`:

- abaixo do setpoint menos a banda morta, a velocidade sobe;
- acima do setpoint mais a banda morta, a velocidade desce;
- dentro da banda morta, a velocidade fica onde está.

A velocidade sobe e desce em rampa, nunca em degrau, e é limitada por um mínimo e um máximo — abaixo
do mínimo o soprador não refrigera a si mesmo. Setpoint de oxigênio, banda morta, velocidade mínima,
velocidade máxima e a rampa (em % por minuto) são todos parametrizáveis pela IHM sem recompilar o
programa.

**Rodízio.** O soprador em serviço é trocado a cada **12 horas de operação acumulada**, contadas por
soprador, e imediatamente quando o soprador em serviço vai para falha. O tempo de rodízio é
parametrizável. A troca respeita uma sobreposição: o soprador que entra atinge a velocidade mínima
antes de o que sai receber o comando de desligar, para a linha de ar não perder pressão.

O soprador **não parte** se a pressão da linha de ar estiver acima do limite de bloqueio — sinal de
registro de saída fechado.

## 5. Proteções e alarmes

Cada soprador vai para **falha**, desliga e exige reconhecimento do operador quando:

- o termostato de sobretemperatura dele atuar;
- a chave seccionadora local dele for para "desligado" com o soprador rodando;
- o inversor dele sinalizar falha;
- a velocidade real não alcançar a velocidade de referência, dentro de uma tolerância, em 60
  segundos após o comando de partida;
- a pressão da linha de ar passar do limite de segurança por mais de 5 segundos com ele rodando.

Alarme de área, que **não** desliga soprador nenhum:

- oxigênio dissolvido abaixo do mínimo por mais de 30 minutos com um soprador rodando na velocidade
  máxima — a aeração não dá conta e o operador precisa saber;
- oxigênio dissolvido acima do máximo por mais de 30 minutos;
- `AIT-31` fora de faixa (abaixo de 4 mA ou acima de 20 mA). Com o analisador fora de faixa, o
  automático **não** para: o soprador em serviço trava na última velocidade válida e o alarme fica
  ativo até o instrumento voltar.

Os dois sopradores em falha ao mesmo tempo é alarme crítico da estação.

## 6. Registros exigidos

Por soprador, todos retentivos, legíveis na IHM e zeráveis por comando do operador:

- **horímetro**;
- **contador de partidas**;
- **horas desde o último rodízio** — é o que decide a troca do item 4.

## 7. Aceite

- Programa compila sem erro e o hardware novo está configurado, endereçado e na rede.
- Os intertravamentos do item 5 desligam o soprador e exigem reconhecimento; os alarmes de área
  sinalizam sem desligar.
- O automático do item 4 roda sozinho, com rodízio e sobreposição.
- Setpoint, banda, velocidades, rampa e tempo de rodízio são alteráveis pela IHM sem recompilar.
- Os registros do item 6 sobrevivem a um desligamento do CLP.
- O programa segue o padrão da casa — quem for dar manutenção nele é a mesma equipe que mantém o
  resto da obra.

---

## Contexto de execução (não faz parte do caderno do cliente)

Projeto TIA de teste: `workspace/newlib/LIB_TESTE/LIB_TESTE.ap21`, CLP `PLC_ZERO`.
O CLP já tem a biblioteca de blocos da casa instalada e equipamentos das revisões anteriores.
O projeto **não** tem periferia remota nem inversor configurados: o hardware do item 2 e do item 3
entra nesta rodada.
