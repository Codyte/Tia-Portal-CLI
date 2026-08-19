<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L17    Achados — banho de inconsistência no projeto do PLC (2026-08-19) -->
<!--   L26    0. O que está são (não mexer) -->
<!--   L41    1. Cinco áreas com instrumentação inacabada -->
<!--   L58    2. Área 9 existe só como dado -->
<!--   L69    3. Nome da pasta da biblioteca: o repo e o projeto discordam -->
<!--   L85    4. Biblioteca: uma instância e uma versão superada -->
<!--   L98    5. R8 — lógica pesada em LAD -->
<!--   L110   6. Hardware -->
<!--   L124   7. Área %M — observação, não risco -->
<!--   L134   8. Resíduo e pastas vazias -->
<!--   L141   9. Sobre o `audit` verde -->
<!--   L151   Ordem sugerida -->
<!-- ======================= END NAV INDEX ======================= -->

# Achados — banho de inconsistência no projeto do PLC (2026-08-19)

Alvo: `PROJETO-MOLDE_V21`, PLC `CPU1.0 CCO` (6ES7 515-2AN03-0AB0/V3.1). Escopo: **programa do
PLC e hardware**. IHM ficou de fora a pedido.

Base de evidência: `tia audit`, `tia tree`, `compile`, `list-io-map`, `list-blocks`, `free-memory`,
`export-cax`, `list-devices`, e o XML da `DB GLOBAL` exportado pelo próprio `audit` (check R2).
Números crus em `workspace/analise/`.

## 0. O que está são (não mexer)

| Prova | Resultado |
|---|---|
| `compile` do PLC | 0 erros / 0 warnings |
| `tia audit` | 10 checks verdes, `complete: true` |
| população varrida pelo audit | 96 pastas · 475 blocos · 46 blocos de chamada · 195 tabelas |
| inversores × constante de telegrama | 34 drives, 34 constantes `Standard_telegram_20` — nenhum drive sem HWID |
| endereçamento de I/O | 38 módulos, todos endereçados, sem sobreposição |
| tipagem de equipamento na `DB GLOBAL` | UDT (`MotorDados`, `MotorPrincipal`, `Aferição CMD`) — R1 cumprido onde importa |
| biblioteca oficial | `SINA_SPEED_TLG20` (DriveLib) em uso, não reescrito |

O projeto **não** está quebrado. O que segue é trabalho inacabado e desalinhamento, não defeito
de funcionamento.

## 1. Cinco áreas com instrumentação inacabada

Desarenador (2), Casa de Sopradores (3), Casa de Cloro (14), Adensadores de Lodo (19) e
Desidratação de Lodo (23). Três evidências independentes apontam para as **mesmas cinco**:

1. sem pasta de tags em `2. Alarmes/2.N`;
2. sem pasta de blocos em `5. Instrumentação / Atuadores/5.1 Aferição Analógica/5.1.N`;
3. `DB GLOBAL.<AREA>.INSTRUMENTACAO` é **`Bool`**, enquanto as outras 15 áreas têm `Struct`.

O `Bool` é o resto do placeholder: o membro nasceu para virar Struct e não virou. Enquanto ele
for `Bool`, qualquer código ou tela que percorra `<AREA>.INSTRUMENTACAO.*` não compila contra
essas cinco.

Ordem de correção por área: criar a tabela em `2. Alarmes/2.N` → trocar o membro para `Struct`
(`add-db-member --member "<AREA>.INSTRUMENTACAO.<TAG>:..."` cria o ramo) → `replicate-instruments`.
É o pré-requisito 5 de área nova do `CLAUDE.md`, aplicado a área que já existe pela metade.

## 2. Área 9 existe só como dado

`DB GLOBAL.DECANTADOR_LAMELAR` tem `ALARMES` (WORD_ALARMES_1..5) + `EVENTOS`, 10 folhas, e
**nenhum** correspondente em `3. Partidas`, `2. Alarmes`, `3.1 Alarmes Words` ou `5.1`. Nenhum
equipamento, nenhuma FC, nenhuma tag.

Ou a área entra no escopo e ganha programa, ou o membro sai (`delete-db-member`). Deixar como
está gasta 5 words de alarme que ninguém escreve e que a IHM pode ler como zero eterno.

Os números de área 11, 16, 17 e 18 não existem em lugar nenhum — buracos limpos, sem dado órfão.

## 3. Nome da pasta da biblioteca: o repo e o projeto discordam

- Projeto (e todo projeto real da casa): **`1. FB Bilbiotecas`**
- Repo inteiro: **`1. FB Bibliotecas`** — `scripts/bake-lib.ps1` (default de `-Root`),
  `CLAUDE.md` (§`add-call`), `library/generic.json` e `library/packages.json`

A grafia certa só aparece em projeto **gerado pelo CLI**; molde, AsBuilt e o projeto real de
2026-07-18 têm o typo.

Consequência concreta: `install-lib.ps1` / `import-master-copy --folder "1. FB Bibliotecas"`
não acha a pasta, **cria uma paralela homônima a partir da raiz** e o gerador seguinte morre em
colisão de nome — é a regra do `--folder` completo do `CLAUDE.md`, disparada por uma letra.

