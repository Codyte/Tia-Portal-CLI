# Handoff · TIA Portal Openness API · 2026-08-11

## Goal
Fila de correção do `BOAS-PRATICAS.md` §3 fechada (os 4 itens abertos que os testes cegos abriram).
O que sobra é escolher a próxima frente — a recomendação é **FP-04**, o caderno cego que mede os
verbos que nasceram da FP-03 e nunca foram exercitados numa rodada.

## State
- HEAD: `6c261a6`. Working tree limpo fora deste handoff.
- Live state: **TIA Portal aberto** (sessão 1) com `LIB_TESTE` (PLCs `PLC_ZERO` e `PLC_RT`).
  O projeto foi tocado no smoke e **devolvido ao estado original** (pasta de teste criada e
  apagada; 20 pastas, 0 ocorrências de `SMOKE`), sem `save-project`. O shell desta sessão nasceu
  na **sessão 0** (roteia pela task `TiaSmokeRun`). `tia.exe` está em dia com o HEAD (`rebuild.ps1`
  rodou duas vezes hoje) — `pack.ps1` aceita empacotar.
- Done nesta sessão:
  - **Artigo do teste cego** — `docs/teste-cego/artigo.md`, PT-BR, 180 linhas, arco FP-01→FP-03,
    linkado no README. Commit `591fc61`.
  - **`create-folder --path` repetível** + **`\/` = barra literal** em qualquer caminho de pasta
    (a regra é do `Ops.SplitPath`, sob o longest-match do `WalkFolders`). Fecha F1/F2 do §F.
    Shape do JSON mudou: `{kind,paths,created,failed,applied,folders[]}`.
  - **`audit` com 4 checks novos** (10 no total): R1 UDT, R2 DB global sem escalar solto, R8
    linguagem do bloco de chamada, `CHAMADA_*` fora da pasta de área. Check que não pode rodar
    devolve `skipped` com motivo e **não reprova**. `--db` nomeia a DB global.
  - **`list-io-map`** — verbo novo (78 verbos agora).
  - **`import-ladder` com `CALL` descartado** com motivo escrito no BOAS-PRATICAS §3.4.
  - Smoke real contra `LIB_TESTE`/`PLC_ZERO`: audit 10/10 verde (`udts: 6`, R2 achou e exportou a
    `DB GLOBAL`); `create-folder` criou UMA pasta com barra no nome (prova: `delete-folder --path
    "9. SMOKE\/BARRA"` resolveu — se fossem dois níveis, o escape procuraria um filho com esse nome
    literal e falharia); re-apply devolveu `created: 0`.
- In progress: nada.

## Decisions (and why)
- **`audit` deixou de ser 100% read-only.** O check R2 exporta a DB global para `--out`: só o
  export mostra o datatype dos membros, não há caminho de API que dê isso sem exportar. Aceito
  porque o alternativo era não ter o check.
- **`\/` em vez de lista de segmentos** (que era a proposta original do BOAS-PRATICAS §3.4). A lista
  só serviria ao `create-folder`; o escape no `SplitPath` vale para todo verbo que recebe caminho.
- **Dentro de `run --script` escreve-se `\\/`** — `\/` é escape válido de JSON e o parser o come
  antes de o argumento chegar ao CLI. Descoberto no smoke; está no CLAUDE.md.
- **`list-io-map` filtra `StartAddress == -1`** e conta em `unassigned`. O ET200SP sem cartão
  devolve 4 desses (interface, PROFINET interface, 2 portas do BA 2xRJ45); entravam no mapa como
  `%IB-1` e no `nextFreeByte` como `{Diagnosis: -1}`. Sumir calado é o defeito que os testes cegos
  mais cobraram, então vira contador, não silêncio.
- **Descartado: `import-ladder` convertendo `CALL`.** A R8 já foi destravada pelo `add-call`; a
  segunda rota duplicaria a parte cara (resolver tipo de pino, montar `Access`/`Wires`) e o
  `#local` como parâmetro fica fora do alcance das duas.
- **Limpeza de nomes de projeto de cliente: separar os dois casos** (ver Open).

