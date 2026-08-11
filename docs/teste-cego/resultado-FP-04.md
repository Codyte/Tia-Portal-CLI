# Resultado FP-04 — aeração `Sopradores/Aeração`, dois sopradores com inversor

Rodada cega de 2026-08-11 sobre [`caderno-FP-04.md`](caderno-FP-04.md), projeto `LIB_TESTE`,
CLP `PLC_ZERO`. Quem escreveu o caderno não executou; a sessão recebeu o caderno, a skill e o repo,
sem os cadernos e resultados das outras rodadas.

**Relógio: 15:05 → 15:50, 45 minutos, ~30 chamadas de verbo** (a maioria agrupada em `run --script`).

O diálogo modal de autorização do Openness que o handoff previa (hash do `tia.exe` mudou com o
Portal aberto) **não apareceu** — a primeira chamada, `tia info`, respondeu direto.

---

## 1. O que entrou no projeto

### Hardware

| Item | O que foi feito |
|---|---|
| Periferia remota da área | `ET200SP_QA-03` (IM 155-6 PN ST `6ES7 155-6AU01-0BN0/V4.2`), IP 192.168.0.13 |
| Cartão DI | `DI 8x24VDC AERACAO` (`6ES7 131-6BF01-0BA0/V0.0`) em `%IB30` — 4 pontos usados, **4 livres (50 %)** |
| Cartão AI | `AI 4xI 2FIOS AERACAO` (`6ES7 134-6GD01-0BA1/V2.0`) em `%IW32..%IW38` — 2 canais usados, **2 livres (50 %)** |
| Módulo servidor | `6ES7 193-6PA00-0AA0/V1.0` |
| Inversores | `INVERSOR_SOP-01_CCM_03` e `INVERSOR_SOP-02_CCM_03` (G120 `6SL3244-0BB12-1FA0/4.7.13`), IPs .11 e .12 |
| Telegrama | Standard telegram 20 nos dois (`insert-telegram --change`, o G120 nasce com o Main #1) |
| Rede | Os três juntados à `PN/IE_1` no IO system `PROFINET IO-System_PLC_ZERO` |

As constantes `INVERSOR_SOP-0N_CCM_03~PROFINET_interface~Standard_telegram_20` nasceram, que é a
prova de que o drive virou IO device do `PLC_ZERO` — é delas que o `SINA_SPEED_TLG20` tira o `HWID`.

### Programa

23 blocos novos, 6 UDTs, 6 tabelas de tag, tudo nascido na pasta certa (nenhum `move-block`):

```
2. Fluxo de Controle
   CHAMADA_AREA_03_SOPRADORES_AERACAO (OB, LAD, 6 redes)
4. Motores/Bombas/4.3 Inversores_CCM_03/4.3.3 Sopradores/Aeração
   FB CONTROLE DE AERACAO (FB, SCL) + iDB
   Soprador 1 (SOP-01)/  PARTIDA_SOPRADOR_1 (SOP-01) (FC, LAD) + 4 iDBs
   Soprador 2 (SOP-02)/  PARTIDA_SOPRADOR_2 (SOP-02) (FC, LAD) + 4 iDBs
5. Instrumentação/5.1 Aferição Analógica/5.1.3 Sopradores/Aeração
   ANALOGS_SOPRADORES_AERACAO (AIT-31) + 3 iDBs
   ANALOGS_SOPRADORES_AERACAO (PIT-31) + 3 iDBs
3. Alarmes/Eventos/Falhas/3.1 Alarmes Words/3.1.3 Sopradores/Aeração
   FC_ALARMES_SOPRADORES_AERACAO + DB_BITS_TO_WORD_SOPRADORES_AERACAO_W1
```

Os dois acionamentos e os dois instrumentos saíram de `clone` dos equipamentos equivalentes que já
existiam no projeto (`PARTIDA_MOTOR_1 (MOTOR_01)` e `ANALOGS_TANQUE_EQUALIZACAO (IIT-05)`), com
`--with-instances`. Autoral mesmo, só o `FB CONTROLE DE AERACAO` e as redes de chamada.

**DB global** ganhou **um** membro, `SOPRADORES_AERACAO : "AeracaoDados"`, agregando
`SOPRADORES` (2 × `MotorDados` + 2 × `SopradorRegistrosDados`), `INSTRUMENTACAO` (2 × `SensorDados`),
`CONTROLE` (`AeracaoControleDados`, 13 setpoints + status + alarmes) e `ALARMES`. Zero escalar solto.

**Divisão de trabalho.** O `FB CONTROLE DE AERACAO` fica com o que é da área — modo, rodízio com
sobreposição, rampa de velocidade pelo oxigênio, alarmes de área e as **causas** de falha, que ele
resume em `SOP-0N_STS_CONJUNTO_OK`. O `FB FALHA` da biblioteca fica com o engate e o reconhecimento,
como em qualquer acionamento da casa. Nada de reimplementar o que a biblioteca já faz.

---

## 2. Veredito do item 7 do caderno

| Aceite | Situação |
|---|---|
| Compila sem erro; hardware configurado, endereçado e na rede | ✅ `compile --plc PLC_ZERO` = **0 erros**, 1 warning. Hardware acima. |
| Intertravamentos desligam e exigem reconhecimento; alarmes de área só sinalizam | ✅ As cinco causas do item 5 chegam ao `FB FALHA` (sobretemperatura, seccionadora aberta com o soprador rodando, falha do inversor e pressão de segurança por 5 s entram por `CONJUNTO_OK`; velocidade não alcançada em 60 s é o `INPUT_TEMPO_NÃO_LIGOU` do próprio bloco). Os alarmes de área só escrevem bits na `WORD_ALARMES_1`. |
| Automático com rodízio e sobreposição | ✅ implementado. O que entra tem de atingir a velocidade mínima antes de o que sai perder o comando; o rodízio dispara por horas de operação acumulada **ou** por falha do que está em serviço. Não foi exercitado ao vivo — **D8**, o repo não coloca PLC online. |
| Setpoint, banda, velocidades, rampa e tempo de rodízio alteráveis pela IHM | ✅ 13 setpoints em `SOPRADORES_AERACAO.CONTROLE`, com default de partida. |
| Registros sobrevivem a desligamento do CLP | ✅ horímetro, contador de partidas e horas desde o rodízio são **statics retentivos** do FB (`set-retain` × 7), espelhados na DB global para a IHM. |
| Segue o padrão da casa | ⚠️ **`audit` 9/10** — ver abaixo. |

### O check que reprovou, e a justificativa que a R9 permite

```
blocos por acionamento (6 com inversor, trio na partida direta)
  .../Soprador 1 (SOP-01) → 5 blocos (com inversor: 6)
  .../Soprador 2 (SOP-02) → 5 blocos (com inversor: 6)
```

O 6º bloco do molde é o iDB de `FB SETPOINT MANUAL`. No próprio `MOTOR_01` deste projeto ele existe
na pasta **e não é chamado pelo FC** — a velocidade passa pelo `FB SETPOINT ESCALONAMENTO`, tanto em
Manual quanto em Automático. Instanciá-lo aqui só para o contador fechar seria código morto com nome
de padrão. A R9 prevê "ou justificativa escrita"; esta é ela.

Os outros nove checks passam, inclusive os quatro novos: `R1 · o PLC tem UDT`, `R2 · DB global sem
escalar solto na raiz`, `R8 · bloco de chamada em linguagem gráfica` e `CHAMADA_* fora da pasta de
área`.

---

## 3. Tropeços da ferramenta — defeito nosso

Ordenados por tempo perdido.

### T1 · `add-call` não chama FC (~25 min, a maior fatia da rodada)

`add-call` exige `--inst` e só monta chamada de FB. Mas o bloco `CHAMADA_*` do padrão da casa é
exatamente uma sequência de chamadas de **FC** — é a rede mais comum do projeto-molde, e a R8 existe
por causa dela. Cinco steps morreram com `Missing required option --inst.`

Contorno: montar cinco `SW.Blocks.CompileUnit` à mão em PowerShell e `import-block --apply`. O FlgNet
de uma chamada de FC são dez linhas (`<Call>` + `<CallInfo BlockType="FC" />` + um wire do powerrail
ao `en`), mas descobrir isso custou exportar o OB vizinho e ler o XML.

**Correção:** `--inst` opcional; sem ele, emitir `BlockType="FC"`. É o mesmo caminho de código,
menos o `<Instance>`.

### T2 · `clone --replace` não alcança caminho de membro de DB (~6 min + um undo de 26 steps)

Passei `--replace "CASA_DE_MOTORES.MOTORES_AREA_01.MOTOR_AREA_01_MOTOR_01=SOPRADORES_AERACAO.SOPRADOR_01"`
esperando reapontar a árvore da DB global. **Zero substituições**: no XML o caminho não é string
pontuada, é uma cadeia de `<Component Name="…" />`. Pior, o `--replace MOTOR_01=SOP-01` seguinte
pegou o componente do meio e produziu `MOTOR_AREA_01_SOP-01`, que não existe — 60 erros de
compilação, todos com a mesma cara.

Consequência de projeto: **a estrutura de destino tem que ter o mesmo número de níveis da origem.**
Tive de refazer os UDTs de dois níveis (`SOPRADORES_AERACAO.SOPRADOR_01`) para três
(`SOPRADORES_AERACAO.SOPRADORES.SOPRADOR_01`), apagar 22 blocos, o membro da DB e um UDT, e clonar
de novo. Acabou melhor — ficou igual ao molde — mas foi por acidente.

**Linha que teria evitado**, em `VERBS.md`, no `clone`: "`--replace` é substituição de texto no XML
exportado. Caminho de membro de DB é cadeia de `<Component>`: troque **um componente por vez**, e a
estrutura de destino precisa ter a mesma profundidade da origem."

### T3 · `add-call` não sabe emitir constante booleana (~8 min)

`--param INPUT_HABILITA_CONJUNTO=TRUE` (e `=true`) morre em:

```
'ConstantValue' has the invalid value 'TRUE' at the object with UID '30'.
```

O `add-call` emite `<Access Scope="TypedConstant"><Constant><ConstantValue>true</…` — sem
`<ConstantType>`. Funciona para `Time` (`T#5S` passou), não para `Bool`. O molde da casa escreve
`Scope="LiteralConstant"` + `<ConstantType>Bool</ConstantType>` + `TRUE`, e
`INPUT_HABILITA_CONJUNTO := TRUE` é pino do `FB FALHA` — o bloco mais chamado da biblioteca. Não é
caso exótico.

Contorno: `add-call` em dry com `--out`, `regex` no XML gerado, `import-block --apply`.

**Correção:** emitir `LiteralConstant` + `ConstantType` a partir do tipo do pino, que o `add-call` já
lê da interface do FB para tipar os `<Parameter>`.

### T4 · `compile --plc` diz 0 erros e o bloco continua inconsistente — e o verbo mente na falha

Duas vezes, `add-db-member` falhou com:

```
Error when calling method 'Export' … Inconsistent blocks and PLC data types (UDT) cannot be exported.
```

**logo depois** de um `compile --plc PLC_ZERO --apply` que devolveu `errors: 0`. E a chamada seguinte
do mesmo `add-db-member` devolveu `action: "exists", applied: false` — ou seja, **o patch tinha
entrado e o verbo reportou falha**. A falha é do *export de prova* do `Ops.ImportAndProve`, depois de
o import já ter acontecido. Quem lê o `ok:false` desfaz e refaz.

**Correção:** (a) separar "não apliquei" de "apliquei e não consegui provar" na mensagem; (b) o
`ExportFresh` compilar o alvo de verdade antes de exportar, ou o `compile --plc` limpar a
inconsistência do que acabou de ser importado.

### T5 · `list-blocks --folder` ignora o escape `\/` — e devolve lista vazia, não erro

```
--folder "5. Instrumentação/5.1 Aferição Analógica/5.1.3 Sopradores\/Aeração"   → count: 0
--folder "5. Instrumentação/5.1 Aferição Analógica" --count                     → 5.1.3 …/Aeração: 8
```

O `CLAUDE.md` diz que o `\/` "vale em qualquer verbo que receba caminho de pasta (a regra é do
`Ops.SplitPath`)". No `list-blocks` não vale, e o modo de falhar é o pior possível: zero resultados,
`ok: true`. Passei perto de concluir que os clones não tinham entrado.

