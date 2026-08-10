# Handoff · TIA Portal Openness API · 2026-08-10 (5ª sessão do dia)

## Goal
Fechar o que sobrou da rodada FP-02, revisar 56 handoffs atrás de pendência perdida e varrer os
verbos em dry contra um projeto real. Tudo fechado. O próximo passo é o único caminho ainda não
exercitado fora do `PLC_TESTE`: **`install-lib` num PLC zerado**.

## State
- HEAD: `5087a1c`, pushado. Working tree limpo fora do `.handoff/`.
- Live state: **TIA Portal aberto** (sessão 1, PID 21440/22212) com
  `workspace/blind/FP02/FP02.ap21` — PLC `CPU_EEB02`, 97 blocos, 8 devices (1 CPU + 2 ET200SP +
  5 G120-2). **Nada foi escrito no projeto nesta sessão**: as 3 varreduras foram dry ou read-only.
  Shell do agente na **sessão 0** (rota da task `TiaSmokeRun`). `tia.exe` rebuildado 1× aqui — o
  diálogo modal de autorização já foi aceito pelo user, então a whitelist está quente.
- `.al21` presente: `src/Tia.Lib/tia-cli/tia-cli.al21`, 151 KB, **21 master copies** (5 pacotes +
  5 blocos base + 4 moldes + 7 extras: 5 UDT e 2 tabelas de tag).
- Done: 2 defeitos da FP-02 (gate de BOM verificado como específico de SCL/AWL; `use-project.ps1`
  com caminho relativo), revisão dos 56 handoffs, varredura dry de 64 steps, 2 defeitos novos
  achados e corrigidos. Saída da varredura em `workspace/sweep/` (gitignored).
- In progress: nada.

## Decisions (and why)
- **O gate de BOM não vai para os verbos de XML.** `import-block`/`import-tags`/`import-type`
  carregam o arquivo com `XDocument.Load` **antes** do `if (apply)`, então o encoding vem da
  declaração do próprio XML (declaração > BOM > UTF-8 por spec) e byte incompatível vira
  `XmlException` no dry, alto e na causa. Aplicar `RequireUtf8Bom` ali daria **falso positivo** em
  arquivo que declara o próprio encoding e está correto. O gate é específico de fonte SCL/AWL.
- **O dry-run passou a dizer `folderAction: create|reuse`** em vez de ganhar um guard que recusa
  caminho parcial. Recusar quebraria o uso legítimo (criar pasta nova de propósito); o problema era
  o dry ser **cúmplice do silêncio**, não a criação em si.
- **Retirado do `standing.md`**: a entrada de 4 linhas sobre caminho absoluto nos macros virou 2
  linhas de regra para script novo — o trap foi corrigido, o que resta é a orientação.
- Verificado e **não** mexido: `set-attr` aceita no dry mudar atributo possivelmente read-only
  (`TypeName`: `"S7-1500 station"` → `"x"`); confirmar exigiria um `--apply` de verdade.

## O que a varredura dry achou (64 steps, 3 rodadas)
1. **`import-*` não avisava que ia criar a pasta** — `--folder "9.9 Nao Existe"` respondia
   `action: override, applied: false`, sem uma palavra sobre a árvore que seria criada a partir da
   raiz. Era a armadilha que só existia num handoff arquivado. Corrigido nos 3 verbos
   (`folderAction`), provado no FP02 nos dois sentidos, documentado no `CLAUDE.md`.
2. **`list-server-projects` como step de batch dava `Unknown verb`** — o verbo roda antes do attach
   (não precisa de projeto), então o dispatcher do batch não o conhece. Entrou no fail-fast de
   `open-project`/`create-project`, com o motivo na mensagem.
3. Ruído do meu próprio script (7 "erros") **virou resultado bom**: apagar o que o dry não criou
   devolve mensagem que ensina — `delete-device` lista os 8 devices, `edit-db-member` lista os 6
   membros de `Static`. Nenhum verbo engasgou, nenhum stack trace vazou.