Decisão pendente: renomear a pasta no projeto (só pela GUI — não há verbo de rename de pasta) ou
alinhar o repo à grafia real. Não dá para deixar os dois.

## 4. Biblioteca: uma instância e uma versão superada

`1. FB Bilbiotecas` = 33 FBs (25 LAD, 8 SCL) **+ 1 InstanceDB**.

- **`DIAG to STRING_DB` (iDB) mora na biblioteca.** `PADRAO.md` diz, sobre essa pasta:
  *"biblioteca — nada de instância aqui"*. Mover para a pasta do consumidor.
- **`FB MODBUS SCAN DRIVERS V1` (FB32) não tem nenhum iDB no projeto** — não é chamado. A V2
  (FB30) tem três (`_QA-01/02/03`). Versão superada viva ao lado da boa: o próximo que instanciar
  pega a errada.
- Outros 16 FBs sem instância são estoque de biblioteca (`FB VALVULA`, `FB CONTADOR`,
  `FB_HORÍMETRO`…) — é o propósito da pasta, não é dívida. **Ressalva:** multi-instância não
  aparece nessa contagem; ela mede iDB próprio.

## 5. R8 — lógica pesada em LAD

R8 quer chamada em LAD e **lógica pesada em SCL dentro de FB**. Estão em LAD, entre outros:

`FB SETPOINT ESCALONAMENTO`, `FB AFERIÇÃO INSTRUMENTOS`, `FB AFERIÇÃO INVERSORES`,
`FB FILTRO DE AMOSTRAGEM  ANALÍTICA`, `FB LIMITES_OPERACAO_SENSOR`, `AUX_PID`.

Escala, filtro e limite de sensor são exatamente o caso de SCL. E mais: **a LGF (Library of
General Functions, SIOS 109479728) já entrega escala, filtro de média e limites prontos e
testados** — `docs/GUIA-SIEMENS.md` chama isso de "dívida sem ganho". Não é para reescrever hoje;
é o que trocar quando um desses blocos precisar de manutenção.

## 6. Hardware

- **34 estações SINAMICS com nome default do TIA** — `SINAMICS G_3`, `G_4`, `G_15`…`G_48`, com
  buracos na numeração. O drive object **dentro** carrega o tag certo (`INVERSOR_B-05A CCM2`), e é
  dele que sai a constante de HWID — por isso o programa está certo. Mas a vista de rede, o
  diagnóstico e `DISPOSITIVOS_PROFINET` mostram o nome default. Renomear a estação (GUI).
- **Firmware desalinhado nas 4 cabeças ET200SP idênticas** (`6ES7 155-6AU02-0BN0`):
  `REM_RM1.0` e `REM.RM2.1` em **V6.3**; `REM_RM1.1` e `REM_RM3.1` em **V6.2**. Mesmo part number,
  duas versões — atrapalha peça reserva e comportamento uniforme.
- **Dois part numbers para o mesmo DI 16x24VDC ST**: `6ES7 131-6BH00-0BA0/V1.1` (um módulo em
  cada uma das stations 1 e 2) contra `6ES7 131-6BH01-0BA0/V0.0` em todo o resto.
- Grafia do nome: `REM_RM1.0`, `REM_RM1.1`, `REM_RM3.1` com underscore e **`REM.RM2.1`** com
  ponto. Uma convenção, quatro estações, três de um jeito.

## 7. Área %M — observação, não risco

`free-memory`: **2275 bytes ocupados**, mais alto em `%M14057`, **11782 bytes livres em 182
buracos**, maior contíguo **1934 bytes**, numa CPU 1515-2 PN (16 KB de memória de bits).

Não há risco de estouro. O que há é fragmentação e a escolha de guardar dado de programa em `%M`.
O style guide da Siemens (SIOS 81318674) manda dado de programa em **DB otimizado** — `%M` existe
por compatibilidade. Recomendação estreita: **não crescer mais por `%M`**; área nova nasce com
dado em DB.

## 8. Resíduo e pastas vazias

- Tabela de tags **`5. Teste / IO Drive`** (2 tags) — nível 1 fora do padrão (`5.` já é
  "Instrumentação / Atuadores" nos blocos; em tags o `5.` não tem dono declarado no `PADRAO.md`).
- Pastas de blocos vazias: `3.4 Eventos Automático`, `7. Comm Skids`, `9. Comm Supervisório`.
  Não quebram nada; declaram intenção que não aconteceu.

## 9. Sobre o `audit` verde

O check R2 do `audit` passou com a `DB GLOBAL` tendo **23 membros de topo, todos `Struct`
anônimo**. Ele procura *escalar solto na raiz*, que é a condição mais fraca. A leitura correta é
a que este documento fez: os **equipamentos** dentro dos Structs de área são UDT-tipados, então a
intenção de R1/R2 está cumprida — mas o `audit` não foi quem provou isso.

Se valer a pena endurecer: o check R2 poderia olhar o `Datatype` dos membros de 2º nível, não só
os de raiz.

## Ordem sugerida

1. §3 (nome da pasta) — decide sozinho e destrava `install-lib`/`bake-lib` nesse projeto.
2. §2 (área 9) — uma decisão, um verbo.
3. §1 (cinco áreas) — o volume real de trabalho.
4. §4, §6 — higiene, sem pressa.
5. §5, §7 — política para o que vier, não retrabalho do que existe.
