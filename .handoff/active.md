# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Biblioteca da casa **genérica, por demanda e hierárquica**, instalável num PLC virgem até compile
0 erros, empacotada como global library (`.al21`). Instalação por pacote já fecha em 0; falta levar
os **moldes** pra `.al21` e o instalador compor DB + iDBs sozinho.

## State
- HEAD: d73138a. Working tree limpo.
- **Live state**: 2 TIA Portal abertos na sessão 1 — PID 240 = `Software de ETE Insular_Inicial_V21`
  (projeto real do user, **só leitura nesta sessão**; baseline `compile` = Success 0 erros, não foi
  tocado) e PID 6920 = `Project1` (descartável). **Todo verbo agora exige `--portal <nome|PID>`.**
- `Project1` acumulou CPUs de teste: `PLC_GEN` (fonte da library, 87 erros — é o projeto-planta),
  `PLC_LIBT`, `PLC_T13/T14/T15/T15B/T16`, `PLC_MIX`, `PLC_DB`, `PLC_DB2` e **`PLC_FULL`** (conjunto
  completo, hoje em **2 erros**). Sem `delete-device` — limpar é manual no Portal.
- Global library de teste: `src/Tia.Lib/tia_cli/tia_cli.al21`, **fora do git** (`.gitignore`), com
  5 pacotes (`1.1 Acionamento`, `1.3`, `1.4`, `1.5`, `1.6`) + 5 blocos soltos de nível 1.
- Resultados medidos: cada pacote sozinho em CPU virgem = **0 erros**; manifesto inteiro + DB
  composto + tags + iDBs = **2 erros** (só a tag de telegrama do G120, que é hardware).
- Trilha paralela no mesmo repo (agente de "auto ajuda"): `scripts/tia-help.py`. **Nunca
  `git add -A`** — commitar com caminhos explícitos.

## Decisions (and why)
- **Master copy de pasta = pacote.** A ajuda oficial lista `PlcBlockUserGroup` entre os
  `IMasterCopySource` → pasta inteira vira 1 master copy, com subpastas. Revoga o "só se produz na
  mão" que tinha descartado a `.al19/.al21`, mas **não** revoga "`.al21` é artefato": fonte segue
  `.scl`/`.xml`, a library sai de `bake-lib.ps1`.
- **Library types: sem caminho por Openness.** `LibraryTypeVersion.Edit()` é "available in Project
  Library and **not supported via Global Library**", e não há API pra criar type do zero. Master
  copy é a via — não re-sondar.
- **`--portal` obrigatório com mais de um portal.** `Attach` fazia `GetProcesses().FirstOrDefault()`;
  com o projeto do cliente aberto ao lado, isso escreve no projeto errado.
- **Insular não foi reorganizado** (o user autorizou, mas não compra nada): a árvore nova se define
  ao gravar na library (`--lib-folder`), e `move-block` num projeto que compila 0 só deixaria
  cicatriz.
- **`move-block` in-place deixa cicatriz**: mover bloco *chamado* quebra o vínculo chamada↔instance
  DB (`Block call was invalid because interface was changed`) e 2 `compile --apply` não limpam. Em
  CPU virgem não acontece; o conserto é reimportar o chamador.
- **`import-source` exige UTF-8 com BOM** — sem BOM o acento corrompe, `"Aferição CMD"` não resolve
  e o erro é só `Error when calling method 'GenerateBlocksFromSource'`.
- Árvore movida na fonte: `1.7 Utilitários` dissolvida (5 blocos soltos no nível 1),
  `1.2 Inversores` → `1.1 Acionamento/1.1.1 Inversores`.
- Mantidas da sessão anterior: tudo por demanda · lei de escopo em 2 eixos (camada + profundidade) ·
  dependência = caminho da pasta (sem `requires[]`) · moldes em `"0 Moldes"`.

### Tentado e descartado (não repetir)
- **Reorganizar pasta por API**: o Openness **renomeia** grupo (`PlcBlockUserGroup.Name`) mas não
  move bloco nem grupo. Só export→delete→import.
- `list-blocks` sem `--folder` devolve **array cru**; com `--folder`, objeto `{count,blocks}`.
- `"a,b"` num token só (chamador bash) não vira array em PowerShell — `install-lib`/`compose-db`
  fazem `-split ','` por isso.
