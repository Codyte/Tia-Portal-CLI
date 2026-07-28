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
| F4 | Polimento p/ GitHub (README EN, licença, exemplos) | repo publicável | ✅ 2026-07-18: LICENSE MIT, README EN completo (contrato dry-run/--apply, 3 gates Openness, tabela de verbos, macros, limitações), nome público decidido `tia-cli`. Publicação em si (gh repo create) pendente de ordem do user. **Gate de publicação (2026-07-28)**: nenhum payload de projeto de cliente entra no repo público — XML/AML exportado de projeto real carrega nome de equipamento, tag e estrutura de DB (`DB GLOBAL.xml` = 869 KB da planta), e publicar é irreversível na prática (fork, cache, índice). O que vai pro Git é autoral ou sanitizado (`clone --replace OLD=NEW`); payload fica gitignored e cada clone repõe o seu (`library/blocks/`, `workspace/`, `Scripts_Siemens/`, `proj/`). |
| F5? | MCP server fino sobre Tia.Core | só se D1 cair | ⬜ |
| F6 | Endurecer os scripts PS (ver seção "F6" no fim) | macros rodáveis do agente (sessão 0) + 5 bugs fechados | ✅ 2026-07-27: `scripts/_common.ps1` (`Invoke-Tia`, roteia por sessão, run-id, `$global:LASTEXITCODE`, timeout 600s, guard D9) + `scripts/tia.ps1` (comando único, substitui `tia-task.ps1` — removido); macros migrados; bugs 2-5 fechados (bug 1 já estava). Verificado end-to-end: `tia.ps1 doctor` exit 0, rota da task (`TIA_VIA_TASK=1`) exit 0, forma legada `["info"]` exit 0, `use-project`/`prep-project` do shell do agente |
| F7 | Camada de compreensão: a IA lê o projeto dentro do orçamento de contexto | `explain-block` (1) e `trace` (2) read-only; depois `index`, `checkpoint`, `apply-spec` | 🔄 item 1 feito 2026-07-27: `explain-block --name X \| --file F.xml` (LAD/FBD → texto; 92KB → 8,3KB no `BombaTemplateFc`; `--file` roda sem TIA, 9 asserts em `Tia.Tests`). Smoke `--name` ok 2026-07-28 no `Software de ETE Insular_Inicial_V21`: `Resets` 58KB → 4,6KB, `Paineis Intertravamento` 53KB → 4,9KB, `FC_ALARMES_PRELIMINAR_P_GM_01` 26KB → 2,2KB — chamadas de FB com pinos, expressões série/paralelo e comentários pt-BR corretos. Item 1 fechado. **Item 2 fechado 2026-07-28**: `trace --equipment X` smoked no mesmo projeto — `AG-01` = 39 símbolos + 39 usos em 10 blocos (`PARTIDA_AGITADOR (AG-01)`, `Resets`, `FB CONDIÇÃO DE PARTIDA`…), **10,1s total / 3,3s de xref, 131 blocos varridos**; cobertura conferida contra `xref --name Resets` independente. `xref` agora resolve bloco → tag → tabela → UDT (`ResolveSymbol`), então serve o sentido direto em qualquer símbolo. O "blocker do xref" do handoff anterior era **diálogo de autorização Openness pendurado na tela**, não custo de API — ver "Openness pede aceite na tela" abaixo. Índice invertido via export XML descartado: não há problema de performance a resolver. **Gargalos de consumo fechados 2026-07-28**: (a) `--out-file F.json` global — JSON completo no arquivo, stdout só `{file,bytes,count,head}`; guard no único `Print` por onde todo verbo sai, sem flag por verbo e sem mudar quem redireciona stdout (`raio-x.ps1`). Motivo medido: `find --pattern "*" --kind tag` = 821 KB / 4372 hits, `snapshot` = 7967 linhas — um verbo desses no contexto custa a sessão que o F7 existe pra proteger. Erro nunca vai pro arquivo. (b) `run --script` isola steps: `{ok:false,error,type}` por item, batch segue, `exit 1` se algum falhou — o batch só compensa se sobreviver à 1ª exceção (attach medido = **2,9s fixo**, não 7s: `info` solo 3,0s, `list-types` 2,9s, batch de 5 steps 7,0s). (c) **`tree` virou a leitura de orientação**: emite blocos + tabelas de tag + UDTs no mesmo `plc-navi.md` — 39 KB / 309 linhas p/ 476 blocos + 194 tabelas + 13 UDTs em 4,0s, contra ~150 KB do JSON equivalente. `snapshot` saiu do bloco "read" do help pro bloco "bulk"; `raio-x.ps1` roda `tree` primeiro e aponta o `plc-navi.md` como entrada. **`--format table` (TSV) foi medido e descartado**: 822 KB → 331 KB é 2x num problema que precisa de 30x — o que paga é agrupar (4,5x) ou não devolver volume (`trace` responde a pergunta inteira em 20 KB). Orçamento resultante: orientação ~10k tokens 1x por sessão, pergunta específica ≤5k, volume bruto nunca no contexto |
| F8 | Caminho de escrita exercitado contra projeto real (`--apply` de verdade, não dry) | cada verbo de escrita aplicado + `compile` 0 erros | 🔄 2026-07-28 no `Software de ETE Insular_Inicial_V21` (projeto de teste com backup; tudo em `ClaudeTest/`). **Primitivas 11/11 ✅**: `create-folder`, `import-block` (FC real de 90 KB), `import-tags`, `clone`, `export-type`→`import-type`, `import-source`, `add-db-member`, `delete-block`, `compile` — pasta compila Success/0 erros. **`import-ladder --apply` ✅** (2 bugs de FlgNet corrigidos, ver item 1b). **6 geradores ✅ em dry** (`gen-all.json`, 0 falhas) + payload de `gen-fault-ob` (OB de 88 KB) e `gen-alarm-fc` importado no sandbox → compile 0 erros → `explain-block` round-trip: o FlgNet desses builders já estava certo. **Pré-requisito descoberto**: `replicate-fc`/`gen-alarm-fc`/`replicate-instruments` falham com `Inconsistent blocks ... cannot be exported` se o PLC não foi compilado antes (eles exportam o GlobalDB) — `compile --apply` do PLC inteiro resolveu (projeto real: Success/0 erros, os 26 erros antigos já não existem). Guard novo em `Ops.ExportBlock` traduz essa mensagem. **Fechado 2026-07-28 (2ª sessão), escopado ao tipo `Soprador` na árvore de produção**: dry = 1 grupo, molde `Soprador 1 (S-01A)`, 2 alvos `overwrite` (S-01B/C — o projeto só tem 3 sopradores nessa pasta, não 6), 6 blocos cada, nada fora de `4. Motores/Bombas`. `--apply` exige **`--force`** quando a pasta-alvo já tem blocos (guard correto: sem ele o batch falha com `2 target folder(s) already have blocks…`). Batch `replicate-soprador-run.json` (save → apply → compile → apply → compile → save) = **0 falhas, os dois compiles Success/0 erros/0 warnings**. **Conteúdo conferido, não só compilação**: export de `PARTIDA_SOPRADOR_2 (S-01B)` e `_3 (S-01C)`, normalizando o ID de volta pro do molde, difere do template em **5 linhas de 1993** — `Created` (timestamp), `Number` (FC 151/152 vs 153), 2 `Component` de tag de IO (sufixo `_2`/`_3` do equipamento) e `ConstantValue` (301/302 vs 300); tudo o que o replicador deve reescrever, nada mais. Idempotência é *funcional*, não no-op: o 2º apply reimporta os mesmos blocos (o verbo não detecta in-sync) e o 2º compile recompila — resultado idêntico, 0 erros. **`gen-profinet --apply` + `standardize-tags --apply` ✅** no mesmo projeto: profinet 43 IO devices, 3 tags `exists` (no-op); tags 131 tabelas = 126 `ok` + 5 `rebuilt` (`SOPRADOR_TANQUE_AERACAO S-02A..E`); `compile --apply` depois = Success/0 erros/0 warnings + `save-project`. **Falta**: `replicate-instruments --apply` (dry dá `in-sync`, não escreveria nada), `scaffold`/`add-device` (bug dos bytes de system/clock memory), `import-master-copy` (sem `.al19` de teste) |

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
- **Openness pede aceite na tela (descoberto 2026-07-28, custou uma sessão inteira de diagnóstico
  errado).** Quando o Portal já está aberto e o `tia.exe` muda de hash (todo `rebuild.ps1`), o
  Portal usa a whitelist que leu ao iniciar e abre um **diálogo modal de autorização** na sessão
  interativa. Ninguém clica → toda chamada fica pendurada com **CPU ~0** e estoura o `TIA_TIMEOUT`,
  ou volta `EngineeringSecurityException: "Security error. The operation has timed out."`.
  Assinatura pra reconhecer: `tia info` (a chamada mais barata que existe) também trava — se
  `info` não responde em segundos, é ambiente, **nunca** custo do verbo. Não medir performance de
  API nesse estado: foi o que gerou o falso blocker "xref do Openness inviável" no handoff de
  2026-07-27. Cura: usuário clica no diálogo (não precisa reiniciar o Portal).

