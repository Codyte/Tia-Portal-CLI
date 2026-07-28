# TIA Portal Openness API — instruções do repo

**Toda sessão: ler `docs/PLANO.md` (decisões + fase atual) e `__navi__.md` antes de qualquer coisa.**

Mapas de navegação: `__navi__.md` na raiz (árvore do repo) e por pasta. O de `src/` é
`src/__navi__.md` — símbolos públicos de cada `.cs` + os `case "verbo"` do CLI com linha;
`navindex.py` não lê C#, então regenerar com `pwsh scripts/navi-cs.ps1` após refatorar.

## Regras duras

- Decisões D1–D9 do PLANO valem — não rediscutir sem motivo novo.
- `Scripts_Siemens/FINAIS/` = referência read-only. `Scripts_Siemens/OLD/` = não tocar.
- Verbos de escrita: dry-run por padrão, `--apply` explícito.
- **Compile entre etapas**: todo import deixa o alvo (e quem o referencia) inconsistente, e o
  Openness recusa exportar bloco inconsistente. `clone`, `diff-block`, `explain-block` e os 4
  geradores exportam por baixo — sem `compile --apply` antes, quebram com essa mensagem.
- Nunca rodar `tia` em paralelo (Openness single-session).
- Nunca commitar `Siemens.Engineering.dll`.
- Testes só contra projeto TIA de teste, nunca produção.

## Build / run (a partir da F1)

- Solução em `src/`, target net48 x64. Binário oficial = Debug (`src\Tia.Cli\bin\Debug\net48\tia.exe`).
- **Máquina nova: `pwsh scripts/init.ps1`** = gates (grupo `Siemens TIA Openness`, .NET SDK,
  `lib/*.dll` copiadas da instalação local do Portal) + tasks (1 UAC) + rebuild. Idempotente.
  Scripts não têm caminho nem usuário fixo — tudo sai de `$PSScriptRoot`/`$env:USERNAME`, e a versão
  do Portal (V19–V21) é descoberta em runtime.
- **Macro-verbos — usar SEMPRE em vez da coreografia manual:**
  - `pwsh scripts/tia.ps1 <verbo> [args]` = chamar o CLI de qualquer sessão (ver seção abaixo).
  - `pwsh scripts/rebuild.ps1` = build + testes offline + whitelist (UAC só se tia.exe mudou).
    Nunca rodar dotnet build/whitelist/testes soltos.
  - `pwsh scripts/use-project.ps1 <Nome|caminho.ap21> [-Save]` = garante projeto aberto
    (no-op se já aberto; fecha o atual sem save por padrão; open leva 2-4 min → background).
  - `pwsh scripts/prep-project.ps1 <Nome> [-Apply]` = use-project + doctor (+ compile --apply +
    save só com `-Apply`; projeto real chega sem compilar — rodar antes de qualquer export).
  - `pwsh scripts/raio-x.ps1 <Nome>` = banho read-only → `workspace/<proj>/` (doctor, snapshot,
    devices, tags, types, plc-navi.md, AML, xref dos OBs).
  - `pwsh scripts/clone-hw.ps1 <Origem> <Destino> [-Apply]` = copia hardware via CAx/AML.
  - `tia run --script ops.json` = batch de verbos, attach 1x (~7s por chamada solta). Fluxo FINAIS
    completo em dry: `tia run --script docs/examples/gen-all.json`.
    **Isola steps**: step que falha vira `{ok:false,error,type}` e o batch segue; `exit 1` se algum
    falhou. Bateria onde falha é esperada roda de uma vez só.
  - **Assinatura de verbo → `docs/VERBS.md`** (gerado do help pelo `rebuild.ps1`). Uma leitura de
    ~80 linhas em vez de grep em `Program.cs`.
  - **Nunca `list-blocks` sem filtro** — são ~480 blocos. `--folder A/B` (pega subpastas),
    `--type FB|FC|OB|GlobalDB|InstanceDB`, `--count` (só o total por pasta, ~10 linhas).
  - **`move-block --name X | --pattern P* --folder A/B [--apply]`** — o Openness não move bloco; o
    verbo faz `export` (de todos primeiro) → `delete` → `import --folder`. Fazer isso na mão custa
    3 chamadas por bloco e falha se a ordem inverter.
  - **`run --script ops.json --summary`** = `{steps,failed,errors[]}` em vez do resultado de cada
    step (98 steps × JSON completo é dump de contexto).
  - **`--out-file F.json` em verbo de leitura** (`find`/`snapshot`/`list-*`/`xref`/`trace`): JSON
    completo no arquivo, stdout devolve só `{file,bytes,count,head}`. `find --pattern "*" --kind tag`
    num projeto real = 821 KB (~200k tokens) — sem a opção, isso cai no contexto inteiro.
  - **Orientação num projeto novo = `tia tree` → `plc-navi.md`, e só isso.** Outline do PLC inteiro
    (blocos + tabelas de tag + UDTs) agrupado por pasta: 39 KB / 309 linhas p/ 476 blocos + 194
    tabelas + 13 UDTs, contra ~150 KB do JSON equivalente, em 4s. Depois vem verbo que responde
    pergunta (`trace`, `xref`, `explain-block`, `find --pattern`). `snapshot` (251 KB) e
    `find --kind tag` (821 KB) são volume bruto: sempre `--out-file` + grep, nunca leitura direta.
  - `tia doctor` = preflight dos 6 verbos antes de qualquer smoke.