**Correção:** aplicar `Ops.SplitPath` no filtro do `list-blocks`, ou corrigir a promessa no
`CLAUDE.md`.

### T6 · Nenhum verbo devolve o nome do IO system existente

O `CLAUDE.md` manda `connect-subnet --io-system NOME` "senão o drive entra no controlador errado
quando duas CPUs dividem a subnet" — e `LIB_TESTE` tem duas CPUs. Mas:

- `list-attrs --device PLC_ZERO --item "PROFINET interface_1"` → `count: 0`;
- o dry-run do `connect-subnet` ecoa o `--io-system` que eu passei, sem dizer se criaria ou reusaria
  (existe `subnetAction: reuse`; não existe o equivalente para IO system).

Achei o nome (`PROFINET IO-System_PLC_ZERO`) exportando o AML inteiro com `export-cax` e grepando
`IoSystem` — duas chamadas e um arquivo de ~1,5 MB para descobrir uma string.

**Correção:** `ioSystemAction: create|reuse` no dry-run do `connect-subnet` — uma linha de JSON que
apaga um `export-cax`.

### T7 · `list-io-map --device <drive>` devolve vazio

O `CLAUDE.md` afirma que o `list-io-map` "é onde se lê o endereço do telegrama de drive, que
`list-telegrams` não traz". Com os dois G120 novos, telegrama 20 posto e IO system conectado:

