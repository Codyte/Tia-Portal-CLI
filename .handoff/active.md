# Handoff · TIA Portal Openness API · 2026-07-20 (publicação GitHub em andamento)

## Goal
Publicar `tia-cli` no GitHub como repo público, **sem expor** `Scripts_Siemens/`
(autoria/segredo do user) nem no working tree nem no histórico git.

## State
- HEAD: b905ed5 (tree limpo, tudo commitado).
- Done:
  - F3.6 (macros prep-project/raio-x/clone-hw + gen-all.json) ✅ commitado e smoked.
  - F4 (LICENSE MIT + README EN completo, chamativo com badges/mermaid/details) ✅ commitado.
  - `gh auth login` feito nesta sessão (conta Codyte, escopo repo/gist/read:org).
  - `Scripts_Siemens/` removido do tracking (`git rm --cached`) + adicionado ao `.gitignore`,
    commitado em b905ed5. **Working tree local ainda tem os arquivos** (rm --cached não apaga
    disco) — só saiu do git.
- In progress: **histórico git ainda contém `Scripts_Siemens/` em ~20 commits antigos**
  (132 arquivos). `git rm --cached` só afeta daqui pra frente — precisa `git filter-repo`
  pra escrubar de verdade antes de tornar o repo público. `git-filter-repo` **não estava
  instalado** (`pip show` não achou); último passo antes da interrupção foi checar isso.

## Decisions (and why)
- Repo remoto já existe: `github.com/Codyte/TIA-Portal` (privado, histórico compatível —
  fast-forward puro, sem divergência). Decisão do user: **renomear pra `tia-cli` e tornar
  público** (não criar repo novo).
- `Scripts_Siemens/` (scripts originais FINAIS/OLD, em PT, citam cliente ETE SG) — decisão
  explícita do user: **excluir do público, é segredo/autoria dele**. Isso implica rewrite de
  histórico, não só gitignore (achado desta sessão, comunicado ao user antes de agir).
- README "chamativo": badges shields.io + hero centrado + diagrama mermaid + tabela de verbos
  + `<details>` colapsáveis pra seções longas (requirements/gates/macros/limitações).

## Next steps (ordered) — RETOMAR AQUI
1. **Instalar `git-filter-repo`**: `pip install git-filter-repo` (ou `pipx install`).
2. **Rewrite de histórico** (remove `Scripts_Siemens/` de TODOS os commits, não só do HEAD):
   ```
   git filter-repo --path Scripts_Siemens --invert-paths --force
   ```
   Roda no repo local `c:\Scripts\TIA Portal`. **Atenção**: `filter-repo` reescreve remotes
   (remove `origin` por segurança, padrão da ferramenta) — precisa re-adicionar
   `git remote add origin https://github.com/Codyte/TIA-Portal.git` depois.
   Verificar depois: `git log --all --oneline -- Scripts_Siemens` deve vir vazio.
3. **Renomear repo remoto**: `gh repo rename tia-cli --repo Codyte/TIA-Portal` (ou via
   `gh api -X PATCH repos/Codyte/TIA-Portal -f name=tia-cli`).
4. **Tornar público**: `gh repo edit Codyte/tia-cli --visibility public --accept-visibility-change-consequences`.
5. **Push** (histórico reescrito = hashes mudaram, mas remoto ainda não tem o rewrite):
   `git push origin main --force` (force justificado: history rewrite deliberado, decidido
   com o user; repo sem outros colaboradores/clones conhecidos).
6. **Descrição + topics** (pedido do user, turno anterior):
   ```
   gh repo edit Codyte/tia-cli --description "JSON-in/JSON-out CLI for Siemens TIA Portal Openness API" \
     --add-topic tia-portal --add-topic siemens --add-topic openness --add-topic plc --add-topic cli
   ```
7. Confirmar pro user: URL final do repo, e que `Scripts_Siemens/` não está nem no working
   tree do repo público nem no histórico (rodar `git clone` fresco em pasta temp e conferir
   se quiser prova).
8. Atualizar PLANO.md: F4 nota final (repo publicado, URL) — linha de pendências já cobre
   isso, só trocar "pendente de ordem" por "publicado" + link.

## Key files
- `.gitignore:22` — `Scripts_Siemens/` adicionado.
- `README.md` — versão "chamativa" já commitada (6b03332 antes desta sessão, sem mudança
  de conteúdo nesta sessão, só o scrub de histórico pendente).
- `docs/PLANO.md` — seção "Pendências / decisões futuras": ainda diz "revisar se
  Scripts_Siemens vai junto" — atualizar depois do push (passo 8 acima).

## Open / blockers
- Nenhum blocker de decisão — todas as 4 perguntas (licença, nome, README, Scripts_Siemens)
  já foram respondidas pelo user nesta sessão. Só falta executar passos 1-8 acima.
- `git filter-repo` precisa estar instalável via pip nesta máquina (Python 3.12 em
  `AppData\Local\Programs\Python\Python312` — havia um shim em `Scripts\git-filter-repo` mas
  `pip show` não achou o pacote → possível instalação quebrada ou script solto sem registro
  pip; investigar antes de rodar, ou reinstalar via pip limpo).
