# Handoff · TIA Portal Openness API · 2026-08-11

## Goal
Fila de melhorias que saiu do teste cego FP-03: entregue e commitada. O que sobra é decidir a
próxima frente (FP-04? publicação? outro projeto real), não terminar nada pendente.

## State
- HEAD: `80de94a` — `feat(cli): fila da FP-03 — add-call, delete-network, set-retain, list-interface (76 verbos)`.
  Working tree limpo (só o arquivo de archive deste handoff por commitar).
- Live state: **TIA Portal aberto** (sessão 1) com `workspace/newlib/LIB_TESTE/LIB_TESTE.ap21`,
  PLC `PLC_ZERO` (o projeto tem 2 PLCs: `PLC_ZERO` e `PLC_RT` → **todo verbo exige `--plc`**).
  Projeto compilado (0 erros / 1 aviso de I/O sem cartão) e `audit` **6/6**. O shell desta sessão
  nasceu na **sessão 1** (`tia` roda direto, sem a rota da task).
- Done: os 7 itens da fila da FP-03 — `add-call`, `delete-network`, `set-retain`, `list-interface`,
  `clone --with-instances`, `audit` reconhecendo partida direta, `create-instance-db --of`
  aproximado, mais o guard `Ops.ImportAndProve` (import → compila → re-exporta e prova).
  72 → **76 verbos**. 8 casos novos em `Tia.Tests` (`ALL PASS`). `rebuild.ps1` rodado, whitelist
  refeita, `VERBS.md`/`src/__navi__.md` regenerados.
- In progress: nada. Ponto de parada limpo.

## Decisions (and why)
- **O tropeço 6 da FP-03 estava mal diagnosticado.** Não era "bloco inconsistente" — a DB
  exportava; estava *modificada-não-compilada*. O guard proposto no relatório (recusar bloco
  inconsistente) não dispararia. O conserto é a coreografia conferir depois do import.
  Corrigido em `docs/teste-cego/resultado-FP-03.md` §5.6 e na fila do PLANO.
- **Um guard só, compartilhado** (`Ops.ImportAndProve`), em vez de patch por verbo: `*-db-member`,
  `add-call`, `delete-network` e `set-retain` usam a mesma rotina.
- **`add-call` monta rede incondicional** (EN no powerrail, sem contatos em série). Condição em
  série continua sendo clone de molde que já a tenha — gerar contato/negação a partir de expressão
  seria reimplementar o `LadConverter` dentro do verbo.
- **FlgNet montado como texto** e depois `XElement.Parse`, não nó a nó: é o mesmo XML que o Portal
  aceitou na FP-03, e o namespace v5 erra fácil na construção programática.
- **Descartados da fila** (registrado no PLANO): `tree` carregando assinatura de FB (custa export de
  todo FB para responder o que `list-interface` responde sob demanda) e `add-db-member --from-scl`
  (com o guard, a cadeia de `--like` deixou de quebrar).
- **Aceite de `add-call` foi ao vivo, no bloco real, e revertido**: rede inserida no
  `PARTIDA_AGITADOR_5 (AG-05)` com `--after 0`, compile 0 erros, `delete-network --index 1`,
  compile 0 erros, `explain-block` de volta às 10 redes originais. `diff-block` contra o XML do
  `make_fc.py` dá `identical: false` — **isso é esperado** (o golden é pré-import; o Portal
  renumera UId no re-export), não é sinal de regressão.

## Next steps (ordered)
1. Commitar `.handoff/archive/2026-08-11T081411.md` (e este `active.md`) — caminhos explícitos,
   nunca `git add -A`.
2. Escolher a próxima frente. Candidatos, sem ordem imposta:
   - **FP-04** (novo caderno cego) já com os 4 verbos novos na mão — mede se a R8 deixou mesmo de
     custar sessão, que é a hipótese que esta fila comprou.
   - `add-call` com condição em série (contato/negação), se a FP-04 mostrar que faz falta.
   - Gate de publicação da skill (F4 do PLANO).
3. Se for FP-04: caderno novo em `docs/teste-cego/`, e quem escreve o caderno não executa.

## Key files
- `docs/PLANO.md` — fim do arquivo: seção "Fila da FP-03 executada (2026-08-11)" com o que virou o quê.
- `docs/teste-cego/resultado-FP-03.md` — os 10 tropeços; §5.6 tem o mecanismo corrigido.
- `src/Tia.Core/BlockEdit.cs` — `add-call`/`delete-network`/`set-retain` + núcleos puros.
- `src/Tia.Core/BlockInterface.cs` — `list-interface` + `FromXml`, que é de onde o `add-call` tira
  tipo e seção de cada pino.
- `src/Tia.Core/Ops.cs:ImportAndProve` — o guard; `Ops.Squash`/`FbsLike` — nome aproximado de FB.
- `src/__navi__.md` — símbolos por arquivo, regenerado (`pwsh scripts/navi-cs.ps1`).
- `docs/VERBS.md` — 76 assinaturas, gerado pelo `rebuild.ps1`.
- `workspace/ag05/` (gitignored) — molde, FC gerado e `make_fc.py`: é o oráculo se `add-call`
  precisar de mais formas de rede.

## Open / blockers
- Nenhum bloqueio. Dois avisos que valem para a próxima sessão:
  - `rebuild.ps1` muda o hash do `tia.exe` → com o Portal aberto, a primeira chamada pode pendurar
    num diálogo modal de autorização na tela (alguém precisa clicar).
  - Verbo com caminho relativo resolve contra o **cwd do processo**, não contra a raiz do repo
    (dois `diff-block` falharam assim no aceite). Passar caminho absoluto em `run --script`.

## Skills
- tia

## Effort
**Baixo** para o passo 1 (commit mecânico). Se a escolha do passo 2 for FP-04, a sessão de execução
é **alta** por natureza (teste cego paga descoberta), mas quem só *escreve* o caderno fica em médio.
Nada aqui é limitado por raciocínio: o relógio é do Portal, ~10-20 s por chamada `tia`.
