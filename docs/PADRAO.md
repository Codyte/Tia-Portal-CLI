# Padrão de projeto (referência: `Software de ETE Insular_Inicial_V21`)

Projeto-molde da casa, importado em `proj/` em 2026-07-27 (zap `..._LS_1_20260727_1715.zap19`,
upgrade V19→V21). É o projeto **conforme** — quando a CLI diverge dele, quem está errado é a CLI.
Banho read-only completo em `workspace/Software de ETE Insular_Inicial_V21/`
(`doctor.json`, `snapshot.json`, `list-tags.json`, `plc-navi.md`, `xref-obs.json`, AML).

PLC único: `CPU1.0 CCO` · 62 devices (3 IHMs WinCC + ET200 nos CCMs/QAs) · 476 blocos em 93 pastas
· 194 tabelas de tags / 4372 tags · 13 UDTs.

## Lei de nomenclatura

Todo nível-folha carrega o TAG do equipamento entre parênteses, e o mesmo TAG é o sufixo de todos
os blocos daquele equipamento:

| nível | exemplo |
|---|---|
| pasta de equipamento | `Soprador 1 (S-01A)` |
| tabela de tags | `PENEIRA_AUTO_LIMPANTE (GM-01A)` |
| FC de partida | `PARTIDA_SOPRADOR_1 (S-01A)` |
| iDB | `FB FALHA_S-01A` |
| pasta de área | `3.1.4 Elevatória de Gordura(EGDA-01)` |

O `(TAG)` no fim da pasta é exatamente o `(ID)` que `replicate-fc` exige. O AsBuilt não tem;
este tem — é aqui que `replicate-fc` deve ser exercitado.

## Program blocks

```
0. Main (16)                    FB INTERTRAVAMENTO_PAINEL_<QA/CCM> + FB ALARME DIGITAL_<QA>
                                OB Resets(129) · Paineis Intertravamento(128) · Paineis %I -> %M(133)
1. FB Bilbiotecas (34)          biblioteca — nada de instância aqui. FB BITS TO WORD, FB FALHA,
                                FB CONDIÇÃO DE PARTIDA, FB SETPOINT MANUAL/ESCALONAMENTO,
                                FB INVERSOR SIEMENS, SINA_SPEED_TLG20, FB MODBUS *, FB DIAG MODULES...
2. Fluxo de Controle (4)        OB_MOLDE_PARTIDAS + CHAMADA_INVERSORES_CCM{1,2,3}
3. Alarmes/Eventos/Falhas
   3.1 Alarmes Words            OB_MOLDE_ALARMES + CHAMADA_ALARMES
     3.1.0 Modelo               FB BITS TO WORD MODELO(iDB) · DB_DUMMY · FC_Modelo      <- molde
     3.1.0 Paineis
     3.1.N <Área>               DB_BITS_TO_WORD_<AREA>_W1..Wn + FC_ALARMES_<AREA>
   3.2 Comunicacao Profinet (8)
   3.4 Eventos Automático (0)
   3.5 Barramento de Módulos    MODULE_ERROR_MOLDE(OB) <- molde · OB_DIAG_QA_00..03
                                FB DIAG MODULES_DB · DB DIAGNOSTICO DISPOSITIVOS
4. Motores/Bombas
   4.N Inversores_CCM<N>/<Área>/<Equipamento> (TAG)   <- 6 blocos, sempre os mesmos:
       SINA_SPEED_TLG20_<TAG>          (iDB)
       FB SETPOINT MANUAL <TAG>        (iDB)
       FB SETPOINT ESCALONAMENTO <TAG> (iDB)
       FB CONDIÇÃO DE PARTIDA_<TAG>    (iDB)
       FB FALHA_<TAG>                  (iDB)
       PARTIDA_<NOME> (<TAG>)          (FC)
5. Instrumentação / Atuadores
   5.1 Aferição Analógica/5.1.0 Molde -> MOLDE_ANALOGS(FC) ; 5.1.N <Área> (4-17 blocos)
   5.2 Totalizadores/5.2.0 Molde      -> MOLDE TOT1(FC)    ; 5.2.N <Área> (2 blocos)
6. Comm Serial 485/QA-0N        7. Comm Skids
8. Compartilhamento             DB GLOBAL(DB46)  <- única DB global do programa
9. Comm Supervisório
```

Numeração de área é **estável entre as três hierarquias**: `3.1.N`, `5.1.N` e `2.N`/`3.N` (tags)
usam o mesmo N para a mesma área (ex.: 4 = Elevatória de Gordura, 15 = Elevatória Água de Serviço).

## PLC tags

