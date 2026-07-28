# Núcleo genérico (fatia 2) — autoral, versionado

Diferente de `library/blocks/` (66 exports do cliente, gitignored): **isto aqui é escrito do zero e
vai pro Git**. É o mínimo que faz `doctor` ficar verde e os 4 geradores rodarem num projeto novo.

| arquivo | tipo | contrato que precisa ser respeitado |
|---|---|---|
| `MotorDados.scl` | UDT | estrutura por motor que `replicate-fc` remapeia |
| `ValvDados.scl` | UDT | idem, para válvula |
| `MotorPrincipal.scl` | UDT | par principal/reserva; **composto** de dois `MotorDados` |
| `DB GLOBAL.scl` | GlobalDB (esqueleto) | `<AREA>.ALARMES.WORD_ALARMES_<n>` e `HARDWARE_INTERRUPT.ALARMES_MODULOS.<QA-xx>.WORD_<n>` |
| `FB BITS TO WORD.scl` | FB | pinos `SIGNAL_Bit0..15` + saída `BITS_TO_WORD` — nomes lidos por [AlarmFc.cs:313](../../src/Tia.Core/AlarmFc.cs#L313) e [:348](../../src/Tia.Core/AlarmFc.cs#L348) |

Faltam os 4 moldes em LAD (`MODULE_ERROR_MOLDE`, `FC_Modelo`, `OB_MOLDE_ALARMES`, `MOLDE_ANALOGS`) —
esses não dá pra escrever em SCL, os geradores clonam rede a rede.

## Ordem de import (obrigatória)

O compilador exige o tipo referenciado já existir: `MotorDados` e `ValvDados` primeiro,
depois `MotorPrincipal` e `DB GLOBAL`, `FB BITS TO WORD` em qualquer ponto.

```powershell
pwsh scripts/tia.ps1 import-source --file "library/core/MotorDados.scl" --apply
pwsh scripts/tia.ps1 import-source --file "library/core/ValvDados.scl" --apply
pwsh scripts/tia.ps1 import-source --file "library/core/MotorPrincipal.scl" --apply
pwsh scripts/tia.ps1 import-source --file "library/core/DB GLOBAL.scl" --apply
pwsh scripts/tia.ps1 import-source --file "library/core/FB BITS TO WORD.scl" --apply
pwsh scripts/tia.ps1 compile --apply
```

Validado 2026-07-28 nessa ordem, **0 erros / 0 warnings** (rodado com nomes sufixados `_T` pra não
colidir com os blocos homônimos do projeto de referência).

## Por que não entra no `scaffold` ainda

`Scaffold.Plan` lê o tipo do objeto do XML ([Scaffold.cs:84](../../src/Tia.Core/Scaffold.cs#L84)) —
não conhece `.scl`. E `import-source` não tem `--folder`: bloco nasce na raiz. Os dois juntos
significam que o caminho de instalação do núcleo é assar o `.scl` em `.xml` uma vez
(`import-source` → `compile --apply` → `export-block`) e versionar o XML ao lado do `.scl`, com um
manifesto próprio. O `.scl` continua sendo a fonte da verdade — é ele que se lê e se diffa.
