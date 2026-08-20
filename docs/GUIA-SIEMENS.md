<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L10    O que a própria Siemens manda fazer -->
<!--   L26    Documentos-lei -->
<!--   L33    Bibliotecas oficiais (código pronto, testado, gratuito) -->
<!--   L52    Onde o guia oficial e o padrão da casa divergem -->
<!--   L66    Como o agente usa isto -->
<!-- ======================= END NAV INDEX ======================= -->

# O que a própria Siemens manda fazer

Este repo carrega **dois** conjuntos de regras, e é preciso saber qual é qual antes de discutir
estilo com alguém de fora:

| Conjunto | Onde | Autoridade |
|---|---|---|
| **R1–R9** | [`BOAS-PRATICAS.md`](BOAS-PRATICAS.md) | padrão desta casa, verificável por `tia audit` |
| **Guia oficial** | este arquivo (ponteiros) | Siemens AG, aplicável a qualquer projeto S7-1200/1500 |

Onde os dois falam da mesma coisa, o oficial vem primeiro; onde a casa é mais estrita, a casa
manda, porque é o que o `audit` cobra. Divergência real está anotada abaixo.

Nada da Siemens é redistribuído aqui: são ponteiros por **Entry ID do SIOS**
(`support.industry.siemens.com/cs/document/<ID>`), que é como se acha o documento na versão atual.

## Documentos-lei

| Entry ID | Documento | Para que serve |
|---|---|---|
| **81318674** | *Programming Guideline for S7-1200/1500* + *Programming Styleguide* | como estruturar programa e como nomear. É a referência de estilo do ecossistema. |
| **109750255** | *Programming Guideline Safety for S7-1200/1500* | safety é outro jogo — F-runtime group, assinatura, teste. Não improvisar. |

## Bibliotecas oficiais (código pronto, testado, gratuito)

| Entry ID | Biblioteca | Cobre |
|---|---|---|
| **109479728** | **LGF** — Library of General Functions | escala, normalização, matemática, string, conversão, tempo, geradores de sinal, contadores. É a primeira parada antes de escrever FB utilitário. |
| **109475044** | **DriveLib** | `SINA_SPEED`, `SINA_POS`, `SINA_PARA` — controle de SINAMICS por telegrama. **O download é este entry**; o `206539` que circula é o post de fórum que aponta para ele, e não tem arquivo. |
| **109747655** | **LSINAExt** | extensão da anterior para controle de SINAMICS por blocos. |

Como consumir, agora que existe o verbo:

```powershell
tia retrieve-library --file DriveLib.zal19 --dir library --upgrade --apply   # .zal1x → .al21
tia list-library    --file library/DriveLib/DriveLib.al21                   # o que tem dentro
tia import-master-copy --file library/DriveLib/DriveLib.al21 --name SINA_SPEED --apply
```

**Regra prática:** biblioteca oficial antes de código autoral. Reescrever escala analógica ou
filtro de média é dívida sem ganho, e num pitch para a Siemens é o oposto do que se quer mostrar.

## Onde o guia oficial e o padrão da casa divergem

| Tema | Siemens (81318674) | Casa (R1–R9) | Quem manda aqui |
|---|---|---|---|
| Prefixo em parâmetro formal de FB/FC | não usar prefixo | R4: sem prefixo húngaro | igual — sem conflito |
| Convenção de nome | recomenda `PascalCase`/`camelCase` no exemplo em inglês | R4 fixa `MAIÚSCULA_UNDERSCORE`, como o molde da casa | **casa**: consistência com ~475 blocos existentes vale mais que a preferência do exemplo |
| Agrupar dados em UDT | recomendado | R1 torna **obrigatório** | **casa**, mais estrita |
| Tamanho de interface | sem número fixo | R3: ~8 escalares | **casa**, mais estrita |
| Linguagem por tipo de bloco | escolha do programador | R8: chamada em LAD, lógica em SCL dentro de FB | **casa** — é o que o eletricista lê na planta |
| Retentividade | declarar no que precisa | declarar **no FB**, `set-retain` | igual, com o detalhe de que o Openness recusa `Remanence` em iDB |

Onde a casa é mais estrita, é decisão de manutenção em campo, não discordância técnica — e é
assim que deve ser apresentada.

## Como o agente usa isto

```powershell
python scripts/tia-help.py --study "<o que vai fazer>"
```

devolve, para o tema: tópicos do F1, membros da API Openness, **qual biblioteca oficial já
resolve**, a restrição de hardware que muda o projeto se descoberta tarde, e as regras R que se
aplicam. O `catalog` da resposta lista o que existe na plataforma mesmo quando o tema não casa com
nenhum domínio — o ponto não é saber fazer tudo, é saber que existe e onde procurar.

Fontes: [Programming Guideline/Styleguide 81318674](https://support.industry.siemens.com/cs/document/81318674),
[Safety 109750255](https://support.industry.siemens.com/cs/document/109750255),
[LGF 109479728](https://support.industry.siemens.com/cs/document/109479728),
[DriveLib 206539](https://support.industry.siemens.com/tf/ww/en/posts/sinamics-blocks-drivelib-for-the-control-in-the-tia-portal/206539),
[LSINAExt 109747655](https://support.industry.siemens.com/cs/document/109747655).
