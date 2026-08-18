<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L29    Auditoria técnica completa e mapa de discussão — tia-cli -->
<!--   L43    0. Status da resposta (2026-08-18, revisão no repo) -->
<!--   L90    1. Resumo executivo -->
<!--   L125   2. Como ler os registros -->
<!--   L162   3. Escopo e evidência coletada -->
<!--   L200   4. Pontos fortes confirmados -->
<!--   L220   5. Guardrails e segurança operacional -->
<!--   L245   6. Contrato de sucesso, erros e JSON -->
<!--   L268   7. Instalação, versões e carregamento de assemblies -->
<!--   L293   8. Release, CI e cadeia de suprimentos -->
<!--   L312   9. Cobertura de testes e lacunas funcionais -->
<!--   L334   10. Arquitetura e manutenibilidade -->
<!--   L353   11. Domínio PLC, Openness, HMI, drives e simulação -->
<!--   L378   12. Desempenho, escala e contexto de IA -->
<!--   L393   13. Documentação e experiência do desenvolvedor -->
<!--   L414   14. Privacidade, segurança de software e aspectos legais -->
<!--   L430   15. Produto e posicionamento -->
<!--   L445   16. Perguntas abertas para discutir com outra IA -->
<!--   L474   17. Plano de ação proposto -->
<!--   L525   18. Backlog priorizado consolidado -->
<!--   L568   19. Validação executada nesta auditoria -->
<!--   L585   20. Estado local observado -->
<!--   L595   21. Definição sugerida de “pronto para uso confiável” -->
<!--   L611   22. Conclusão -->
<!-- ======================= END NAV INDEX ======================= -->

# Auditoria técnica completa e mapa de discussão — tia-cli

Data da auditoria: **2026-08-18**

Commit inspecionado: **`0af70b4` — `Refactor scripts and improve functionality`**

Repositório: `Codyte/Tia-Portal-CLI`
Objetivo deste arquivo: servir como contexto autossuficiente para discussão com outra IA, revisão
humana, planejamento de correções e definição de roadmap.

> Este documento é deliberadamente detalhado. Ele separa fato observado, inferência, dúvida e
> proposta. Um item marcado como hipótese não deve ser tratado como defeito confirmado sem o teste
> indicado no próprio item.

## 0. Status da resposta (2026-08-18, revisão no repo)

Cada achado citado abaixo foi conferido no código antes de virar tarefa. Os "Confirmado" testados
batem; a auditoria foi estática (a máquina onde ela rodou não tinha `lib/`, então nenhum C# compilou
lá) e usa régua de produto público com equipe, não de ferramenta interna de um mantenedor — daí boa
parte dos P1/P2 ser checklist genérico de OSS maduro.

**Fechado** — `SAFE-01`, `SAFE-02`, `SAFE-03`, `SAFE-04`, `SAFE-07`, `SAFE-08`, `SAFE-11`, `SAFE-12`,
`SAFE-13`, `SAFE-14`, `API-01`, `API-02`, `API-03` (parcial: erro de topo vira exit ≠ 0; não há
envelope comum), `API-05`, `API-06`, `API-08` (nos códigos de saída; sem helpers de faixa),
`API-09`, `API-10`, `PLC-08`, `INST-01`, `INST-02`, `INST-03`, `INST-05`, `INST-07`, `INST-09`,
`INST-10`, `SAFE-09` (backup antes do delete; rollback automático não — ver abaixo), `SAFE-15`,
`SAFE-16`, `SAFE-17`, `PLC-03`, `DOC-02`, `DOC-03`, `TEST-13`.

**Obsoleto** — `DOC-01` (índice raiz regenerado no commit `56c34c6`) e `DOC-16` (o `navindex.py`
instalado já indexa `.cs` e, desde 2026-08-18, títulos de Markdown).

**Aceito como está, com a decisão registrada:**

- `SAFE-05`: `open/create/save/close-project` continuam sem `--apply` — o efeito é o propósito do
  verbo. Agora estão listados como exceção explícita no `SECURITY.md`.
- `SAFE-06`: `--out-file` continua podendo escrever fora de `workspace/`. Quem chama o CLI já pode
  escrever no disco; o dry-run protege o projeto TIA, e essa fronteira está documentada.
- `INST-08`: build a partir do fonte continua exigindo o PLCSIM Advanced instalado (o `Sim.cs`
  compila contra a API dele). Tornar opcional pede referência condicional + `#if`; a release, que é
  o caso de quem só usa, não precisa mais dele.
- `PLC-06`: `sim-run` agora recusa instância vazia (0 tags) com `--no-download` e devolve
  `programCheck` (nome do controller × PLC do projeto). Fingerprint de versão do programa não existe:
  a API do PLCSIM não expõe assinatura, e gravar uma no programa mudaria o projeto do usuário.

**Não fazer** — registry declarativo de comandos, envelope JSON versionado com SemVer, split
`Tia.Domain`/`Tia.Openness`/`Tia.Cli`, migração para xUnit, cobertura, analyzers, SBOM, assinatura,
build reproduzível, pin de action por SHA, redaction/TTL de telemetria, matriz de locale, separação
"core genérico × profile ETE", MCP e plugin API: `ARCH-01..14`, `API-04`, `API-16..18`, `CI-03`,
`CI-06..14`, `SAFE-18`, `SAFE-20`, `TEST-09/10`, `PROD-07/08`. São a agenda de um produto com equipe
e usuários externos; aqui compram risco de regressão sem comprar nada.

`SAFE-09` fechou pela metade cara: tudo que morre sob `--force` é exportado antes para
`workspace/recovery/<verbo>-<timestamp>/` (`Ops.Backup`, chamado nos 7 sítios de delete de
`Library.cs`, `Scaffold.cs`, `Replicate.cs` e `Standardize.cs`), o caminho volta em `recoveryDir` e
a falha do export **impede** o delete — apagar sem rede exige `--no-backup` por escrito. Rollback
automático, não: desfazer um import parcial pede transação que o Openness não tem, e o XML
exportado é o que o `import-block` já sabe reler. O `MasterCopy` de `bake-lib` fica de fora — a API
não expõe `Export` de master copy, e a fonte continua viva no PLC.

**Segunda leva, quando doer** — `CI-01`/`CI-02` (extrair lógica pura para compilar no CI).

## 1. Resumo executivo

O repositório é tecnicamente interessante e resolve uma dor real: transformar operações do TIA
Portal Openness em uma CLI JSON, segura o bastante para ser dirigida por engenheiros e agentes de
IA. Há profundidade funcional, documentação de descobertas difíceis, dry-run para a maioria das
escritas, testes offline de transformações XML e provas registradas contra TIA Portal V21.

Ao mesmo tempo, a superfície cresceu mais rápido que alguns contratos transversais. Os maiores
riscos atuais não estão nos geradores individuais, mas nas fronteiras comuns:

1. **`sim-run` pode selecionar uma interface de download que não seja PLCSIM.** Como o resolvedor
   aceita automaticamente opções como `StopAll`, `AcceptAll` e `DownloadToDevice`, isso contraria a
   decisão de nunca baixar em PLC físico. A proteção precisa estar no código, não apenas no default
   `PLCSIM` e na documentação.
2. **Falha parcial nem sempre vira exit code diferente de zero.** Vários verbos capturam exceções em
   campos `error`/`warnings` e retornam normalmente. O dispatcher então encerra com código zero e o
   batch marca o step como `ok:true`.
3. **`--timeout` pode abandonar uma operação de escrita no meio.** A chamada roda em `Task.Run`; no
   timeout, o processo retorna 5 e termina sem cancelamento/rollback cooperativo.
4. **Opções desconhecidas são silenciosamente ignoradas.** Um erro como `--ara` no lugar de
   `--area`, junto de `--apply`, pode remover o escopo pretendido e aplicar o gerador em todas as
   áreas.
5. **A promessa V19/V20 não coincide com o build/loader atual.** O projeto referencia assemblies
   separadas de V21+, enquanto o próprio comentário do loader diz que V19/V20 usam a assembly
   monolítica.
6. **A integração recente com PLCSIM Advanced quebrou a história de distribuição.** O build por
   fonte agora exige a DLL do PLCSIM mesmo para quem não usa simulação; o pacote proíbe distribuir
   essa DLL; e a instalação prebuilt não a copia nem possui resolver para encontrá-la.
7. **O contrato de segurança/documentação ficou desatualizado.** `SECURITY.md` ainda afirma que não
   há download, operação online nem chamada de rede, mas agora existem `sim-run`, Project Server e
   o Help Viewer local.

Recomendação de sequência: fechar primeiro os guardrails P0, depois tornar instalação/release
reproduzíveis, em seguida uniformizar resultado/exit code e só então ampliar verbos.

## 2. Como ler os registros

### 2.1 Prioridade

| Prioridade | Significado |
|---|---|
| **P0** | Pode violar uma decisão de segurança, atingir PLC físico, corromper trabalho ou produzir falso sucesso grave. Corrigir antes de recomendar uso amplo. |
| **P1** | Risco relevante de instalação, compatibilidade, resultado incorreto, perda parcial ou release quebrada. |
| **P2** | Manutenibilidade, experiência, cobertura ou eficiência; deve entrar no roadmap próximo. |
| **P3** | Polimento, oportunidade de produto ou melhoria de longo prazo. |

### 2.2 Estado da evidência

| Estado | Significado |
|---|---|
| **Confirmado** | Observável diretamente no código, documentação, comandos locais ou contradição reproduzida. |
| **Inferência forte** | Fluxo do código aponta para o comportamento, mas o aceite final exige TIA/PLCSIM real. |
| **Pergunta** | Decisão de produto ou comportamento da API que precisa de resposta explícita. |
| **Oportunidade** | Não é bug; pode aumentar valor, segurança ou alcance. |

### 2.3 Protocolo para discutir este arquivo com outra IA

Trate cada ID como uma unidade de trabalho estável. Para cada item discutido, peça uma resposta no
seguinte formato; isso evita que perguntas, hipóteses e decisões desapareçam em um resumo genérico:

1. `ID` e veredito: confirmar, contestar, rebaixar/elevar prioridade ou pedir evidência.
2. Evidência: arquivo/linha, teste reproduzível ou documentação oficial necessária.
3. Decisão: comportamento desejado e não objetivo explícito.
4. Solução mínima: arquivos afetados, contrato antes/depois e compatibilidade.
5. Testes: caso feliz, falha, regressão e, quando aplicável, teste vivo TIA/PLCSIM.
6. Risco residual e rollback.
7. Dependências: IDs que bloqueiam ou são bloqueados por esta decisão.

