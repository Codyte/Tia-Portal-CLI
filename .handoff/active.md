# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Fechar o ciclo da biblioteca. O bloqueio de várias sessões (telegrama do G120) caiu nesta sessão —
falta o único ramo que escreve, num G120-2 virgem.

## State
- HEAD: `df451bc` — em sincronia com `origin/main`. O repo de skills também (submódulo bumpado).
- Live state: **TIA Portal aberto na sessão 1, projeto `Base_tia_cli`**
  (`proj/Base_tia_cli/Base_tia_cli.ap21`), 24 devices, PLCs `CPU1.0 CCO` e `PLC_TESTE`. Uma
  instância só — verbo não precisa de `--portal`. Shell do agente nasceu na **sessão 0** (rota da
  task `TiaSmokeRun`). O `rebuild.ps1` rodou 3x com o Portal aberto: se um verbo devolver
  `EngineeringSecurityException: The operation has timed out`, é o diálogo modal de autorização do
  Openness esperando clique na tela — repetir a chamada depois de clicar resolve.
- Done: repo renomeado pra `Codyte/Tia-Portal-CLI` em todas as referências; submódulo `tia`
  registrado no repo de skills (o gitlink nunca tinha sido commitado, só o `.gitmodules`); análise
  do `totally-integrated-claude`; verbos `list-telegrams`/`insert-telegram`; `tia-help.py --sdk`;
  dois bugs de script (ver Decisions). 69 verbos, `init.ps1 -Check` = 9/9 verde.
- In progress: nada mid-flight.

## Decisions (and why)
- **Telegrama de drive System nunca foi `plug-module`.** O drive object tem `TelegramComposition`
  própria (`Siemens.Engineering.MC.Drives`, assembly `Startdrive`); não existe TypeIdentifier de
  catálogo pra procurar — daí três frentes de busca terem dado nada. A API ficou invisível porque
  `init.ps1` copiava 3 das 14 assemblies do `PublicAPI`. Detalhe em `docs/PLANO.md` § "Telegrama do
  G120".
- **`DriveObjectNumber` estoura em vez de devolver valor** em G120 sem dados de comissionamento
  (`Drive object number could not be retrieved`). Toda leitura de atributo de drive passa por
  `Try()`. Quem identifica o drive é o caminho do item, não o número.
- **`currentStateHash` no dry-run: descartado.** Pressupõe servidor segurando estado entre preview
  e apply; cada `tia` é attach novo, então exigiria store de nonce em disco. `run --script` já fecha
  a janela (dry e apply no mesmo attach). Reabrir só se aparecer dry reaproveitado entre sessões.
- **`totally-integrated-claude` (MIT): avaliado, nada de código reaproveitado.** São skills de
  documentação da API, não CLI concorrente. Rendeu o telegrama, as assemblies faltando e a ideia do
  `--sdk`. Clone em `workspace/_ext/tic` (gitignored). Descartado: o wheel Python
  `siemens_tia_scripting` e o framework de skills roteadas.
- **Dois bugs de script que davam relatório falso em máquina correta:** `gen-verbs.ps1` lia stdout
  sem fixar encoding (mojibake em todo o `VERBS.md` a cada regeneração), e `init.ps1 -Check`
  comparava caminho por string com `~/.claude/skills` sendo Junction — os gates 7/8/9 mandavam
  *mover* um checkout que estava certo. `Resolve-RealPath` resolve, e o `rebuild.ps1` tem o check
  que impede a versão degenerada.

## Next steps (ordered)
1. **Ramo que escreve do `insert-telegram`**, o único não testado: precisa de um G120-2
   (`OrderNumber:6SL3244-0BB12-1FA0/4.7.13`) **sem** telegrama. Sequência: apagar o `PLC_TESTE`
   velho (68 blocos duplicados) → `new-plc.ps1 PLC_TESTE "<pacotes>" -Apply` numa CPU virgem →
   `insert-telegram --number 20` em dry (esperar `canInsert: true`) → `--apply` → comparar com a
   régua. É o que fecha os 4 erros `INVERSOR_MOTOR_01_CCM_01~PROFINET_interface~Standard_telegram_20`.
2. Se sobrar tempo: as 10 assemblies ainda fora do `lib/` (Safety, SafetyValidation,
   TeamcenterGateway, WinCC clássico, 5 de AddIn) — nenhuma necessária hoje, acrescentar só quando
   um verbo pedir. `--sdk` já indexa as 14, então dá pra confirmar a API antes de mexer no csproj.

## Key files
- `src/Tia.Core/Drives.cs` — os dois verbos novos; `Try()` no topo é o que segura atributo que estoura.
- `docs/PLANO.md` § "Telegrama do G120" — achado, smoke de 3 ramos, o que falta.
- `scripts/__navi__.md` e `src/__navi__.md` — mapas das duas pastas que o passo 1 toca (ambos
  regenerados nesta sessão).
- `scripts/new-plc.ps1` — o macro do passo 1.
- `python scripts/tia-help.py --sdk "termo"` — antes de sondar qualquer API no braço.

## Open / blockers
- Nenhum bloqueio de decisão. O passo 1 depende só de haver uma CPU virgem com G120-2 no projeto.

## Skills
- tia
- ponytail
- caveman

## Effort
**Médio** para o passo 1: a sequência é documentada e os três ramos de leitura já estão provados,
mas é o primeiro `--apply` do verbo e comparar com a régua exige ler o diff de erros com cuidado.
Subir pra **alto** só se `canInsert` vier `true` e o `InsertTelegram` mesmo assim falhar — aí é API
contra a própria documentação. O gargalo aqui não é raciocínio: `new-plc.ps1` numa CPU virgem e cada
attach do Openness dominam o relógio.
