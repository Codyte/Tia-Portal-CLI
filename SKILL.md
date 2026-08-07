---
name: tia
description: >-
  Dirigir o TIA Portal (Siemens) pela linha de comando via Openness — CLI `tia`, 67 verbos com
  JSON na entrada e na saída: ler projeto, exportar/importar bloco, tags, hardware, compilar,
  replicar FC de acionamento/alarme/instrumento, instalar biblioteca de blocos num PLC.
  Use sempre que a conversa envolver TIA Portal, Openness, PLC S7-1500, bloco FB/FC/OB/DB, UDT,
  tabela de tag, projeto .ap21/.al21 — e também quando o user pedir para instalar, atualizar ou
  verificar o tia-cli numa máquina. Traz o protocolo de instalação e as invariantes que, ignoradas,
  custam uma sessão inteira.
---

# tia — CLI do TIA Portal Openness

## 1. Achar o CLI

Nesta ordem, pare no primeiro que responder:

```powershell
$env:TIA_CLI_HOME                                          # variável de usuário, gravada pelo init
[Environment]::GetEnvironmentVariable('TIA_CLI_HOME','User')   # shell velho não vê a de cima
Get-Command tia -ErrorAction SilentlyContinue              # shim tia.cmd no PATH
```

Achou → `$repo = ...`. Não achou → seção 2.

**A 2ª linha não é redundância.** `init.ps1` grava a variável no perfil do usuário, e processo já
rodando **não recebe** — o shell persistente do agente (e qualquer terminal aberto antes da
instalação) enxerga `$env:TIA_CLI_HOME` vazio e `tia` fora do PATH, com tudo instalado
corretamente. Ler o escopo `User` direto resolve sem reiniciar nada; chamar pelo caminho completo
(`& "$repo\scripts\tia.cmd" <verbo>`) sempre funciona.

## 2. Instalar numa máquina nova

Este repo **é** a skill: o checkout tem que ficar em `~/.claude/skills/tia`, como submódulo do
repo de skills.

```powershell
cd "$HOME\.claude\skills"
git submodule add https://github.com/Codyte/Tia-Portal-CLI.git tia   # ou git clone, se não for repo
pwsh "$HOME\.claude\skills\tia\scripts\init.ps1"
```

`init.ps1` é idempotente e faz tudo: confere os gates que só um humano resolve (grupo Windows
`Siemens TIA Openness` + logoff/logon, .NET SDK 8, TIA Portal instalado), copia as DLLs do
Openness da instalação local, registra as tasks (**1 UAC**), builda, whitelista o `tia.exe` e
põe o shim no PATH. Rodar de novo depois de `git pull` — reinstala o que mudou e não mexe no resto.

**Um checkout só.** A whitelist do Openness é gravada por caminho do exe e a task `TiaSmokeRun`
guarda o caminho absoluto do `taskrun.ps1`: dois clones brigam pela whitelist, e mover o clone
mata a rota da sessão 0 até rodar `init.ps1` de novo (ele detecta e re-registra a task).

**Verificar o que está instalado** (read-only, não escreve nada):

```powershell
pwsh "$env:TIA_CLI_HOME\scripts\init.ps1" -Check
```

Sai a lista dos 9 pontos (grupo, dotnet, Portal, `lib/`, `tia.exe`, whitelist, tasks, PATH,
lugar do checkout) + o estado vivo (sessão do shell, Portal rodando, `.al21` presente). Exit 1 se faltar algo.

O que **não** vem no clone (gitignored, cada máquina repõe o seu): `lib/*.dll` (licença Siemens,
o `init` copia da instalação local), `library/blocks/` e `src/Tia.Lib/*.al21` (payload de projeto
de cliente — assar com `bake-lib.ps1` a partir de um projeto que já tenha a biblioteca).

## 3. Chamar

**Sempre pelo `tia.ps1`/`tia.cmd`, nunca `tia.exe` direto.** O shim roteia por sessão do Windows:
se o shell nasceu na sessão 0 (isolada de serviços), o Openness não enxerga o Portal da sessão 1 e
todo attach devolve `No running TIA Portal instance found` — é fronteira do SO, não configuração.
`Invoke-Tia` esconde isso passando pela task `TiaSmokeRun`.

```powershell
tia doctor                                    # preflight, se o shim está no PATH
pwsh "$env:TIA_CLI_HOME\scripts\tia.ps1" tree --plc "CPU1"      # de qualquer diretório
```

## 4. Invariantes (ignorar custa sessão)

- **Verbo de escrita é dry por padrão.** `--apply` explícito. Nunca contra projeto de produção.
- **Uma chamada por vez.** Openness é single-session; nada de paralelizar `tia` (nem via agentes).
- **Compile entre etapas.** Todo import deixa o alvo inconsistente e o Openness recusa exportar
  bloco inconsistente — `compile --apply` antes de `clone`, `diff-block`, `explain-block` e dos
  4 geradores.
- **Mais de um Portal aberto** → todo verbo exige `--portal <projeto|PID>`.
- **`rebuild.ps1` muda o hash do `tia.exe`** → o Portal já aberto abre um **diálogo modal de
  autorização** na tela. Chamada pendurada com CPU ~0 = alguém precisa clicar; não é bug de API.

## 5. Orçamento de contexto (o CLI devolve volume que estoura sessão)

- **Orientação em projeto novo = `tia tree`** → `plc-navi.md` (39 KB para 476 blocos), e só isso.
  Depois vem verbo que responde pergunta: `trace`, `xref`, `explain-block`, `find --pattern`.
- **`--out-file F.json`** em qualquer verbo de leitura: o JSON completo vai pro arquivo, stdout
  devolve `{file,bytes,count,head}`. Sem isso, `find --pattern "*" --kind tag` num projeto real
  são 821 KB no contexto.
- **Nunca `list-blocks` sem filtro** (~480 blocos): use `--folder`, `--type` ou `--count`.
- **`run --script ops.json --summary`** para lote: 1 attach (~3 s) em vez de um por chamada.

## 6. Referência (ler no repo, não deduzir)

| Preciso de | Arquivo |
|---|---|
| assinatura dos 67 verbos | `$env:TIA_CLI_HOME\docs\VERBS.md` (~90 linhas, gerado do help) |
| decisões, fases, o que já foi medido | `$env:TIA_CLI_HOME\docs\PLANO.md` |
| regras de operação do repo | `$env:TIA_CLI_HOME\CLAUDE.md` |
| macros de fluxo | `$env:TIA_CLI_HOME\scripts\` (`prep-project`, `raio-x`, `install-lib`, `bake-lib`) |
| como a API Openness se comporta | `python "$env:TIA_CLI_HOME\scripts\tia-help.py" --search "termo"` — 1083 tópicos da ajuda oficial do F1. **Usar antes de sondar por tentativa e erro.** |
