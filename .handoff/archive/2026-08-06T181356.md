# Handoff · TIA Portal Openness API · 2026-08-06

## Goal
**Migrar o repo pra dentro de `~/.claude/skills/tia`, como submódulo** — aprovado pelo user.
O repo inteiro vira a skill. Depois disso, retomar o ciclo da biblioteca (re-testar o fix do
`--force` numa CPU virgem contra a régua dos 4 erros do G120).

## State
- HEAD: `d6bfbe7` + o commit deste ajuste. **~7 commits à frente do `origin/main`, NÃO pushados —
  bloqueio real, ver Open**. O submódulo do passo 3 clona do remote: sem push, ele nasce em
  `a0df2f7` e perde tudo o que foi feito hoje.
- Live state: nenhum TIA Portal aberto; shell do agente na sessão 0. `init.ps1 -Check` = 9/9 ok.
  `.al21` de 148 KB assada. `~/.claude/skills/tia` existe hoje como **cópia untracked** (feita
  pelo gate 6) dentro do repo `Codyte/skills` — é ela que o submódulo substitui.
- Done: docs sincronizados com o disco; `init.ps1` virou instalador completo (`-Check`, PATH,
  skill); skill `tia` criada, instalada e smoked; plano de migração aprovado.
- In progress: nada mid-flight. Nenhuma chamada ao Portal nesta sessão.

## Decisions (and why)
- **Migração = submódulo, não cópia.** `~/.claude/skills/` **já é o repo `Codyte/skills`** com 4
  submódulos (`navindex`, `caveman`, `handoff`, `ponytail`), cada um seu repo. A `tia` entra igual.
  A cópia de hoje está lá como pasta untracked — fora do padrão e divergindo em silêncio.
- **Os números aprovam**: repo *tracked* = **1,36 MB / 159 arquivos**; zero caminho fixo
  `C:\Scripts` no código (tudo sai de `$PSScriptRoot`). O pesado é untracked e não viaja:
  `proj/` = **1,9 GB**, `workspace/` 36 MB, `src/Tia.Lib` 8,1 MB.
- **Um checkout só.** A whitelist do Openness é gravada **por caminho do exe** — dois checkouts =
  dois `tia.exe` = whitelist brigando. Por isso é mover, não clonar ao lado.
- **Trap medida, não deduzida**: a task `TiaSmokeRun` aponta pra
  `c:\Scripts\TIA Portal\scripts\taskrun.ps1` — **caminho absoluto**. Mover o repo **quebra a rota
  da sessão 0** até re-registrar as tasks (1 UAC). É o passo mais fácil de esquecer.
- **Teto do padrão** (dito ao user): "tudo vira skill" funciona enquanto o *tracked* for pequeno e
  a instalação couber num script. Se um projeto futuro versionar payload pesado, volta o padrão de
  hoje — skill fina + repo separado, ligados por `TIA_CLI_HOME`.

## Next steps (ordered)
1. **`SKILL.md` pra raiz do repo** (`git mv skills/tia/SKILL.md SKILL.md`, apaga `skills/`) — o
   Claude Code lê `~/.claude/skills/<nome>/SKILL.md`, então a raiz do repo tem que ser a skill.
   Ajustar `$skillSrc`/gate 6 do `init.ps1`, o link no `README.md` e o `CLAUDE.md`.
2. **`init.ps1`: gate 6 deixa de copiar.** Vira verificação — o repo *é* a skill; avisa se a raiz
   não for `~/.claude/skills/tia`. E **gate 4 passa a re-registrar a task quando o caminho gravado
   nela diverge do repo atual** (`(Get-ScheduledTask TiaSmokeRun).Actions.Arguments`), senão a
   migração deixa a sessão 0 morta. Commit + push.
3. **Push primeiro** (o user autentica — ver Open), conferir `git status -sb` limpo contra
   `origin/main`. **Submódulo**: em `~/.claude/skills/`, `Remove-Item tia -Recurse` (é cópia untracked, nada a
   perder) → `git submodule add https://github.com/Codyte/TIA-Portal.git tia` → clona os 1,36 MB
   limpos. Commit no repo `Codyte/skills`.
4. **Mover só o untracked** de `c:\Scripts\TIA Portal` pro clone novo: `lib/`, `workspace/`,
   `src/Tia.Lib/`, `library/blocks/`, `Scripts_Siemens/`, `bin/`+`obj/` (ou deixa o rebuild
   refazer). **`proj/` (1,9 GB): decidir** — checar antes se algum script resolve caminho de
   projeto por `proj/` (`use-project.ps1` parece resolver por nome via Portal); se não resolver,
   pode ficar onde está.
5. **`pwsh scripts/init.ps1` no destino novo** → refaz whitelist (caminho do exe mudou), PATH,
   `TIA_CLI_HOME`, tasks. Depois `-Check` tem que dar 9/9 e `tia --help` tem que voltar pela rota
   da task. **Só então** apagar `c:\Scripts\TIA Portal` (irreversível — confirmar com o user).
6. **Retirar a entrada do `standing.md`** que diz "a skill é uma cópia" — deixa de valer.
7. Só então voltar ao ciclo da biblioteca: apagar o `PLC_TESTE` velho (68 blocos duplicados),
   `new-plc.ps1 PLC_TESTE "<pacotes>" -Apply` numa CPU virgem, comparar com a régua — **4 erros,
   todos `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`**.

## Key files
- `scripts/init.ps1` — gates 1-6 + `-Check`; `scripts/setup-tasks.ps1` — registra as tasks com
  caminho absoluto (o que quebra na mudança); `scripts/_common.ps1` — rota da sessão 0.
- `skills/tia/SKILL.md` — vira `SKILL.md` na raiz no passo 1.
- `~/.claude/skills/.gitmodules` — o padrão a seguir (4 submódulos).
- `docs/PLANO.md` — seção "Bake real da `.al21` + bug do `--force`" (o passo 7).
- `.handoff/` é versionado → viaja no clone; por isso o push antes de migrar.

## Open / blockers
- **BLOQUEIO: o agente não consegue pushar.** `credential.helper=manager` exige TTY/GUI e o shell
  do agente não tem (`/dev/tty: No such device`), então o push **pendura pra sempre**; `gh auth
  status` diz `The token in default is invalid`. Leitura do remote funciona (anônima). **O user
  precisa rodar `gh auth login -h github.com` (ou `git push` num terminal dele) antes do passo 3.**
  Não insistir no `git push` daqui sem isso — trava o turno.
- **`proj/` = 1,9 GB** vai ou fica? Só decidir depois de conferir a resolução de caminho.
- Repo remoto é `Codyte/TIA-Portal.git`; o user citou `Tia-Portal-CLI.git`. Renomear no GitHub é
  1 linha em 2 arquivos — decidir antes do `submodule add`, que grava a URL.
- Telegrama do G120 parado no clique do user — são os 4 erros da régua do passo 7.

## Skills
- tia
- ponytail
- caveman

## Effort
**Médio** para o passo 1-2 — é edição mecânica, mas mexe no instalador que os 3 gates dependem, e
um erro só aparece depois da mudança (quando não dá mais pra voltar barato). **Alto no passo 5**
se o `-Check` não fechar 9/9 no destino: aí é whitelist ou task apontando pro caminho velho, e o
sintoma (`No running TIA Portal instance found`) é idêntico ao de "não tem Portal aberto".
Raciocínio não é o gargalo em nenhum deles — é UAC, rebuild e mover arquivo.