## Backlog v2 (cobertura Openness — priorizado)

1. ~~**Fontes externas**: `import-source`~~ ✅ feito (`tia import-source --file X.scl [--apply]`;
   ext .scl/.awl/.st/.db/.udt; exemplo em `docs/examples/example.scl`). ✅ smoke dry 2026-07-27.
1b. ~~**Conversor SCL→LAD**: `import-ladder`~~ ✅ feito (`tia import-ladder --file X.scl [--name N]
   [--folder A/B] [--apply]`). Subset: bool AND/OR/NOT/parênteses, comparadores, IF→Set/Reset/Coil,
   MOVE de literal. Rejeita FOR/WHILE/CASE/aritmética/#locais com erro claro. Dry-run gera XML
   **sem TIA** (testado offline ✅). ✅ **`--apply` validado 2026-07-28** no projeto real:
   `ladder.scl` → import → `compile` Success/0 erros → `export-block` → `explain-block` reproduz
   o SCL de origem. Dois defeitos que só o import revelava, ambos corrigidos:
   comparador usa `pre`/`in1`/`in2` (emitia `in`/`operand1`/`operand2`), e OR paralelo exige parte
   `O` com `Card` + pinos `in1..inN` — juntar dois pinos `out` no mesmo fio o Portal recusa
   (`invalid connection ... at pin "out"`). Verdade de referência: `docs/examples/BombaTemplateFc.xml`
   (Contact/Coil `in`/`out`/`operand`; Move `en`/`in`/`out1`; comparador `pre`/`in1`/`in2`/`out`).
   `SrcType` DInt default compila limpo contra tag `Int`. Tags do fixture:
   `docs/examples/LadderTags.xml` (sem elas o bloco importa mas fica inconsistente e nem exporta).
   Exemplo: `docs/examples/ladder.scl`. Fora do V1: FB calls (TON/CTU), edge, copy tag→tag.
