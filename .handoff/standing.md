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
- **A skill em `~/.claude/skills/tia` é uma cópia.** A fonte é `skills/tia/SKILL.md` no repo —
  editar lá e rodar `pwsh scripts/init.ps1` pra propagar (gate 6). Editar o instalado direto
  perde na próxima instalação e o `-Check` acusa divergência.
- **`--out-file` nunca em `$env:TEMP`** — vira caminho 8.3 (`CARLOS~1`) que o Python não abre.
  Usar `workspace/`.
