<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L11    Divulgação — como o CLI ganha visibilidade (2026-08-19) -->
<!--   L16    Estado de partida -->
<!--   L21    O que falta para o reconhecimento pela Siemens -->
<!--   L43    Canais, por retorno -->
<!--   L56    Antes de mandar tráfego -->
<!--   L65    O vídeo -->
<!-- ======================= END NAV INDEX ======================= -->

# Divulgação — como o CLI ganha visibilidade (2026-08-19)

Documento de planejamento, não de engenharia. O plano de correção de código está no `PLANO.md`
(F16); aqui só o que diz respeito a mostrar o projeto para fora.

## Estado de partida

Repo já é **público** (`Codyte/Tia-Portal-CLI`, MIT, 2 stars), com release `v1.0.0` de 2026-08-11
e topics de busca já postos. O gargalo é distribuição, não permissão.

## O que falta para o reconhecimento pela Siemens

Duas leituras diferentes, e só a segunda é trabalho de divulgação.

**Reconhecimento técnico já existe:** o Portal executa o CLI pela whitelist do Openness (registro,
por caminho e hash do exe) com o usuário no grupo `Siemens TIA Openness`.

**Reconhecimento como produto de terceiro** — o que falta, do barato ao caro:

| # | Lacuna | Natureza |
|---|--------|----------|
| 1 | Binário não assinado (`CI-09`, P2). Hoje só SHA-256 nas release notes. Sem Authenticode, todo `rebuild.ps1` muda o hash e dispara o diálogo modal — e avaliação externa não aceita exe sem assinatura. Precisa de certificado OV/EV em nome de PJ e `signtool` no `pack.ps1` | técnica, no repo |
| 2 | Matriz de versão de uma linha só: provado em V21. Os topics do GitHub anunciam `v19`/`v20`/`v21`, que é promessa não provada — a primeira issue de quem chegar será "não roda no V19" | técnica, no repo |
| 3 | Nada verificável sem a máquina do autor (`CI-01`/`CI-02` dependem de um split recusado na §0 da auditoria). "Verificação é o que você rodou localmente" não sobrevive a um revisor externo | técnica, no repo |
| 4 | Matriz de hardware certificado (`PLC-14`): hoje uma CPU e um drive | técnica, no repo |
| 5 | Siemens Xcelerator / Solution Partner Program — único caminho para "reconhecido" de fato. Exige PJ, contrato, revisão jurídica de marca, documentação em EN (normalmente DE também) | formal, fora do repo |
| 6 | Publicação no SIOS como Application Example — caminho mais leve que parceria: a Siemens revisa e hospeda. Pede documentação no padrão SIOS, em EN, e código sem nome de cliente (já garantido pelo gate F4) | formal, fora do repo |
| 7 | Badge com a cor corporativa `#009999` + link para `siemens.com` no README. O disclaimer de marca protege, mas a cor sugere endosso. Trocar por cinza neutro é uma linha | jurídica, no repo |

Ordem sugerida: 7 (grátis) → 1 (assinatura) → 3 (CI) → 6 (SIOS). O item 5 só depois de haver
usuário externo real.

## Canais, por retorno

Postagem semanal em comentário de blog da Siemens no LinkedIn foi considerada e **descartada**:
comentário em post alheio quase não converte, e cadência semanal sem novidade queima audiência. O
que fica:

1. **Fórum SIOS "TIA Portal Openness"** — onde de fato estão os que usam Openness. Responder pergunta
   real ("como exportar bloco em lote?") com solução e link converte muito acima de qualquer post.
   ~20 min/semana.
2. **r/PLC (Reddit)** — comunidade grande e receptiva a ferramenta grátis.
3. **Post próprio no LinkedIn**, com vídeo, **na semana de cada release** — não semanal.
4. **PLCtalk / control.com**, PR em listas `awesome-*` de automação industrial.

## Antes de mandar tráfego

O visitante decide nos primeiros 15 segundos:

- **Demo visual no topo do README** (GIF de ~25 s). Maior retorno por hora de todo o plano.
- **Corrigir a promessa de versão** (item 2 acima) — ou testa V19/V20, ou declara "provado em V21".
- **Badge neutro** (item 7).
- Duas ou três issues marcadas como ponto de entrada.

## O vídeo

**Regra dura:** nada de nome de cliente na tela — nem no terminal, nem na barra de título do Portal,
nem na lista de recentes. Gravar em `Base_tia_cli` / PLC `PLC_TESTE`. Se o take pedir área molde para
`replicate-fc`, popular esse projeto antes; vira fixture reutilizável.

### Roteiro — 75 s, cinco planos

| # | Tempo | Plano | Comando |
|---|-------|-------|---------|
| 1 | 0-6 s | Gancho, só terminal. Legenda: "TIA Portal from the command line" | `tia tree` |
| 2 | 6-25 s | Split terminal/Portal: bloco aparecendo na árvore do Portal enquanto o comando roda. É o plano que prova que é real | `add-call` ou `clone --apply` |
| 3 | 25-45 s | JSON in/JSON out + batch | `run --script gen-all.json --summary` |
| 4 | 45-60 s | O diferencial: agente dirigindo o CLI sozinho | `audit` → `trace` → `explain-block` |
| 5 | 60-75 s | Fecho: URL, `git clone` + `pwsh scripts/init.ps1`, MIT | estático |

