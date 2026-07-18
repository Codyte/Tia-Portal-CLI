# PLANO — TIA Portal Openness API (V19+)

> Fonte de verdade do projeto. Toda sessão começa lendo este arquivo + `__navi__.md`.
> Atualizar a tabela de fases ao fim de cada sessão de trabalho.

## Objetivo

CLI .NET (`tia`) que expõe operações Openness do TIA Portal V19+ como verbos com
entrada/saída JSON — consumível por agentes IA (Claude via shell) e engenheiros.
Extraído dos scripts provados em `Scripts_Siemens/FINAIS/`.

## Decisões travadas (mudar só com motivo forte)

| # | Decisão | Motivo |
|---|---------|--------|
| D1 | **CLI primeiro, MCP depois (talvez nunca)** | Claude Code roda shell local — CLI JSON já é consumível. MCP só se surgir uso remoto/claude.ai. |
| D2 | **1 exe único, multi-verbo** (`tia <verbo>`) | Whitelist do firewall Openness é por exe — 1 exe = 1 autorização. |
| D3 | **net48 / x64** | Openness V19 = .NET Framework 4.8. `Siemens.Engineering.dll` resolvida do diretório de instalação via `AssemblyResolve` — DLL da Siemens **nunca commitada** (licença). |
| D4 | **Attach preferido; abrir também suportado** (revisado a pedido do user) | Attach = padrão provado. `open-project --file X [--no-ui]` inicia portal (headless opcional) e abre projeto; `save-project`/`close-project [--save]` fecham o ciclo. Só single-user (Multiuser: check-in via TIA). |
| D5 | **Código e CLI em inglês; docs em PT** | Publicação GitHub futura. Decidido agora pra evitar rework. |
| D6 | **XML roundtrip = primitiva central** | Export → transformar → import. Todo verbo de alto nível constrói sobre isso. |
| D7 | **Read/write separados; write com `--apply`** | Verbos de leitura livres. Verbos de escrita rodam dry-run por padrão e só executam com `--apply`. Agente não estraga projeto por ruído. |
| D8 | **Sem operações online no v1** | Nada de download/go-online/commit Multiuser via API. Projeto offline + compile apenas. Humano faz check-in no TIA. |
| D9 | **1 chamada por vez** | Openness não é thread-safe pra esse uso. Nunca paralelizar chamadas `tia` (nem via agentes). |

## Delimitações — o que a API NÃO é

- Não gera lógica de automação por IA — expõe operações; a inteligência fica no agente que a usa.
- Não controla PLC online (D8).
- Não gerencia o TIA (abrir/fechar/instalar) — pressupõe TIA aberto com projeto carregado.
- Não abstrai o XML Siemens em modelo próprio no v1 — entrega/aceita o XML nativo no workspace.

## Arquitetura

```
src/
├── Tia.Core/          lib: sessão (attach, resolve projeto/PLC/HMI), XML export/import,
│                      compile, inventário, helpers (natural sort, alocador de endereços)
└── Tia.Cli/           exe único: parse de verbos, JSON out, exit codes
workspace/             exports XML transitórios (gitignored)
```

Contrato CLI:
- stdout = JSON (resultado ou `{"error": ...}`), stderr = log humano, exit 0/1.
- Zero prompt interativo. Config por argumento ou arquivo JSON passado por caminho.

Verbos por fase (nomes finais definidos na F1):
- **Leitura:** `info`, `list-devices`, `list-blocks`, `list-tags`, `export-block`, `export-tagtable`, `export-screen`
- **Escrita:** `import-block`, `import-tagtable`, `compile`, `create-tags`
- **Portados dos FINAIS:** `gen-profinet`, `standardize-tags`, `replicate-fc`, `gen-fault-ob`,
  `gen-alarm-fc`, `replicate-instruments` — as 3 variantes "Replicador de FC" NÃO foram unificadas:
  algoritmos distintos (replicação por pasta / bits-to-word / replicação por instrumento) → 3 verbos.
  Desvios deliberados dos originais: sem compile automático (verbo `compile` separado), sem menu
  interativo (dry-run + `--apply`), sem cache de template em disco (template deve existir no projeto).

