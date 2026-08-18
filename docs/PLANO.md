<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L27    PLANO — TIA Portal Openness API (V19+) -->
<!--   L32    Objetivo -->
<!--   L38    Decisões travadas (mudar só com motivo forte) -->
<!--   L52    Delimitações — o que a API NÃO é -->
<!--   L59    Arquitetura -->
<!--   L82    Fases -->
<!--   L104   Verificação (cada fase) -->
<!--   L112   Economia de tokens (regras da sessão) -->
<!--   L124   Skills em uso (nada novo pra instalar) -->
<!--   L135   Ambiente (descoberto na F1) -->
<!--   L163   Backlog v2 (cobertura Openness — priorizado) -->
<!--   L266   Projeto de referência (2026-07-27) -->
<!--   L272   Fronteira da engine — F7 itens 3-5 decididos (2026-08-07) -->
<!--   L299   D8 fechada — sem superfície online, e não é adiamento (2026-08-07) -->
<!--   L318   Pendências / decisões futuras -->
<!--   L359   F11 — IHM (em andamento, 2026-08-17) -->
<!--   L444   F13 — objetos de dentro da tela ✅ (2026-08-17) -->
<!--   L595   F14 — área nova ponta a ponta ✅ (2026-08-18) -->
<!--   L644   F12 — `sim-diag` camada 1 ✅ (2026-08-17) -->
<!--   L673   Teste cego ponta a ponta — caderno escrito (2026-08-07) -->
<!--   L942   Histórico fechado -->
<!--   L955   F15 — auditoria externa: P0 fechados ✅ (2026-08-18) -->
<!-- ======================= END NAV INDEX ======================= -->

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
| D8 | **Sem operações online — definitivo (fechado 2026-08-07)** | Nada de download/go-online/compare online/commit Multiuser via API. Projeto offline + compile apenas. Humano faz check-in e download no TIA. Não é "adiado pro v2": ver "D8 fechada" abaixo. |
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
| F3.5 | Melhorias pré-projeto-real (backlog handoff itens 1-3) + banho de projeto real Fase A/B | robustez por-item, idempotência alarm-fc, verbo `doctor`, achados documentados | ✅ 2026-07-18: itens 1+2+3 aplicados e smoked; `tia doctor` novo (preflight read-only, 6 verbos); `Ops.BlocksIdentical` normaliza namespace+Informative; fix pastas TIA com `/` literal (Replicate/Doctor); Fase A/B contra cópia `PROJETO-ASBUILT_V21` → 8 achados em `docs/projeto-real-fase-A.md` (viram backlog de adaptação); testes offline `Tia.Tests` (console assert, sem TIA): 31 asserts sobre BuildFcXml/BuildCallObXml/BuildObXml/BuildAreaFcXml/LadConverter vs fixtures `docs/examples/` — ALL PASS 2026-07-18 |
| v2 | Backlog de cobertura Openness (itens 1-10 abaixo) | verbos compilando 0 erros | 🟡 código 100% offline; smoke V21 core ok (add-device/set-address/connect-subnet/create-folder/import-tags/import-source/import-ladder/compile/export/diff/delete/save); 9 (online) bloqueado por D8; smoke 2026-07-18 contra projeto real (read-only): export-tags/list-types/export-type/xref/export-cax ✅, list-hmi erro claro (projeto sem Unified); smoke mutação 2026-07-18 no SmokeTest_01 ✅: import-type (dry override→apply), import-cax (AML 1.7MB do real; fix: sem ExclusiveAccess — Openness proíbe), gen-alarm-fc callOb=in-sync (idempotência total) |
| F3.6 | Macros de fluxo (itens 1-4 da lista aprovada) | smoked contra SmokeTest_01 | ✅ 2026-07-18: `prep-project.ps1` (use-project+doctor+compile+save), `raio-x.ps1` (banho read-only → workspace/<proj>/, xref de todos os OBs), `clone-hw.ps1` (CAx A→B, dry por padrão, -Apply salva), `docs/examples/gen-all.json` (6 verbos FINAIS dry via `tia run`, attach 1x). Macros 5-7 (new-area/sync-check/adopt-project) só se user pedir. |
| F4 | Polimento p/ GitHub (README EN, licença, exemplos) | repo publicável | ✅ 2026-07-18: LICENSE MIT, README EN completo (contrato dry-run/--apply, 3 gates Openness, tabela de verbos, macros, limitações), nome público decidido `tia-cli`. Publicação em si (gh repo create) pendente de ordem do user. **Gate de publicação (2026-07-28)**: nenhum payload de projeto de cliente entra no repo público — XML/AML exportado de projeto real carrega nome de equipamento, tag e estrutura de DB (`DB GLOBAL.xml` = 869 KB da planta), e publicar é irreversível na prática (fork, cache, índice). O que vai pro Git é autoral ou sanitizado (`clone --replace OLD=NEW`); payload fica gitignored e cada clone repõe o seu (`library/blocks/`, `workspace/`, `Scripts_Siemens/`, `proj/`). |
| F5? | MCP server fino sobre Tia.Core | só se D1 cair | ⬜ |
| F6 | Endurecer os scripts PS (ver seção "F6" no fim) | macros rodáveis do agente (sessão 0) + 5 bugs fechados | ✅ 2026-07-27: `scripts/_common.ps1` (`Invoke-Tia`, roteia por sessão, run-id, `$global:LASTEXITCODE`, timeout 600s, guard D9) + `scripts/tia.ps1` (comando único, substitui `tia-task.ps1` — removido); macros migrados; bugs 2-5 fechados (bug 1 já estava). Verificado end-to-end: `tia.ps1 doctor` exit 0, rota da task (`TIA_VIA_TASK=1`) exit 0, forma legada `["info"]` exit 0, `use-project`/`prep-project` do shell do agente |
| F7 | Camada de compreensão: a IA lê o projeto dentro do orçamento de contexto | `explain-block` (1) e `trace` (2) read-only | ✅ **fechada em 2 itens 2026-08-07** — `index`/`checkpoint`/`apply-spec` descartados com motivo, ver "Fronteira da engine" abaixo. Item 1 feito 2026-07-27: `explain-block --name X \| --file F.xml` (LAD/FBD → texto; 92KB → 8,3KB no `BombaTemplateFc`; `--file` roda sem TIA, 9 asserts em `Tia.Tests`). Smoke `--name` ok 2026-07-28 no `Software de ETE Modelo_Inicial_V21`: `Resets` 58KB → 4,6KB, `Paineis Intertravamento` 53KB → 4,9KB, `FC_ALARMES_PRELIMINAR_P_GM_01` 26KB → 2,2KB — chamadas de FB com pinos, expressões série/paralelo e comentários pt-BR corretos. Item 1 fechado. **Item 2 fechado 2026-07-28**: `trace --equipment X` smoked no mesmo projeto — `AG-01` = 39 símbolos + 39 usos em 10 blocos (`PARTIDA_AGITADOR (AG-01)`, `Resets`, `FB CONDIÇÃO DE PARTIDA`…), **10,1s total / 3,3s de xref, 131 blocos varridos**; cobertura conferida contra `xref --name Resets` independente. `xref` agora resolve bloco → tag → tabela → UDT (`ResolveSymbol`), então serve o sentido direto em qualquer símbolo. O "blocker do xref" do handoff anterior era **diálogo de autorização Openness pendurado na tela**, não custo de API — ver "Openness pede aceite na tela" abaixo. Índice invertido via export XML descartado: não há problema de performance a resolver. **Gargalos de consumo fechados 2026-07-28**: (a) `--out-file F.json` global — JSON completo no arquivo, stdout só `{file,bytes,count,head}`; guard no único `Print` por onde todo verbo sai, sem flag por verbo e sem mudar quem redireciona stdout (`raio-x.ps1`). Motivo medido: `find --pattern "*" --kind tag` = 821 KB / 4372 hits, `snapshot` = 7967 linhas — um verbo desses no contexto custa a sessão que o F7 existe pra proteger. Erro nunca vai pro arquivo. (b) `run --script` isola steps: `{ok:false,error,type}` por item, batch segue, `exit 1` se algum falhou — o batch só compensa se sobreviver à 1ª exceção (attach medido = **2,9s fixo**, não 7s: `info` solo 3,0s, `list-types` 2,9s, batch de 5 steps 7,0s). (c) **`tree` virou a leitura de orientação**: emite blocos + tabelas de tag + UDTs no mesmo `plc-navi.md` — 39 KB / 309 linhas p/ 476 blocos + 194 tabelas + 13 UDTs em 4,0s, contra ~150 KB do JSON equivalente. `snapshot` saiu do bloco "read" do help pro bloco "bulk"; `raio-x.ps1` roda `tree` primeiro e aponta o `plc-navi.md` como entrada. **`--format table` (TSV) foi medido e descartado**: 822 KB → 331 KB é 2x num problema que precisa de 30x — o que paga é agrupar (4,5x) ou não devolver volume (`trace` responde a pergunta inteira em 20 KB). Orçamento resultante: orientação ~10k tokens 1x por sessão, pergunta específica ≤5k, volume bruto nunca no contexto |
| F8 | Caminho de escrita exercitado contra projeto real (`--apply` de verdade, não dry) | cada verbo de escrita aplicado + `compile` 0 erros | ✅ **fechada 2026-08-07** — `replicate-instruments --apply` era o último e escreveu de verdade (ver "F8 fechada" abaixo). 2026-07-28 no `Software de ETE Modelo_Inicial_V21` (projeto de teste com backup; tudo em `ClaudeTest/`). **Primitivas 11/11 ✅**: `create-folder`, `import-block` (FC real de 90 KB), `import-tags`, `clone`, `export-type`→`import-type`, `import-source`, `add-db-member`, `delete-block`, `compile` — pasta compila Success/0 erros. **`import-ladder --apply` ✅** (2 bugs de FlgNet corrigidos, ver item 1b). **6 geradores ✅ em dry** (`gen-all.json`, 0 falhas) + payload de `gen-fault-ob` (OB de 88 KB) e `gen-alarm-fc` importado no sandbox → compile 0 erros → `explain-block` round-trip: o FlgNet desses builders já estava certo. **Pré-requisito descoberto**: `replicate-fc`/`gen-alarm-fc`/`replicate-instruments` falham com `Inconsistent blocks ... cannot be exported` se o PLC não foi compilado antes (eles exportam o GlobalDB) — `compile --apply` do PLC inteiro resolveu (projeto real: Success/0 erros, os 26 erros antigos já não existem). Guard novo em `Ops.ExportBlock` traduz essa mensagem. **Fechado 2026-07-28 (2ª sessão), escopado ao tipo `Soprador` na árvore de produção**: dry = 1 grupo, molde `Soprador 1 (S-01A)`, 2 alvos `overwrite` (S-01B/C — o projeto só tem 3 sopradores nessa pasta, não 6), 6 blocos cada, nada fora de `4. Motores/Bombas`. `--apply` exige **`--force`** quando a pasta-alvo já tem blocos (guard correto: sem ele o batch falha com `2 target folder(s) already have blocks…`). Batch `replicate-soprador-run.json` (save → apply → compile → apply → compile → save) = **0 falhas, os dois compiles Success/0 erros/0 warnings**. **Conteúdo conferido, não só compilação**: export de `PARTIDA_SOPRADOR_2 (S-01B)` e `_3 (S-01C)`, normalizando o ID de volta pro do molde, difere do template em **5 linhas de 1993** — `Created` (timestamp), `Number` (FC 151/152 vs 153), 2 `Component` de tag de IO (sufixo `_2`/`_3` do equipamento) e `ConstantValue` (301/302 vs 300); tudo o que o replicador deve reescrever, nada mais. Idempotência é *funcional*, não no-op: o 2º apply reimporta os mesmos blocos (o verbo não detecta in-sync) e o 2º compile recompila — resultado idêntico, 0 erros. **`gen-profinet --apply` + `standardize-tags --apply` ✅** no mesmo projeto: profinet 43 IO devices, 3 tags `exists` (no-op); tags 131 tabelas = 126 `ok` + 5 `rebuilt` (`SOPRADOR_TANQUE_AERACAO S-02A..E`); `compile --apply` depois = Success/0 erros/0 warnings + `save-project`. ~~**Falta**~~ ✅ **fechado 2026-08-07**: `replicate-instruments --apply` criou `TESTE_TOTALIZADOR` num instrumento novo (Success/0 erros) e `import-master-copy` foi exercitado na régua da CPU virgem `PLC_LIB2` — ver as duas seções de 2026-08-07 na parte da biblioteca. Registro do que estava pendente: `replicate-instruments --apply` (dry dava `in-sync`), `import-master-copy` real — a `.al21` deixou de ser hipótese em 2026-07-29 (bake real, 148 KB, 10 master copies), mas o `--force --apply` numa CPU virgem parou no bug do `CreateFrom` duplicado; fix commitado (`a0df2f7`) e **não re-testado** — ver "Bake real da `.al21`" na seção da biblioteca |
| F9 | Distribuição: o repo deixa de exigir toolchain pra ser experimentado | release baixável + CI verde + portas de entrada (CHANGELOG/CONTRIBUTING/SECURITY/templates) | ✅ **2026-08-11**. Diagnóstico que abriu a fase: 25 dias público, 1 star, 0 fork — o produto tinha profundidade e a distribuição estava em zero (sem release, sem tag, sem CI, sem versão; experimentar exigia clonar + SDK 8 + compilar). Entregue: `tia --version` (versão + qual Openness o exe carrega; o resolver virou `SiemensProbeDirs()`, compartilhado), `src/Directory.Build.props` (versão única dos 3 projetos), `scripts/pack.ps1` (zip de release do build local — o que entra sai de `git ls-files`, e uma guarda aborta se DLL da Siemens entrar; layout do zip = layout do repo, então whitelist/shim funcionam sem caminho especial), `init.ps1` detecta instalação de release (sem fonte = nada pra buildar) e pula os gates de build via `rebuild.ps1 -WhitelistOnly`. **CI não builda C# e isso é estrutural** — as assemblies do Openness são licenciadas e não existem em runner nenhum; o workflow verifica o que dá sem a Siemens: parse dos scripts, JSON válido, versão com entrada no CHANGELOG e a regra dura de nunca versionar DLL licenciada ou payload de cliente (`ci` verde na 1ª rodada). Release [v1.0.0](https://github.com/Codyte/Tia-Portal-CLI/releases/tag/v1.0.0) publicada, 614 KB, zero binário Siemens; instalação a partir do zip validada por extração + `init.ps1 -Check` (o ramo `$prebuilt` do não-`-Check` não dá pra testar sem sequestrar a whitelist do checkout oficial — um checkout só). SemVer é sobre o **contrato do CLI** (nomes de verbo, flags, shape do JSON, exit codes). 77 verbos na data da release (hoje são **78**, conferidos 1:1 contra o `Program.cs`). |
| F10 | Execução: o CLI fecha o ciclo escreve → compila → **roda → observa** | `sim-run` baixa o programa num PLC virtual e lê/escreve tag | ✅ **2026-08-17**. `sim-run` faz attach numa instância do control panel do **S7-PLCSIM Advanced**, baixa o programa por Openness (`--pc-interface PLCSIM`, default) e roda os passos do `--script` (`write`/`read`/`wait`/`run`/`stop`/`state`/`tags`). Provado no `PROJETO-MOLDE_V21`: download `Success`/0 erros, **41550 tags**, `CMD_LIGA` de um motor do `DB GLOBAL` escrito e relido (`false → true → false`), CPU em `Run`. **Quem liga a instância é um host longevo, e desde 2026-08-17 é o próprio repo** (`scripts/sim-host.ps1 -Start`, task `TiaSimHost`) — registrar dentro do `tia.exe` continua descartado, a instância morre com o processo (o Runtime Manager sobe in-proc, `Get-Service *PLCSIM*` vazio). **Correção do que esta linha dizia antes:** não é o usuário nem o control panel da Siemens — qualquer processo de sessão 1 que fique vivo serve, e o host sobe o Runtime Manager sozinho (medido com control panel e manager **mortos**: `RegisterInstance` ressuscitou o manager, `powerOn=OK`, e o `sim-run --apply` seguinte deu `Success`/0 erros, 41550 tags). O `Connect to module failed` da 1ª tentativa era o clássico sequestrando o canal, não limitação de instância própria. **A parede é a sessão do Windows**, a mesma do Openness: da sessão 0, `SimulationRuntimeManager.Version` volta vazio e `RegisterInstance` dá `-1, InvalidErrorCode` com o manager vivo na sessão 1 — daí `-Start` rotear pela task `TiaSimHost` (`LogonType Interactive`). **Medido o custo:** download = 45-52 s de 49-57 s do verbo (~91%); `--no-download` pula ele e roda os passos no programa que já está na instância. O JSON traz `download.ms` e `ms`. **PLCSIM clássico fechado é pré-requisito**: ele toma o canal (`-48, CommunicationInterfaceNotAvailable`) e sequestra o access point `PLCSIM` do S7ONLINE, onde o download sai `Success` com a instância Advanced vazia — o falso positivo que custou mais tempo nesta fase. Descartado por medição: tipo de CPU no registro (`CPU1515` não muda nada), `ConfigurationTargetInterface.Addresses` (sempre vazio), `RemoteConnect`/`OpenPort` (é ponte entre Runtime Managers de máquinas, não entre produtos). |

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

