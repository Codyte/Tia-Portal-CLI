# Track 2 · Offline · biblioteca de blocos ("arsenal")
DONE 2026-07-28 — fatia 1 fechada, 4/4 passos entregues, commitado. Nada do Portal foi tocado
(zero chamadas `tia`, zero `rebuild.ps1`). **Não escrevi `active.md`** — o track 1 funde os dois.

Você é **100% offline: nunca chame `tia`, nunca rode `rebuild.ps1`**. O Portal pertence ao track 1
(Openness é single-session; duas chamadas simultâneas derrubam a dele, e rebuild troca o `tia.exe`
que ele está usando).

## Entregue

| arquivo | Git | o que é |
|---|---|---|
| `library/library.json` | sim | ex-`docs/examples/scaffold-padrao.json`, `Source: "blocks"` (relativo ao manifesto; `baseDir` = `Path.GetDirectoryName(manifest)`, `Program.cs:357`) |
| `library/export-all.json` | sim | batch inverso gerado do manifesto: 66 exports, verbo certo por tipo, `--out library/blocks`, 1 attach |
| `library/README.md` | sim | inventário dos 66 por pasta, o que cada gerador exige, como repor, como instalar, limitações |
| `library/blocks/` | **não** | ex-`workspace/padrao/`, 66 XMLs / 3,3 MB |

- `.gitignore`: `library/blocks/`. Clone recebe manifesto + README e repõe o próprio payload.
- PLANO **F4**: gate de publicação explícito (nenhum payload de cliente no repo público).
- PLANO **"Biblioteca de blocos"**: fatia 1 fechada + desenho do núcleo genérico (~10 itens,
  `.scl` × `.xml`, cada um linkado ao default do gerador que o exige) + gap real do `scaffold`.
- Repontados: `docs/PADRAO.md`, `README.md`, `__navi__.md` (raiz, `docs/examples/`, `scripts/`).
- **Removido** `scripts/export-fixtures.ps1` (cobria 15 dos 66) e a dependência do
  `workspace/export-padrao.json` (gitignored, caminho absoluto da máquina) — `export-all.json`
  substitui os dois.
- Verificado offline: 66/66 itens do manifesto resolvem em `library/blocks/`; 66 steps no batch com
  nomes conferidos 1:1 contra os arquivos.

## Correções ao briefing (não reintroduzir)

- O gap *"`scaffold` não ordena UDT antes de DB/FC"* (passo 4 do briefing) **não existe**:
  `Scaffold.Rank` sempre teve `SW.Types` = 0 (`src/Tia.Core/Scaffold.cs:58`), desde o commit do verbo.
- Gap real no lugar dele: **item UDT ignora `Folder`** — `src/Tia.Core/Scaffold.cs:126` importa todo
  `SW.Types.*` na raiz do `TypeGroup`, enquanto bloco e tabela resolvem caminho. Inofensivo hoje
  (13 UDTs com `"Folder": []`). Correção = `ResolveTypePath` análogo aos outros dois — **exige
  rebuild**, então é do track 1 ou de sessão única.

## Aberto

1. **Teste contra Portal** (fora do alcance deste track): `scaffold --manifest library/library.json`
   dry no projeto de referência → esperado 66/66 `skip (exists)`; `run --script
   library/export-all.json` → 66 arquivos de volta em `library/blocks/` (exige PLC compilado antes,
   bloco inconsistente não exporta).
2. **Fatia 2** — escrever os ~10 itens autorais do núcleo genérico (tabela no PLANO).
3. **Fatia 3** — utilitários genéricos (escala, debounce, first-out, watchdog, rampa).
4. **`docs/examples/*.xml` são fixtures de projeto real E estão versionados** — sanitizar
   (`clone --replace OLD=NEW`) ou trocar por sintéticas antes de o repo ficar visível de fato.

## Regras de empacotamento (já decididas, não rediscutir)
`.scl` é o padrão (texto diffável, SCL inteiro via `import-source`, imune à versão do Engineering;
limitação: bloco nasce na raiz, contorno = `export-block` → `import-block --folder` →
`delete-block`). `.xml` só pro que precisa nascer em LAD. `.al19` descartado (binário, não diffa).
`import-ladder` não serve pra escrever biblioteca (sem timer, sem aritmética).

## Território
- Escreveu: `library/**`, `.gitignore`, `README.md`, seção "Biblioteca de blocos" + linha F4 do
  `docs/PLANO.md`, `docs/PADRAO.md`, `scripts/export-fixtures.ps1` (removido), os `__navi__.md`,
  `.handoff/track2.md`.
- Não tocou: `src/**`, linha F8 do PLANO, `docs/examples/replicate-*.json`.
