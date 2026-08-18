<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L11    Critérios de aprovação — teste cego FP-06 -->
<!--   L18    O que esta rodada testa, que as anteriores não testaram -->
<!--   L44    Armadilhas plantadas na seção 6 do caderno -->
<!--   L66    Portões objetivos (passa/não passa) -->
<!--   L78    Inspeção (julgamento, registrado por escrito) -->
<!--   L97    Condução -->
<!-- ======================= END NAV INDEX ======================= -->

# Critérios de aprovação — teste cego FP-06

Escritos **antes** da rodada, em 2026-08-13. Não editar depois que a execução começar; critério mal
formulado vira resultado do teste, não régua reescrita no meio da prova.

**Este arquivo não vai para a sessão que executa.** O que vai é `caderno-FP-06.md` e a skill `tia`.

## O que esta rodada testa, que as anteriores não testaram

1. **Acionamento por inversor SINAMICS.** Todas as rodadas até aqui foram partida direta. Cinco
   drives novos exercitam a cadeia inteira que só existe em teoria no `CLAUDE.md`:
   `insert-telegram --change` (drive novo nasce com `MainTelegram #1` e o telegrama Main não pode ser
   apagado), os **dois** `connect-subnet` na ordem (PLC com `--io-system NOME`, depois o drive), e a
   constante `<drive>~PROFINET_interface~Standard_telegram_20` que só nasce quando o drive é IO
   device daquele controlador. Errar a ordem, ou tentar `plug-module`, é o tropeço que já custou
   várias sessões — aqui se mede quanto custa com a CLI atual.
2. **Cinco equipamentos idênticos = terreno dos geradores.** A FP-05 fechou com `replicate-fc`,
   `gen-alarm-fc`, `gen-fault-ob` e `install-lib` **sem uso nenhum** (I3 daquela rodada), porque duas
   bombas em partida direta não têm molde na casa. Cinco inversores têm: o projeto já traz
   `CHAMADA_INVERSORES_CCM*`. Se a rodada replicar bomba a bomba no braço com cinco `clone`, isso é
   achado — ou o gerador não serve, ou não está achável.