## Fases

| Fase | Entrega | Critério de pronto | Status |
|------|---------|--------------------|--------|
| F0 | Este plano + CLAUDE.md do repo | commitado | ✅ |
| F1 | Solução .NET, Tia.Core mínimo, verbos de leitura | `tia info` e `tia list-blocks` rodando contra TIA real | ✅ smoke V21 2026-07-17 (info/list-devices/find/snapshot/xref ok) |
| F2 | Export/import XML + compile | roundtrip de 1 FC sem diff + compile ok | ✅ smoke V21 2026-07-17: round-trip FC_SmokeLad identical, compile 0 erros |
| F3 | Portar os 4 tools dos FINAIS como verbos | paridade com os scripts originais em projeto de teste | ✅ 6 verbos com smoke ok contra SmokeTest_01 (gen-profinet, standardize-tags, gen-fault-ob, replicate-fc, gen-alarm-fc, replicate-instruments — dry→apply→compile 0 err→re-run idempotente); code-review dos 6 ports vs FINAIS ✅ 2026-07-18: paridade de lógica ok, 0 bugs; 2 ressalvas menores de error-handling (Standardize rebuild e FaultOb import sem try/catch por item — original avisava e continuava) |
| F3.5 | Melhorias pré-projeto-real (backlog handoff itens 1-3) + banho de projeto real Fase A/B | robustez por-item, idempotência alarm-fc, verbo `doctor`, achados documentados | ✅ 2026-07-18: itens 1+2+3 aplicados e smoked; `tia doctor` novo (preflight read-only, 6 verbos); `Ops.BlocksIdentical` normaliza namespace+Informative; fix pastas TIA com `/` literal (Replicate/Doctor); Fase A/B contra cópia `Automação ETE SG AsBuilt_1_V21` → 8 achados em `docs/projeto-real-fase-A.md` (viram backlog de adaptação); testes offline `Tia.Tests` (console assert, sem TIA): 31 asserts sobre BuildFcXml/BuildCallObXml/BuildObXml/BuildAreaFcXml/LadConverter vs fixtures `docs/examples/` — ALL PASS 2026-07-18 |
| v2 | Backlog de cobertura Openness (itens 1-10 abaixo) | verbos compilando 0 erros | 🟡 código 100% offline; smoke V21 core ok (add-device/set-address/connect-subnet/create-folder/import-tags/import-source/import-ladder/compile/export/diff/delete/save); 9 (online) bloqueado por D8; smoke 2026-07-18 contra projeto real (read-only): export-tags/list-types/export-type/xref/export-cax ✅, list-hmi erro claro (projeto sem Unified); smoke mutação 2026-07-18 no SmokeTest_01 ✅: import-type (dry override→apply), import-cax (AML 1.7MB do real; fix: sem ExclusiveAccess — Openness proíbe), gen-alarm-fc callOb=in-sync (idempotência total) |
| F3.6 | Macros de fluxo (itens 1-4 da lista aprovada) | smoked contra SmokeTest_01 | ✅ 2026-07-18: `prep-project.ps1` (use-project+doctor+compile+save), `raio-x.ps1` (banho read-only → workspace/<proj>/, xref de todos os OBs), `clone-hw.ps1` (CAx A→B, dry por padrão, -Apply salva), `docs/examples/gen-all.json` (6 verbos FINAIS dry via `tia run`, attach 1x). Macros 5-7 (new-area/sync-check/adopt-project) só se user pedir. |
| F4 | Polimento p/ GitHub (README EN, licença, exemplos) | repo publicável | ⬜ |
| F5? | MCP server fino sobre Tia.Core | só se D1 cair | ⬜ |

Regra: **uma fase por vez, commit + handoff no fim de cada uma.** FINAIS vira referência
read-only — nunca editar lá; extrair pra `src/` e pronto.

## Verificação (cada fase)

