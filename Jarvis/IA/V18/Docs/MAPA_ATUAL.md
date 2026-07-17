# Mapa Atual V18

Projeto validado:

- `Automação ETE Saco Grande RevIHM`

PLC validado:

- `CPU CCO`

HMIs encontradas:

- `HMI_RT_1` no device `HMI_CCM1 R`
- `HMI_RT_2` no device `HMI_CCM2 R`
- `HMI_RT_3` no device `HMI_CCM3 R`
- `HMI_RT_5` no device `HMI_CCO R`

Areas ja validadas por Openness:

- identificacao do projeto e do PLC
- leitura do estado da sessao Multiuser (`IsUptoDate`)
- arvore de `Program blocks`
- criacao de pasta em `PLC tags`
- criacao de tabela e tags de teste em `PLC tags`
- inventario de devices
- leitura de telas da `HMI_RT_1`

Pastas principais de `Program blocks` confirmadas:

- `0. Main`
- `1. FB Bilbiotecas`
- `2. Fluxo de Controle`
- `3. Alarmes/Eventos/Falhas`
- `4. Motores/Bombas`
- `5. Instrumentação / Atuadores`
- `6. Comm Serial 485`
- `7. Comm Skids`
- `8. Compartilhamento`
- `9. Comm Supervisório`

Telas lidas na `HMI_RT_1`:

- `1.Telas Menu`
- `2.Telas Detalhamento`
- `3. Telas Aferições Sensores`
- `4. Telas Aferição de Inversores`
- `5. Graficos`

Observacoes:

- Em 12/03/2026, a validacao do utilitario `tia-check-session-status` retornou `IS_UP_TO_DATE=True`.
- Em 12/03/2026, o utilitario `tia-create-test-tags` criou a pasta `teste_ia`, a tabela `teste_ia_basicas` e as tags `Jarvis_Test_Bool`, `Jarvis_Test_Word` e `Jarvis_Test_Int` no PLC `CPU CCO`.
- O script de leitura de telas da `HMI_RT_1` esta validado.
- Em 12/03/2026, a tela playground `TEST` da `HMI_RT_1` foi alterada via exportacao XML e reimportacao com `ImportOptions.Override`.
- O utilitario `tia-import-hmi-screen.exe` foi criado para reimportar telas HMI no mesmo folder da tela original.
- A tela `TEST` agora contem o titulo `PLAYGROUND JARVIS`, um bloco principal redimensionado e um botao `REABRIR PLAYGROUND` para testes visuais.
- Em 12/03/2026, a tela `TEST` foi reorganizada como uma visao geral de playground, com cabecalho superior, subtitulo explicativo, cartao central de testes e melhor distribuicao visual do objeto grafico.
- Em 12/03/2026, a tela `1.Visao Geral` da `HMI_RT_1` foi exportada e usada como referencia para reorganizar a `TEST`.
- A `TEST` agora possui um layout resumido inspirado na visao geral real, com um bloco central grafico e dois `IOField` ligados a tags reais de `FQIT-01` e `FQIT-02`.
- Em 12/03/2026, a `TEST` tambem foi sobrescrita como copia direta da tela `1.Visao Geral`, mantendo apenas `Name=TEST` e `Number=2` para importacao por `Override`.
- Em 12/03/2026, a tela `TEST` recebeu normalizacao automatica de alinhamento horizontal (`Left`) para objetos do mesmo tipo com deslocamentos pequenos.
- Na mesma etapa, as geometrias de `Line` da `TEST` foram recalculadas (`Left`, `Top`, `Width`, `Height`, `StartLeft`, `EndLeft`) para manter consistencia na importacao do TIA.
- O inventario de Data Blocks ainda precisa refinamento antes de ser promovido a script padrao.
- A base futura para V19 deve reutilizar a mesma organizacao e o mesmo fluxo de validacao.
