# Handoff · TIA Portal Openness API · 2026-08-11 (3)

## Goal
Sessão de ferramenta, não de CLI: o extrator do `navindex` deixou de indexar `case` de dado como
destino de dispatch, e os mapas do repo foram regenerados. A frente de trabalho continua sendo a
**FP-04** (caderno cego novo), intocada — é o próximo passo, detalhado abaixo.

## State
- HEAD: `1e0bc11`, pushado. Working tree limpo. `navindex` também pushado (`2e113ec` em
  `Codyte/navindex`).
- Live state: **TIA Portal aberto** (sessão 1) com `LIB_TESTE`, herdado de duas sessões atrás;
  nada foi tocado nele nesta sessão nem na anterior. Nenhum verbo do `tia` rodou aqui.
  **`tia.exe` está defasado do fonte**: 3 `.cs` mudaram (só comentário de header NAV INDEX) e
  ninguém rodou `rebuild.ps1`. O primeiro rebuild vai mudar o hash do exe e **abrir o diálogo
  modal de autorização** no Portal já aberto — chamada pendurada com CPU ~0 é isso.
- Done:
  - **`Codyte/navindex` `2e113ec`** — `case "..."` só indexa se o literal for todo minúsculo.
    Verbo de CLI é lowercase por convenção (`list-blocks`); switch sobre dado carrega maiúscula.
    `CACHE_VER` 5 → 6 (senão o cache serve símbolo velho e a mudança parece não ter efeito).
    Teste atualizado, passa.
  - **`1e0bc11`** — mapas e headers do tia regenerados. 25 entradas de ruído fora
    (`case "Coil"`, `"BOOL"`, `"BYTE"`, `"LEITURA_*"`); 71 verbos do `Program.cs` e 6 do
    `Doctor.cs` intactos. `BlockExplain.cs` agora cabe inteiro no mapa da pasta e
    `Standardize.cs` recuperou 2 símbolos que o ruído empurrava para fora do corte de 24.
- In progress: nada.

## Decisions (and why)
- **Nada de call graph / "quem chama quem" no navindex.** Medido neste repo: 399 métodos,
  354 nomes únicos — só 8,8% de nome repetido, então `grep "Nome("` já é resposta exata em 91%
  dos casos, e nos 8,8% ambíguos (`Run` declarado 9x) regex também falha, porque exige resolver
  o tipo do receptor. Protótipo de grafo devolveu uma frase ("`Ops` é chamado por 16 arquivos,
  resto ≤4") e marcou **36 de 60 classes como dead code, 100% falso positivo** — são configs/DTOs
  instanciados com `new`, não chamados por `Classe.Metodo(`. Índice que afirma código vivo morto
  é pior que não ter. Se navegação doer de verdade um dia, o degrau é LSP via MCP (Serena), não
  mais markdown gerado.
- **Filtro lowercase é aposta em convenção, não parse** — assumido. Repo que despacha em
  PascalCase (`case "OrderPlaced":`, event routing) perde esses rótulos do índice. Degradação
  suave: omite, não aponta linha errada; método e tipo não são tocados. Fix de 1 linha registrado
  em nota `ponytail:` no próprio `navindex.py`.
- **Descartado: mapa para `docs/teste-cego/`.** Sai bom com `--threshold 150`, mas o comando padrão
  da raiz **apaga** o mapa na regeneração seguinte — só sobrevive trocando o comando documentado do
  repo. Ganho marginal (nomes já auto-descritivos no root tree) contra instruções divergentes.
- **Banner de seção não é ruído.** `L92:lookup`, `L281:structure` vêm de `// ----- lookup -----`,
  são deliberados e marcam região do arquivo. Só o `case` de dado era ruído. Constante UPPER em
  Python (`DEFAULT_BASE`) é jump target legítimo — não mexido.

## Next steps (ordered)
1. **FP-04 — escrever o caderno cego novo.** É o passo 1 há três handoffs; nada dele foi feito.
   - **O que ele tem que medir** (superfície que nunca passou por rodada cega): `add-call`,
     `delete-network`, `set-retain`, `list-interface`, `clone --with-instances`, o guard de
     compile-e-confere dos verbos que editam bloco por XML, os **4 checks novos do `audit`** e
     `create-folder` com `\/` no nome de pasta.
   - **Como se escreve**: régua pronta em `docs/teste-cego/criterios.md` (G1–G4 + I1–I4 +
     condução); molde de redação nos cadernos FP-01/02/03. É memorial descritivo fictício de
     planta — o executor recebe o caderno e mais nada.
   - **A disciplina que custa**: escrever numa sessão e **executar em outra**. E resistir a
     escrever um caderno que o CLI já resolve fácil — o valor da rodada cega está no que ela
     reprova.
   - **De quebra**: se o caderno pedir um drive G120, fecha o caso real do `list-io-map`
     (endereço do telegrama), que segue sem prova.
2. Depois: MCP em 2 tools, tradução do artigo para EN, postar (SIOS / r/PLC / LinkedIn).

## Key files
- `docs/teste-cego/criterios.md` (55 ln) — a régua. Ler primeiro.
- `docs/teste-cego/caderno-FP-03.md` (88 ln) — o molde mais enxuto; FP-02 (255 ln) é o mais completo.
- `docs/teste-cego/resultado-FP-03.md` — o formato do que a execução devolve.
- `docs/__navi__.md` e o root `__navi__.md` — orientação em 1 read; `docs/teste-cego/` não tem mapa
  por decisão (acima), o root tree lista os 8 arquivos com contagem de linha.
- `docs/BOAS-PRATICAS.md` (R1–R9) — é o que o caderno cobra do executor.

## Open / blockers
- `list-io-map` **ainda não foi provado no caso que o motivou**: `LIB_TESTE` não tem cartão de I/O
  nem G120. Endereço do telegrama de drive continua por confirmar.
- Os 4 checks novos do `audit` só foram vistos **passando** — nenhum foi visto reprovando contra
  projeto que viole a regra. A FP-04 é onde isso aparece.
- `tia.exe` defasado do fonte (só comentário) — rodar `pwsh scripts/rebuild.ps1` antes de qualquer
  smoke, e contar com o diálogo modal de autorização no Portal aberto.

## Skills
- tia

## Effort
**Médio** para o passo 1. É redação com régua pronta e decisão de conduta já tomada — o custo não é
raciocínio, é caprichar no memorial fictício e não facilitar para o executor. Sobe para **alto** se
a rodada for executada nesta mesma linhagem de sessões: aí escolher o que revelar ao executor vira o
problema, e o vazamento é invisível. Nada aqui é limitado por raciocínio assim que o Portal entra:
o relógio é dele (~10-20 s por chamada, 2-4 min por `open-project`).
