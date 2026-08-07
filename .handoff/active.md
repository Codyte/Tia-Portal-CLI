# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Fechar a engine (CLI + macros). Os 4 buracos do levantamento anterior foram executados nesta
sessão; o que sobra é decisão do user (D8) e um gate nunca exercitado (`init.ps1 -Check` em
máquina limpa).

## State
- HEAD: `4f31d3f` (pushed) — "install-lib installs from the .al21 alone, hardware included".
- Live state: **TIA Portal aberto na sessão 1 com `Base_tia_cli`**; `PLC_TESTE` restaurado
  (drive `INVERSOR_MOTOR_01_CCM_01` de volta no `PROFINET IO-System_TESTE`, `PLC_LIB2` apagado,
  compile Success/0, projeto salvo). O `Software de ETE Insular_Inicial_V21` foi aberto para a
  régua da F8 e **fechado sem salvar** — o `.ap21` em disco não mudou. Shell do agente na
  **sessão 0** (rota da task `TiaSmokeRun`). A `.al21` em disco tem 7 extras (4 UDT novos assados
  do `PLC_TESTE` nesta sessão) — não está no Git, é payload local.
- Done: os 4 itens do levantamento. (1) `install-lib` lê UDT/tabela da `.al21` (`extras`), não mais
  de `library/blocks`; `bake-lib -MoldsOnly` assa também os UDT citados pelo SCL do DB GLOBAL.
  (2) bloco `devices` no `packages.json` → hardware do molde entra no mesmo comando; régua em CPU
  virgem deu Success/0 e reinstalar é no-op. (3) F8 fechada com `replicate-instruments --apply`
  real. (4) F7 fechada em 2 itens; `index`/`checkpoint`/`apply-spec` descartados com motivo.
- In progress: nada mid-flight.

## Decisions (and why)
- **A engine para no `run --script`** — `apply-spec` genérico seria um 2º interpretador por cima do
  1º, e a parte com valor já existe com escopo (`packages.json` + `install-lib`). Reabrir só se um
  3º macro repetir a reconciliação do `install-lib`. `index` morreu por medição (`trace` = 3,3 s),
  `checkpoint` perde para cópia do `.ap21` (restauraria bloco sem restaurar hardware, que é o que
  quebra). Registrado em "Fronteira da engine" no PLANO.
- **`in-sync` eterno do `replicate-instruments` nunca foi bug**: o projeto de teste foi gerado pelo
  mesmo algoritmo. Duas rotas tentadas e descartadas: deletar a FC alvo (a OB de chamada fica
  inconsistente e o gerador precisa exportá-la — `Inconsistent blocks ... cannot be exported`) e
  deslocar `NextCommandIds` (os números de comando não entram no XML da família TOT1, continua
  in-sync). O que destrava é **instrumento novo**, que é o caso de uso real.
- **`-Update` não vale para hardware**: apagar e recriar device derruba a rede. Device presente fica
  como está; o par `connect-subnet` vai mesmo assim, porque o drive pode estar ligado noutro
  controlador.
- **Extras assados do `PLC_TESTE` nesta rodada**, não do `PLC_ZERO` — `Project1` estava fechado e
  abrir custa 2-4 min. O `bake-lib -MoldsOnly` contra `PLC_ZERO` pega os mesmos 7 daqui pra frente.

## Next steps (ordered)
1. **`init.ps1 -Check` numa máquina limpa de verdade** — nunca exercitado ponta a ponta. O passo 1
   encurtou o caminho (some `library/blocks/`), mas a `.al21` continua gitignored: clone limpo ainda
   precisa de um `bake-lib` a partir de um projeto que tenha a biblioteca. Conferir se o `-Check`
   diz isso com clareza.
2. **D8 (online: `go-online`/`download`/`compare`)** — decisão do user, não trabalho técnico. É o
   maior buraco de superfície da API que sobra.
3. Opcional: `add-db-member` não tem contrário (`delete-db-member`). Apareceu na limpeza da F8;
   custou nada porque o projeto foi fechado sem salvar, mas num projeto salvo seria manual.

## Key files
- `scripts/install-lib.ps1` — bloco de `devices` (~linha 110), extras via `import-master-copy`
  (~linha 140), `Get-Existing` com o fix do array cru.
- `scripts/bake-lib.ps1:53` — regex que colhe os UDT dos `library/db-global/*.scl`.
- `src/Tia.Core/Hardware.cs:385-420` — `connect-subnet`: IO system por controlador, compare por
  nome, `DisconnectFromIoSystem` antes de mover.
- `library/packages.json` — bloco `devices` + os dois `_doc`.
- `docs/PLANO.md` — "Biblioteca em um comando", "F8 fechada", "Fronteira da engine" (todas de
  2026-08-07); tabela de fases com F7 e F8 ✅.
- `scripts/__navi__.md` e `src/__navi__.md` — regenerados nesta sessão.

## Open / blockers
- **D8** de pé bloqueia toda a superfície online.
- Diálogo modal `Openness access` volta a cada `rebuild.ps1` com o Portal aberto: chamada pendurada
  com CPU ~0 = alguém precisa clicar.
- `import-block --folder` **cria a árvore que faltar a partir da raiz** — caminho parcial
  (`5.2 Totalizadores` em vez de `5. Instrumentação / Atuadores/5.2 Totalizadores`) cria pasta
  paralela homônima e o gerador seguinte morre em colisão de nome. Sempre caminho completo.

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo** para o passo 1: é rodar `init.ps1 -Check` e ler os 9 pontos; o julgamento é sobre texto de
mensagem, não sobre API. Sobe pra **médio** se a máquina limpa não tiver o grupo `Siemens TIA
Openness` ou o Portal, porque aí o gate falha por ambiente e é preciso separar "o script está certo"
de "a máquina não está pronta". Raciocínio não é o gargalo em nenhum dos dois casos — o relógio é o
`init.ps1` e o eventual logoff/logon do grupo do Windows.
