# Teste cego FP-03 — agitador `AG-05` do tanque de equalização

Execução do caderno [`caderno-FP-03.md`](caderno-FP-03.md) sem consultar as sessões anteriores
(os cadernos FP-01/FP-02, os resultados datados e o `DIARIO.md` ficaram fechados até o fim da
execução). Projeto: `workspace/newlib/LIB_TESTE/LIB_TESTE.ap21`, CLP `PLC_ZERO`.

**Resultado do programa:** compila **0 erros / 1 aviso** (o aviso é `Inputs or outputs are used
that do not exist in the configured hardware` — o projeto de teste não tem cartão de I/O; o caderno
diz que a periferia da Área 2 já existe). `tia audit` fecha **5 de 6 checks**; o único que reprova
é "6 blocos por acionamento", com justificativa escrita no §4.

**Resultado do teste** (que é o que interessa): o programa saiu conforme, mas **duas das nove
regras da lei de construção só foram alcançadas escrevendo XML de FlgNet na mão**, e um verbo
devolveu `ok: true` sem ter mudado nada. Os dez tropeços estão no §5, com a proposta de verbo
para cada um.

---

## 1. O que foi entregue

### Blocos (10 novos, 78 no total)

| Bloco | Tipo | Pasta |
|---|---|---|
| `FB SUPERVISAO AGITADOR` | FB (SCL) | `1. FB Bibliotecas/1.1 Acionamento` |
| `FB FALHA_AG-05` | iDB | `4. Motores/Bombas/4.2 Partidas Diretas_CCM_01/4.2.2 Tanque de Equalizacao/Agitador 5 (AG-05)` |
| `FB CONDIÇÃO DE PARTIDA_AG-05` | iDB | idem |
| `FB SUPERVISAO AGITADOR_AG-05` | iDB | idem |
| `PARTIDA_AGITADOR_5 (AG-05)` | FC (LAD) | idem |
| `FB FILTRO DE AMOSTRAGEM  ANALITICA_IIT-05` | iDB | `5. Instrumentação/5.1 Aferição Analógica/5.1.2 Tanque de Equalizacao` |
| `FB AFERIÇÃO INSTRUMENTOS_IIT-05` | iDB | idem |
| `FB LIMITES OPERACAO SENSOR_IIT-05` | iDB | idem |
| `ANALOGS_TANQUE_EQUALIZACAO (IIT-05)` | FC (LAD) | idem |
| `CHAMADA_AREA_02_TANQUE_EQUALIZACAO` | OB (LAD, ProgramCycle) | `2. Fluxo de Controle` |

### UDT e DB global

`AgitadorDados` (novo, 31 membros) — o que o `MotorDados` da casa não tem: corrente nominal,
percentuais de alarme/falha, tempos de filtro, cadastro do ciclo intermitente, corte de
submergência, contador de partidas e horímetro. Todos os cadastros nascem com valor inicial
(8,5 A / 80 % / 90 % / 20 % / 30 s / 5 s / 10 min / 120 min), então o programa chega comissionável.

Ramo novo na `DB GLOBAL`, no formato das outras áreas:

```
TANQUE_EQUALIZACAO
  AGITADORES_AREA_02
    AGITADOR_AREA_02_AG_05            : "MotorDados"
    AGITADOR_AREA_02_AG_05_SUPERVISAO : "AgitadorDados"
  INSTRUMENTACAO
    INSTR_05_TRANSDUTOR_DE_CORRENTE_AG_05 : "SensorDados"
```

### Tabelas de tags (5, 26 tags)

`1. I/OS/QA-01` → `ENTRADAS_DIGITAIS (QA-01)` (3), `SAIDAS_DIGITAIS (QA-01)` (1),
`ENTRADAS_ANALOG (QA-01)` (1) · `3. Partidas/3.2 Tanque de Equalizacao` → `AGITADOR (AG-05)` (16)
· `2. Alarmes/2.2 Tanque de Equalizacao` → `TRANSDUTOR_DE_CORRENTE (IIT-05)` (5).

I/O: `%I20.0` retorno de `K5`, `%I20.1` relé de sobrecarga (NF), `%I20.2` seccionadora local (NF),
`%Q20.0` bobina de `K5`, `%IW100` transdutor `IIT-05`. Espelhos em `%M5600`–`%M5611`
(o bloco de 20 bytes por acionamento da casa; o último ocupado era `%M5576`).

