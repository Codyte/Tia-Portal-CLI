# Resultado do teste cego FP-02 — 2026-08-10

Entrega do item 11 do [`caderno-FP-02.md`](caderno-FP-02.md): elevatória de esgoto bruto +
tratamento preliminar, duas áreas, montadas **inteiramente pelos verbos do CLI** — zero SCL
autoral, zero edição manual no Portal.

O objetivo da rodada não era o programa: era exercitar os 7 verbos `--apply` que até aqui nunca
tinham construído planta nenhuma. Todos os 7 rodaram, e cada um deixou defeito no caminho.

**Ressalva de condução:** como na FP-01, a rodada **não foi cega** — a mesma linhagem de sessões
escreveu o caderno e executou. O que vale são os defeitos de ferramenta, que independem de quem
executa.

## Entregue

Projeto `workspace/blind/FP02/FP02.ap21`, PLC `CPU_EEB02`, **compile Success / 0 erros / 0
warnings**, **`audit` 6/6**, salvo.

| Camada | O que entrou |
|---|---|
| I/O | 6 tabelas (`1. I/OS/QA-00`, `QA-01`) |
| Instrumentos | 4 tabelas `MEDIDOR_* (<TAG>)` em `2. Alarmes/2.1` e `2.2` |
| Acionamentos | 5 (`replicate-fc`), 6 blocos cada, tags padronizadas |
| Diagnóstico | `OB_DIAG_QA_00` e `OB_DIAG_QA_01`, 11 módulos cada (`gen-fault-ob`) |
| Alarmes | `FC_ALARMES_*` por área + `CHAMADA_ALARMES` (`gen-alarm-fc`) |
| Analógicas | `*_ANALOGS` por área, 4 instrumentos (`replicate-instruments`) |

## Verbos exercitados

| Verbo | 1º `--apply` da vida | Defeitos que expôs |
|---|---|---|
| `replicate-fc` | não (FP-01) | 3 (commit `4289164`) |
| `create-folder` / `import-tags` | sim | 1 — pasta com `/` no nome (`2f3896a`) |
| `gen-fault-ob` | **sim** | 1 — exigia `DeviceUserGroup` (`2f3896a`) |
| `gen-alarm-fc` | **sim** | 3 (`383a3bf`) |
| `replicate-instruments` | **sim** | 4 + 1 (`5c4dce8`) |
| `standardize-tags` | **sim** | 1 (`995a7cb`) |
| `move-block` | sim | 0 |

## Defeitos desta sessão

### `gen-alarm-fc`

1. **Struct da DB global vinha de heurística sobre nome de tag.** O verbo procurava, entre os
   membros da DB, um cujo nome contivesse o nome de uma tag da área. Projeto cujos ramos não
   repetem o nome da área (aqui, `AREA_01`/`AREA_02`) caía no fallback e gerava FC apontando para
   struct inexistente — erro de compile longe da causa. Config ganhou `Structs` (área → struct),
   validado contra a DB exportada.
2. **O OB de chamada listava o FC do molde.** A pasta do molde mora *dentro* do root alvo
   (`3.1 Alarmes Words/3.1.0 Modelo`) e a varredura pegava tudo. Passou a pular `TemplateFolder`.
3. **Tag `Real` entrava como bit de alarme.** A tabela de instrumento mistura o valor analógico com
   os bits `STS_*`, e o `FB BITS TO WORD` só aceita `Bool`: 4 erros de
   `Real ≠ Bool` no compile. A coleta filtra por tipo.

### `replicate-instruments`

4. **Molde genérico não era aceito.** O instrumento do molde era *adivinhado* entre os instrumentos
   reais do projeto — o que só funciona se o molde for o FC de um instrumento do próprio projeto.
   Molde vindo de biblioteca (`INSTR_01`) reprovava com "Could not identify the template's mold
   instrument". Config ganhou `MoldInstrumentId`; basta o instrumento existir na DB global.
5. **`ExtractId` parava no primeiro separador.** `PIT-10_STS_X` virava Id `PIT`, e o replace gerava
   `PIT_01_*` — o número do molde, não o do instrumento. Passou a capturar letras + número, e a ler
   o TAG do parêntese na tag do valor analógico.
6. **Nome de membro de DB não aceita hífen.** O Id `LIT-01` nunca casava `MEDIDOR_DE_NIVEL_LIT_01`.
   A busca normaliza hífen/underscore.
7. **A tag do valor de processo tem sufixo próprio de cada instrumento** (`NIVEL_POCO` ×
   `VAZAO_INSTANTANEA`), então trocar o Id do molde produzia tag inexistente. O instrumento passou
   a carregar a `PvTag` descoberta.
8. **O gate de "in-sync" olhava só a existência do FC.** Bloco gerado por molde velho ficava preso,
   sem nunca ser regenerado — o `--apply` seguinte devolvia `in-sync` com o bloco errado no
   projeto. Agora compara conteúdo, como o `gen-alarm-fc` já fazia. **É o defeito mais grave da
   rodada**: silencioso, e some no relatório como sucesso.

### `standardize-tags`

9. **ID com underscore duplicava no nome.** O filtro de identificador só reconhece ID com hífen
   (`BG-01A`); tabela cujo ID é `MOTOR_01` produzia
   `MOTOR_01_CMD_MOTOR_01_BOTAO_RESET_FALHA`. O prefixo do masterId passou a ser removido antes do
   split.

## O que a rodada ensinou sobre os geradores

Os 4 geradores nasceram como port de scripts escritos *para um projeto*. Todo defeito desta sessão
é a mesma família: **o gerador confundia "o que este projeto tem" com "o que todo projeto tem"** —
o nome da área repetido na DB, o molde sendo um instrumento real, o ID sempre com hífen, o sufixo
da tag de PV sempre igual, o molde nunca mudando. O que resolveu, em todos os casos, foi mover a
suposição para o config e deixar o código exigir só o que é estrutural.

## Aberto

- **`import-source` sem BOM = mojibake silencioso** (`AferiÃ§Ã£o CMD`), erro de compile longe da
  causa. Vale um gate no verbo.
- **`run --script` exige projeto já aberto** — batch não pode começar com `create-project` /
  `open-project`.
- **`use-project.ps1` com o Portal fechado** continua não provado (a rota da sessão 0 pede caminho
  absoluto; a tentativa desta rodada morreu antes disso).
- `WalkFolders` (longest-match de pasta) não tem teste offline: precisa de `PlcSoftware` vivo, e foi
  validado só em runtime.
