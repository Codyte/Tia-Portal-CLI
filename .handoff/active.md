# Handoff · TIA Portal Openness API · 2026-07-18 (5)

## Goal
Smoke completo no TIA **V21** (user instalou; V19 nunca instalado). User cria projeto de
teste manualmente no TIA com UI; sessão nova testa TODOS os verbos contra ele
(attach → hardware → tags → blocos → compile → round-trip).

## State
- HEAD: f63b933 + working tree: create-project (Program.cs/TiaSession.cs), scripts/ novos — NÃO commitado.
- Migração V21 pronta (f63b933): V21 **não tem mais Siemens.Engineering.dll monolítica** —
  trio Base/Step7/WinCCUnified em `PublicAPI\V21\net48` (namespaces iguais). lib/ trocada,
  csproj com 3 refs, resolver aceita qualquer `Siemens.Engineering*` (V21 net48 + V19/V20),
  env `TIA_ENGINEERING_DLL`→`TIA_ENGINEERING_DIR`. Build 0 erros; dry-run ladder ok.
- Verbo novo `create-project --dir D --name N [--no-ui]` (banido no batch como open-project).
- **Openness V21 tem 2 gates, estado atual:**
  1. Grupo Windows "Siemens TIA Openness": Carlos_Ortiz ADICIONADO ✅ (via UAC). Mas token de
     sessões existentes é velho — só logon novo pega o grupo. Task agendada **TiaSmokeRun**
     (S4U, token fresco) PASSA no gate do grupo ✅.
  2. Whitelist/firewall: registro HKLM+HKCU `...\Openness\21.0\Whitelist\tia.exe\Entry`
     (Path/DateModified UTC e local/FileHash SHA256-Base64) escrito e **RECUSADO** —
     headless morre com "not white-listed and the Openness Access dialog can´t be shown"
     (client S4U não-interativo). Formato V19/V20 documentado não colou no V21. Engenharia
     reversa (strings + ilspycmd em Base/Mapper.Impl/Server.Host) não achou o leitor — parar.
- runas interativo falhou (quoting do cmd /k) → user decidiu: **cria projeto manualmente**.

## Decisions (and why)
- D1-D9 valem. D9 (nunca paralelo) crítico nos smokes.
- Attach com TIA UI aberto = caminho FINAIS provado; headless/whitelist fica pra depois.
- Elevação: user aceita UAC prompts (vê o desktop do titanxnexus). runas c/ senha = só user digita.

## Next steps (ordered)
1. User abre TIA V21 **com UI** + projeto de teste (novo, descartável). Confirmar com user.
2. Rodar `tia info` via task S4U (protocolo abaixo). Esperado: popup Openness no TIA UI →
   user clica **"Sim para todos"** (+UAC; o próprio TIA grava a whitelist certa).
   Se popup não aparecer (client não-interativo bloqueia) → fallback: user roda
   `tia.exe info` direto num cmd NOVO no desktop (token do desktop ainda é velho → se falhar
   grupo, user faz logoff/logon do RDP — VSCode roda remoto, avaliar se sobrevive).
3. Após 1º verbo ok: `reg query "HKLM\...\Openness\21.0\Whitelist\tia.exe" /s` e HKCU —
   copiar formato REAL que o TIA gravou pro scripts/whitelist.ps1 (hoje é chute V19/V20).
4. Smoke sequência (D9, um por vez): info → list-devices → add-device --apply (MLFB sugerido:
   6ES7 511-1AK02-0AB0 V2.9 ou CPU 1214C 6ES7 214-1AG40-0XB0 V4.4; validar catálogo V21) →
   set-address → connect-subnet → create-folder → import-tags → import-source --apply →
   import-ladder --apply (RISCO: FlgNet de memória + `Engineering version="V19"` em portal V21
   — 1º ponto a checar se import falhar) → compile → export-block → diff-block → snapshot/find/xref
   → delete-block/folder → save-project.
5. Corrigir bugs no caminho; commit working tree atual + fixes (caveman-commit); PLANO tabela
   de fases; navindex regen (navi raiz está DESATUALIZADO — não lista src/*.cs).
6. Pendente antigo: /code-review dos 5 ports vs FINAIS; item 9 (online) segue bloqueado por D8.

## Key files
- src/Tia.Core/TiaSession.cs:74 — CreateProject novo (não commitado)
- src/Tia.Cli/Program.cs:394 — ResolveSiemensAssembly V21; :122 open/create-project pré-Attach
- scripts/whitelist.ps1 — formato provavelmente errado p/ V21; corrigir no passo 3
- scripts/taskrun.ps1 + task TiaSmokeRun — protocolo: escrever
  `workspace\taskio\cmd.json` (array JSON de args), apagar exit.txt/out.txt ANTES,
  `schtasks /Run /TN TiaSmokeRun`, poll exit.txt (race: sempre limpar antes de rodar)
- Task TiaWhitelist (SYSTEM, roda scripts/whitelist.ps1) — /Run exige elevação;
  ACL da chave HKLM Whitelist já dada ao user (grant-whitelist-acl.ps1) → editar direto
- workspace/testproj — dir do projeto de teste (vazio se user criar em outro lugar)

## Open / blockers
- Whitelist V21 headless sem resposta — mistério do formato; contornado via UI+popup.
- Projeto de teste ainda não existe (user vai criar).
- PATH PowerShell: `$env:Path=[Environment]::GetEnvironmentVariable("Path","Machine")+";"+[Environment]::GetEnvironmentVariable("Path","User")` se dotnet sumir.
- ilspycmd 8.2.0.7535 instalado global (decompilação futura).