Ordem sugerida para a conversa: P0 → perguntas abertas da seção 16 → P1 → arquitetura/testes → P2/P3.
Não aceite “resolvido” sem vincular a decisão a um ID e a uma evidência verificável. Itens podem ser
exportados para issue tracker preservando `ID`, prioridade, achado, pergunta e mitigação.

## 3. Escopo e evidência coletada

### 3.1 Superfície inspecionada

- `README.md`, `CLAUDE.md`, `SKILL.md`, `CHANGELOG.md`, `CONTRIBUTING.md`, `SECURITY.md`.
- `docs/VERBS.md`, `BENCHMARKS.md`, `LIMITES.md`, `PLANO.md`, `DIARIO.md`, boas práticas,
  resultados de testes cegos e exemplos.
- `src/Tia.Cli/Program.cs`, os 28 arquivos de `src/Tia.Core/`, o harness em `src/Tia.Tests/` e os
  três `.csproj`.
- 20 scripts/macroarquivos PowerShell/Python e o workflow público de CI.
- Instalação local por `scripts/init.ps1 -Check`, sem aplicar nenhuma mudança.
- Estado Git, histórico, arquivos rastreados, exclusões e mapas NAV INDEX.

### 3.2 Métricas observadas

| Métrica | Valor observado | Observação |
|---|---:|---|
| Verbos documentados | 92 pela convenção do projeto | A contagem depende de incluir ajuda/versão e linhas agrupadas; convém automatizar a definição. |
| Linhas C# | aproximadamente 13.312 | Inclui CLI, Core e testes. |
| Linhas PowerShell | aproximadamente 1.468 | 19 arquivos `.ps1`; há também `tia.cmd`. |
| Commits | 321 | Todos os commits do histórico local aparecem atribuídos ao mesmo autor. |
| Arquivos rastreados | 163 | Inclui fixtures XML/SCL e documentação. |
| Grupos de teste offline | 29 | Harness próprio, não framework de teste. |
| Chamadas `Check(...)` | aproximadamente 247 | Métrica estática; não equivale a cobertura. |
| Domínios do `--study` | 22 | `python scripts/tia-help.py --selftest` passou. |
| Scripts PowerShell parseados | 19/19 | Nenhum erro de parser. |
| JSON verificados | 16/16 | Sintaxe válida; não houve validação semântica/schema. |

### 3.3 Limites desta auditoria

- O C# **não compilou nesta máquina**, porque `lib/*.dll` está ausente. Isso é compatível com o
  modelo de licença do projeto, mas impede afirmar que o HEAD atual passa na suíte.
- Nenhum comando foi executado contra TIA Portal, projeto `.ap*`, biblioteca `.al*`, Project Server
  ou PLCSIM.
- Nenhuma consulta externa de CVEs, NuGet, GitHub, popularidade ou documentação Siemens foi feita.
- Achados sobre comportamento vivo de Openness/PLCSIM estão marcados como inferência quando não
  puderam ser provados estaticamente.

## 4. Pontos fortes confirmados

| ID | Ponto forte | Evidência e por que importa |
|---|---|---|
| S-01 | Dry-run amplamente adotado | A maioria dos handlers de escrita calcula plano/ação e só chama a API sob `apply`; o dispatcher usa `WriteLock` para muitas escritas (`src/Tia.Cli/Program.cs:607-1059`). |
| S-02 | Seleção segura de Portal quando há múltiplos processos | `TiaSession.PickProcess` recusa escolher ao acaso e exige `--portal` em caso ambíguo (`TiaSession.cs:30-59`). |
| S-03 | Saída separada para máquina e humano | JSON em stdout e log em stderr, inclusive na rota por scheduled task (`taskrun.ps1:36-59`). |
| S-04 | Redução deliberada de contexto | `tree`, `--out-file`, auto-spill e batch atacam um problema real para agentes; há medições em `BENCHMARKS.md`. |
| S-05 | Operações Openness serializadas na rota de sessão 0 | Lock atômico `CreateNew`, estado da task e run-id evitam vários races históricos (`_common.ps1:68-118`). |
| S-06 | Proteção da task elevada | `TiaWhitelist` executa uma cópia em `%ProgramData%` com ACL restrita; não aponta para script gravável do repo (`setup-tasks.ps1:18-58`). |
| S-07 | Privacidade considerada desde o desenho | `lib/`, `workspace/`, projetos, payload de biblioteca e DLLs Siemens são gitignored; CI bloqueia vários caminhos/extensões. |
| S-08 | Transformações XML têm prova pós-import | `ImportAndProve` recompila/reexporta e verifica o patch, reduzindo falso `ok:true` após export defasado (`Ops.cs:1170-1242`). |
| S-09 | Descobertas difíceis estão documentadas perto do código | Sessão do Windows, whitelist, telegramas Startdrive, inconsistência pós-import, barras em pastas e limitações do PLCSIM estão registradas. |
| S-10 | Testes cegos têm régua anterior à execução | Critérios e resultados são separados em `docs/teste-cego/`, reduzindo a chance de ajustar o aceite depois do resultado. |
| S-11 | Empacotamento tenta provar procedência | `pack.ps1` confere versão, commit carimbado, bloqueia `Siemens.*` e calcula SHA-256. |
| S-12 | Operações caras são mensuradas | Batch e steps trazem `ms`; `--summary` lista os três mais lentos. |
| S-13 | Limitações da API são tratadas como produto | `LIMITES.md` separa limite Siemens, decisão do repo e ausência de DLL, evitando sondagem infinita. |
| S-14 | O projeto evita dependências desnecessárias | C# usa basicamente Newtonsoft.Json e assemblies do fornecedor; PowerShell é nativo do Windows. |
| S-15 | Erros comuns ganharam mensagens acionáveis | Muitos caminhos traduzem exceções cruas em comando seguinte, candidatos, preflight ou restrição concreta. |

## 5. Guardrails e segurança operacional

