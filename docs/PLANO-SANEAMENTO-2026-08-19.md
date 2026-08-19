<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L15    Plano de saneamento — `PROJETO-MOLDE_V21` (PLC) -->
<!--   L20    Por que estrutura e nomenclatura vêm primeiro -->
<!--   L45    Economia de tokens — as cinco regras deste plano -->
<!--   L66    F0 — decisões (sem Portal, sem risco) -->
<!--   L77    F1 — nomenclatura (1 batch dry + 1 apply) -->
<!--   L109   F2 — estrutura de pastas (1 batch dry + 1 apply) -->
<!--   L135   F3 — `DB GLOBAL` (1 batch dry + 1 apply, `--fail-fast`) -->
<!--   L156   F4 — geradores (1 batch dry + 1 apply, `--fail-fast`) -->
<!--   L179   F5 — resto (1 batch) -->
<!--   L189   Contagem -->
<!-- ======================= END NAV INDEX ======================= -->

# Plano de saneamento — `PROJETO-MOLDE_V21` (PLC)

Companheiro de [ACHADOS-PROJETO-2026-08-19.md](ACHADOS-PROJETO-2026-08-19.md). Aquele diz **o
que** está torto; este diz **em que ordem** e **com quantas chamadas**.

## Por que estrutura e nomenclatura vêm primeiro

Não é estética. Os geradores do CLI **endereçam por nome e caminho**:

| Verbo | O que ele usa como endereço |
|---|---|
| `replicate-fc` | palavra-chave de `EquipmentTypes` no nome da pasta-molde + `(ID)` na pasta-folha |
| `gen-alarm-fc --area X` | nome da pasta sob `2. Alarmes` |
| `replicate-instruments` | árvore de `5.1 Aferição Analógica` |
| `install-lib` / `import-master-copy --folder` | caminho literal, **a partir da raiz** |
| `audit` R9 | `(TAG)` na folha + tabela de tag de mesmo nome |

E o modo de falha é o pior possível: **`--folder` errado não falha — cria uma árvore paralela**
(`CLAUDE.md`, regra do caminho completo). O gerador seguinte é que morre, em colisão de nome, num
lugar sem relação com a causa.

Some-se a isso o que o `PADRAO.md` chama de invariante: **o número da área é o mesmo nas quatro
hierarquias** (`3.N` partidas, `2.N` alarmes, `3.1.N` alarm words, `5.1.N` instrumentos). Onde ele
diverge, `--area` deixa de ser escopo confiável — é o caso da área 6 abaixo.

Consertar detalhe antes de nome é consertar num endereço que vai mudar. Daí a ordem.

O que isso indica sobre o projeto: **ele cresceu na GUI, fora dos geradores.** Por isso a lei que
os geradores codificam derivou em pontos isolados, e não de forma sistemática.

## Economia de tokens — as cinco regras deste plano

1. **Uma fase = um `run --script`**, não N chamadas. Attach é ~7 s **por chamada**; num batch é
   ~7 s **por fase**. 5 fases em batch = ~35 s de attach contra ~9 min soltas.
2. **`--summary` sempre.** A conversa recebe `{steps, failed, ms, slowest[3], errors[]}` — ~10
   linhas — em vez do resultado de cada step. É isto que impede a conversa de inchar.
3. **Todo step de leitura com `--out-file`.** O JSON vai pro disco; só o que falhou é lido, e por
   `grep`, não por `Read` do arquivo inteiro.
4. **O mesmo arquivo roda dry e apply.** Acrescentar `--apply` nos steps é a única diferença.
   Corrigir e repetir = editar uma linha, não remontar o batch.
5. **`--fail-fast` em corrente de escrita.** Sem ele o step seguinte trabalha em cima do que o
   anterior não fez. Em bateria de diagnóstico (só leitura) é o contrário: sem `--fail-fast`,
   para colher todos os erros de uma vez.

**Reentrada:** `errors[]` do `--summary` traz o índice do step. Conserta aquele step, roda o mesmo
arquivo de novo. `create-folder` em pasta existente é `reuse`, `gen-alarm-fc` volta `in-sync`,
`compile` é idempotente — repetir é barato. A exceção é `delete-db-member`: na 2ª vez falha com
"não achei", o step fica `ok:false` e (sem `--fail-fast`) o batch segue.

---

## F0 — decisões — **TOMADAS pelo usuário em 2026-08-19**