2. ~~**Estrutura**~~ ✅ feito (`create-folder`/`delete-folder --path A/B [--tags]`,
   `delete-block --name X`, `export-type`/`import-type` p/ UDT). Dry-run conta conteúdo antes
   de deletar. ✅ smoke 2026-07-27 no projeto real: `create-folder --apply`, `delete-block --apply`
   e `delete-folder --apply` executados contra alvo descartável (`ZZ_Smoke`) e revertidos —
   1011 blocos antes e depois. `export-type`/`import-type` (dry `action: override`) ok **após**
   `compile --apply`: sem compilar, Openness recusa (`Inconsistent blocks and PLC data types
   (UDT) cannot be exported`).
3. ~~**Hardware**~~ ✅ feito (`add-device --mlfb X --name N [--station S]`,
   `set-address --device X [--ip] [--mask] [--pn-name]`, `connect-subnet --device X --subnet S
   [--io-system IO]` — controller cria IO-system, IO device entra num existente;
   `export-cax`/`import-cax` AML). ✅ smoke dry 2026-07-27: `set-address` lê o endereço atual
   (`192.168.10.1`), `connect-subnet` detecta `subnetAction: reuse`, `add-device`/`export-cax`/
   `import-cax` ok. `--apply` de hardware não exercitado (atributos Node e CreateIoSystem seguem
   não validados). ~~`--device` só aceitava nome de estação~~ ✅ 2026-07-27: `Hardware.FindDevice`
   cai pra busca recursiva nos `DeviceItems`, então `--device "CPU CCO"` resolve pra
   `S7-1500/ET200MP station_1`; quando não acha, o erro lista os devices conhecidos.
4. ~~**Compile granular + diff-block**~~ ✅ feito (`compile [--block X | --folder A/B]`,
   `diff-block --file F.xml [--name X]` read-only). `BlocksIdentical` movido pra `Ops`
   (param ignoreComments; AlarmFc=true, InstrumentFc=false — comportamento preservado).
5. ~~**Inspeção**~~ ✅ feito (`find --pattern P* [--kind block|table|tag|type]` wildcard,
   `list-types`, `snapshot` inventário completo, `xref --name BLOCK` via CrossReferenceService —
   API compilou contra DLL real; formato do resultado validar no smoke).
6. ~~**Batch**~~ ✅ feito (`tia run --script ops.json` — JSON array de arg-arrays, attach 1x;
   falha para no passo com erro; `run`/`open-project` proibidos como step.
   Exemplo: `docs/examples/batch.json`). ✅ smoke 2026-07-27.
   **Limitação**: sem try/catch por step, a 1ª exceção aborta o batch **e descarta os resultados
   já colhidos** (o `Print` final nunca roda) — `Program.cs:148-155`. Pra bateria onde falha é
   esperada, rodar um verbo por vez.
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

11. ~~**Scaffold de projeto**~~ ✅ feito 2026-07-27 (`scaffold --manifest F.json [--apply] [--force]`):
    projeto novo recebe a árvore da lei + os moldes exportados do projeto de referência.
    Idempotente (objeto existente = `skip`), ordem de import por tipo (UDT→tags→FB→DB→iDB→FC→OB),
    caminho de pasta em segmentos (nome real tem `/`). Manifesto:
    `library/library.json`; fonte `library/blocks/` (66 itens, gitignored) — antes em
    `docs/examples/scaffold-padrao.json` + `workspace/padrao/`, movidos em 2026-07-28.
    Dry contra o de referência: 26/26 pastas e 66/66 itens `exists`. ✅ **aceite fechado**
    (`workspace/ScaffoldTest`): `create-project` → `add-device` → `scaffold --apply` (66/66 criados)
    → `compile --apply` → `save-project` → `audit` **5/5 limpo**. Dois bugs que só o ramo `create`
    expôs (culturas do projeto, `<Culture>` elemento vs atributo) — ver PADRAO.
    `add-device --apply` exercitado pela 1ª vez aqui (item 3 do backlog). Compile do projeto
    scaffoldado dá 26 erros de ambiente ausente (system/clock memory bits, tags de IO, iDB dos
    moldes) — nada de import; detalhe e pendência em `docs/PADRAO.md`.

## Otimização de tokens do CLI — ✅ 2026-07-28

Levantada pelo custo real da reorganização da `1. FB Bilbiotecas` (6 chamadas de ferramenta e um
gerador de batch em PowerShell pro que devia ser uma linha). Seis pontos, todos fechados:

