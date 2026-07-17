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
| D4 | **Attach em instância aberta** (`TiaPortal.GetProcesses().First().Attach()`) | Padrão já provado nos FINAIS. CLI não abre TIA sozinho. |
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
- **Portados dos FINAIS:** `gen-profinet`, `standardize-tags`, `replicate-fc`, `gen-fault-ob`

## Fases

| Fase | Entrega | Critério de pronto | Status |
|------|---------|--------------------|--------|
| F0 | Este plano + CLAUDE.md do repo | commitado | ✅ |
| F1 | Solução .NET, Tia.Core mínimo, verbos de leitura | `tia info` e `tia list-blocks` rodando contra TIA real | 🟡 código+build ok; smoke aguarda instalação do TIA V19 aqui |
| F2 | Export/import XML + compile | roundtrip de 1 FC sem diff + compile ok | 🟡 código+build ok; smoke aguarda TIA |
| F3 | Portar os 4 tools dos FINAIS como verbos | paridade com os scripts originais em projeto de teste | 🟡 gen-profinet portado; faltam standardize-tags, gen-fault-ob, replicate-fc |
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
  da instalação real (env `TIA_ENGINEERING_DLL` → pasta do exe → Portal V19/V20 padrão).
- Deploy do smoke: copiar `src\Tia.Cli\bin\Release\net48\` (tia.exe + Newtonsoft.Json.dll +
  Tia.Core.dll) pra máquina do TIA e rodar lá.

## Pendências / decisões futuras

- Licença (MIT provável) — decidir na F4.
- Nome público do repo — F4.
- Smoke F1 na máquina do TIA (user leva o exe; primeira execução dispara popup Openness — permitir).