```
(raiz)               Default tag table (18)
1. I/OS/QA-0N        ENTRADAS_DIGITAIS | SAIDAS_DIGITAIS | ENTRADAS_ANALOG | SAIDAS_ANALOG (QA-0N)
                     14 tabelas / 517 tags
2. Alarmes/2.N <Área>[/<Skid>]   1 tabela por instrumento: MEDIDOR_<TIPO> (<TAG>), 8-10 tags
                     42 tabelas / 401 tags
3. Partidas/3.N <Área>[/SKID <X>]  1 tabela por acionamento: <EQUIPAMENTO> (<TAG>)
                     131 tabelas / 3261 tags — 102 com 29 tags (acionamento padrão),
                     20 com 4 (válvula solenoide), 4 com 17 (válvula motorizada), 5 com 31
4. Comm/4.1 Profinet COM_PROFINET(81) · DISPOSITIVOS_PROFINET(45)
   4.2 ModBus RS485  QA-0N_COMM_MODBUS
```

`3. Partidas` é a raiz que o `standardize-tags` procura; 12 memory sets detectados.

## Endereço de alarme de módulo

Não existe DB chamada `ALARMES_MODULOS`. No molde `MODULE_ERROR_MOLDE` o acesso real é:

```
DB GLOBAL . HARDWARE_INTERRUPT . ALARMES_MODULOS . QA-00 . WORD_1.x0
DB DIAGNOSTICO DISPOSITIVOS . HW_DIAG_STATE . {OwnState,IOState,OperatingState,MaintenceState,
                                               ComponentStateDetail,Ret_Val}
```

`ALARMES_MODULOS` é **membro** da DB global, não bloco. `FaultObConfig.AlarmDb` é o nome do
`Component` no FlgNet — o `gen-fault-ob` já casava certo; só o check do `doctor` procurava bloco.

## Estado da CLI contra este projeto

`tia doctor`: `standardize-tags` ok · `gen-alarm-fc` ok (8/8: `3.1.0 Modelo`, `FC_Modelo`,
`OB_MOLDE_ALARMES`, `DB GLOBAL`, `2. Alarmes`, `3. Partidas`, `FB BITS TO WORD`, `3.1 Alarmes Words`)
· `gen-fault-ob` ok após a correção abaixo · `gen-profinet`/`replicate-fc`/`replicate-instruments`
pedem `--config`.

Correções que este projeto provocou:
- `Doctor.cs` — removido o check `alarm DB 'ALARMES_MODULOS'` (`FindBlock`): reprovava até em
  projeto 100% conforme, porque o alvo é membro de DB.
- `FaultOb.cs` — `RewireNetwork` agora **lança** quando o template não tem acesso ao `AlarmDb`;
  antes seguia em silêncio e todo OB gerado ficava com o bit de alarme do molde.
- `docs/examples/ModuleErrorMolde.xml` — fixture sintética (4.8K) trocada pelo export real do
  `MODULE_ERROR_MOLDE` (14K). Suíte offline segue `ALL PASS`: o `999` que o real usa também como
  índice de `HW_DIAG_STATE[999]` é trocado de propósito (o FINAL faz o mesmo, linha 398-407).

Configs de exemplo acertados contra este projeto:
- `replicate-instruments.json` — era `5. Instrumentos`/`5.2 Instrumentos`/`DB INSTRUMENTOS`/
  `OB_INSTRUMENTOS`, nada disso existe. Real: tags em `2. Alarmes`, blocos em `5.2 Totalizadores`,
  `DB GLOBAL`, OB `Chamada Totalizadores Instrumentos` (OB130). `IgnoreFolders`/`TagFilters` vêm
  do config FINAL (`2.0 Paineis`, molde da pasta, `FQIT`/`FIT`).
- `profinet.json` — `TagFolder` era `4. Comm`; a tabela `DISPOSITIVOS_PROFINET` vive em
  `4. Comm/4.1 Profinet`. **O script FINAL diverge do projeto aqui**: hardcoda `4. Comm` e
  *cria* a tabela lá se não achar (linhas 78-81) — rodá-lo neste projeto duplicaria a tabela.
  `ProfinetConfig.TagFolder` mantém o default `4. Comm` do FINAL; o exemplo aponta o certo.
  `Hardware` = nome da station (`SINAMICS G_24`), `EquipmentTag` = nome do device
  (`INVERSOR_AG-04 CCM1`) — pares tirados do config FINAL.

Pendências abertas contra este projeto:
- `replicate-fc` dry (pastas `(TAG)` existem aqui) e `replicate-instruments` dry.
- Projeto importado chega inconsistente: `export-*` morre com
  `Inconsistent blocks and PLC data types (UDT) cannot be exported`. `prep-project` resolve
  (compile Success / 0 erros / salvo em 2026-07-27).
