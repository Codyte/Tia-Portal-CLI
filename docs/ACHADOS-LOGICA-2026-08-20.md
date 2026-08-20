# Achados de lógica — biblioteca `1. FB Bilbiotecas` (2026-08-20)

Leitura dos FBs da pasta 1, em **dois projetos**:

| | blocos | subpastas |
|---|---|---|
| `PROJETO-MOLDE_V21` (PLC `CPU1.0 CCO`) | 34 | 7 (1.1 Acionamento 7 · 1.2 Inversores 4 · 1.3 Instrumentação 6 · 1.4 Alarmes e Falhas 2 · 1.5 Diagnóstico 6 · 1.6 Comunicação Modbus 4 · 1.7 Utilitários 5) |
| `PROJETO-MOLDE_V21_1` (cópia de trabalho) | 33 | 5 (1.1 Acionamento 12 · 1.2 Inversores 3 · 1.3 Instrumentação 4 · 1.4 Alarmes e Diagnóstico 11 · 1.5 Comunicação 3) |

**A lógica é a mesma nos dois.** Comparação token a token dos XMLs exportados (parts, símbolos e
tokens por rede) dos 7 blocos exportados de ambos: **idênticos**. A única diferença de população é
`FB MODBUS SCAN DRIVERS V1`, que **já não existe no `_1`** — os outros 33 blocos são os mesmos, só
arranjados em 5 pastas em vez de 7. Salvo onde dito, todo achado abaixo vale nos dois projetos.

Método: `list-interface` por subpasta → `explain-block` nos blocos com lógica própria (27) →
`export-block` + `xref` nos casos que o `explain` não resolvia. No molde, 4 chamadas do CLI (~44 s);
a revisão contra o `_1`, mais 3 (~27 s).

**Ressalva de método:** `explain-block` **não renderiza caixa de matemática** (`Calc`, `Normalize`,
`Scale`, `Mul`). Saída que "não aparece escrita" no texto do `explain` **não é prova** de saída
morta — cada suspeita abaixo foi conferida no XML exportado, e duas caíram nessa conferência
(`FB SETPOINT ESCALONAMENTO` e `FB TOTALIZADOR` escrevem sim, por `Calc`/`Mul`).

**Segunda ressalva:** varrer `<Section Name="Input">` do XML atrás de parâmetro morto pega também as
seções das **instâncias de instrução** declaradas em `Static` (`RDREC`, `WRREC`, `MB_MASTER`). Foi o
que produziu 25 falsos "parâmetros mortos" em `FB MODBUS MASTER BLOCK` — `PORT`, `BUFFER`, `RECORD`,
`ID` e afins são pinos de instrução da Siemens, não da interface do bloco. Só conta o que está na
`<Interface>` de primeiro nível.

JSON crus e XMLs em `workspace/logica/` (gitignored).

---

## A1 · `FB INTERTRAVAMENTO_PAINEL`: `INPUT_TESTE` anula todos os intertravamentos — FECHADO, é comissionamento

> **Veredito do usuário (2026-08-20): é intencional — pino de comissionamento, para teste.** Fica
> como está. O registro abaixo permanece porque o comportamento precisa estar escrito em algum lugar:
> quem fiar esse pino a comando de IHM entrega um bypass de emergência à operação. O pino é de
> comissionamento e só isso.

Fonte SCL reconstruída do XML:

```pascal
IF ((#INPUT_BT_EMERGENCIA AND NOT #INPUT_DISJUNTOR AND #INPUT_FONTE1 AND #INPUT_FONTE2
     AND #INPUT_RELE_FALTA AND #INPUT_CONDICAO)
    OR #INPUT_TESTE) THEN
  #OUTPUT_PAINEL_OK := TRUE;
ELSE
  #OUTPUT_PAINEL_OK := FALSE;
END_IF;
```

`INPUT_TESTE` está em **OR com o conjunto inteiro**: um bit de teste em TRUE declara o painel OK com
botão de emergência atuado, disjuntor aberto, fonte caída e relé de falta atuado. O comentário do
bloco (`teste força a saída TRUE/FALSE`) admite a intenção — mas o que ficou no programa é um bypass
permanente, disponível a qualquer tag que escreva nesse pino.

