# Handoff · TIA Portal Openness API · 2026-08-06

## Goal
Fechar o ciclo da biblioteca contra o Portal: re-testar o fix do `--force` (bug do pacote
duplicado) numa CPU virgem e medir contra a régua conhecida — **4 erros, todos o G120 ausente**.
Em paralelo, decidir se o modo de operação do `tia` vira uma skill de usuário.

## State
- HEAD: `a0df2f7`. **Working tree sujo de propósito**: docs atualizados nesta sessão, ainda não
  commitados — `docs/PLANO.md`, `.handoff/standing.md` (novo), `__navi__.md` + 16 mapas de pasta,
  `src/__navi__.md`.
- Live state: **nenhum TIA Portal aberto**. Shell do agente nasceu na **sessão 0** (rota da task;
  `scripts/tia.ps1` resolve sozinho). `src/Tia.Lib/tia-cli/tia-cli.al21` = **148 KB assada**
  (10 master copies), backup em `src/Tia.Lib/tia-cli.backup/`.
- Done nesta sessão: levantamento do estado real (o handoff anterior estava 11 min defasado do
  próprio dia) + sincronização da documentação com o disco.
- In progress: nada mid-flight. Nenhuma chamada ao Portal foi feita.

## Decisions (and why)
- **A linha do tempo de 07-29 estava perdida** — handoff escrito 09:24, trabalho seguiu até 09:35:
  `bake-lib -Apply` (09:28) assou a `.al21`; `install-lib` em `PLC_TESTE` (09:33) **falhou** com
  pacote duplicado (`1.5 Diagnóstico_1`, **34 → 68 blocos, compile Error**); fix em `Library.cs`
  (09:35) + rebuild, commitado 08-03 como `a0df2f7 "updt"`. Agora está em `docs/PLANO.md` →
  "Bake real da `.al21` + bug do `--force`", com o buraco nomeado: **o fix nunca rodou contra o
  Portal**.
- **O bake saiu com 5 pacotes, não 6** (`1.1`, `1.3`, `1.4`, `1.5`, `1.6` — `1.1.1 Inversores` vai
  dentro de `1.1`). O handoff anterior dizia 6; corrigido.
- **Passo "UDT/tabela como master copy" continua não feito** — `bake-lib.ps1` não tem uma linha de
  type/tag, e `packages.json` aponta pra `library/blocks/<UDT>.xml` + `library/tags/`, que são
  payload **gitignored**. Clone limpo não instala a biblioteca. `add-master-copy --name` já sabe
  fazer (o import roteia por `ContentType`); falta o macro chamar.
- **Skill do `tia`: viável e barata, mas só como roteador fino** (~40 linhas em
  `~/.claude/skills/tia/SKILL.md`), apontando pros docs do repo por caminho absoluto. Ganho real =
  **portabilidade** (usar o CLI de outro diretório/projeto, onde o `CLAUDE.md` deste repo não
  carrega), não economia de token. Copiar `VERBS.md`/`PLANO.md` pra dentro da skill = duas fontes
  de verdade que divergem — descartado. As regras de segurança (dry/`--apply`, D9, sessão 0)
  continuam no `CLAUDE.md` do repo, sempre ligadas. **Não construída ainda — aguarda o ok.**
- **`standing.md` criado** só com o que não está no `CLAUDE.md`/`PLANO.md` (5 restrições).

## Next steps (ordered)
1. **Commitar os docs** com caminhos explícitos (nunca `git add -A`): `docs/PLANO.md`,
   `.handoff/`, `__navi__.md`, `src/__navi__.md` e os mapas de pasta.
2. **Abrir o Portal com o `Base_tia_cli`** (`pwsh scripts/use-project.ps1 Base_tia_cli`, 2-4 min,
   background) e **re-rodar o ciclo** numa CPU virgem:
   `pwsh scripts/new-plc.ps1 PLC_TESTE "<pacotes>" -Apply` (ou `install-lib.ps1` num PLC criado na
   hora). Comparar com a régua: **4 erros, todos
   `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`**. Igual = ciclo validado.
   Diferente = o `--force` ainda está errado. Apagar o `PLC_TESTE` velho antes (`delete-device`) —
   ele ficou com 68 blocos duplicados.
3. **Assar UDT e tabela de tag na `.al21`** (`add-master-copy --name` no `bake-lib.ps1`) — mata a
   dependência do payload gitignored.
4. **Skill do `tia`** se o user aprovar o formato roteador.
5. Só então a poda do `Base_tia_cli` (442 blocos fora da biblioteca; inventário em
   `workspace/base-inventory.json`). **Irreversível.**

## Key files
- `docs/PLANO.md` — F8 (tabela de fases) + seção "Biblioteca de blocos": bake real, bug do
  `--force`, régua dos 4 erros, G120.
- `src/Tia.Core/Library.cs` — `ImportMasterCopy`: o fix de `--force` (apaga antes de criar).
- `scripts/bake-lib.ps1` / `scripts/install-lib.ps1` / `scripts/new-plc.ps1` — o ciclo.
- `library/packages.json` — `requires`/`db`/`tags`/`types`/`instances` por master copy.
- `workspace/bake-lib.json` e `workspace/install-lib.json` — os batches exatos que rodaram em
  07-29 (modelo reusável).
- `docs/VERBS.md` — 67 verbos, uma leitura em vez de grep no `Program.cs`.
- `src/__navi__.md` e `__navi__.md` — regenerados hoje.

## Open / blockers
- **Telegrama do G120** segue parado no clique do user (device view do `SINAMICS G_ZERO`) — são os
  4 erros residuais da régua.
- **Quais dos 33 FBs são autorais** e podem virar `.scl`/`.xml` versionado? Decide se o arsenal
  viaja no Git ou só na `.al21` local. `SINA_SPEED_TLG20` (FB38003) é da Siemens.
- `rebuild.ps1` pode derrubar a 1ª chamada seguinte (`Openness access (0033:000666)` — diálogo
  modal na tela; só o clique resolve).
- Item 9 (online) segue barrado por D8.

## Skills
- ponytail
- caveman

## Effort
**Baixo** para o passo 1 (commit mecânico) e **médio** para o passo 2 — a sequência está
documentada e o dry mostra tudo antes de escrever, mas é `--apply` real em CPU virgem. O gargalo
não é raciocínio: é abrir o projeto (2-4 min) e o compile. **Sobe pra alto** se a contagem de erros
divergir dos 4 da régua — aí o `--force` continua errado e o `ImportMasterCopy` volta pra mesa.
