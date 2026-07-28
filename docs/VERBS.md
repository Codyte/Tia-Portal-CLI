# Verbos do `tia` (gerado por `scripts/gen-verbs.ps1` — nao editar a mao)

`tia <verb> [--plc NAME] [--apply]`

## session
- `open-project --file X.ap21 [--no-ui]`
- `create-project --dir D --name N [--no-ui]`
- `save-project`
- `close-project [--save]`

## read
- `info`
- `list-devices`
- `list-blocks [--folder A/B] [--type FB|FC|OB|GlobalDB|InstanceDB] [--count]  (sem filtro = ~500 blocos num projeto real; --count = só o total por pasta)`
- `list-tags`
- `list-types`
- `tree [--out DIR]  ← COMECE AQUI: outline do PLC inteiro (blocos + tabelas de tag + UDTs) em plc-navi.md, ~26 KB num projeto de 476 blocos (o mesmo em JSON: 117 KB)`
- `find --pattern P* [--kind block|table|tag|type]`
- `xref --name X  (bloco, tag, tabela ou UDT → o que ele usa)`
- `trace --equipment AG-01  (símbolos do equipamento + quem referencia; ~9s em projeto grande)`
- `list-hmi [--device X]  (WinCC Unified: telas + tag tables)`
- `export-block --name X [--out DIR]`
- `export-tags --table X [--out DIR]`
- `explain-block --name X | --file F.xml  (LAD/FBD → texto compacto; --file roda sem TIA)`
- `export-type --name X [--out DIR]`
- `free-memory [--bytes N] [--from B] [--count K]  (buracos livres na área %M; length -1 = até o fim)`

## structure
- `create-folder --path A/B [--tags|--types] [--apply]`
- `delete-folder --path A/B [--tags|--types] [--apply]`
- `delete-block --name X [--apply]`
- `move-block --name X | --pattern P* --folder A/B [--out DIR] [--apply]  (export→delete→import; o Openness não move bloco)`
- `delete-type --name X [--apply]  (UDT)`
- `import-type --file F.xml [--apply]`
- `scaffold --manifest F.json [--apply] [--force]  (árvore da lei + moldes num projeto novo)`

## hardware
- `add-device --mlfb "6ES7 ..." --name X [--station S] [--group G] [--apply]`
- `set-address --device X [--ip A.B.C.D] [--mask M] [--pn-name N] [--apply]`
- `connect-subnet --device X --subnet S [--io-system IO] [--apply]`
- `export-cax [--out DIR]`
- `import-cax --file F.aml [--apply]`

## write
- `import-block --file F [--folder A/B] [--apply]`
- `import-source --file F.scl [--apply]`
- `import-ladder --file F.scl [--name N] [--folder A/B] [--apply]  (SCL subset → LAD; dry-run works without TIA)`
- `import-tags --file F [--folder A/B] [--apply]`
- `clone --block N | --table T --replace OLD=NEW [--replace ...] [--at %M432.0] [--folder A/B] [--apply]`
- `add-db-member --db X --name M [--path A.B] [--type T | --like SIBLING] [--out DIR] [--apply]`
- `compile [--block X | --folder A/B] [--apply]`
- `diff-block --file F.xml [--name X]  (read-only, normalized compare)`
- `doctor [--verb V] [--config F]  (read-only preflight dos verbos geradores)`
- `audit [--plc N] [--max 50]  (read-only: projeto × lei de nomenclatura do PADRAO)`
- `gen-profinet --config F [--apply]`
- `standardize-tags [--config F] [--apply]`
- `gen-fault-ob [--config F] [--out DIR] [--apply]`
- `replicate-fc --config F [--out DIR] [--apply] [--force]  (--force: sobrescreve pasta já populada)`
- `gen-alarm-fc [--config F] [--out DIR] [--apply]`
- `replicate-instruments --config F [--out DIR] [--apply]`

## library
- `list-library --file X.al19`
- `import-master-copy --file X.al19 --name M [--folder A/B] [--apply]`

## multiuser
- `list-server-projects --server HOST [--port N] [--http] [--keep-connection]  (read-only: projetos do TIA Project Server, lock e sessões locais)`

## bulk
- `snapshot  (inventário completo: devices + blocos + tabelas + UDTs de todo PLC)`
- `find --pattern "*" --kind tag  (todas as tags)`
- `→ saída na casa das centenas de KB (snapshot = 251 KB, find de tag = 821 KB num projeto real). SEMPRE com --out-file, depois grep no arquivo. Não é leitura de orientação: pra isso é `tree``

## batch
- `run --script ops.json [--summary]  (JSON array de arg-arrays, uma sessão só; step que falha vira {ok:false,error} e o batch segue; exit 1 se algum falhou. --summary = só {steps,failed,errors[]}, sem o resultado de cada step)`

## notas
write verbs are dry-run unless --apply; default --out is .\workspace\exports; --out-file F.json (qualquer verbo: JSON completo no arquivo, stdout só {file,bytes,count,head} — use em find/snapshot/list-*/xref, que dão centenas de KB); --retry N (busy, default 3) --timeout SEC; exit: 0 ok, 1 geral, 2 uso, 3 arquivo, 4 TIA, 5 timeout