```
list-io-map --device INVERSOR_SOP-01_CCM_03  →  { "addresses": 0, "map": [] }
list-io-map (projeto inteiro)                →  { "addresses": 2, "unassigned": 9 }
```

Os 9 `unassigned` são os itens de drive. Ou o endereço só materializa depois de algo que não fiz, ou
o verbo não alcança — de qualquer forma, a linha do `CLAUDE.md` promete o que não entrega. Não
travou a rodada porque o `SINA_SPEED_TLG20` usa o `HWID`, não o endereço.

### T8 · `plug-module --type` derrama os `freeSlots` inteiros junto do `canPlug`

Sondar 9 MLFBs candidatos custou ~330 linhas de JSON, das quais 9 interessavam (`canPlug`). O
`--summary` do `run --script` esconde justamente o `canPlug`, então **não há saída enxuta possível**
para uma sonda de catálogo.

**Correção:** com `--type`, devolver só `{typeIdentifier, position, canPlug}`; os `freeSlots` já são
a resposta do modo sem `--type`.

### T9 · Não há como descobrir o sufixo de versão do MLFB de um módulo

`6ES7 131-6BF01-0BA0` → `canPlug: false`. Com `/V0.0` → true. O AI quer `/V2.0`, o módulo servidor
quer `/V1.0`. Não há regra: sondei nove combinações num batch. O GUI mostra o catálogo plugável no
slot; a CLI não.

