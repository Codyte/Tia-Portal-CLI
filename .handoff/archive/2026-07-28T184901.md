# Handoff · TIA Portal Openness API · 2026-07-28

## Goal
Biblioteca da casa **genérica, por demanda e hierárquica**, instalável num PLC virgem até compile
0 erros — e, no fim, empacotada como **global library (`.al21`)** ao estilo das bibliotecas Siemens.

## State
- HEAD: 6e559e1. Working tree limpo (fora este handoff).
- Live state: Portal na sessão 1 com **Project1** (`proj/Project1/Project1.ap21`, descartável).
  3 devices: `PLC_1` (S7-1200 órfã), `PLC_1500` (biblioteca antiga instalada, **suja** — não medir
  nela) e **`PLC_GEN`** (`6ES7 515-2AM02-0AB0/V2.9`, criado nesta sessão, é o PLC de medição).
- **Trilha paralela no MESMO repo** (agente de "auto ajuda"): `scripts/tia-help.py` (ajuda oficial
  do F1 como texto — 45518 tópicos, 1083 de Openness; `--search`/`--topic`) + regra no `CLAUDE.md`
  "consultar antes de deduzir a API". Commits `f3c1c78`/`6e559e1`. **Nunca `git add -A`** — commitar
  com caminhos explícitos; os dois agentes dividem working tree e `.handoff/active.md`.
- Done nesta sessão: `--replace OLD=NEW` no `scaffold`/`import-block` (`514d91b`) · campo `Replace`
  no manifesto + `library/generic.json` (`bc38ee7`) · `compile --errors` lista plana (`ebcd3e2`).
- Medição no `PLC_GEN` virgem (core + generic + set-memory-bytes, 0 falha de verbo): **82 erros** —
  63 de ramo ausente no `DB GLOBAL` genérico, 14 de tag de PLC, 5 `Missing instance DB`.
  Concentrados em 5 blocos: `PARTIDA_MOTOR_1` 39, `MOLDE_ANALOGS` 36, `MOLDE TOT1` 5, resto 2.
- Medição de acoplamento da biblioteca: 14 chamadas entre blocos → **12 entre irmãos**, 2 na mesma
  pasta, **0 sobe/desce**. Provedores são sempre `1. FB Bibliotecas`; consumidores são molde,
  aplicação (`3.`, `4.`) e `1.1 Acionamento`. Nada de `1.x` chama `3./4.`.

## Decisions (and why)
- **Tudo por demanda** (decisão do user) — projeto pequeno leva só o que usa. Unidade da demanda é
  **pacote = pasta**, não bloco: bloco sozinho não importa limpo (fecho de dependência).
- **Lei de escopo em 2 eixos** (a de 1 eixo não sobrevive à medição acima):
  1. *Camada*: `core` + `1. FB Bibliotecas` = escopo global, visível de qualquer lugar; aplicação
     consome biblioteca, biblioteca nunca consome aplicação. Sem isso `0 Moldes → 1.3` seria
     violação eterna (em TIA a biblioteca não tem como ser pasta-mãe da aplicação).
  2. *Profundidade* (a regra do user): o que é compartilhado sobe; `X.` nível 1, `X.X` nível 2.
     Largura cresce só na folha (`1.1.1 Inversores`, `1.1.2 Válvulas`, …) sem encanamento novo.
- **`requires[]` deixa de existir** — dependência = caminho da pasta (instalar `1.1.1` instala os
  ancestrais). Mata o `packages.json` com lista manual que eu havia proposto.
- **`DB GLOBAL` não tem "4 ramos fixos"** — cada pacote traz o seu ramo como fragmento `.scl`; o DB
  final é a concatenação dos pacotes escolhidos. Molde é o *exemplo de instância do pacote*, entra
  junto com ele (é o que permite 0 erros sem carregar planta alheia).
- **`.al21` é artefato, não fonte** — binário opaco: git versiona o blob mas não dá diff/review/merge.
  Fonte fica `.scl`/`.xml` em texto; `.al21` sai de um build (mesma forma do `bake.json`).
- **Library *type* ≠ master copy** — type é versionado e propaga *Update instances*; é o que dá cara
  de biblioteca Siemens. Custo: bloco tipado fica read-only no projeto.
- `DB GLOBAL.xml` e `DISPOSITIVOS_PROFINET.xml` **fora** do manifesto genérico: são a planta
  (152 e 35 tokens de tag distintos), mapa de substituição só disfarçaria.
- Moldes em `"0 Moldes"` (sem ponto): `"0.0"` cai depois de `"0. Main"` na ordenação.

### Tentado e descartado (não repetir)
- **Portar molde 1500 → 1200 por XML**: `grep DisableENO` nos 13 XMLs = 0 ocorrências — é o Portal
  materializando a instrução, não o arquivo. Sem saída por texto; caminho é set de molde por família.
