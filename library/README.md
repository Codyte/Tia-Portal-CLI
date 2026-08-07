# `library/` — biblioteca de blocos instalável ("arsenal")

Árvore de pastas da lei de nomenclatura + os moldes que os geradores (`gen-alarm-fc`,
`gen-fault-ob`, `gen-instrument-fc`, `gen-startup-ob`) exigem, num manifesto único que
`scaffold` aplica num projeto vazio.

| arquivo | vai pro Git? | o que é |
|---|---|---|
| `library.json` | **sim** | manifesto `ScaffoldManifest`: 27 pastas de bloco, 6 de tag, 65 itens |
| `export-all.json` | **sim** | batch inverso: exporta os 65 do projeto de referência de volta pra `blocks/` |
| `README.md` | **sim** | este arquivo |
| `blocks/*.xml` | **não** (`.gitignore`) | payload: os XMLs exportados (66 arquivos; 65 no manifesto) |

## Por que o payload não viaja no repo

Os XMLs de `blocks/` saíram de um projeto real de cliente — nomes de equipamento, tags,
estrutura de DB (`DB GLOBAL.xml` sozinho tem 869 KB da planta). Este repo é **público**
(`github.com/Codyte/Tia-Portal-CLI`), e publicar isso é irreversível na prática (fork, cache,
índice de busca). Regra do PLANO (F4): *nenhum payload de projeto de cliente entra no repo
público; o que for publicado tem que ser autoral ou sanitizado*.

Consequência aceita: num clone, `blocks/` chega vazio e `scaffold` falha com
`Scaffold item not found: ...` — o manifesto é a receita, o payload é local.

## Como repor o payload

Com o projeto de referência aberto no Portal (`Software de ETE Insular_Inicial_V21`), da raiz
do repo:

```powershell
pwsh scripts/prep-project.ps1 "Software de ETE Insular_Inicial_V21" -Apply   # compila antes: bloco inconsistente não exporta
pwsh scripts/tia.ps1 run --script library/export-all.json                    # 65 exports, 1 attach
```

`export-all.json` é gerado a partir do `library.json` — mesmos 65 objetos, verbo por tipo
(`export-type` p/ UDT, `export-tags` p/ tabela, `export-block` p/ o resto) e `--out library/blocks`
relativo à raiz do repo. Para um item solto, o nome está no batch.

Dois detalhes que mordem:
- **Compile antes.** `Ops.ExportBlock` recusa bloco inconsistente com
  `Block 'X' is inconsistent (imported or edited, never compiled)` — é o Openness, não a CLI.