O plano 4 é o que separa este repo de qualquer script Openness que já exista. Não cortar.

### Comandos read-only para o take do agente

Critério: 2-9 s, saída curta e legível, dor real de engenheiro.

| Prompt | Verbo | Tempo | Por que prende |
|--------|-------|-------|----------------|
| "Como esse PLC está organizado?" | `tree` | 3 s | 476 blocos viram um markdown de 26 KB |
| "Quem usa a `DB GLOBAL`?" | `xref --name` | 2 s | Cross-reference no Portal é clique e espera |
| "Me mostra tudo do equipamento AG-01" | `trace --equipment` | ~9 s | É *a* pergunta do dia a dia |
| "Explica o que esse FC faz" | `explain-block --name` | ~4 s | LAD vira texto legível — o plano de maior "como assim?" |
| "Esse projeto segue o padrão?" | `audit --max 5` | 4 s | 10 checks com ✓/✗; visualmente o melhor take |
| "Qual o próximo byte de I/O livre?" | `list-io-map` | rápido | Dor conhecida, resposta instantânea |
| "Roda esses 5 comandos de uma vez" | `run --script --summary` | 8,1 s | Permite a legenda "20,1 s soltos → 8,1 s em batch" |

Bônus seguros: `replicate-fc` **em dry-run** (mostra os 6 blocos que seriam criados, sem escrever) e
`sim-diag` (estado do PLC virtual, sem Portal aberto nem projeto — bom plano de fecho).

**Nunca gravar:** `snapshot` (251 KB), `find --kind tag` (821 KB), `list-blocks` sem filtro. A tela
vira cachoeira de JSON.

### Take de `--apply` — a GUI se mexendo sozinha

Vista de Redes aberta, terminal embaixo. É o material mais convincente que existe, porque não há
cursor na tela.

| # | Comando | O que aparece |
|---|---------|---------------|
| 1 | `add-device --mlfb "6ES7 ..." --name Bomba_01 --apply` | device brota na vista de rede |
| 2 | `set-address --device Bomba_01 --ip 192.168.0.11 --apply` | IP muda no rótulo |
| 3 | `connect-subnet --device PLC_TESTE --subnet PN/IE_1 --io-system PNIO_1 --apply` | subnet nasce |
| 4 | `connect-subnet --device Bomba_01 --subnet PN/IE_1 --io-system PNIO_1 --apply` | **a linha PROFINET é desenhada** — o frame do vídeo |

Ordem obrigatória: PLC primeiro (cria o IO system), device depois (entra nele). Invertido, o device
cai no controlador errado. Se for SINAMICS, encaixa `insert-telegram --number 20 --change --apply`.

Outros applies filmáveis: `set-memory-bytes --clock 0 --apply` (checkbox marcando sozinho nas
propriedades da CPU), `create-folder` com vários `--path` (pastas em cascata), `add-tag` (linha
aparecendo na tabela aberta), `rename-block`, e `add-call --apply` como clímax (rede LAD escrita
pelo agente; conte 15-30 s e corte na edição).

`add-db-member --apply` custa ~48 s numa DB grande (medido — ver `BENCHMARKS.md`) e cai bem abaixo
disso numa DB de demo. Filmável com corte seco. **Não filmar** `replicate-fc --apply` nem
`import-screen` (20-170 s).

### Gravação e edição

- OBS Studio, 1920×1080, 30 fps, fonte do terminal em 18-20 pt (LinkedIn toca em mobile).
- Tema claro grava melhor que escuro sob compressão de rede social.
- Comando já no histórico, seta para cima + Enter. Digitação ao vivo com erro custa retake.
- Sem narração; legenda queimada (85 % assiste sem som).
- Cortar toda espera acima de ~2 s, com legenda dizendo o custo real ("compile: 48 s"). Esconder o
  custo queima credibilidade no primeiro usuário. **Não acelerar em 4x** — parece demo falsa.
- Fechar com número, não com adjetivo: "5 comandos: 20,1 s soltos → 8,1 s em batch".

### Três cuidados de ensaio

1. **Confirmar que a GUI atualiza ao vivo** antes de montar o roteiro em cima disso. O Openness
   escreve no mesmo projeto em memória do Portal anexado, então deve redesenhar — mas se a vista
   precisar de refoco, o take muda. É o único risco real do plano.
2. **Salvar cópia do projeto neutro antes de gravar.** Cada retake deixa device e pasta para trás;
   restaurar a cópia é mais rápido que apagar na mão.
3. Rodar cada comando uma vez antes de gravar: o primeiro attach do dia é mais lento e pode disparar
   o diálogo modal de autorização.

### Saídas

- **GIF de ~25 s** (planos 1 e 3), sem áudio, ate 5 MB, no topo do README — todo visitante vê.
- **MP4 de 75 s** com os cinco planos, para LinkedIn e Reddit.

**Não fazer:** intro animada, logo girando, música épica, narração explicando o que é Openness. O
público é engenheiro de automação.