3. **A hierarquia de área da `DB GLOBAL`, agora que dá.** O item 1 da seção 4 da FP-05 ("`RECIRCULACAO`
   plana") saiu de `add-db-member` não construir sub-struct. O `--path` que cria o ramo entrou em
   2026-08-12 e **nunca foi usado contra o molde de verdade**. Se a área nova sair plana de novo, o
   conserto não resolveu o problema que dizia resolver.
4. **Palavra de alarme, não bit `%M`.** Item 2 da seção 4 da FP-05. O caderno pede na seção 5 que os
   alarmes cheguem à IHM pelo mesmo mecanismo das outras áreas — que é `FB BITS TO WORD` gravando
   `DB GLOBAL.<área>.ALARMES`, com `gen-alarm-fc`. Desta vez o requisito é explícito no caderno.
5. **`nextFreeByte` como piso declarado.** A FP-05 mediu 398 bytes de mentira (T2). O conserto não
   inventou verdade: declarou `nextFreeByteExact: false` + `nextFreeByteNote`, e `set-io-address`
   dry-run passou a cruzar o `--start` com o mapa (`conflictCheck`). Aqui se mede se a rodada
   **acredita no aviso** e vai atrás do `Next free address: N` do Portal em vez de repetir a colisão.

## Armadilhas plantadas na seção 6 do caderno

Quatro pedidos que violam `docs/BOAS-PRATICAS.md`, escolhidos entre as regras que nenhuma rodada
anterior pressionou (a FP-05 pressionou R1, R2, R8 e §F):

| # | O caderno pede | Viola | O que se mede |
|---|---|---|---|
| B1 | todos os sinais por pino, um por sinal (~15 escalares) | R3 (≤8 escalares por FB) | se a rodada agrupa em UDT e registra a divergência, ou entrega interface inchada como o `FP01` da BOAS-PRATICAS |
| B2 | prefixo de tipo nos nomes (`bFalha`, `tRetardo`, `rNivel`) | R4 | é exatamente o achado D do `FP01`; nenhum `audit` cobre isso, então só o relatório denuncia |
| B3 | alarmes em `Array[1..16] of Bool` indexado por número | R5 | achado E do `FP01`; casa com o pedido da seção 5 (palavra de alarme) — os dois podem coexistir se o array tiver UDT/constantes por trás |
| B4 | pasta de 1º nível `10. Elevatória Final` | R7 (0..9 têm dono, categoria nova entra como sub-nível) | é o mais fácil de obedecer sem perceber, porque a CLI cria a pasta sem reclamar |

**Nenhum dos 10 checks do `audit` pega B1–B4.** É de propósito: a FP-05 provou que a régua
automática funciona quando existe; esta rodada mede se a `BOAS-PRATICAS.md` sozinha, sem check que
reprove, ainda orienta a decisão. Obedecer registrando é resultado válido; recusar registrando é
resultado válido. Obedecer ou recusar **sem registrar** é o que não vale — o item 7 do caderno torna
o registro parte da entrega.

Armadilha operacional, não escrita como pedido: o caderno diz "**mesmo telegrama que os inversores
existentes já usam**". Descobrir qual é (`list-telegrams`) antes de inserir é o caminho barato;
assumir 20 de cabeça é o caro se a estação usar outro.

## Portões objetivos (passa/não passa)

| # | Portão | Como verificar | Aprova se |
|---|---|---|---|
| G1 | Compila | `tia compile --errors --apply` | 0 erros. Warnings permitidos, contar e registrar |
| G2 | Drives na rede | `tia list-devices`, `tia list-telegrams --device BEF-0N` | 5 drive objects, cada um IO device do `CPU1.0 CCO`, telegrama igual ao dos inversores existentes, **e a constante `~Standard_telegram_NN` existindo** para os 5 |
| G3 | Endereço não colide | `tia list-io-map --out-file workspace/fp06-iomap.json` | nenhum byte novo (periferia + os 5 telegramas) sobrepõe byte já usado; o `%I` novo respeita o `Next free address` real do Portal, não o `nextFreeByte` do mapa |
| G4 | Área integrada | `tia xref --name <bloco de chamada da área>` | a chamada da área é alcançada pela chamada cíclica da estação; nada órfão |
| G5 | Régua da casa | `tia audit --out workspace/fp06-audit` | `scanned.blocks` maior que o inicial **e** cada check vermelho com justificativa escrita no relatório |
| G6 | Alarme como o resto da estação | export do `FC_ALARMES_*` / `explain-block` | alarmes chegam em palavra na `DB GLOBAL.<área>.ALARMES`, não só em bits `%M` soltos (é o pedido da seção 5 do caderno) |
| G7 | `DB GLOBAL` hierárquica | `tia audit --out` (o export do R2) | a área nova tem `ALARMES`/`EVENTOS`/grupo de equipamento/`INSTRUMENTACAO` como o resto da DB — plana reprova, e o motivo vai para os tropeços |

## Inspeção (julgamento, registrado por escrito)

- **I1 — A lógica está lá?** `explain-block` na cascata, no rodízio e na regulação de nível. Os 8
  itens da seção 4 do caderno existem, ou o programa é casca que compila? Ponto fino: o rodízio
  semanal (item 3) e o limite de quatro bombas com exceção no nível alto (itens 2 e 6).
- **I2 — Retentividade.** Horímetro e contador de partidas das 5 bombas, declarados no FB com
  `set-retain`.
- **I3 — Quanto veio de gerador.** É o item 2 lá de cima virado métrica: contar blocos vindos de
  `replicate-fc`/`gen-alarm-fc`/`install-lib`/`import-master-copy` contra blocos autorais e contra
  `clone` manual. Cinco bombas iguais replicadas a mão é reprovação da ferramenta, não da rodada.
- **I4 — Custo.** Minutos de relógio, número de chamadas, e quanto do tempo foi contorno de CLI e não
  engenharia. É a métrica que decidiu as filas das rodadas anteriores. Comparar com os ~32 % da
  FP-05.
- **I5 — Os sete consertos da FP-05 seguraram?** `add-call` (FB sem pino, Input solto, `networksBefore/After`),
  `add-db-member --path`, `list-io-map` (`nextFreeByteExact`/`InDevice`), `connect-subnet`
  (`existingSubnets`), `set-io-address` (`conflictCheck`), `clone`/`delete-network` (`networks`).
  Cada um que voltar a doer volta para a fila com o motivo — conserto que não segurou em projeto
  real é pior que conserto que não existe.

## Condução

- **Projeto: `proj/PROJETO-MOLDE_V21`, PLC `CPU1.0 CCO`** — projeto de teste, com
  escrita liberada pelo usuário (2026-08-12). Ao contrário da FP-05, salvar é permitido; o que
  continua valendo é a etiqueta do `standing.md` e o registro do que foi mexido. Fechar sem salvar
  segue sendo o undo mais barato de uma rodada que der errado.
- **Um TIA Portal só aberto.** Nada de `tia` em paralelo.
- Tudo o que a rodada escrever nasce em pastas novas da área. Bloco existente do molde só se toca
  onde a integração exige (a chamada cíclica), e isso se registra.
- O resultado vai para `docs/teste-cego/resultado-FP-06.md`, no formato das rodadas anteriores: o
  que foi entregue, os tropeços medidos da ferramenta, e a fila que sai deles.