## 2. Como cada item do caderno foi atendido

| Caderno | Onde |
|---|---|
| §4 Manual pela IHM | `FB CONDIÇÃO DE PARTIDA` em remoto manual (`CMD_LIGA` da DB global) |
| §4 Automático 10 min / 2 h do fim do ciclo | `FB_LIGA/DESLIGA MODO AUTO` (biblioteca) como multi-instância dentro do `FB SUPERVISAO AGITADOR`; `CAD_TEMPO_LIGADA/DESLIGADA` em **minutos**, lidos de `AgitadorDados` |
| §4 tempos parametrizáveis | `CMD_TEMPO_LIGADO_AUTO_MIN` / `CMD_TEMPO_DESLIGADO_AUTO_MIN` na DB global |
| §4 não parte sem submergência | `STS_PERMISSIVO_DE_PARTIDA` em série com a bobina de `K5` (N10 do FC) — vale em manual e em automático — e também no `STS_HABILITA_LIGA_AUTO` |
| §5 sobrecarga / seccionadora com motor rodando / arraste / corrente baixa | causas calculadas no `FB SUPERVISAO AGITADOR`, agregadas em `STS_CONDICOES_OK`, que entra no `FB FALHA` (latch + reconhecimento pelo botão do QA ou pela IHM) |
| §5 contator não confirma em 3 s | `INPUT_TEMPO_NÃO_LIGOU := T#3S` do `FB FALHA`, comparando `%Q20.0` com `%I20.0` |
| §5 corrente alta sinaliza sem desligar | `STS_ALARME_CORRENTE_ALTA` (80 % da nominal), fora do agregado de falha |
| §6 horímetro retentivo e zerável | `HORIMETRO_EM_SEGUNDOS` (estática **Retain**) no `FB SUPERVISAO AGITADOR`, zerado por `CMD_RESET_HORIMETRO`; publicado em `STS_HORIMETRO_EM_HORAS` |
| §6 contador de partidas retentivo | `CONTAGEM_DE_PARTIDAS` (estática **Retain**), zerado por `CMD_RESET_CONTADOR_DE_PARTIDAS` |
| §7 padrão da casa | 8 dos 10 blocos são instância ou clone da biblioteca; o FC de partida é o molde `PARTIDA_MOTOR_1` com as redes de inversor trocadas |

## 3. Premissas (o caderno não dizia, e a decisão foi minha)

1. **Sem seletora local/remoto de campo.** O caderno só lista 3 DIs e o modo vem da IHM, então a
   rede de entradas força `STS_MODO_REMOTO := AlwaysTRUE` e `STS_MODO_LOCAL := AlwaysFALSE`. Se o
   painel tiver seletora, é trocar dois operandos na N1.
2. **Nível do tanque.** O caderno manda usar "o instrumento de nível já existente da Área 2"; no
   projeto de teste esse instrumento não existe. `NIVEL_DO_TANQUE` está ligado em
   `DB GLOBAL.PRELIMINAR.INSTRUMENTACAO.INSTR_01_….STS_STATUS_SENSOR` como marcador — **trocar
   pelo LIT real da área antes de comissionar**. A unidade do corte (`CMD_NIVEL_MINIMO_SUBMERGENCIA
   := 0.5`) segue a unidade daquele instrumento.
3. **Endereços de I/O.** A periferia da Área 2 está vazia no projeto de teste (o ET200SP só tem
   interface e BA 2xRJ45), então os 5 pontos foram endereçados em faixa livre (`%I20`, `%Q20`,
   `%IW100`) sem plugar cartão — é o que gera o único aviso do compile. Com a periferia real, é
   corrigir o endereço das 5 tags.
4. **Corrente nominal 8,5 A** estimada para 4 kW em 380 V trifásico, coerente com o transdutor de
   0–15 A. É cadastro de IHM, não constante de programa.
5. **`4.2 Partidas Diretas_CCM_01`.** O molde só documenta `4.N Inversores_CCM<N>`; partida direta
   não tinha lugar. Entrou como `4.2` no mesmo CCM, com o nível de área `4.2.2` casando com o
   `N = 2` das outras hierarquias (`2.2`, `3.2`, `5.1.2`).
