# Handoff · TIA Portal Openness API · 2026-08-11

## Goal
Distribuição do repo (F9) fechada: release v1.0.0 publicada, CI verde, portas de entrada e mascote
no ar. O que sobra é escolher a próxima frente — nada pendente da F9.

## State
- HEAD: `f1a3c31`+ (o mascote foi seguido de 2 commits de ajuste da imagem). Working tree limpo
  fora deste handoff.
- Live state: **TIA Portal aberto** (2 processos, sessão 1) com `LIB_TESTE`. O `tia.exe` local foi
  compilado a partir do commit **da tag v1.0.0** (`c1435df`), não do HEAD — foi assim de propósito,
  para o binário publicado declarar a procedência certa. Consequência: o próximo `pack.ps1` vai
  **recusar** empacotar até rodar `rebuild.ps1`. É a guarda funcionando, não erro.
  O shell desta sessão nasceu na **sessão 0** (roteia pela task `TiaSmokeRun`).
- Done (F9, tudo commitado e pushado):
  - `tia --version` (versão + qual Openness o exe carrega); resolver virou `SiemensProbeDirs()`.
  - `src/Directory.Build.props` — versão única dos 3 projetos, hoje `1.0.0`.
  - `scripts/pack.ps1` — zip de release; entra só o que `git ls-files` lista; aborta se DLL da
    Siemens aparecer; recusa se o commit carimbado no exe ≠ HEAD.
  - `init.ps1` detecta instalação de release (sem fonte) e pula os gates de build;
    `rebuild.ps1 -WhitelistOnly` é o passo que sobra.
  - `.github/workflows/ci.yml` + templates de issue/PR; CHANGELOG, CONTRIBUTING, SECURITY.
  - Release **v1.0.0** publicada e **verificada por download** (sha bate, exe carimba `c1435df`).
  - README: badges, hero novo (tarefa ponta a ponta + teste cego), instalação por zip, mascote.
  - `docs/assets/` — mascot.png (512), mascot-avatar.png (400), favicon.png (32), fundo transparente.
- In progress: nada.

## Decisions (and why)
- **CI não builda C# e nunca vai buildar.** As assemblies do Openness são licenciadas e não existem
  em runner nenhum. O workflow verifica o que dá sem a Siemens (parse dos scripts, JSON, versão com
  entrada no CHANGELOG, guarda de licença/privacidade). Escrito no topo do workflow, no CONTRIBUTING
  e no README — badge honesto vale mais que badge que mente.
- **SemVer é sobre o contrato do CLI** (nome de verbo, flag, shape do JSON, exit code), não sobre o
  código interno. Critério: script de terceiro quebra? MAJOR.
- **Zip de release preserva o layout do repo** (`scripts/` + `src/Tia.Cli/bin/Debug/net48/`) porque
  whitelist, shim e init derivam tudo de `$PSScriptRoot` — assim o zip extraído se comporta como um
  checkout, sem nenhum caminho especial.
- **Asset da v1.0.0 foi recortado do próprio commit da tag** (checkout detached → rebuild → pack →
  volta pra main → `gh release upload --clobber`). Não movi a tag. A alternativa (mover a tag pro
  HEAD) foi descartada: tag publicada é fato histórico.
- **Mascote: fundo e contorno branco keyed out por canal mínimo alto.** Creme `(250,249,241)` e
  branco puro têm os 3 canais altos; nenhuma cor do personagem chega perto (menta ~140 de mínimo,
  laranja ~60). Gradiente "até o branco" foi descartado: só resolveria no tema claro.
- **Contagem de verbos foi 76 → 77** porque `version` entra no help. SKILL.md e README ajustados.
- Descartado por ora: MCP server, GIF de demo, tradução de `docs/` para EN, verificação V19/V20.

## Next steps (ordered)
1. Escolher a frente. Candidatos, com o que cada um compra:
   - **MCP server** fino sobre os mesmos verbos — é o canal de descoberta em 2026 (listas de MCP do
     Claude/Cursor/Copilot) e é onde o concorrente `totally-integrated-claude` já está. Note que a
     **D1 do PLANO diz "CLI primeiro, MCP depois (talvez nunca)"** e a F5 está `⬜ só se D1 cair` —
     fazer isso é **reabrir a D1**, com motivo novo (distribuição), não contrariá-la em silêncio.
   - **Artigo do teste cego** — publicar `docs/teste-cego/` como texto ("um agente escreveu programa
     de PLC ponta a ponta; aqui está a régua e os 10 tropeços"). É o ativo que nenhum concorrente do
     nicho tem. Move mais agulha que verbo novo.
   - **FP-04** (novo caderno cego) com `add-call`/`delete-network`/`set-retain`/`list-interface` na
     mão — mede se a R8 deixou de custar sessão.
   - **SVG do mascote** (vetor, poucos KB) — precisa de trace manual, não sai por script.
2. Se for qualquer coisa que empacote release: `pwsh scripts/rebuild.ps1` **antes** (ver Live state).
3. Postar em SIOS / r/PLC / LinkedIn é decisão do user — é o que de fato leva o repo até a Siemens.

## Key files
- `docs/PLANO.md` — tabela de fases; F9 é a última linha, com o diagnóstico que a abriu.
- `CHANGELOG.md` — seção `[Unreleased]` é onde entra o que for landing agora.
- `scripts/pack.ps1` — o fluxo de release inteiro; ler antes de publicar qualquer versão.
- `CONTRIBUTING.md` — a restrição do CI está na abertura, é o que responde 80% das dúvidas de PR.
- `README.md:1-60` — hero + badges; `docs/assets/` — os 3 PNGs do mascote.
- `src/__navi__.md` — símbolos por arquivo (não regenerado nesta sessão: só `Program.cs` mudou,
  com `SiemensProbeDirs()` novo).

## Open / blockers
- Nenhum bloqueio.
- `rebuild.ps1` mudou o hash do `tia.exe` várias vezes hoje com o Portal aberto: a próxima chamada
  `tia` pode pendurar num diálogo modal de autorização na tela (alguém precisa clicar).
- `scripts/navi-cs.ps1` vale rodar quando alguém mexer em C# de novo.

## Skills
- tia

## Effort
**Baixo a médio** para o passo 1 — é uma escolha de frente, não um problema técnico. Se a escolha
for o **MCP server**, sobe para **alto**: é reabrir a D1 e desenhar superfície nova (JSON-RPC sobre
stdio em net48, mapeamento verbo→tool, e sem cliente de teste no repo hoje). Se for o **artigo** ou
a **FP-04**, médio — o material já existe, o custo é redação e execução. Nada aqui é limitado por
raciocínio quando envolve o Portal: o relógio é dele, ~10-20 s por chamada `tia`.
