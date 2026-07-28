# `library/` — biblioteca de blocos instalável ("arsenal")

Árvore de pastas da lei de nomenclatura + os moldes que os geradores (`gen-alarm-fc`,
`gen-fault-ob`, `gen-instrument-fc`, `gen-startup-ob`) exigem, num manifesto único que
`scaffold` aplica num projeto vazio.

| arquivo | vai pro Git? | o que é |
|---|---|---|
| `library.json` | **sim** | manifesto `ScaffoldManifest`: 20 pastas de bloco, 6 de tag, 66 itens |
| `README.md` | **sim** | este arquivo |
| `blocks/*.xml` | **não** (`.gitignore`) | payload: os 66 XMLs exportados |

## Por que o payload não viaja no repo

Os XMLs de `blocks/` saíram de um projeto real de cliente — nomes de equipamento, tags,
estrutura de DB (`DB GLOBAL.xml` sozinho tem 869 KB da planta). Este repo é **público**
(`github.com/Codyte/TIA-Portal`), e publicar isso é irreversível na prática (fork, cache,
índice de busca). Regra do PLANO (F4): *nenhum payload de projeto de cliente entra no repo
público; o que for publicado tem que ser autoral ou sanitizado*.

Consequência aceita: num clone, `blocks/` chega vazio e `scaffold` falha com
`Scaffold item not found: ...` — o manifesto é a receita, o payload é local.

## Como repor o payload

Com o projeto de referência aberto no Portal (`Software de ETE Insular_Inicial_V21`):

```powershell
pwsh scripts/tia.ps1 --script-ps1 scripts\export-fixtures.ps1   # 13 blocos + 2 tabelas → library/blocks/
```

Isso cobre o núcleo (moldes + o acionamento Soprador 1). O resto dos 66 foi exportado item a
item; para um que falte, o nome está no `library.json` e o verbo é
`tia export-block --name "<nome>" --out library/blocks` (tabela de tag: `export-tags --table`,
UDT: `export-type --name`).

## Como instalar num projeto

```powershell
pwsh scripts/tia.ps1 scaffold --manifest library/library.json            # dry: lista o que criaria
pwsh scripts/tia.ps1 scaffold --manifest library/library.json --apply
pwsh scripts/tia.ps1 compile --apply
```

`Source` é relativo ao próprio manifesto, então `library.json` + `blocks/` podem ser copiados
juntos pra qualquer lugar. Idempotente: item que já existe sai `skip (exists)` (`--force`
sobrescreve). A ordem de import é por tipo — UDT → tabela de tag → FB → DB → iDB → FC → OB
([`Scaffold.Rank`](../src/Tia.Core/Scaffold.cs#L58)) — porque bloco só importa limpo depois do
que ele referencia.

**Limitação conhecida**: `Folder` de item UDT é ignorado — `Scaffold.Run` importa todo
`SW.Types.*` na raiz do `TypeGroup` ([`Scaffold.cs:126`](../src/Tia.Core/Scaffold.cs#L126)),
enquanto bloco e tabela respeitam o caminho. Sem impacto hoje (os 13 UDTs do manifesto já são
`"Folder": []`); vira bug no dia em que a biblioteca quiser UDT em subpasta.

## Empacotamento (decisão fechada 2026-07-28)

`.scl` é o padrão para bloco novo autoral (texto diffável, SCL inteiro via `import-source`,
imune à versão do Engineering; limitação: nasce na raiz, contorno = `export-block` →
`import-block --folder` → `delete-block`). `.xml` só pro que precisa nascer em LAD legível —
é o caso de todo o payload atual, que veio de export. `.al19` descartado (binário, não diffa).
`import-ladder` não serve pra escrever biblioteca (sem timer, sem aritmética).

Desenho do núcleo genérico (o que seria autoral e publicável) na seção **"Biblioteca de
blocos"** de [`docs/PLANO.md`](../docs/PLANO.md).
