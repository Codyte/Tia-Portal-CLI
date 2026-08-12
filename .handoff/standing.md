# Standing · TIA Portal Openness API

Restrições que continuam valendo em qualquer sessão. Regras de operação do CLI (dry/`--apply`,
sessão 0 × 1, compile entre etapas, nunca paralelizar) vivem no `CLAUDE.md` do repo — não repetir
aqui.

- **Nunca `git add -A`** — o repo carrega uma trilha paralela (`scripts/tia-help.py` e afins) que
  não deve entrar nos commits do CLI. Commitar sempre com caminhos explícitos.
- **Nada de payload de projeto de cliente no Git** (gate de publicação, F4). XML/AML exportado
  carrega nome de equipamento, tag e estrutura de DB. O que é versionado é autoral ou sanitizado;
  payload fica gitignored (`library/blocks/`, `workspace/`, `proj/`, `Scripts_Siemens/`).
- **Nome de pacote da biblioteca é estável** — é o que ancora o `import-master-copy --force` na
  atualização futura (a biblioteca vai ser atualizada a partir de outro projeto). Renomear pacote
  desincroniza `packages.json`, `generic.json`, `library.json`, `export-all.json` e a `.al21`.
- **Não renomear `FB_LIGA/DESLIGA MODO AUTO` (tem `/`) nem `FB FILTRO DE AMOSTRAGEM  ANALÍTICA`
  (espaço duplo)** — os nomes estão gravados em 9 arquivos + moldes. São bombas latentes, não bugs
  ativos. Tentado e descartado; o custo é maior que a estética.
- **O repo é a skill e mora em `~/.claude/skills/tia`** (submódulo de `Codyte/skills`, desde
  2026-08-06). `SKILL.md` na raiz, nada é copiado. **Um checkout só**: a whitelist do Openness é
  gravada por caminho do exe e a task `TiaSmokeRun` guarda o caminho absoluto do `taskrun.ps1`.
  Mover o checkout exige `pwsh scripts/init.ps1` de novo (re-registra a task e refaz a whitelist);
  sem isso a rota da sessão 0 devolve `No running TIA Portal instance found` com o Portal aberto.
  Os dois caminhos são o mesmo checkout: `~/.claude/skills` é **Junction** para `~/.agents/skills`
  (`~/.agents/skills/tia` é o diretório real). Ver dois caminhos não é clone duplicado.
- **Escrita no projeto-molde real (`proj/Software de ETE Insular_Inicial_V21`) só sem salvar.**
  Liberado pelo usuário em 2026-08-12 para a FP-05 rodar em projeto grande de verdade. O molde é a
  referência da casa e **não tem backup nesta máquina**: `save-project` e `close-project --save`
  estão proibidos enquanto a rodada durar. Fechar sem salvar é o único undo — e reverte tudo.
- **`--out-file` nunca em `$env:TEMP`** — vira caminho 8.3 (`CARLOS~1`) que o Python não abre.
  Usar `workspace/`.
- **Script que recebe caminho testa contra o cwd E contra a raiz do repo** — o macro roda num pwsh
  filho que não nasce na raiz, e o relativo cai calado no ramo errado (`use-project.ps1`, 2026-08-10).