6. **Sem cadeia de alarme em word.** `FC_ALARMES_<AREA>` + `DB_BITS_TO_WORD_…` não entraram: o
   aceite do caderno não pede e a área nasceu com um único equipamento. O ramo
   `TANQUE_EQUALIZACAO.ALARMES` foi deixado de fora da DB global por isso.

## 4. O check que reprova, e por quê

`audit` → `6 blocos por acionamento`: a pasta do AG-05 tem **4** (3 iDB + FC). Um acionamento com
inversor tem 6 porque carrega `SINA_SPEED_TLG20_<TAG>`, `FB SETPOINT MANUAL <TAG>` e
`FB SETPOINT ESCALONAMENTO <TAG>`. Partida direta por contator não tem telegrama nem referência de
velocidade; em troca ganha `FB SUPERVISAO AGITADOR_<TAG>`. É a justificativa escrita que a R9
prevê — e é também um caso para a régua enxergar (§5, tropeço 9).

## 5. O que a CLI atrapalhou (resultado do teste)

Ordenado por custo. "Chamadas" conta invocações do `tia`, ~10–20 s cada.

1. **Reconstituir o padrão da casa custou 12 chamadas de leitura antes da primeira escrita.**
   Não existe verbo que responda "como esta casa monta um acionamento": foi `explain-block` do
   `PARTIDA_MOTOR_1`, `export-block` de 8 FBs de biblioteca só para ler a interface, `export-type`
   de 2 UDTs e um dump da `DB GLOBAL`. A interface do FB só sai por XML + grep local.
   **Proposta:** `list-interface --folder "1. FB Bibliotecas" [--name X]` devolvendo
   `bloco → Input/Output/InOut` em uma chamada; e o `tree` carregando a assinatura dos FBs de
   biblioteca (é o que se lê antes de escrever qualquer chamada).

2. **R8 (chamada em LAD) não tem caminho pela CLI.** `import-ladder` não converte `CALL`, então
   montar o FC de partida foi: exportar o molde, apagar 2 `CompileUnit`, reescrever 3 `Access`,
   remover a negação de um contato, inserir 1 contato em série (3 wires) e **escrever uma
   `CompileUnit` inteira na mão** (1 `Call`, 9 `Access`, 11 `Wires`) — 276 linhas de Python
   (`workspace/ag05/make_fc.py`, não versionado). Funcionou de primeira e o resultado é LAD legítimo, mas isso é
   trabalho de verbo, não de sessão.
   **Proposta:** `add-call --block X [--after N] --fb "FB Y" --inst "iDB" --param p=<tag|DB.path|const>…`
   (monta a rede inteira, resolve tipo pela interface do FB) e `delete-network --block X --index N`.
   Com esses dois, este FC sairia em 4 chamadas sem uma linha de XML.

3. **Retentividade não é alcançável onde ela mora.** `Remanence` **não pode ser setado em iDB**
   (`The attribute 'Remanence' cannot be set`) — só na declaração do FB. Como o horímetro da casa
   está dentro de `FB CONDIÇÃO DE PARTIDA` → `FB_HORÍMETRO` (biblioteca), a única forma de entregar
   "horímetro retentivo" sem alterar a biblioteca foi **reimplementar o horímetro** no meu FB. O
   projeto agora tem dois horímetros para o AG-05: o volátil da biblioteca e o retentivo meu.
   **Proposta:** `set-retain --block <FB> --member M [--apply]`, recusando iDB com a mensagem certa.
   E vale decidir em separado se o `FB_HORÍMETRO` da casa deveria nascer retentivo.

4. **`import-source` não expressa retentividade.** SCL não tem como marcar `Retain`, então o ciclo
   de um atributo foi `import-source` → `export-block` → patch no XML → `import-block` → `compile`.
   4 chamadas para dois checkboxes.

5. **Construir um ramo novo na DB global custou 8 chamadas e 4 compiles.**
   `add-db-member --type Struct` é (corretamente) recusado porque struct vazia deixa a DB
   inconsistente, e a alternativa `--like` **só vê irmãos do mesmo caminho** — para criar
   `TANQUE_EQUALIZACAO.AGITADORES_AREA_02.<UDT>` foi preciso clonar `PRELIMINAR`, renomear o membro
   de dentro, clonar o `INSTRUMENTACAO` resultante, renomear e trocar o tipo. Duas edições na mesma
   DB no mesmo `run --script` falham com `Inconsistent blocks and PLC data types (UDT) cannot be
   exported` — cada `add`/`edit` exige `compile --apply` no meio.
   **Proposta:** `add-db-member` compilando sozinho quando o alvo está inconsistente (ou
   `--from-scl F.scl`, importando um `STRUCT` inteiro num caminho: um verbo, um compile).

