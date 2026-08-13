# Régua-base do teste cego — parte fixa, igual em toda rodada

Escrita em 2026-08-13, depois da revisão da série FP-01→FP-06. Existe porque a régua vinha sendo
reescrita inteira a cada rodada (`criterios.md` → `-FP-05` → `-FP-06` → `-FP-07`): quando a régua
muda junto com o terreno, não há série comparável, e o único número que se comparava (% de contorno
de CLI, 32 % → 12 %) era confundido pelo terreno.

A partir da FP-07 o arquivo por rodada é **anexo**: traz só o que é daquela rodada (portões do
terreno, dívida perseguida, armadilhas, projeto usado). Tudo que está aqui vale sem repetir.

**Este arquivo não vai para a sessão que executa.** A sessão cega recebe o caderno da rodada + a
skill `tia`, e nada mais.

## Condução (invariante)

- **Quem escreveu o caderno não executa.** A sessão que roda recebe o `caderno-FP-NN.md`, a skill
  `tia`, e nada mais da conversa que escreveu a rodada.
- **Busca em rodada cega exclui `docs/teste-cego/` explicitamente.** Lista de não-ler não vale para
  `grep`: na FP-04 um `grep` em `docs/` bateu em resultado antigo e mostrou MLFBs que a rodada
  deveria descobrir sozinha. Vazamento que acontecer, **registrar como aconteceu** em vez de anular
  a rodada.
- **Régua congelada quando a execução começa.** Critério mal formulado vira resultado do teste, não
  régua reescrita no meio da prova.
- **Caderno conferido contra o projeto antes da rodada.** O briefing da FP-04 afirmava que o projeto
  não tinha periferia nem inversor — tinha. Toda afirmação de fato do caderno (o que existe, o que
  falta, endereço livre, nome de pasta) se confere com verbo de leitura antes de entregar o caderno
  à sessão cega, e a conferência vai no anexo da rodada.
- **Um TIA Portal aberto**, ou `--portal` em todas as chamadas. Nunca duas chamadas `tia` em
  paralelo.
- **Sem toque no GUI.** Clique necessário (o diálogo de autorização do Openness conta) = registrar o
  quê e por quê: cada clique é um furo na alegação de ponta a ponta.

## Portões que valem em toda rodada

| # | Portão | Aprova se |
|---|---|---|
| G-A | Compila | `compile` do PLC: **0 erros**. Warnings permitidos, contados e registrados |
| G-B | Nada órfão | Todo bloco novo é alcançado a partir de um OB; nenhuma chamada pendurada |
| G-C | Régua da casa | `audit` sem check vermelho, ou justificativa escrita por check que reprove. **`scanned` registrado** — é o que separa check conforme de check cego |
| G-D | Endereço não colide | Mapa de I/O final sem sobreposição em `%I`/`%Q`, telegramas de drive incluídos |

Portões de terreno (hardware daquela obra, endereço de diagrama, integração de área) entram
numerados `G1…Gn` no anexo da rodada.

## Métricas fixas (é isto que torna a série comparável)

Toda rodada registra estes seis, sempre no mesmo formato — mesmo que o terreno mude:

| # | Métrica | Como se mede |
|---|---|---|
| M1 | Relógio | início, fim, total, e a repartição de onde o tempo foi. Desde 2026-08-13 o `run --script` traz `ms` por step e `--summary` traz `slowest[3]` — usar isso, não `Measure-Command` por fora |
| M2 | Chamadas | quantas invocações de `tia`, e quantas foram batch (`run --script`) |
| M3 | **Contorno de CLI** | % das chamadas que fizeram no braço (Python, XML, GUI) o que um verbo faria. Série: FP-05 32 % → FP-06 12 % |
| M4 | Origem dos blocos | % vindo de gerador/replicação/clone × autoral. Série: FP-05 0 % (sem molde) → FP-06 80 % (terreno favorável) |
| M5 | Tropeços | um por linha, separados entre **"o caderno não dizia"** (esperado — obra real também não diz) e **"a ferramenta não dizia"** (defeito nosso, e provavelmente do `SKILL.md`) |
| M6 | Cliques de GUI | quantos, e por quê |

M3 e M4 só se comparam entre rodadas junto com uma linha sobre o terreno (havia molde na casa?
projeto novo ou adendo?) — sem isso o número engana, que é o defeito que esta régua conserta.

## O que o anexo da rodada traz

1. Portões de terreno (`G1…Gn`) e pontos de inspeção daquela obra.
2. A **dívida perseguida**: que conserto de CLI o caminho natural da entrega força a exercitar, e
   há quantos ciclos está parado. Veredito por dívida no fim: **segurou**, **doeu**, ou **não
   exercitado** (numa rodada desenhada para exercitar, "não exercitado" é achado, não neutro).
3. As **armadilhas** plantadas no caderno — pedido que contraria as regras da casa. Recusar **sem
   registro escrito** conta como recusa cega e é tão ruim quanto obedecer: o produto é a
   justificativa, não o "não".
4. A conferência do caderno contra o projeto (item da Condução).
5. Projeto usado e estado esperado dele.

## O que se registra no resultado

Além das seis métricas: o veredito por portão, o veredito por dívida, e **a fila que sai da rodada,
ordenada por dor evitada ÷ tamanho do diff**. Mais importante que o veredito são os tropeços (M5) —
é deles que sai o trabalho.