| ID | Pri. / estado | Achado e evidência | Dúvida ou pergunta a decidir | Resolução proposta e critério de aceite |
|---|---|---|---|---|
| SAFE-01 | **P0 · Inferência forte** | `sim-run` aceita `--pc-interface` arbitrária e `FindTarget` seleciona a primeira interface cujo nome contém o texto (`Sim.cs:463-475`). O resolvedor aceita opções como `StopAll`, `AcceptAll`, `DownloadToDevice`, `DeleteAndReplace` (`Sim.cs:506-526`). Isso pode permitir download em interface física, contrariando D8. | É possível provar pela API que o target pertence à instância PLCSIM Advanced escolhida? O nome `PLCSIM` é suficiente em todas as versões/idiomas? | Remover a liberdade de apontar para qualquer interface ou criar validação fail-closed que aceite apenas o access point comprovadamente PLCSIM. Teste negativo obrigatório: interface PN/IE física deve ser recusada antes de `Download`. |
| SAFE-02 | **P0 · Confirmado** | `SECURITY.md:5-12` diz que nunca há operação online, download ou rede. Hoje há `sim-run`, `OnlineProvider.GoOffline`, `DownloadProvider.Download`, Project Server remoto e HTTP local do Help Viewer. | O escopo de segurança passa a ser “nenhuma escrita em PLC físico”, com exceção explícita para PLC virtual? | Atualizar threat model, CONTRIBUTING, README e issue templates. O texto deve distinguir projeto offline, PLCSIM virtual, Project Server e CPU física. |
| SAFE-03 | **P0 · Confirmado** | `--timeout` executa `Run(args)` em `Task.Run`, espera e retorna 5; a chamada pode estar no meio de import/delete/compile (`src/Tia.Cli/Program.cs:371-381`). Não há cancelamento nem rollback. | Timeout deve existir para verbos de escrita ou apenas leituras? Como o Openness reage quando o processo cliente morre durante import? | Proibir `--timeout` junto de `--apply` até existir cancelamento seguro; alternativamente executar em processo supervisor e marcar projeto como “estado desconhecido”, exigindo compile/audit/reopen. Teste deve matar cada classe de escrita e provar recuperação. |
| SAFE-04 | **P0 · Confirmado** | O parser não valida opções desconhecidas. `OptionValue` apenas procura nomes conhecidos e o resto é ignorado (`src/Tia.Cli/Program.cs:1067-1091`). Um typo de escopo pode cair no default amplo com `--apply`. | Qual é o conjunto de opções globais e por verbo? Repetição de opção não repetível deve falhar? | Adicionar validação declarativa de flags antes do attach. Testes: `--ara`, `--aply`, valor ausente, flag duplicada e flag de outro verbo devem retornar exit 2 sem attach. |
| SAFE-05 | **P0 · Confirmado** | `open-project`, `create-project`, `save-project` e `close-project` alteram estado sem `--apply` (`src/Tia.Cli/Program.cs:449-459`, `612-616`). `create-project` cria diretório/projeto. Isso contradiz “Every write is a dry-run”. | Lifecycle é exceção consciente ou deve obedecer ao mesmo contrato? | Declarar taxonomia de efeitos ou exigir `--apply` para criar/salvar/fechar. No mínimo, help e SECURITY devem listar exceções em destaque. |
| SAFE-06 | **P1 · Confirmado** | `--out-file` e vários exports sobrescrevem arquivos arbitrários sem `--apply`; `WriteOut` chama `File.WriteAllText` em qualquer caminho (`src/Tia.Cli/Program.cs:1157-1161`). Dry-run protege o projeto TIA, não o filesystem. | Um agente deve poder sobrescrever fora de `workspace/` sem confirmação? | Default `--no-clobber`; exigir `--force-file` para sobrescrever ou caminho fora de workspace. Documentar claramente a fronteira. |
| SAFE-07 | **P1 · Confirmado** | O lock cross-process existe apenas na rota de task/sessão 0. Na sessão interativa, `Invoke-Tia` chama o exe diretamente sem lock (`_common.ps1:60-64`). Dois terminais podem violar D9. | O CLI deve garantir D9 sozinho ou a disciplina humana basta? | Mutex nomeado ou lock no próprio `tia.exe`, compartilhado por todas as rotas. Teste com dois processos: o segundo deve falhar rápido e de forma determinística. |
| SAFE-08 | **P1 · Confirmado** | `move-block` exporta todos, apaga e importa. Falha de import é registrada, mas o bloco original já foi removido (`Ops.cs:667-729`). | O XML exportado é considerado backup suficiente? Quem restaura e para qual pasta? | Implementar rollback para a pasta original ou retornar recovery manifest explícito. Aceite: falha injetada no import deixa o bloco original restaurado. |
| SAFE-09 | **Fechado (parcial)** | `import-master-copy --force`, `scaffold --force`, replicadores e padronização podem apagar antes de recriar (`Library.cs:162-244`, `Scaffold.cs:190-211`, `Replicate.cs:303-347`, `Standardize.cs:436-490`). | Quais operações aceitam perda parcial? Existe backup do projeto como pré-condição verificável? | Estratégia comum de stage/backup/rollback; gerar recovery bundle antes do delete. Se rollback for impossível, resultado deve ser `partial` e exit não zero. |
| SAFE-10 | **P1 · Confirmado** | Batch continua após qualquer exceção. Isso é útil para diagnóstico, mas perigoso quando um step aplicado falha e os seguintes dependem dele (`src/Tia.Cli/Program.cs:486-548`). | Default deve continuar ou parar em fluxo de escrita? | Acrescentar `--fail-fast` e `allowFailure` por step; considerar fail-fast como default quando qualquer step contém `--apply`. |
| SAFE-11 | **P1 · Confirmado** | `audit` marca check não executado como `ok:true`; o `ok` global pode ser true com checks críticos pulados (`Audit.cs:233-240`, `329-336`). | Conformidade incompleta é sucesso, desconhecido ou falha? | Resultado tri-state: `pass/fail/skipped`; campos `complete`, `skippedChecks`; `--strict` retorna não zero se qualquer check pular. |
| SAFE-12 | **P1 · Confirmado** | Falha ao ler telegrama é engolida em `CollectTelegramMap` (`Hardware.cs:468-490`), mas o mapa ainda pode declarar `nextFreeByteExact:true`. | Como distinguir “não há telegrama” de “não consegui ler telegrama”? | Coletar `scanErrors`, incrementar `unreadable`, forçar `nextFreeByteExact:false`. Teste com drive sem commissioning data. |
| SAFE-13 | **P1 · Confirmado** | `FindItem` retorna o primeiro item por nome em busca recursiva (`Hardware.cs:242-263`). Nomes repetidos são comuns em hardware; escrita pode atingir o item errado. | Todo verbo de escrita deve exigir caminho completo quando houver duplicidade? | Enumerar todos os matches; aceitar nome curto apenas se único. Retornar candidatos e exigir path em caso ambíguo. |
| SAFE-14 | **P1 · Inferência forte** | `SetAddress` usa a primeira interface e o primeiro node (`Hardware.cs:268-294`). CPUs/dispositivos podem ter mais de uma interface. | Qual interface é a correta em CPU com X1/X2 ou dispositivos múltiplos? | Opção `--interface`/`--item`, detecção de ambiguidade e dry-run exibindo o path exato. |
| SAFE-15 | **P1 · Confirmado** | `SetAttr.TryGet` engole erro e retorna null; `Coerce` então assume string (`Hardware.cs:557-585`). Dry-run pode prometer valor incompatível e ocultar “atributo ilegível”. | `AttributeInfo` expõe o tipo esperado de forma confiável? | Separar `value:null` de `readError`; se tipo não puder ser provado, recusar apply ou exigir tipo explícito. |
| SAFE-16 | **P1 · Inferência forte** | `set-memory-bytes` seleciona atributos por substring e define todo bool correspondente como true (`Hardware.cs:597-647`). Uma versão futura pode expor atributo adicional com nome semelhante. | Quais nomes exatos existem em V19/V20/V21? | Capability table por versão + allowlist exata; dry-run deve listar selecionados e ignorados. |
| SAFE-17 | **P1 · Confirmado** | `list-server-projects` pode criar e manter conexão no Portal com `--keep-connection` sem `--apply`; `--http` permite transporte sem TLS (`Multiuser.cs:21-65`). | Manter conexão é efeito de escrita? HTTP deve existir fora de laboratório? | Exigir `--apply` para persistir conexão; warning/opt-in forte para HTTP; registrar origem de credenciais e política TLS. |
| SAFE-18 | **P2 · Confirmado** | Arquivos `out-*.txt`, `err-*.txt`, `exit-*.txt` ficam até a limpeza após um dia; `telemetry.log` não tem retenção e contém nomes de bloco/erros (`_common.ps1:86-91`, `Ops.cs:1219-1234`). | Qual é a política de retenção de dados de cliente no disco local? | Comando `tia cleanup`, TTL configurável, redaction opcional e documentação. Telemetria deve ser opt-in ou explicitamente local com rotação. |
| SAFE-19 | **P2 · Confirmado** | `--version` diz qual diretório “vai carregar”, não qual assembly foi efetivamente carregada (`src/Tia.Cli/Program.cs:137-150`). | Bug report precisa do caminho previsto ou da identidade carregada? | Expor versão, nome forte, hash e caminho das assemblies após load controlado; informar `predicted` versus `loaded`. |
| SAFE-20 | **P2 · Pergunta** | O código retorna caminhos absolutos de projeto, sessão Multiuser e arquivos em JSON. Isso ajuda diagnóstico, mas pode vazar hostname/usuário em tickets ou prompts. | Deve existir `--redact-paths` para compartilhamento externo? | Modo de redaction que preserve basename/IDs técnicos e remova perfil/host. |

## 6. Contrato de sucesso, erros e JSON

| ID | Pri. / estado | Achado e evidência | Dúvida ou pergunta | Resolução proposta e aceite |
|---|---|---|---|---|
| API-01 | **P0 · Confirmado** | `Sim.Run` captura falhas e devolve `plan["error"]`; o dispatcher retorna 0 porque não houve exceção (`Sim.cs:105-185`, `src/Tia.Cli/Program.cs:985-993`). | Campo `error` com exit 0 é diagnóstico válido ou falso sucesso? | Erro terminal deve lançar/retornar envelope comum com exit não zero. Testar instância ausente, interface ausente, download com erro e tag inválida. |
| API-02 | **P0 · Confirmado** | Steps do PLCSIM capturam falha individual e continuam; não existe `failed` agregado nem alteração de exit (`Sim.cs:341-395`). | Um script com uma escrita falha e as demais passam deve ser sucesso? | Resultado `{steps,failed,results}` alinhado ao batch; exit 1 se `failed>0`, salvo `allowFailure`. |
| API-03 | **P1 · Confirmado** | `create-folder`, `move-block`, `FaultOb`, `Standardize`, `Replicate`, `InstrumentFc` e Multiuser podem incorporar erros/warnings e retornar normalmente. Batch considera o step `ok:true`. | Quais falhas parciais são aceitáveis por verbo? | Envelope comum com `status` igual a `success`, `partial` ou `failed`, mais `errors[]` e `warnings[]`; dispatcher calcula exit code pelo status. |
| API-04 | **P1 · Confirmado** | Há múltiplos dialetos: `apply` versus `applied`, `action`, `status`, `ok`, `error`, `failed`, `warnings`. | O JSON é contrato estável para consumidores ou apenas saída humana estruturada? | Definir schema base versionado: `ok`, `status`, `applied`, `changed`, `warnings`, `errors`, `data`, `meta`. Preservar campos antigos até major version. |
| API-05 | **P1 · Confirmado** | `WriteOut` chama `json.Length` de `bytes`; isso conta chars UTF-16, não bytes UTF-8 (`src/Tia.Cli/Program.cs:1153-1173`). | Métricas históricas chamam KB de bytes reais ou chars? | Usar `new FileInfo(path).Length`; renomear limite para `chars` ou medir UTF-8. Teste com acentos/emoji. |
| API-06 | **P1 · Confirmado** | `ExitCodeFor` classifica `ArgumentException`, `FileNotFoundException` e tipos Siemens; `FormatException`, `DirectoryNotFoundException`, JSON inválido e wrappers podem cair em geral 1 (`src/Tia.Cli/Program.cs:400-408`). | Quais erros são uso (2), arquivo (3) ou ambiente TIA (4)? | Tabela central de exceções/códigos + root-cause traversal completo. Testes parametrizados para cada opção numérica e JSON inválido. |
| API-07 | **P1 · Confirmado** | `--retry` detecta busy apenas procurando a palavra inglesa `busy` na mensagem (`src/Tia.Cli/Program.cs:578-600`). | Mensagem é localizada? Existem exception types/HResults estáveis? | Preferir tipo/código Siemens; fallback multilíngue documentado. Testar Portal ocupado nas versões suportadas. |
| API-08 | **P1 · Confirmado** | Valores numéricos de `--retry`, `--timeout`, `--max`, portas, posições etc. usam `int.Parse` disperso, sem limites uniformes. | Quais intervalos são seguros? | Helpers `RequireInt/OptionalInt(min,max)`; inválido retorna exit 2 antes do attach. |
| API-09 | **P1 · Confirmado** | JSON de config é desserializado com default Newtonsoft, que ignora propriedades desconhecidas (`src/Tia.Cli/Program.cs:789`, `1009-1056`). Typos podem ativar defaults silenciosamente. | Compatibilidade forward exige ignorar desconhecidos ou segurança exige falhar? | `MissingMemberHandling.Error` para arquivos de execução; JSON Schema versionado; comando `validate-config`. |
| API-10 | **P1 · Confirmado** | `sim-run` não valida todos os steps antes de baixar/rodar. Array curto ou operação desconhecida falha durante execução e os demais continuam. | Erros de script devem ser descobertos antes do download? | Pré-validação completa de shape/op/arity/tipo/limites antes de attach/download. |
| API-11 | **P1 · Inferência forte** | `DownloadProvider.Download` devolve contagem de erros, mas o código não transforma `ErrorCount>0` em falha e continua para tag list/RUN (`Sim.cs:148-174`). | `result.State` pode ser Success com erros? | Abortar steps se state não for sucesso ou errors > 0; preservar mensagens completas em arquivo. |
| API-12 | **P2 · Confirmado** | `run --summary` perde `type`, detalhes estruturados e usa índice zero-based sem documentar claramente (`src/Tia.Cli/Program.cs:524-547`). | Consumidores esperam step 0 ou 1? | Documentar e incluir `stepIndex` zero-based + `stepNumber` humano; preservar `type/code/details`. |
| API-13 | **P2 · Confirmado** | Flags globais do processo não descem aos steps do batch; cada step precisa repetir `--plc`, `--out`, possivelmente `--portal` (`VERBS.md:110`). | É intencional por isolamento ou dívida de UX? | Permitir defaults do batch e override por step, mantendo semântica explícita no JSON final. |
| API-14 | **P2 · Confirmado** | Auto-spill usa caminho fixo `workspace/auto-<verb>.json`; chamadas sucessivas sobrescrevem o resultado anterior (`src/Tia.Cli/Program.cs:1138-1145`). | Histórico é desejado? | Acrescentar operation-id/timestamp ou opção `--latest`; nunca colidir em processos distintos. |
| API-15 | **P2 · Confirmado** | `CountOf` reconhece apenas `count:int`, `hits:ICollection` ou coleção raiz; vários resultados grandes não recebem count no stub (`src/Tia.Cli/Program.cs:1175-1190`). | Quais coleções devem ser resumidas? | Metadata explícita por envelope ou contador recursivo configurado por comando. |
| API-16 | **P2 · Oportunidade** | Não há `operationId`, versão da CLI, duração total ou projeto/PLC selecionado em todo resultado. | Quanto de metadata é aceitável sem inflar contexto? | `meta` compacto opcional/default em erros e writes; facilita correlação com taskio/telemetria. |
| API-17 | **P2 · Confirmado** | Mensagens e campos misturam português e inglês. Automação não deve depender de texto localizado. | Produto será bilíngue ou inglês no contrato? | Códigos estáveis (`TIA_CLI_*`) + mensagem humana localizada; nomes de campo sempre inglês. |
| API-18 | **P2 · Pergunta** | Help/VERBS é texto gerado de arrays manuais, não de uma especificação tipada. | O custo de adicionar biblioteca de CLI é aceitável em net48? | Solução mínima: registro declarativo próprio com verbo, opções, efeitos, exemplos e handler; gerar help, validação e docs da mesma fonte. |