- TIA real precisa estar aberto → smoke test é semi-manual: eu rodo `tia <verbo>` via shell
  com você confirmando que o TIA está de pé com **projeto de teste** carregado.
- **Nunca desenvolver contra projeto de produção.** Criar projeto TIA descartável de teste
  (1 PLC, meia dúzia de blocos) antes da F1.
- Lógica pura (parsers, sort, alocador) ganha 1 teste rodável sem TIA.

## Economia de tokens (regras da sessão)

1. **Início de sessão:** ler `docs/PLANO.md` + `__navi__.md` — nada de reler histórico ou FINAIS inteiros.
2. **`/handoff` + `/clear`:** no fim de cada fase, ou contexto >~150k. Estado persistente vive
   nos arquivos (este plano + código), não na conversa — handoff fica barato.
3. **Sem spawn de agentes por padrão.** Repo pequeno + navindex = navegação direta. Exceção
   única: `cavecrew-investigator` pra varrer massa de XML exportado desconhecida. Workflows/
   ultracode: não.
4. **Leitura cirúrgica:** FINAIS já foram analisados; extrair por faixa de linha via navi, não reler.
5. **navindex regen** após mudança estrutural em `src/` (hook pre-commit instala isso).
6. Saída de build/test filtrada (`| Select-Object -Last`), nunca dump completo.

## Skills em uso (nada novo pra instalar)

| Skill | Quando |
|-------|--------|
| navindex | regen após mudanças estruturais; hook pre-commit |
| handoff | fim de fase / contexto grande |
| verify | após mudança não-trivial com superfície executável |
| caveman:caveman-commit | commits |
| code-review | fim de F2 e F3 (pontos de maior risco) — não a cada diff |
| ponytail/caveman | ativos, permanentes |

## Ambiente (descoberto na F1)

- **Esta máquina = titanxnexus** (servidor: TIA Project Server, TIA Administrator, WinCC Unified RT).
  Usuário acessa de pcprojetos5 via VSCode Remote. TIA Portal V19 foi desinstalado daqui;
  **usuário vai reinstalar nesta máquina** — build e execução serão ambos aqui. Até lá, só código.
- Build: .NET SDK 8 (instalado 2026-07-17) compilando net48/x64. `lib/Siemens.Engineering.dll`
  (v19.0.0.0, cópia local, gitignored) é referência de compile; em runtime o exe resolve a DLL
  da instalação real (env `TIA_ENGINEERING_DIR` → pasta do exe → Portal V21/V20/V19 padrão;
  V21+ = assemblies separadas Base/Step7/WinCCUnified em `PublicAPI\V21\net48`).