**Saída:** nenhuma alteração no código. Pendência residual: conferir, nas chamadas de área, que
`INPUT_TESTE` não está fiado a tag escrita pela IHM — comissionamento é ligar o pino na mão, não
deixar um botão de tela ligá-lo.

## A2 · `FB MODOS DE OPERAÇÃO`: comentário contradiz o código — ALTO (documental)

Comentário do bloco: `Remoto Manual = 1, Remoto Automático = 2, Local Automático = 3, Local Manual = 4`.
Código:

```
IF INPUT_LM THEN OUTPUT_MODO := 0
IF INPUT_LA THEN OUTPUT_MODO := 1
IF INPUT_RA THEN OUTPUT_MODO := 2
IF INPUT_RM THEN OUTPUT_MODO := 3
```

Nenhum dos quatro códigos bate com o comentário, e o comentário sequer tem o valor 0. Como
`OUTPUT_MODO` chega à IHM (via `OUTPUT_MODO_OPER` do `FB CONDIÇÃO DE PARTIDA`), quem escrever tela
nova guiado pelo comentário mapeia errado os quatro modos.

Segundo ponto: as quatro atribuições estão em cascata na mesma rede, sem exclusão mútua — com mais
de uma entrada em TRUE, **a última vence** (prioridade implícita RM > RA > LA > LM). Pode ser
intencional; não está escrito em lugar nenhum.

**Saída:** corrigir o comentário para o que o código faz e declarar a prioridade. Trocar os valores
não é opção sem varrer a IHM antes.

## A3 · `FB CONDIÇÃO DE PARTIDA` NW2: ordem das atribuições atrasa a troca de modo em um scan — MÉDIO

A rede 2 calcula os modos **antes** de atualizar a variável de que eles dependem:

```
REMOTO_MAN          := REMOTO AND NOT CMD_MODO_OPERACAO
OUTPUT_REMOTO_AUTO  := REMOTO AND CMD_MODO_OPERACAO
...
REMOTO              := INPUT_REMOTO        <-- atualizado depois de ser usado
```

`REMOTO_MAN` e `OUTPUT_REMOTO_AUTO` usam o valor do **ciclo anterior** de `REMOTO`. O bloco gêmeo
`FB VALVULA` faz a mesma rede na ordem certa (`REMOTO := INPUT_REMOTO` primeiro). O efeito é um scan
de atraso na transição local↔remoto — invisível na operação normal, capaz de deixar um comando passar
na borda.

**Saída:** mover `REMOTO := INPUT_REMOTO` para o topo da rede, igualando ao `FB VALVULA`. Não há
verbo de reordenar linha de LAD: é edição na GUI ou clone de um molde já correto.

## A4 · `FB CONDIÇÃO DE PARTIDA`: saída `STS_INTERTRAVAMENTO_AUTOMATICO` nunca escrita — MÉDIO

Varredura do XML exportado: o membro aparece só na declaração da interface, em rede nenhuma. É o
**único** parâmetro morto dos 8 blocos exportados. Quem lê essa saída na área lê zero permanente.

**Saída:** implementar (a intenção do nome é publicar o intertravamento que segura a partida em
automático — hoje isso vive só dentro de `LIGA_BOMBA`) ou apagar o pino. Apagar mexe na assinatura e
obriga a rever todas as chamadas de área.

## A5 · Cinco blocos sem nenhum chamador — MÉDIO

`xref` com `UsedBy` vazio:

| Bloco | Situação | molde | `_1` |
|---|---|---|---|
| `FB MODBUS SCAN DRIVERS V1` | superado pelo V2, chamado por `MODBUS_QA-01/02/03_PTP1` | morto | **já removido** |
| `PROFINET_DEVICE_STATES` | duplicado de `FB PROFINET DEVICE STATES to Word` (2 words × 4 words) | morto | morto |
| `FB INVERSOR SIEMENS` | escala RPM→telegrama por `Normalize`; o projeto usa `SINA_SPEED_TLG20` | morto | morto |
| `AUX_PID` | seleção de ganho/tempo por faixa; nenhum PID do projeto o chama | morto | morto |
| `FB SETPOINT MANUAL` | clamp de setpoint manual entre `RANGE_MIN/MAX` | não medido | morto |