- Smoke test exige TIA Portal aberto com projeto de teste — confirmar com o usuário antes.

## Não sabe como a API se comporta? Consulte a ajuda oficial, não deduza

`python scripts/tia-help.py --search "termo"` → busca nos **45518 tópicos** da ajuda do TIA Portal
(a mesma do F1), dos quais **1083 são de Openness**; `--topic "PKG/TOC/ID.htm"` devolve o texto
limpo. Sobe o serviço e monta o índice sozinho na 1ª vez (`--ensure`).

**Use antes de sondar a API por tentativa e erro** — nome de atributo, o que o import recusa,
diferença entre famílias de CPU, assinatura de instrução. O custo é ~1 s e uns poucos KB; o de
descobrir no braço foi metade de uma sessão. Busca casa por **AND de palavras no título** (o índice
não tem corpo): termo que só existe no texto dá 0 hits — achar o tópico plausível e ler com
`--topic`.

## Sessão 0 × sessão 1 (por que `tia` às vezes não roda direto)

`pwsh scripts/tia.ps1 <args>` é **o comando único** — resolve isso sozinho, use sempre.

Se o shell nascer na **sessão 0** do Windows (isolada de serviços, `UserInteractive=False`), TIA
Portal e desktop vivem na **sessão 1** e `TiaPortal.GetProcesses()` não enxerga processo de outra
sessão: `Attach()` devolve `"No running TIA Portal instance found"` mesmo com o portal na tela.
É fronteira do SO, não configuração; `--no-ui` não resolve (só troca o erro de modo pelo de
whitelist). O shell do agente **pode** nascer na sessão 1 (VSCode na sessão do usuário) — daí
tudo roda direto; checar com `(Get-Process -Id $PID).SessionId`.

`Invoke-Tia` (`scripts/_common.ps1`, dot-sourced por todos os macros) roteia: sessão ≠ 0 invoca
`tia.exe` direto; sessão 0 passa pela task `TiaSmokeRun` (`LogonType Interactive` = sessão 1).
Caller não vê diferença — `$LASTEXITCODE` e stdout/stderr valem nas duas rotas.
`TIA_VIA_TASK=1` força a rota da task (é como se testa esse ramo da sessão 1);
`TIA_TIMEOUT` = segundos (default 600).

Protocolo da task, se precisar na unha: `workspace/taskio/cmd.json` recebe
`{"id":"<run>","args":[...]}` (ou array cru `["doctor"]`, forma legada) →
`Start-ScheduledTask -TaskName TiaSmokeRun` → poll de `exit-<run>.txt`; saída em
`out-<run>.txt` / `err-<run>.txt` (stdout e stderr **separados**; sem `id`, os nomes são
`out.txt`/`err.txt`/`exit.txt`). Nome único por rodada é obrigatório: verbo que inicia o portal
deixa o handle do arquivo de saída herdado e aberto enquanto o portal viver.

O runner é `scripts/taskrun.ps1`. Não exige janela interativa aberta pelo user
(`scripts/smokeloop.ps1` é rota alternativa, mesmo protocolo, útil só pra ver a saída ao vivo).
O portal só morre junto com a task se tiver sido *iniciado por ela* (fica na árvore de processos);
portal aberto à mão pelo user sobrevive.

Whitelist stale = `EngineeringSecurityException`. Refazer com
`Start-ScheduledTask -TaskName TiaWhitelist` (SYSTEM, sem UAC); `rebuild.ps1` já compara contra
o hash gravado no registro e falha alto se continuar divergente.

## Economia de tokens

- Sem spawn de agentes por padrão (repo pequeno; navi resolve). Sem workflows.
- `/handoff` + `/clear` no fim de cada fase ou >~150k de contexto.
- Atualizar tabela de fases do PLANO ao encerrar sessão de trabalho.