## 7. Instalação, versões e carregamento de assemblies

| ID | Pri. / estado | Achado e evidência | Dúvida ou pergunta | Resolução proposta e aceite |
|---|---|---|---|---|
| INST-01 | **P0 · Confirmado** | `.csproj` referencia `Siemens.Engineering.Base/Step7/WinCCUnified` separadas; `init.ps1:42-43` exige essas DLLs. O loader diz que V19/V20 eram monolíticos (`src/Tia.Cli/Program.cs:1191-1218`). | O projeto realmente pode buildar/rodar com V19/V20? | Escolher: declarar V21+ agora, ou criar builds por major (`v19`, `v20`, `v21`) com referências condicionais/adaptadores. Matriz end-to-end obrigatória antes de recolocar badges. |
| INST-02 | **P1 · Confirmado** | `init.ps1` considera qualquer `Portal V*` instalado, inclusive V17/V18, como gate válido (`init.ps1:45-46`, `135`). Nesta máquina reportou V17/V18/V19 como “ok”. | Quais versões mínimas são realmente suportadas? | Parse numérico e filtre versões suportadas; mostre unsupported separadamente. |
| INST-03 | **P1 · Confirmado** | Cada DLL é encontrada separadamente no primeiro Portal que a possuir (`init.ps1:193-201`). É possível misturar versões no mesmo `lib/`. | Mistura de assemblies é suportada pela Siemens? Provavelmente não. | Selecionar uma única raiz de PublicAPI que contenha o conjunto coerente; falhar se incompleto. |
| INST-04 | **P1 · Confirmado** | DLL existente em `lib/` nunca é atualizada (`init.ps1:194-195`). Upgrade/downgrade do Portal pode deixar referências antigas. | Quando o usuário troca Update/major, como o repo detecta? | Manifest `lib/.source.json` com versão, path e hashes; refresh atômico do conjunto quando divergir. |
| INST-05 | **P1 · Confirmado** | O resolver busca cada assembly independentemente em env, exe e V21/V20/V19 (`src/Tia.Cli/Program.cs:1200-1219`), podendo misturar roots em runtime. | É aceitável fallback por assembly? | Resolver primeiro uma instalação coerente, fixar root e validar versões fortes de todas as assemblies. |
| INST-06 | **P1 · Confirmado** | `Test-Whitelisted` passa se o hash casar em qualquer versão instalada, não necessariamente a que o loader usará (`init.ps1:53-62`). | Qual registry hive/version será consultado pelo runtime selecionado? | Vincular `--version`, loader e whitelist à mesma versão; checar todas as entries requeridas ou apenas a efetiva, de modo explícito. |
| INST-07 | **P1 · Confirmado** | Após UAC, `init.ps1` verifica apenas se `TiaWhitelist` existe, mas imprime que as três tasks estão registradas (`init.ps1:231-240`). | Setup parcial pode passar? | Reexecutar `Test-TasksCurrent` completo e falhar se qualquer task/ACL/action divergir. |
| INST-08 | **P1 · Confirmado** | Build por fonte exige `Siemens.Simatic.Simulation.Runtime.Api.x64.dll`; ausência de PLCSIM Advanced entra em `$missing` e aborta init (`init.ps1:203-217`). README não o declara como requisito geral. | Simulação é feature opcional ou requisito de toda a CLI? | Separar `Tia.Sim` opcional/reflection/plugin, ou compilar stub quando PLCSIM não existe. Core PLC deve instalar só com TIA Portal. |
| INST-09 | **P0 · Confirmado** | A DLL PLCSIM é `Private=true`, mas `pack.ps1` proíbe qualquer `Siemens.*` no zip. Instalação prebuilt pula cópia de lib e o resolver só trata `Siemens.Engineering.*`. Logo `sim-run` da próxima release fica sem DLL ou o pack aborta. | A licença permite copiar a DLL localmente durante init? | Nunca distribuir; no prebuilt, localizar/copy local ou resolver de `Common Files`. Teste de instalação limpa da release com e sem PLCSIM. |
| INST-10 | **P1 · Confirmado** | `pack.ps1` avisa que mudanças não commitadas não entram, mas seleciona nomes com `git ls-files` e copia os arquivos do working tree (`pack.ps1:40-64`). Mudanças rastreadas e não commitadas entram. | Release deve abortar em árvore suja ou empacotar HEAD? | Preferir fail em dirty tree e `git archive HEAD`; teste altera README sem commit e prova que zip não muda. |
| INST-11 | **P1 · Confirmado** | Pack copia todo arquivo do diretório bin exceto PDB (`pack.ps1:49-53`). Um artefato inesperado pode entrar. | Qual é a allowlist mínima de runtime? | Copiar lista explícita e validar hash/nomes; falhar em extras desconhecidos. |
| INST-12 | **P2 · Confirmado** | Binário oficial é Debug, sem PDB no pacote (`rebuild.ps1`, `pack.ps1`). | Debug foi escolhido por compatibilidade/diagnóstico ou apenas hábito? | Medir Release; publicar Release otimizado e PDB separado para diagnóstico, preservando whitelist. |
| INST-13 | **P2 · Confirmado** | Não há `global.json`, lock de NuGet ou pin do SDK. A máquina atual usou SDK 9 para target net48. | Qual SDK é oficialmente suportado: 8 exato ou 8+? | `global.json` com roll-forward consciente e `packages.lock.json` em locked mode. |
| INST-14 | **P2 · Confirmado** | O checkout é exigido em `~/.claude/skills/tia`, mas esta execução Codex carregou a skill em `.agents/skills/Tia-Portal-CLI`; `init -Check` marcou “não”. | O produto quer suportar Claude Code, Codex ou ambos? | Separar instalação da CLI da integração de agente; documentar paths por host e evitar um gate específico do Claude em diagnóstico genérico. |
| INST-15 | **P2 · Pergunta** | “Um checkout só” reduz conflito de whitelist, mas dificulta desenvolver branch e usar versão estável. | É possível ter um binário instalado estável e vários worktrees de fonte sem whitelist concorrente? | Instalação versionada/canônica em `%LocalAppData%\tia-cli`, com dev build usando outro assembly name/exe e whitelist explícita. |
| INST-16 | **P2 · Confirmado** | `tia-help.py` depende de `httpx[http2]`, mas não existe requirements/pyproject nem gate no init. | Fresh install tem Python/httpx garantidos? | Declarar `requirements.txt` com hash ou migrar modo local para runtime empacotado; `init -Check` deve validar Python e HTTP/2 se a skill depender dele. |
| INST-17 | **P2 · Confirmado** | Help usa `DEFAULT_API=PortalV21` e porta fixa 5112 (`tia-help.py:74-75`), apesar da promessa V19+. | V19/V20 usam o mesmo serviço, porta e api? | Autodetectar versão/API/porta; incluir no diagnóstico e em cache. |
| INST-18 | **P2 · Confirmado** | Índice SDK percorre múltiplas versões e mistura membros sem registrar a versão do Portal (`tia-help.py:150-188`). | Uma busca pode recomendar API existente só em V21 para projeto V19? | Indexar por major e filtrar pelo target; resultado deve mostrar assembly + versão + path. |
| INST-19 | **P2 · Confirmado** | Cache de corpo usa apenas `ItemId`, sem base/API/version (`tia-help.py:260-273`). | ItemId igual pode mudar entre updates? | Namespace do cache por Portal/API/hash do índice; comando de invalidation/status. |
| INST-20 | **P3 · Oportunidade** | `init -Check` é excelente, mas não emite JSON. | Agentes precisam parsear cores/texto? | Adicionar `-Json`/`tia install-doctor` com gates, versão e ações recomendadas. |

## 8. Release, CI e cadeia de suprimentos