Não é erro de lógica — é peso morto na biblioteca. E `V1`/`V2` convivendo com nome versionado
convida a chamar a versão errada numa área nova.

`FB SETPOINT MANUAL` merece um olhar antes de qualquer decisão: ele é o gêmeo do
`FB SETPOINT ESCALONAMENTO`, que tem **41 chamadores**. Bloco de setpoint manual sem nenhum chamador
num projeto que opera em manual é mais provável de ser função que ficou pelo caminho do que de ser
reserva deliberada.

**Saída:** decisão do usuário. Se são reserva de projeto, ficam; se não, `--force` exporta antes de
apagar (`workspace/recovery/`).

## A6 · `AFERIÇÃO INSTRUMENTOS` × `AFERIÇÃO INVERSORES`: gêmeos que divergem no rompimento de fio — MÉDIO

Mesma equação da reta, mesmos pinos de configuração (`X1/X2/Y1/Y2_CONFIG`), o de inversores
acrescentando `GRANDEZA_MAX`. Divergem em três pontos que não parecem deliberados:

| | INSTRUMENTOS | INVERSORES |
|---|---|---|
| zera a saída quando | `ANALOG_IN_4a20mA < RANGE_MIN_SEM_4mA` | `ANALOG_IN_4a20mA < 0` |
| clamp de saída negativa | `IF ANALOG_OUT < 0.0 THEN 0.0` (N2) | ausente |
| tipo de `RANGE_MIN/MAX_CADASTRADO` | `UInt` | `Int` |

Numa entrada 4-20 mA escalada 0..27648, o valor de fio rompido é **positivo e baixo** (abaixo do
ponto de 4 mA), não negativo: a condição `< 0` do bloco de inversores praticamente nunca dispara, e
a saída mantém o último valor válido com o fio rompido. O alarme `ALARME_SEM_4mA` (TOF) continua
funcionando nos dois — o que diverge é o valor entregue ao programa.

**Saída:** alinhar o bloco de inversores ao de instrumentos (comparar contra `RANGE_MIN_SEM_4mA`), ou
fundir os dois num só bloco com `GRANDEZA_MAX` opcional. A LGF (SIOS 109479728) tem escala pronta —
ler `docs/GUIA-SIEMENS.md` antes de reescrever.

## A7 · `FB FILTRO DE AMOSTRAGEM  ANALÍTICA`: comentário diz 4 amostras, código guarda 8 — BAIXO

Comentário: *"o sistema irá realizar 4 amostra no periodo de 2s"*. Código: `AUX_1..AUX_8` gravados em
`INDEX_AMOSTRAGEM` = 10, 20, … 80, com o índice zerando em 100 (`AUX_10` é o acumulador do `Add`).
São 8 amostras num ciclo de 100 incrementos.

É também o candidato mais direto a virar biblioteca oficial: média móvel é bloco pronto na LGF, e
manter 8 redes de `Move` indexado é dívida sem ganho.

## A8 · R3 (≤8 parâmetros escalares por FB) violado em massa — BAIXO, estrutural

| Bloco | Escalares |
|---|---|
| `FB MODBUS MASTER BLOCK` | 25 in + 5 out + 6 inout |
| `FB ALARME DIGITAL` | 17 in + 16 out |
| `FB BITS TO DOUBLE WORD` | 32 in |
| `FB CONDIÇÃO DE PARTIDA` | 12 in + 7 out |
| `FB VALVULA` | 12 in + 6 out |
| `FB LIMITES_OPERACAO_SENSOR` | 9 in + 8 out |

`FB ALARME DIGITAL` e os `FB BITS TO *` são o mesmo caso: 16/32 pinos Bool que deveriam ser **um**
`Word`/`DWord` ou um UDT. `SINA_SPEED_TLG20` é bloco da Siemens e fica fora do julgamento.

**Saída:** não é correção de sessão — é agenda de refactor por UDT, e mexer na assinatura obriga a
rever todas as chamadas. Registrado para não ser redescoberto.

