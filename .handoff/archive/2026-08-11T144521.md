# Handoff · TIA Portal Openness API · 2026-08-11 (2)

## Goal
Sessão de higiene, não de CLI: nome de projeto de cliente saiu da prosa e a navegação de C# passou
a ser gerada pelo `navindex`. A frente de trabalho continua sendo a **FP-04** (caderno cego novo),
intocada.

## State
- HEAD: `f2adeba`, pushado. Working tree limpo. `tia.exe` em dia (`rebuild.ps1` rodou e deu
  **ALL PASS**, whitelist refeita pela task).
- Live state: **TIA Portal aberto** (sessão 1) com `LIB_TESTE` desde a sessão anterior; nada foi
  tocado nele nesta sessão. O shell desta sessão nasceu na sessão 0 (rota da task). Atenção: o
  `rebuild.ps1` mudou o hash do `tia.exe`, então o **próximo attach nesse Portal já aberto pode
  abrir o diálogo modal de autorização** — chamada pendurada com CPU ~0 é isso, não bug.
- Done:
  - **`c85809a` — nome de cliente fora da prosa.** As duas plantas de referência passaram a ter
    nome pelo papel: `Software de ETE Modelo_Inicial_V21` (molde da casa) e
    `Automação ETE Campo AsBuilt_1_V21` (as-built de campo). Registro em `docs/DIARIO.md`
    (§Procedência) e no CHANGELOG.
  - **`f2adeba` — `scripts/navi-cs.ps1` morreu.** O `navindex` (repo `Codyte/navindex`, HEAD
    `78ac7ce`) passou a indexar C# e a pular caminho gitignored. Um comando regenera tudo:
    `python ~/.claude/skills/navindex/scripts/navindex.py <pasta|.>` da raiz.
- In progress: nada.

## Decisions (and why)
- **Histórico não foi reescrito.** O repo é público desde 2026-07-20: `.handoff/archive/` e os
  commits antigos já estão espelhados, `git-filter-repo` não despublica nada e quebraria os SHA
  que a release v1.0.0 carimba. `library/*.json` também ficou como está — o nome do objeto ancora
  o `import-master-copy --force`.
- **Nome fictício pelo papel, não sigla nova.** "Modelo" e "Campo" dizem o que cada projeto é
  dentro da doc; o resíduo aceito é que comando copiado da prosa não casa com o `.ap21` no disco
  de quem tem a cópia real.
- **A lista dos 71 `case "verbo"` mudou de lugar**: agora é o header NAV INDEX no topo de
  `src/Tia.Cli/Program.cs`. O mapa da pasta mostra só os 24 primeiros símbolos — teto do
  `navindex`, e subi-lo foi descartado lá (o header já tem tudo).
- **Descartado: banir `proj`/`workspace` no `SKIP_DIRS` do navindex.** Nome genérico demais para
  ferramenta global; a regra certa é o `.gitignore`, e é o que foi implementado.

## Next steps (ordered)
1. **FP-04 — caderno cego novo** (era o passo 1 do handoff anterior e segue intacto). Mede o que
   nunca passou por rodada cega: `add-call`, `delete-network`, `set-retain`, `list-interface`,
   `clone --with-instances`, o guard de compile-e-confere, os 4 checks novos do `audit` e
   `create-folder` com `\/`. Escrever numa sessão e **executar em outra**. Com um drive G120 no
   caderno, fecha de quebra o caso real do `list-io-map`.
2. Depois: MCP em 2 tools, tradução do artigo para EN, postar (SIOS / r/PLC / LinkedIn).

## Key files
- `docs/teste-cego/criterios.md` — a régua (G1–G4 + I1–I4 + condução); é o molde do caderno FP-04.
- `docs/BOAS-PRATICAS.md` §3 — fila fechada, com o motivo de cada item.
- `src/Tia.Cli/__navi__.md` e `src/Tia.Core/__navi__.md` — mapas por pasta (substituíram
  `src/__navi__.md`); a lista de verbos com linha está no header do `Program.cs`.
- `docs/DIARIO.md` §Procedência — o que foi sanitizado, o mapeamento dos nomes e o que ficou de fora.

## Open / blockers
- `list-io-map` **ainda não foi provado no caso que o motivou**: `LIB_TESTE` não tem cartão de I/O
  nem G120. O endereço do telegrama de drive continua por confirmar.
- Os 4 checks novos do `audit` só foram vistos **passando** — nenhum foi visto reprovando contra
  projeto que viole a regra. A FP-04 é onde isso aparece.
- Regenerar mapa agora é `navindex` puro, mas quem tiver checkout velho do repo de skills precisa
  do `navindex` ≥ `4ad0dfc` — versão antiga não lê C# e apaga os mapas de `src/*/`.

## Skills
- tia

## Effort
**Médio** para o passo 1 (FP-04): escrever o caderno é redação com régua pronta e decisão de
conduta já tomada; o custo é caprichar no memorial fictício e resistir a escrever um caderno que o
CLI resolve fácil. Sobe para **alto** se a rodada for executada nesta mesma linhagem de sessões —
aí escolher o que revelar ao executor vira o problema. Nada aqui é limitado por raciocínio quando
envolve o Portal: o relógio é dele (~10-20 s por chamada, 2-4 min por `open-project`).