- **Nome ≠ arquivo.** `ExportPath` troca caractere inválido de arquivo por `_`
  ([Ops.cs:245](../src/Tia.Core/Ops.cs#L245)): o bloco `FB_LIGA/DESLIGA MODO AUTO` vira
  `FB_LIGA_DESLIGA MODO AUTO.xml`. O manifesto usa o nome do **arquivo**, o batch usa o nome do
  **objeto** — por isso os dois não são idênticos nesse item.
- **Hash muda todo export.** Cada XML carrega `<DocumentInfo><Created>` com o timestamp da
  exportação, então os arquivos mudam de hash a cada rodada mesmo sem mudança no projeto.
  Para comparar duas exportações, ignore essa linha (`(Get-Content f) -notmatch '<Created>'`).

## Como instalar num projeto

```powershell
pwsh scripts/tia.ps1 scaffold --manifest library/library.json            # dry: lista o que criaria
pwsh scripts/tia.ps1 scaffold --manifest library/library.json --apply
pwsh scripts/tia.ps1 compile --apply
```

`Source` é relativo ao próprio manifesto, então `library.json` + `blocks/` podem ser copiados
juntos pra qualquer lugar. Idempotente: item que já existe sai `skip (exists)` (`--force`
sobrescreve). A ordem de import é por tipo — UDT → tabela de tag → FB → DB → iDB → FC → OB
([`Scaffold.Rank`](../src/Tia.Core/Scaffold.cs#L58)) — porque bloco só importa limpo depois do
que ele referencia.

`Folder` de item UDT **é respeitado desde 2026-07-28**
([`ResolveTypePath`](../src/Tia.Core/Scaffold.cs#L188)) — antes todo `SW.Types.*` caía na raiz do
`TypeGroup`. Os 13 UDTs do manifesto continuam `"Folder": []`, mas agora dá pra agrupá-los.

## O que cada gerador exige daqui

`doctor` só fica verde se o projeto tiver estes nomes (são os defaults dos configs; todos
sobrescrevíveis no JSON do verbo):

| verbo | item exigido | de onde vem o default |
|---|---|---|
| `gen-fault-ob` | OB `MODULE_ERROR_MOLDE` | [FaultOb.cs:19](../src/Tia.Core/FaultOb.cs#L19) |
| `gen-alarm-fc` | FC `FC_Modelo` (pasta `3.1.0 Modelo`), OB `OB_MOLDE_ALARMES` (pasta `3.1 Alarmes Words`), FB `FB BITS TO WORD`, `DB GLOBAL`, pastas de tag `2. Alarmes` e `3. Partidas` | [AlarmFc.cs:19-27](../src/Tia.Core/AlarmFc.cs#L19-L27) |
| `replicate-instruments` | `DB GLOBAL`, molde `MOLDE_ANALOGS` | [InstrumentFc.cs:22](../src/Tia.Core/InstrumentFc.cs#L22) |
| `replicate-fc` | `DB GLOBAL`, UDTs por tipo de equipamento (`MotorDados`, `ValvDados`, …), FC modelo na pasta de origem | [Replicate.cs:25](../src/Tia.Core/Replicate.cs#L25) |

## Inventário — 65 itens

Por tipo: 33 FB · 13 UDT (`SW.Types.PlcStruct`) · 8 instance DB · 4 FC · 3 global DB · 3 OB ·
2 tabela de tag. Import roda nessa ordem de dependência, não na ordem do manifesto.

### raiz do PLC — 13 UDTs
`Processo Batelada` · `Conjunto CMD` · `Aferição CMD` · `ValvDados` · `SensorDados` ·
`Diag_Hardware` · `Tlg_20_Out` · `Tlg_20_In` · `HACH_DataType` · `MotorPrincipal` · `MotorDados` ·
`SULZER_Compressor_Comando` · `SULZER_Compressor_Status`

### `1. FB Bibliotecas` — 33 FBs em 7 subpastas por função
Reorganizada em 2026-07-28 (era plana). O Openness não move bloco: é `export` → `delete` →
`import --folder`, nessa ordem (importar com o original no lugar falha com *"A program element with
this fully qualified name already exists in this CPU"*). Hoje isso está encapsulado em
**`tia move-block --name X | --pattern P* --folder A/B --apply`**.

- **`1.1 Acionamento`** (7): `FB_LIGA/DESLIGA MODO AUTO` · `FB_PARTIDA_INVERSOR` ·
  `FB CONDIÇÃO DE PARTIDA` · `FB MODOS DE OPERAÇÃO` · `FB VALVULA` · `FB INTERTRAVAMENTO_PAINEL` ·
  `FB SUCÇÃO OK`
- **`1.2 Inversores`** (4): `FB INVERSOR SIEMENS` · `SINA_SPEED_TLG20` · `FB AFERIÇÃO INVERSORES` ·
  `FB STATUS ECSX`
- **`1.3 Instrumentação`** (6): `FB AFERIÇÃO INSTRUMENTOS` · `FB LIMITES_OPERACAO_SENSOR` ·
  `FB FILTRO DE AMOSTRAGEM  ANALÍTICA` (dois espaços no nome) · `FB SETPOINT ESCALONAMENTO` ·
  `FB SETPOINT MANUAL` · `AUX_PID`
- **`1.4 Alarmes e Falhas`** (2): `FB FALHA` · `FB ALARME DIGITAL`
- **`1.5 Diagnóstico`** (5): `FB DIAG MODULES` · `DIAG to STRING` · `PROFINET_DEVICE_STATES` ·
  `FB PROFINET DEVICE STATES to BIT` · `FB PROFINET DEVICE STATES to Word`
  (o iDB `DIAG to STRING_DB` foi apagado do projeto pelo user em 2026-07-28 — era instância de
  teste em pasta errada. Saiu dos dois manifestos: **65 itens**, não 66. O XML continua em
  `blocks/` mas não é mais reposto nem instalado.)
- **`1.6 Comunicação Modbus`** (4): `FB MODBUS MASTER BLOCK` · `FB MODBUS MASTER BLOCK MMW` ·
  `FB MODBUS SCAN DRIVERS V1` · `FB MODBUS SCAN DRIVERS V2`
- **`1.7 Utilitários`** (5): `FB BITS TO WORD` · `FB BITS TO DOUBLE WORD` · `FB CONTADOR` ·
  `FB TOTALIZADOR` · `FB_HORÍMETRO`

### moldes dos geradores
| pasta | itens |
|---|---|
| `2. Fluxo de Controle` | `OB_MOLDE_PARTIDAS` (OB) |
| `3. Alarmes/Eventos/Falhas > 3.1 Alarmes Words` | `OB_MOLDE_ALARMES` (OB) |
| `… > 3.1 Alarmes Words > 3.1.0 Modelo` | `FC_Modelo` (FC) · `FB BITS TO WORD MODELO` (iDB) · `DB_DUMMY` (DB) |
| `… > 3.5 Barramento de Módulos` | `MODULE_ERROR_MOLDE` (OB) · `FB DIAG MODULES_DB` (iDB) · `DB DIAGNOSTICO DISPOSITIVOS` (DB) |
| `5. Instrumentação / Atuadores > 5.1 Aferição Analógica > 5.1.0 Molde` | `MOLDE_ANALOGS` (FC) |
| `5. Instrumentação / Atuadores > 5.2 Totalizadores > 5.2.0 Molde` | `MOLDE TOT1` (FC) |
| `8. Compartilhamento` | `DB GLOBAL` (DB, 869 KB — o item que trava a publicação) |

### acionamento-modelo `Soprador 1 (S-01A)` — 6 blocos
`PARTIDA_SOPRADOR_1 (S-01A)` (FC) + 5 iDBs (`FB CONDIÇÃO DE PARTIDA_S-01A`, `FB FALHA_S-01A`,
`FB SETPOINT ESCALONAMENTO S-01A`, `FB SETPOINT MANUAL S-01A`, `SINA_SPEED_TLG20_S-01A`), em
`4. Motores/Bombas > 4.1 Inversores_CCM1 > 4.1.1 Desarenador > Soprador 1 (S-01A)`. É a unidade
que `replicate-fc` replica.

**Detalhe do manifesto**: esse caminho é o único que **não** está declarado em `Folders` — os dois
últimos segmentos nascem na hora, porque `ResolveBlockPath` cria o que falta
([`Scaffold.cs:170`](../src/Tia.Core/Scaffold.cs#L170)). `Folders` só existe para pasta que deve
existir mesmo vazia.

### tabelas de tag
`DISPOSITIVOS_PROFINET` em `4. Comm > 4.1 Profinet` · `SOPRADOR_DESARENADOR (S-01A)` em
`3. Partidas`.

## Empacotamento (decisão fechada 2026-07-28)

`.scl` é o padrão para bloco novo autoral (texto diffável, SCL inteiro via `import-source`,
imune à versão do Engineering; limitação: nasce na raiz, contorno = `export-block` →
`import-block --folder` → `delete-block`). `.xml` só pro que precisa nascer em LAD legível —
é o caso de todo o payload atual, que veio de export. `.al19` descartado (binário, não diffa).
`import-ladder` não serve pra escrever biblioteca (sem timer, sem aritmética).

Desenho do núcleo genérico (o que seria autoral e publicável) na seção **"Biblioteca de
blocos"** de [`docs/PLANO.md`](../docs/PLANO.md).