## Next steps (ordered)
1. **FP-04 — caderno cego novo.** É o que mede o que esta sessão e a anterior construíram:
   `add-call`, `delete-network`, `set-retain`, `list-interface`, `clone --with-instances`, o guard
   de compile-e-confere, e agora os 4 checks do `audit` + `create-folder` com `\/`. Nenhum passou
   por uma rodada cega. Escrever o caderno numa sessão e **executar em outra** (o critério de
   condução que só a FP-03 respeitou). Se o caderno tiver um drive G120, fecha de quebra o caso
   real do `list-io-map`.
2. **Limpeza barata dos nomes de cliente** (10 min, working tree só — ver Open).
3. Depois: MCP em 2 tools, tradução do artigo para EN, postar (SIOS / r/PLC / LinkedIn).

## Key files
- `docs/BOAS-PRATICAS.md` §3 — a fila, agora com os 4 itens riscados e o motivo de cada um.
- `docs/teste-cego/criterios.md` — a régua (G1–G4 + I1–I4 + condução). É o molde do caderno FP-04.
- `docs/teste-cego/artigo.md` — o texto público; fonte da versão EN quando for a hora.
- `docs/DIARIO.md:163-177` — procedência dos fixtures e a lista exata do que ficou por sanitizar.
- `src/__navi__.md` — símbolos por arquivo, regenerado nesta sessão.
- `CHANGELOG.md` `[Unreleased]` — já descreve tudo desta sessão, inclusive o shape que mudou.

## Open / blockers
- **Nomes de projeto de cliente — decisão do user, dois casos diferentes:**
  - **Não é problema:** `preliminar`, `elevatória`, `desidratação`, `casa de motores`,
    `tanque de equalização`. São nomes de **etapa de processo de ETE** (vocabulário do domínio),
    não identificador de cliente. E os fixtures de `docs/examples/` **já foram sanitizados** em
    2026-07-28 (`CASA_DE_SOPRADORES` → `AREA_01`, `SOPRADOR_DESARENADOR_S-01A` → `MOTOR_S-01A` etc.,
    `DIARIO.md:165-172`). Limpar isso deixaria os cadernos piores — as plantas dos testes cegos são
    fictícias e é isso que se publica.
  - **É real e continua lá:** `Insular`, `ETE SG`, `AsBuilt` em prosa (`docs/PLANO.md`,
    `docs/PADRAO.md`, `docs/projeto-real-fase-A.md`, `library/README.md`, `scripts/raio-x.ps1`,
    `__navi__.md`), e `SOPRADOR_DESARENADOR (S-01A)` em `library/{library,export-all,generic}.json`.
  - **Recomendação:** fazer só a parte barata — `sed` no working tree para os 3 nomes de projeto em
    prosa. **Não reescrever histórico**: o repo é público desde 2026-07-20, então `.handoff/archive/`
    e os commits antigos já estão espelhados (forks, cache, índice), e `git-filter-repo` não
    despublica nada — só quebra os SHA que a release v1.0.0 carimba. E os nomes em `library/*.json`
    são **deliberados**: ancoram o `import-master-copy --force` (standing.md), renomear desincroniza
    4 arquivos + a `.al21`.
- `list-io-map` **não foi provado no caso que o motivou**: `LIB_TESTE` não tem cartão de I/O nem
  G120, então o verbo rodou com 0 endereços atribuídos. O endereço do telegrama de drive continua
  por confirmar — só um projeto com drive fecha o tropeço 2 da FP-01.
- Os 4 checks novos do `audit` só foram vistos **passando** (o projeto de referência é limpo).
  Nenhum foi visto reprovando contra um projeto que viole a regra — a FP-04 é onde isso aparece.

## Skills
- tia

## Effort
**Médio** para o passo 1 (FP-04). Escrever o caderno é redação com régua já pronta (`criterios.md`
é o molde) e a decisão de conduta já está tomada — o custo é caprichar no memorial fictício e
resistir a escrever um caderno que o CLI resolve fácil. Sobe para **alto** se a rodada for
executada nesta mesma linhagem de sessões: aí a escolha do que revelar ao executor vira o problema.
O passo 2 é **baixo** (um `sed` e reler o diff). Nada aqui é limitado por raciocínio quando envolve
o Portal — o relógio é dele, ~10-20 s por chamada `tia`, 2-4 min por `open-project`.
