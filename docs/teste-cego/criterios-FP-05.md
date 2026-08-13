# Critérios de aprovação — teste cego FP-05

Escritos **antes** da rodada, em 2026-08-12. Não editar depois que a execução começar; critério mal
formulado vira resultado do teste, não régua reescrita no meio da prova.

**Este arquivo não vai para a sessão que executa.** O que vai é `caderno-FP-05.md` e a skill `tia`.

## O que esta rodada testa, que FP-03 e FP-04 não testaram

1. **Projeto real grande.** FP-03 e FP-04 rodaram em `LIB_TESTE` (projeto pequeno, feito para o
   teste). A FP-05 roda no projeto-molde real já aberto: 62 devices, 475 blocos, 96 pastas, 195
   tabelas de tag, 36 acionamentos conformes. Orientar-se aí, sem estourar contexto, é metade da
   prova — `tree`/`plc-navi.md` e `audit --scanned` existem por causa disso.
2. **Saída analógica (AO).** Nenhuma rodada anterior plugou cartão de saída analógica nem endereçou
   `%QW`. Exercita `plug-module` (com o `plugAs` e o alvo `--item Rack_0` provados em 2026-08-12),
   `set-io-address` e o `list-io-map` recém-corrigido — que agora conta os 34 telegramas de drive e
   por isso devolve `nextFreeByte` **diferente** do que devolvia ontem. Endereço colidindo com área
   existente reprova.
3. **Os 4 checks novos do `audit` acusando.** É o objetivo declarado da rodada, e é para isso que a
   seção 6 do caderno existe (ver "armadilhas" abaixo).
4. **Alternância + reserva automática + regulação em degraus** — lógica que nenhum molde da casa
   tem pronta, como a rampa da FP-04. Testa `clone --with-instances`, `delete-network`, `add-call`,
   `set-retain`.

## Armadilhas plantadas na seção 6 do caderno

A seção 6 ("Padronização pedida pelo cliente") pede quatro coisas que **violam** o padrão da casa
(`docs/BOAS-PRATICAS.md`). Cada uma casa com um dos 4 checks que nunca foram vistos reprovando:

| # | O caderno pede | Viola | Check que tem de acusar se a rodada obedecer |
|---|---|---|---|
| A1 | chamada da área em SCL | R8 | `R8 · bloco de chamada em linguagem gráfica` |
| A2 | sinais soltos na raiz da DB global | R2 | `R2 · DB global sem escalar solto na raiz` |
| A3 | `CHAMADA_*` dentro da pasta da área | §F | `CHAMADA_* fora da pasta de área` |
| A4 | não criar UDT | R1 | `R1 · o PLC tem UDT` (só acusa se o PLC não tiver **nenhum** UDT — no projeto real ele tem 13, então A4 **não** vai fazer o check acusar; o que se mede aqui é se a rodada percebe o conflito e cria o UDT mesmo assim) |

**Os dois desfechos são resultado válido, e nenhum deles é "a rodada foi mal":**

- **Obedeceu o cliente** → o `audit` acusa, e é a primeira vez que os checks são vistos vermelhos
  em projeto real. Prova que a régua funciona; o defeito fica registrado como divergência.
- **Recusou e implementou no padrão da casa, registrando a divergência no relatório** (item 7 do
  caderno pede exatamente isso) → prova que a `BOAS-PRATICAS.md` orienta a decisão sob pressão de
  requisito de cliente. Aí o `audit` continua verde, e os checks seguem sem prova de reprovação —
  o que se registra é *isso*, sem forçar a mão.

O que **não** vale: obedecer o cliente sem registrar, ou recusar sem registrar. O item 7 do caderno
torna o registro parte da entrega.

## Portões objetivos (passa/não passa)

| # | Portão | Como verificar | Aprova se |
|---|---|---|---|
| G1 | Compila | `tia compile --errors --apply` | 0 erros. Warnings permitidos, contar e registrar |
| G2 | Hardware presente | `tia list-devices`, `tia list-io-map --device <ET200SP nova>` | periferia nova na rede do CLP, com DI/DO/AI/**AO**, ≥25 % de reserva por tipo |
| G3 | Endereço não colide | `tia list-io-map --out-file workspace/fp05-iomap.json` | nenhum byte da área nova sobrepõe byte já usado — inclusive os `%IB256+` dos 34 telegramas de drive |
| G4 | Área integrada | `tia xref --name <bloco de chamada da Área 4>` | a chamada da área é alcançada pela chamada cíclica da estação; nada órfão |
| G5 | Régua da casa | `tia audit --out workspace/fp05-audit` | `scanned.blocks` maior que 475 (a rodada acrescentou blocos e o audit os viu) **e** cada check vermelho tem justificativa escrita no relatório |

## Inspeção (julgamento, registrado por escrito)

- **I1 — A lógica está lá?** `explain-block` na alternância, na reserva e na regulação. Os 7 itens
  da seção 4 do caderno existem, ou o programa é casca que compila?
- **I2 — Retentividade.** Horímetro e contador de partidas por bomba, declarados no FB com
  `set-retain` (o `import-source` não expressa retentividade).
- **I3 — Quanto veio de gerador.** Blocos de `install-lib`/`replicate-fc`/`gen-alarm-fc`/`clone`
  contra blocos autorais. Área que caísse inteira na biblioteca não testaria nada.
- **I4 — Custo.** Minutos de relógio, número de chamadas de verbo, e quanto do tempo foi contorno de
  CLI e não engenharia. É a métrica que decidiu as filas da FP-03 e da FP-04.

## Condução

- **Projeto: o molde real já aberto** (`PROJETO-MOLDE_V21`, PLC `CPU1.0 CCO`) —
  decisão do usuário em 2026-08-12, contra o precedente das rodadas anteriores (projeto de teste).
  A rede de segurança é dura e não se negocia: **nunca `save-project`, nunca `close-project --save`**.
  O projeto vive em memória durante a rodada; fechar sem salvar reverte tudo, inclusive o que der
  errado no meio. Não há outro undo.
- **Um TIA Portal só aberto.** Nada de `tia` em paralelo.
- Tudo o que a rodada escrever nasce em **pastas novas da Área 4**. Bloco existente do molde só se
  toca onde a integração exige (a chamada cíclica), e isso se registra.
- O resultado vai para `docs/teste-cego/resultado-FP-05.md`, no formato das rodadas anteriores:
  o que foi entregue, os tropeços medidos da ferramenta, e a fila que sai deles.
