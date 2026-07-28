# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Fechar a **inicialização de projeto novo**: instalar a biblioteca da casa num PLC virgem e chegar a
compile 0 erros. O ciclo continua sendo *operação real → medir onde doeu → virar verbo/flag*.
Escopo novo dado pelo user: **biblioteca genérica** (sem equipamento de projeto real), **moldes numa
pasta separada acima da Main**, e **adaptar-se ao controlador que estiver no projeto**.

## State
- HEAD: 3b15134. Working tree limpo.
- Portal na sessão 1 com **Project1** (`proj/Project1/Project1.ap21`), projeto de teste descartável.
  2 devices: `PLC_1` (S7-1200, órfão — não existe `delete-device`) e **`PLC_1500`**
  (`6ES7 515-2AM02-0AB0/V2.9`, adicionado por `add-device`) — é nele que tudo foi testado.
- **Fatia 2 SCL fechada** ✅ — `library/core/bake.json` assa os 5 `.scl` em `library/core/xml/`
  (import-source na ordem → compile → export-type/export-block); `library/core/core.json` é o
  manifesto do `scaffold`. Round-trip validado: delete dos 5 → `scaffold --apply` = 5 created →
  compile **0 erros / 0 warnings**.
- **`set-memory-bytes` novo** ✅ (verbo de hardware) — habilita `SystemMemoryByte`/`ClockMemoryByte`;
  os atributos de endereço só existem **depois** do enable e já nascem `%MB1`/`%MB0`. Nome descoberto
  em runtime por substring em `GetAttributeInfos()` (varia V19–V21) → dry-run serve de sonda.
  Idempotente (`changes: []` na 2ª chamada). `rebuild.ps1` ALL PASS.
- **Orçamento de erro medido no PLC_1500 virgem**: só os 4 moldes = **65** → + `library.json`
  (51 blocos + árvore de pastas) = **33** → + `set-memory-bytes` = **25**. Os 25 restantes: ~20 tags
  de projeto (`FQIT-01_*`, `S-01A_*`, `QA-01_*`, `CCM-1_*`) + 5 `Missing instance DB` de molde.

## Decisions (and why)
- **Os 4 moldes já existiam** em `docs/examples/` (`ModuleErrorMolde.xml` = `MODULE_ERROR_MOLDE`,
  `FcModeloAlarmes.xml` = `FC_Modelo`, `ObMoldeAlarmes.xml` = `OB_MOLDE_ALARMES`,
  `InstrumentTemplateFc.xml` = `MOLDE_ANALOGS`). Não falta desenhá-los — falta a camada de
  dependência. PLANO/PADRAO já corrigidos.
- **Molde é preso à família da CPU.** No S7-1200 o import morre em `The property 'DisableENO' is not
  supported for this instruction by the CPU used`. `grep DisableENO` nos 13 XMLs = **0 ocorrências**:
  a propriedade não está no arquivo, é o Portal materializando a instrução. Logo **não dá pra portar
  1500→1200 por XML**; caminho é um set de moldes por família + detectar a família e recusar cedo.
  `<AutoNumber>true</AutoNumber>` já está nos XMLs → número de OB/FC não colide.
- **Molde depende de instância modelo**: chama `FB AFERIÇÃO INSTRUMENTOS_FQIT-01`, ou seja o iDB
  nomeado pelo instrumento. Molde sozinho nunca compila limpo.
- **`scaffold --force` não apaga antes de importar** — falha com *"already exists in this CPU"*.
  Reinstalar por cima exige `delete-block`/`delete-type` primeiro.
- Nome de cliente não está só nos blocos: está na árvore de pastas do `library.json`
  (`4.1.1 Desarenador/Soprador 1 (S-01A)`) e nas tags.

## Next steps (ordered)
1. **`--replace OLD=NEW` no `scaffold`/`import-block`** (substituição de texto no XML **antes** do
   import, offline). Destrava biblioteca genérica e instância-neutra de uma vez; `clone --replace`
   não serve porque exige o bloco já no projeto. Mapa proposto: `FQIT-01`→`INSTR_01`,
   `S-01A`→`MOTOR_01`, `QA-01`→`QA_01`, `CCM-1`/`CCM1`→`CCM_01`, `Desarenador`→`AREA_01`,
   `SOPRADOR`→`MOTOR`. Aplicar também nas pastas do `library.json`.
2. **Sanitizar a biblioteca** com o verbo acima.
3. **`compile --errors`** = lista plana e única `{block, message, count}` (hoje é árvore de 15–18 KB;
   precisei agregar por fora 2x nesta sessão).
4. **Moldes em pasta própria** — `Folder` por item no manifesto, já suportado, 1 linha cada.
   ⚠️ Perguntar o nome: `"0.0 Moldes"` cai **depois** de `"0. Main"` na ordenação alfabética
   (`'0'`=0x30 > `' '`=0x20 na 3ª posição); pra ficar acima é `"0 Moldes"` ou `"00 Moldes"`.
5. `scaffold` valida a família do PLC alvo contra um campo `"Cpu"` do manifesto → erro claro em vez
   de `EngineeringTargetInvocationException`.
6. `--force` = delete + reimport de verdade · tag tables genéricas no manifesto (mata os ~20) ·
   `add-device` sugerindo `/Vx.y` na mensagem · `delete-device`.
7. Pendentes de antes: rodada de otimização do `raio-x.ps1` (`snapshot` 251 KB, `find --kind tag`
   821 KB sem filtro) · fatia 3 (utilitários genéricos) · sanitizar nome de cliente em prosa.

## Key files
- `library/core/bake.json` · `library/core/core.json` · `library/core/xml/` (5 XMLs versionados) ·
  `library/core/README.md` (contrato + comandos).
- `src/Tia.Core/Hardware.cs` — `SetMemoryBytes` + `FindMemoryItem`/`IsMemoryAttribute` (discovery).
- `src/Tia.Cli/Program.cs` — `case "set-memory-bytes"`, helper `ParseByte`.
- `docs/PADRAO.md` — seção dos bits de system/clock (pendência fechada, atributos documentados).
- `docs/PLANO.md` — "Instalação num PLC virgem, medida 2026-07-28" (orçamento 65→33→25).
- `docs/VERBS.md` — assinatura de todo verbo. Ler isto em vez de grepar `Program.cs`.

## Open / blockers
- Project1 tem uma station S7-1200 órfã; sem verbo pra apagar device.
- `import-master-copy` sem `.al19` de teste. Sem `checkpoint`/`restore` (ponto de retorno =
  `save-project` + backup).
- Regra dura: todo import deixa o alvo **e quem o referencia** inconsistente → `compile --apply`
  entre etapas.
- Chamada pendurada com CPU ~0 = diálogo de aceite do Openness na tela: pedir o clique.