Quatro. Sem elas a F1 não começa, porque cada uma escolhe um nome que tudo abaixo usa.
O usuário aprovou as quatro recomendações e a contagem de ~11 chamadas.

| # | Decisão | **Resolvido** |
|---|---|---|
| D1 | `1. FB Bilbiotecas` (projeto) × `1. FB Bibliotecas` (repo) | **Alinhar o repo ao projeto** — `bake-lib.ps1`, `CLAUDE.md`, `library/generic.json`, `library/packages.json` passam a dizer `Bilbiotecas`. 4 arquivos, zero Portal, zero risco. Rejeitado o caminho oposto: não há verbo de rename de pasta, seriam 33 `move-block`, e os 3 projetos reais da casa já têm a grafia atual. |
| D2 | `3.6 Tanque de Aeração 03/Reator (MBBR2)` | **Trocar para `(MBBR3)`.** Evidência: os equipamentos dentro são todos `-03` (`BRI-03`, `MS-03A/B`) e o lado de alarme já diz `2.6 …/Reator (MBBR3)`. `MBBR2` aparece 1× no projeto inteiro. |
| D3 | `DB GLOBAL.DECANTADOR_LAMELAR` (área 9) | **Sai da DB** — `delete-db-member --db "DB GLOBAL" --name DECANTADOR_LAMELAR`. Dado sem programa em nenhuma das quatro hierarquias. |
| D4 | Tabela de tags `5. Teste / IO Drive` (2 tags) | **Apagar** — `delete-folder --tags --path "5. Teste"`. |

## F1 — nomenclatura (1 batch dry + 1 apply)

Só renomeia. Nada de conteúdo novo.

**F1.a — D2, renomear pasta de tags.** Não há rename de pasta; é reconstrução, e **nesta ordem**
(o `delete-folder` não tem backup — a pasta velha só morre depois de a nova estar populada):

```json
[
  ["create-folder","--tags","--path","3. Partidas/3.6 Tanque de Aeração 03/Reator (MBBR3)","--apply"],
  ["export-tags","--table","<cada tabela de 3.6 .../Reator (MBBR2)>","--out","workspace/f1"],
  ["import-tags","--file","workspace/f1/<tabela>.xml","--folder","3. Partidas/3.6 Tanque de Aeração 03/Reator (MBBR3)","--apply"],
  ["delete-folder","--tags","--path","3. Partidas/3.6 Tanque de Aeração 03/Reator (MBBR2)","--apply"]
]
```

`--fail-fast` obrigatório aqui: se o import falhar, o delete **não** pode rodar.

**F1.b — D1**, se for o lado do repo: `scripts/bake-lib.ps1` (default de `-Root`), `CLAUDE.md`,
`library/generic.json`, `library/packages.json`. Nenhum verbo, nenhum Portal.

**F1.c — hardware.** Antes de qualquer `set-attr`, **um step de sonda**, porque não está provado
que o nome da estação é atributo gravável:

```json
[["list-attrs","--device","SINAMICS G_3","--like","Name","--out-file","workspace/f1/probe-name.json"]]
```

Se `Name` aparecer: 34 steps `set-attr --device "SINAMICS G_N" --name Name --value "INVERSOR_<TAG>_<CCM>"`
num batch só (o valor sai do drive object de dentro, que já tem o tag certo), mais
`REM.RM2.1` → `REM_RM2.1`. Se não aparecer, é GUI e sai do plano.

## F2 — estrutura de pastas (1 batch dry + 1 apply)

`create-folder --path` é repetível: **as 10 pastas num step só**.

Faltam, para as áreas 2, 3, 14, 19 e 23:

- `2. Alarmes/2.2 Desarenador (DA-01)`, `2.3 Casa de Sopradores`, `2.14 Casa de Cloro (CC-01)`,
  `2.19 Adensadores de Lodo`, `2.23 Desidratação de Lodo` — com `--tags`
- `5. Instrumentação \/ Atuadores/5.1 Aferição Analógica/5.1.{2,3,14,19,23} <mesmo nome>` — blocos

**Atenção ao `/` literal:** `5. Instrumentação / Atuadores` é **uma** pasta. Na linha de comando
escreve-se `5. Instrumentação \/ Atuadores`; **dentro do JSON do batch, `\\/`** — `\/` é escape de
JSON e o parser o come antes de o argumento chegar no CLI.

No mesmo batch, de graça (mesmo attach), a leitura que a F3 precisa:

```json
["list-tags","--table","<tabelas de 1. I\\/OS/QA-0N>","--out-file","workspace/f2/pv-tags.json"]
```

Confirmação barata no fim do batch: `["list-blocks","--count"]` — 10 linhas, diz se a árvore
ficou como se pediu.

**As 3 pastas vazias** (`3.4 Eventos Automático`, `7. Comm Skids`, `9. Comm Supervisório`) **ficam.**
Não quebram compile, `delete-folder` não tem backup, e apagar declaração de intenção não paga.

## F3 — `DB GLOBAL` (1 batch dry + 1 apply, `--fail-fast`)

Duas edições por área, nesta ordem, para as 5:

```json
[
  ["delete-db-member","--db","DB GLOBAL","--path","DESARENADOR","--name","INSTRUMENTACAO","--apply"],
  ["add-db-member","--db","DB GLOBAL","--member","DESARENADOR.INSTRUMENTACAO.<TAG>.<CAMPO>:<Tipo>","--apply"]
]
```

Porquê nessa ordem e não `edit-db-member --type Struct`: `Struct` vazio deixa o DB inconsistente e
trava todo verbo que exporta — por isso `--type Struct` é recusado. O `add-db-member` com caminho
cria o ramo **já com a folha dentro** (`structsCreated` lista o que nasceu).

**Uma chamada, N membros:** `--member` é repetível e o custo é do tamanho do DB, não do número de
edições — 5 membros na `DB GLOBAL` custam 23,9 s contra 23,4 s de 1 (`docs/BENCHMARKS.md`). Então
as 5 áreas cabem em **um** `add-db-member`.

Se D3 = sair: mais um step, `["delete-db-member","--db","DB GLOBAL","--name","DECANTADOR_LAMELAR","--apply"]`.

## F4 — geradores (1 batch dry + 1 apply, `--fail-fast`)

```json
[
  ["replicate-instruments","--config","<cfg>","--apply"],
  ["gen-alarm-fc","--area","Desarenador (DA-01)","--area","Casa de Sopradores","--area","Casa de Cloro (CC-01)","--area","Adensadores de Lodo","--area","Desidratação de Lodo","--apply"],
  ["compile","--apply","--errors"],
  ["audit","--out-file","workspace/f4/audit.json"]
]
```

Três coisas que essa forma resolve sozinha:

- **`--area` é repetível**: as 5 áreas num step. Sem escopo, criar 1 área **regenera todas**.
- **`compile --apply` e `audit` como últimos steps do mesmo batch**: o portão não custa attach
  extra. `error` de topo em qualquer step = `exit 1` e o step conta em `failed`.
- **Não é preciso `compile --apply` entre os steps** — todo export do CLI passa por
  `Ops.ExportFresh`, que compila só o alvo (desde 2026-08-13).

Leitura do `audit`: `ok: true` com **`complete: false`** é conformidade *não provada*, não projeto
aprovado. Conferir `scanned` (`folders/blocks/callBlocks/tagTables`) contra 96 / 475 / 46 / 195 —
população que encolheu quer dizer check cego, não check limpo.

## F5 — resto (1 batch)

- `FB MODBUS SCAN DRIVERS V1` (FB32, zero iDB): `delete-block --name "FB MODBUS SCAN DRIVERS V1" --apply`.
- `DIAG to STRING_DB` (iDB na biblioteca): `move-block --name "DIAG to STRING_DB" --folder "<pasta do consumidor>" --apply`.
- D4, se apagar: `delete-folder --tags --path "5. Teste" --apply`.

**Fora do CLI, deliberadamente:** o DI 16x24 com dois part numbers (`-6BH00` × `-6BH01`) e o
firmware ET200SP desalinhado (V6.2 × V6.3). Trocar módulo remapeia endereço, e qual módulo está no
painel é pergunta de campo, não de projeto. Documentado, não automatizado.

## Contagem

| Fase | Batches (dry+apply) | Bloqueada por |
|---|---|---|
| F0 decisões | 0 | — |
| F1 nomenclatura | 2 (+1 sonda) | D1, D2 |
| F2 estrutura | 2 | F1 |
| F3 `DB GLOBAL` | 2 | F2 (leitura dos PV) + D3 |
| F4 geradores | 2 | F3 |
| F5 resto | 2 | D4 |

**~11 chamadas ao Portal no total.** A mesma lista de tarefas em chamadas soltas passa de 80.
