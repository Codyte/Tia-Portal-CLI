# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
**F7 — camada de compreensão.** Transformar a CLI (44 verbos de ação) em ferramenta que a IA
usa pra *diagnosticar* ("problema no acionamento BH-01A" → mapear tudo e alterar o necessário) e
pra *criar projeto a partir de documentos*. Gargalo real: não é o que a CLI escreve, é o que a IA
consegue **ler** dentro do orçamento de contexto (1 FC em LAD = ~200KB de XML).

## State
- HEAD: 10944f7 — working tree limpo (só `.handoff/` desta escrita).
- Done nesta sessão: **F6 fechada** (`scripts/_common.ps1` → `Invoke-Tia` roteia por sessão;
  `scripts/tia.ps1` = comando único, substitui `tia-task.ps1` removido; macros migrados;
  `prep-project` ganhou `-Apply`; bugs 2-5 da auditoria fechados — bug 1 já estava).
  Verificado: `tia.ps1 doctor` exit 0, rota da task (`TIA_VIA_TASK=1`) exit 0, forma legada
  `["info"]` exit 0, `use-project`/`prep-project` do shell do agente, `rebuild.ps1` ALL PASS.
- In progress: nada mid-flight. Plano das 5 melhorias apresentado ao user, **aguardando aceite
  do par 1+2 e o veredito do D8 read-only**.

## Decisions (and why)
- **IA escreve a *spec*, nunca o XML.** Docs → `plant.json` (extração é trabalho do LLM) → CLI
  valida schema e aplica determinístico/idempotente. XML gerado por LLM é onde projetos assim quebram.
- **Leitura antes de escrita.** 1+2 são read-only, risco zero no projeto real; a IA hoje já
  *pode* mutar, o que falta é entender o que está mutando.
- **`checkpoint` antes de `--apply` autônomo.** `dry-run` protege contra ruído, não contra decisão
  errada aplicada e salva.
- Achados de PowerShell da F6 (valem pra qualquer script novo): `-is [pscustomobject]` é verdadeiro
  até pra `[string]`; splat de array vazio vira argumento `""` pro CLI.

## Next steps (ordered)
1. **`explain-block --name X`** — XML LAD/FBD → texto compacto (redes numeradas, contatos/bobinas/
   comparadores, chamadas de FB, comentários). ~200KB → ~3KB. Inverso do `LadConverter` que já
   existe (SCL→LAD), formato do `FlgNet` já mapeado. **Offline, testável em `Tia.Tests` sem TIA.**
2. **`trace --equipment BH-01A`** — vizinhança semântica em 1 chamada: tags %I/%Q/%M do símbolo,
   membro do DB global (instância do UDT), iDBs, FCs que referenciam, word de alarme, OB que chama,
   endereço físico, pasta. Hoje = ~15 chamadas. `xref` só aceita bloco, não tag → índice invertido
   próprio (mais barato e offline-testável que forçar `CrossReferenceService` por tag).
3. **`index` cacheado** → `workspace/<proj>/index.json` (tag→blocos, membro de DB→usuários,
   bloco→chamadas), invalidado por hash/contagem; `trace` lê o índice. **Só quando 2 doer** no
   projeto real (1011 blocos).
4. **`checkpoint` / `restore`** — `export-block` dos blocos no escopo → `workspace/checkpoint/<ts>/`;
   restore = `import-block` override. Cirúrgico, reusa verbos existentes, sem arquivar projeto inteiro.
   Pré-requisito de qualquer `--apply` autônomo.
5. **`apply-spec --file plant.json`** — orquestrador + schema; compõe `scaffold` + `clone` +
   `import-tags` + `add-db-member`, idempotente. Tudo já existe, falta a casca.

Backlog anterior (não perdido, entra depois): `import-ladder --apply` contra um `PARTIDA_*` real
(FlgNet foi escrito de memória, `--apply` nunca rodou); `replicate-fc --apply` no ScaffoldTest;
bytes de system/clock memory no `scaffold`/`add-device` (8 dos 26 erros de compile); multiuser 3b/3c.

## Key files
- `src/Tia.Core/LadConverter.cs` — SCL→LAD; ponto de partida do `explain-block` (inverso).
- `src/Tia.Cli/Program.cs` — `Dispatch` (44 `case`), onde entram os verbos novos.
- `src/Tia.Core/Ops.cs` — `BlocksIdentical`, `RequireRootType`, `EnsureCultures`, `ResolveFolder`.
- `src/Tia.Tests/Program.cs` — console assert offline; 1+2 devem nascer com teste aqui.
- `scripts/_common.ps1` — `Invoke-Tia` (rota por sessão, `TIA_VIA_TASK=1`, `TIA_TIMEOUT`).
- `docs/PLANO.md` — F6 ✅ + achados; tabela de fases; D1-D9.

## Open / blockers
- **Decisão do user pendente: revogar D8 só pra leitura online** (diagnostic buffer, compare
  online×offline, watch de valores; download/start/stop seguem proibidos). Sem isso, "problema no
  acionamento" só resolve o que é lógica/config offline — metade do objetivo fica de fora.
- Aceite do par 1+2 como F7 ainda não confirmado.
- Portal com **ScaffoldTest aberto**. Voltar ao de referência:
  `pwsh scripts/use-project.ps1 "Software de ETE Insular_Inicial_V21"` (2-4 min).
- Falta host/porta do TIA Project Server + projeto de teste lá (nunca produção) — trava multiuser.