6. **`edit-db-member --rename` devolveu `ok: true` sem efeito.** No batch, o passo seguinte ainda
   viu o nome antigo (`Known members: INSTR_01_MEDIDOR_DE_VAZAO_ULTRASSONICO`). Isso é pior que um
   erro — sai `ok` e o projeto não mudou.
   **Mecanismo** (revisto depois da execução, com os `step2*.json` na mão): a DB **não** estava
   inconsistente — o export funcionou. Estava *modificada-não-compilada*, porque o passo anterior
   (`add-db-member --like`, um `Import Override`) não tinha compilado. Nesse estado o export
   seguinte devolve o conteúdo pré-import, e o patch é calculado em cima de um XML velho.
   **Proposta:** não é "recusar bloco inconsistente" — esse guard não dispararia aqui. É a
   coreografia `export → patch → Import Override`, comum a `add`/`edit`/`delete-db-member`,
   **conferir o resultado depois de importar** (re-exportar e verificar o patch) e compilar sozinha
   quando o alvo está modificado-não-compilado.

7. **`clone --replace` foi a melhor ferramenta da sessão** e resolveu o instrumento inteiro em
   1 chamada (46 substituições, incluindo caminho de DB, nomes de iDB e tags). O que faltou: o
   clone **não cria os iDBs** que o XML clonado passa a referenciar, e o nome deles tem que ser
   deduzido aplicando os `--replace` na cabeça (`FB FILTRO DE AMOSTRAGEM  ANALITICA_IIT-05`, com
   espaço duplo). **Proposta:** `clone --with-instances` criando os iDBs faltantes na mesma pasta.

8. **`create-instance-db` cobra o nome exato do FB, com acento e espaço duplo.** Três nomes
   (`FB FILTRO DE AMOSTRAGEM  ANALÍTICA`, `FB AFERIÇÃO INSTRUMENTOS`, `FB LIMITES_OPERACAO_SENSOR`)
   e a instância da casa usa grafia diferente da do FB (`ANALITICA` sem acento,
   `LIMITES OPERACAO SENSOR` sem underscore). Errar um caractere = uma chamada perdida.

9. **`audit` assume acionamento com inversor.** Partida direta reprova em "6 blocos" (§4).
   **Proposta:** contar 6 só quando houver telegrama/inversor na pasta; senão exigir
   `FC PARTIDA_* + FB FALHA_<TAG> + FB CONDIÇÃO DE PARTIDA_<TAG>` e aceitar o resto.

10. **O que *não* atrapalhou, e vale registrar:** `--folder` com `/` no nome (`1. I/OS/QA-01`,
    `4. Motores/Bombas/4.2 …`) funcionou nos imports — o resolver casa o prefixo existente, então a
    divergência F1/F2 do `BOAS-PRATICAS §F` está restrita ao `create-folder`. `run --script` com
    `--summary` e `--out-file` manteve a sessão inteira dentro do orçamento de contexto.
    `explain-block --file` (offline, sem TIA) foi o que permitiu entender 5 FBs de biblioteca sem
    gastar chamada.

## 6. Fila de correção que este teste sugere

Ordem por (dor evitada ÷ tamanho do diff), para entrar na fila do `BOAS-PRATICAS §3`:

1. `add-call` + `delete-network` — destrava a R8 de verdade (tropeço 2).
2. **Um guard só** na coreografia `export → patch → Import Override` de `add`/`edit`/`delete-db-member`:
   compilar quando o alvo está modificado-não-compilado e conferir o patch depois de importar
   (tropeços 5 e 6 — o 6 é bug, não falta de recurso).
3. `set-retain --block --member` (tropeços 3 e 4).
4. `list-interface` (tropeço 1).
5. `clone --with-instances` (tropeço 7).
6. `audit` reconhecendo acionamento sem inversor (tropeço 9).
