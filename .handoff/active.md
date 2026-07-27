# Handoff · TIA Portal Openness API · 2026-07-27

## Goal
CLI usável em qualquer máquina, contra TIA Project Server, com `init` de máquina e de **projeto**
(scaffold) — sem coreografia manual. Régua = projeto de referência `Software de ETE Insular_Inicial_V21`.

## State
- HEAD: bdaa59a — working tree limpo. `rebuild.ps1` ALL PASS (13 suítes, `Scaffold.Plan` incluída).
- Done nesta sessão:
  - **`tia scaffold --manifest F.json [--apply] [--force]`** — projeto novo recebe a árvore da lei
    (26 pastas de blocos+tags) e 66 objetos do padrão. Idempotente (`skip (exists)`).
  - **`workspace/padrao/` completado**: +51 exports do projeto de referência (13 UDTs, 34 FBs de
    `1. FB Bilbiotecas`, `DB_DUMMY`, `FB BITS TO WORD MODELO`, `FB DIAG MODULES_DB`,
    `DB DIAGNOSTICO DISPOSITIVOS`). Regen: `tia run --script workspace/export-padrao.json`.
  - **Aceite do `--apply` fechado** em `workspace/ScaffoldTest` (projeto novo, criado pela CLI):
    `create-project` → `add-device` CPU 6ES7 515-2AN03-0AB0/V3.1 → `scaffold --apply` **66/66** →
    `compile --apply` → `save-project` → `audit` **5/5 limpo**.
  - **2 bugs que só o ramo `create` expôs** (o dry passava): projeto novo sem a cultura `pt-BR`
    derrubava todo import → `Ops.EnsureCultures`; e `<Culture>` é **elemento**, não atributo.
  - `add-device --apply` exercitado pela 1ª vez (pendência do backlog item 3).
- In progress: nada mid-flight.

## Decisions (and why)
- Manifesto guarda caminho de pasta como **lista de segmentos**, não string com `/`: nomes reais
  contêm barra (`3. Alarmes/Eventos/Falhas`, `4. Motores/Bombas`) e `Ops.ResolveFolder` quebraria.
- Ordem de import é por **tipo de objeto** (UDT→tags→FB→DB global→iDB→FC→OB), não pela ordem do
  manifesto: iDB não importa limpo sem o FB dele.
- `Scaffold.Plan` é puro (sem TIA) → testável offline; `Run` só entra no portal.
- Objeto existente = `skip`, nunca override silencioso (`--force` é a válvula consciente).
- API de idioma levantada por reflexão na DLL: `LanguageSettings.ActiveLanguages.Add(...)`,
  `Languages.Find(CultureInfo)` (`LanguageComposition` não tem Create — é o que está instalado).

## Next steps (ordered)
1. `import-ladder` contra a verdade — gerar e comparar com `diff-block` contra um `PARTIDA_*` real.
   O FlgNet foi escrito de memória e `--apply` nunca rodou.
2. `replicate-fc --apply` — agora tem projeto vazio (`ScaffoldTest`) com o molde dentro; é o alvo
   certo (no de referência o guard barra, corretamente).
3. `scaffold`/`add-device` habilitar os bytes de **system/clock memory** da CPU: 8 dos 26 erros de
   compile do ScaffoldTest são `FirstScan`/`Clock_1Hz`/`AlwaysTRUE` não definidos.
4. Multiuser 3b/3c (`open-session`/`close-session`, commit com `MultiuserException` como conflito).
5. Concorrência (D9 vale por máquina): `--out workspace/` colide com N engenheiros; decidir lock.

## Key files
- `src/Tia.Core/Scaffold.cs` — `Rank` (ordem), `Plan` (puro), `Run`, `ResolveBlockPath`/`ResolveTagPath`.
- `src/Tia.Core/Ops.cs` — `EnsureCultures` + `XmlCultures` (perto de `XmlObjectName`).
- `src/Tia.Cli/Program.cs` — `case "scaffold"` no Dispatch.
- `docs/examples/scaffold-padrao.json` — manifesto (26 pastas, 66 itens, fonte `workspace/padrao/`).
- `docs/PADRAO.md` — seção `tia scaffold` + aceite e os 26 erros de compile explicados.
- `src/Tia.Tests/Program.cs` — `Scaffold_Plan` (ordem, segmento com `/`, culturas, arquivo ausente).

## Open / blockers
- Portal está com **`ScaffoldTest` aberto** (o de referência foi fechado sem salvar, autorizado pelo
  user, que tem backup). Voltar: `pwsh scripts/use-project.ps1 "Software de ETE Insular_Inicial_V21"`.
- Falta host/porta do TIA Project Server + projeto de teste lá (nunca produção) — trava o passo 4.
- Só 2 das 194 tabelas de tags entram no manifesto (as do acionamento-modelo); o resto é conteúdo
  de projeto, não padrão — se projeto novo precisar do esqueleto de IO, é decisão a tomar.
