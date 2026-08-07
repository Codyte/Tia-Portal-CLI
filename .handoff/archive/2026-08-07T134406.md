# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Fechar a **engine** (o CLI + macros), não a biblioteca: biblioteca é conteúdo e pode ser trocada
depois. Levantar e executar o que falta para o `tia` ser ferramenta completa.

## State
- HEAD: `2d5adc7` — commit do ciclo da biblioteca.
- Live state: **TIA Portal aberto na sessão 1, projeto `Base_tia_cli` salvo.** `PLC_TESTE` está com
  a biblioteca **inteira** instalada (5 pacotes + 4 moldes + extras) e **compila Success / 0 erros**,
  com o G120 `INVERSOR_MOTOR_01_CCM_01` (telegrama 20) ligado no IO system
  `PROFINET IO-System_TESTE` da subnet `PN/IE_1`. `Project1` foi aberto e fechado sem salvar.
  Shell do agente na **sessão 0** (rota da task `TiaSmokeRun`).
- Done: **ciclo da biblioteca fechado ponta-a-ponta** — `bake-lib -MoldsOnly` (moldes + UDT/tabelas
  do `packages.json`), fix do `Library.Open` (read-only), régua 4 erros → 0 com
  `add-device` + `insert-telegram --change` + 2 × `connect-subnet`. Tudo commitado e documentado no
  PLANO ("Ciclo completo da biblioteca fechado").
- In progress: nada mid-flight. Próximo trabalho é o levantamento abaixo.

## Decisions (and why)
- **Moldes vêm do `PLC_ZERO` do `Project1`**, saída do `scaffold --manifest library/generic.json`.
  O `CPU1.0 CCO` do `Base_tia_cli` é CPU de cliente — assar dali levaria payload de cliente pra
  `.al21`. Por isso `-MoldsOnly` existe: fonte diferente da dos 5 pacotes.
- **Extras (UDT/tabela) vão pra `--lib-folder extras`, nunca pra `$Root`** — o `install-lib` trata
  master copy não-pasta em `$Root` como *base* e instalaria UDT/tabela como se fosse bloco.
- **`-Prune` recusado junto com `-MoldsOnly`** — a rodada só enxerga a fatia dos moldes e apagaria
  os 5 pacotes como órfãos.
- **Telegrama posto não cria a constante de hardware.** `insert-telegram` sozinho deixou os mesmos
  4 erros; `X~PROFINET_interface~Standard_telegram_20` só nasce quando o drive é IO device daquele
  controlador (2 × `connect-subnet`, PLC primeiro).
- **`--format table` (TSV) descartado** (registro antigo, continua valendo): 2x num problema que
  precisa de 30x — o que paga é agrupar ou não devolver volume.

## Next steps (ordered)
O user pediu o levantamento "o que falta pra engine ficar pronta". Ordem sugerida, do que já tem
evidência medida pro que é decisão:

1. **`install-lib` consumir os extras da `.al21`, não os XML de `library/`** (linhas 123-132 de
   `scripts/install-lib.ps1`): hoje UDT e tabela de tag saem de `library/blocks/<UDT>.xml` e
   `library/tags/*.xml`, que são **payload gitignored** — clone limpo do repo não instala. As master
   copies já estão na `.al21` (pasta `extras`); falta trocar `import-type`/`import-tags` por
   `import-master-copy --name <extra>`. **É o último buraco conhecido do caminho da biblioteca.**
2. **Dependência de hardware não é declarável.** `packages.json` declara `requires`/`db`/`tags`/
   `types`/`instances`, mas não o inversor que o molde `Motor 1 (MOTOR_01)` exige. Hoje o
   `add-device` + `insert-telegram` + 2 × `connect-subnet` são passo manual fora do macro. Um bloco
   `devices` no `packages.json` fecharia o "instalar biblioteca" em um comando de verdade.
3. **F8 fecha com `replicate-instruments --apply` real** — é o único dos geradores que nunca escreveu
   contra projeto real (dry sempre deu `in-sync`). Todos os outros já têm `--apply` medido.
4. **F7 itens 3-5 nunca começaram**: `index`, `checkpoint`, `apply-spec`. `explain-block` e `trace`
   estão fechados e medidos. Decidir se `apply-spec` (escrita a partir de spec declarativa) entra
   ou se a engine para no `run --script`.
5. **v2 item 9 (online: go-online/download/compare)** está bloqueado pela decisão **D8**, não por
   código. É o maior buraco de superfície da API; reabrir D8 é decisão do user.
6. **`init.ps1 -Check` numa máquina limpa de verdade** — o gate de clone-limpo nunca foi exercitado
   ponta-a-ponta (é o que o passo 1 destrava).

## Key files
- `scripts/install-lib.ps1:123-132` — o `import-type`/`import-tags` que o passo 1 troca.
- `scripts/bake-lib.ps1` — `-MoldsOnly`, `$molds`/`$extras`, guard do `-Prune`.
- `library/packages.json` — onde entraria o bloco `devices` do passo 2.
- `src/Tia.Core/Library.cs:19-40` — `Open` com o fix de ReadOnly→ReadWrite.
- `docs/PLANO.md` — tabela de fases (F7/F8 🔄) + "Ciclo completo da biblioteca fechado" (2026-08-07).
- `scripts/__navi__.md` e `src/__navi__.md` — mapas das duas pastas.

## Open / blockers
- **D8 (online)** é decisão do user, não trabalho técnico: enquanto ficar de pé, `go-online`,
  `download` e `compare online/offline` não existem, e isso é o que mais falta pra "ferramenta
  completa" no sentido amplo.
- Diálogo modal `Openness access` volta a cada `rebuild.ps1` com o Portal aberto: chamada pendurada
  com CPU ~0 = alguém precisa clicar, não é bug de API.

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo** para o passo 1: a troca é mecânica (`import-master-copy --name` já existe e foi exercitado
hoje), e o que ele toca — o bloco de `$types`/`$tags` do `install-lib` — tem um caller só. Subir
pra **médio** se `import-master-copy` de `PlcTagTable`/`PlcStruct` não aceitar `--folder` como o de
bloco aceita (aí vira ramo novo no `Library.ImportMasterCopy`, não edição de script). O gargalo do
relógio não é raciocínio: cada attach do Openness e o `install-lib` dominam, e o diálogo modal
bloqueia tudo até alguém clicar.
