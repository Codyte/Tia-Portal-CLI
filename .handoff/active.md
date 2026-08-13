# Handoff · TIA Portal Openness API · 2026-08-13

## Goal
FP-06 executada e documentada. O que sobra é **a fila de 6 tropeços que a rodada abriu** —
conserto de verbo em `src/Tia.Core/`, na ordem do `resultado-FP-06.md`.

## State
- HEAD: `0a64f4d` (`docs(teste-cego): FP-06 executada`). Working tree limpo fora de `workspace/`.
- Live state: **TIA Portal aberto na sessão 1 com 2 processos** (`Siemens.Automation.Portal`) —
  todo verbo exige `--portal "Software de ETE Insular_Inicial_V21"`. O projeto está **salvo** e
  agora **contém a Área 24 (Elevatória Final, EFE-01) inteira**: 6 devices novos
  (`SINAMICS G_49..G_53`, `ET 200SP station_5`), 46 blocos, 12 tabelas de tags, ramo
  `ELEVATÓRIA_FINAL` na `DB GLOBAL`, UDT `ElevatoriaDados`, OB `CHAMADA_INVERSORES_CCM4`.
  Compile Success 0/0, `audit` 10/10. Diferente da FP-05, **isto não foi revertido**.
- Done: rodada FP-06 (49 min), `entrega-FP-06.md`, `resultado-FP-06.md`, tabela do PLANO, commit.
- In progress: nada.

## Decisions (and why)
- **As 4 armadilhas do caderno (R3/R4/R5/R7) foram recusadas com motivo escrito**, não obedecidas:
  pinos agrupados em UDT, nomes sem prefixo de tipo, palavra de alarme em vez de `Array[1..16]`,
  área 24 em vez de pasta `10.` de 1º nível. O registro é a seção 3 da `entrega-FP-06.md`.
- **Acionamento-semente derivado à mão** (export do `PARTIDA_BOMBA (B-10A)` → patch de texto no XML
  → `import-block` + 5 `create-instance-db`) porque `replicate-fc` replica **entre pastas irmãs** e
  a área nova não tinha irmã com blocos. Depois disso o gerador fez as outras 4 bombas.
- **Horímetro retentivo ficou dentro do `FB CASCATA DE BOMBAS`**, não no `FB_HORÍMETRO` da
  biblioteca: aquele é `NonRetain` e é compartilhado pelos 36 acionamentos — mudar lá mudaria a
  planta inteira.
- **`gen-alarm-fc` rodado sem escopo** (regenerou as 19 áreas existentes como `update`): é o
  desenho do verbo hoje. Compile 0/0 depois, nenhum dano observado — virou item T6 da fila.
- Tentado e descartado: `--fb "FC NOME"` no `add-call` (o prefixo de tipo não entra no valor);
  `--type` do `plug-module` sem `OrderNumber:` (devolve `canPlug:false` mudo).

## Next steps (ordered)
1. **T3** — `replicate-instruments` procurar o tag `_PV_` no PLC inteiro quando não achar na pasta
   de alarme da área (`InstrumentFc.cs`, campo `PvTag`, ~L217). É o único tropeço que gerou bloco
   que não compila. Espelhar o fallback que `Replicate.cs` já usa para `MODO_LOCAL`/`MODO_REMOTO`.
2. **T5** — `set-retain` compilar o alvo antes de exportar, como `add-call`/`delete-network`/
   `add-db-member` fazem (`BlockEdit.cs`).
3. **T2** — `add-call --fb` aceitar o prefixo `FB `/`FC ` (ou corrigir o texto do help).
4. **T1** — `plug-module` normalizar MLFB sem `OrderNumber:`, ou devolver `reason` no `canPlug:false`.
5. **T6** — escopo de área no `gen-alarm-fc` (`--area`/`IncludeFolders`).
6. **T4** — mensagem "mold instrument" citar `MoldInstrumentId`.
7. `pwsh scripts/rebuild.ps1` ao fim (muda o hash do `tia.exe` → o Portal aberto abre diálogo modal
   de autorização; não rebuildar com verbo em voo).

## Key files
- `docs/teste-cego/resultado-FP-06.md` — tropeços T1–T6 com custo medido e a fila ordenada.
- `docs/teste-cego/entrega-FP-06.md` — o que foi entregue e as 4 recusas registradas.
- `src/Tia.Core/InstrumentFc.cs` (T3, `PvTag` ~L217 e `RewireNetwork` ~L395), `BlockEdit.cs` (T5, T2),
  `Hardware.cs` (T1), `AlarmFc.cs` (T6).
- `src/Tia.Core/__navi__.md` — mapa da pasta.
- `workspace/fp06/` — todos os batches e saídas da rodada (gitignored), útil para reproduzir um caso.

## Open / blockers
- Nada bloqueando.
- Compile do PLC inteiro leva ~5 min: sempre em background, nunca em foreground com timeout curto.
- `set-io-address --conflictCheck` (conserto 6 da FP-05) segue **não exercitado** em projeto real.

## Skills
- tia

## Effort
**Baixo** para o passo 1 — é conserto mecânico de um fallback que já existe em `Replicate.cs`, com o
caso de falha reproduzido e conhecido. Suba para **médio** se o `PvTag` precisar de desempate quando
mais de um tag `_PV_` casar com o mesmo instrumento. Reasoning não é o gargalo: o relógio é do
`rebuild.ps1` e do compile do Portal.