| ID | Pri. / estado | Achado e evidência | Pergunta | Resolução proposta e aceite |
|---|---|---|---|---|
| CI-01 | **P1 · Confirmado** | CI não compila C# nem executa `Tia.Tests` (`.github/workflows/ci.yml:3-8`). Regressão de sintaxe/API pode chegar ao main. | É realmente impossível compilar a lógica pura sem DLL Siemens? | Extrair `Tia.Transforms`/abstrações sem Siemens para compilar/testar no CI; gerar stubs apenas para compile se juridicamente aceitável. |
| CI-02 | **P1 · Confirmado** | `Tia.Tests` referencia `Tia.Core`, que por sua vez referencia Siemens; portanto testes “offline” ainda exigem DLLs para compilar. | Quais módulos são de fato puros? | Projeto separado para XML/naming/planners sem Openness; o runner público passa a rodá-los. |
| CI-03 | **P1 · Confirmado** | Testes são console assert, não `dotnet test`; não há discovery, categorias, fixtures isoladas, cobertura ou relatório padrão. | Manter zero dependência vale perder tooling? | MSTest/xUnit/NUnit apenas no projeto de testes ou um adapter TRX mínimo; cobertura da lógica pura no CI. |
| CI-04 | **P1 · Confirmado** | Não há teste automático que compare `docs/VERBS.md`, help e handlers. | A contagem “92” e assinaturas podem divergir? | Registro único de comandos + snapshot do help; CI falha em doc stale sem precisar de Siemens. |
| CI-05 | **P1 · Confirmado** | O check de privacidade é baseado em paths/extensões. Conteúdo de cliente colocado em `docs/` ou renomeado passa (`ci.yml:46-61`). | Há um catálogo/proveniência dos fixtures rastreados? | Scanner de padrões/projeto, allowlist de fixtures, revisão de entropia/names e declaração de origem/licença. Não vender o check atual como garantia absoluta. |
| CI-06 | **P2 · Confirmado** | Não há análise estática C#, analyzers, warnings-as-errors ou nullable. | net48/C#7.3 aceita analyzers modernos compatíveis? | Habilitar warnings importantes, StyleCop/Roslyn compatível e baseline incremental. |
| CI-07 | **P2 · Confirmado** | Não há verificação automática de dependências/CVEs, SBOM ou licença transitiva. | Pode usar serviços externos no projeto público? | Dependabot/Renovate, `dotnet list package --vulnerable` em job permitido, SBOM CycloneDX da release. |
| CI-08 | **P2 · Confirmado** | `actions/checkout@v4` não está preso a SHA. | Política de supply chain exige pin imutável? | Pin por SHA e atualização automatizada. |
| CI-09 | **P2 · Confirmado** | SHA-256 é publicado em release notes, mas binário/tag não é assinado. | Usuários precisam verificar autoria ou apenas integridade? | Assinar tag e, se viável, Authenticode; publicar checksum como asset separado. |
| CI-10 | **P2 · Confirmado** | Zip usa arquivos/timestamps locais e `Compress-Archive`; não é build reproduzível. | Reprodutibilidade é meta? | Empacotar HEAD limpo, timestamps normalizados e manifesto de conteúdo/hashes. |
| CI-11 | **P2 · Confirmado** | Não há job de markdown links, JSON Schema, XML parse completo, shell lint ou Python compile. | Quais checks têm melhor custo/benefício? | Adicionar `python -m py_compile`, PSScriptAnalyzer selecionado, link checker local, parse XML/SCL fixtures e validação schema. |
| CI-12 | **P2 · Pergunta** | Self-hosted runner com TIA poderia compilar/integrar, mas envolve licença, GUI, sessão interativa e dados. | Há máquina de laboratório dedicada e autorização Siemens? | Se sim, runner isolado e manual/nightly contra projeto sintético; nunca expor em PR não confiável. |
| CI-13 | **P2 · Confirmado** | Release depende de execução manual local e `gh release create`; não há checklist mecanizado de compatibilidade/smoke. | Quem aprova e registra o smoke? | Manifesto de release com versão TIA, PLCSIM, checks executados e hashes; anexar à release. |
| CI-14 | **P3 · Oportunidade** | Benchmarks são úteis, mas manuais. | Regressão de performance é relevante para attach/batch? | Dataset sintético e baseline de funções puras; métricas reais em job de laboratório. |

## 9. Cobertura de testes e lacunas funcionais

| ID | Pri. / estado | Achado | Pergunta | Ação/teste recomendado |
|---|---|---|---|---|
| TEST-01 | **P1 · Confirmado** | O HEAD atual não foi compilado nesta auditoria: faltam DLLs Siemens/PLCSIM. | Existe outro ambiente com `rebuild.ps1` verde neste commit exato? | Registrar log resumido e hash do commit em `docs/verification/` ou release manifest. |
| TEST-02 | **P1 · Confirmado** | Não há teste do guard de interface física do `sim-run` porque o guard não existe. | — | Mock/fake de `DownloadProvider` + teste vivo que lista PN/IE mas recusa antes do download. |
| TEST-03 | **P1 · Confirmado** | Não há testes de exit code para resultados parciais incorporados. | — | Casos para Sim, Standardize, Replicate, FaultOb, move-block e create-folders. |
| TEST-04 | **P1 · Confirmado** | Não há testes sistemáticos para `--apply` ausente em todos os verbos de escrita. | Algum novo handler pode esquecer o guard? | Teste de contrato enumerando command registry: todo comando classificado write deve provar dry-run sem chamada mutante. |
| TEST-05 | **P1 · Confirmado** | Lifecycle e persistência Multiuser não participam do contrato dry-run. | — | Testes explícitos da política escolhida, não deixar como exceção implícita. |
| TEST-06 | **P1 · Confirmado** | `TiaSession`, Drives, Hardware, Library, Motion, Multiuser, Sim e grande parte da orquestração não têm testes unitários diretos. | Que parte pode ser abstraída da API Siemens? | Interfaces finas/adapters e fakes; manter smoke vivo para semântica real. |
| TEST-07 | **P1 · Confirmado** | Testes de geradores cobrem XML puro, mas não todas as sequências delete/import/compile/rollback. | — | Fault injection por etapa e verificação de recuperação/idempotência. |
| TEST-08 | **P2 · Confirmado** | `Tia.Tests` usa diretório temporário fixo `tia-tests`; paralelização futura pode colidir. | A suíte continuará estritamente serial? | Temp por GUID/teste e cleanup em finally. |
| TEST-09 | **P2 · Confirmado** | Não há medida de cobertura. 247 asserts podem deixar branches críticos sem teste. | Qual meta faz sentido para núcleo puro? | Cobertura por branch do projeto puro; não impor número artificial ao adapter Siemens. |
| TEST-10 | **P2 · Confirmado** | Não há matriz por cultura/locale, embora o domínio use acentos, decimal e nomes pt-BR. | Quais locales Windows são suportados? | Rodar lógica pura em pt-BR/en-US/de-DE; garantir JSON/números invariantes. |
| TEST-11 | **P2 · Confirmado** | Não há teste de caminhos longos, UNC, espaços, aspas, `&`, parênteses e Unicode em todas as rotas. Há testes parciais do quoting. | — | Matriz de argumentos direto/task/smokeloop; incluir barra final e path >260 quando suportado. |
| TEST-12 | **P2 · Confirmado** | `--timeout` não tem testes com operações reais bloqueadas/penduradas. | — | Teste controlado por processo filho; nunca experimentar primeiro em projeto relevante. |
| TEST-13 | **P2 · Confirmado** | Não há prova V19/V20 end-to-end, explicitamente admitida no README. | Manter badge V19/V20? | Retirar promessa até passar suíte mínima em cada versão. |
| TEST-14 | **P2 · Confirmado** | S7-1200 G2 no PLCSIM está parametrizado, mas não testado; famílias não suportadas estão documentadas. | — | Capability matrix com `tested`, `expected`, `unsupported`. |
| TEST-15 | **P2 · Confirmado** | HMI clássico possui operações de tela novas; Unified continua limitado. Cobertura viva por painel/versão não está resumida numa matriz. | — | Fixtures sanitizados por família e smoke de export/import/copy/audit. |
| TEST-16 | **P2 · Confirmado** | Testes de privacidade não verificam o conteúdo dos fixtures e documentos. | Alguns comentários dizem “valores reais do molde”. Há autorização/proveniência? | Revisão de provenance e arquivo `FIXTURES.md` declarando fictício/sanitizado/original. |
| TEST-17 | **P3 · Oportunidade** | Testes cegos geram enorme aprendizado, mas resultado não vira suíte regressiva automaticamente. | Quais tropeços podem virar unit/integration test determinístico? | Para cada T1/T2/T3 fechado, ligar commit → teste → requisito numa matriz. |

## 10. Arquitetura e manutenibilidade

| ID | Pri. / estado | Achado | Pergunta | Resolução proposta |
|---|---|---|---|---|
| ARCH-01 | **P1 · Confirmado** | `Program.cs` tem ~1.200 linhas, 84 cases e parsing/execução/help/output no mesmo arquivo. | Quando o custo do switch supera a simplicidade? | Extrair registry/handlers por domínio sem framework pesado; manter exe único. |
| ARCH-02 | **P1 · Confirmado** | `Ops.cs` tem ~1.337 linhas e mistura lookup, estrutura, tags, import/export, fonte, prova e compile. | Qual fronteira já existe naturalmente? | `BlockOps`, `TagOps`, `SourceOps`, `CompileOps`, `XmlRoundtrip`; fachada `Ops` temporária para compatibilidade. |
| ARCH-03 | **P1 · Confirmado** | Lógica pura e tipos Siemens vivem no mesmo assembly, bloqueando CI. | — | Arquitetura em três camadas: `Tia.Domain/Transforms` puro, `Tia.Openness` adapter, `Tia.Cli`. |
| ARCH-04 | **P2 · Confirmado** | `Dictionary<string,object>` domina respostas. Flexível, mas sem verificação de campos/tipos. | Quanto do contrato precisa de tipos fortes? | Envelope tipado mínimo e DTOs apenas para contratos estáveis; não criar classe para cada linha interna. |
| ARCH-05 | **P2 · Confirmado** | Configs têm defaults e validação desigual; alguns campos obrigatórios só falham tarde. | — | `Validate()` por config, schema e preflight antes de qualquer mutação. |
| ARCH-06 | **P2 · Confirmado** | Tratamento de parcialidade é implementado ad hoc em cada módulo. | — | Política transversal de `OperationResult`, severidade e exit aggregation. |
| ARCH-07 | **P2 · Confirmado** | Muitos comentários guardam conhecimento essencial; alguns já divergem do comportamento atual. | Comentário é fonte de verdade ou documentação gerada? | ADRs curtos para decisões, testes para invariantes executáveis e redução de duplicação entre SKILL/CLAUDE/README/PLANO. |
| ARCH-08 | **P2 · Confirmado** | Adapters usam reflection/strings para atributos e configuração de download. Isso dá compatibilidade, mas falha silenciosa é possível. | Quais strings são contrato Siemens por versão? | Capability discovery explícita, log de escolhas e fail-closed em escrita. |
| ARCH-09 | **P2 · Pergunta** | `TiaSession.Plcs()` e `Sim.DeviceItemOf()` percorrem apenas itens de primeiro nível de cada device. | PLC software pode estar em item mais profundo em famílias suportadas? | Confirmar SDK/projetos; se sim, recursão comum com path. |
| ARCH-10 | **P2 · Pergunta** | Attach escolhe primeiro projeto/local session dentro do processo; normalmente há um, mas a premissa não está formalizada. | Um processo TIA pode expor mais de um projeto/session? | Detectar quantidade e exigir seletor se >1. |
| ARCH-11 | **P2 · Confirmado** | Geradores embutem convenções ETE/nomes portugueses e defaults específicos da casa. | Produto é ferramenta genérica ou plataforma opinativa para essa engenharia? | Separar core genérico de “profile ETE”; profile versionado contém regras, nomes, layouts e audit. |
| ARCH-12 | **P2 · Oportunidade** | `doctor` cobre seis geradores, mas não todas as features/hardware/sim/release. | — | Capability/preflight registry por comando: requisitos locais, Portal, PLC, HMI, Startdrive, PLCSIM, escrita. |
| ARCH-13 | **P3 · Oportunidade** | CLI JSON já atende agentes; MCP foi adiado com razão. | Existe necessidade real de descoberta remota/streaming? | Não construir MCP sem caso concreto. Primeiro estabilizar schema/exit codes, que beneficiam ambos. |
| ARCH-14 | **P3 · Oportunidade** | Há espaço para plugins/perfis sem transformar o core em framework. | Usuários externos precisam de geradores próprios? | Manifestos + scripts externos chamando primitivas; só criar plugin API quando houver segundo perfil real. |

