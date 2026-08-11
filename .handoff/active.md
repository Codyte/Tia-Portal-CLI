# Handoff · TIA Portal Openness API · 2026-08-11

## Goal
Fechar as duas pontas soltas da fila da FP-04 (itens 8 e `plugAs`) e decidir se existe FP-05. As
duas rodadas cegas (FP-03, FP-04) já entregaram programa conforme e viraram fila implementada —
o que sobrou é sonda de API, não construção de programa.

## State
- HEAD: `76e36ab`, working tree limpo. **Esta sessão não deixou commit novo** — o trabalho da
  FP-03 (agitador `AG-05`) está em `993aace` e a fila que saiu dela em `80de94a`; a FP-04 e sua
  fila em `90d392b`/`59e58ac`/`76e36ab`.
- Live state: **TIA Portal aberto (sessão 1) com o projeto-molde real**
  `proj/Software de ETE Insular_Inicial_V21` (62 devices, PLC `CPU1.0 CCO`). É a **referência
  read-only da casa** — nenhum verbo de escrita contra ele. Para testar, `use-project.ps1` troca
  para `workspace/newlib/LIB_TESTE` (abrir custa 2-4 min).
- Done: fila da FP-04 = 6 de 8 itens, com aceite ao vivo. `audit` com 10 checks. 78 verbos.
- In progress: nada em vôo.

## Decisions (and why)
- **`list-io-map` não serve para endereço de telegrama de drive** — com G120 + telegrama 20 + IO
  system conectado, `--device <drive>` devolve `{addresses: 0}` e os itens caem em `unassigned`.
  Quem precisa do telegrama no programa usa o `HWID` da constante
  `<drive>~PROFINET_interface~Standard_telegram_20`. Já está no `CLAUDE.md`; falta só decidir se
  isso é a resposta final (item 8) ou se o verbo vai atrás do endereço.
- **Retentividade se declara no FB, nunca no iDB** — o Openness recusa `Remanence` em instância
  (`The attribute 'Remanence' cannot be set`) e o `import-source` não expressa retentividade. Foi o
  que motivou o `set-retain`, já entregue.
- **Chamada em LAD por verbo, não por FlgNet na mão** — a FP-03 provou que dava para escrever a rede
  no XML, e provou também que custa 250 linhas de Python por rede. `add-call` + `delete-network`
  substituíram isso.

## Next steps (ordered)
1. **Decidir o item 8** (`list-io-map` × telegrama de drive): ou o verbo passa a alcançar o
   endereço, ou a decisão é "não serve para drive" e o `CLAUDE.md` já é a resposta final.
   Investigar custa uma sessão de sondagem com o Portal; nenhum programa depende disso.
2. **Provar o `plugAs`** do `plug-module` num slot que aceite plug de verdade — ou marcar a sonda
   como descartável se ninguém tropeçar nela de novo.
3. Se houver apetite de FP-05: a rodada tem de mirar o que FP-03 e FP-04 **não** cobriram — os
   4 checks novos do `audit` seguem sem ter sido vistos **reprovando**.
4. Trilha paralela (não bloqueia nada): MCP em 2 tools, tradução do artigo para EN, publicação.

## Key files
- `docs/PLANO.md` — decisões, fases, e a fila de cada rodada cega.
- `docs/teste-cego/resultado-FP-03.md` e `resultado-FP-04.md` — os tropeços medidos e a fila que
  saiu de cada um (é onde estão os itens 8 e `plugAs`).
- `docs/BOAS-PRATICAS.md` — R1–R9, o aceite de qualquer programa novo.
- `docs/VERBS.md` — assinatura dos 78 verbos (gerado pelo `rebuild.ps1`).
- `src/Tia.Cli/Program.cs` — o header NAV INDEX no topo lista todos os `case "verbo"`.
- `src/Tia.Core/__navi__.md` — mapa da pasta, antes de qualquer busca ampla.

## Open / blockers
- Item 8 (`list-io-map` × telegrama de drive): aberto, é decisão de escopo, não bug.
- Sonda `plugAs`: implementada, sem prova ao vivo.
- Os 4 checks novos do `audit` nunca foram vistos reprovando — duas rodadas cegas passaram limpo.

## Skills
- tia

## Effort
**Médio** para o passo 1 — é sondagem de API contra o Portal, e a resposta pode ser "não dá".
Suba para **alto** se o `--sdk`/ajuda oficial disserem que o endereço do telegrama existe em
algum lugar e o `list-io-map` continuar não achando (aí é bug de varredura, não limite do
Openness). O gargalo é o relógio do Portal (10-20 s por chamada), não o raciocínio: mais
pensamento não acelera o laço.