- **Esta máquina = <host>** (servidor: TIA Project Server, TIA Administrator, WinCC Unified RT).
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
9. ~~**Online**~~ ❌ **descartado 2026-08-07** (go-online, download, compare online/offline,
   start/stop CPU, watch tables). D8 fechada como definitiva — ver "D8 fechada" abaixo.
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

12. ~~**Edição pontual (fechar a superfície de escrita)**~~ ✅ 2026-07-28:
    `set-tag --table T --name N [--type] [--address] [--comment] [--rename]` (PlcTag tem esses
    atributos RW; `Name` só V20+ — dry mostra o antes→depois, nada a mudar = `skip (no change)`),
    `rename-block --name X --to NEW` (bloco **ou** UDT via `SetAttribute("Name", …)` — mesmo
    caminho do GUI: xref do iDB antes e depois mostra o mesmo caller, **sem** export/delete/import
    e sem cicatriz no vínculo chamada↔iDB), `edit-db-member --db X --name M [--type] [--rename]`
    (membro de DB não é atributo: export → XML → import Override, núcleo `ChangeInXml` testado
    offline; troca de tipo remove o `<Sections>` da instância antiga; **dois edits seguidos exigem
    `compile --apply` no meio**, o export recusa DB inconsistente; rename **não** corrige quem
    referencia o membro — o resultado carrega o aviso). Smoke ida-e-volta no `PLC_ZERO`
    (`Genericos`, `FB FALHA_MOTOR_01`, `DB_DUMMY`), tudo revertido.

13. ~~**`Cpu` no manifesto + validação de família**~~ ✅ 2026-07-28: `ScaffoldManifest.Cpu`
    (`"S7-1500"` em `library/library.json`) é conferido contra o `TypeIdentifier` da estação do PLC
    (`System:Device.S71500` → `S71500`; compara só letras e dígitos, então `S7-1500` == `s7 1500`).
    Família errada falha **antes** de escrever, com o motivo (`not supported for this instruction by
    the CPU used` só apareceria no compile); `--force` importa mesmo assim e devolve o mismatch em
    `cpu`. Estação ilegível não bloqueia. Smoke dry: `PLC_1` (1200) barrado, `--force` passou,
    `PLC_ZERO` (1500) passou.

14. ~~**`raio-x.ps1` em 2 attaches**~~ ✅ 2026-07-28: o banho era ~12 chamadas soltas (7 s de attach
    cada); agora são dois `run --script` (banho + xref de todos os OBs) e o merge dos parciais em
    `xref-obs.json`. Exigiu duas correções no CLI:
    **(a) `--out-file` por step** — o `--out-file` do processo vale só pro batch inteiro, então cada
    step carrega o seu e o resultado dele no batch vira o stub (`Print` foi partido em `WriteOut`);
    **(b) `list-blocks --type` casava substring** — `OB` batia dentro de `GlobalDB` (`Gl-ob-alDB`) e
    o filtro devolvia DB junto com OB (4 OBs viravam 7). Agora é igualdade.
    `raio-x.ps1` ganhou `-Portal` e `-Plc` (com mais de um Portal aberto ou vários PLCs, sem eles
    todo verbo falha); `--plc` do `run` também não desce pros steps.

## Projeto de referência (2026-07-27)

`proj/Software de ETE Modelo_Inicial_V21` = projeto-molde da casa, conforme ao padrão que gerou
os scripts FINAIS. Estrutura completa + divergências CLI×padrão em [`docs/PADRAO.md`](PADRAO.md).
Regra: **quando a CLI diverge desse projeto, quem está errado é a CLI.**

## Fronteira da engine — F7 itens 3-5 decididos (2026-08-07)

**A F7 fecha em 2 itens (`explain-block`, `trace`). `index`, `checkpoint` e `apply-spec` não
entram. A engine para no `run --script`.**

- **`index` — morreu por medição, não por opinião.** O próprio registro da F7 já dizia "índice
  invertido via export XML descartado: não há problema de performance a resolver": `trace` varre
  131 blocos em 3,3 s. Índice é cache com invalidação — e o projeto muda por fora, na UI do
  Portal — para economizar 3 segundos.
- **`checkpoint` — o que existe é melhor.** O valor seria rollback de um `--apply` ruim, e isso já
  se faz com cópia do `.ap21` (é o que os testes fazem) mais `export-all.json`. Checkpoint parcial
  é o pior dos dois mundos: restaura bloco e não restaura hardware/subnet — e a régua do G120
  mostrou que o estado que quebra é justamente hardware.
