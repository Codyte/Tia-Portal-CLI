# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Biblioteca da casa genérica, por demanda e hierárquica, instalável num PLC virgem em **1 comando**
até o compile só acusar hardware. Passos 1–3 do handoff anterior fechados; sobra o G120 (hardware),
lint de camada e limpeza do projeto de teste.

## State
- HEAD: `ecdc354` (`feat(library): install-lib instala projeto inteiro; CPU virgem -> 4 erros`).
  Working tree limpo fora do próprio `.handoff/archive/`.
- **Live state**: 2 TIA Portal abertos na sessão 1 — PID 240 = `Software de ETE Insular_Inicial_V21`
  (projeto do cliente, **só leitura**, não foi tocado) e PID 6920 = `Project1` (descartável).
  **Todo verbo exige `--portal <nome|PID>`.** `Project1` já tem ~15 CPUs de teste, incluindo as duas
  desta sessão: `PLC_ONE` e `PLC_ZERO` (essa é a medição limpa: `add-device` + 1 comando = 4 erros).
- Done: **moldes na `.al21`** (`0 Moldes` na raiz + `Motor 1 (MOTOR_01)`, `3.1.0 Modelo`,
  `3.5 Barramento de Módulos` dentro da pasta `0 Moldes`); `library/packages.json` (requires/db/
  tags/types/instances por master copy); `install-lib.ps1` compondo tudo numa rodada; fix do
  `Ops.ResolveFolder` para nome de pasta com `/`; `library/tags/MOTOR_AREA_01 (MOTOR_01).xml`.
- In progress: nada mid-flight. Fatia commitada e documentada no PLANO
  (seção "Instalação em 1 comando").

## Decisions (and why)
- **Master copy de pasta leva os iDBs junto** (medido: `Motor 1 (MOTOR_01)` = 5 iDB + 1 FC importados
  inteiros) — era a dúvida que travava o passo 1.
- **Master copy NÃO leva UDT nem tabela de tag**, mesmo `PlcType`/`PlcTagTable` sendo
  `IMasterCopySource` (ajuda `TIAPortalOpennessenUS/37231359755/85077725323.htm`). Sem UDT o compile
  dá `Data type 'Diag_Hardware' no longer exists`. Seguem via XML: os UDTs que o `DB GLOBAL` usa saem
  do próprio SCL composto (regex `: "X"` → `library/blocks/X.xml`), o resto é `types[]` no
  `packages.json`. Não re-sondar master copy de UDT — não paga o verbo novo.
- **Nome de pasta contém `/`** no projeto real (`3. Alarmes/Eventos/Falhas`, `4. Motores/Bombas`,
  `5. Instrumentação / Atuadores`). `ResolveFolder` agora casa prefixo mais longo por nível, só na
  leitura (`create=false`); criar continua um segmento por vez.
- **Mapa pacote → fragmento de DB virou `packages.json`** (era o passo 3), junto com requires/tags/
  instances — um arquivo só em vez de quatro tabelas.
- Seguem valendo: pacote = pasta = 1 master copy · `.al21` é artefato (fonte = `.scl`/`.xml`) ·
  library types sem caminho por Openness · `import-source` exige UTF-8 com BOM · dependência =
  caminho da pasta · `--portal` obrigatório com mais de um portal.

### Tentado e descartado (não repetir)
- `list-blocks --folder ""` (raiz) devolve **array cru**, sem `{count,blocks}` — quebrou o skip do
  `install-lib` até tratar as duas formas.
- PowerShell: `$x = @(...) | Where-Object {...}` com 1 item devolve **escalar**; o `+=` seguinte vira
  concatenação de string. Envolver o pipe inteiro em `@( )`.
- Openness derrubou uma chamada longa com `EngineeringSecurityException: The operation has timed
  out` logo depois do `rebuild.ps1` (hash novo do `tia.exe`); repetir resolveu — não era whitelist
  stale.
- Anteriores: reorganizar pasta por API (Openness renomeia grupo, não move) · `move-block` in-place
  deixa cicatriz no vínculo chamada↔iDB · portar molde 1500→1200 por XML · `scaffold --force` ·
  `ValueFromRemainingArguments` engole `-Portal`.

## Next steps (ordered)
1. **Os 4 erros do `PLC_ZERO` = G120 ausente** (`INVERSOR_MOTOR_01_CCM_01`,
   `~PROFINET_interface~Standard_telegram_20`): achar a MLFB do G120 no catálogo V21, `add-device` +
   `connect-subnet` + telegrama 20, ou aceitar como "requer hardware" e documentar no PLANO.
   Consultar `python scripts/tia-help.py --search` antes de sondar a API.
2. **`delete-device`** — `Project1` com ~15 CPUs de teste e uma station S7-1200 órfã; hoje limpar é
   manual no Portal.
3. Lint de camada no `audit` (`CallInfo` pai→filho ou irmão falha).
4. Pendentes antigos: `Cpu` no manifesto + validação de família · `--force` = delete + reimport ·
   otimizar `raio-x.ps1`.
5. Opcional: trimar `requires` de `0 Moldes` (hoje puxa 1.1/1.3/1.4/1.5 sem medição por molde).

## Key files
- `scripts/install-lib.ps1` (`.al21` + `packages.json` → PLC, idempotente) ·
  `library/packages.json` · `scripts/bake-lib.ps1` (PLC → `.al21`) · `scripts/compose-db.ps1`.
- `library/db-global/*.scl` (`00-core` sempre + `motores`/`instrumentacao`/`afericao`) ·
  `library/blocks/*.xml` (fonte dos UDTs) · `library/tags/` (`Genericos.xml`,
  `MOTOR_AREA_01 (MOTOR_01).xml`).
- `src/Tia.Core/Ops.cs:67` (`ResolveFolder`, prefixo mais longo) · `src/Tia.Core/Library.cs:100`
  (`AddMasterCopy`).
- `docs/PLANO.md` → "Instalação em 1 comando" (números medidos desta sessão).
- Library de teste: `src/Tia.Lib/tia_cli/tia_cli.al21` (fora do git).

## Open / blockers
- User abriu o Insular dizendo "projeto base que será copiadas as fcs" e **nunca disse quais FCs** —
  pergunta ainda aberta.
- Todo import deixa o alvo e quem o referencia inconsistente → `compile --apply` entre etapas;
  bloco inconsistente não exporta.
- `--out-file` em `$env:TEMP` dá caminho 8.3 (`CARLOS~1`) que o Python não abre — usar `workspace/`.
- Chamada pendurada com CPU ~0 = diálogo de aceite do Openness na tela: pedir o clique.
- Nunca `git add -A` (trilha paralela do `scripts/tia-help.py` no mesmo repo).

## Effort
**Alto** para o passo 1 — MLFB do G120, `connect-subnet` e configuração de telegrama são terreno não
verificado, e hardware errado suja a CPU de medição. Ler a ajuda oficial antes
(`tia-help.py --search "telegram"` / `"GSD"`). Se a decisão for "aceitar como requer hardware e
documentar", cai pra **baixo**. Gargalo real não é raciocínio: attach do Portal leva 3–7 s e cada
compile ~1 min — juntar verbos em `run --script` vale mais que esforço extra.
