# Resultado do teste cego FP-01 — 2026-08-07

Entrega do item 9 do [`caderno-FP-01.md`](caderno-FP-01.md): projeto TIA novo, hardware do item 3
configurado e endereçado, lista de I/O do item 4 em tabelas de tag, programa organizado no padrão
de pastas da casa e a sequência do item 6 como bloco próprio, chamado ciclicamente.

**Ressalva de condução:** esta rodada **não foi cega**. A sessão que executou herdou o handoff do
repo (a mesma que escreveu o caderno), o que viola o item "quem escreveu o caderno não executa" de
[`criterios.md`](criterios.md). O que vale desta rodada são os tropeços de ferramenta, que
independem de quem executa; o veredito de "um agente sem contexto consegue" continua **não provado**.

## Entregue

Projeto `workspace/blind/FP01/FP01.ap21`, PLC `CPU_FP01`, **compile Success / 0 erros / 0 warnings**,
salvo.

### Hardware

| Item | Configurado |
|---|---|
| CPU | `6ES7 515-2AN03-0AB0/V3.1`, nome `CPU_FP01`, IP `192.168.0.10/24`, system+clock memory ligados (`%MB1`/`%MB0`) |
| Periferia | ET200SP `ET200_FP01`, IM `6ES7 155-6AU30-0CN0/V4.2`, IP `192.168.0.11/24` |
| Cartões | DI 16×24 V `6ES7 131-6BH01-0BA0` @ `%I0.0` · DQ 16×24 V/0,5 A `6ES7 132-6BH01-0BA0` @ `%Q0.0` · AI 8×I 2 fios `6ES7 134-6GF00-0AA1` @ `%IW64` · módulo servidor `6ES7 193-6PA00-0AA0` |
| Inversor | `BL-01`, `6SL3244-0BB12-1FA0/4.7.13`, IP `192.168.0.20/24`, **telegrama padrão 20** (trocado in-place sobre o MainTelegram #1 de fábrica) |
| Rede | sub-rede `PN_FP01`, IO system `PNIO_FP01` — CPU cria, ET200SP e inversor entram como IO devices |

### Tags — 35 em 4 tabelas

`1. I-OS/ET200_FP01`: `ENTRADAS_DIGITAIS (ET200_FP01)` (16), `SAIDAS_DIGITAIS (ET200_FP01)` (9),
`ENTRADAS_ANALOG (ET200_FP01)` (2). `1. I-OS/BL-01`: `INVERSOR (BL-01)` (8, telegrama 20).

Os 27 pontos do item 4 conferidos um a um contra o caderno: **0 divergências de endereço**.

### Programa — 22 blocos em 13 pastas

```
0. Main                      Main (OB1)
1. FB Bibliotecas            FB INSTRUMENTO_ANALOGICO · FB PARTIDA_DIRETA · FB INVERSOR_TLG20
2. Fluxo de Controle         FC SAIDAS_FP-01
3. Alarmes/3.1 Desidratacao  FB+DB ALARMES_DESIDRATACAO · CHAMADA_ALARMES_DESIDRATACAO
4. Motores/4.1 Desidratacao/Bomba de Lodo (BL-01)        DB INVERSOR_BL-01 · PARTIDA_BOMBA_DE_LODO (BL-01)
                            /Unidade Hidraulica (BH-01)  DB PARTIDA_BH-01 · PARTIDA_UNIDADE_HIDRAULICA (BH-01)
                            /Bomba de Lavagem (BW-01)    DB PARTIDA_BW-01 · PARTIDA_BOMBA_DE_LAVAGEM (BW-01)
5. Instrumentacao/5.1 Desidratacao          AFERICAO_DESIDRATACAO
     /Pressao de Alimentacao (PIT-01)       DB INSTRUMENTO_PIT-01
     /Pressao Hidraulica (PIT-02)           DB INSTRUMENTO_PIT-02
6. Sequencia/6.1 Filtro Prensa (FP-01)      FB+DB SEQUENCIA_FP-01 · CICLO_FILTRO_PRENSA (FP-01)
7. Intertravamentos (FP-01)                 FC INTERTRAVAMENTOS_FP-01
8. Compartilhamento                         DB_FP01
```

Cadeia cíclica: `Main (OB1)` → `CICLO_FILTRO_PRENSA (FP-01)` → `FB SEQUENCIA_FP-01`. Nenhuma
chamada órfã.

Os 9 passos do item 6 estão no `FB SEQUENCIA_FP-01` com os tempos máximos do caderno, a rampa de
25→45 Hz em 30 s, a manutenção de pressão 200/170 bar ativa em S2 **e** S3, e o desvio para S5 no
estouro de tempo. Os 8 intertravamentos do item 7 estão implementados: 1 e 3 em
`FC INTERTRAVAMENTOS_FP-01` (derrubam saída no mesmo ciclo, fora do CASE da sequência), 2 e 5 como
liberação por equipamento, 4 dentro do S3, 6 no `FC SAIDAS_FP-01`, 7 e 8 no `FB PARTIDA_DIRETA`.
Os 12 alarmes do item 8 em `FB ALARMES_DESIDRATACAO`.

100 % autoral: nenhum bloco veio de `install-lib`, `replicate-fc` ou `gen-alarm-fc`.

## Portões

| # | Portão | Resultado |
|---|---|---|
| G1 | compila | **passa** — Success, 0 erros, 0 warnings (a 1ª compilação, antes de mover os blocos para as pastas, deu 0 erros / 21 warnings; após os moves e recompilações, 0/0) |
| G2 | hardware presente e conectado | **passa** |
| G3 | endereçamento fiel | **passa** — 27/27 |
| G4 | sequência chamada ciclicamente | **passa** |

## Tropeços (o produto do teste)

### 1. Endereço inicial de módulo de I/O não tinha verbo — e nenhum caminho existente chegava lá

O caderno põe as analógicas em `%IW64`; a ET200SP nasceu com o AI em `%IW2`. `list-attrs` não
mostra endereço (não é atributo do `DeviceItem`), `set-attr` portanto não alcança, e
**`import-cax` aceitou o AML com `StartAddress` editado e ignorou a mudança em silêncio** — o
export seguinte continuava em 2. Custou um verbo novo, `set-io-address`
([Hardware.cs](../../src/Tia.Core/Hardware.cs)): os `Address` vivem no submódulo, não no módulo que
o usuário nomeia, então ele varre item + descendentes.

*Defeito da ferramenta, não da sessão.* O `import-cax` engolir a alteração sem erro é o pior
pedaço: o caminho parecia ter funcionado.

### 2. O telegrama do inversor não expõe endereço por API nenhuma

`DeviceItem.Addresses` do G120 vem vazio, `list-telegrams` (mesmo dumpando todos os atributos do
`Telegram`: só `PKW`, `TelegramNumber`, `Type`) não traz endereço, e o CAx não exporta o drive com
endereço. Sem os endereços não dá para escrever o STW1/NSOLL.

Descoberto **por sonda indireta**: mover o AI da ET200SP pelo mapa de entradas e ver onde
`set_StartAddress` responde `"This address is already being used. Next free address: 268"`. Daí
saiu o telegrama em `%IB256..267` (entrada) e `%QB256..259` (saída). É uma sonda de 18 chamadas
para uma informação que o Portal mostra num clique.

*Defeito da ferramenta.* Falta um `list-io-map` (ou o endereço no `list-telegrams`).

### 3. TypeIdentifier de módulo ET200SP: descoberto na força bruta

Nenhuma das duas ajudas (`--search` e `--sdk`) responde "qual o TypeIdentifier do DI 16×24 V da
ET200SP" — elas documentam o *formato*, não o catálogo. `canPlug` foi a única sonda. Duas
armadilhas caras:

- Sem sufixo de firmware, `canPlug` é `false`. **Os módulos de I/O querem `/V0.0`**
  (`6ES7 131-6BH01-0BA0/V0.0`), mas o AI 8×I quer `/V2.0` e o módulo servidor `/V1.0` — não há
  regra, é tentativa.
- `plug-module` sem `--item` mira o *device*, onde nada pluga. O alvo é `--item Rack_0`, e o dry-run
  do próprio verbo mostra isso na lista `itemSlots` — mas só se a pessoa reparar.

*Metade defeito de documentação:* uma linha no `CLAUDE.md`/`VERBS.md` dizendo "módulo de ET200SP =
`--item Rack_0` + sufixo de firmware obrigatório, descobrir com `canPlug`" teria economizado ~8
chamadas.

### 4. `import-tags` e `import-block` não ativavam a cultura do XML

Projeto novo nasce só com a cultura de instalação do Portal. `Ops.EnsureCultures` existia desde o
aceite do `scaffold`, mas **só o `scaffold` chamava**: todo `import-tags` com comentário `pt-BR`
morria com `Cannot import multilingual text with culture 'pt-BR'`. Corrigido na raiz — os dois
imports agora chamam `EnsureCultures` (o projeto sai do `PlcSoftware` por `Parent`, que é o que
faltava para a função servir fora do `scaffold`).

*Defeito da ferramenta*, e do tipo que só aparece em projeto novo — exatamente o caso de uso que a
demonstração para a Siemens vai ter.

### 5. `TITLE` em bloco SCL derruba o `import-source` inteiro

`FUNCTION_BLOCK` com `TITLE = '...'` depois do `VERSION` → `Syntax error: The specified value
"TITLE = $'...$'$L" is invalid` e **o lote inteiro é abortado** ("The block generation has been
stopped"): 3 blocos válidos não entraram por causa de 2 inválidos. Resolvido virando comentário.

*Defeito de documentação:* nada no repo diz onde o `TITLE` pode ficar num source SCL.

### 6. IDs de XML de tabela de tag precisam ser únicos no documento inteiro

Reaproveitar `ID="F"` em objetos irmãos dá `Duplicate Simatic ML ID 'F'`. Duas rodadas perdidas
(uma por reusar o ID, outra porque o meu regex de renumeração não pegava IDs não-hexadecimais).
Vale uma nota no repo, ou um gerador de tabela de tag — montar o XML na mão é passo obrigatório
hoje porque `add-tag` exige tabela já existente.

### 7. `move-block` exige `compile --apply` entre **cada** move

Documentado no `CLAUDE.md` em termos de "compile entre etapas", mas a forma prática só aparece
apanhando: um lote de 17 `move-block` seguidos falha do 12º em diante, porque cada move deixa
inconsistente quem referencia o bloco movido. O lote que funciona é `move → compile → move →
compile…` (35 steps). Isso deveria estar no help do `move-block`, ou o próprio verbo deveria
compilar antes de exportar.

### 8. `create-folder --path` não expressa nome de pasta com `/`

A lei da casa tem `1. I/OS` e `3. Alarmes/Eventos/Falhas`; `--path` usa `/` como separador, então
esses nomes são inatingíveis. O `scaffold` já resolve isso com lista de segmentos — `create-folder`
não. Entreguei como `1. I-OS`, que **não** é o nome do padrão. Divergência assumida, não erro de
digitação.

### 9. O que precisou ser adivinhado por causa do caderno (de propósito)

- Códigos dos módulos da ET200SP (o caderno delega ao integrador).
- Frequência de referência do inversor (adotado 50 Hz).
- Faixa bruta do cartão (0..27648 para 4..20 mA) e limiar de rompimento de fio (3,6 mA / 3 s).
- Colisão de nome: `KM-BH01` é entrada (retorno) **e** saída (contator) no caderno. Tag de PLC é
  global, então o retorno virou `KM-BH01_RETORNO` (idem `KM-BW01`).
- O caderno lista 9 saídas digitais nomeadas; o critério G3 fala em "10 DO usadas". Segui o caderno.

## Cliques no GUI

**Zero.** Os três `rebuild.ps1` (verbo novo + sonda + correção de cultura) refizeram a whitelist
sem abrir o diálogo modal de autorização do Openness, mesmo com o Portal aberto. Uma interrupção
manual houve na primeira chamada de `create-project` — mas o projeto já tinha sido criado e a
sessão seguiu por `attach`.

## O que mudou na CLI por causa desta rodada

- **`set-io-address`** — verbo novo (endereço inicial de módulo de I/O; sem `--item` varre o device).
- **`list-telegrams`** — passa a dumpar os atributos do `Telegram` (foi assim que se provou que o
  endereço não está lá).
- **`import-tags` / `import-block`** — chamam `EnsureCultures`; `Ops.ProjectOf(PlcSoftware)` novo.

## Pendências

- Rodar de novo **de verdade cego**, com sessão sem handoff, agora que os 4 defeitos de ferramenta
  acima estão corrigidos ou documentados.
- `audit` fecha 3/5: 3 acionamentos com 2 blocos (contra os 6 da lei) e 2 sem tabela de tag própria.
  É consequência de não usar a biblioteca da casa — o caderno foi desenhado para isso. Registrado,
  não "consertado".
- Os 21 warnings da primeira compilação não foram inspecionados um a um: depois dos moves e das
  recompilações o Portal fecha em 0/0 e não reemite a lista.