| ponto | antes | agora |
|---|---|---|
| `list-blocks` sem filtro | dump de ~480 blocos | `--folder A/B` (inclui subpastas) · `--type FB\|FC\|OB\|GlobalDB\|InstanceDB` · `--count` (total por pasta, ~10 linhas) |
| não existia move | `export`+`delete`+`import` por bloco, na ordem certa | `move-block --name X \| --pattern P* --folder A/B [--apply]` ([Ops.cs:290](../src/Tia.Core/Ops.cs#L290)) |
| regra do nome de arquivo (`/` → `_`) reimplementada fora | PowerShell replicando `ExportPath` | interno ao `move-block` |
| acento virava `?` na rota da task | round-trip por arquivo pra qualquer saída com acento | `[Console]::OutputEncoding` UTF-8 em `taskrun.ps1` e `_common.ps1` |
| assinatura de verbo | ~5 greps em `Program.cs` por sessão | `docs/VERBS.md`, gerado do help por `scripts/gen-verbs.ps1` dentro do `rebuild.ps1` |
| `run --script` | resultado completo de cada step (98 steps = dump) | `--summary` → `{steps,failed,errors[]}` |

Junto: `create-folder`/`delete-folder --types` (pasta de UDT era o único dos três tipos de pasta
sem verbo) e `delete-type`. Smoke no projeto de referência: `move-block --apply` + `compile` +
`create/delete-folder --types` + `list-blocks --count` num batch, `{steps:6, failed:0}`.

**Regra do `move-block`, que o verbo agora encapsula**: exporta **todos** os alvos antes de apagar
o primeiro. O `delete` deixa quem referencia inconsistente, e bloco inconsistente não exporta.

## Biblioteca de blocos ("arsenal") — 🔄 fatia 1 fechada + testada no Portal 2026-07-28 (`library/`)

Problema que resolve: os 4 geradores só rodam se o projeto do cliente **já tiver** os moldes e a
lei de pastas (`doctor` checa `FC_Modelo`, `OB_MOLDE_ALARMES`, `DB GLOBAL`, `2. Alarmes`,
`3. Partidas`, UDTs `MotorDados`/`ValvDados`). Sem isso, `doctor` vermelho e acabou. Com biblioteca
instalável, vira um comando.

**Empacotamento decidido**: `.scl` como padrão, `.xml` só pro que precisa nascer em LAD.
- `.scl` via `import-source` — texto diffável, **linguagem SCL inteira** (compilador da Siemens),
  gera FC/FB/OB/DB/UDT ([Ops.cs:311](../src/Tia.Core/Ops.cs#L311) faz `GenerateBlocksFromSource` e
  apaga a fonte). Imune à versão do Engineering. Limitação: bloco nasce na raiz (verbo não tem
  `--folder`); contorno com verbos já validados = `export-block` → `import-block --folder` →
  `delete-block`.
- `.xml` via `import-block --folder` — escolhe pasta, preserva LAD e comentários multilíngues.
  Custo: `<Number>` colide (foi preciso reescrever no teste de 2026-07-28) e o `<Engineering
  version="V21">` prende à versão.
- `.al19` via `import-master-copy` — **descartado**: binário, não diffa, só se produz na mão.
- `import-ladder` (subset nosso) **não serve** pra escrever a biblioteca: sem timer nem aritmética.

**Instalação**: `tia scaffold --manifest library/library.json --apply` — sem verbo novo,
`scaffold` já é "árvore de pastas + moldes num projeto". A ordem de import por tipo já está certa
(UDT → tabela → FB → DB → iDB → FC → OB, [`Scaffold.Rank`](../src/Tia.Core/Scaffold.cs#L58)) —
a anotação anterior de "falta ordenar UDT antes de DB/FC" estava obsoleta, `Rank` sempre teve
`SW.Types` = 0.

**Fatia 1 ✅ 2026-07-28 (offline, sem Portal)** — `library/` na raiz:
- `library/library.json` (versionado) = o antigo `docs/examples/scaffold-padrao.json` com
  `Source: "blocks"` (relativo ao manifesto, então manifesto + payload viajam juntos pra
  qualquer pasta). 20 pastas de bloco, 6 de tag, 66 itens.
- `library/blocks/` (gitignored) = o antigo `workspace/padrao/`, 66 XMLs / 3,3 MB.
- `library/export-all.json` (versionado) = batch inverso, gerado do manifesto: 66 exports com o
  verbo certo por tipo, `--out library/blocks`, 1 attach. Substituiu `scripts/export-fixtures.ps1`
  (cobria 15 dos 66) e o `workspace/export-padrao.json` (gitignored, caminho absoluto da máquina).
- `library/README.md` (versionado) = por que o payload não viaja, inventário dos 66 por pasta,
  o que cada gerador exige, como repor, como instalar, limitação do `Folder` de UDT, e as duas
  pegadinhas do export (compile antes; `ExportPath` troca `/` do nome por `_` no arquivo, caso
  `FB_LIGA/DESLIGA MODO AUTO`).
- **Testado contra o Portal ✅ 2026-07-28** (projeto `Software de ETE Insular_Inicial_V21`):
  `scaffold --manifest library/library.json` dry = 26 pastas `none (exists)` + **66/66
  `skip (exists)`**, zero item não encontrado (manifesto casa 1:1 com o payload e com o projeto);
  `run --script library/export-all.json` = **66/66 `ok`** num attach. Duas rodadas de export
  seguidas dão 66 arquivos byte-idênticos exceto `<DocumentInfo><Created>` (timestamp) — hash muda
  sempre, conteúdo não; anotado no README pra ninguém caçar diff fantasma.

**Gap do `scaffold` — corrigido ✅ 2026-07-28**: item UDT ignorava `Folder` (todo `SW.Types.*` caía
na raiz do `TypeGroup`). Agora passa por [`ResolveTypePath`](../src/Tia.Core/Scaffold.cs#L188),
análogo a `ResolveBlockPath`/`ResolveTagPath` — cria a subpasta de tipo se faltar. Validado no
Portal: manifesto com `"Folder": ["ClaudeTest","Tipos"]` e `--apply` → `find --kind type` mostra o
UDT em `ClaudeTest/Tipos`. `rebuild.ps1` ALL PASS.
Junto veio **`delete-type --name X [--apply]`** ([Ops.cs:205](../src/Tia.Core/Ops.cs#L205)) — não
existia jeito de tirar UDT pela CLI, e teste de biblioteca cria UDT descartável. Os 4 UDTs de teste
(`*_T`, `MotorDados_LIB`) foram apagados com ele, compile 0 erros. Sobra: `delete-folder` não
apaga pasta de **tipo** (só bloco e tag) — `ClaudeTest/Tipos` ficou vazia no projeto de teste.

**Fatia 2 — parte SCL ✅ 2026-07-28** (`library/core/`, autoral e versionado, com README próprio):
`MotorDados.scl`, `ValvDados.scl`, `MotorPrincipal.scl` (composto de dois `MotorDados`, não campos
`CMD_&_*` duplicados), `DB GLOBAL.scl` (esqueleto: `AREA_01.ALARMES.WORD_ALARMES_1..8`,
`HARDWARE_INTERRUPT.ALARMES_MODULOS.QA-00/QA-01.WORD_1..2`) e `FB BITS TO WORD.scl` (slice access
`#BITS_TO_WORD.%X0..15`, pinos `SIGNAL_Bit0..15`). Importados na ordem UDT → DB/FB no projeto de
referência com nomes sufixados `_T` (pra não sobrescrever os homônimos do cliente) — **compile
0 erros / 0 warnings**. Os 2 blocos de teste foram apagados; os 3 UDTs `*_T` ficaram (não existe
verbo `delete-type`). Falta: os 4 moldes em LAD e assar `.scl` → `.xml` num projeto vazio pra
instalar via `scaffold` (`Scaffold.Plan` lê o tipo do XML, [:84](../src/Tia.Core/Scaffold.cs#L84),
e `import-source` não tem `--folder`).

**Núcleo genérico (fatia 2, autoral e publicável — desenho original)**. Os 66 itens de
hoje são exports do cliente e nunca vão pro Git; o que fecha `doctor` verde num projeto qualquer
são ~10 itens, escritos do zero:

| item | tipo | formato | por que |
|---|---|---|---|
| `MODULE_ERROR_MOLDE` | OB molde de erro de módulo | `.xml` | LAD; template de `gen-fault-ob` ([FaultOb.cs:19](../src/Tia.Core/FaultOb.cs#L19)) |
| `FC_Modelo` | FC modelo de alarmes | `.xml` | LAD; template de `gen-alarm-fc` ([AlarmFc.cs:20](../src/Tia.Core/AlarmFc.cs#L20)) |
| `OB_MOLDE_ALARMES` | OB molde de chamada | `.xml` | LAD; `AlarmFc.ObTemplate` |
| `MOLDE_ANALOGS` | molde de instrumento | `.xml` | LAD; template de `gen-instrument-fc` |
| `FB BITS TO WORD` | FB 16 bits → word | `.scl` | `AlarmFc.MasterFb`; lógica pura, sem LAD |
| `MotorDados` / `MotorPrincipal` / `ValvDados` | UDT | `.scl` (`TYPE`) | estrutura por equipamento que `replicate-fc` espera |
| `DB GLOBAL` **esqueleto** | GlobalDB | `.scl` (`DATA_BLOCK`) | `GlobalDb` dos 3 geradores; só a casca, **não** os 869 KB do cliente |
| árvore de pastas | — | manifesto | `2. Alarmes`, `3. Partidas`, `3.1 Alarmes Words`, `3.1.0 Modelo` são nome default nos configs |

`.scl` onde a lógica é aritmética/estrutura (diffável, imune à versão do Engineering, nasce na
raiz → contorno `export-block` → `import-block --folder` → `delete-block`); `.xml` só nos 4 moldes,
que precisam nascer em LAD legível porque o engenheiro edita e os geradores clonam rede a rede.

**Conteúdo, por valor**: (1) os pré-requisitos dos geradores — é onde está o retorno;
(2) utilitários genéricos (escala raw↔EU + clamp, debounce de falha, borda + selo com falha/reset,
horímetro, contador de partidas, bits→word e inverso, first-out, watchdog de comunicação, rampa de
setpoint); (3) diagnóstico (OB de erro de módulo já existe em `ModuleErrorMolde.xml`).

**Procedência** — resolvida pelo gate de publicação da F4: payload de cliente fica gitignored
(`library/blocks/`), o que for pro Git é autoral ou sanitizado com `clone --replace OLD=NEW`.
Vale também pros XMLs de `docs/examples/`: **sanitizados ✅ 2026-07-28** por substituição de texto
(não deu pra usar `clone --replace`, que exige o bloco no projeto) — `CASA_DE_SOPRADORES` → `AREA_01`,
`SOPRADORES_DESARENADOR` → `AREA_01_MOTORES`, `SOPRADOR_DESARENADOR_S-01A` → `MOTOR_S-01A`,
`PARTIDA_SOPRADOR_1` → `PARTIDA_MOTOR_1`, e as duas tags `..._STS_SOPRADOR_DESANERADOR_MODO_*`.
Tocou `BombaTemplateFc.xml`, `StdBombaA.xml` e as asserções de `Tia.Tests/Program.cs` que citavam
esses nomes; `rebuild.ps1` **ALL PASS**. Sobra proposital: `library/library.json` e
`library/export-all.json` citam `SOPRADOR_DESARENADOR (S-01A)` porque o nome tem que casar com o
objeto no projeto do cliente pra repor o payload — nome de objeto, não payload.

**Ainda por resolver antes de tornar o repo visível de fato**: nome de projeto de cliente em prosa
(`Insular`, `ETE SG`, `AsBuilt`) aparece em `docs/PLANO.md`, `docs/PADRAO.md`,
`docs/projeto-real-fase-A.md`, `library/README.md`, `scripts/raio-x.ps1`, `__navi__.md` e em todo
o histórico de `.handoff/` — sanitizar isso é reescrever histórico já commitado, decisão do user.

**Fatia 3** (utilitários genéricos: escala raw↔EU + clamp, debounce, bits→word e inverso,
first-out, watchdog, rampa de setpoint) só depois da fatia 2. Teste das fatias 2/3 = instalar em
`ClaudeTest/` e `compile` 0 erros, um `run --script`.

## Bugs abertos (smoke 2026-07-27)

- ~~**`import-block` dry-run dá falso positivo em XML que não é bloco.**~~ ✅ corrigido
  2026-07-27. `Ops.RequireRootType` valida o root object antes de reportar `action`:
  `SW.Blocks.*` (`import-block`), `SW.Tags.PlcTagTable` (`import-tags`), `SW.Types.*`
  (`import-type`). Dry agora sai 1 com
  `XML root object is 'SW.Tags.PlcTagTable', expected 'SW.Blocks.*'`. Teste offline
  `Ops.RequireRootType`; smoke real: 4 combinações (2 aceitas, 2 recusadas) no AsBuilt.

## Projeto de referência (2026-07-27)

`proj/Software de ETE Insular_Inicial_V21` = projeto-molde da casa, conforme ao padrão que gerou
os scripts FINAIS. Estrutura completa + divergências CLI×padrão em [`docs/PADRAO.md`](PADRAO.md).
Regra: **quando a CLI diverge desse projeto, quem está errado é a CLI.**

## Clonar acionamento — fluxo real validado (2026-07-27, AsBuilt)

Objetivo do usuário ("mais uma bomba igual à BH-01A") fechado ponta-a-ponta clonando
**BH-01B → BH-01C** na Elevatória de Gordura. Verbos novos desta rodada:

- `add-db-member --db X --name M [--path A.B] [--type T | --like SIBLING] [--apply]` — a lacuna
  registrada antes (nenhum verbo *criava* instância de UDT na DB global). `--like` clona o nó do
  irmão e insere logo depois. Idempotente (`action: exists`). `ResolveSection` cobre as duas
  formas do XML: Struct nativo aninha `<Member>` direto, instância de UDT expande em
  `<Sections><Section>`.
- `clone --block N | --table T --replace OLD=NEW [--at %M432.0] [--folder A/B] [--apply]` —
  export → substituição textual → import. Um `--replace BH-01B=BH-01C` reescreve de uma vez nome
  do bloco, símbolos de tag, caminhos do DB global e instance DBs. `--at` reendereça as tags Bool
  em sequência; tag de largura maior aborta em vez de sobrepor endereço.
- `free-memory [--bytes N] [--from B]` — read-only, buracos livres da área %M (2588 tags,
  605 bytes usados, topo %M9001). Foi ele que apontou o bloco usado no teste (`%M432.0`, 8 bytes).

**Sequência que funciona** (cada passo um verbo, nunca `run --script`):
`free-memory` → `add-db-member` (instância + struct de comando do par) → `compile --block "DB GLOBAL"`
→ `clone --table` → `clone --block` (5 instance DBs, depois o FC) → `compile --apply` → `save-project`
→ `diff-block`. Resultado: PLC inteiro compila Success/0 erros, `diff-block` do FC clonado `identical`.

**Ordem é obrigatória, não estilo**: todo import deixa o alvo inconsistente e
`Inconsistent blocks and PLC data types (UDT) cannot be exported` derruba o *próximo* export —
inclusive de blocos que só *referenciam* o DB alterado. Compilar entre etapas.

`replicate-fc` **não** serve para este projeto: exige pasta nomeada `... (ID)` (AsBuilt usa
`Bomba Reserva BH-01B`) e é replicador em massa — sobrescreve todas as pastas irmãs a partir do
molde, não clona um equipamento. Dry no AsBuilt: 0 grupos, 61 pastas puladas.

**Limite conhecido**: tags de IO físico (`BOMBA_2_ELEVATORIA_DE_GORDURA_*`) não são clonadas —
uma bomba nova de verdade precisa de %I/%Q próprios, que dependem de hardware novo. `free-memory`
cobre só %M; endereço físico continua manual, de propósito.

## Pendências / decisões futuras

- ~~Licença~~ ✅ MIT (F4, 2026-07-18). ~~Nome público~~ ✅ `tia-cli`.
- ~~Publicar no GitHub~~ ✅ publicado 2026-07-20: https://github.com/Codyte/tia-cli (público).
  `Scripts_Siemens/` excluído do público — removido do tracking + scrubado do histórico
  via `git-filter-repo` (verificado: clone fresh sem o diretório em working tree ou histórico).
- Smoke F1 na máquina do TIA (user leva o exe; primeira execução dispara popup Openness — permitir).

## F6 — Endurecer os scripts PS (✅ executada 2026-07-27)

**Resultado.** `scripts/_common.ps1` + `scripts/tia.ps1` entregues como planejado; `tia-task.ps1`
removido (o wrapper o substitui). Bugs: **1 já estava corrigido** na máquina *e* no script
(`setup-tasks.ps1:37` já era `-LogonType Interactive` — a auditoria leu o principal da
`TiaWhitelist`, que é S4U de propósito); 2, 3, 4 e 5 fechados.

Dois achados novos, ambos de PowerShell:
- **`$raw -is [pscustomobject]` é verdadeiro até pra `[string]`** (tudo vira PSObject). Era como o
  runner distinguia `{"id","args"}` de array cru — a forma legada `["doctor"]` virava `args` vazio e
  o CLI cuspia o help com exit 1. Correto é testar a propriedade: `if ($null -ne $raw.args)`.
- **Splat de array vazio vira argumento `""`**: `Invoke-Tia close-project @($Save ? @('--save') : @())`
  passa string vazia pro CLI. Trocado por `if/else` explícito em `use-project`/`clone-hw`.

**O shell do agente pode nascer na sessão 1** (VSCode na sessão do usuário — foi o caso na
verificação: `SessionId=1`, `UserInteractive=True`). A premissa "nenhum macro roda do agente"
vale só quando ele nasce na sessão 0. O roteamento cobre os dois casos e `TIA_VIA_TASK=1` força a
rota da task — sem esse knob o ramo da sessão 0 seria código morto até falhar em produção.

### Plano original (referência)

Auditoria dos 11 scripts em `scripts/`. Problema central: **nenhum macro roda a partir do
agente**. `use-project`/`prep-project`/`raio-x`/`clone-hw` chamam `& $exe` no processo local;
na sessão 0 isso é sempre `"No running TIA Portal instance found"` (confirmado nesta máquina:
`[Environment]::UserInteractive=False`, `SessionId=0`; portal em `SessionId=1`). Hoje eles só
servem se o **usuário** rodar à mão numa janela da sessão 1 — o agente refaz o protocolo taskio
na unha todo turno.

### F6.1 — Bugs pontuais (independentes, fazer primeiro)

| # | Arquivo | Defeito | Correção |
|---|---------|---------|----------|
| 1 | `setup-tasks.ps1:19` | Registra `TiaSmokeRun` com `LogonType S4U`. S4U cai na sessão 0 e nunca attacha. A task viva na máquina está `Interactive` (corrigida à mão), o script ficou pra trás. | `-LogonType Interactive`. **Não re-rodar o script depois de corrigir** — é `-Force`, recria a task e derruba o canal que hoje funciona. Corrigir e deixar quieto; vale só pra máquina nova. |
| 2 | `taskrun.ps1:15` | `& $tia @tiaArgs *> out.txt` funde stdout e stderr. Contrato do CLI é stdout=JSON / stderr=log humano; fundido, `ConvertFrom-Json` engasga. | Redirects separados (`1>` / `2>`), arquivos distintos. |
| 3 | `taskrun.ps1:11` | Quem apaga `exit.txt` é o runner, depois da task já ter arrancado. Entre `Start-ScheduledTask` e esse `Remove-Item` o `exit.txt` da rodada anterior ainda está no disco → poller lê e conclui que terminou. | Resolvido de graça pelo run-id da F6.2 (nome único por chamada = sem arquivo velho pra ler). Runner para de apagar. |
| 4 | `rebuild.ps1` `Get-RegHash` | `Select-Object -First 1` sobre os filhos de `...\Openness`: com V19 e V21 no registro compara contra a que vier primeiro, enquanto `whitelist.ps1` grava em todas. Só olha a chave `Entry`, ignora a `EntryLocal` que o próprio whitelist escreve. | Comparar contra **todas** as versões/chaves; stale = qualquer uma divergente. |
| 5 | `smokeloop.ps1` × `taskrun.ps1` | Nomes de saída divergentes (`result.txt` vs `out.txt`); CLAUDE.md documenta só `out.txt`. Poll no arquivo errado dependendo da rota de pé. | Mesmo protocolo run-id nas duas rotas (F6.2). |

### F6.2 — `scripts/_common.ps1` + `Invoke-Tia` (o núcleo)

Um arquivo novo, dot-sourced pelos macros. Mata as três duplicações de hoje
(caminho do exe em 5 arquivos, `c:\Scripts\TIA Portal` hardcoded em 5, `TITANXNEXUS\Carlos_Ortiz`
em 2 — e o repo é público como `tia-cli`, nada disso roda em clone de terceiro).

```powershell
$script:Repo = Split-Path $PSScriptRoot
$script:Exe  = Join-Path $script:Repo 'src\Tia.Cli\bin\Debug\net48\tia.exe'

function Invoke-Tia {
    param([int]$TimeoutSec = 600, [Parameter(ValueFromRemainingArguments)][string[]]$TiaArgs)
    if ((Get-Process -Id $PID).SessionId -ne 0) { & $script:Exe @TiaArgs; return }   # sessão 1: direto
    # sessão 0: rotear pela task TiaSmokeRun (que roda na 1)
}
```

Regras de projeto (cada uma resolve um problema conhecido):

- **Roteamento por sessão.** `SessionId -ne 0` → invoca direto. Senão → canal taskio. Callers
  não sabem a diferença.
- **`$global:LASTEXITCODE`.** Uma função PS não seta `$LASTEXITCODE` sozinha. No caminho da task,
  ler o código de `exit-<id>.txt` e atribuir a `$global:LASTEXITCODE` — assim todos os
  `if ($LASTEXITCODE) { exit }` dos macros continuam valendo **sem edição**.
- **Run-id único por chamada.** `cmd.json` passa a aceitar as duas formas: array (`["doctor"]`,
  compatível com o uso manual documentado) ou objeto `{"id":"...","args":[...]}`. Com id, o runner
  escreve `out-<id>.txt` / `err-<id>.txt` / `exit-<id>.txt`. Isso resolve **dois** problemas de
  uma vez: a race do item 3 (não existe arquivo velho com aquele nome) e o lock que forçou o
  `smokeloop` a rotacionar pra `result.txt` — quando um verbo inicia o portal, o portal herda o
  handle do arquivo de saída e o mantém aberto enquanto viver; nome fixo = próxima rodada não
  consegue redirecionar pra ele. Nome único contorna sem depender de o portal morrer.
  Prune de `out-*`/`err-*`/`exit-*` com mais de 1 dia na entrada, erro ignorado (podem estar
  travados pelo portal). `workspace/` é gitignored.
- **Ordem do protocolo** (cliente): escreve `cmd.json` → `Start-ScheduledTask TiaSmokeRun` →
  poll de `exit-<id>.txt` → emite `out-<id>` em stdout, `err-<id>` em stderr → seta
  `$global:LASTEXITCODE`.
- **Timeout.** Default 600s (`open-project` leva 2-4 min; compile de projeto real também demora).
  Estouro = erro claro, não trava. Cobre também o gap do `smokeloop`, que hoje faz
  `Start-Process -Wait` sem limite e prende o loop pra sempre num `open-project` travado.
- **Guard de concorrência (D9).** `cmd.json` já existente na entrada = outra chamada em andamento
  → falha alto em vez de clobber.

Depois disso, `scripts/tia.ps1` vira wrapper de 3 linhas (`. _common.ps1; Invoke-Tia @args`) —
o comando único que hoje não existe e que o CLAUDE.md descreve em prosa como 3 passos manuais.

### F6.3 — Migrar os macros

`use-project.ps1`, `prep-project.ps1`, `raio-x.ps1`, `clone-hw.ps1`: trocar `& $exe` por
`Invoke-Tia` e o `& pwsh -NoProfile -File use-project.ps1` (spawn de pwsh ~1s) por dot-source.
Zero mudança de lógica — os checks de `$LASTEXITCODE` seguem funcionando pelo `$global:`.
Ganho: os quatro passam a rodar do agente.

### F6.4 — Robustez menor

- `prep-project.ps1` é o único macro que muta (`compile --apply` + `save-project`) **sem gate** —
  `clone-hw.ps1` tem `-Apply`, esse não. Apontar projeto errado grava nele. Adicionar `-Apply`
  com o mesmo contrato (dry = só `doctor`).
- `use-project.ps1:21`: `open-project` é a última linha, sem checar exit — propaga pelo exit do
  script, mas sem mensagem própria de "abriu e falhou".
- `clone-hw.ps1`: sem check de exit no `save-project` final; salva sem confirmar que o
  `import-cax` aplicou.
- `raio-x.ps1`: `ConvertTo-Json -Depth 8` no agregado de xref pode truncar em silêncio.

### F6.5 — CLI (opcional, C#)

`raio-x.ps1` faz **um Attach por OB** no loop de xref (segundos cada). `xref --name` aceitar
lista de nomes (ou `--all-obs`) resolve na raiz. Só vale se o raio-x doer no projeto real.

### Verificação

- F6.1: `pwsh scripts/rebuild.ps1` ALL PASS; diff do `setup-tasks.ps1` conferido **sem re-rodar**.
- F6.2: o check é end-to-end e vale mais que teste unitário — `pwsh scripts/tia.ps1 doctor`
  **do shell do agente** (sessão 0) tem que devolver JSON e sair 0. Hoje isso é impossível.
- F6.3: `pwsh scripts/raio-x.ps1 <Projeto>` do agente, read-only, contra o AsBuilt.
- F6.4: `prep-project` sem `-Apply` não pode salvar nada.

### Ordem

F6.1 → F6.2 → F6.3 (F6.4 junto com a 3, mesma edição de arquivo) → F6.5 só se necessário.
Commit por bloco.
