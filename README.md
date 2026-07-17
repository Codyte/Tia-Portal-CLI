# TIA Portal Openness API (V19+)

API/ferramentas para automação de projetos TIA Portal V19+ via Siemens Openness,
projetada para uso por agentes de IA (Claude e afins) e por engenheiros.

## Estrutura

| Pasta | Conteúdo |
|-------|----------|
| `src/` | Código .NET do projeto (lib core + ferramentas) — em construção |
| `docs/` | Documentação |
| `Scripts_Siemens/FINAIS/` | Scripts V19+ provados em campo — fonte de verdade para a migração |
| `Scripts_Siemens/OLD/` | Legado (iterações de teste e experimentos V18) — não usar |

## Requisitos

- TIA Portal V19+ com Openness habilitado (usuário no grupo `Siemens TIA Openness`)
- .NET Framework compatível com a `Siemens.Engineering.dll` do V19
- Projeto aberto em sessão Multiuser (os scripts fazem attach na instância em execução)

## Status

Privado, em reorganização. Publicação no GitHub planejada quando a lib core estiver estável.