## 11. Domínio PLC, Openness, HMI, drives e simulação

| ID | Pri. / estado | Achado/limite | Pergunta | Próxima ação possível |
|---|---|---|---|---|
| PLC-01 | **P1 · Confirmado** | Safety está fora de escopo e não profundamente verificado (`LIMITES.md`). | Deve haver recusa explícita ao detectar blocos/CPU F? | `doctor/audit` com finding “Safety requer GUI/humano”; nunca sugerir automação parcial silenciosa. |
| PLC-02 | **P1 · Confirmado** | `nextFreeByte` é apenas piso; endereços unassigned e falhas de leitura impedem garantia. | Um agente pode aplicar endereço só com esse valor? | Exigir confirmação do Portal/error probe ou política de faixa reservada; tornar a inexatidão machine-readable e bloqueante opcional. |
| PLC-03 | **P1 · Confirmado** | Download PLCSIM pode dar falso positivo quando o clássico sequestra o access point. Hoje isso é pré-requisito documental. | Dá para detectar processo/serviço do clássico antes do download? | Preflight automático e verificação pós-download por tag/program signature; falhar se instância continuar vazia. |
| PLC-04 | **P1 · Confirmado** | `Resolve(DownloadConfiguration)` escolhe enum por nome e ignora configuração sem propriedade/enum compatível (`Sim.cs:519-526`). | Uma pergunta Siemens desconhecida pode ficar com default perigoso ou abrir diálogo? | Logar cada configuração e seleção; fail-closed se tipo/seleção não estiver allowlisted para PLCSIM. |
| PLC-05 | **P1 · Confirmado** | `sim-run` chama `GoOffline()` automaticamente se o projeto estiver online (`Sim.cs:139-146`). | Isso pode desconectar uma sessão que o engenheiro usa contra CPU real? | Bloquear se online target não for comprovadamente simulação; pedir ação humana ou flag específica com contexto. |
| PLC-06 | **P1 · Confirmado** | `sim-run --no-download` pode operar sobre programa antigo/instância vazia; só reporta tagCount. | Como provar que o programa corresponde ao projeto atual? | Gravar assinatura/version tag no programa ou comparar fingerprint antes dos steps; `--no-download` exige match. |
| PLC-07 | **P2 · Confirmado** | PLCSIM steps suportam números e Bool para escrita, mas não strings/time/date/arrays/UDTs. | Quais tipos são prioritários nos testes reais? | Capability list e erros antecipados; ampliar apenas com casos concretos. |
| PLC-08 | **P2 · Confirmado** | `wait` aceita inteiro direto em `Thread.Sleep`; não há limite total por script. | Agente pode pedir espera de horas por engano? | Limites e orçamento total, `--max-wait`, validação prévia. |
| PLC-09 | **P2 · Confirmado** | WinCC Unified não possui roundtrip SimaticML de tela; engine atual de tela é clássico. | Vale construir engine tipada Unified? | Só com caso real e fixtures; manter APIs separadas para não fingir paridade. |
| PLC-10 | **P2 · Confirmado** | `audit-screen` não cruza totalmente tag HMI → tag PLC por limitação/forma do export. | Há rota via conexões/tag tables ou API tipada que feche o vínculo? | Estudo SDK específico e resultado `skipped` explícito; não inferir conexão pelo nome. |
| PLC-11 | **P2 · Confirmado** | Motion é inventário read-only; TO não pode ser criado pela composição disponível. | Import de projeto/library pode transportar TO com segurança? | Documentar workflow oficial; não adicionar verbo fake. |
| PLC-12 | **P2 · Confirmado** | Multiuser é inventário read-only; check-in fica na GUI. | Necessidade real de automação de sessão local? | Manter fechado até laboratório dedicado e threat model de conflitos/credenciais. |
| PLC-13 | **P2 · Confirmado** | `import-ladder` cobre subset deliberado, não SCL geral. | Usuário entende que não é compilador SCL→LAD completo? | Capability command/exemplos negativos; mensagem recomenda `import-source`/`add-call`. |
| PLC-14 | **P2 · Confirmado** | Telegrama SINAMICS Startdrive e submódulo GSD são modelos diferentes. Documentação está boa, mas testes vivos dependem da família. | Quais drives/firmwares são certificados? | Matriz `family × Portal × Startdrive × telegram` e fixtures de saída sanitizada. |
| PLC-15 | **P2 · Confirmado** | `plug-module` tenta uma lista interna de versões de firmware até achar `CanPlugNew`. | Lista pode envelhecer ou escolher versão diferente da intenção. | Retornar candidatos, mas nunca adivinhar no apply; cache/catálogo derivado de hardware existente. |
| PLC-16 | **P2 · Confirmado** | `set-attr` é uma escape hatch poderosa para qualquer atributo gravável. | Deve haver denylist para atributos que mudam identidade/rede/segurança? | Classificar efeito e exigir `--force`/confirmação adicional para atributos sensíveis; sempre mostrar path/tipo. |
| PLC-17 | **P2 · Confirmado** | Importações deixam bloco inconsistente e o projeto criou `ExportFresh`/prova para absorver isso. | Todos os 16 exports passam realmente pela primitiva comum após features novas? | Teste estático/arquitetural que proíbe `.Export` direto fora de allowlist. |
| PLC-18 | **P2 · Confirmado** | Audit e geradores assumem padrão da casa: UDT, seis blocos por inversor, pastas numeradas, nomes específicos. | Usuário externo pode distinguir regra universal de profile local? | `audit --profile ete-v1`; profile documenta regras e versão. Core oferece checks genéricos separados. |
| PLC-19 | **P2 · Pergunta** | Compile “0 errors” pode ser seguido de export inconsistente por dependência externa/estado do Portal, conforme histórico. | Há maneira de consultar consistency state antes do export? | Se SDK não expõe, manter fallback, mas registrar bloco/dependência e número de compiles caros. |
| PLC-20 | **P3 · Oportunidade** | O ciclo criar → compilar → simular → observar permite testes de aceitação de PLC. | Qual formato de caso de teste industrial é desejado? | Manifesto com preconditions, inputs, waits, expected reads/tolerances e relatório JUnit/JSON, apenas PLCSIM. |

## 12. Desempenho, escala e contexto de IA

| ID | Pri. / estado | Achado | Pergunta | Otimização/aceite |
|---|---|---|---|---|
| PERF-01 | **P1 · Confirmado** | O limite é em chars, mas documentação/stub fala bytes; vide API-05. | — | Corrigir unidade antes de comparar benchmarks. |
| PERF-02 | **P2 · Confirmado** | Auto-spill reduz stdout, mas o objeto completo ainda é materializado e serializado em memória. | Projetos maiores que 476 blocos podem estourar memória/tempo? | Streaming para inventários grandes ou paginação/cursor; medir pico de memória. |
| PERF-03 | **P2 · Confirmado** | `find --kind tag` e snapshot produzem centenas de KB; auto-spill ajuda, mas consumers podem pedir `--full` sem perceber. | — | Warning/meta com estimativa antes do dump; filtros/paginação. |
| PERF-04 | **P2 · Confirmado** | Attach custa ~2–3 s; docs antigas ainda citam ~7 s em alguns pontos. | Qual hardware/projeto é baseline oficial? | Atualizar números com data/ambiente e evitar valores duplicados em várias fontes. |
| PERF-05 | **P2 · Confirmado** | Download domina `sim-run` (~91%); `--no-download` é rápido, mas pode usar programa stale. | — | Fingerprint de programa para habilitar skip seguro. |
| PERF-06 | **P2 · Oportunidade** | Batch reduz attach, mas não há planejador que agrupe automaticamente operações independentes. | Vale complexidade? | Preferir macros/manifestos explícitos; medir antes de automatizar agrupamento. |
| PERF-07 | **P2 · Confirmado** | `tia-help` baixa TOC de ~350 MB na primeira indexação e guarda caches sem versionamento. | Custo de disco/rede local é aceitável em cada update? | Cache versionado, status/tamanho/limpeza e índice incremental se API permitir. |
| PERF-08 | **P2 · Confirmado** | `trace/xref` varre fontes e pode crescer linearmente; hoje medido em 131 blocos/10 s e projeto de 476 blocos. | Quando índice invertido passa a valer? | Manter implementação simples até benchmark cruzar limite definido; registrar threshold. |
| PERF-09 | **P3 · Oportunidade** | `run --summary` mostra slowest 3, mas não percentuais/categorias (attach, export, compile). | — | Telemetria estruturada opcional com fase e duração, sem nomes sensíveis. |
| PERF-10 | **P3 · Oportunidade** | Repetidos exports/compiles poderiam usar cache, mas consistency e estado externo tornam cache arriscado. | Qual chave invalida corretamente? | Só cache read-only com fingerprint do objeto/LastModified exposto pela API; caso contrário, não otimizar. |

## 13. Documentação e experiência do desenvolvedor

