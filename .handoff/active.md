# Handoff · TIA Portal Openness API · 2026-08-10 (4ª sessão do dia)

## Goal
Fechar os defeitos abertos que a rodada FP-02 deixou. **Os três que estavam em aberto e eram
acionáveis foram fechados** — gate de BOM, `run --script` e a cobertura offline de pasta. Nada
mid-flight.

## State
- HEAD: `38c49d5`, **pushado** (`origin/main` igual). Working tree limpo fora do `.handoff/`.
- Live state: **TIA Portal aberto** (sessão 1) com `workspace/blind/FP02/FP02.ap21` aberto e salvo;
  nenhuma escrita nova no projeto — todas as rodadas desta sessão foram dry ou read-only. Shell do
  agente na sessão 0 (rota da task `TiaSmokeRun`). `tia.exe` rebuildado 3× aqui: **o Portal aberto
  abre diálogo modal de autorização a cada hash novo**, e ninguém precisa clicar até a próxima
  chamada `tia` — 2 rodadas morreram no timeout de 600 s por isso.
- Done nesta sessão: (1) gate de BOM no `import-source` (`06b478b`), (2) `run --script` valida antes
  do attach + mensagem que ensina a saída (`b6e3bd9`), (3) `WalkFolders` e o filtro de
  `list-blocks --folder` com teste offline (`38c49d5`); mais `20ae2f0` de doc. Suíte: `ALL PASS` +
  3 checks de CLI/init.
- In progress: nada.

## Decisions (and why)
- **Attach lazy no `run --script` foi descartado.** Abrir projeto custa 2-4 min e a chamada solta a
  mais custa ~7 s: o ganho seria ruído sobre um refactor do `Run()`. O que faltava era **ensinar** —
  a mensagem de fail-fast agora diz o porquê e manda chamar `open-project`/`create-project` (ou
  `use-project.ps1`) antes. Limitação documentada no `CLAUDE.md`, no help do `batch` e no `VERBS.md`.
- **A validação do script saiu de dentro do `using (Attach())` para antes dele** — erro de uso não
  custa mais sessão nem exige portal, e é isso que tornou o caso testável offline (check novo no
  `rebuild.ps1`, que falha se a validação voltar pra dentro do `using`).
- **Gate de BOM recusa, não reescreve.** Regravar a fonte do usuário por baixo é pior que erro claro;
  ASCII puro continua passando sem BOM (não exigir BOM à toa). Reescrever automático só se aparecer
  gerador que não controla o encoding.
- **Nada de mock de `PlcSoftware` para testar pasta.** `WalkFolders` virou `public` e os delegados
  `find`/`create` que já existiam bastaram para caminhar uma árvore de nós em memória; o predicado do
  filtro saiu para `Inventory.FolderMatches` sem mudança de regra.
- **Sessão cega FP-03 adiada, com o usuário de acordo.** A cegueira mede a documentação de entrada, e
  a doc mudou em 3 pontos hoje; nenhum dos 14 defeitos da FP-02 dependeu de cegueira. Quando for,
  **cegar pequeno**: caderno de uma tarefa só, não planta inteira.

## Next steps (ordered)
1. **Varrer `import-tags` / `import-block` / `import-type` atrás do mesmo mojibake que o gate de BOM
   pegou.** XML carrega declaração de encoding, então provavelmente estão limpos — mas isso é uma
   verificação, não uma suposição. Se estiverem, registrar no resultado da FP-02 que o gate é
   específico de fonte SCL/AWL.
2. `use-project.ps1` / `prep-project.ps1` exigem caminho absoluto (o `Test-Path` roda num pwsh filho
   que não nasce na raiz do repo, e caminho relativo cai calado no ramo de nome curto, procurando em
   `proj\`). Continua aberto no resultado da FP-02; o trap está no `standing.md`.
3. FP-03 cega, quando fizer sentido — sessão nova, sem handoff, só com o caderno e o `SKILL.md`.

## Key files
- `src/Tia.Core/Ops.cs` — `RequireUtf8Bom` (o gate) e `WalkFolders` (agora `public`, longest-match).
- `src/Tia.Core/Inventory.cs` — `FolderMatches` (predicado do `--folder`).
- `src/Tia.Cli/Program.cs` — `ParseScript` (validação do batch, antes do `Attach`).
- `scripts/rebuild.ps1:44-54` — os checks offline de CLI (`--out-file`, `run --script`).
- `src/Tia.Tests/Program.cs` — `Ops_RequireUtf8Bom`, `Ops_WalkFolders`, `Inventory_FolderMatches`.
- `docs/teste-cego/resultado-2026-08-10.md` — achados 12 e 13 + a seção "Aberto" (2 itens restantes).
- `src/__navi__.md` — **regenerado ao fim desta sessão** (`pwsh scripts/navi-cs.ps1`), já com
  `RequireUtf8Bom`, `WalkFolders`, `FolderMatches` e `ParseScript`.

## Open / blockers
- `use-project.ps1` exige caminho absoluto (passo 2).
- Nenhum blocker técnico. O único atrito é operacional: `rebuild.ps1` com o Portal aberto pendura a
  próxima chamada `tia` até alguém autorizar na tela.

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo** para o passo 1 — é varredura de leitura em 3 verbos que já existem, com o padrão de
comparação pronto (`RequireUtf8Bom`), e o piso é ler quem chama cada um antes de concluir. Sobe pra
**médio** se algum dos 3 realmente aceitar XML sem declaração de encoding, porque aí a correção não é
copiar o gate: XML tem regra própria (declaração > BOM) e recusar por BOM ausente daria falso
positivo. O relógio não é gargalo de raciocínio: cada chamada `tia` custa ~10-20 s e um
`rebuild.ps1` com o Portal aberto pede clique na tela.
