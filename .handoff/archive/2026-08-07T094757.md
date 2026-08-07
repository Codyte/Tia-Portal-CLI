# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Sessão foi manutenção de infra, não CLI: consertar o acesso remoto (VS Code Remote Tunnel) que não
conectava. **Resolvido.** O trabalho de produto continua sendo o ciclo da biblioteca (passo 1 abaixo).

## State
- HEAD: `4ab09f2` — **em sincronia com `origin/main`** (o push que estava bloqueado no handoff
  anterior foi feito; `git status -sb` = `## main...origin/main`, sem ahead/behind).
- `C:\Scripts\TIA Portal` (pasta velha, vazia) — **já apagada**. Pendência anterior fechada.
- Live state: nenhum TIA Portal aberto. Shell da sessão nasceu na **sessão 1** (RDP `rdp-tcp#17`,
  usuário `Carlos_Ortiz` ativo). Console é a sessão 3, sem usuário.
- Túnel VS Code no ar: nome `titanxnexus`, id `joyful-hill-nlx5dr5`, cluster `brs`.
  Acesso por `https://vscode.dev/tunnel/titanxnexus`. Dois processos `code-tunnel` na sessão 0
  (host + agent supervisor), um lock, log sem loop de erro.
- Done nesta sessão: diagnóstico e conserto do túnel (ver Decisions).
- In progress: nada mid-flight.

## Decisions (and why)
- **Causa do túnel não conectar: três lançadores concorrentes** apontando pro mesmo
  `--cli-data-dir C:\Users\Carlos_Ortiz\.vscode\cli` — Scheduled Task `VSCodeTunnel` (S4U, sessão 0),
  chave Run `HKCU\...\Run\Visual Studio Code Tunnel`, e o botão "Remote Tunnel Access" da janela do
  Code (`tunnel --name TITANxNEXUS --parent-process-id 5892`). Brigavam pelo singleton
  `tunnel-stable.lock`: log em loop 1x/s com
  `error access singleton, retrying: the process holding the singleton lock file (pid=14340) exited`
  e `tunnel status` devolvendo `{"tunnel":null,...}` — nenhum túnel registrado.
- **Ficou só a Scheduled Task `VSCodeTunnel`**; a chave Run foi removida. Motivo: a task é S4U
  ("rodar esteja o usuário logado ou não") = acesso remoto de verdade, enquanto a chave Run só sobe
  no logon do usuário. Efeito colateral cosmético: `code tunnel status` agora responde
  `service_installed:false` porque esse flag lê a chave Run, não a task — o log é a fonte de verdade.
- **A task estava `Ready` mas 3 processos da sessão 0 sobreviviam** segurando o lock — órfãos de uma
  execução anterior. `Stop-ScheduledTask` e `schtasks /End` não os mataram, e `Stop-Process` deu
  `Acesso negado`. Só morreram com `taskkill /F /IM code-tunnel.exe /T` elevado (1 UAC).
- **Uso declarado do acesso remoto = conversar com Claude e tocar os projetos**, não pilotar o TIA
  Portal. Por isso a sessão 0 do túnel não é problema no dia a dia.

## Next steps (ordered)
1. **Ciclo da biblioteca** (o que sobrou do handoff anterior, nada mudou): apagar o `PLC_TESTE` velho
   (68 blocos duplicados), rodar `new-plc.ps1 PLC_TESTE "<pacotes>" -Apply` numa CPU virgem e comparar
   com a régua — **4 erros, todos
   `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`** (telegrama do G120, parado no
   clique do user).
2. Decidir se o repo vira `tia-cli` no GitHub (hoje `Codyte/TIA-Portal`) — antes de outra máquina
   clonar. Se renomear: corrigir `~/.claude/skills/.gitmodules` + `git remote set-url`.

## Key files
- `~/.vscode/cli/tunnel-service.log` — fonte de verdade do túnel (`tunnel status` mente sobre
  `service_installed` desde a remoção da chave Run).
- `~/.vscode/cli/code_tunnel.json` — `{"name":"titanxnexus","id":"joyful-hill-nlx5dr5","cluster":"brs"}`.
- `scripts/__navi__.md` — mapa da pasta que o passo 1 toca.
- `docs/PLANO.md` § "Bake real da `.al21` + bug do `--force`" — contexto do passo 1.

## Open / blockers
- Telegrama do G120 (`Standard_telegram_20`) continua parado no clique do user — são os 4 erros da
  régua do passo 1.
- Renomear o repo no GitHub: não decidido.

## Skills
- tia
- ponytail
- caveman

## Effort
**Médio** para o passo 1: é rodar sequência documentada, mas comparar com a régua exige ler o diff de
erros com cuidado. Subir pra **alto** só se o `--force` continuar não sobrescrevendo depois do fix —
aí é comportamento de API contra a documentação, não falta de raciocínio. Passo 2 é decisão de
usuário, não trabalho.
