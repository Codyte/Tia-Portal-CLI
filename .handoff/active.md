# Handoff · TIA Portal Openness API · 2026-08-06

## Goal
**Migração do repo para skill: FEITA.** O repo agora *é* a skill `tia` e mora em
`~/.claude/skills/tia`, como submódulo de `Codyte/skills`. Resta o push (bloqueado por
autenticação) e o ciclo da biblioteca.

## State
- HEAD: `f5209ea` — **9 commits à frente do `origin/main`, NÃO pushados** (bloqueio, ver Open).
- **O repo mudou de lugar**: `c:\Scripts\TIA Portal` → `C:\Users\Carlos_Ortiz\.claude\skills\tia`.
  Abrir sessão nova já nesse diretório. A pasta antiga ficou **vazia** (não deu pra apagar: era o
  cwd do shell desta sessão) — apagar depois com `Remove-Item "C:\Scripts\TIA Portal"`.
- Live state: nenhum TIA Portal aberto; shell da sessão nasceu na **sessão 1**.
  `init.ps1 -Check` = **9/9 ok** no caminho novo (whitelist refeita, tasks re-registradas,
  `TIA_CLI_HOME` e PATH apontando pro novo, entrada velha do PATH removida).
  `proj/` (1,82 GB), `workspace/`, `lib/`, `src/Tia.Lib`, `library/blocks/`, `Scripts_Siemens/`
  vieram junto — todos gitignored.
- Done: `SKILL.md` na raiz (`skills/` morreu); `init.ps1` gate 6 = verificação, gate 4 re-registra
  task quando o caminho diverge; README/CLAUDE/SKILL/PLANO atualizados; submódulo registrado e
  commitado em `Codyte/skills` (`b5b8fd5`); `standing.md` atualizado.
- In progress: nada mid-flight.

## Decisions (and why)
- **Mover o checkout em vez de clonar do remote.** O plano previa `git submodule add` clonando —
  mas com o push bloqueado o clone nasceria em `a0df2f7` e perderia 9 commits. Mover preserva
  commits + payload untracked numa operação, não apaga nada e é reversível.
  `git submodule add <url> tia` com o diretório já existente responde
  `Adding existing repo at 'tia' to the index` — registra sem clonar.
- **`Move-Item` da pasta inteira falha** (`being used by another process`): o shell do agente tem
  cwd lá e o harness reseta o cwd a cada chamada. Mover os **filhos** um a um funciona (17/17) e
  deixa só o diretório vazio pra trás.
- **`proj/` (1,82 GB) foi junto.** Move no mesmo volume é rename, custo zero, e mantém qualquer
  resolução relativa de caminho intacta. Se incomodar em `~/.claude`, é `Move-Item` de novo.
- **URL do submódulo = `https://github.com/Codyte/TIA-Portal.git`** (o remote real). Se renomear
  pro `tia-cli` no GitHub, corrigir em `~/.claude/skills/.gitmodules` + `git remote set-url`.
- **`totally-integrated-claude` (Czarnak): só registrado, não avaliado** — decisão do user.
  Está em `docs/PLANO.md` § Pendências. Não foi lido nem clonado.

## Next steps (ordered)
1. **User roda `gh auth login -h github.com`** (ou `git push` num terminal dele). Depois:
   `git -C "$HOME\.claude\skills\tia" push origin main` e o push do repo `Codyte/skills`.
   Enquanto não pushar, o gitlink do submódulo aponta pra commit que só existe nesta máquina.
2. `Remove-Item "C:\Scripts\TIA Portal"` (vazia) de uma sessão cujo cwd não seja ela.
3. Voltar ao ciclo da biblioteca: apagar o `PLC_TESTE` velho (68 blocos duplicados),
   `new-plc.ps1 PLC_TESTE "<pacotes>" -Apply` numa CPU virgem, comparar com a régua —
   **4 erros, todos `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`**.

## Key files
- `scripts/init.ps1` — `Test-SkillInstalled` (repo == skill) e `Test-TasksCurrent` (caminho da
  task) são os dois helpers novos; gates 4 e 6 mudaram.
- `docs/PLANO.md` § "Migração do repo para skill" e § "Bake real da `.al21` + bug do `--force`".
- `~/.claude/skills/.gitmodules` — 5 submódulos agora.
- `scripts/__navi__.md` — mapa da pasta que o passo 3 toca.

## Open / blockers
- **Push bloqueado**: `gh auth status` = `The token in default is invalid`; `git push` morre com
  `Cannot prompt because user interactivity has been disabled` (falha rápido, não pendura).
  Só o user destrava.
- Renomear o repo no GitHub (`TIA-Portal` → `tia-cli`?) — decidir antes de outra máquina clonar.
- Telegrama do G120 parado no clique do user — são os 4 erros da régua do passo 3.

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo** para os passos 1-2 — é autenticação e apagar pasta vazia; raciocínio não é o gargalo.
**Médio** no passo 3: é rodar sequência documentada, mas a comparação com a régua exige ler o
diff de erros com cuidado. Subir pra alto só se o `--force` continuar não sobrescrevendo depois
do fix — aí é comportamento de API contra a documentação.