4. Menores, registrados e não consertados: `insert-telegram` devolve exceção crua no campo
   `driveObject` (`get_DriveObjectNumber`) e mesmo assim funciona; `delete-type` não diz no dry quem
   referencia o UDT.

## Next steps (ordered)
1. **`install-lib` num PLC zerado do FP02** — o caminho biblioteca→projeto nunca rodou fora do
   `PLC_TESTE`. **Antes de criar o PLC**: (a) `pwsh scripts/prep-project.ps1` não serve aqui (quer
   nome/caminho e o projeto já está aberto) — rodar `tia compile --apply` + `save-project`, porque
   o `install-lib` importa e o Openness recusa exportar bloco inconsistente; (b) conferir com o user
   se pode escrever no FP02 (é sandbox de teste cego, mas a rodada anterior está registrada em
   `docs/teste-cego/resultado-2026-08-10.md`); (c) `tia add-device --mlfb "6ES7 515-2AN03-0AB0/V3.1"
   --name PLC_ZERO_FP02 --apply`. Só então `pwsh scripts/install-lib.ps1 "<pacotes>" -Plc
   PLC_ZERO_FP02 -Apply`. Régua conhecida (`PLC_TESTE`, 2026-08-07): 35 blocos, **nenhuma pasta
   `_1`**, compile Success/0; 2ª instalação = `já presentes (pulados): 10`.
2. `pwsh scripts/init.ps1 -Check` — read-only, nunca lido com olho crítico: interessa se a mensagem
   sobre a `.al21` gitignored é clara pra quem clonou limpo.
3. **FP-03 cega** — sessão nova, sem handoff, só com o caderno e o `SKILL.md`. Não é executável de
   uma sessão que já tem contexto. Cegar pequeno: caderno de uma tarefa só.

## Key files
- `src/Tia.Core/Ops.cs` — `FolderAction` (novo), `ImportBlock`, `ImportTagTable`, `ImportSource`,
  `RequireUtf8Bom`, `WalkFolders`.
- `src/Tia.Cli/Program.cs:294` — `ParseScript`, o fail-fast dos verbos pré-attach.
- `scripts/install-lib.ps1` — o passo 1; `scripts/use-project.ps1:8-12` — resolução de caminho.
- `workspace/sweep1.json` / `sweep2.json` / `sweep3.json` — as 3 rodadas da varredura, reexecutáveis.
- `docs/teste-cego/resultado-2026-08-10.md` — os 13 achados da FP-02, seção "Aberto" zerada.
- `docs/PLANO.md` — biblioteca (L259) e bake (L518) destalados nesta sessão.
- `src/__navi__.md` — **desatualizado**: `FolderAction` é novo. Regenerar com
  `pwsh scripts/navi-cs.ps1` antes de navegar `src/`.

## Open / blockers
- Escrever no FP02 (passo 1) precisa do seu ok — o projeto guarda o resultado da rodada FP-02.
- `rebuild.ps1` com o Portal aberto reabre o diálogo modal de autorização: chamada pendurada com
  CPU ~0 = alguém precisa clicar. Bateu 1× nesta sessão (`Security error … timed out`).
- Pendências antigas que continuam de pé: baseline manual dos benchmarks (só você cronometrando),
  os 21 warnings da FP-01 nunca lidos um a um, `init.ps1 -Check` em máquina limpa de verdade.

## Skills
- tia
- ponytail
- caveman

## Effort
**Baixo-médio** para o passo 1: a sequência é documentada e a régua existe, mas é o primeiro
`--apply` desta série de sessões — o piso é ler `install-lib.ps1` antes de rodar, não só executar.
Sobe pra **alto** se aparecer pasta `_1` no PLC novo (seria regressão do fix do `--force`, que só o
`-Update` exercita). Raciocínio não é o gargalo: `install-lib` num PLC virgem é minutos de Portal, e
cada chamada `tia` custa 10-20 s.
