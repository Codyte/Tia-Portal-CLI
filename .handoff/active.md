# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Fechar a **inicialização de projeto novo**: biblioteca da casa instalada num PLC virgem até compile
0 erros. Escopo dado pelo user: **biblioteca genérica** (sem equipamento de projeto real), **moldes
em pasta própria acima da Main**, **adaptar-se ao controlador que estiver no projeto**.

## State
- HEAD: f3c1c78. Working tree limpo.
- Live state: Portal aberto na sessão 1 com **Project1** (`proj/Project1/Project1.ap21`, descartável),
  2 devices — `PLC_1` (S7-1200, órfão, não existe `delete-device`) e **`PLC_1500`**
  (`6ES7 515-2AM02-0AB0/V2.9`), onde tudo foi testado; `PLC_1500` já tem os bits de system/clock
  habilitados e a biblioteca instalada (51 blocos). Serviço **"Siemens TIA Help Viewer Service"**
  no ar (Auto) e `workspace/help-index.txt` já gerado. `h2` instalado no Python do sistema.
- **Fatia 2 SCL fechada** ✅ — `library/core/bake.json` assa os 5 `.scl` em `library/core/xml/`;
  `library/core/core.json` é o manifesto. Round-trip: delete dos 5 → `scaffold --apply` = 5 created
  → compile **0 erros / 0 warnings**.
- **`set-memory-bytes` novo** ✅ — atributos `SystemMemoryByte`/`ClockMemoryByte` (bool) e
  `*ByteAddress` (só existem **depois** do enable, já nascem `%MB1`/`%MB0`). Idempotente.
- **`scripts/tia-help.py` novo** ✅ — ajuda oficial do TIA (a do F1) como texto: `--search "termo"`
  em 45518 tópicos (**1083 de Openness**), `--topic "PKG/TOC/ID.htm"` devolve o conteúdo.
  Ligado no `CLAUDE.md` do repo como regra: **consultar antes de deduzir a API**.
- **Orçamento de erro no PLC_1500 virgem**: 4 moldes = **65** → + `library.json` = **33** →
  + `set-memory-bytes` = **25** (≈20 tags de projeto + 5 `Missing instance DB` de molde).

## Decisions (and why)
- **Os 4 moldes já existiam** em `docs/examples/` (`ModuleErrorMolde.xml`, `FcModeloAlarmes.xml`,
  `ObMoldeAlarmes.xml`, `InstrumentTemplateFc.xml`). Falta a camada de dependência, não o desenho.
- **Molde é preso à família da CPU** — no S7-1200 o import morre em `The property 'DisableENO' is
  not supported for this instruction by the CPU used`. `<AutoNumber>true</AutoNumber>` já está nos
  XMLs, então número de OB/FC não colide.
- **Molde depende de instância modelo**: chama `FB AFERIÇÃO INSTRUMENTOS_FQIT-01`, o iDB nomeado
  pelo instrumento. Molde sozinho nunca compila limpo.
- Nome de cliente não está só nos blocos: está na árvore de pastas do `library.json` e nas tags.

### Tentado e descartado (não repetir)
- **Portar molde 1500 → 1200 mexendo no XML**: `grep DisableENO` nos 13 XMLs = **0 ocorrências**.
  A propriedade não está no arquivo; é o Portal materializando a instrução. Sem saída por texto.
- **`scaffold --force` pra reinstalar por cima**: não apaga antes, falha com *"A program element
  with this fully qualified name already exists in this CPU"*. Precisa `delete-block`/`delete-type`.
- **Ler a ajuda com `curl.exe` do Windows ou `Invoke-WebRequest`**: servidor só fala HTTP/2 sobre
  TLS; schannel morre em `SEC_E_ILLEGAL_MESSAGE`. Só com cliente OpenSSL (`httpx[http2]`).
- **`/HelpViewer/Search` do viewer**: responde 404 (assinatura não confere) — o índice sai do `/Toc`.

## Next steps (ordered)
1. **`--replace OLD=NEW` no `scaffold`/`import-block`** — troca de texto no XML **antes** do import
   (offline). Destrava biblioteca genérica e instância-neutra de uma vez; `clone --replace` não
   serve (exige o bloco já no projeto). Mapa: `FQIT-01`→`INSTR_01`, `S-01A`→`MOTOR_01`,
   `QA-01`→`QA_01`, `CCM-1`/`CCM1`→`CCM_01`, `Desarenador`→`AREA_01`, `SOPRADOR`→`MOTOR`.
   Aplicar também nas pastas do `library.json`.
2. **Sanitizar a biblioteca** com o verbo acima.
3. **`compile --errors`** = lista plana e única `{block, message, count}` (hoje é árvore de 15–18 KB;
   agreguei por fora 2x nesta sessão).
4. **Moldes em pasta própria** — `Folder` por item no manifesto, já suportado. ⚠️ Confirmar o nome:
   `"0.0 Moldes"` cai **depois** de `"0. Main"` na ordenação (`'0'`=0x30 > `' '`=0x20 na 3ª
   posição); pra ficar acima é `"0 Moldes"` ou `"00 Moldes"`.
5. `scaffold` valida a família do PLC alvo contra um campo `"Cpu"` do manifesto.
6. `--force` = delete + reimport · tag tables genéricas no manifesto (mata os ~20) · `add-device`
   sugerindo `/Vx.y` · `delete-device`.
7. Antigos: otimizar `raio-x.ps1` (`snapshot` 251 KB, `find --kind tag` 821 KB) · fatia 3 ·
   sanitizar nome de cliente em prosa.

## Key files
- `scripts/tia-help.py` — ajuda oficial como texto · `workspace/help-index.txt` (gitignored, 3.9 MB, regenerável em 16 s).
- `library/core/{bake,core}.json` · `library/core/xml/` · `library/core/README.md`.
- `src/Tia.Core/Hardware.cs` — `SetMemoryBytes` + discovery de atributo.
- `docs/PADRAO.md` (bits de system/clock) · `docs/PLANO.md` ("Instalação num PLC virgem") ·
  `docs/VERBS.md` (assinatura de todo verbo — ler em vez de grepar `Program.cs`).
- `CLAUDE.md` — seção nova "Não sabe como a API se comporta? Consulte a ajuda oficial".

## Open / blockers
- Project1 com station S7-1200 órfã; sem `delete-device`.
- `import-master-copy` sem `.al19`. Sem `checkpoint`/`restore` (retorno = `save-project` + backup).
- Todo import deixa o alvo **e quem o referencia** inconsistente → `compile --apply` entre etapas.
- Chamada pendurada com CPU ~0 = diálogo de aceite do Openness na tela: pedir o clique.

## Effort
**Médio** pro passo 1 (`--replace`) — troca de texto em XML; edge case é encoding (`AFERIÇÃO`,
`ANALÍTICA`) e ordem (`CCM-1` antes de `CCM1`). Sobe pra alto no mapa de sanitização (reescreve os
artefatos) ou se o Openness contrariar o doc — mas agora, antes de sondar, `tia-help.py --search`.
Gargalo real é attach do Portal (~3 s/chamada), não pensamento: `run --script` em lote vale mais
que qualquer nível.