| ID | Pri. / estado | Achado | Pergunta | Melhoria proposta |
|---|---|---|---|---|
| DOC-01 | **P1 · Confirmado** | Índice raiz NAV está stale: registra `Hmi.cs` com 43 linhas, omite `ScreenItems.cs` e `sim-host.ps1`, enquanto mapas filhos estão atualizados. | Hook atual só atualiza headers/algumas pastas? | Regenerar raiz após mudança estrutural e adicionar check de consistência no CI. |
| DOC-02 | **P1 · Confirmado** | `SECURITY.md` e CONTRIBUTING contradizem simulação/download/Project Server; vide SAFE-02. | — | Atualização imediata e changelog. |
| DOC-03 | **P1 · Confirmado** | README promete V19/V20 como superfície presente, mas admite ausência de teste; build atual indica incompatibilidade. | — | Capability/support matrix visível no topo. |
| DOC-04 | **P2 · Confirmado** | Números de `tree` variam entre help (~26 KB/117 KB), README/docs (~39 KB/150 KB) e histórico. | Qual medição é atual? | Uma tabela benchmark gerada, com projeto/data/commit; help evita número exato ou aponta para a tabela. |
| DOC-05 | **P2 · Confirmado** | `PLANO.md` continua enorme e mistura fonte de verdade, decisões, fases e histórico, apesar de o topo dizer que sagas foram movidas ao DIARIO. | Outra IA precisa ler quanto para entender o estado atual? | `STATUS.md` curto + ADRs + roadmap; PLANO/DIARIO como histórico pesquisável. |
| DOC-06 | **P2 · Confirmado** | Regras críticas se repetem em README, SKILL, CLAUDE, VERBS, LIMITES e PLANO. Divergências já apareceram. | Qual arquivo é canônico por assunto? | Mapa de autoridade e geração/inclusão onde possível. Ex.: segurança em SECURITY, comandos em registry/VERBS, operação em SKILL. |
| DOC-07 | **P2 · Confirmado** | Links em `library/README.md` apontam linhas antigas (`AlarmFc.cs:19-27` etc.) e podem estar stale após NAV headers. | — | Link checker e referências por símbolo/âncora quando possível. |
| DOC-08 | **P2 · Confirmado** | Contagem de 92 verbos não tem definição explícita (inclui help/version? linhas agrupadas?). | — | Script de contagem da command registry e badge gerado. |
| DOC-09 | **P2 · Confirmado** | Não há guia curto “efeitos no filesystem”, embora vários reads escrevam em workspace e `--out-file` sobrescreva. | — | Matriz por verbo: projeto TIA, filesystem, registry/task, rede, PLCSIM, persistência. |
| DOC-10 | **P2 · Confirmado** | Não há guia formal de recuperação após partial apply/timeout. | — | Runbook: não salvar, compilar, inspecionar recovery XML, reabrir backup, limpar lock/task. |
| DOC-11 | **P2 · Confirmado** | Instalação explica um checkout, mas não atualização, rollback de release ou desinstalação completa de tasks/PATH/registry. | — | `uninstall.ps1` seguro e documentado; rollback para release anterior. |
| DOC-12 | **P2 · Confirmado** | Dependência Python/httpx e Help Viewer não aparece nos requirements principais. | — | Seção “study tool requirements” e gate separado. |
| DOC-13 | **P2 · Pergunta** | Documentos de teste citam projeto-molde real, nomes e estruturas; cadernos dizem fictício, mas fixtures têm comentários “valores reais do molde”. | Há autorização para publicar todos os derivados? | Revisão de provenance por arquivo; substituir identificadores quando não houver certeza. |
| DOC-14 | **P3 · Oportunidade** | Não existe demo curta reproduzível nem baseline manual medido, admitido em BENCHMARKS. | Quem pode medir sem expor cliente? | Projeto sintético criado pelo CLI, gravação e cronômetro GUI vs CLI. |
| DOC-15 | **P3 · Oportunidade** | README é inglês, docs internas português; decisão é explícita, mas contribuidor externo pode se perder. | Público alvo principal? | Glossário bilíngue e tradução apenas dos guias de entrada/arquitetura, sem duplicar todo histórico. |
| DOC-16 | **P2 · Confirmado durante esta auditoria** | O `navindex.py` instalado declara suporte a Python/JS/TS/Go/PowerShell, mas não a C#. Ao regenerar a raiz, omitiu todos os `.cs`, incluiu artefatos ignorados em `obj/` e removeu os mapas C# existentes como “stale”. A saída foi corrigida/restaurada nesta entrega, mas o gerador continua incapaz de reproduzi-la sozinho. | O `navindex` deve ganhar suporte oficial a C# e respeitar `.gitignore`, ou este repo deve manter um gerador próprio? | Adicionar `.cs` ao parser/extensões, excluir `bin/obj` e consultar arquivos rastreados/ignore do Git; criar teste de regressão que exige `Program.cs`, `ScreenItems.cs` e ausência de `obj/` no índice global. |

## 14. Privacidade, segurança de software e aspectos legais

| ID | Pri. / estado | Achado | Pergunta | Mitigação proposta |
|---|---|---|---|---|
| SEC-01 | **P1 · Confirmado** | CI impede diretórios/extensões conhecidos, mas não conteúdo sensível fora deles. | — | Scanner de conteúdo + revisão humana + provenance. |
| SEC-02 | **P1 · Confirmado** | Release inclui todos os docs/library rastreados; um dado sensível em arquivo permitido vai para o zip. | — | Manifesto allowlist de release separado do conjunto total rastreado. |
| SEC-03 | **P1 · Confirmado** | Project Server pode usar HTTP (`Multiuser.cs:64-65`). | Existe ambiente legado que exige HTTP? | Deprecar; exigir flag de risco explícita e nunca default. |
| SEC-04 | **P2 · Confirmado** | Help Viewer usa TLS local com `verify=False`. É justificável por cert self-signed, mas o threat model não registra. | Um processo local malicioso pode ocupar a porta e servir conteúdo falso para a IA? | Validar processo/serviço proprietário da porta, restringir localhost e registrar risco; nunca aceitar host remoto com verify false. |
| SEC-05 | **P2 · Confirmado** | Scheduled task interativa lê `cmd.json` de workspace gravável pelo usuário. Isso não eleva privilégio, mas qualquer processo do mesmo usuário pode dirigir o exe whitelisted. | Esse é o boundary aceito? | Documentar ACL/boundary; opcional nonce/ACL mais restrita se houver múltiplos processos não confiáveis no mesmo usuário. |
| SEC-06 | **P2 · Confirmado** | Task elevada de whitelist pode whitelistar qualquer binário substituído no path canônico pelo próprio usuário. Não é elevação OS, mas amplia acesso Openness daquele usuário. | Grupo Openness já concede essa confiança? | Documentar modelo; verificar assinatura/hash esperado do build antes de executar task quando possível. |
| SEC-07 | **P2 · Confirmado** | Erros podem incluir caminhos, host, projeto e estrutura, e ficam em taskio/telemetry. | — | TTL/redaction; SECURITY deve cobrir dados em repouso, não só exfiltração. |
| SEC-08 | **P2 · Pergunta** | Fixtures XML/SCL e mascote têm licença/proveniência implícita sob MIT, mas não há inventário de origem. | Autor detém direitos de todos? | `THIRD_PARTY_NOTICES`/`FIXTURES.md`, inclusive assets gerados/derivados. |
| SEC-09 | **P2 · Confirmado** | Não há secret scanning específico. GitHub pode ter scanning do host, mas não está declarado no repo. | — | Habilitar secret scanning e pre-commit opcional; não registrar tokens/credenciais de Project Server. |
| SEC-10 | **P2 · Confirmado** | Não há política de backup obrigatória tecnicamente; docs dizem nunca produção, mas `--apply` funciona em qualquer projeto aberto. | É possível detectar caminho/projeto classificado como produção? | Guard opcional `--project-allowlist`, marcador `.tia-cli-test`, ou exigir `--ack-production-risk` fora de projetos marcados. |
| SEC-11 | **P3 · Oportunidade** | Nenhum log de auditoria imutável das escritas, apenas resultados e telemetry parcial. | Empresas precisarão rastreabilidade? | Journal local JSONL com operação, hash do plano, usuário, commit e resultado, sem payload sensível; opt-in. |

## 15. Produto e posicionamento

| ID | Pri. / estado | Observação | Pergunta estratégica | Possível direção |
|---|---|---|---|---|
| PROD-01 | **P1 · Pergunta** | O produto mistura engine genérica Openness e automação opinativa de ETE. | Quem é o usuário primário? | Definir core genérico + profile ETE oficial. Isso clareia documentação, audit e suporte. |
| PROD-02 | **P1 · Pergunta** | “92 verbos” comunica amplitude, mas também sugere superfície difícil de estabilizar para um mantenedor. | Crescer verbos ou consolidar fluxos? | Congelar novos verbos até fechar guardrails/schema/testes; priorizar workflows de valor. |
| PROD-03 | **P2 · Oportunidade** | Maior valor imediato é raio-X/audit de projeto legado, com risco baixo. | Esse deve ser o onboarding padrão? | “Read-only mode”/tour: doctor → tree → audit → trace → relatório. |
| PROD-04 | **P2 · Oportunidade** | Segundo maior valor é geração repetitiva com padrão da casa. | Quais três fluxos economizam mais horas? | Medir manual vs CLI para nova área, biblioteca e comissionamento PLCSIM. |
| PROD-05 | **P2 · Oportunidade** | Pipeline de aceitação PLCSIM pode diferenciar o projeto. | Há capacidade de mantê-lo seguro? | Só promover após SAFE-01/API-01/INST-09 e testes negativos. |
| PROD-06 | **P2 · Pergunta** | Release pública sem ambiente de CI Siemens depende fortemente da palavra do mantenedor. | Qual nível de garantia será prometido? | Labels: experimental/verified por versão; release manifest e suporte “latest only”. |
| PROD-07 | **P2 · Oportunidade** | CLI JSON é excelente base para outras IAs. | Precisa MCP agora? | Não. Primeiro schema estável, dry-run plan hash e guardrails; wrappers vêm depois. |
| PROD-08 | **P2 · Oportunidade** | Empresas podem querer profiles privados sem publicar IP. | Como separar código público e payload privado? | Package/profile local gitignored, schema público e comandos de bake/install; nunca enviar payload à nuvem por default. |
| PROD-09 | **P3 · Oportunidade** | Falta demonstração verificável simples. | — | Demo em projeto sintético criado do zero, sem cliente, com vídeo e artefatos esperados. |
| PROD-10 | **P3 · Oportunidade** | Suporte V19/V20 amplia mercado, mas multiplica custo. | Vale mais que estabilizar V21? | Basear decisão em usuários reais; não prometer sem hardware/licença/teste. |

## 16. Perguntas abertas para discutir com outra IA

Estas perguntas exigem decisão; não devem ser “resolvidas” apenas por refatoração automática.

1. A política irrevogável é **“nenhum download em CPU física”**? Se sim, `--pc-interface` deve ser
   removido/restrito e o código deve provar target PLCSIM.
