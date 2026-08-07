# Handoff · TIA Portal Openness API · 2026-08-07

## Goal
Provar a engine num teste cego ponta a ponta: um caderno de especificação fictício (máquina, CPU,
sensores, I/O, narrativa de controle) entra numa sessão sem contexto, e ela tem que sair com um
projeto TIA que compila. É o mesmo que a Siemens vai perguntar — "um agente consegue mesmo?".

## State
- HEAD: `7afab95` + 1 commit local de benchmarks (todos pushados).
- Live state: **TIA Portal aberto na sessão 1, 2 processos**, projeto `Base_tia_cli` com dois PLCs
  (`CPU1.0 CCO`, `PLC_TESTE`). `DB_DUMMY` do `PLC_TESTE` foi mexido e voltou ao original (compile
  Success/0); projeto **não foi salvo**. Shell do agente na sessão 0 (rota da task). Diálogo de
  autorização Openness já foi aceito pro hash atual do `tia.exe` — novo `rebuild.ps1` traz ele de
  volta.
- Done nesta sessão: gate de máquina limpa exercitado (clone local em temp) + 2 hints corrigidos;
  D8 fechada como definitiva (sem superfície online, item 9 do backlog v2 descartado);
  `delete-db-member` implementado, testado offline e com régua real (`failed: 0`); bug de raiz no
  `ResolveSection` (struct esvaziado deixava de ser navegável); README atualizado (69 verbos, IP em
  seção própria no topo, alegação de versão estreitada pra V21); `docs/BENCHMARKS.md` novo.
- In progress: nada mid-flight.

## Decisions (and why)
- **Gravação de tela fica pra depois** (decisão do user nesta virada). O que entra agora é o teste
  cego; o vídeo vira subproduto dele, com roteiro já validado.
- **Baseline manual não será refeito por inteiro** — amostra de **um** pacote/instrumento
  cronometrada à mão, reportada por unidade, nunca extrapolada. Tabela em `docs/BENCHMARKS.md`
  espera esses números; extrapolar mataria a credibilidade do benchmark.
- **Alegação de versão estreitada de propósito**: era "V19+", virou "exercised against V21 only".
  V19/V20 nunca rodaram ponta a ponta.
- **`-Check` não reprova mais checkout fora de `~/.claude/skills/tia`** — o `init.ps1` só avisa e
  instala; ter os dois discordando fazia quem clonava pra avaliar instalar com sucesso e ouvir
  "init incompleto". Lugar do checkout virou estado vivo, não gate (agora são 8 gates).
- **Caminho até a Siemens é local, não HQ** — representante Siemens DI Brasil via relacionamento de
  integrador. Enquadrar como complemento do TIA Portal, não concorrente. Duas perguntas a esperar:
  redistribuição de DLL (respondida no README) e uso da marca no nome `tia-cli` (pode virar pedido
  de renomear).

## Next steps (ordered)
1. **Escrever o caderno de especificação fictício** — como se um cliente tivesse jogado documentos
   na mesa: máquina com função clara, CPU S7-1500 com MLFB real, lista de sensores/atuadores,
   tabela de I/O (~20-30 pontos), narrativa de controle (intertravamentos, modos, alarmes). Vale
   escolher uma máquina que **não** seja resolvida só com `install-lib`, senão o teste vira
   demonstração da biblioteca. Salvar em `docs/teste-cego/` (fictício, pode ir pro Git).
2. **Definir os critérios de aprovação ANTES de rodar** — compile Success/0, hardware presente,
   tags endereçadas, blocos na lei de pastas, `audit` limpo. Escrever junto com o caderno.
3. **Rodar cego**: `/clear`, sessão nova recebe só os documentos + "se vira". **Quem escreveu o
   caderno não pode ser quem executa** — senão é corrigir a própria prova.
4. **Registrar cada tropeço** — onde a sessão travou, o que teve que adivinhar, que verbo faltou.
   Os tropeços são o produto do teste, mais que o resultado final.
5. Depois: números manuais (item 1 do BENCHMARKS) e gravação.

## Key files
- `docs/BENCHMARKS.md` — números medidos + tabela de baseline manual em branco esperando cronômetro.
- `README.md` — seção "What this project does not touch" (IP) e Requirements (claim de versão).
- `docs/PLANO.md` — "D8 fechada", "delete-db-member", "Gate de máquina limpa exercitado" (todas de
  2026-08-07); "Fronteira da engine" define o que a engine **não** vai virar.
- `docs/VERBS.md` — 69 assinaturas; é o que a sessão cega vai ler pra se orientar.
- `scripts/__navi__.md`, `src/__navi__.md` — atualizados.
- `SKILL.md` — o que a sessão cega recebe automaticamente; se ela travar, o defeito provavelmente
  está aqui.

## Open / blockers
- Nada bloqueia o passo 1 (escrever caderno é offline, não precisa do Portal).
- O passo 3 precisa de projeto TIA novo ou vazio, e de confirmar com o user antes de escrever.
- `rebuild.ps1` com o Portal aberto reabre o modal de autorização Openness: chamada pendurada com
  CPU ~0 = alguém precisa clicar.

## Skills
- tia
- ponytail
- caveman

## Effort
**Médio** para o passo 1 — não é código, é projeto de experimento: o caderno tem que ser realista o
bastante pra valer e enxuto o bastante pra caber numa sessão, e os critérios de aprovação precisam
ser falseáveis antes de a prova rodar. Raciocínio é o gargalo aqui, não o relógio (nada de Portal
envolvido). Sobe pra **alto** só se decidir também a arquitetura de controle da máquina em vez de
só especificá-la. O passo 3, quando chegar, é **alto** por natureza — é a prova.