**Correção:** `plug-module` sem `--type` (ou com `--like`) listar os tipos plugáveis naquele slot.

---

## 4. Tropeços do caderno — esperado, obra real também não diz

Decisões de engenharia que a sessão teve de tomar, registradas para quem for revisar:

- **Dimensionamento dos cartões.** O caderno pede "pelo menos 25 % de pontos livres". DI de 8 pontos
  (4 usados) e AI de 4 canais (2 usados) dão 50 % em ambos — o degrau seguinte para baixo não existe
  no catálogo ET200SP.
- **Painel/CCM da área.** O caderno não nomeia. Como o item 3 pede periferia remota própria da área,
  criei `QA-03` (tags de I/O) e `CCM_03` (inversores), seguindo a numeração de área 3.
- **Não há seletora local/remoto de campo por soprador**, e o molde da casa depende dela. Decidi que
  a **chave seccionadora local** é que estabelece o modo remoto: seccionadora fechada = remoto,
  aberta = local (e, com o soprador rodando, falha). O Manual/Automático do item 4 é da **área**, na
  IHM, não do equipamento.
- **"12 horas de operação acumulada"** foi lido literalmente: só conta com o soprador rodando
  (velocidade real ≥ mínima − tolerância), não hora de calendário.
- **Limites numéricos** não são dados. Entrei com defaults parametrizáveis: OD 2,0 mg/L, banda
  0,3 mg/L, velocidade 40–100 %, rampa 20 %/min, rodízio 12 h, bloqueio de partida 800 mbar,
  pressão de segurança 900 mbar, OD mín/máx 1,0/4,0 mg/L, tolerância de velocidade 10 %.
