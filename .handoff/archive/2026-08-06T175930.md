# Handoff · TIA Portal Openness API · 2026-08-06

## Goal
Fechar o ciclo da biblioteca contra o Portal: re-testar o fix do `--force` (bug do pacote
duplicado) numa CPU virgem e medir contra a régua conhecida — **4 erros, todos o G120 ausente**.
É a única espinha da ferramenta sem medição real.

## State
- HEAD: `fa68009`. Working tree limpo (só `.handoff/` desta rodada).
- Live state: **nenhum TIA Portal aberto**. Shell do agente na **sessão 0** (roteia pela task).
  `src/Tia.Lib/tia-cli/tia-cli.al21` = **148 KB assada** (10 master copies).
  `init.ps1 -Check` = **9/9 ok**. Skill `tia` instalada e ativa em `~/.claude/skills/tia`.
- Done: (a) documentação sincronizada com o disco — o handoff de 07-29 estava 11 min defasado do
  próprio dia e o trabalho perdido virou seção no `PLANO.md`; (b) `init.ps1` virou instalador
  completo (`-Check`, shim no PATH, skill); (c) skill `tia` criada, instalada e smoked.
- In progress: nada mid-flight. **Nenhuma chamada ao Portal foi feita nesta sessão** — só
  `--help` e `list-blocks --count` (sem Portal aberto, erro esperado).

## Decisions (and why)
- **A linha do tempo de 07-29 estava perdida** e agora está no `PLANO.md` ("Bake real da `.al21` +
  bug do `--force`"): bake assou a `.al21` 09:28; `install-lib` em `PLC_TESTE` 09:33 **falhou** com
  pacote duplicado (`1.5 Diagnóstico_1`, **34 → 68 blocos, compile Error**); fix em `Library.cs`
  09:35 + rebuild, commitado 08-03 como `a0df2f7 "updt"`. **O fix nunca rodou contra o Portal** —
  é o buraco que o passo 1 fecha. O bake saiu com **5** pacotes, não 6 (`1.1.1` vai dentro de `1.1`).
- **Skill = roteador fino, não cópia.** `skills/tia/SKILL.md` aponta pros docs do repo por
  `$env:TIA_CLI_HOME`. Copiar `VERBS.md`/`PLANO.md` pra dentro = duas fontes que divergem —
  descartado. As regras de segurança ficam no `CLAUDE.md` do repo, sempre ligadas.
- **Instalação = `init.ps1`, não um script novo.** Ele já era o bootstrap; ganhou gate 5 (shim
  `tia.cmd` no PATH + `TIA_CLI_HOME`) e gate 6 (copia a skill). `-Check` é read-only, 9 pontos,
  exit 1 se faltar. Escrever um `install.ps1` separado duplicaria os gates.
- **Bug achado testando a própria skill**: `init.ps1` grava env no perfil do **usuário** e
  processo já rodando não recebe — o shell persistente do agente via `$env:TIA_CLI_HOME` vazio
  com a instalação 9/9 ok, e a skill mandava reinstalar. Corrigido (`fa68009`): lê o escopo `User`
  direto.
- **Manifesto de plugin (`.claude-plugin/`) não foi feito** — daria `/plugin marketplace add`
  nativo com update/uninstall de graça, mas vira um segundo caminho de instalação. Aguarda ordem.
- **Symlink da skill descartado** (exige dev mode no Windows): cópia + `-Check` acusando
  divergência resolve.

## Next steps (ordered)
1. **Abrir o Portal com o `Base_tia_cli`** (`pwsh scripts/use-project.ps1 Base_tia_cli`, 2-4 min,
   background) e **re-rodar o ciclo numa CPU virgem**. Apagar o `PLC_TESTE` velho antes
   (`delete-device` — ficou com 68 blocos duplicados), depois
   `pwsh scripts/new-plc.ps1 PLC_TESTE "<pacotes>" -Apply`. Comparar com a régua: **4 erros, todos
   `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`**. Igual = ciclo validado.
   Diferente = o `--force` ainda está errado.
2. **`tia doctor` com o Portal aberto** — fecha a prova da skill: hoje "sem Portal" e "sessão
   errada" dão a mesma mensagem, e a rota da sessão 0 nunca foi exercitada nesta sessão.
3. **Assar UDT e tabela de tag na `.al21`** (`add-master-copy --name` no `bake-lib.ps1`) — mata a
   dependência do payload gitignored, que hoje impede um clone limpo de instalar a biblioteca.
4. Poda do `Base_tia_cli` (442 blocos fora da biblioteca; inventário em
   `workspace/base-inventory.json`). **Irreversível.**

## Key files
- `docs/PLANO.md` — F8 + seção da biblioteca: bake real, bug do `--force`, régua dos 4 erros, G120.
- `src/Tia.Core/Library.cs` — `ImportMasterCopy`: o fix de `--force` (apaga antes de criar).
- `scripts/init.ps1` — instalador; `-Check` nos 9 pontos. `skills/tia/SKILL.md` — a skill.
- `scripts/bake-lib.ps1` / `install-lib.ps1` / `new-plc.ps1` — o ciclo.
- `workspace/bake-lib.json` e `workspace/install-lib.json` — os batches exatos de 07-29.
- `docs/VERBS.md` (67 verbos) · `scripts/__navi__.md` · `src/__navi__.md` — regenerados hoje.

## Open / blockers
- **Telegrama do G120** parado no clique do user (device view do `SINAMICS G_ZERO`) — são os 4
  erros residuais da régua.
- **Quais dos 33 FBs são autorais** e podem virar `.scl`/`.xml` versionado? `SINA_SPEED_TLG20`
  (FB38003) é da Siemens.
- O repo remoto é `Codyte/TIA-Portal.git`, não `Tia-Portal-CLI.git` como o user citou — renomear
  no GitHub é 1 linha em 2 arquivos.
- 1 commit local não pushado antes desta sessão; agora são 4.
- Item 9 (online) barrado por D8.

## Skills
- tia
- ponytail
- caveman

## Effort
**Médio** para o passo 1 — a sequência está documentada e o dry mostra tudo antes de escrever, mas
é `--apply` real em CPU virgem. Raciocínio não é o gargalo: abrir o projeto leva 2-4 min e o
compile de projeto real leva minutos — juntar operação em `run --script` rende mais que pensar
mais. **Sobe pra alto** se a contagem divergir dos 4 erros da régua: aí o `--force` continua
errado e o `ImportMasterCopy` volta pra mesa.
