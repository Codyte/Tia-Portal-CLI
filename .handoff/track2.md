# Track 2 · Offline · biblioteca de blocos ("arsenal")

Você é **100% offline: nunca chame `tia`, nunca rode `rebuild.ps1`**. O Portal pertence ao track 1
(Openness é single-session; duas chamadas simultâneas derrubam a dele, e rebuild troca o `tia.exe`
que ele está usando). Leia `.handoff/active.md` antes; não leia `track1.md`.

## Objetivo
A biblioteca já existe e já instala — `docs/examples/scaffold-padrao.json` são 20 pastas, 6 pastas
de tag e **66 itens**, aplicáveis por `tia scaffold --manifest ... --apply`. Dois defeitos a fechar:

1. **O payload está fora do Git.** O manifesto aponta `Source: ../../workspace/padrao`, e
   `workspace/` está no `.gitignore` — 66 XMLs, 3,3 MB, só nesta máquina. Num clone, `scaffold`
   quebra por arquivo ausente.
2. **O repo é público** (`github.com/Codyte/TIA-Portal`, `isPrivate: false`) e o payload inclui
   `DB GLOBAL.xml` com **869 KB** da planta real do cliente (equipamentos, tags, estrutura).
   Commitar isso publica IP de engenharia de cliente — irreversível na prática (fork, cache, índice).

## Next steps
1. **`library/` na raiz**, payload local, manifesto versionado:
   - mover `workspace/padrao/*.xml` → `library/blocks/`;
   - `library/library.json` = cópia do `scaffold-padrao.json` com `Source` apontando p/ `blocks`;
   - `.gitignore`: ignorar **`library/blocks/`** (payload), **manter versionados**
     `library/library.json` e `library/README.md`. Um clone recebe manifesto + instruções e
     fornece o próprio payload.
   - `library/README.md`: o que é, por que o payload não viaja, como repor (exportar de um projeto
     padrão), como instalar (`tia scaffold --manifest library/library.json --apply`).
2. **Gate de publicação no PLANO (F4)**: linha explícita — *nenhum payload de projeto de cliente
   entra no repo público*; o que for publicado tem que ser autoral ou sanitizado.
3. **Doc do núcleo genérico** (só o desenho, não escrever os blocos ainda): o mínimo que deixa
   `doctor` verde são ~10 itens — 4 moldes (OB de erro de módulo, FC modelo de alarmes, OB molde
   de alarmes, molde de instrumento), FB `BITS TO WORD`, 3 UDTs (`MotorDados`, `MotorPrincipal`,
   `ValvDados`), **`DB GLOBAL` esqueleto** (não os 869 KB do cliente) e a árvore de pastas.
   Anotar quais dá pra escrever em `.scl` e quais precisam ser `.xml` (LAD legível).
   Ampliar a seção "Biblioteca de blocos" que já existe no `docs/PLANO.md`.
4. **Gap conhecido do `scaffold`**, documentar (não corrigir — mexer em `src/` exige rebuild, que é
   proibido pra você): a ordem de import é tabela → FC → OB, falta o degrau **UDT antes de DB/FC**.
   Deixar anotado como item de backlog com o arquivo/linha.

## Regras de empacotamento (já decididas, não rediscutir)
`.scl` é o padrão (texto diffável, SCL inteiro via `import-source`, imune à versão do Engineering;
limitação: bloco nasce na raiz, contorno = `export-block` → `import-block --folder` →
`delete-block`). `.xml` só pro que precisa nascer em LAD. `.al19` descartado (binário, não diffa).
`import-ladder` não serve pra escrever biblioteca (sem timer, sem aritmética).

## Seu território
- Escreve: `library/**`, `.gitignore`, `README.md`, seção "Biblioteca de blocos" + linha F4 do
  `docs/PLANO.md`, `.handoff/track2.md`.
- **Não toca**: `src/**`, `workspace/` (fora mover o `padrao/`), linha F8 do PLANO,
  `docs/examples/replicate-*.json`.
- Commit com caminhos explícitos, nunca `git add -A`. Antes de editar o PLANO, reler o arquivo —
  o track 1 também escreve nele (em outra seção).
