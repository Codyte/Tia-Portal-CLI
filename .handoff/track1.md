# Track 1 · Portal · `replicate-fc --apply` contra dados reais
DONE: (preencher ao terminar — você é o **dono do merge**: quando este e o `track2.md` estiverem
DONE, leia os dois e rode `/handoff` pra fundir num `active.md` único. Protocolo em `active.md`.)

Você é o **único agente autorizado a chamar `tia`**. O track 2 é 100% offline e não vai te
atrapalhar. Leia `.handoff/active.md` (regras compartilhadas) antes; não leia `track2.md`.

## Objetivo
Último buraco do caminho de escrita: `replicate-fc --apply` nunca rodou contra dados reais, só
contra projeto scaffoldado (PLANO F3). Tudo já está preparado em disco — executar e julgar.

## Estado de partida
- Portal na sessão 1 com **Software de ETE Insular_Inicial_V21** (cópia de teste, backup do user,
  dano liberado). PLC compila **Success / 0 erros / 0 warnings**: qualquer erro depois do apply é
  do verbo, não herdado. Confirmar com `pwsh scripts/tia.ps1 info` (3s) antes de começar.
- Escrita fora de `ClaudeTest/` **autorizada** para os passos abaixo (decisão do user, 2026-07-28).

## Next steps (cada um com critério de aceite)
1. **Dry** — `replicate-fc-soprador.json` é *config*, não script de batch:
   ```
   pwsh scripts/tia.ps1 replicate-fc --config docs/examples/replicate-fc-soprador.json --out-file workspace/rep-dry.json
   ```
   Aceite: 1 grupo `Soprador`, molde `Soprador 1 (S-01A)`, **5 alvos `overwrite`** (S-01B..S-01F),
   6 blocos cada, nada fora de `4. Motores/Bombas`. Listou outro tipo → **parar** e reportar.
2. **Apply + idempotência** (batch pronto, steps isolados: falha vira `{ok:false,error}` e segue):
   ```
   pwsh scripts/tia.ps1 run --script docs/examples/replicate-soprador-run.json --out-file workspace/rep-run.json
   ```
   = `save-project` → `--apply` → `compile --apply` → `--apply` de novo → `compile --apply` →
   `save-project`. Aceite: **os dois compiles 0 erros** e o 2º apply sem reescrever nada.
3. **Conteúdo, não só compilação**: `diff-block --file <xml gerado em workspace/exports> --name
   "PARTIDA_SOPRADOR_2 (S-01B)"`. Aceite: `identical` (ou diferença explicável pelo replace de ID).
4. Se 1-3 verdes, emendar `gen-profinet --apply` e `standardize-tags --apply` — o dry mostrou
   `action: exists`/`ok`, quase no-op, custo marginal ~zero.
5. Fechar F8: atualizar **só a linha F8** da tabela de fases em `docs/PLANO.md` com o resultado.
   Se o track 2 estiver editando o PLANO, esperar — reler o arquivo imediatamente antes do Edit.

## Open / blockers — se der errado
- `Inconsistent blocks ... cannot be exported` → faltou compilar: `compile --apply` e repetir.
  (Todo import deixa o alvo e quem o referencia inconsistente — regra dura do CLAUDE.md.)
- Chamada pendurada com `tia.exe` vivo e CPU ~0 → diálogo de aceite do Openness na tela.
  Pedir o clique ao user, não investigar código.
- Whitelist stale (`EngineeringSecurityException`) → `Start-ScheduledTask -TaskName TiaWhitelist`.
  **Não** rodar `rebuild.ps1` (ver proibições em `active.md`).

## Seu território
- Escreve: `workspace/**`, linha F8 do `docs/PLANO.md`, `.handoff/track1.md`.
- **Não toca**: `library/`, `.gitignore`, `README.md`, seção "Biblioteca de blocos" do PLANO,
  `src/**`.
- Commit com caminhos explícitos, nunca `git add -A`.
