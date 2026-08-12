# Handoff · TIA Portal Openness API · 2026-08-12

## Goal
FP-05 executada e relatada. O próximo passo é decidir o que vira código na CLI a partir dos 7
tropeços medidos — a fila está escrita na seção 6 de `docs/teste-cego/resultado-FP-05.md`.

## State
- HEAD: `46a3cf6` (`docs(teste-cego): resultado da FP-05`), working tree limpo fora de `workspace/`
  (gitignored).
- **Live state: TIA Portal aberto na sessão 1 com `proj/Software de ETE Insular_Inicial_V21`
  MODIFICADO E NÃO SALVO.** A Área 24 `Recirculação` inteira (hardware + programa) está só em
  memória. Fechar sem salvar reverte tudo; salvar contamina o molde da casa, que não tem backup.
  Enquanto esse Portal viver: nada de `save-project` nem `close-project --save`.
- Done: FP-05 completa — 07:51→08:23 (32 min, ~41 chamadas). `ET 200SP station_5` (DI/DQ/AI/AO),
  15 tags de campo, 9 tabelas, ramo `RECIRCULACAO` na `DB GLOBAL`, 17 blocos, `CHAMADA_RECIRCULACAO
  (QA-04)` (OB142) na chamada cíclica. `compile` 0/0, `audit` 10/10, 0 colisão de endereço.
  Relatório commitado.
- In progress: nada em vôo.

## Decisions (and why)
- **As 3 exigências da seção 6 do caderno que violam a lei da casa foram recusadas** (chamada em
  SCL, escalar na raiz da DB, `CHAMADA_*` na pasta da área), com o motivo escrito no relatório — o
  item 7 do caderno prevê exatamente isso. A 4ª ("não criar UDT novo") saiu de graça: a casa já tem
  `MotorPrincipal`/`MotorDados`/`ValvDados`/`SensorDados`.
- **Área nasceu como 24, não 4** — a 4 do projeto já é `Elevatória de Gordura` em `2.4`/`3.1.4`/
  `5.1.4`, e usar 4 reprovaria o check de numeração consistente.
- **Horímetro e contador de partidas ficaram em estáticas retentivas do FB novo**, não no
  `STS_HORIMETRO` do `FB CONDIÇÃO DE PARTIDA`: torná-lo retentivo exigiria `set-retain` no FB **da
  biblioteca**, atingindo os 36 acionamentos existentes. O valor é publicado na DB, a IHM não vê
  diferença.
- **`RECIRCULACAO` ficou plana na `DB GLOBAL`** (sem `ALARMES`/`EVENTOS`/`INSTRUMENTACAO`) — não é
  estética, é limite da CLI (tropeço T4). Registrado como divergência.
- Rejeitado: clonar `PARTIDA_BOMBA (B-10A)` como molde das bombas novas. É acionamento com inversor,
  e o `--replace` traria tag de I/O que não existe na Área 24 — os `PARTIDA_*` novos saíram de
  `import-ladder` + `add-call`.
- Retirado do `standing.md`: a entrada sobre `run --script` não abrir projeto — já está no
  `CLAUDE.md` do repo, era duplicata.

## Next steps (ordered)
1. **Fechar a rodada com o usuário**: perguntar se o Portal fecha (sem salvar, revertendo a Área 24)
   ou fica aberto para inspeção visual do que foi construído. Não decidir sozinho.
2. Atacar a fila da seção 6 do `resultado-FP-05.md`, na ordem escrita. Os dois primeiros são os que
   pagam: `add-call` aceitando FB sem parâmetro (o `empty` do `BlockEdit.cs` já resolve o caso do FC
   — falta estender ao FB, que só precisa do `<Instance>`), e `nextFreeByte` honesto no
   `list-io-map` (hoje entrega endereço que o Portal recusa; o próprio verbo já conta
   `unassigned: 130`, que é onde os 398 bytes invisíveis moram).
3. Atualizar a tabela de fases do `docs/PLANO.md` com a FP-05 fechada.
4. Cada item da fila que virar código: `pwsh scripts/rebuild.ps1` e teste offline antes do smoke.

## Key files
- `docs/teste-cego/resultado-FP-05.md` — a entrega desta sessão; a fila está na seção 6, e os
  tropeços medidos (com o erro exato do Portal) na seção 3.
- `docs/teste-cego/criterios-FP-05.md` — a régua, lida depois da execução; os 5 portões passaram.
- `src/Tia.Core/BlockEdit.cs` — `add-call` (T5 e T6 moram aqui; `empty` por volta da L300).
- `src/Tia.Core/Hardware.cs` — `list-io-map`/`set-io-address`/`connect-subnet` (T1, T2, T3).
- `src/Tia.Core/DbMember.cs` — `add-db-member`, guarda contra `--type Struct` na L67 (T4).
- `src/Tia.Core/__navi__.md` e `src/Tia.Cli/__navi__.md` — mapa das duas pastas.
- `workspace/fp05/` — tudo que a rodada leu e escreveu (gitignored): `plc-navi.md`, `io-map.json`,
  `lib-interface.json`, `src/*.scl` das 6 fontes.

## Open / blockers
- O projeto real está aberto e sujo (ver Live state). Qualquer sessão que rodar `tia` nele está
  trabalhando em cima da Área 24 — e um `save-project` acidental é irreversível.
- Os `workspace/fp05-*.json` (scripts de batch da rodada) ficam como receita do que foi feito, mas
  são gitignored: se a Área 24 tiver que ser reconstruída depois de fechar sem salvar, é deles que
  sai a sequência.

## Skills
- tia

## Effort
**Baixo** para o passo 1 (é uma pergunta ao usuário) e **médio** para o passo 2: os dois primeiros
itens da fila são mudança pontual em arquivo conhecido, com o sintoma já medido e o local apontado.
Suba para **alto** só se o `nextFreeByte` exigir entender por que 130 itens ficam `unassigned` — aí
vira sonda de API e vale `tia-help.py --sdk "Address"` antes de tentar.