- Deploy do smoke: copiar `src\Tia.Cli\bin\Release\net48\` (tia.exe + Newtonsoft.Json.dll +
  Tia.Core.dll) pra máquina do TIA e rodar lá.
- **Gates Openness V21 (resolvidos 2026-07-17):** (1) user no grupo Windows "Siemens TIA
  Openness" + **logon novo** (token velho não pega o grupo — logoff/logon do RDP); (2) whitelist
  registro `HKLM/HKCU\...\Openness\21.0\Whitelist\tia.exe\Entry` (Path + DateModified
  `yyyy/MM/dd HH:mm:ss.fff` do LastWriteTime + FileHash SHA256-Base64) — `scripts/whitelist.ps1`
  gera certo; re-rodar após rebuild (hash muda); (3) client Openness precisa rodar na **mesma
  sessão interativa** do TIA UI (task S4U/sessão 0 não attacha); (4) TIA e client ambos com
  token fresco. Licença STEP 7 necessária pra add-device (LicenseNotFoundException sem ela).

## Backlog v2 (cobertura Openness — priorizado)

1. ~~**Fontes externas**: `import-source`~~ ✅ feito (`tia import-source --file X.scl [--apply]`;
   ext .scl/.awl/.st/.db/.udt; exemplo em `docs/examples/example.scl`). Smoke pendente.
1b. ~~**Conversor SCL→LAD**: `import-ladder`~~ ✅ feito (`tia import-ladder --file X.scl [--name N]
   [--folder A/B] [--apply]`). Subset: bool AND/OR/NOT/parênteses, comparadores, IF→Set/Reset/Coil,
   MOVE de literal. Rejeita FOR/WHILE/CASE/aritmética/#locais com erro claro. Dry-run gera XML
   **sem TIA** (testado offline ✅); import real smoke pendente — risco: detalhes FlgNet (nomes de
   porta de comparador/MOVE, SrcType DInt default) escritos de memória, validar no primeiro smoke.
   Exemplo: `docs/examples/ladder.scl`. Fora do V1: FB calls (TON/CTU), edge, copy tag→tag.
2. ~~**Estrutura**~~ ✅ feito (`create-folder`/`delete-folder --path A/B [--tags]`,
   `delete-block --name X`, `export-type`/`import-type` p/ UDT). Dry-run conta conteúdo antes
   de deletar. Smoke pendente — risco: `PlcBlockUserGroup.Delete()`/`PlcTagTableUserGroup.Delete()`
   e `plc.TypeGroup.Types.Import` de memória, validar no primeiro smoke.
3. ~~**Hardware**~~ ✅ feito (`add-device --mlfb X --name N [--station S]`,
   `set-address --device X [--ip] [--mask] [--pn-name]`, `connect-subnet --device X --subnet S
   [--io-system IO]` — controller cria IO-system, IO device entra num existente;
   `export-cax`/`import-cax` AML). Smoke pendente — risco: atributos Node ("Address",
   "PnDeviceName", "PnDeviceNameAutoGeneration") e CreateIoSystem de memória.
4. ~~**Compile granular + diff-block**~~ ✅ feito (`compile [--block X | --folder A/B]`,
   `diff-block --file F.xml [--name X]` read-only). `BlocksIdentical` movido pra `Ops`
   (param ignoreComments; AlarmFc=true, InstrumentFc=false — comportamento preservado).
5. ~~**Inspeção**~~ ✅ feito (`find --pattern P* [--kind block|table|tag|type]` wildcard,
   `list-types`, `snapshot` inventário completo, `xref --name BLOCK` via CrossReferenceService —
   API compilou contra DLL real; formato do resultado validar no smoke).
6. ~~**Batch**~~ ✅ feito (`tia run --script ops.json` — JSON array de arg-arrays, attach 1x;
   falha para no passo com erro; `run`/`open-project` proibidos como step.
   Exemplo: `docs/examples/batch.json`).
7. ~~**Robustez**~~ ✅ feito (exit codes: 0 ok, 1 geral, 2 uso, 3 arquivo, 4 TIA/Openness
   (inclui DLL ausente), 5 timeout; `--retry N` em "busy" com backoff linear, default 3;
   `--timeout SEC` via Task.Wait — abandona a chamada, processo sai). Testado offline: 2/4 ok.
8. ~~**Libraries**~~ ✅ feito (`list-library --file X.al19` — master copies + types;
   `import-master-copy --file X.al19 --name M [--folder A/B]` via Blocks.CreateFrom;
   read-only Open a cada verbo). Fora do V1: instanciar library *types* (workflow de
   versão/instância bem mais complexo — adicionar se precisar).
9. **Online (revoga D8 — só com decisão explícita)**: go-online, download, compare online/offline,
   start/stop CPU, watch tables.
10. ~~**HMI Unified**~~ ✅ parcial (`list-hmi [--device X]` — telas + tag tables; API HmiUnified
    compilou contra DLL real). **Limite de plataforma**: Openness V19 não exporta/importa telas
    Unified como XML (sem SimaticML pra Unified) — export/import de telas fica fora até a Siemens
    expor; tags HMI editáveis via objetos dinâmicos, adicionar verbo se precisar.

## Pendências / decisões futuras

- Licença (MIT provável) — decidir na F4.
- Nome público do repo — F4.
- Smoke F1 na máquina do TIA (user leva o exe; primeira execução dispara popup Openness — permitir).