- **Os dois sopradores em falha**: o automático fica sem soprador em serviço e o alarme crítico sobe.
  O caderno pede o alarme e não diz o resto.
- **`AIT-31` fora de faixa** foi detectado no valor bruto do canal (`< -100` ou `> 27648`), não no
  alarme de "sem 4 mA" do bloco de aferição, porque o caderno fala de *faixa de corrente* (abaixo de
  4 mA **ou acima de 20 mA**) e o bloco da casa só sinaliza a ponta de baixo.

**Erro no briefing do próprio caderno.** O "Contexto de execução" afirma que o projeto **não** tem
periferia remota nem inversor configurados. Tem: um `ET200SP_QA` (sem cartões plugados) e dois G120
`INVERSOR_MOTOR_0{1,2}_CCM_01` já com telegrama 20. Não atrapalhou — a Área 3 ganhou os seus —, e na
verdade ajudou: os G120 existentes foram o gabarito do MLFB e da coreografia telegrama → subnet.

---

## 5. Vazamento da regra cega, registrado

Um `grep` por `6ES7 1[39]` em `docs/` bateu em `docs/teste-cego/resultado-2026-08-07.md`, que está na
lista de não-ler. Vi **duas linhas** no output do grep: uma tabela de MLFBs de cartões ET200SP
(DI 16×, DQ 16×, AI 8×I, módulo servidor) e uma frase sobre os sufixos de versão não serem uniformes.
Não abri o arquivo, não li mais nada dele.

Efeito possível: pode ter encurtado o T9. Contra-argumento: os cartões que usei (DI 8×, AI 4×I) são
**outros** — o dimensionamento veio do caderno —, e a sonda das nove combinações foi feita do mesmo
jeito, porque a linha vazada não dava o sufixo do DI 8×.

**Lição de método:** `grep` em `docs/` não respeita lista de não-ler. Numa rodada cega, a busca tem
de excluir `docs/teste-cego/` explicitamente.

---

## 6. Fila que sai desta rodada

Ordenado por (dor evitada ÷ tamanho do diff):

| # | Item | Onde |
|---|---|---|
| 1 | `add-call` com `--inst` opcional → chamada de FC | `BlockEdit.cs`. Destrava a R8 para o bloco `CHAMADA_*`, que é o caso mais comum do padrão. |
| 2 | `add-call` emitindo `LiteralConstant` + `ConstantType` pelo tipo do pino | `BlockEdit.cs`. O tipo já é lido da interface do FB. |
| 3 | `ioSystemAction: create\|reuse` no dry-run do `connect-subnet` | Apaga um `export-cax` de 1,5 MB por área nova. |
| 4 | `Ops.ImportAndProve` distinguindo "não apliquei" de "apliquei e não provei" | Hoje o verbo reporta falha depois de ter aplicado — o caminho de recuperação do agente é desfazer o que já está certo. |
| 5 | `Ops.SplitPath` no filtro do `list-blocks` (ou corrigir a promessa no `CLAUDE.md`) | Falso-negativo silencioso é o pior modo de falhar. |
| 6 | `plug-module --type` sem o dump de `freeSlots`; e listagem do catálogo plugável no slot | Resolve T8 e T9 juntos. |
| 7 | Documentar no `VERBS.md` que `--replace` é textual sobre o XML e que caminho de DB é cadeia de `<Component>` | Uma linha contra um undo de 26 steps. |
| 8 | Conferir a promessa do `list-io-map` sobre endereço de telegrama de drive | Ou o verbo passa a alcançar, ou a linha do `CLAUDE.md` sai. |