2. `create-project`, `save-project`, `close-project` e `--keep-connection` são exceções formais ao
   dry-run ou passarão a exigir `--apply`?
3. Um resultado parcial deve retornar exit 1 sempre, ou existirão categorias `partial-success` com
   código próprio?
4. Batch de escrita deve ser fail-fast por padrão?
5. `audit` com check skipped pode afirmar `ok:true`, ou o correto é `complete:false`?
6. A compatibilidade oficial será V21 somente até haver laboratório V19/V20?
7. PLCSIM Advanced é requisito opcional, extra de instalação ou parte obrigatória do produto?
8. O release deve falhar com working tree suja ou empacotar exatamente `HEAD`?
9. O produto público é uma CLI genérica ou uma CLI + profile ETE de referência?
10. O contrato JSON será formalmente versionado e sujeito a SemVer?
11. Opções/configs desconhecidos devem sempre falhar ou haverá modo permissivo explícito?
12. Arquivos fora de `workspace/` podem ser sobrescritos por default?
13. Qual política de backup/rollback é exigida antes de `--apply`?
14. Quais dados locais devem ser apagados automaticamente e em quanto tempo?
15. Os fixtures e documentos derivados de projeto real têm provenance/autorização registrada?
16. É aceitável um runner self-hosted com TIA/PLCSIM para nightly/release?
17. Vale separar lógica pura agora, antes de novos verbos?
18. Quais três fluxos terão benchmark manual vs CLI para demonstrar ROI?
19. Codex deve ser alvo suportado além de Claude Code? Onde a skill deve ser instalada?
20. Safety será apenas recusado, inventariado ou terá integração manual assistida?
21. O índice de navegação será suportado para C# no skill compartilhado ou mantido localmente neste repo?

## 17. Plano de ação proposto

### Fase A — contenção de risco (antes de ampliar features)

1. Bloquear qualquer interface não comprovadamente PLCSIM em `sim-run`.
2. Fazer falha de download/step/partial operation produzir exit não zero.
3. Recusar flags desconhecidas e configs com propriedade desconhecida.
4. Proibir `--timeout` com `--apply` ou implementar modelo seguro.
5. Atualizar SECURITY/CONTRIBUTING/README para refletir simulação e rede.
6. Adicionar testes negativos dos cinco pontos acima.

Critério de saída: nenhuma forma documentada ou por typo baixa programa em interface física; nenhum
erro terminal retorna zero; escrita não pode ser interrompida sem estado explicitamente desconhecido.

### Fase B — instalação e release

1. Decidir V21-only versus multi-major e alinhar badges/loader/init.
2. Tornar PLCSIM opcional e resolver sua DLL localmente no prebuilt.
3. Selecionar conjunto coerente de assemblies, sem mistura e com manifesto de hashes/versões.
4. Corrigir pack dirty/HEAD e allowlist do bin.
5. Testar zip limpo com: TIA apenas; TIA+PLCSIM; máquina sem PLCSIM; upgrade de checkout.

Critério de saída: `init -Check` prediz corretamente se cada capability funciona; release é derivada
de commit limpo e não inclui binário Siemens.

### Fase C — contrato e arquitetura testável

1. Introduzir command registry/validação de opções.
2. Definir envelope de resultado e política de parcialidade.
3. Extrair transformações puras para assembly compilável no CI.
4. Migrar harness para test runner padrão sem perder fixtures.
5. Gerar VERBS/help/schema do mesmo registro.

Critério de saída: CI compila e testa toda lógica que não requer Siemens; docs não divergem do
dispatcher; consumidores podem confiar em `ok/status/exitCode`.

### Fase D — recuperação, observabilidade e compatibilidade

1. Recovery manifest/rollback para delete-before-import e force.
2. Journal/operation-id e retenção configurável.
3. Matriz TIA/Startdrive/HMI/PLCSIM/CPU.
4. Nightly/manual lab suite com projeto sintético.
5. Benchmarks reproduzíveis e fingerprint para `--no-download`.

### Fase E — produto

1. Separar profile ETE do core genérico.
2. Criar demo pública e três estudos de ROI.
3. Definir suporte Codex/Claude e instalação canônica.
4. Só depois avaliar MCP, plugins ou profiles de terceiros.

## 18. Backlog priorizado consolidado

### P0 — fazer primeiro

- [ ] SAFE-01: impedir download fora de PLCSIM.
- [ ] SAFE-03: tornar timeout seguro ou incompatível com apply.
- [ ] SAFE-04: rejeitar opções desconhecidas.
- [ ] API-01/API-02: erro interno/step deve alterar exit code.
- [ ] INST-01: corrigir ou retirar promessa V19/V20.
- [ ] INST-09: fechar distribuição/resolução da DLL PLCSIM.
- [ ] DOC-02: atualizar o contrato de segurança que hoje está factualmente stale.

### P1 — estabilização

- [ ] Resultado parcial e envelope comum.
- [ ] Fail-fast/allowFailure no batch.
- [ ] Audit tri-state e IO map com erros visíveis.
- [ ] Ambiguidade de item/interface em hardware.
- [ ] Rollback/recovery para operações destrutivas.
- [ ] Conjunto coerente de DLLs + whitelist da versão efetiva.
- [ ] Pack somente de HEAD limpo e allowlist binária.
- [ ] PLCSIM opcional no build.
- [ ] Lógica pura compilável no CI.
- [ ] Provenance/privacidade por conteúdo, não apenas path.

### P2 — maturidade

- [ ] Registry declarativo de comandos e schema de config.
- [ ] Test framework/coverage/analyzers/locks de dependência.
- [ ] Redaction, cleanup e retenção.
- [ ] Capability matrix e release verification manifest.
- [ ] Atualizar NAV/doc links/métricas duplicadas.
- [ ] Separar profile ETE.
- [ ] Fingerprint de simulação e preflight do PLCSIM clássico.

### P3 — expansão consciente

- [ ] Demo e ROI medido.
- [ ] Test cases de aceitação PLCSIM em formato declarativo.
- [ ] Assinatura/SBOM/reproducible build.
- [ ] Profiles privados/terceiros.
- [ ] MCP apenas se aparecer necessidade que CLI JSON não cubra.

## 19. Validação executada nesta auditoria

| Comando/verificação | Resultado |
|---|---|
| `git status --short --branch` | PASS antes da criação deste relatório; branch `main`, sem mudanças prévias. |
| `git log`, `git shortlog`, contagem de arquivos/linhas | PASS. |
| `pwsh scripts/init.ps1 -Check` | **FAIL esperado do ambiente**: grupo e .NET ok; TIA V17/V18/V19 detectados; faltam `lib/*.dll`, `tia.exe`, whitelist, tasks e PATH. |
| `dotnet run --project src/Tia.Tests/Tia.Tests.csproj` | **FAIL antes dos testes**: assemblies Siemens e PLCSIM ausentes. Nenhum PASS de C# é alegado. |
| `python scripts/tia-help.py --selftest` | PASS: `22` domínios. |
| Parser PowerShell em todos os `.ps1` | PASS: 19 scripts, zero erro. |
| Parse JSON em `docs/` e `library/` | PASS: 16 arquivos, zero JSON inválido. |
| Comparação estática docs/handlers | Sem verbo evidentemente órfão pelo extrator simples; a definição exata da contagem ainda deve ser formalizada. |
| Inspeção de `.gitignore` | PASS para paths principais de DLL/projeto/workspace/payload. Não prova ausência de dados sensíveis em paths permitidos. |
| Validação estrutural deste relatório | PASS: 170 achados únicos, zero ID duplicado, zero linha de achado malformada, zero whitespace final e todas as seções obrigatórias presentes. |
| Validação de referências `arquivo:linha` | PASS: 60 referências resolvidas, zero inválida e zero ambígua após qualificar os dois `Program.cs`. |
| Regeneração NAV | Suporte a títulos `#`/`##` de Markdown implementado e testado; este relatório ficou navegável por 23 títulos. A execução também expôs DOC-16; mapas C# removidos foram restaurados, `obj/` foi retirado e o índice global final preserva código-fonte + novo relatório. |

## 20. Estado local observado

- TIA Portal instalado: V17, V18 e V19; nenhum Portal estava rodando.
- Usuário pertence ao grupo `Siemens TIA Openness`.
- .NET SDK disponível.
- Checkout não está no path que o script considera `~/.claude/skills/tia`, embora o Codex desta
  sessão tenha carregado a skill diretamente deste repositório.
- Biblioteca `.al21` ausente, como esperado para payload gitignored.
- Não foi executado `init.ps1` sem `-Check`; nenhuma task, whitelist, PATH ou DLL foi modificada.

## 21. Definição sugerida de “pronto para uso confiável”

O repositório pode ser considerado pronto para recomendação ampla quando, no mínimo:

1. A fronteira PLC físico × PLCSIM estiver garantida por código e teste negativo.
2. Todo resultado terminal/partial tiver exit code coerente e schema estável.
3. Nenhuma opção desconhecida for ignorada em modo de escrita.
4. Release limpa instalar sem SDK, sem distribuir DLL Siemens e com PLCSIM opcional funcional.
5. Versões suportadas corresponderem à matriz realmente testada.
6. Toda lógica pura compilar/testar no CI público.
7. Operações delete-before-create possuírem rollback ou recovery manifest obrigatório.
8. SECURITY/README/CONTRIBUTING descreverem o comportamento atual, não a arquitetura anterior.
9. Fixtures/documentos tiverem provenance revisada.
10. Um projeto sintético reproduzir read → write → compile → PLCSIM → assertions sem intervenção
    além dos gates Siemens inevitáveis.

## 22. Conclusão

O projeto já passou da fase de prova de conceito. Ele contém conhecimento operacional raro e uma
superfície que pode economizar muitas horas em projetos Siemens. O maior retorno agora não virá do
93º verbo, mas de transformar as invariantes hoje espalhadas em guardrails executáveis e contratos
testáveis.

A tese de produto continua forte:

- leitura e auditoria de projetos legados;
- geração repetitiva guiada por profiles;
- biblioteca instalável e reprodutível;
- pipeline de compilação e aceitação em PLCSIM;
- interface segura para agentes de IA.

Mas a promoção para uso crítico deve esperar os P0: isolamento absoluto do download físico,
timeout seguro, exit code honesto, validação de flags e distribuição correta do PLCSIM. Fechados
esses pontos, o repositório tem uma base excepcionalmente promissora para engenharia Siemens
repetível, auditável e assistida por IA.