- `ValueFromRemainingArguments` engole `-Portal` como "resto" — usar `[Parameter(Position=0)]`.
- Anteriores que seguem valendo: portar molde 1500→1200 por XML · `scaffold --force` · injetar
  bloco no painel *Instructions* · medir no `PLC_1500` · ler a ajuda com `curl.exe`.

## Next steps (ordered)
1. **Moldes como pacote na `.al21`**: hoje só `1.x` está lá. `add-master-copy --folder "0 Moldes"`
   (+ `3.`/`4.` se fizer sentido) e conferir se o master copy leva junto os iDBs criados por
   `create-instance-db`.
2. **`install-lib.ps1` compondo o resto**: chamar `compose-db.ps1` conforme os pacotes escolhidos,
   `import-tags` do `Genericos.xml` e os `create-instance-db` — roteiro pronto em
   `docs/examples/install-full.json`. Alvo: "projeto novo até 0 erros" em 1 comando.
3. **Mapa pacote → fragmento de DB** (hoje o fragmento é escolhido à mão no `compose-db`).
4. Os 2 erros finais do `PLC_FULL` exigem o **G120 no hardware** (`INVERSOR_MOTOR_01_CCM_01`,
   telegrama 20): descobrir MLFB e fechar via `add-device` + `connect-subnet`, ou aceitar como
   "requer hardware" e documentar.
5. Lint de camada no `audit` (`CallInfo` pai→filho ou irmão falha).
6. Pendentes antigos: `Cpu` no manifesto + validação de família · `--force` = delete + reimport ·
   `delete-device` · otimizar `raio-x.ps1`.

## Key files
- `scripts/bake-lib.ps1` (PLC → `.al21`) · `scripts/install-lib.ps1` (`.al21` → PLC, idempotente) ·
  `scripts/compose-db.ps1` (fragmentos → `DB GLOBAL`, grava com BOM).
- `library/db-global/*.scl` — `00-core` (sempre) + `motores`/`instrumentacao`/`afericao`.
- `library/tags/Genericos.xml` — 11 tags em `%M` a partir de 5520.
- `library/generic.json` — 64 itens (entrou `DIAG to STRING_DB.xml`).
- `docs/examples/install-full.json` — receita que leva 81 → 2 erros.
- `src/Tia.Core/Library.cs` (add/delete master copy, `UserGlobalLibrary.Save()`) ·
  `src/Tia.Core/TiaSession.cs:PickProcess` (`--portal`) · `src/Tia.Core/Ops.cs:CreateInstanceDb`.
- `docs/PLANO.md`, seção "Biblioteca de blocos" — as 3 subseções novas têm os números medidos.

## Open / blockers
- User abriu o Insular dizendo "projeto base que será copiadas as fcs" e **nunca disse quais FCs** —
  pergunta aberta. Copiar do Insular traz os nomes reais do cliente; genericizar exige
  export → `--replace` → import no `PLC_GEN` + rebake, não master copy direto.
- `Project1` com ~11 CPUs de teste e uma station S7-1200 órfã; sem `delete-device`/`checkpoint`.
- Todo import deixa o alvo **e quem o referencia** inconsistente → `compile --apply` entre etapas;
  bloco inconsistente não exporta.
- `--out-file` em `$env:TEMP` dá caminho 8.3 (`CARLOS~1`) que o Python não abre — usar `workspace/`.
- Chamada pendurada com CPU ~0 = diálogo de aceite do Openness na tela: pedir o clique.

## Effort
**Baixo–médio** para o passo 1 — `add-master-copy --folder` já está provado em 5 pastas e o verbo faz
a coreografia; o único desconhecido é se o master copy de `0 Moldes` leva os iDBs junto, e o
`compile --errors` responde na hora. Sobe para **alto** no passo 4 (MLFB do G120 e configuração de
telegrama são terreno não verificado — consultar `scripts/tia-help.py --search` antes de sondar).
Gargalo real não é raciocínio: é attach do Portal (~3–7 s por chamada) e compile — juntar tudo num
`run --script` vale mais que qualquer nível de esforço.