- **`apply-spec` — não entra como verbo genérico.** (a) `run --script` já é a forma declarativa:
  JSON na entrada, steps isolados, `--summary`, `exit 1` em falha; um `apply-spec` genérico seria um
  segundo interpretador de intenção por cima do primeiro. (b) A parte com valor real já existe e é
  **específica de domínio**: `packages.json` + `install-lib` são spec declarativa ("este PLC deve
  ter estes pacotes, ramos de DB, tabelas, iDBs, hardware") com reconciliação de verdade
  (`Get-Existing` pula o que já existe, `-Update` força, `$haveDev`/`$haveTables` idem). O bloco
  `devices` de 2026-08-07 é exatamente apply-spec com escopo. (c) Genérico exigiria diff
  bidirecional (o que existe × o que a spec pede) para todo tipo de objeto — é o `compare` do
  v2 item 9 disfarçado, e com **D8** de pé metade fica sem sentido.
  **Sinal para reabrir:** um 3º macro repetindo a lógica de reconciliação do `install-lib`.

Fronteira declarada: **verbos + `run --script` + macros PS**. Reconciliação declarativa mora nos
macros, por domínio.

## D8 fechada — sem superfície online, e não é adiamento (2026-08-07)

Decisão do user, tomada com a engine offline já fechada: **`go-online`, `download` e `compare
online/offline` não entram**. Sai do backlog v2 (item 9 some junto).

Por quê, na ordem que pesou:
- **Download é risco operacional que um CLI não deve carregar.** O objeto do verbo seria escrever
  num PLC em campo; `--apply` protege projeto, não protege processo. O humano faz download no TIA,
  vendo a tela, com a planta na frente.
- **`go-online` + `compare` sem download têm valor baixo** e custo de teste alto: exigiriam PLC
  real ou PLCSIM dedicado, que hoje não existe no ambiente de teste. Ficaria superfície sem régua.
- **A engine não fica capenga sem isso.** O que o agente precisa — ler, gerar, importar, compilar,
  instalar biblioteca — está fechado offline. Online é outro produto, com outro perfil de risco.

Consequências já registradas: `apply-spec` continua fora (metade do argumento dele era o `compare`
online), e as Delimitações seguem valendo — "não controla PLC online (D8)" agora é permanente, não
"no v1". **Sinal para reabrir:** um PLC/PLCSIM de teste dedicado *e* um caso de uso que não seja
download.

## Pendências / decisões futuras

- ~~Licença~~ ✅ MIT (F4, 2026-07-18). ~~Nome público~~ ✅ `tia-cli` (comando); o repo no GitHub é
  `Tia-Portal-CLI` — decidido 2026-08-07, nome fica, é o que quem procura acha.
- ~~Publicar no GitHub~~ ✅ publicado 2026-07-20: https://github.com/Codyte/Tia-Portal-CLI (público).
  `Scripts_Siemens/` excluído do público — removido do tracking + scrubado do histórico
  via `git-filter-repo` (verificado: clone fresh sem o diretório em working tree ou histórico).
- ~~Smoke F1 na máquina do TIA~~ ✅ 2026-07-17 (ver F1 na tabela de fases).
- ~~**`totally-integrated-claude`**~~ ✅ avaliado 2026-08-07
  (https://github.com/Czarnak/totally-integrated-claude, **MIT**). Não é CLI concorrente: são 17
  skills de documentação roteada da API (113 `.md`), extraídas do IntelliSense XML do V21, mais um
  MCP server à parte. Zero código reaproveitado; o valor foi apontar API que não enxergávamos.
  Rendeu: (a) o telegrama do G120 — ver seção própria; (b) `lib/` deixava 11 das 14 assemblies de
  fora; (c) a ideia do índice `--sdk` no `tia-help.py`. Não vale pegar: o wheel Python
  `siemens_tia_scripting` (caminho paralelo ao nosso C#) nem o framework de skills roteadas
  (`VERBS.md` + `tree` custa menos).
  **`currentStateHash` no dry-run — descartado (2026-08-07).** No MCP deles o preview devolve um
  hash do estado e o apply só passa se bater. Não traduz pro nosso shape: o MCP é um servidor vivo
  que segura estado entre preview e apply, enquanto cada `tia` é um attach novo — guardar o hash
  exigiria um store de nonce em disco, invalidação e um caminho de erro novo em todo verbo de
  escrita, para proteger uma janela que já é curta. E o que ela protege, `run --script` resolve de
  graça: dry e apply no mesmo attach, sem intervalo onde o projeto mude. Reabrir só se aparecer
  dry-run reaproveitado entre sessões, que é quando a janela deixa de ser curta.
- **Assemblies do Openness fora do `lib/`** (a instalação tem 14, o build referencia 4): `Safety` +
  `SafetyValidation` (F-blocks), `TeamcenterGateway`, `WinCC` clássico (só temos Unified) e as 5 de
  `AddIn`. Nenhuma necessária hoje; acrescentar quando um verbo pedir — o `--sdk` do `tia-help.py`
  já indexa as 14, então dá pra confirmar a API antes de mexer no csproj.
  **Atualização 2026-08-17:** `WinCC` clássico **entrou** no `lib/` e no `Tia.Core.csproj` — é ela
  que tem o roundtrip SimaticML de tela (`Screen.Export` / `ScreenComposition.Import`). Máquina que
  der pull precisa re-rodar `init.ps1`. Ver `docs/LIMITES.md`, seção HMI.
- ~~**`docs/LIMITES.md`**~~ ✅ **escrito 2026-08-17.** O que Openness/PLCSIM não fazem, com evidência
  (termo sondado / mensagem exata), natureza (limite de API × decisão do repo × DLL faltando ×
  limite de produto/SO) e saída. Motivo: a sondagem de diagnóstico online custou meia sessão na F10
  e ia custar de novo. Apontado do `CLAUDE.md`, do `SKILL.md` e dos `warn` de dois domínios do
  `study-map.json`.
- **F11 `sim-diag` — aberta, não iniciada, e agora se sabe que é 1500-only.** Camada 1
  (`OperatingState`/LED/eventos) e camada 2 (ler tag de alarme e cruzar com o texto dos blocos)
  dependem as duas da API do PLCSIM Advanced. Não alcança PLCSIM clássico nem S7-1200 de 1ª
  geração, S7-300 ou S7-400 — ver `docs/LIMITES.md`, seção Simulação. Despriorizada em favor de HMI
  (decisão do user, 2026-08-17): o gargalo do produto passou a ser tela.

## F11 — IHM (em andamento, 2026-08-17)

Gargalo do produto deixou de ser PLC/estrutura e passou a ser tela. **WinCC clássico antes de
Unified, e não por preferência: é a única das duas com roundtrip SimaticML de tela** — Unified é
modelo de objetos tipado (`HmiScreenBase`), sem export de tela. O projeto real é 100% clássico.

**Etapa 1 — leitura + roundtrip ✅ (2026-08-17)**

- `Siemens.Engineering.WinCC.dll` em `lib/`, `<Reference>` e `$dllNames` do `init.ps1`.
- `list-hmi` fala as duas APIs (campo `api: classic|unified`) e imprime caminho de pasta
  (`Pasta/Sub/Tela`). Medido no projeto real: 4 IHMs, 76 telas, 73 tabelas, 4354 tags, todas
  `classic`.
- `hmi-tree` → `hmi-navi.md` agrupado por pasta, irmão do `plc-navi.md`. **10,0 KB contra 14,3 KB
  do JSON = 1,4x**, longe dos 4,5x do `plc-navi.md`: o JSON de HMI já é compacto (tela é string,
  não dicionário de 6 campos como bloco). O verbo fica **pela forma** — agrupamento por área —,
  não pelo tamanho. Escrito aqui para não vender ganho que não existe.
- `export-screen --screen "Pasta/Sub/Tela"` e `import-screen --file X.xml [--folder P] [--apply]`,
  mesmo contrato do `import-block` (dry por padrão, `--folder` é caminho completo a partir da raiz
  de telas, `folderAction: create|reuse`). Raiz do XML é `Hmi.Screen.Screen`. Roundtrip provado no
  projeto-molde: export (14138 B) → import `--apply` `override` → re-export byte-idêntico.

**Etapa 2 — gerar tela de área a partir de molde: fechada com `import-screen --replace`**
(2026-08-17), sem verbo novo. A árvore da IHM **espelha a do PLC**
(`3. Partidas/3.1 Preliminar (P-GM-01)`), então o vínculo tela↔área já existe no nome.

O probe da tela real de área (`QA-01/1. Preliminar (P-GM-01)/Gradeamento Mecanizada`, 540 KB /
11114 linhas) decidiu o tamanho da etapa, e ela é menor do que os três ramos previstos:

- **Não há faceplate** (0 ocorrências) — é desenho solto: `TextField` (59), `Rectangle` (33),
  `GraphicIOField` (15), `SymbolicIOField` (12). Descartado o caminho
  `ScreenComposition.CreateFrom(MasterCopy)`.
- **Nenhum vínculo é por ID.** Toda referência é por **nome**:
  `<Tag TargetID="@OpenLink"><Name>…</Name></Tag>` — `@OpenLink` é marcador de resolução, e o
  `ID=` de cada objeto é contador local do arquivo. **Não há remapeamento a fazer.**
- **O nome da área não aparece como pasta**: `QA-01` = 0 hits. A área só existe **dentro do nome
  das tags** (`DB GLOBAL_PRELIMINAR_SKID_GRADEAMENTO_MECANIZADA_…`), que é texto puro no XML.
- 21 tags distintas, 3 delas placeholders (`tag`, `tag1`, `aux`); as outras 18 carregam o
  equipamento (`B-13A`, `GM-01A/B`, `RT-01A/B`, `XV-10/11`). **Confirma a restrição já enquadrada:
  replicar só serve para área com o mesmo elenco do molde.**
- ~90% do arquivo é `MultilingualTextItem`/`FontItem` — volume, não estrutura.

Logo, replicar tela = **troca de texto**, que é o `--replace` que o `clone` já tem. A implementação
foi ligar `Clone.RewriteFile` no `import-screen` (2 linhas em `Program.cs`), em vez de um
`replicate-screen` novo. Fluxo:

```
export-screen --screen "<molde>"
import-screen --file X.xml --folder "<pasta da área destino>" \
  --replace PRELIMINAR=<AREA> --replace "Gradeamento Mecanizada=<nome novo>" --apply
```

`--replace` também reescreve o **nome do arquivo** de saída (comportamento do `RewriteFile`), então
a cópia reescrita nunca sobrepõe o XML de origem.

**Número de tela é único por DEVICE, e a colisão derruba o TIA Portal** (medido 2026-08-17): a tela
clonada chega com o `<Number>` do molde, o `Import` estoura
`NonRecoverableException` — *"screen number '11' … is not unique for this device"* — e **o processo
do Portal morre**; os steps seguintes do batch só viram `EngineeringObjectDisposedException`. Não é
erro recuperável, é prevenção: em `action: create` o `import-screen` **tira o `<Number>` da tela**
(`Hmi.StripScreenNumber`, cópia `.renum.xml` ao lado do XML) e o Portal atribui um livre. O dry-run
declara em **`numberStripped`**. Só o `<Number>` da tela sai — o de objeto de dentro fica.
Em `override` o XML vai inteiro, que é o que mantém o roundtrip byte-idêntico da etapa 1.

**`delete-screen --screen "Pasta/Sub/Tela" [--apply]`** fecha o par do import: sem ele, tela de
smoke só saía pela GUI. `Screen.Delete()` existe; `ScreenComposition` não tem `Delete`.

Smoke ponta a ponta no projeto-molde (`run --script`, 23,1 s):
`import-screen --apply` (`numberStripped: true`, 19,9 s) → `export-screen` da tela nova
(`<Number>2</Number>` atribuído pelo Portal, 23 referências a `DB GLOBAL_PRELIMINAR` preservadas) →
`delete-screen --apply` → `export-screen` **falha com `not found`**, que é a prova de que sumiu.
Checks offline: `Clone.Rewrite` na forma da tela (nome + tag trocados, `@OpenLink` intocado) e
`Hmi.StripScreenNumber`.

- `export-hmi-tags --table "Pasta/Tabela"` — SimaticML da tabela de tags da IHM. **É por aqui que
  se vê o vínculo com o PLC**: dump de atributo não serve, `GetAttributeInfos` numa tag de HMI
  devolve **só `Name`** (medido 2026-08-17, primeira tentativa descartada). No XML a tag traz
  `<LinkList>` com `Connection`, `DataType`, `HmiDataType` e `AcquisitionCycle`, além de escala
  (`ScalingPlc*`/`ScalingHmi*`) e `AddressAccessMode: Symbolic`.

**`connections: []` do `list-hmi` é do modelo, não do projeto** (medido 2026-08-17):
`HmiTarget.Connections` volta vazio enquanto as tags do mesmo target apontam para
`HMI_Connection_2` no `LinkList` do export. Quem quer a conexão de verdade lê o
`export-hmi-tags`. Não bloqueia a etapa 2 — o nome da conexão está no XML que a própria etapa
vai clonar.

## F13 — objetos de dentro da tela ✅ (2026-08-17)

Três verbos que fecham "montar tela complexa", todos por SimaticML (export → edita → import
`Override`), porque a API do WinCC clássico não expõe o objeto de tela:

- **`list-screen-items --screen X [--like P] [--group]`** — uma linha por objeto (nome, tipo,
  x, y, w, h, tag). **150 objetos = 7,4 KB contra 798 KB do XML** (medido na tela Biofiltro).
  `--group` agrega pelo **1º código de equipamento da tag** (`..._BF-02-EC-01_STS_x` → `BF-02`) e
  devolve a `region` de cada um, que é o recorte pronto do `copy-screen-items`. O bbox é só dos
  objetos **com tag** — fundo e rótulo não carregam equipamento, então a região quase sempre precisa
  ser alargada até o retângulo de fundo, que aparece na lista.
- **`set-screen-items --set "Nome:x=,y=,w=,h="`** (repetível) — move/redimensiona. Um export e um
  import para N objetos; por objeto seria inviável (import de tela custou 70-168 s neste projeto).
  Nome ausente vira `missing` e os outros seguem; nome repetido na tela é erro, porque mover "um dos
  dois" seria adivinhação.
- **`copy-screen-items --from-screen <molde> --region x,y,w,h --screen <destino> --at x,y
  [--replace BF-01=BF-05]`** — a estampa. Copia o que está **inteiramente contido** na região
  (critério que faz "o cartão do motor" virar seleção sem nomear 13 objetos), desloca, renumera `ID`
  e desduplica `ObjectName`.

**Objeto de tela não fica na `ObjectList` da tela: fica dentro de `Hmi.Screen.ScreenLayer`.** Colar
no nível da tela passa no XML e o **Portal recusa no import** — *"The 'ScreenItems' composition at
line 17646 … is not supported"*. A cola vai na camada de **mesmo índice** do destino, que é o que
preserva visibilidade e ordem de desenho; camada que não existe no destino é erro com a contagem.

**Não há catálogo de estampas no CLI, e é decisão.** O cartão de motor do Biofiltro usa símbolo
`GraphicIOField` 77x63 com `Switch` 94x36 em dy +95/+138; o do Gradeamento, 87x58 com `Button`
93x39 em dy +208 — mesmo equipamento, dialetos diferentes, e o Gradeamento ainda por cima com tags
placeholder (`tag`, `tag1`, `aux`). Catálogo embutido envelheceria na primeira tela nova e mentiria
sobre qual é o padrão. O grupo sai da tela que serve de molde, em runtime — o molde é o projeto.

Smoke ponta a ponta no projeto-molde (`run --script`, 5 steps): `list --group` (4 skids
`BF-01..BF-04`) → `import-screen --replace` clonando a tela → `copy-screen-items` da região
`233,100,232,654` com `--replace BF-01=BF-05` → `list --group` na cópia: **150 → 187 objetos
(+37 = a coluna inteira)** e o skid **BF-05** presente com os mesmos 11 objetos com tag → `delete-screen`.
Checks offline em `ScreenItems.Core`: `Parse`, `Groups`, `Patch` (inclusive `missing`), `CopyInto`
(contenção, deslocamento, `--replace`, ID sem colisão, sufixo de `ObjectName`, cola dentro da camada).

O que a análise da tela real deixou medido e ainda não virou verbo: a tela é **4 colunas estampadas
à mão com deriva** — passo 244/244/**245** e derivas internas de (-1,+1), (-2,-1), (-4,-3) em ~8
objetos por coluna, mais um `TextField` 17x25 órfão em (966,100). É exatamente o que
`set-screen-items` corrige em lote, e é o argumento de por que estampar mecanicamente: cópia à mão
erra, cópia mecânica não.

### F13b — `set-screen-items` fecha o ciclo: `--remove`, `--rename`, `--group` ✅ (2026-08-17)

Três opções repetíveis no mesmo verbo, no mesmo par export/import (import de tela custou 58-123 s
nesta rodada — verbo separado por operação seria pagar isso três vezes). Ordem fixa **set → remove
→ rename → group**: agrupar por último porque a região se confere contra a geometria **já
corrigida** pelos `--set` da mesma chamada.

- **`--remove Nome`** — nome ausente vai p/ `missingRemove` e os outros seguem; repetido é erro.
- **`--rename Velho=Novo`** — destino já ocupado é erro (o Portal recusa `ObjectName` duplicado).
- **`--group NOME=x,y,w,h`** — embrulha num `Hmi.Screen.Group` os objetos **inteiramente contidos**
  na região (mesmo critério do `CopyInto`), dentro da própria camada. **Coordenada de filho é
  absoluta** no SimaticML (medido na tela Gradeamento, que já vem agrupada), então embrulhar não
  mexe em geometria nenhuma. Só objeto de 1º nível da camada entra — o que já está num grupo fica
  onde está. Região vazia devolve `items: 0` com nota, não exceção.

**Tela Biofiltro padronizada e agrupada (o teste real).** Plano derivado por comparação mecânica
contra a coluna 1: **53 `--set`** (passo exato 244 e as derivas internas), **1 `--remove`** (o
`TextField` órfão), **44 `--rename`** (`Switch_18` → `BF-01-EC-01_CMD_LIGA`, o nome saindo da
própria tag — R4 de `BOAS-PRATICAS.md` aplicado à tela) e **4 `--group`** (`SKID_BF-01..04`,
`233|477|721|965,100,232,654`). Rodado primeiro numa cópia (`import-screen --replace
Biofiltro=ZZ_TESTE Biofiltro`) e só então na tela real; a cópia morreu no `delete-screen`.

Aceite: `list-screen-items --group` devolve os 4 skids com **`region` idêntica** —
`244|488|732|976,154,209,529`, passo 244 exato e mesmo bbox nos quatro, contra o 244/244/245 com
deriva de antes — e 150 → **149 objetos** (o órfão). No XML, 4 `Hmi.Screen.Group` de **37 filhos**
cada; sobra `Text preliminar`, que é o título da tela.

**Só objeto com tag foi renomeado** (44 de 149): o nome vem do equipamento lido da tag e é
determinístico. Botão, retângulo e rótulo não têm tag — batizá-los seria adivinhação, e ficaram com
o nome do editor de propósito. Restam **3 `I/O field` com a tag placeholder `tag1`** (um por coluna,
BF-02/03/04), que é questão de engenharia da tela, não de ferramenta.

### F13c — o que a rodada da Biofiltro mostrou que faltava ✅ (2026-08-17)

Duas coisas foram feitas **fora do CLI**, em script descartável, e voltaram como verbo:

- **`list-screen-items` agora traz `group`** (nome do `Hmi.Screen.Group` que contém o objeto).
  Sem isso, conferir se o agrupamento pegou exigia abrir o XML de 800 KB na mão — foi exatamente o
  que aconteceu na 1ª rodada. Confirmado na tela real: `"group": "SKID_BF-04"`.
- **`set-screen-items --rename-from-tag`** — os 44 `--rename` saíram de um regex de sessão. Agora o
  nome sai da própria tag a partir do 1º código de equipamento, na tela inteira, **idempotente**:
  rodado de novo na Biofiltro já padronizada, `renamed: []` e `skippedRename` com os **3 `tag1`**
  nominalmente listados. Ou seja, o mesmo comando que padroniza também **denuncia o placeholder** —
  a pendência de engenharia aparece sozinha em vez de depender de alguém lembrar de procurar.

**O que ficou de fora, e por quê.** Um `--align` que derive os `--set` sozinho (comparar coluna
contra coluna) foi **rejeitado**: o plano desta rodada saiu de um pareamento por índice depois de
ordenar por (y, x), que só é confiável quando as colunas têm exatamente o mesmo elenco — e não
tinham (a coluna 04 vinha com o órfão). Errar o par significa **mover o objeto errado**. O caminho
canônico continua sendo o do F13: **regenerar a coluna** com `copy-screen-items --replace`, onde o
molde é a coluna boa e não há pareamento nenhum a adivinhar. Corrigir objeto a objeto é a exceção,
e para a exceção `--set` já basta.

**O custo continua sendo o import** (58-123 s por chamada, ~95% do verbo): a otimização real não é
verbo mais esperto, é **juntar tudo num `run --script`** — as 102 edições da tela foram 1 export e 1
import.

### F13d — `audit-screen` ✅ (2026-08-17)

**`audit-screen [--screen "Pasta/Sub/Tela"] [--device X] [--max N]`** — cruza a tag de cada objeto
de tela com as tags da própria IHM. Sem `--screen`, varre toda tela do device (um export por tela).
Saída no feitio do `audit`: `checks` com `ok`/`findings`/`detail` + `scanned` (`screens`, `items`,
`taggedItems`, `hmiTags`), que é o que distingue check conforme de check cego.

Dois checks e um `skipped`:

- **tag do objeto existe na IHM** — hoje tag de tela quebrada só aparece no compile do HMI, e o
  compile não diz qual objeto a usa.
- **tag do objeto tem código de equipamento** — o placeholder do editor (`tag`, `tag1`, `aux`)
  *existe* como tag, então passa no check anterior; o que denuncia é a ausência de código. Medido na
  Biofiltro real: 150 objetos, 47 com tag, 0 tag inexistente e os **3 `tag1`** (`I/O field_9/10/12`)
  nominalmente listados — os mesmos que o `--rename-from-tag` já apontava.
- **tag da IHM aponta pra tag do PLC** → `skipped`, e é **limite de API, não escolha** (ver
  `docs/LIMITES.md`): a tag de HMI clássica só expõe `Name`, e o SimaticML da tabela traz do vínculo
  só o nome da `Connection`. O nome da tag parece o caminho do PLC com `_` no lugar do separador,
  mas `_` também vive dentro de nome de membro — reverter seria adivinhação, e adivinhar num verbo
  de auditoria é pior que não checar.

**Varredura de device inteiro medida** (2026-08-17, IHM_2.1): 9 telas, 591 objetos, 175 com tag,
**86 s** — ~9,5 s por tela, que é um export cada. Escala linear: as 76 telas das 4 IHMs do projeto
dariam ~12 min. Serve como banho ocasional (o `raio-x` do lado da IHM), não como passo de iteração —
para iterar, `--screen`. Achado: **0 tag inexistente** em 175 objetos com tag nas duas IHMs varridas,
e 62 sem código de equipamento numa só (uma tela inteira de `tag1`, o Decantador Secundário). O
buraco real do projeto não é tag quebrada, é tag **não ligada**.

**Prova negativa do check 1** (2026-08-17): o check "tag existe na IHM" fechou 0 achados em 175
objetos com tag nas telas reais, e check que nunca acusa é indistinguível de check cego. Montada
tela de smoke a partir da menor tela real (`1_Telas_Menu/4.Ajustes` da IHM_1.1: 18 objetos, 1 com
tag) — `export-screen` → `import-screen --folder ZZ_TESTE --replace "4.Ajustes=ZZ_TESTE_AUDIT"
--replace "Calibração=BF-01-EC-99_CMD_FAKE"`. A tag falsa leva **código de equipamento de
propósito**, para que só o check 1 possa acusar: o resultado foi `ok: false` com
`"Symbolic I/O field_1 : BF-01-EC-99_CMD_FAKE"` no check 1 e o check 2 **verde**, que é a prova de
que os dois checks são independentes e não um só disfarçado de dois. Tela apagada com
`delete-screen --apply` e ausência conferida no `list-hmi` (a pasta `ZZ_TESTE`, vazia, fica — não há
verbo de apagar pasta de tela).

**Gradeamento Mecanizada com a cadeia nova** (2026-08-17, tela real, IHM_1.1): 142 objetos, 50 com
tag, **35 placeholders** (`tag`, `tag1`, `aux`) contra 3 da Biofiltro — os 15 restantes foram
renomeados e o `--rename-from-tag` repetido depois voltou `renamed: []`, provando idempotência
contra tela já padronizada.

**`--group` não se aplicou ali, e o motivo é o achado**: a tela **já vinha agrupada**, com 9
`Hmi.Screen.Group` de nome de editor (`Group`, `Group_1`, …), e **nenhum** dos objetos agrupados tem
tag com código de equipamento — são justamente os placeholders. Batizar grupo a partir dos filhos
seria adivinhação, exatamente o que o `--rename` recusa fazer para objeto sem tag. Grupo se batiza
depois que as tags forem ligadas; antes disso o verbo não tem de onde tirar o nome.

## F14 — área nova ponta a ponta ✅ (2026-08-18)

Primeira vez que a cadeia inteira de uma área nova rodou numa sessão só, no projeto-molde: área de
smoke `ZZ_TESTE (ZT-01)`, equipamento `Bomba ZZ Teste (B-99A)`, molde `Bomba Centrífuga 1 (B-04A)`
da Elevatória de Purga de Lodo (CCM2). Terminou em `compile` 0 erros / 0 avisos e **`audit` 10/10
verde** com a área dentro (`scanned`: 106 pastas, 530 blocos, 53 blocos de chamada, 208 tabelas), e
foi apagada em seguida sem deixar resíduo.

**O achado da fase: são cinco pré-requisitos, não três.** Os três do acionamento já estavam
escritos; os dois de baixo apareceram aqui.

1. Pasta do equipamento com o `(ID)` no nome.
2. Membro UDT do equipamento no DB global — sem ele o `replicate-fc` pula o alvo com
   `Target 'X' has no global-DB instance. Skipped.`
3. Tags `<ID>*MODO_LOCAL` / `MODO_REMOTO` — a falta é aviso **depois** de os 6 blocos já terem sido
   escritos, e exige 2ª passada.
4. **Drive SINAMICS no hardware, para área de inversor.** A FC replicada referencia a constante de
   HWID do drive, e o compile do PLC inteiro reprova com
   `Tag "INVERSOR_B-99A_CCM2~PROFINET_interface~Standard_telegram_20" not defined.` A constante não
   nasce com o drive, nem com o telegrama: nasce quando o drive vira IO device do controlador. A
   sequência que resolveu, 4 verbos: `add-device --mlfb OrderNumber:6SL3244-0BB12-1FA0/4.7.13`
   (mesmo MLFB dos 42 inversores do projeto) → `insert-telegram --number 20 --change` (o G120 novo
   nasce com o `MainTelegram #1` e telegrama Main não se apaga) → `connect-subnet --subnet PN/IE_1
   --io-system "PROFINET IO-System"` → constante criada (`Hw_SubModule`, valor 929). Nome de device
   com espaço (`INVERSOR_B-99A CCM2`) vira `_` na constante, que é como o nome do molde casa com o
   texto que o `replicate-fc` escreveu.
5. **Pasta de tags em `2. Alarmes` de mesmo nome-base da pasta de `3. Partidas`, mais
   `<AREA>.ALARMES.WORD_ALARMES_1 : Word` no DB global** — pré-requisito do `gen-alarm-fc`, que ao
   menos **diagnostica sozinho** (o aviso vem com o `add-db-member` pronto para copiar).

**`replicate-instruments` numa área sem instrumento é no-op limpo**: `ZZ_TESTE` não aparece entre as
8 áreas do dry-run e `warnings` volta vazio. Área sem tag de instrumento na pasta de alarme
simplesmente não entra na iteração — não há nada a configurar para excluí-la.

**A tela replica, as tags da IHM não.** `import-screen --replace "B-04A=B-99A"` criou a tela da área
em `2_Telas_Detalhamento/QA-02/99.ZZ_TESTE (ZT-01)` (o `--replace` troca texto no XML, e a tela liga
tag por nome), mas as 5 tags trocadas não existem na IHM — e é o `audit-screen` que diz quais, uma a
uma, em vez do compile do HMI. **Falta o verbo `import-hmi-tags`**: a API existe
(`Siemens.Engineering.Hmi.Tag.TagTableComposition.Import(FileInfo, ImportOptions)`, achada com
`tia-help.py --sdk "TagTable Import"`), o CLI só tem o `export-hmi-tags`. É o único elo da cadeia de
área nova que ainda pede GUI.

**Limpeza em um batch de 18 verbos, 17,3 s** (`workspace/zzarea/cleanup.json`): as 2 redes de
chamada, os 8 blocos, as 4 pastas, as 2 tags de I/O, a tela e o device. O membro do DB global saiu
à parte (`delete-db-member --db "DB GLOBAL" --name ZZ_TESTE`), porque escrita no DB global é o verbo
caro. Conferência: `find --pattern "*B-99A*"`, `"*ZZ_TESTE*"` e `"*ZZ*" --kind block` os três com
`count: 0`, e `compile` de volta a 0 erros. Sobrou só a **pasta de telas vazia** `99.ZZ_TESTE
(ZT-01)`: não há verbo que apague pasta de tela na IHM (`delete-folder` é do PLC).

## F12 — `sim-diag` camada 1 ✅ (2026-08-17)

**`sim-diag [--instance plc_1500_1] [--watch SEG]`** — diagnóstico do PLC virtual. Roda **antes do
attach** (o `case` fica no bloco pré-`Attach()` do `Program.cs`, junto de `open-project`): a API do
PLCSIM é independente do Openness, então o verbo **não precisa de TIA Portal aberto nem de projeto**
e não paga os ~7 s do attach. É o único verbo de leitura do repo que roda com o Portal fechado.

Retrato: `state`, `operatingMode`, `cpuType`, `articleNumber`, `licenseStatus`, `ip` (as duas),
`systemTime`, `storagePath`, `cycleTimeMonitoring` (`mode` + `ns`), `tagList`
(`details`/`upToDate`/`count`). Campo que a API recusar vira `"error: …"` no seu lugar em vez de
derrubar o retrato — instância desligada responde quase tudo (medido: só `controller` e
`shortDesignation` voltam vazios, e `tagList.count` = 0).

**LED não tem getter na API** — só o evento `OnLedChanged`. Por isso `--watch SEG`: assina LED,
mudança de estado operacional e falha de rack/estação, dorme a janela e devolve o que **mudou**,
com `atMs` relativo ao início. Sem `--watch` não há estado de LED para reportar, e o `ledNote` do
JSON diz isso. Lista vazia = nada mudou na janela, não "não funciona".

Provado no `plc_1500_1` com uma janela de 45 s cruzando um `sim-host.ps1 -Start`: **13 eventos**,
o boot inteiro em ordem — `Off→Booting` (4131 ms), `Booting→Stop`, `Stop→Startup`,
`Startup→Run` (6527 ms), com os LEDs `Stop On → FlashFast → Off` e `Run FlashFast → On`. Um valor
de `ELEDType` sai como número (`"10"`): o enum documentado vai até `Busf4`, e o valor cru é melhor
que engolir o evento. `stateAfterWatch` existe porque o `state` do retrato é lido **antes** da
janela: sem ele, retrato tirado durante o boot volta dizendo `Off` com a lista mostrando `Run`.

**Camada 2 (ler tag de alarme e cruzar com o texto dos blocos) não foi feita, e por ora não vale**:
`sim-run --no-download` com passos `read` já lê tag de alarme, e cruzar com bloco é `xref`/
`explain-block`. Verbo novo só se aparecer o caso de uso que essas duas peças não cobrem.

## Teste cego ponta a ponta — caderno escrito (2026-08-07)

A prova que a Siemens vai pedir ("um agente consegue mesmo?") virou experimento com régua escrita
antes da rodada, em `docs/teste-cego/`:

- **`caderno-FP-01.md`** — memorial fictício de um filtro prensa de sala de desidratação: máquina
  com função clara, CPU 1515-2 PN + ET200SP + G120, 28 pontos de I/O endereçados, 9 passos de
  sequência, 8 intertravamentos, 12 alarmes. Escolhido de propósito para **não** cair inteiro na
  biblioteca: acionamentos e instrumentos são território de `install-lib`/`replicate-*`, mas a
  sequência com timeout por passo e os intertravamentos de segurança têm que ser autorais. Se a
  máquina fosse só motor e instrumento, o teste mediria a biblioteca, não a engine.
- **`criterios.md`** — 4 portões objetivos (compila 0 erros · hardware conectado com telegrama ·
  os 28 endereços fiéis à lista · sequência chamada por OB cíclico) e 4 pontos de inspeção
  (lógica de fato implementada · padrão de pastas · segurança não diluída · quanto veio de gerador).
  Os portões passam com comando, sem julgamento.

MLFBs do caderno são os já exercitados neste repo (`6ES7 515-2AN03-0AB0/V3.1`,
`6SL3244-0BB12-1FA0/4.7.13`) — falha de catálogo no meio da prova seria ruído de digitação, não
resultado. Os módulos da ET200SP ficam **em aberto de propósito**: o caderno pede contagem de
pontos e deixa o código pro integrador, que é como obra real chega.

**Regra de condução: quem escreveu o caderno não executa.** A sessão cega recebe o caderno + a
skill e mais nada; o arquivo de critérios não vai junto.

**Busca em rodada cega exclui `docs/teste-cego/` explicitamente** (regra desde 2026-08-13, tirada do
vazamento da FP-04): lista de não-ler não vale para `grep` — um `grep` em `docs/` bateu em resultado
antigo e mostrou MLFBs que a rodada deveria descobrir sozinha. Vazamento que acontecer, registrar
como aconteceu, em vez de anular a rodada. **O produto do teste são os tropeços** — cada
travada separada entre "o caderno não dizia" (esperado, obra real também não diz) e "a ferramenta
não dizia" (defeito nosso, e provavelmente do `SKILL.md`).

### FP-03 executada (2026-08-10) — agitador `AG-05`, partida direta

Caderno [`caderno-FP-03.md`](teste-cego/caderno-FP-03.md), resultado em
[`resultado-FP-03.md`](teste-cego/resultado-FP-03.md). O programa saiu conforme (compila 0/1,
`audit` 5/6, 10 blocos novos, 8 deles instância ou clone da biblioteca) — o resultado do teste são
os 10 tropeços, e a fila que sai deles:

1. `add-call` + `delete-network` — **R8 (chamada em LAD) hoje só é alcançável escrevendo FlgNet na
   mão**; foram 276 linhas de Python para uma rede de chamada. O buraco é **chamada nova**: quando a
   chamada já existe em algum molde, `clone` resolve (foi assim que o IIT-05 saiu em 1 chamada).
2. **Um guard só** na coreografia `export → patch → Import Override` dos `*-db-member`: conferir o
   patch depois de importar e compilar quando o alvo está modificado-não-compilado. Resolve o
   `edit-db-member` devolvendo `ok: true` sem efeito (bug) e a cadeia de `--like` que quebrava.
3. `set-retain --block <FB> --member M` — `Remanence` não pode ser setado em iDB, só no FB, e o
   `import-source` não expressa retentividade: entregar "horímetro retentivo" obrigou a
   reimplementar o horímetro fora da biblioteca.
4. `list-interface` — reconstituir o padrão da casa custou 12 chamadas de leitura. O leitor de
   interface é o mesmo que o `add-call` precisa para resolver tipo de parâmetro.
5. `clone --with-instances`; 6. `audit` reconhecendo acionamento sem inversor;
   7. `create-instance-db` sugerindo o nome do FB quando o exato não casa (acento e espaço duplo).

**Fora da fila:** `tree` carregando assinatura de FB (custa export de todo FB para responder o que
`list-interface` responde sob demanda) e `add-db-member --from-scl` (com o guard do item 2, a cadeia
de `--like` deixa de quebrar).

### Fila da FP-03 executada (2026-08-11) — 72 → 76 verbos

Os 7 itens saíram, com teste offline para cada núcleo puro (`Tia.Tests`, 8 casos novos):

| Item | Como ficou |
|---|---|
| 1. `add-call` + `delete-network` | `BlockEdit.cs`. `add-call` monta a rede LAD (EN no powerrail), tipa os pinos pela interface do FB e recusa entrada sem valor; `delete-network --index N` é 1-based. **Aceite ao vivo**: rede inserida no `PARTIDA_AGITADOR_5 (AG-05)`, compile 0 erros, rede removida, FC de volta às 10 redes originais. |
| 2. guard do `*-db-member` | `Ops.ImportAndProve`: import `Override` → compila o alvo → re-exporta e confere o patch. `DbMember.ExportFresh` compila antes de exportar quando o bloco está sujo. Falha alta em vez de `ok: true` vazio. |
| 3. `set-retain` | `--block <FB> --member M [--off]`; recusa iDB com a mensagem do Openness. |
| 4. `list-interface` | `[--folder|--name|--file]`, um attach para a pasta inteira; `--file` roda sem TIA. É de onde o `add-call` tira tipo e seção de cada pino. |
| 5. `clone --with-instances` | `Clone.InstancesInXml` acha as chamadas com instância própria (multi-instância fica de fora) e chama `create-instance-db`, que é idempotente. |
| 6. `audit` sem inversor | com telegrama/setpoint exige 6 blocos; sem, exige o trio `PARTIDA_* + FALHA + CONDIÇÃO DE PARTIDA`. Partida direta deixa de reprovar. |
| 7. `create-instance-db --of` aproximado | `Ops.Squash`: acento, caixa, underscore e espaço duplo não contam. Casou um, resolve e devolve `resolvedFrom`; casou vários, falha listando. |

### FP-04 escrita (2026-08-11) — aeração `Sopradores/Aeração`, dois sopradores com inversor

Caderno em [`caderno-FP-04.md`](teste-cego/caderno-FP-04.md), **ainda não executado** — a régua é a
mesma de [`criterios.md`](teste-cego/criterios.md), e quem escreveu não executa: a rodada tem que
sair em outra sessão, recebendo o caderno e a skill `tia`, nada mais.

O caderno foi desenhado para bater na superfície que a FP-03 acrescentou e que nunca passou por
rodada cega, sem citar verbo nenhum:

| O que o caderno pede | Superfície que ele exercita |
|---|---|
| área de nome `Sopradores/Aeração` (barra é da folha de dados) | `create-folder`/`--folder` com `\/`, e o `\\/` dentro de `run --script` |
| dois inversores PROFINET novos, com comando/referência/estado pela rede | `insert-telegram --change` (drive nasce com `MainTelegram #1`), `connect-subnet` nas duas ordens com `--io-system`, e o endereço do telegrama por `list-io-map` — o caso real que motivou o verbo e segue sem prova |
| periferia remota nova dimensionada pelos instrumentos | `list-io-map` para achar o próximo byte livre |
| dois sopradores + rodízio + alarme de área | UDT por soprador e DB global de agregados (R1/R2 do `audit`), bloco de chamada em LAD na pasta da área (R8 + `CHAMADA_*` fora da pasta) |
| horímetro, contador de partidas e horas desde o rodízio, retentivos | `set-retain` no FB (o `import-source` não expressa retentividade) |
| lógica de rampa/banda morta que nenhum molde da casa tem pronta | `clone --with-instances` + `delete-network` + `add-call` depois de `list-interface`, e o guard de compile-e-confere em duas escritas seguidas no mesmo bloco |

Os 4 checks novos do `audit` (R1, R2, R8, `CHAMADA_*` fora da pasta) só foram vistos **passando**.
Um programa de duas máquinas com parametrização de IHM é onde eles têm chance de reprovar — e
reprovar é o produto do teste.

### FP-04 executada (2026-08-11) — aeração, dois sopradores com inversor

Resultado em [`resultado-FP-04.md`](teste-cego/resultado-FP-04.md). **45 min** (15:05→15:50),
~30 chamadas de verbo. Entregue: ET200SP nova (DI 8x + AI 4xI, 50 % de reserva nos dois), dois G120
com telegrama 20 no IO system do `PLC_ZERO`, 23 blocos, 6 UDTs, 6 tabelas de tag, DB global com
**um** membro agregado. `compile` 0 erros; `audit` **9/10**, com justificativa escrita no único que
reprova (o 6º bloco do molde é iDB de um FB que o próprio `MOTOR_01` do projeto não chama).
Os **4 checks novos passaram** — continuam sem ter sido vistos reprovando.

**Encaminhado em 2026-08-12 sem gastar uma rodada nisso.** A dúvida real não era "reprova?" e sim
"olhou alguma coisa?" — check que passa por população vazia é o modo de falha do `--folder` do
`list-blocks` (`count: 0`, `ok: true`). O `audit` passou a devolver **`scanned`**
(`folders`/`blocks`/`callBlocks`/`tagTables`), e no projeto-molde real dá 96 / 475 / **46** / 195
com 10 checks verdes: o R8 examinou 46 blocos de chamada com linguagem conhecida e aprovou os 46.
Os predicados (`IsCallBlock`, `IsLooseScalar`) já têm teste offline reprovando o caso ruim; o que
faltava era a prova de que a população chega neles, e é isso que o `scanned` mostra em toda rodada.
Ver os 4 acusando em projeto real continua valendo — como subproduto da próxima rodada cega, não
como rodada própria.

O programa saiu conforme; o produto do teste foram **9 tropeços da ferramenta**, e a rodada custou
mais em contorno de CLI do que em engenharia: só o T1 comeu 25 dos 45 min.

**Fila da FP-04** (ordem = dor evitada ÷ tamanho do diff):

| # | Item | Onde | Por quê |
|---|---|---|---|
| 1 | `add-call` com `--inst` opcional → chamada de FC | `BlockEdit.cs` | O `CHAMADA_*` do padrão é sequência de chamadas de **FC**, o caso mais comum da R8, e o verbo só monta FB. 25 min montando 5 `CompileUnit` na mão. |
| 2 | `add-call` emitindo `LiteralConstant` + `ConstantType` pelo tipo do pino | `BlockEdit.cs` | `=TRUE` num pino Bool morre em `'ConstantValue' has the invalid value 'TRUE'`. `Time` passa. É pino do `FB FALHA`, o mais chamado da biblioteca. O tipo já vem da interface. |
| 3 | `ioSystemAction: create\|reuse` no dry-run do `connect-subnet` | `Hardware` | Nenhum verbo devolve o nome do IO system existente; achar o do `PLC_ZERO` custou `export-cax` + grep num AML de 1,5 MB. |
| 4 | `Ops.ImportAndProve`: separar "não apliquei" de "apliquei e não provei" | `Ops.cs` | `add-db-member` reportou `ok:false` **depois** de o patch entrar (o export de prova é que falhou, com `compile` tendo dito 0 erros). Quem lê o erro desfaz o que estava certo. |
| 5 | `Ops.SplitPath` no filtro de `--folder` do `list-blocks` | `Blocks` | Falso-negativo silencioso: `count: 0`, `ok: true` para pasta com `/` no nome. Documentado como exceção no `CLAUDE.md` enquanto não sai. |
| 6 | `plug-module --type` sem o dump de `freeSlots`, e listagem do catálogo plugável no slot | `Hardware` | Sondar 9 MLFBs custou ~330 linhas de JSON para 9 úteis, e o sufixo de versão do MLFB (`/V0.0`, `/V2.0`, `/V1.0`) não tem regra — só o GUI mostra o catálogo do slot. |
| 7 | Help do `clone --replace` dizendo que é troca textual no XML | `Program.cs` (`VERBS.md` é gerado) | Uma linha contra o undo de 26 steps do T2. Já escrita no `CLAUDE.md`. |
| 8 | Promessa do `list-io-map` sobre endereço de telegrama de drive | verbo ou `CLAUDE.md` | `--device <drive>` volta vazio com telegrama posto e IO system conectado; os itens do drive caem em `unassigned`. `CLAUDE.md` já corrigido — falta decidir se o verbo passa a alcançar. |

### Fila da FP-04 executada (2026-08-11) — 6 dos 8 itens, e um bug novo achado no aceite

| Item | Como ficou |
|---|---|
| 1. `add-call` chama FC | `BlockEdit.cs`. `--inst` é opcional: exigido para FB, recusado para FC; sem pino, o `CallInfo` se fecha sozinho. **Aceite ao vivo**: chamada de `FC_ALARMES_SOPRADORES_AERACAO` inserida no `CHAMADA_AREA_03_*`, compile limpo, rede removida depois. |
| 2. constante tipada pelo pino | `TRUE` num pino Bool sai `LiteralConstant` + `ConstantType`; `T#5S` continua `TypedConstant` (é o que o Portal escreve, conferido em export real). **Aceite ao vivo**: `FB FALHA` chamado com os 11 inputs constantes, `INPUT_HABILITA_CONJUNTO=TRUE` inclusive. |
| 3. `connect-subnet` diz o IO system | Dry-run devolve `ownedIoSystem`, `ioSystemsOnSubnet` e `ioSystemAction` (`create/exists/join/move/already/missing`), e a CPU que já tem outro IO system falha nomeando o dela. Ao vivo: `PROFINET IO-System_PLC_ZERO` numa chamada read-only, contra o `export-cax` de 1,5 MB. |
| 4. `ImportAndProve` não mente mais | Export de prova que falha agora compila o PLC inteiro e tenta de novo; se ainda falhar, a mensagem abre com **"JÁ ENTROU … não repita nem desfaça"** em vez de um `ok:false` que faz o agente desfazer o que estava certo. |
| 5. `list-blocks --folder` com `\/` | `Ops.SplitPath` no `Inventory.FolderMatches`. Ao vivo: `5.1.3 Sopradores\/Aeração` devolve os 8 blocos (era `count: 0`). |
| 6. `plug-module` enxuto | Com `--type`, os `freeSlots` saem da resposta (eram ~330 linhas para 9 `canPlug`). O catálogo do slot **não tem API** — `CanPlugNew` é a única pergunta que o Openness responde (confirmado no SDK), então em vez de listar, o verbo **sonda 11 sufixos de firmware** quando o MLFB vem sem `/V` e devolve `plugAs` com o que passa. Não deu para provar ao vivo na própria rodada: em `LIB_TESTE` todo slot livre recusa até o MLFB versionado que a rodada plugou, então `canPlug` ficou preso em restrição de slot, não de versão. **Provado em 2026-08-11** no projeto-molde real: `plug-module --device "ET 200SP station_1" --item Rack_0 --type "OrderNumber:6ES7 131-6BH00-0BA0" --pos 5` (dry-run) devolve `plugAs: ".../V1.0"`. O que estava faltando era o alvo: plug é no **rack**, não no device — sem `--item Rack_0` o slot pertence a outra composição e `canPlug` é sempre falso, com ou sem versão. Está no help agora. |
| 7. help do `clone --replace` | Escrito no help (e no `CLAUDE.md`): troca textual no XML, caminho de DB é cadeia de `<Component>`, mesma profundidade na origem e no destino. |
| 8. `list-io-map` × telegrama de drive | **Resolvido em 2026-08-11, e era bug de verdade, não limite do Openness.** O `--sdk` achou `MC.Drives.Telegram.Addresses` em um comando: o endereço do telegrama existe, só não em `DeviceItem.Addresses`. `list-telegrams` passou a devolver `addresses[]` por telegrama e o `list-io-map` a varrer os drive objects. **Aceite ao vivo** no projeto-molde real (34 drives): `list-telegrams --device "SINAMICS G_23"` dá `%IB256..267` / `%QB256..259`, e o mapa do projeto foi de 45 para 113 endereços — com `nextFreeByte` corrigido de `Input: 156` para `664` e `Output: 46` para `392`. O número velho era resposta errada, não resposta faltante: quem pedisse o próximo byte livre recebia byte já ocupado por telegrama. |

**Bug novo, achado no aceite ao vivo do item 2:** o `add-call` declarava **todos** os pinos do bloco
e ligava só os que tinham valor. O Portal recusa o import disso — *"The connection with the name '12'
is not connected to the object with the UID '32'"* —, e em todo `Call` de export real vale
`wires == parâmetros + 1` (o `en`). Passou despercebido na FP-03 e na FP-04 porque as duas só
chamaram bloco com **todos** os pinos preenchidos. Agora só o pino com fio entra declarado, com
teste offline fechando a conta.

**Fora da fila, registrado:** o "Contexto de execução" do caderno errou ao afirmar que `LIB_TESTE`
não tinha periferia nem inversor — tinha (`ET200SP_QA` sem cartões, dois G120 do CCM_01). Não
atrapalhou; serviu de gabarito de MLFB.

**Vazamento da regra cega, assumido pela própria rodada:** um `grep` em `docs/` bateu em duas linhas
de um resultado antigo (MLFBs de ET200SP). O executor não abriu o arquivo e registrou o efeito
possível. Lição de método para a próxima rodada: **busca em rodada cega exclui `docs/teste-cego/`
explicitamente** — lista de não-ler não vale para `grep`.

### Fila da FP-05 executada (2026-08-12) — 7 de 7, tudo provado ao vivo

A rodada está em [`resultado-FP-05.md`](teste-cego/resultado-FP-05.md) (Área 24 de recirculação no
projeto-molde real, 32 min, `audit` 10/10). A Área 24 em si foi revertida por um reboot da máquina,
que era o undo previsto; o que ficou foi a fila de 7 tropeços, fechada no mesmo dia.

| Item | Como ficou |
|---|---|
| 1. `add-call` aceita FB sem pino (T5) | `BlockEdit.cs`. A guarda `has no Input/Output/InOut` morreu; o `CallInfo` de FB **não** se autofecha, o `<Instance>` mora dentro dele. **Aceite ao vivo**: FB sem interface nenhuma chamado num FC LAD, `parameters: 0`, compile Success 0/0. Bloco de área que só opera sobre tag global e estática retentiva não precisa mais de pino inventado. |
| 2. `nextFreeByte` honesto (T2) | `Hardware.cs`. Não virou verdade — virou **piso declarado**: `nextFreeByteExact` + `nextFreeByteNote` quando há `unassigned` ou filtro. Não existe API de "next free address" no Openness (conferido no `--sdk`); a autoridade continua sendo o `Next free address: N` da mensagem de erro do Portal. A nota sai **antes** do `map`, senão cai fora do head do `--out-file`. |
| 3. `add-db-member --path` cria o ramo (T4) | `DbMember.cs`. Segmento inexistente nasce `Struct` **no mesmo XML** do membro-folha, então o DB nunca chega vazio no Portal — que era exatamente o que a guarda de `--type Struct` protegia (ela continua de pé). `structsCreated` diz o que nasceu. **Aceite ao vivo**: `--path ZZ_TESTE_AREA.ALARMES --name ALM_ZZ --type Bool` na `DB GLOBAL`, compile Success 0/0, segunda chamada reusando o ramo. |
| 4. `networksBefore/After` (T7) | `add-call` e `delete-network` devolvem os dois; `clone` devolve `networks`. **Aceite ao vivo**: clone de `MOLDE_ANALOGS` devolveu `networks: 2` (o molde tem 3 — a rede vazia não sobrevive ao export, que é a armadilha do T7), e o `delete-network` seguinte, `2 → 1`. |
| 5. `connect-subnet` lista as subnets (T1) | Nome que não casa devolve `existingSubnets` em vez de só `subnetAction: create`. Ao vivo: `["PN/IE_1", "PN/IE_2"]`. |
| 6. `nextFreeByteInDevice` (T3) | O campo muda de nome quando há `--device`: é o próximo livre daquele device, não do projeto. |
| 7. Régua do pino solto (T6) | Decidida a favor do projeto de referência: **Input sem valor é `warning`**, fica solto na rede como no molde da casa. **`InOut` sem valor continua erro** — referência sem fio não compila. |

Método: os aceites ao vivo saíram de dois batches num projeto de teste (blocos `ZZ_TESTE_*` +
pasta + ramo na `DB GLOBAL`), apagados depois, com compile limpo antes e depois.

### FP-06 executada (2026-08-13) — elevatória de 5 bombas em inversor SINAMICS

Rodada em [`resultado-FP-06.md`](teste-cego/resultado-FP-06.md); a entrega, em
[`entrega-FP-06.md`](teste-cego/entrega-FP-06.md). **49 min**, projeto-molde real, área 24
`Elevatória Final (EFE-01)`: 5 G120 PN com telegrama 20, uma ET 200SP nova, 46 blocos novos,
**compile Success 0/0**, **`audit` 10/10** (476 → 522 blocos, 36 → 41 acionamentos), projeto salvo.

O que a rodada mediu:

- **As quatro armadilhas da `BOAS-PRATICAS.md` sem check que reprove (R3, R4, R5, R7) foram
  recusadas com motivo escrito.** A doutrina sozinha orientou a decisão — é o que a rodada existia
  para saber.
- **80 % dos blocos vieram de gerador** (`replicate-fc` 24, `replicate-instruments` 10,
  `gen-alarm-fc` 3). Contorno de CLI caiu de ~32 % (FP-05) para **~12 %**.
- **Cinco dos sete consertos da FP-05 seguraram em projeto real** (`add-call` com pinos,
  `add-db-member --path` → `DB GLOBAL` hierárquica, `list-io-map` com piso declarado,
  `connect-subnet` com `existingSubnets`, `networksBefore/After`). `set-io-address --conflictCheck`
  não foi exercitado; `add-call --fb` com prefixo de tipo doeu (T2 da fila nova).

**Fila fechada no mesmo dia (2026-08-13)**, cada item conferido contra o projeto real — tabela em
[`resultado-FP-06.md` §6.1](teste-cego/resultado-FP-06.md): **T3** `replicate-instruments` acha o
`_PV_` no PLC inteiro e declara `pvTag` · **T5** `BlockEdit.Patch` compila o alvo antes de exportar
(`set-retain`/`add-call`/`delete-network` de uma vez) · **T2** `add-call --fb` aceita o prefixo
`FB `/`FC ` · **T1** `plug-module` normaliza o MLFB e explica o `canPlug: false` · **T6**
`gen-alarm-fc --area` · **T4** a mensagem do molde cita `MoldInstrumentId`.

Fora da fila, o **acionamento-semente** (seção 7 do resultado, ~10 min da rodada) virou verbo:
`replicate-fc --template <pasta molde> --target-folder <pasta da área>` replica de qualquer área
para a área nova, com escopo — antes o molde era "a 1ª irmã populada" e os alvos, "todas as irmãs".

### Revisão da série FP-01→FP-06 (2026-08-13) — o que a série mede e o que ela deixou passar

Revisão pedida depois de fechar a fila da FP-06, cruzando handoffs, cadernos, resultados e código.
Achados que viraram trabalho no mesmo dia, e o que ficou de fila.

**Feito (mesma data):**

1. **Um pré-compile só para todo export** (`Ops.ExportFresh`, sobrecarga para `PlcBlock` e
   `PlcType`). O repo tinha **três políticas para o mesmo estado**: `BlockEdit.Patch` e
   `DbMember.ExportFresh` compilavam sozinhos, `Ops.ExportBlock` lançava erro pedindo
   `compile --block`, e `AlarmFc`/`FaultOb`/`InstrumentFc`/`Replicate`/`BlockInterface` exportavam
   cru (o agente recebia a mensagem bruta do Openness). Eram 16 exports: 2 com pré-compile, 1 com
   erro traduzido, 13 sem nada. Agora os 16 passam pelo mesmo helper — compila **só o alvo** e
   segue; inconsistência vinda de fora (UDT/DB usado pelo bloco) volta com a mensagem mandando
   `compile --apply`. **Motivo medido:** compile do PLC inteiro foi ~20 dos 49 min da FP-06 (41 %
   do relógio da rodada), e o compile de um bloco é de segundos. `CLAUDE.md`, `SKILL.md` e
   `BENCHMARKS.md` corrigidos — os três ensinavam a regra antiga.
2. **`run --script` cronometra.** Cada step traz `ms`, o batch traz o total, e `--summary` traz
   `slowest[3]`. Sem isso, medir rodada exigia `Measure-Command` por fora e o número não
   sobrevivia no resultado — que é por que "onde foi o tempo" nunca virou série comparável.
3. **FP-07 escrita como rodada de dívida** ([`caderno-FP-07.md`](teste-cego/caderno-FP-07.md) +
   [`criterios-FP-07.md`](teste-cego/criterios-FP-07.md)): adensador de lodo, três acionamentos
   idênticos em partida direta, **endereços fixos do diagrama elétrico**, cartões por MLFB de lista
   de compra, diagnóstico de falha de estação, e entrega **em duas etapas com `audit` ao fim de
   cada uma**. O caminho natural da entrega passa por cima de cada conserto que nunca foi
   exercitado — `replicate-fc --template --apply`, `set-io-address --conflictCheck` (2 ciclos
   parado), `plug-module --apply`, `gen-fault-ob` (4 ciclos), e as **duas fotos do `audit`**, que é
   a primeira chance real de ver check reprovando (aberto desde a FP-04).
4. **`SKILL.md` alinhado ao código** — não trazia `replicate-fc --template`, `gen-alarm-fc --area`,
   `set-io-address --conflictCheck` nem o prefixo opcional do `add-call --fb`. A sessão cega recebe
   `SKILL.md`, então conserto que não chega lá não existe para quem executa (é a origem do T2 da
   FP-06).
5. **`CLAUDE.md` enxuto** de 22,9 KB para 19,6 KB, tirando a arqueologia de tropeço ("que até a
   FP-04 ignorava…") e mantendo toda regra viva. O histórico não se perdeu: mora nos
   `resultado-FP-*.md`. É arquivo re-enviado todo turno — o repo estava violando a própria regra de
   orçamento de contexto no maior arquivo que tem.

**Fila que saiu da revisão — 5 de 7 fechados no mesmo dia:**

| # | Item | Estado |
|---|---|---|
| 1 | Régua-base fixa + anexo por rodada | ✅ [`regua-base.md`](teste-cego/regua-base.md): condução invariante, portões `G-A`…`G-D` e as **seis métricas comparáveis `M1`…`M6`** (relógio, chamadas, contorno de CLI, origem dos blocos, tropeços, cliques de GUI). O `criterios-FP-07.md` virou anexo e é o primeiro a usá-la; as rodadas anteriores ficam como estão, que é o registro do que valia na época. A régua vinha sendo reescrita inteira a cada rodada, e o único número que se comparava (contorno de CLI, 32 % → 12 %) era confundido pelo terreno — agora `M3`/`M4` só se leem junto com uma linha sobre o terreno. |
| 2 | Re-testar `import-master-copy --force --apply` em CPU virgem | ⏳ Fix commitado em `a0df2f7` e **nunca re-testado** — dívida aberta mais antiga do repo. Junto com `install-lib`, exige rodada de projeto novo, que a FP-07 não é. |
| 3 | "Busca em rodada cega exclui `docs/teste-cego/`" vira regra escrita | ✅ subiu para o protocolo geral (seção do teste cego, acima) e para a régua-base. |
| 4 | Conferência do caderno contra o projeto antes da rodada | ✅ virou item de condução da régua-base, e foi feita na FP-07 — **achou três erros de fato** (ver abaixo). |
| 5 | Telemetria do ramo caro do `ImportAndProve` | ✅ uma linha em `workspace/telemetry.log` por queda no compile-do-PLC-inteiro (`Ops.LogFallback`), com bloco, o que se editava e o erro do export. Contador em memória não serve: cada `tia` é processo novo, então "quantas vezes por rodada" é `wc -l` no arquivo. Telemetria nunca derruba verbo — falha de escrita é engolida. |
| 6 | F9 diz 77 verbos; são **78** | ✅ conferido 1:1 contra o `Program.cs`; `VERBS.md` sai do help com 78. |
| 7 | Terreno da série é sempre o mesmo | ⏳ 4 rodadas cegas, todas ETE, todas área de acionamento no mesmo projeto-molde. Máquina sequencial só na FP-01, que **não foi cega**. HMI, nunca. |

**A conferência do caderno FP-07 pegou três erros de fato** (detalhe em
[`criterios-FP-07.md`](teste-cego/criterios-FP-07.md)), o que fecha o item 4 provando o próprio
motivo dele:

1. Os endereços "fixos do diagrama" `%IB1100` e `%IB1110` caíam **em cima dos telegramas dos
   SINAMICS `BEF-01/02/04`** (`%IB1100..1135`). O portão `G2` da rodada era inalcançável: o Portal
   recusaria, e a rodada mediria uma colisão plantada por engano. Passaram para `%IB1200`/`%IB1210`.
2. `%QB420` estava livre (`%Q` ocupado até 411) — mantido.
3. "A área é nova no CLP" era falso: existem `3. Partidas/3.19 Adensadores de Lodo` com dois SKID
   mecanizados populados, `3.21 Elevatória Lodo Adensado` e a pasta de alarme homônima. O caderno
   passou a reconhecer os adensadores existentes e a unidade nova virou `Adensador por Gravidade`.

O custo da conferência foi **um batch de 4 verbos de leitura, 16 s**. O custo de não a ter feito
seria uma rodada inteira medindo o caderno em vez da ferramenta.

## Histórico fechado

As sagas já resolvidas (biblioteca, hardware do molde, telegrama, F6, migração para skill,
bugs corrigidos) saíram daqui inteiras para [`DIARIO.md`](DIARIO.md) — ler só quando a
pergunta for "como chegamos nisso". Seções lá:

- Otimização de tokens do CLI — ✅ 2026-07-28
- Biblioteca de blocos ("arsenal") — ✅ ciclo fechado 2026-08-07 (`library/`)
- Bugs abertos (smoke 2026-07-27)
- Clonar acionamento — fluxo real validado (2026-07-27, AsBuilt)
- Migração do repo para skill (2026-08-06)
- F6 — Endurecer os scripts PS (✅ executada 2026-07-27)

## F15 — auditoria externa: P0 fechados ✅ (2026-08-18)

Auditoria de 170 achados feita por outra IA (`docs/AUDITORIA-COMPLETA-2026-08-18.md`), conferida
achado a achado no código. Os "Confirmado" que foram testados batem; a leitura foi estática (a
máquina da auditoria não tinha `lib/`, então nada de C# compilou lá) e a régua é de produto público
com equipe, não de ferramenta interna — daí metade dos P1/P2 ser checklist genérico de OSS maduro.

**Fechado neste commit** (os 9 de melhor custo/benefício): SAFE-04 (opção desconhecida = exit 2
antes do attach, com o teste offline `Cli.KnownOptions` cobrando a lista), API-01/02/03 (`error` de
topo = exit 1, e step de batch com erro embutido conta em `failed`), SAFE-03 (`--timeout` recusado
com `--apply`), SAFE-01 (`sim-run` só baixa em access point PLCSIM; `--allow-physical` é o opt-in),
INST-09 (DLL do PLCSIM fora do `bin/`, resolvida em runtime — com `Private=true` o `pack.ps1`
abortava e a release não saía), SAFE-11 (`audit` com `complete`/`skippedChecks`), SAFE-02/DOC-02
(SECURITY.md descrevendo as três fronteiras reais), INST-01/TEST-13 (V21 como única versão
suportada; o build referencia as assemblies split, que V19/V20 não têm), INST-02/INST-10 (gate de
Portal só V19+, `pack` falhando em árvore suja).

**Segunda leva, quando doer**: SAFE-07 (mutex no próprio `tia.exe` — hoje a D9 só é garantida na
rota da task, dois terminais na sessão 1 passam), SAFE-08 (`move-block` reimportar o XML se o
import falhar), SAFE-12/13/14 (ambiguidade de item de hardware e erro de telegrama engolido),
API-05 (`bytes` conta chars UTF-16 — renomear o campo).

**Não fazer**: registry declarativo de comandos, envelope JSON versionado, split
`Tia.Domain`/`Tia.Openness`, migração para xUnit, cobertura, analyzers, SBOM/assinatura/build
reproduzível, redaction/TTL de telemetria, matriz de locale, separação "core × profile ETE", MCP.
Agenda de produto com equipe e usuários externos; aqui compra risco de regressão sem comprar nada.
DOC-16 (navindex sem C#) morreu sozinho: o gerador já indexa `.cs` e, desde 2026-08-18, também os
títulos de Markdown.