- **`scaffold --force` pra reinstalar por cima**: não apaga antes; falha com *"already exists in this
  CPU"*. Exige `delete-block`/`delete-type`.
- **Injetar bloco de usuário no painel *Instructions***: não existe API — é conteúdo de firmware.
  O painel certo é *Libraries → Global libraries* (é assim que a própria Siemens distribui a LGF).
- **Medir orçamento de erro no `PLC_1500`**: já tem a biblioteca instalada, quase tudo volta
  `skip (exists)`. Número honesto só em CPU virgem.
- **Ler a ajuda com `curl.exe`/`Invoke-WebRequest`**: servidor só fala HTTP/2 sobre TLS, schannel
  morre em `SEC_E_ILLEGAL_MESSAGE`. Só com cliente OpenSSL (`httpx[http2]`).

## Next steps (ordered)
1. **Decidir a movida da árvore** (pergunta aberta ao user): mover `1.7 Utilitários` (4 blocos —
   `FB CONTADOR`, `FB_HORÔMETRO`, `FB BITS TO WORD`, `FB TOTALIZADOR`) para soltos em
   `1. FB Bibliotecas`, e `1.2 Inversores` → `1.1.1`. Verbo é `move-block` (export→delete→import).
   Isso zera 7 das 12 chamadas irmãs; as 5 restantes viram legais pela regra de camada.
2. **Pacote = pasta** no `scaffold`: `--package "1.1 Acionamento"` instala ancestrais + a pasta.
3. **`DB GLOBAL` composto**: um fragmento `.scl` por pacote, concatenado e importado de uma vez.
4. **Critério de aceite**: cada pacote sozinho num PLC virgem = **0 erros** (hoje 82 no conjunto).
5. **Lint de camada** dentro do `audit`: `CallInfo` que aponta pai→filho ou irmão falha (~30 linhas;
   a varredura ad-hoc que mediu isso está descrita em State).
6. **Probe da API de library types** — reflexão sobre a DLL falhou por dependência; confirmar
   abrindo `.al21` em RW (precisa de uma library de teste vazia, criada no Portal). `list-library`
   já lê master copies **e** types; falta o lado da escrita.
7. Pendentes antigos: `Cpu` no manifesto + validação de família · `--force` = delete + reimport ·
   tag tables genéricas (mata os 14) · `delete-device` · otimizar `raio-x.ps1`.

## Key files
- `library/generic.json` — 63 itens, 11 pares `Replace`, moldes em `0 Moldes`.
- `library/core/{bake,core}.json` · `library/core/xml/` · `library/core/README.md`.
- `src/Tia.Core/Clone.cs:RewriteFile` — substituição offline pré-import (scaffold e import-block).
- `src/Tia.Core/Scaffold.cs` — `Merge` (manifesto + CLI), `Apply` (segmentos de pasta), `Plan`.
- `src/Tia.Core/Ops.cs:~650` — `Compile(..., errorsOnly)` + `FlattenErrors`.
- `scripts/tia-help.py` (trilha paralela) — ajuda oficial como texto; consultar antes de deduzir API.
- `docs/VERBS.md` — assinatura de todo verbo, ler em vez de grepar `Program.cs`.

## Open / blockers
- **Duas perguntas abertas ao user**: mover a árvore agora ou depois? E crio a `.al21` vazia de
  teste no Portal para o probe de types?
- Escrever `--out-file` em `$env:TEMP` dá caminho 8.3 (`CARLOS~1`) que o Python não abre — usar
  `workspace/` (já gitignored, caminho curto).
- `EngineeringSecurityException: "The operation has timed out."` apareceu 1x com whitelist boa e
  sumiu no retry — a mensagem de whitelist stale também cobre timeout transitório de attach.
- Project1 com station S7-1200 órfã; sem `delete-device`. Sem `checkpoint`/`restore`.
- Todo import deixa o alvo **e quem o referencia** inconsistente → `compile --apply` entre etapas.
- Chamada pendurada com CPU ~0 = diálogo de aceite do Openness na tela: pedir o clique.

## Effort
**Médio** para o passo 1 — `move-block` é export→delete→import e a ordem importa (importar antes de
apagar falha), mas o verbo existe e faz a coreografia; o risco é referência quebrada, que o
`compile --errors` mostra na hora. Sobe para **alto** se a movida quebrar chamada que hoje compila,
ou no passo 6 (API de types, terreno não verificado — consultar `tia-help.py --search` antes de
sondar). Gargalo real é attach do Portal (~3 s/chamada) e o compile, não raciocínio: `run --script`
em lote vale mais que qualquer nível.
