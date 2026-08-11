# Handoff · TIA Portal Openness API · 2026-08-11 (5)

## Goal
A rodada cega FP-04 foi executada (por outra sessão) e a fila que ela gerou foi atacada: 6 dos 8
itens entregues, com aceite ao vivo. Sobram 2 itens de decisão e a pergunta de sempre — próxima
rodada cega ou empacotar (MCP, artigo).

## State
- HEAD: `59e58ac`, pushado, working tree limpo (só o arquivo de archive deste handoff pendente).
- Live state: **TIA Portal aberto** (sessão 1) com `LIB_TESTE`. Foi **escrito** nesta sessão: uma
  chamada de FC e uma de FB entraram no `CHAMADA_AREA_03_SOPRADORES_AERACAO` e foram removidas, e
  um `iDB TESTE BOOL` foi criado e apagado. `compile --plc PLC_ZERO` fecha em **0 erros / 1 warning**
  (o warning é o de I/O sem hardware, anterior). Projeto **não salvo** — nada obriga a salvar.
  `tia.exe` está em dia com o fonte (`rebuild.ps1` rodou 4x); o diálogo modal de autorização do
  Openness **não apareceu** em nenhuma delas.
- Done:
  - **`ba13dce`** — resultado da FP-04 registrado no PLANO (fila de 8 itens) e 3 promessas do
    `CLAUDE.md` corrigidas.
  - **`59e58ac`** — itens 1–7 da fila. `add-call` chama FC (`--inst` opcional, exigido para FB e
    recusado para FC); constante sai tipada pelo pino; `connect-subnet` dry devolve
    `ownedIoSystem`/`ioSystemsOnSubnet`/`ioSystemAction`; `ImportAndProve` não devolve mais
    `ok:false` depois de ter aplicado; `list-blocks --folder` honra `\/`; `plug-module --type` sem
    dump de `freeSlots` + sonda de sufixo de firmware (`plugAs`); help do `clone --replace`.
    7 casos offline novos, `ALL PASS`.
- In progress: nada.

## Decisions (and why)
- **Bug novo achado no aceite ao vivo, não no teste:** o `add-call` declarava todos os pinos e
  ligava só os com valor; o Portal recusa `<Parameter>` sem fio ("The connection with the name '12'
  is not connected to the object with the UID '32'"). Em `Call` de export real vale
  `wires == parâmetros + 1` (o `en`). FP-03 e FP-04 não pegaram porque só chamaram bloco com todos
  os pinos preenchidos — **o offline sozinho não teria achado**; foi o import do Portal que falou.
- **Forma da constante saiu de export real, não de dedução:** o molde `PARTIDA_MOTOR_1` escreve
  `TypedConstant` sem tipo para `T#2S` e `LiteralConstant` + `ConstantType` para `TRUE`/`300`. Daí
  a regra: constante que carrega o próprio tipo no texto (`T#`, `W#16#`, `'A'`) fica `TypedConstant`;
  o resto ganha o tipo do pino.
- **"Listar o catálogo plugável no slot" é impossível** — confirmado no SDK: `CanPlugNew` é a única
  pergunta que o Openness responde sobre catálogo. Virou sonda de 11 sufixos de firmware no ramo de
  falha, devolvendo `plugAs`. **Não provado ao vivo**: em `LIB_TESTE` todo slot livre recusa até o
  MLFB versionado que a rodada plugou, então `canPlug` está preso em restrição de slot, não de
  versão.
- **Vazamento da FP-04 vira regra de método:** `grep` em `docs/` não respeita lista de não-ler.
  Rodada cega tem de excluir `docs/teste-cego/` na busca. Está no PLANO.

## Next steps (ordered)
1. **Decidir o item 8 da fila**: `list-io-map --device <drive>` volta vazio com telegrama posto e IO
   system conectado (os itens do drive caem em `unassigned`). Ou o verbo passa a alcançar o endereço,
   ou a decisão é "não serve para drive" e o `CLAUDE.md` (já corrigido) é a resposta final. Investigar
   custa uma sessão de sondagem com o Portal; o programa não precisa (usa o `HWID`).
2. **Provar o `plugAs`** num projeto/slot que aceite plug de verdade — ou marcar a sonda como
   descartável se ninguém tropeçar nela de novo.
3. Depois: MCP em 2 tools, tradução do artigo para EN, postar (SIOS / r/PLC / LinkedIn).
   E, se houver apetite, **FP-05** — a rodada que ainda não existe teria de mirar o que a FP-04 não
   cobriu: os 4 checks novos do `audit` continuam sem ter sido vistos **reprovando**.

## Key files
- `docs/PLANO.md` — seções "FP-04 executada" (fila de 8) e "Fila da FP-04 executada" (como ficou).
- `docs/teste-cego/resultado-FP-04.md` (286 ln) — os 9 tropeços, com o que teria evitado cada um.
- `src/Tia.Core/BlockEdit.cs` — `AddCall`/`InsertCallInXml`, a maior parte do diff.
- `src/Tia.Core/__navi__.md`, `src/Tia.Tests/__navi__.md` — mapas regenerados nesta sessão.

## Open / blockers
- `list-io-map` × endereço de telegrama de drive: aberto (item 8).
- Sonda `plugAs` do `plug-module`: implementada, sem prova ao vivo.
- Os 4 checks novos do `audit` seguem sem ter sido vistos reprovando — duas rodadas cegas passaram.

## Skills
- tia

## Effort
**Médio** para o passo 1. Não é implementação: é sondar o Openness com o Portal aberto para decidir
se `list-io-map` pode alcançar endereço de drive — e o `--sdk` do `tia-help.py` vem antes de qualquer
tentativa e erro. Sobe para **alto** só se a API contradisser a ajuda oficial. O relógio é do Portal
(~10-20 s por verbo), então pensar mais não acelera a parte lenta; se a resposta do SDK for clara,
cai para **baixo** e vira uma linha de `CLAUDE.md`.
