# Verbos do `tia` (gerado por `scripts/gen-verbs.ps1` — nao editar a mao)

`tia <verb> [--plc NAME] [--portal PROJETO|PID] [--apply]  (--portal obrigat├│rio se houver mais de um TIA Portal aberto)`

## session
- `open-project --file X.ap21 [--no-ui]`
- `create-project --dir D --name N [--no-ui]`
- `save-project`
- `close-project [--save]`

## read
- `info`
- `list-devices`
- `list-blocks [--folder A/B] [--type FB|FC|OB|GlobalDB|InstanceDB] [--count]  (sem filtro = ~500 blocos num projeto real; --count = s├│ o total por pasta)`
- `list-tags`
- `list-types`
- `tree [--out DIR]  ÔåÉ COMECE AQUI: outline do PLC inteiro (blocos + tabelas de tag + UDTs) em plc-navi.md, ~26 KB num projeto de 476 blocos (o mesmo em JSON: 117 KB)`
- `find --pattern P* [--kind block|table|tag|type]`
- `xref --name X  (bloco, tag, tabela ou UDT ÔåÆ o que ele usa)`
- `trace --equipment AG-01  (s├¡mbolos do equipamento + quem referencia; ~9s em projeto grande)`
- `list-hmi [--device X]  (WinCC Unified: telas + tag tables)`
- `export-block --name X [--out DIR]`
- `export-tags --table X [--out DIR]`
- `explain-block --name X | --file F.xml  (LAD/FBD ÔåÆ texto compacto; --file roda sem TIA)`
- `export-type --name X [--out DIR]`
- `free-memory [--bytes N] [--from B] [--count K]  (buracos livres na ├írea %M; length -1 = at├® o fim)`

## structure
- `create-folder --path A/B [--tags|--types] [--apply]`
- `delete-folder --path A/B [--tags|--types] [--apply]`
- `delete-block --name X [--apply]`
- `create-instance-db --name X --of FB [--folder A/B] [--apply]  (molde importado por XML chega sem iDB ÔåÆ 'Missing instance DB')`
- `move-block --name X | --pattern P* --folder A/B [--out DIR] [--apply]  (exportÔåÆdeleteÔåÆimport; o Openness n├úo move bloco)`
- `delete-type --name X [--apply]  (UDT)`
- `import-type --file F.xml [--apply]`
- `scaffold --manifest F.json [--replace OLD=NEW ...] [--apply] [--force]  (├írvore da lei + moldes num projeto novo; --replace troca no XML e nas pastas antes do import; "Cpu" no manifesto barra fam├¡lia errada, --force ignora)`

## hardware
- `add-device --mlfb "6ES7 ..." --name X [--station S] [--group G] [--apply]`
- `delete-device --name X [--apply]`
- `list-attrs --device X [--item I] [--like SUB]  (read-only: atributos e valores do device item)`
- `set-attr --device X [--item I] --name A --value V [--apply]  (qualquer atributo que o list-attrs mostrar; tipo vem do valor atual)`
- `plug-module --device X [--item I] [--type TID] [--name N] [--pos P] [--apply]  (sem --type: lista slots livres; com --type: canPlug e, com --apply, pluga)`
- `set-address --device X [--ip A.B.C.D] [--mask M] [--pn-name N] [--apply]`
- `connect-subnet --device X --subnet S [--io-system IO] [--apply]`
- `set-memory-bytes --device X [--system 1] [--clock 0] [--apply]  (habilita FirstScan/AlwaysTRUE/Clock_1Hz na CPU)`
- `export-cax [--out DIR]`
- `import-cax --file F.aml [--apply]`

## write
- `import-block --file F [--folder A/B] [--replace OLD=NEW ...] [--apply]`
- `import-source --file F.scl [--apply]`
- `import-ladder --file F.scl [--name N] [--folder A/B] [--apply]  (SCL subset ÔåÆ LAD; dry-run works without TIA)`
- `import-tags --file F [--folder A/B] [--apply]`
- `add-tag --table T --name N --type Bool --address %M10.0 [--comment C] [--apply]  (uma tag em tabela existente; endere├ºo livre em %M sai do free-memory)`
- `delete-tag --table T --name N [--apply]`
- `rename-block --name X --to NEW [--apply]  (bloco ou UDT; refs seguem, igual ao GUI)`
- `set-tag --table T --name N [--type T] [--address %M10.0] [--comment C] [--rename NEW] [--apply]  (s├│ o que for passado muda; --rename exige Openness V20+)`
- `clone --block N | --table T --replace OLD=NEW [--replace ...] [--at %M432.0] [--folder A/B] [--apply]`
- `add-db-member --db X --name M [--path A.B] [--type T | --like SIBLING] [--out DIR] [--apply]`
- `edit-db-member --db X --name M [--path A.B] [--type T] [--rename NEW] [--out DIR] [--apply]  (rename n├úo corrige quem referencia o membro)`
- `compile [--block X | --folder A/B] [--errors] [--apply]  (--errors = lista plana {where,message,count} em vez da ├írvore)`
- `diff-block --file F.xml [--name X]  (read-only, normalized compare)`
- `doctor [--verb V] [--config F]  (read-only preflight dos verbos geradores)`
- `audit [--plc N] [--max 50]  (read-only: projeto ├ù lei de nomenclatura do PADRAO)`
- `gen-profinet --config F [--apply]`
- `standardize-tags [--config F] [--apply]`
- `gen-fault-ob [--config F] [--out DIR] [--apply]`
- `replicate-fc --config F [--out DIR] [--apply] [--force]  (--force: sobrescreve pasta j├í populada)`
- `gen-alarm-fc [--config F] [--out DIR] [--apply]`
- `replicate-instruments --config F [--out DIR] [--apply]`

## library
- `list-library --file X.al19`
- `import-master-copy --file X.al19 --name M [--folder A/B] [--apply]`
- `add-master-copy --file X.al21 (--name BLOCO | --folder A/B) [--lib-folder L] [--apply]  (PLC ÔåÆ library; --folder = pasta inteira = pacote; substitui se j├í existir)`
- `delete-master-copy --file X.al21 --name M [--apply]`

## multiuser
- `list-server-projects --server HOST [--port N] [--http] [--keep-connection]  (read-only: projetos do TIA Project Server, lock e sess├Áes locais)`

## bulk
- `snapshot  (invent├írio completo: devices + blocos + tabelas + UDTs de todo PLC)`
- `find --pattern "*" --kind tag  (todas as tags)`
- `ÔåÆ sa├¡da na casa das centenas de KB (snapshot = 251 KB, find de tag = 821 KB num projeto real). SEMPRE com --out-file, depois grep no arquivo. N├úo ├® leitura de orienta├º├úo: pra isso ├® `tree``

## batch
- `run --script ops.json [--summary]  (JSON array de arg-arrays, uma sess├úo s├│; step que falha vira {ok:false,error} e o batch segue; exit 1 se algum falhou. --summary = s├│ {steps,failed,errors[]}, sem o resultado de cada step. --plc/--out-file do processo N├âO descem pros steps: cada step carrega os seus)`

## notas
write verbs are dry-run unless --apply; default --out is .\workspace\exports; --out-file F.json (qualquer verbo: JSON completo no arquivo, stdout s├│ {file,bytes,count,head} ÔÇö use em find/snapshot/list-*/xref, que d├úo centenas de KB); --retry N (busy, default 3) --timeout SEC; exit: 0 ok, 1 geral, 2 uso, 3 arquivo, 4 TIA, 5 timeout