## A9 · Redes vazias e redes sem bobina — BAIXO

`FB SETPOINT ESCALONAMENTO` N2 · `FB INVERSOR SIEMENS` N2 · `FB STATUS ECSX` N2 ·
`FB TOTALIZADOR` N4 e N5 · `FB MODBUS MASTER BLOCK` N5.

Rede vazia **não sobrevive ao export/import**: qualquer bloco que passe por `clone`, `add-call` ou
`delete-network` volta com uma rede a menos, e `--index` planejado de cabeça apaga a rede errada. É o
tropeço já registrado no `CLAUDE.md`; a lista acima diz onde ele mora.

## A10 · `FB VALVULA`: `FECHA_VALVULA` sem intertravamento — BAIXO

```
FECHA_VALVULA := REMOTO AND NOT LOCAL_MAN AND NOT ABRE_VALVULA
```

Como `ABRE_VALVULA` já carrega `NOT FALHA` e as condições de manutenção, o comando de **fechar** sai
sempre que a válvula está em remoto e não está abrindo — inclusive em falha, em manutenção e com
PROFINET caído. Para válvula normalmente-fechada isso é o fail-safe desejado; para válvula que deva
**congelar** na posição em falha, é comando indevido.

A rede 1 do mesmo bloco também difere da irmã `FB CONDIÇÃO DE PARTIDA`: a primeira linha
(`STATUS_VALV := 0`) não tem a guarda `NOT INPUT_FALHA_PROFINET` que as demais têm. A ordem das
atribuições salva o resultado, mas a assimetria é gratuita.

**Saída:** confirmar com o usuário qual é o comportamento desejado em falha. É decisão de processo,
não defeito provado.

## A11 · `FB VALVULA`: `PARAMETRO_ABRE` e `PARAMETRO_FECHA` nunca lidos — MÉDIO

Os dois pinos de entrada não aparecem em rede nenhuma (zero ocorrências no XML fora da declaração).
O comentário da rede 4 explica o que eles deveriam fazer — *"se PARAMETRO < PARAMETRO_LIGA, então
LIGA_REM_AUTO = True (se a leitura for menor que o cadastro para ligar, liga em remoto automático)"* —
mas o automático da válvula entra só por `INPUT_LIGA_AUTO`, vindo de fora. A abertura por comparação
de parâmetro está documentada no bloco e não existe no código.

Medido no `_1`; o `FB VALVULA` do molde não foi exportado nesta rodada, e como todos os blocos
comparados saíram idênticos, o esperado é que valha lá também — **conferir antes de agir**.

**Saída:** implementar a comparação (é onde a lógica de nível/pressão abrir válvula sozinha
deveria morar) ou apagar os dois pinos e a promessa do comentário. Apagar mexe na assinatura e
obriga a rever as chamadas de área.

---

## O que foi conferido e está certo

- `FB SETPOINT ESCALONAMENTO` e `FB TOTALIZADOR` **escrevem** suas saídas (por `Calc` e `Mul`,
  invisíveis ao `explain-block`). A suspeita de saída morta caiu nos dois.
- `FB_LIGA/DESLIGA MODO AUTO`, `FB_PARTIDA_INVERSOR`, `FB FALHA`, `FB CONTADOR`, `FB_HORÍMETRO`,
  `FB SUCÇÃO OK`, `FB STATUS ECSX`, `FB MODBUS MASTER BLOCK`, `FB MODBUS MASTER BLOCK MMW` e
  `FB MODBUS SCAN DRIVERS V2` fazem o que o nome promete.
- Parâmetros mortos: só os do A4 (`STS_INTERTRAVAMENTO_AUTOMATICO`) e do A11 (`PARAMETRO_ABRE`,
  `PARAMETRO_FECHA`) nos 19 blocos exportados dos dois projetos. Os 25 "mortos" de
  `FB MODBUS MASTER BLOCK` são pinos de instrução da Siemens — ver a segunda ressalva de método.
- Os XMLs de molde e `_1` batem token a token nos 7 blocos comparáveis: reorganizar a biblioteca em
  pastas não mexeu em lógica nenhuma.
