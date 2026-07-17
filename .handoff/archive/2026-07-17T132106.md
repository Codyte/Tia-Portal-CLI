# Handoff · TIA Portal Openness API · 2026-07-17

## Goal
CLI .NET `tia` (verbos JSON) expondo Openness TIA V19+ p/ agentes IA + engenheiros.
Extração dos scripts provados em `Scripts_Siemens/FINAIS/` (referência read-only).

## State
- HEAD: 5e670d3
- Done: F0 plano; F1 código (attach/inventário, verbos info/list-devices/list-blocks/list-tags);
  F2 código (export-block/export-tags/import-block/import-tags/compile, dry-run + --apply);
  F3 parcial: gen-profinet portado. Build 100% ok (.NET SDK 8, net48/x64).
- In progress: F3 — faltam portar 3 tools dos FINAIS:
  1. standardize-tags ← `Padronizador de Variável-FINAL.txt` (664 ln)
  2. gen-fault-ob ← `Gerador de OB Falha Módulos FINAL.txt` (612 ln)
  3. replicate-fc ← `Replicador de FC AcionamentosV3-FINAL-Program.txt` (917 ln) + variantes
     Alarmes (1028 ln) e Instrumentos (1087 ln) — unificar as 3 num verbo se possível
- NENHUM smoke real rodado ainda: TIA V19 desinstalado da máquina; user vai instalar (hoje ~meio-dia).

## Decisions (and why)
- Ler docs/PLANO.md → D1–D9 travadas (CLI-first, exe único, net48/x64, dry-run/--apply,
  sem online ops, 1 chamada por vez, código EN/docs PT). NÃO rediscutir.
- Máquina atual = titanxnexus (servidor; user acessa de pcprojetos5 via VSCode Remote).
  TIA será REINSTALADO AQUI — build e execução na mesma máquina. Cuidado: não derrubar sessão remota.
- lib/Siemens.Engineering.dll v19 (gitignored; origem: E:\Scripts\adam_optmizer\AdamTarget\AdamTarget\bin\Debug\)
  = referência de compile; runtime resolve via env TIA_ENGINEERING_DLL → pasta exe → Portal V19/V20.
- Devices podem estar em DeviceGroups → sempre usar TiaSession.AllDevices(), nunca Project.Devices direto.
- Escrita aplicada sempre dentro de session.ExclusiveAccess (WriteLock no Program.cs).
- Attach: portal.Projects.First() → fallback LocalSessions (Multiuser). ProjectBase em toda a API.
- Config de tools: JSON chaves EN (ex.: docs/examples/profinet.json), defaults = valores dos scripts originais.
- Token economy: sem spawn de agentes; leitura cirúrgica via __navi__.md; build output filtrado.

## Next steps (ordered)
1. Portar standardize-tags: ler `Scripts_Siemens/FINAIS/Padronizador de Variável-FINAL.txt` inteiro
   (única leitura), extrair p/ src/Tia.Core/Standardize.cs + verbo no Program.cs (padrão do Profinet.cs:
   config class + Generate(session, plc, config, apply) retornando Dictionary).
2. Portar gen-fault-ob (mesmo padrão).
3. Portar replicate-fc (maior; 3 variantes — comparar as 3 e unificar; XML roundtrip via Ops.cs existe).
4. Quando TIA V19 instalado: smoke F1/F2 (tia info, list-blocks, export→import roundtrip, compile)
   com PROJETO DE TESTE (nunca produção), depois marcar fases ✅ no PLANO.
5. Fim de F3: /code-review (previsto no plano), atualizar PLANO, commit.

## Key files
- docs/PLANO.md — decisões D1–D9, fases, regras de sessão (LER PRIMEIRO)
- src/Tia.Core/TiaSession.cs — attach, AllDevices, ExclusiveAccess, GetPlc
- src/Tia.Core/Ops.cs — FindBlock/ResolveFolder, export/import XML, compile
- src/Tia.Core/Profinet.cs — MODELO do padrão de port (config class + dry-run actions list)
- src/Tia.Cli/Program.cs — switch de verbos, WriteLock, AssemblyResolve, Require/OptionValue
- Scripts_Siemens/FINAIS/*.txt — originais (read-only, ler 1x por port)
- __navi__.md — mapa do repo (navindex; regen após mudança estrutural)

## Open / blockers
- TIA V19 ainda não instalado (bloqueia smokes; código avança sem ele).
- Verbos HMI (telas) não planejados no v1 — só se user pedir.
- Build: dotnet no PATH exige `$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine") + ...`
  (sessão PowerShell não recarrega PATH pós-install).
