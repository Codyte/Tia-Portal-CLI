# Adendo de projeto — Agitador do Tanque de Equalização `AG-05`

**Cliente:** SAAE Vila Nova (fictícia) · **Obra:** EEB-02, adendo à revisão 0
**Documento:** memorial descritivo do adendo + lista de I/O, revisão 1
**O que se pede:** incluir o agitador `AG-05` no programa do CLP existente, pronto para comissionamento.

> Caderno fictício, escrito como entrada de um teste. Nada aqui corresponde a uma instalação real;
> nomes de equipamento e de área foram inventados.

---

## 1. Por que o adendo

O tanque de equalização a jusante do preliminar apresentou deposição de sólidos no fundo durante o
comissionamento. A solução aprovada foi instalar um **agitador submersível** no tanque, com operação
intermitente, e medir a corrente do motor para detectar arraste de trapo no impelidor — a falha que
derrubou o equipamento duas vezes na partida assistida.

O agitador entra **na área de processo que já existe**, no mesmo CLP e no mesmo painel. Nenhum
equipamento existente muda de função.

## 2. Equipamento novo

| TAG | Equipamento | Acionamento |
|---|---|---|
| `AG-05` | Agitador submersível do tanque de equalização, 4 kW | partida direta, contator com reversão **não** prevista |
| `IIT-05` | Transdutor de corrente do motor do agitador, 0–15 A | 4–20 mA, 2 fios |

O agitador não tem inversor: parte por contator, com relé de sobrecarga e chave seccionadora local.

## 3. I/O

Todos os pontos entram na periferia remota **já instalada** da Área 2. Não há cartão novo: usar
pontos livres dos cartões existentes.

| Sinal | Tipo | Descrição |
|---|---|---|
| Comando de partida do agitador | DO | aciona a bobina do contator `K5` |
| Contator ligado (retorno) | DI | contato auxiliar de `K5` |
| Relé de sobrecarga atuado | DI | contato NF, abre em sobrecarga |
| Chave seccionadora local em "desligado" | DI | contato NF |
| Corrente do motor | AI | `IIT-05`, 4–20 mA |

## 4. Operação

**Manual.** Pela IHM, com o seletor da área em Manual, o operador liga e desliga o agitador
diretamente. Todo intertravamento de proteção continua valendo em Manual.

**Automático.** Com o seletor da área em Automático, o agitador opera de forma intermitente:
roda **10 minutos a cada 2 horas**, contadas do fim do ciclo anterior. Os dois tempos são
parametrizáveis pela IHM sem recompilar o programa.

O agitador **não parte** se o nível do tanque estiver abaixo do mínimo de submergência — o sinal de
nível vem do instrumento de nível já existente da Área 2, e o valor de corte é parametrizável.

## 5. Proteções e alarmes

O agitador vai para **falha**, desliga e exige reconhecimento do operador quando:

- o relé de sobrecarga atuar;
- a chave seccionadora local for para "desligado" com o agitador rodando;
- o retorno do contator não confirmar a partida dentro de 3 segundos após o comando;
- a corrente do motor passar de 90 % da nominal por mais de 30 segundos (**arraste de trapo**);
- a corrente ficar abaixo de 20 % da nominal com o contator confirmado (**impelidor solto ou
  motor a vazio**).

Além das falhas, sinalizar sem desligar: **corrente alta** acima de 80 % da nominal.

## 6. Registros exigidos

- **Horímetro** do agitador, retentivo, legível na IHM e zerável por comando do operador.
- **Contador de partidas**, retentivo, com a mesma regra de zeragem.

## 7. Aceite

- Programa compila sem erro.
- Os intertravamentos do item 5 desligam o agitador e exigem reconhecimento.
- O ciclo do item 4 roda sozinho com o seletor em Automático.
- Os tempos, o corte de nível e os limites de corrente são alteráveis pela IHM sem recompilar.
- O programa segue o padrão da casa — quem for dar manutenção nele é a mesma equipe que mantém o
  resto da obra.

---

## Contexto de execução (não faz parte do caderno do cliente)

Projeto TIA de teste: `workspace/newlib/LIB_TESTE/LIB_TESTE.ap21`, CLP `PLC_ZERO`.
O CLP já tem a biblioteca de blocos da casa instalada e equipamentos das revisões anteriores.
