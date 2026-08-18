// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L134   class Program
//   L136   .Main
//   L419   .ExitCodeFor
//   L433   .RunLadderDryRun
//   L443   .RunExplainFile
//   L451   .RunInterfaceFile
//   L465   .Run
//   L480   .SingleCall
//   L496   class Release
//   L507   .RunExclusive
//   L632   .ParseScript
//   L654   .DispatchWithRetry
//   L668   .IsBusy
//   L674   .Dispatch
//   L684   case "save-project"
//   L687   case "close-project"
//   L690   case "info"
//   L693   case "list-devices"
//   L696   case "list-blocks"
//   L700   case "list-tags"
//   L703   case "tree"
//   L707   case "list-types"
//   L710   case "find"
//   L714   case "snapshot"
//   L717   case "xref"
//   L720   case "trace"
//   L723   case "list-hmi"
//   L726   case "export-hmi-tags"
//   L730   case "import-hmi-tags"
//   L737   case "hmi-tree"
//   L740   case "export-screen"
//   L744   case "import-screen"
//   L751   case "delete-screen"
//   L756   case "list-screen-items"
//   L761   case "audit-screen"
//   L766   case "set-screen-items"
//   L774   case "copy-screen-items"
//   L782   case "list-motion"
//   L786   case "free-memory"
//   L792   case "export-block"
//   L795   case "explain-block"
//   L800   case "export-tags"
//   L803   case "list-interface"
//   L809   case "import-block"
//   L816   case "import-ladder"
//   L824   case "import-source"
//   L829   case "create-folder"
//   L834   case "delete-folder"
//   L839   case "delete-block"
//   L843   case "move-block"
//   L848   case "delete-type"
//   L852   case "export-type"
//   L855   case "import-type"
//   L859   case "scaffold"
//   L868   case "clone"
//   L875   case "add-call"
//   L882   case "delete-network"
//   L887   case "set-retain"
//   L892   case "add-db-member"
//   L898   case "import-tags"
//   L905   case "retrieve-library"
//   L910   case "create-library"
//   L914   case "list-library"
//   L917   case "import-master-copy"
//   L923   case "add-master-copy"
//   L929   case "create-instance-db"
//   L934   case "delete-master-copy"
//   L939   case "add-device"
//   L944   case "delete-device"
//   L948   case "add-tag"
//   L954   case "delete-tag"
//   L959   case "edit-db-member"
//   L965   case "delete-db-member"
//   L970   case "rename-block"
//   L975   case "set-tag"
//   L982   case "set-attr"
//   L988   case "list-attrs"
//   L992   case "plug-module"
//   L998   case "list-telegrams"
//   L1001  case "insert-telegram"
//   L1008  case "set-address"
//   L1014  case "set-io-address"
//   L1020  case "list-io-map"
//   L1024  case "set-memory-bytes"
//   L1030  case "connect-subnet"
//   L1035  case "export-cax"
//   L1038  case "import-cax"
//   L1042  case "compile"
//   L1057  case "sim-run"
//   L1066  case "diff-block"
//   L1070  case "audit"
//   L1075  case "doctor"
//   L1083  case "gen-profinet"
//   L1088  case "standardize-tags"
//   L1096  case "gen-fault-ob"
//   L1104  case "replicate-fc"
//   L1114  case "gen-alarm-fc"
//   L1124  case "replicate-instruments"
//   L1165  .ValidateOptions
//   L1195  .HasError
//   L1202  .OptionValue
//   L1208  .ParseInt
//   L1214  .ParseByte
//   L1221  .OptionValues
//   L1230  .WriteLock
//   L1235  .Require
//   L1267  .Print
//   L1283  .Sanitize
//   L1290  .WriteOut
//   L1312  .CountOf
//   L1334  .ResolveSiemensAssembly
//   L1353  .PlcSimProbeDirs
//   L1367  .SiemensProbeDirs
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Tia.Cli
{
    /// <summary>
    /// tia — TIA Portal Openness CLI (V19+).
    /// stdout = JSON result (or {"error": ...}); exit 0 = ok, 1 = error.
    /// Read verbs: info | list-devices | list-blocks [--plc NAME] | list-tags [--plc NAME]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveSiemensAssembly;
            try { ValidateOptions(args); }
            catch (ArgumentException ex)
            {
                Print(new Dictionary<string, object> { { "error", ex.Message }, { "exitCode", 2 } });
                return 2;
            }
            _outFile = OptionValue(args, "--out-file"); // antes do --help: vale pra toda saída, inclusive ele
            _full = Array.IndexOf(args, "--full") >= 0;
            _verb = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "out";
            // SAFE-09: o que `--force` apaga vai antes para workspace/recovery/<verbo>-<timestamp>/
            Core.Ops.Verb = _verb;
            Core.Ops.NoBackup = Array.IndexOf(args, "--no-backup") >= 0;
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v" || args[0] == "version"))
            {
                // Primeira linha de qualquer bug report: versão + qual Openness este exe vai carregar.
                var asm = typeof(Program).Assembly;
                var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var engineering = SiemensProbeDirs()
                    .FirstOrDefault(d => File.Exists(Path.Combine(d, "Siemens.Engineering.Base.dll"))
                                      || File.Exists(Path.Combine(d, "Siemens.Engineering.dll")));
                Print(new Dictionary<string, object>
                {
                    { "version", info != null ? info.InformationalVersion : asm.GetName().Version.ToString() },
                    { "exe", asm.Location },
                    { "engineeringDir", engineering ?? "(nenhuma instalação do Openness encontrada)" },
                });
                return 0;
            }
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                Print(new Dictionary<string, object>
                {
                    { "usage", "tia <verb> [--plc NAME] [--portal PROJETO|PID] [--apply]" +
                        "  (--portal obrigatório se houver mais de um TIA Portal aberto)" },
                    { "session", new[] { "open-project --file X.ap21 [--no-ui]",
                        "create-project --dir D --name N [--no-ui]",
                        "save-project", "close-project [--save]" } },
                    { "read", new[] { "info", "list-devices",
                        "list-blocks [--folder A/B] [--type FB|FC|OB|GlobalDB|InstanceDB] [--count]  "
                            + "(sem filtro = ~500 blocos num projeto real; --count = só o total por pasta)",
                        "list-tags [--table T]  (sem --table: uma linha por tabela; com --table: as tags dela)",
                        "list-types",
                        "tree [--out DIR]  ← COMECE AQUI: outline do PLC inteiro (blocos + tabelas de tag + UDTs) "
                            + "em plc-navi.md, ~26 KB num projeto de 476 blocos (o mesmo em JSON: 117 KB)",
                        "find --pattern P* [--kind block|table|tag|type|constant]  "
                            + "(constant = constantes de sistema e de usuário; é como se confere "
                            + "<drive>~PROFINET_interface~Standard_telegram_20 sem ler o compile)",
                        "xref --name X  (bloco, tag, tabela ou UDT → o que ele usa)",
                        "trace --equipment AG-01  (símbolos do equipamento + quem referencia; ~9s em projeto grande)",
                        "list-hmi [--device X]  (WinCC clássico e Unified: telas + tag tables; `api` diz qual)",
                        "export-hmi-tags --table \"Pasta/Tabela\" [--device X]  (SimaticML da tabela de tags "
                            + "da IHM; é onde aparece a conexão e a tag do PLC por trás de cada tag de tela)",
                        "import-hmi-tags --file F.xml [--device X] [--folder \"Pasta/Sub\"] [--replace OLD=NEW ...] [--apply]  "
                            + "(par do export-hmi-tags; --folder é caminho completo a partir da raiz de tags "
                            + "e o nome da tabela sai do XML)",
                        "hmi-tree  (outline de todas as IHMs → hmi-navi.md, agrupado por pasta; irmão do `tree`)",
                        "export-screen --screen \"Pasta/Sub/Tela\" [--device X]  (SimaticML da tela; "
                            + "só WinCC clássico — Unified não exporta tela)",
                        "import-screen --file F.xml [--device X] [--folder \"Pasta/Sub\"] [--replace OLD=NEW ...] [--apply]  "
                            + "(--folder é caminho completo a partir da raiz de telas, como no import-block; "
                            + "--replace troca texto no XML antes do import — é assim que se replica tela de área, "
                            + "porque a tela liga tag por NOME (TargetID=\"@OpenLink\"), sem ID a remapear)",
                        "delete-screen --screen \"Pasta/Sub/Tela\" [--device X] [--apply]  "
                            + "(par do import-screen; sem ele tela de smoke só sai pela GUI)",
                        "list-screen-items --screen \"Pasta/Sub/Tela\" [--device X] [--like P] [--group]  "
                            + "(um objeto por linha: nome, tipo, x, y, w, h, tag — 150 objetos cabem em 7 KB, "
                            + "contra 800 KB do XML da tela. --group agrega por equipamento lido do nome da tag "
                            + "e devolve a `region` de cada um, que é o recorte pronto p/ copy-screen-items; "
                            + "a coluna `group` diz de que Hmi.Screen.Group o objeto faz parte; "
                            + "o bbox é só dos objetos COM tag, então fundo e rótulo pedem alargar a região)",
                        "audit-screen [--screen \"Pasta/Sub/Tela\"] [--device X] [--max N]  "
                            + "(cruza a tag de cada objeto de tela com as tags da própria IHM: tag que "
                            + "não existe, e tag sem código de equipamento (o placeholder `tag1` do "
                            + "editor). Sem --screen varre TODA tela do device — um export por tela. "
                            + "Feitio do `audit`: checks com ok/findings/detail e `scanned`. Cruzar com "
                            + "a tag do PLC sai `skipped`: a tag de HMI clássica só expõe Name e o "
                            + "SimaticML da tabela traz só a Connection)",
                        "set-screen-items --screen \"Pasta/Sub/Tela\" [--set \"Nome:x=530,y=356\"] "
                            + "[--remove Nome] [--rename Velho=Novo] [--rename-from-tag] [--group NOME=x,y,w,h] "
                            + "[--device X] [--apply]  (todos repetíveis, um export e um import para N "
                            + "edições — import de tela custa 20-170 s. --set move/redimensiona (x,y,w,h em "
                            + "qualquer combinação); --remove apaga; --rename dá nome auto-descritivo no lugar "
                            + "do contador do editor (Switch_18 -> BF-01-EC-01_CMD_LIGA); --rename-from-tag faz "
                            + "isso na tela inteira, tirando o nome da própria tag a partir do 1º código de "
                            + "equipamento (objeto SEM tag fica com o nome do editor: batizar seria "
                            + "adivinhação; é idempotente e o que não dá vai p/ `skippedRename` com o motivo); "
                            + "--group embrulha num "
                            + "Hmi.Screen.Group os objetos INTEIRAMENTE contidos na região, sem mexer em "
                            + "geometria (coordenada de filho é absoluta). Ordem fixa: set, remove, rename, "
                            + "group. Nome ausente vai p/ `missing` e os outros seguem; nome repetido na tela "
                            + "é erro)",
                        "copy-screen-items --from-screen \"<molde>\" --region x,y,w,h --screen \"<destino>\" "
                            + "--at x,y [--replace BF-01=BF-05] [--device X] [--apply]  (estampa: copia os "
                            + "objetos INTEIRAMENTE contidos na região, deslocados, renumerando ID e "
                            + "desduplicando ObjectName. Não há catálogo de estampas no CLI — cada tela da casa "
                            + "tem seu dialeto, então o grupo sai da tela que serve de molde)",
                        "list-motion [--like X] [--params]  (objetos tecnológicos: eixo, came, cinemática — "
                            + "nome, tipo (TO_PositioningAxis...) e versão; --params traz os parâmetros, "
                            + "centenas por eixo. Read-only: o Openness não cria TO)",
                        "export-block --name X [--out DIR]", "export-tags --table X [--out DIR]",
                        "explain-block --name X | --file F.xml  (LAD/FBD → texto compacto; --file roda sem TIA)",
                        "list-interface [--folder A/B] [--name X] [--file F.xml] [--out DIR]  "
                            + "(assinatura Input/Output/InOut dos FB/FC da pasta numa chamada só — é o que se lê "
                            + "antes de escrever qualquer chamada; --file roda sem TIA)",
                        "export-type --name X [--out DIR]",
                        "free-memory [--bytes N] [--from B] [--count K]  (buracos livres na área %M; length -1 = até o fim)" } },
                    { "structure", new[] { "create-folder --path A/B [--path C/D ...] [--tags|--types] [--apply]  (repetir --path cria a árvore toda num attach; '\\/' é barra literal no nome da pasta: \"1. I\\/OS/QA-01\")",
                        "delete-folder --path A/B [--tags|--types] [--apply]",
                        "delete-block --name X [--apply]",
                        "create-instance-db --name X --of FB [--folder A/B] [--apply]" +
                        "  (molde importado por XML chega sem iDB → 'Missing instance DB')",
                        "move-block --name X | --pattern P* --folder A/B [--out DIR] [--apply]  "
                            + "(export→delete→import; o Openness não move bloco)",
                        "delete-type --name X [--apply]  (UDT)",
                        "import-type --file F.xml [--apply]",
                        "scaffold --manifest F.json [--replace OLD=NEW ...] [--apply] [--force]  "
                            + "(árvore da lei + moldes num projeto novo; --replace troca no XML e nas pastas antes do import; "
                            + "\"Cpu\" no manifesto barra família errada, --force ignora)" } },
                    { "hardware", new[] { "add-device --mlfb \"6ES7 ...\" --name X [--station S] [--group G] [--apply]",
                        "delete-device --name X [--apply]",
                        "list-attrs --device X [--item I] [--like SUB]  (read-only: atributos e valores do device item)",
                        "set-attr --device X [--item I] --name A --value V [--apply]  "
                            + "(qualquer atributo que o list-attrs mostrar; tipo vem do valor atual)",
                        "plug-module --device X [--item I] [--type TID] [--name N] [--pos P] [--apply]  "
                            + "(sem --type: lista slots livres; com --type: canPlug e, com --apply, pluga. "
                            + "Alvo de plug é o rack: --item Rack_0, não o device. MLFB sem versão "
                            + "devolve plugAs com a 1ª versão que o slot aceita)",
                        "list-telegrams --device X  (read-only: drive objects SINAMICS, telegramas de cada um "
                            + "e o endereço de cada telegrama — %IB/%QB, que não aparece em DeviceItem.Addresses)",
                        "insert-telegram --device X --number N [--type Main|Supplementary|Safety|Torque|Edge] "
                            + "[--item I] [--drive-object D] [--change] [--apply]  "
                            + "(--change troca o telegrama presente: G120 novo já vem com o 1)  "
                            + "(telegrama de drive NÃO é submódulo de catálogo — plug-module não coloca)",
                        "set-address --device X [--ip A.B.C.D] [--mask M] [--pn-name N] [--item X1] [--apply]  (device com mais de uma interface exige --item)",
                        "set-io-address --device X [--item I] [--io Input|Output] [--start N] [--apply]  "
                            + "(endereço inicial do módulo de I/O; não é atributo — set-attr não alcança, "
                            + "e o import-cax ignora. Sem --item: varre o device (sonda). Sem --start: só lista)",
                        "list-io-map [--device X] [--io Input|Output]  (read-only: todo endereço de I/O "
                            + "do projeto — device/item, %IB..%QB e o próximo byte livre por tipo; "
                            + "inclui o telegrama de drive SINAMICS, que não vive em DeviceItem.Addresses "
                            + "e sem isso deixava o nextFreeByte entregar byte já ocupado)",
                        "connect-subnet --device X --subnet S [--io-system IO] [--apply]",
                        "set-memory-bytes --device X [--system 1] [--clock 0] [--apply]  (habilita FirstScan/AlwaysTRUE/Clock_1Hz na CPU)",
                        "export-cax [--out DIR]", "import-cax --file F.aml [--apply]" } },
                    { "write", new[] { "import-block --file F [--folder A/B] [--replace OLD=NEW ...] [--apply]",
                        "import-source --file F.scl [--folder A/B] [--apply]  (bloco nasce na pasta, sem move-block; fonte só de TYPE vai pra pasta de UDT. KeepOnError: bloco inválido entra inconsistente em vez de derrubar o lote — compile depois pra ver o erro. Fonte com acento exige UTF-8 com BOM: sem BOM o dry-run recusa)",
                        "import-ladder --file F.scl [--name N] [--folder A/B] [--apply]  (SCL subset → LAD; dry-run works without TIA)",
                        "import-tags --file F [--folder A/B] [--replace OLD=NEW ...] [--apply]  "
                            + "(--replace reescreve o XML antes de importar — nome da tabela e das tags; "
                            + "tag de PLC é única no CPU, então derivar tabela de outra exige trocar todos os nomes)",
                        "add-tag --table T --name N --type Bool --address %M10.0 [--comment C] [--apply]  "
                            + "(uma tag em tabela existente; endereço livre em %M sai do free-memory)",
                        "delete-tag --table T --name N [--apply]",
                        "rename-block --name X --to NEW [--apply]  (bloco ou UDT; refs seguem, igual ao GUI)",
                        "set-tag --table T --name N [--type T] [--address %M10.0] [--comment C] [--rename NEW] [--apply]  "
                            + "(só o que for passado muda; --rename exige Openness V20+)",
                        "clone --block N | --table T --replace OLD=NEW [--replace ...] [--at %M432.0] [--folder A/B] "
                            + "[--with-instances] [--apply]  (--replace é troca de TEXTO no XML exportado: caminho de "
                            + "membro de DB lá é cadeia de <Component>, então troque um componente por vez e mantenha "
                            + "a mesma profundidade da origem. --with-instances cria os iDBs que o clone passa a "
                            + "referenciar; sem eles o compile morre em 'Missing instance DB')",
                        "add-call --block X --fb NOME [--inst iDB] [--param P=<tag|DB.caminho.membro|const>] "
                            + "[--after N] [--title T] [--comment C] [--out DIR] [--apply]  "
                            + "(rede LAD com a chamada, EN no powerrail; os pinos saem da interface do bloco chamado. "
                            + "--fb aceita o nome com ou sem o prefixo 'FB '/'FC '. "
                            + "--inst é exigido para FB e recusado para FC. --after 0 = primeira rede, omitido = no fim)",
                        "delete-network --block X --index N [--out DIR] [--apply]  (N é 1-based, a numeração do explain-block)",
                        "set-retain --block FB --member M [--off] [--out DIR] [--apply]  "
                            + "(Remanence na declaração do FB; o Openness recusa em iDB e o import-source não expressa)",
                        "add-db-member --db X --name M [--path A.B] [--type T | --like SIBLING] [--out DIR] [--apply]",
                        "edit-db-member --db X --name M [--path A.B] [--type T] [--rename NEW] [--out DIR] [--apply]  "
                            + "(rename não corrige quem referencia o membro)",
                        "delete-db-member --db X --name M [--path A.B] [--out DIR] [--apply]  "
                            + "(não corrige quem referencia o membro)",
                        "compile [--block X | --folder A/B] [--errors] [--apply]  (--errors = lista plana {where,message,count} em vez da árvore)",
                        "diff-block --file F.xml [--name X]  (read-only, normalized compare)",
                        "doctor [--verb V] [--config F]  (read-only preflight dos verbos geradores)",
                        "audit [--plc N] [--max 50] [--db \"DB GLOBAL\"]  (projeto × lei do PADRAO/BOAS-PRATICAS; o check R2 exporta a DB global para --out, o resto é read-only)",
                        "gen-profinet --config F [--apply]",
                        "standardize-tags [--config F] [--apply]",
                        "gen-fault-ob [--config F] [--out DIR] [--apply]",
                        "replicate-fc --config F [--template PASTA] [--target-folder PASTA] [--out DIR] [--apply] [--force]  (--template: molde de outra área; --target-folder: só escreve sob ela; --force: sobrescreve pasta populada)",
                        "gen-alarm-fc [--config F] [--area NOME]* [--out DIR] [--apply]",
                        "replicate-instruments --config F [--out DIR] [--apply]" } },
                    { "library", new[] { "list-library --file X.al19",
                        "retrieve-library --file X.zal19 [--dir D] [--upgrade] [--apply]" +
                        "  (dearquiva .zal1x → .al2x; é como se consome biblioteca oficial da Siemens " +
                        "(LGF 109479728, DriveLib 206539), que o SIOS entrega arquivada; " +
                        "--upgrade sobe a versão da library junto)",
                        "create-library --file X.al21 [--apply]" +
                        "  (library vazia; o Portal cria <pasta>/<nome>/<nome>.al21 — caminho real volta em \"path\")",
                        "import-master-copy --file X.al19 --name M [--folder A/B] [--apply] [--force]" +
                        "  (--force: apaga o de mesmo nome e recria — é como se atualiza pacote já instalado)",
                        "add-master-copy --file X.al21 (--name BLOCO | --folder A/B) [--lib-folder L] [--apply]" +
                        "  (PLC → library; --folder = pasta inteira = pacote; substitui se já existir)",
                        "delete-master-copy --file X.al21 --name M [--apply]" } },
                    { "multiuser", new[] { "list-server-projects --server HOST [--port N] [--http] [--keep-connection]" +
                        "  (read-only: projetos do TIA Project Server, lock e sessões locais)" } },
                    { "bulk", new[] { "snapshot  (inventário completo: devices + blocos + tabelas + UDTs de todo PLC)",
                        "find --pattern \"*\" --kind tag  (todas as tags)",
                        "→ saída na casa das centenas de KB (snapshot = 251 KB, find de tag = 821 KB num projeto " +
                        "real). SEMPRE com --out-file, depois grep no arquivo. Não é leitura de orientação: " +
                        "pra isso é `tree`" } },
                    { "batch", new[] { "run --script ops.json [--summary]  (JSON array de arg-arrays, uma sessão só; " +
                        "step que falha vira {ok:false,error} e o batch segue; exit 1 se algum falhou. " +
                        "--summary = só {steps,failed,ms,slowest[3],errors[]}, sem o resultado de cada step. " +
                        "Todo step traz `ms`, e o batch traz o total — é a medida de onde foi o tempo. " +
                        "--plc/--out-file do processo NÃO descem pros steps: cada step carrega os seus. " +
                        "Exige projeto JÁ aberto: o attach é 1x, antes do 1º step, então open-project/create-project " +
                        "(e list-server-projects, que roda sem projeto) não podem ser step — chamar antes, sozinhos)" } },
                    { "sim", new[] { "sim-run [--plc X] [--instance plc_1500_1] [--pc-interface PLCSIM] " +
                        "[--script sim.json] [--no-download] [--apply]  (PLC virtual do S7-PLCSIM Advanced: attach " +
                        "na instância ligada por 'pwsh scripts/sim-host.ps1 -Start' (ou pelo control panel), baixa " +
                        "o programa do projeto por Openness, roda os passos. Exige o PLCSIM CLÁSSICO FECHADO — ele " +
                        "toma o mesmo canal. So baixa em access point PLCSIM: nome fora disso e recusado antes do download " + "(--allow-physical libera; nunca ha download em CPU real). --no-download pula o download e roda os passos no programa que já está " +
                        "na instância (o download é ~80% do tempo). Passos do script: " +
                        "[\"write\",\"tag\",\"valor\"], [\"read\",\"tag\"], [\"wait\",\"ms\"], [\"run\"], [\"stop\"], " +
                        "[\"state\"], [\"tags\",\"filtro\"]; tag de DB vai com as aspas do Portal. " +
                        "Dry-run lista as instâncias registradas e as interfaces de PC do download)",
                        "sim-diag [--instance plc_1500_1] [--watch SEG]  (retrato da instância do PLCSIM " +
                        "Advanced: estado, modo, CPU, IP, licença, monitoração de ciclo, tag list. NÃO precisa de " +
                        "TIA Portal aberto nem de projeto — a API do PLCSIM é independente do Openness. " +
                        "--watch SEG assina os eventos e devolve o que MUDOU na janela (LED, estado operacional, " +
                        "falha de rack/estação); LED não tem getter na API, só evento, então sem --watch não há " +
                        "estado de LED)" } },
                    { "meta", new[] { "--version  (versão do CLI + qual instalação do Openness este exe carrega; " +
                        "é a 1ª linha de qualquer bug report)" } },
                    { "notes", "write verbs are dry-run unless --apply; default --out is .\\workspace\\exports; " +
                        "saída acima de 60k chars (TIA_MAX_STDOUT) derrama SOZINHA em workspace/auto-<verbo>.json e " +
                        "o stdout recebe o stub {file,bytes,count,head,autoSpill} — --full desliga e dumpa tudo no " +
                        "stdout (use em script que faz ConvertFrom-Json); " +
                        "--out-file F.json (qualquer verbo: JSON completo no arquivo escolhido, stdout só o stub); " +
                        "o que --force apaga é exportado antes para workspace/recovery/<verbo>-<timestamp>/ " +
                        "(caminho no campo recoveryDir; --no-backup apaga sem rede); " +
                        "--retry N (busy, default 3) --timeout SEC; exit: 0 ok, 1 geral, 2 uso, 3 arquivo, 4 TIA, 5 timeout" },
                });
                return args.Length == 0 ? 1 : 0;
            }
            try
            {
                // pure XML generation, no Siemens types — must not enter Run() or its JIT pulls the DLL
                if (args[0] == "import-ladder" && !args.Contains("--apply"))
                    return RunLadderDryRun(args);
                if (args[0] == "explain-block" && OptionValue(args, "--file") != null)
                    return RunExplainFile(args);
                if (args[0] == "list-interface" && OptionValue(args, "--file") != null)
                    return RunInterfaceFile(args);

                var timeout = OptionValue(args, "--timeout");
                if (timeout == null) return Run(args);
                // SAFE-03: o timeout abandona a chamada no meio (Task.Run + exit), sem cancelamento
                // nem rollback. Em leitura isso só custa o resultado; em escrita deixa o projeto em
                // estado desconhecido — import pela metade, bloco inconsistente.
                if (args.Contains("--apply"))
                    throw new ArgumentException("--timeout is not allowed with --apply: a timed-out write is "
                        + "abandoned mid-call, with no cancellation or rollback. Run the write without --timeout.");
                var task = System.Threading.Tasks.Task.Run(() => Run(args));
                if (!task.Wait(TimeSpan.FromSeconds(int.Parse(timeout))))
                {
                    _outFile = null; // erro sempre em stdout: quem chamou precisa ver, não caçar arquivo
                    Print(new Dictionary<string, object>
                        { { "error", "Timeout after " + timeout + "s." }, { "exitCode", 5 } });
                    return 5; // portal call may still be blocked; process exit abandons it
                }
                return task.Result;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                while (inner is AggregateException agg && agg.InnerException != null)
                    inner = agg.InnerException;
                int code = ExitCodeFor(inner);
                _outFile = null; // idem: erro nunca vai pro arquivo
                Print(new Dictionary<string, object>
                {
                    { "error", inner.Message },
                    { "type", inner.GetType().Name },
                    { "exitCode", code },
                });
                return code;
            }
        }

        /// <summary>1 general · 2 usage · 3 file missing · 4 TIA/Openness. Type-name match: no Siemens JIT load.</summary>
        private static int ExitCodeFor(Exception ex)
        {
            if (ex is ArgumentException) return 2;
            // API-06/08: número mal formado, JSON inválido e opção fora de faixa são erro de USO —
            // caíam no 1 genérico, indistinguíveis de falha do Portal
            if (ex is FormatException || ex is OverflowException || ex is JsonException) return 2;
            if (ex.Message.Contains("Siemens.Engineering")) return 4; // DLL missing = TIA env, not user file
            if (ex is FileNotFoundException || ex is DirectoryNotFoundException) return 3;
            var t = ex.GetType().FullName;
            if (t != null && t.StartsWith("Siemens.Engineering")) return 4;
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int RunLadderDryRun(string[] args)
        {
            var outDir = OptionValue(args, "--out") ?? Path.Combine("workspace", "exports");
            var dry = Core.LadConverter.Convert(Require(args, "--file"), OptionValue(args, "--name"), outDir);
            dry["applied"] = false;
            Print(dry);
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int RunExplainFile(string[] args)
        {
            Print(Core.BlockExplain.Explain(Require(args, "--file"),
                OptionValue(args, "--out") ?? Path.Combine("workspace", "exports")));
            return 0;
        }

        // núcleo puro do list-interface: XML na mão dispensa TIA (e o JIT dos tipos Siemens)
        private static int RunInterfaceFile(string[] args)
        {
            var file = Path.GetFullPath(Require(args, "--file"));
            if (!File.Exists(file)) throw new FileNotFoundException("XML not found: " + file);
            Print(new Dictionary<string, object>
            {
                { "count", 1 },
                { "blocks", new List<object> { Core.BlockInterface.Describe(XDocument.Load(file)) } },
            });
            return 0;
        }

        // Must not be inlined into Main: Siemens types may only be JITted after AssemblyResolve is hooked.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run(string[] args)
        {
            using (var single = SingleCall())
            {
                return RunExclusive(args);
            }
        }

        /// <summary>
        /// SAFE-07/D9: o Openness é single-session e duas chamadas simultâneas corrompem a sessão. O
        /// lock de arquivo do `_common.ps1` só cobre a rota da scheduled task — dois terminais na
        /// sessão interativa passavam direto. O mutex é por sessão de logon (`Local\`, que é o escopo
        /// que um usuário sem privilégio consegue criar sempre); a rota da task, que vive noutra
        /// sessão do Windows, continua serializada pelo lock de arquivo.
        /// </summary>
        private static IDisposable SingleCall()
        {
            var mutex = new System.Threading.Mutex(false, "tia-cli-single-call");
            bool held;
            try { held = mutex.WaitOne(TimeSpan.Zero); }
            catch (System.Threading.AbandonedMutexException) { held = true; } // dono morreu: o lock é nosso
            if (!held)
            {
                mutex.Dispose();
                throw new InvalidOperationException("Another tia call is running in this Windows session. "
                    + "Openness is single-session (D9): run one verb at a time, or batch them with "
                    + "'tia run --script ops.json'.");
            }
            return new Release(mutex);
        }

        private sealed class Release : IDisposable
        {
            private readonly System.Threading.Mutex _mutex;
            public Release(System.Threading.Mutex mutex) { _mutex = mutex; }
            public void Dispose()
            {
                try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
                _mutex.Dispose();
            }
        }

        private static int RunExclusive(string[] args)
        {
            // com mais de um portal aberto, escolhe qual (senão o attach falha alto)
            Core.TiaSession.PortalFilter = OptionValue(args, "--portal");

            // run before Attach: may start the portal themselves
            if (args[0] == "open-project")
            {
                var opened = Core.TiaSession.OpenProject(Require(args, "--file"), !args.Contains("--no-ui"));
                Print(opened);
                return HasError(opened) ? 1 : 0;
            }
            if (args[0] == "create-project")
            {
                var created = Core.TiaSession.CreateProject(Require(args, "--dir"), Require(args, "--name"),
                    !args.Contains("--no-ui"));
                Print(created);
                return HasError(created) ? 1 : 0;
            }

            // read-only no servidor: precisa de portal aberto, mas NÃO de projeto aberto
            if (args[0] == "list-server-projects")
            {
                var servers = Core.Multiuser.ListServerProjects(Require(args, "--server"),
                    int.Parse(OptionValue(args, "--port") ?? "0"),
                    args.Contains("--http"), args.Contains("--keep-connection"),
                    args.Contains("--apply"));
                Print(servers);
                return HasError(servers) ? 1 : 0;
            }

            // PLCSIM tem API própria, independente do Openness: diagnóstico da instância não precisa
            // de portal nem de projeto aberto — roda antes do attach e economiza os ~7 s dele.
            if (args[0] == "sim-diag")
            {
                var diag = Core.Sim.Diag(OptionValue(args, "--instance") ?? "plc_1500_1",
                    int.Parse(OptionValue(args, "--watch") ?? "0"));
                Print(diag);
                return HasError(diag) ? 1 : 0;
            }

            // script malformado = erro de uso: falha antes do attach (não custa os ~7s nem exige portal)
            var batch = args[0] == "run" ? ParseScript(Require(args, "--script")) : null;

            using (var session = Core.TiaSession.Attach())
            {
                // batch: list of verbs in one attach — [["list-blocks"],["compile","--apply"]]
                if (args[0] == "run")
                {
                    var steps = batch;
                    var results = new List<object>();
                    int failed = 0;
                    // Relógio por step: sem ele, medir uma rodada exige Measure-Command por fora e o
                    // número não sobrevive no resultado. Com ele, o batch diz sozinho onde foi o tempo
                    // (na FP-06, 20 dos 49 min foram compile e isso só se soube pelo relógio de parede).
                    var batchClock = System.Diagnostics.Stopwatch.StartNew();
                    foreach (var step in steps)
                    {
                        var entry = new Dictionary<string, object> { { "verb", step[0] } };
                        var stepClock = System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            var value = DispatchWithRetry(session, step, args);
                            // --out-file do step: cada leitura pesada do batch vai pro seu arquivo,
                            // e o resultado no batch fica sendo o stub (o do processo vale só p/ o batch todo)
                            var stepOut = OptionValue(step, "--out-file");
                            entry["result"] = stepOut == null ? value : WriteOut(value, stepOut);
                            // erro embutido no resultado (o verbo voltou normal) também é falha do step:
                            // sem isso o batch marcava ok:true e o exit ficava 0 — API-01/02/03
                            entry["ok"] = !HasError(value);
                            if (HasError(value)) { entry["error"] = ((IDictionary<string, object>)value)["error"]; failed++; }
                        }
                        catch (Exception ex)
                        {
                            // sem isso, a 1ª exceção aborta o batch e joga fora os resultados já obtidos —
                            // justo o caso em que o batch (attach 1x) mais compensa.
                            var inner = ex.InnerException ?? ex;
                            entry["ok"] = false;
                            entry["error"] = inner.Message;
                            entry["type"] = inner.GetType().Name;
                            failed++;
                        }
                        entry["ms"] = stepClock.ElapsedMilliseconds;
                        results.Add(entry);
                    }
                    batchClock.Stop();
                    // no --summary o tempo de cada step some junto com o resultado; os 3 mais caros
                    // são o que responde "onde foi o tempo" sem trazer o dump de volta.
                    var slowest = results.Cast<Dictionary<string, object>>()
                        .Select((e, i) => new Dictionary<string, object>
                            { { "step", i }, { "verb", e["verb"] }, { "ms", e["ms"] } })
                        .OrderByDescending(e => (long)e["ms"]).Take(3).ToList();
                    // --summary: 98 steps × resultado completo é o dump que estoura contexto do agente.
                    // Só o que muda decisão: contagem, e o erro dos que falharam.
                    if (args.Contains("--summary"))
                        Print(new Dictionary<string, object>
                        {
                            { "steps", results.Count },
                            { "failed", failed },
                            { "ms", batchClock.ElapsedMilliseconds },
                            { "slowest", slowest },
                            { "errors", results.Cast<Dictionary<string, object>>()
                                .Select((e, i) => new { e, i })
                                .Where(x => !(bool)x.e["ok"])
                                .Select(x => new Dictionary<string, object>
                                    { { "step", x.i }, { "verb", x.e["verb"] }, { "error", x.e["error"] } })
                                .ToList() },
                        });
                    else
                        Print(new Dictionary<string, object>
                            { { "steps", results.Count }, { "failed", failed },
                              { "ms", batchClock.ElapsedMilliseconds }, { "results", results } });
                    return failed > 0 ? 1 : 0;
                }
                var result = DispatchWithRetry(session, args, args);
                Print(result);
                return HasError(result) ? 1 : 0;
            }
        }

        /// <summary>
        /// Lê e valida o script do batch. Roda antes do Attach: erro de uso não deve custar sessão
        /// nem exigir portal aberto. open-project/create-project não podem ser step — o attach é 1x,
        /// antes do 1º step, então o batch não abre o projeto em que ele mesmo trabalha.
        /// </summary>
        private static List<string[]> ParseScript(string file, bool verbs = true)
        {
            var steps = JsonConvert.DeserializeObject<List<string[]>>(File.ReadAllText(file));
            if (steps == null || steps.Count == 0)
                throw new ArgumentException(
                    "Script must be a JSON array of arg arrays, e.g. [[\"list-blocks\"],[\"compile\",\"--apply\"]].");
            // sim-run reusa o formato, mas os passos são ops do PLC virtual (write/read/wait), não verbos
            if (!verbs) return steps;
            foreach (var step in steps)
                if (step == null || step.Length == 0 || step[0] == "run"
                    || step[0] == "open-project" || step[0] == "create-project"
                    || step[0] == "list-server-projects")
                    throw new ArgumentException(
                        "Each step must be a verb on an open project (not 'run'/'open-project'/'create-project'/"
                        + "'list-server-projects'): the batch attaches once, before the first step, so it cannot open "
                        + "or create the project it works on — and list-server-projects runs before the attach, "
                        + "without a project. Call those on their own first (or scripts/use-project.ps1), "
                        + "then run the batch.");
            return steps;
        }

        /// <summary>Retries on "portal busy" errors: --retry N (default 3, 0 disables), linear backoff.</summary>
        private static object DispatchWithRetry(Core.TiaSession session, string[] args, string[] rootArgs)
        {
            int retries = int.Parse(OptionValue(rootArgs, "--retry") ?? "3");
            for (int attempt = 0; ; attempt++)
            {
                try { return Dispatch(session, args); }
                catch (Exception ex) when (attempt < retries && IsBusy(ex))
                {
                    Console.Error.WriteLine("portal busy, retry " + (attempt + 1) + "/" + retries);
                    System.Threading.Thread.Sleep(2000 * (attempt + 1));
                }
            }
        }

        private static bool IsBusy(Exception ex)
        {
            var inner = ex.InnerException ?? ex;
            return inner.Message.IndexOf("busy", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static object Dispatch(Core.TiaSession session, string[] args)
        {
            string verb = args[0];
            string plcName = OptionValue(args, "--plc");
            string outDir = OptionValue(args, "--out") ?? Path.Combine("workspace", "exports");
            bool apply = args.Contains("--apply");
            {
                object result;
                switch (verb)
                {
                    case "save-project":
                        result = session.Save();
                        break;
                    case "close-project":
                        result = session.CloseProject(args.Contains("--save"));
                        break;
                    case "info":
                        result = Core.Inventory.Info(session);
                        break;
                    case "list-devices":
                        result = Core.Inventory.Devices(session);
                        break;
                    case "list-blocks":
                        result = Core.Inventory.Blocks(session.GetPlc(plcName), OptionValue(args, "--folder"),
                            OptionValue(args, "--type"), args.Contains("--count"));
                        break;
                    case "list-tags":
                        result = Core.Inventory.TagTables(session.GetPlc(plcName), OptionValue(args, "--table"));
                        break;
                    case "tree":
                        result = Core.Inventory.Tree(session.GetPlc(plcName),
                            Path.Combine(outDir, "plc-navi.md"));
                        break;
                    case "list-types":
                        result = Core.Inventory.Types(session.GetPlc(plcName));
                        break;
                    case "find":
                        result = Core.Inventory.Find(session.GetPlc(plcName),
                            Require(args, "--pattern"), OptionValue(args, "--kind"));
                        break;
                    case "snapshot":
                        result = Core.Inventory.Snapshot(session);
                        break;
                    case "xref":
                        result = Core.Inventory.Xref(session.GetPlc(plcName), Require(args, "--name"));
                        break;
                    case "trace":
                        result = Core.Inventory.Trace(session.GetPlc(plcName), Require(args, "--equipment"));
                        break;
                    case "list-hmi":
                        result = Core.Hmi.List(session, OptionValue(args, "--device"));
                        break;
                    case "export-hmi-tags":
                        result = Core.Hmi.ExportTagTable(session, OptionValue(args, "--device"),
                            Require(args, "--table"), outDir);
                        break;
                    case "import-hmi-tags":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hmi.ImportTagTable(session, OptionValue(args, "--device"),
                                Core.Clone.RewriteFile(Require(args, "--file"),
                                    Core.Clone.ParseReplaces(OptionValues(args, "--replace")), outDir),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "hmi-tree":
                        result = Core.Hmi.Tree(session, Path.Combine(outDir, "hmi-navi.md"));
                        break;
                    case "export-screen":
                        result = Core.Hmi.ExportScreen(session, OptionValue(args, "--device"),
                            Require(args, "--screen"), outDir);
                        break;
                    case "import-screen":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hmi.ImportScreen(session, OptionValue(args, "--device"),
                                Core.Clone.RewriteFile(Require(args, "--file"),
                                    Core.Clone.ParseReplaces(OptionValues(args, "--replace")), outDir),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "delete-screen":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hmi.DeleteScreen(session, OptionValue(args, "--device"),
                                Require(args, "--screen"), apply);
                        break;
                    case "list-screen-items":
                        result = Core.ScreenItems.List(session, OptionValue(args, "--device"),
                            Require(args, "--screen"), OptionValue(args, "--like"),
                            args.Contains("--group"), outDir);
                        break;
                    case "audit-screen":
                        result = Core.ScreenItems.Audit(session, OptionValue(args, "--device"),
                            OptionValue(args, "--screen"),
                            int.Parse(OptionValue(args, "--max") ?? "20"), outDir);
                        break;
                    case "set-screen-items":
                        using (WriteLock(session, apply, verb))
                            result = Core.ScreenItems.Set(session, OptionValue(args, "--device"),
                                Require(args, "--screen"), OptionValues(args, "--set"),
                                OptionValues(args, "--remove"), OptionValues(args, "--rename"),
                                OptionValues(args, "--group"), args.Contains("--rename-from-tag"),
                                apply, outDir);
                        break;
                    case "copy-screen-items":
                        using (WriteLock(session, apply, verb))
                            result = Core.ScreenItems.Copy(session, OptionValue(args, "--device"),
                                Require(args, "--from-screen"), Require(args, "--screen"),
                                Core.ScreenItems.Coords(Require(args, "--region"), 4, "--region"),
                                Core.ScreenItems.Coords(Require(args, "--at"), 2, "--at"),
                                OptionValues(args, "--replace"), apply, outDir);
                        break;
                    case "list-motion":
                        result = Core.Motion.List(session, session.GetPlc(plcName),
                            OptionValue(args, "--like"), args.Contains("--params"));
                        break;
                    case "free-memory":
                        result = Core.Memory.FreeM(session.GetPlc(plcName),
                            int.Parse(OptionValue(args, "--bytes") ?? "1"),
                            int.Parse(OptionValue(args, "--from") ?? "0"),
                            int.Parse(OptionValue(args, "--count") ?? "5"));
                        break;
                    case "export-block":
                        result = Core.Ops.ExportBlock(session.GetPlc(plcName), Require(args, "--name"), outDir);
                        break;
                    case "explain-block":
                        var xml = (string)((Dictionary<string, object>)Core.Ops.ExportBlock(
                            session.GetPlc(plcName), Require(args, "--name"), outDir))["file"];
                        result = Core.BlockExplain.Explain(xml, outDir);
                        break;
                    case "export-tags":
                        result = Core.Ops.ExportTagTable(session.GetPlc(plcName), Require(args, "--table"), outDir);
                        break;
                    case "list-interface":
                        var ifaceFile = OptionValue(args, "--file");
                        result = Core.BlockInterface.Run(
                            ifaceFile != null ? null : session.GetPlc(plcName),
                            OptionValue(args, "--name"), OptionValue(args, "--folder"), ifaceFile, outDir);
                        break;
                    case "import-block":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportBlock(session.GetPlc(plcName),
                                Core.Clone.RewriteFile(Require(args, "--file"),
                                    Core.Clone.ParseReplaces(OptionValues(args, "--replace")), outDir),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "import-ladder":
                        var lad = Core.LadConverter.Convert(Require(args, "--file"), OptionValue(args, "--name"), outDir);
                        using (WriteLock(session, apply, verb))
                            lad["import"] = Core.Ops.ImportBlock(session.GetPlc(plcName), (string)lad["xmlFile"],
                                OptionValue(args, "--folder"), apply);
                        lad["applied"] = apply;
                        result = lad;
                        break;
                    case "import-source":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportSource(session.GetPlc(plcName), Require(args, "--file"),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "create-folder":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.CreateFolders(session.GetPlc(plcName), OptionValues(args, "--path"),
                                args.Contains("--tags"), apply, args.Contains("--types"));
                        break;
                    case "delete-folder":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.DeleteFolder(session.GetPlc(plcName), Require(args, "--path"),
                                args.Contains("--tags"), apply, args.Contains("--types"));
                        break;
                    case "delete-block":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.DeleteBlock(session.GetPlc(plcName), Require(args, "--name"), apply);
                        break;
                    case "move-block":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.MoveBlock(session.GetPlc(plcName), OptionValue(args, "--name"),
                                OptionValue(args, "--pattern"), Require(args, "--folder"), outDir, apply);
                        break;
                    case "delete-type":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.DeleteType(session.GetPlc(plcName), Require(args, "--name"), apply);
                        break;
                    case "export-type":
                        result = Core.Ops.ExportType(session.GetPlc(plcName), Require(args, "--name"), outDir);
                        break;
                    case "import-type":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportType(session.GetPlc(plcName), Require(args, "--file"), apply);
                        break;
                    case "scaffold":
                        var manifestFile = Path.GetFullPath(Require(args, "--manifest"));
                        var manifest = JsonConvert.DeserializeObject<Core.ScaffoldManifest>(
                            File.ReadAllText(manifestFile));
                        using (WriteLock(session, apply, verb))
                            result = Core.Scaffold.Run(session, session.GetPlc(plcName), manifest,
                                Path.GetDirectoryName(manifestFile), apply, args.Contains("--force"),
                                Core.Clone.ParseReplaces(OptionValues(args, "--replace")), outDir);
                        break;
                    case "clone":
                        using (WriteLock(session, apply, verb))
                            result = Core.Clone.Run(session.GetPlc(plcName), OptionValue(args, "--block"),
                                OptionValue(args, "--table"), OptionValues(args, "--replace"),
                                OptionValue(args, "--at"), OptionValue(args, "--folder"), outDir, apply,
                                args.Contains("--with-instances"));
                        break;
                    case "add-call":
                        using (WriteLock(session, apply, verb))
                            result = Core.BlockEdit.AddCall(session.GetPlc(plcName), Require(args, "--block"),
                                Require(args, "--fb"), OptionValue(args, "--inst"), OptionValues(args, "--param"),
                                int.Parse(OptionValue(args, "--after") ?? "-1"), OptionValue(args, "--title"),
                                OptionValue(args, "--comment"), outDir, apply);
                        break;
                    case "delete-network":
                        using (WriteLock(session, apply, verb))
                            result = Core.BlockEdit.DeleteNetwork(session.GetPlc(plcName), Require(args, "--block"),
                                int.Parse(Require(args, "--index")), outDir, apply);
                        break;
                    case "set-retain":
                        using (WriteLock(session, apply, verb))
                            result = Core.BlockEdit.SetRetain(session.GetPlc(plcName), Require(args, "--block"),
                                Require(args, "--member"), !args.Contains("--off"), outDir, apply);
                        break;
                    case "add-db-member":
                        using (WriteLock(session, apply, verb))
                            result = Core.DbMember.Add(session.GetPlc(plcName), Require(args, "--db"),
                                OptionValue(args, "--path"), Require(args, "--name"),
                                OptionValue(args, "--type"), OptionValue(args, "--like"), outDir, apply);
                        break;
                    case "import-tags":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportTagTable(session.GetPlc(plcName),
                                Core.Clone.RewriteFile(Require(args, "--file"),
                                    Core.Clone.ParseReplaces(OptionValues(args, "--replace")), outDir),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "retrieve-library":
                        using (WriteLock(session, apply, verb))
                            result = Core.Library.Retrieve(session, Require(args, "--file"),
                                OptionValue(args, "--dir"), args.Contains("--upgrade"), apply);
                        break;
                    case "create-library":
                        using (WriteLock(session, apply, verb))
                            result = Core.Library.Create(session, Require(args, "--file"), apply);
                        break;
                    case "list-library":
                        result = Core.Library.List(session, Require(args, "--file"));
                        break;
                    case "import-master-copy":
                        using (WriteLock(session, apply, verb))
                            result = Core.Library.ImportMasterCopy(session, session.GetPlc(plcName),
                                Require(args, "--file"), Require(args, "--name"),
                                OptionValue(args, "--folder"), apply, args.Contains("--force"));
                        break;
                    case "add-master-copy":
                        using (WriteLock(session, apply, verb))
                            result = Core.Library.AddMasterCopy(session, session.GetPlc(plcName),
                                Require(args, "--file"), OptionValue(args, "--name"),
                                OptionValue(args, "--folder"), OptionValue(args, "--lib-folder"), apply);
                        break;
                    case "create-instance-db":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.CreateInstanceDb(session.GetPlc(plcName), Require(args, "--name"),
                                Require(args, "--of"), OptionValue(args, "--folder"), apply);
                        break;
                    case "delete-master-copy":
                        using (WriteLock(session, apply, verb))
                            result = Core.Library.DeleteMasterCopy(session, Require(args, "--file"),
                                Require(args, "--name"), apply);
                        break;
                    case "add-device":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.AddDevice(session, Require(args, "--mlfb"),
                                Require(args, "--name"), OptionValue(args, "--station"), OptionValue(args, "--group"), apply);
                        break;
                    case "delete-device":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.DeleteDevice(session, Require(args, "--name"), apply);
                        break;
                    case "add-tag":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.AddTag(session.GetPlc(plcName), Require(args, "--table"),
                                Require(args, "--name"), OptionValue(args, "--type"),
                                OptionValue(args, "--address"), OptionValue(args, "--comment"), apply);
                        break;
                    case "delete-tag":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.DeleteTag(session.GetPlc(plcName), Require(args, "--table"),
                                Require(args, "--name"), apply);
                        break;
                    case "edit-db-member":
                        using (WriteLock(session, apply, verb))
                            result = Core.DbMember.Change(session.GetPlc(plcName), Require(args, "--db"),
                                OptionValue(args, "--path"), Require(args, "--name"),
                                OptionValue(args, "--type"), OptionValue(args, "--rename"), outDir, apply);
                        break;
                    case "delete-db-member":
                        using (WriteLock(session, apply, verb))
                            result = Core.DbMember.Remove(session.GetPlc(plcName), Require(args, "--db"),
                                OptionValue(args, "--path"), Require(args, "--name"), outDir, apply);
                        break;
                    case "rename-block":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.Rename(session.GetPlc(plcName), Require(args, "--name"),
                                Require(args, "--to"), apply);
                        break;
                    case "set-tag":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.SetTag(session.GetPlc(plcName), Require(args, "--table"),
                                Require(args, "--name"), OptionValue(args, "--type"),
                                OptionValue(args, "--address"), OptionValue(args, "--comment"),
                                OptionValue(args, "--rename"), apply);
                        break;
                    case "set-attr":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.SetAttr(session, Require(args, "--device"),
                                OptionValue(args, "--item"), Require(args, "--name"),
                                Require(args, "--value"), apply);
                        break;
                    case "list-attrs":
                        result = Core.Hardware.ListAttrs(session, Require(args, "--device"),
                            OptionValue(args, "--item"), OptionValue(args, "--like"));
                        break;
                    case "plug-module":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.PlugModule(session, Require(args, "--device"),
                                OptionValue(args, "--item"), OptionValue(args, "--type"),
                                OptionValue(args, "--name"), ParseInt(OptionValue(args, "--pos")), apply);
                        break;
                    case "list-telegrams":
                        result = Core.Drives.ListTelegrams(session, Require(args, "--device"));
                        break;
                    case "insert-telegram":
                        using (WriteLock(session, apply, verb))
                            result = Core.Drives.InsertTelegram(session, Require(args, "--device"),
                                OptionValue(args, "--item"), int.Parse(Require(args, "--number")),
                                OptionValue(args, "--type"), ParseInt(OptionValue(args, "--drive-object")),
                                args.Contains("--change"), apply);
                        break;
                    case "set-address":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.SetAddress(session, Require(args, "--device"),
                                OptionValue(args, "--ip"), OptionValue(args, "--mask"),
                                OptionValue(args, "--pn-name"), apply, OptionValue(args, "--item"));
                        break;
                    case "set-io-address":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.SetIoAddress(session, Require(args, "--device"),
                                OptionValue(args, "--item"), OptionValue(args, "--io"),
                                ParseInt(OptionValue(args, "--start")), apply);
                        break;
                    case "list-io-map":
                        result = Core.Hardware.ListIoMap(session, OptionValue(args, "--device"),
                            OptionValue(args, "--io"));
                        break;
                    case "set-memory-bytes":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.SetMemoryBytes(session, Require(args, "--device"),
                                ParseByte(OptionValue(args, "--system")), ParseByte(OptionValue(args, "--clock")),
                                apply);
                        break;
                    case "connect-subnet":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.ConnectSubnet(session, Require(args, "--device"),
                                Require(args, "--subnet"), OptionValue(args, "--io-system"), apply);
                        break;
                    case "export-cax":
                        result = Core.Hardware.CaxExport(session, outDir);
                        break;
                    case "import-cax":
                        // Openness: CaxProvider.Import não é suportado dentro de ExclusiveAccess
                        result = Core.Hardware.CaxImport(session, Require(args, "--file"), apply);
                        break;
                    case "compile":
                        var plc = session.GetPlc(plcName);
                        var scopeBlock = OptionValue(args, "--block");
                        var scopeFolder = OptionValue(args, "--folder");
                        if (apply)
                            using (WriteLock(session, true, verb))
                                result = Core.Ops.Compile(plc, scopeBlock, scopeFolder,
                                    args.Contains("--errors"));
                        else
                            result = new Dictionary<string, object>
                            {
                                { "wouldCompile", scopeBlock ?? scopeFolder ?? plc.Name },
                                { "applied", false },
                            };
                        break;
                    case "sim-run":
                        result = Core.Sim.Run(session, session.GetPlc(plcName),
                            OptionValue(args, "--instance") ?? "plc_1500_1",
                            OptionValue(args, "--pc-interface") ?? "PLCSIM",
                            OptionValue(args, "--script") == null
                                ? new List<string[]>()
                                : ParseScript(OptionValue(args, "--script"), false),
                            apply, args.Contains("--no-download"), args.Contains("--allow-physical"));
                        break;
                    case "diff-block":
                        result = Core.Ops.DiffBlock(session.GetPlc(plcName),
                            OptionValue(args, "--name"), Require(args, "--file"));
                        break;
                    case "audit":
                        result = Core.Audit.Run(session.GetPlc(plcName),
                            int.Parse(OptionValue(args, "--max") ?? "50"),
                            outDir, OptionValue(args, "--db"));
                        break;
                    case "doctor":
                        var docVerb = OptionValue(args, "--verb");
                        var docConfig = OptionValue(args, "--config");
                        if (docConfig != null && docVerb == null)
                            throw new ArgumentException("doctor --config requires --verb (configs are per-verb).");
                        result = Core.Doctor.Run(session, session.GetPlc(plcName), docVerb, docConfig,
                            (path, type) => ConfigJson.Read(path, type));
                        break;
                    case "gen-profinet":
                        var config = ConfigJson.Read<Core.ProfinetConfig>(Require(args, "--config"));
                        using (WriteLock(session, apply, verb))
                            result = Core.Profinet.Generate(session, session.GetPlc(plcName), config, apply);
                        break;
                    case "standardize-tags":
                        var stdPath = OptionValue(args, "--config");
                        var stdConfig = stdPath != null
                            ? ConfigJson.Read<Core.StandardizeConfig>(stdPath)
                            : new Core.StandardizeConfig();
                        using (WriteLock(session, apply, verb))
                            result = Core.Standardize.Run(session.GetPlc(plcName), stdConfig, apply);
                        break;
                    case "gen-fault-ob":
                        var fobPath = OptionValue(args, "--config");
                        var fobConfig = fobPath != null
                            ? ConfigJson.Read<Core.FaultObConfig>(fobPath)
                            : new Core.FaultObConfig();
                        using (WriteLock(session, apply, verb))
                            result = Core.FaultOb.Generate(session, session.GetPlc(plcName), fobConfig, outDir, apply);
                        break;
                    case "replicate-fc":
                        var repConfig = ConfigJson.Read<Core.ReplicateFcConfig>(Require(args, "--config"));
                        // molde de outra área + escopo: é o que dispensa derivar o acionamento-semente
                        // no braço quando a área nova não tem irmã populada (FP-06, §7)
                        repConfig.TemplateFolder = OptionValue(args, "--template") ?? repConfig.TemplateFolder;
                        repConfig.TargetFolder = OptionValue(args, "--target-folder") ?? repConfig.TargetFolder;
                        using (WriteLock(session, apply, verb))
                            result = Core.ReplicateFc.Run(session.GetPlc(plcName), repConfig, outDir, apply,
                                args.Contains("--force"));
                        break;
                    case "gen-alarm-fc":
                        var almPath = OptionValue(args, "--config");
                        var almConfig = almPath != null
                            ? ConfigJson.Read<Core.AlarmFcConfig>(almPath)
                            : new Core.AlarmFcConfig();
                        // escopo: sem --area, gerar 1 área regenerava as 19 existentes (FP-06, T6)
                        almConfig.IncludeFolders.AddRange(OptionValues(args, "--area"));
                        using (WriteLock(session, apply, verb))
                            result = Core.AlarmFc.Generate(session, session.GetPlc(plcName), almConfig, outDir, apply);
                        break;
                    case "replicate-instruments":
                        var insConfig = ConfigJson.Read<Core.InstrumentFcConfig>(Require(args, "--config"));
                        using (WriteLock(session, apply, verb))
                            result = Core.InstrumentFc.Run(session, session.GetPlc(plcName), insConfig, outDir, apply);
                        break;
                    default:
                        throw new ArgumentException("Unknown verb '" + verb + "'. Run tia --help.");
                }
                return result;
            }
        }

        /// <summary>
        /// Toda opção que algum verbo lê. Fonte da verdade do SAFE-04: opção fora desta lista é typo,
        /// e typo de escopo (`--ara` por `--area`) junto de `--apply` roda o gerador no projeto todo.
        /// O teste `unknown-option-guard` compara esta lista com os literais `--x` dos fontes: opção
        /// nova sem entrada aqui reprova offline, antes de chegar num projeto.
        /// </summary>
        private static readonly HashSet<string> KnownOptions = new HashSet<string>(StringComparer.Ordinal)
        {
            "--address", "--after", "--allow-physical", "--apply", "--area", "--at", "--block", "--bytes",
            "--change", "--clock", "--comment", "--config", "--count", "--db", "--device", "--dir",
            "--drive-object", "--equipment", "--errors", "--fb", "--file", "--folder", "--force", "--from",
            "--from-screen", "--full", "--group", "--help", "--http", "--index", "--inst", "--instance",
            "--io", "--io-system", "--ip", "--item", "--keep-connection", "--kind", "--lib-folder", "--like",
            "--manifest", "--mask", "--max", "--member", "--mlfb", "--name", "--no-backup", "--no-download", "--no-ui",
            "--number", "--of", "--off", "--out", "--out-file", "--param", "--params", "--path", "--pattern",
            "--pc-interface", "--plc", "--pn-name", "--port", "--portal", "--pos", "--region", "--remove",
            "--rename", "--rename-from-tag", "--replace", "--retry", "--save", "--screen", "--script",
            "--server", "--set", "--start", "--station", "--subnet", "--summary", "--system", "--table",
            "--tags", "--target-folder", "--template", "--timeout", "--title", "--to", "--type", "--types",
            "--upgrade", "--value", "--verb", "--version", "--watch", "--with-instances",
            "-h", "-v",
        };

        /// <summary>
        /// SAFE-04: o parser lê a opção que conhece e ignora o resto, então `--ara X --apply` perdia o
        /// escopo em silêncio. Aqui um token `--x` fora de <see cref="KnownOptions"/> é erro de uso
        /// (exit 2) antes do attach — não custa portal nem toca no projeto. Valor de opção nunca é
        /// examinado: só o token seguinte a um `--conhecido` é pulado.
        /// </summary>
        private static void ValidateOptions(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var token = args[i];
                if (!token.StartsWith("-") || token == "-") continue;
                if (!KnownOptions.Contains(token))
                {
                    // sugestão por prefixo: `--ara` → `--area`, `--aply` → `--apply`. Busca por
                    // substring devolvia `--param` para `--ara`, que não ajuda ninguém.
                    var stem = token.TrimStart('-');
                    var near = KnownOptions
                        .Where(k => stem.Length >= 2 && k.TrimStart('-').StartsWith(stem.Substring(0, 2),
                            StringComparison.OrdinalIgnoreCase))
                        .OrderBy(k => Math.Abs(k.Length - token.Length)).Take(3).ToList();
                    throw new ArgumentException("Unknown option '" + token + "'."
                        + (near.Count > 0 ? " Did you mean " + string.Join(", ", near) + "?" : "")
                        + " Run tia --help.");
                }
                // pula o valor da opção, que não é validado — mas só quando ele não parece outra
                // opção: senão `--apply --ara X` engoliria justo o typo que este guard existe p/ pegar
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) i++;
            }
        }

        /// <summary>
        /// API-01/02/03: verbo que embute a falha num campo `error` do resultado e volta normalmente
        /// fazia o processo sair 0 e o batch marcar o step `ok:true` — falso sucesso. Erro de topo
        /// vira exit 1; campos `error` de item (dentro de listas) continuam sendo diagnóstico parcial.
        /// </summary>
        private static bool HasError(object result)
        {
            var dict = result as IDictionary<string, object>;
            object error;
            return dict != null && dict.TryGetValue("error", out error) && error != null;
        }

        private static string OptionValue(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static int? ParseInt(string value)
        {
            return string.IsNullOrEmpty(value) ? (int?)null : int.Parse(value);
        }

        /// <summary>"1" ou "%MB1" → 1; ausente → null (atributo não é tocado).</summary>
        private static int? ParseByte(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return int.Parse(value.TrimStart('%').TrimStart('M', 'm').TrimStart('B', 'b'));
        }

        /// <summary>Todas as ocorrências de uma opção repetível (ex.: --replace A=B --replace C=D).</summary>
        private static List<string> OptionValues(string[] args, string name)
        {
            var values = new List<string>();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) values.Add(args[i + 1]);
            return values;
        }

        /// <summary>Multiuser ExclusiveAccess for applied writes; no-op handle on dry-run.</summary>
        private static IDisposable WriteLock(Core.TiaSession session, bool apply, string verb)
        {
            return apply ? session.ExclusiveAccess("tia " + verb) : null;
        }

        private static string Require(string[] args, string name)
        {
            var v = OptionValue(args, name);
            if (v == null) throw new ArgumentException("Missing required option " + name + ".");
            return v;
        }

        /// <summary>
        /// --out-file: JSON completo vai pro arquivo, stdout recebe só {file,bytes,count,head}.
        /// Um `find --pattern *` num projeto real são 821 KB — na sessão de um agente isso é a
        /// sessão inteira. Guarda no único ponto por onde todo verbo sai; sem a opção, nada muda
        /// (raio-x.ps1 e afins seguem redirecionando stdout).
        /// </summary>
        private static string _outFile;
        private static bool _full;
        private static string _verb = "out";

        /// <summary>
        /// Teto do stdout em chars. Acima dele, sem --out-file e sem --full, a saída derrama
        /// sozinha pra workspace/auto-&lt;verbo&gt;.json. 60k fica acima do `tree` (39 KB, leitura de
        /// orientação legítima) e abaixo de `snapshot` (251 KB) e `find --kind tag` (821 KB).
        /// </summary>
        private static int MaxStdout
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("TIA_MAX_STDOUT");
                int n;
                return int.TryParse(env, out n) && n > 0 ? n : 60000;
            }
        }

        private static void Print(object value)
        {
            // SAFE-09: um lugar só — quem apagou não precisa carregar o caminho até o resultado
            var dict = value as IDictionary<string, object>;
            if (dict != null && Core.Ops.RecoveryDir != null && !dict.ContainsKey("recoveryDir"))
                dict["recoveryDir"] = Core.Ops.RecoveryDir;
            if (_outFile != null)
            {
                Console.WriteLine(JsonConvert.SerializeObject(WriteOut(value, _outFile), Formatting.Indented));
                return;
            }
            var json = JsonConvert.SerializeObject(value, Formatting.Indented);
            if (_full || json.Length <= MaxStdout) { Console.WriteLine(json); return; }
            // saída grande sem destino: derrama em vez de despejar no chamador (--full desliga)
            var stub = WriteOut(value, "workspace/auto-" + Sanitize(_verb) + ".json", json);
            stub["autoSpill"] = "saída " + json.Length + " chars > TIA_MAX_STDOUT " + MaxStdout +
                "; JSON completo no arquivo. Use --full pro dump inteiro no stdout, ou --out-file F.json.";
            Console.WriteLine(JsonConvert.SerializeObject(stub, Formatting.Indented));
        }

        private static string Sanitize(string verb)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) verb = verb.Replace(c, '_');
            return verb;
        }

        /// <summary>Grava o JSON completo em `file` e devolve o stub {file,bytes,head[,count]}.</summary>
        private static Dictionary<string, object> WriteOut(object value, string file, string serialized = null)
        {
            var json = serialized ?? JsonConvert.SerializeObject(value, Formatting.Indented);
            var path = Path.GetFullPath(file);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);

            var stub = new Dictionary<string, object>
            {
                { "file", path },
                // API-05: json.Length é char UTF-16; o arquivo tem os bytes UTF-8 (acento conta 2).
                // O nome do campo é `bytes`, então quem manda é o arquivo.
                { "bytes", new FileInfo(path).Length },
                { "chars", json.Length },
                { "head", json.Length <= 600 ? json : json.Substring(0, 600) + "\n… (truncado; JSON completo no arquivo)" },
            };
            var count = CountOf(value);
            if (count >= 0) stub["count"] = count;
            return stub;
        }

        /// <summary>Tamanho do resultado quando ele é uma lista, ou já traz "count"/"hits". -1 = não aplicável.</summary>
        private static int CountOf(object value)
        {
            var dict = value as IDictionary<string, object>;
            if (dict != null)
            {
                object c;
                if (dict.TryGetValue("count", out c) && c is int) return (int)c;
                object hits;
                if (dict.TryGetValue("hits", out hits) && hits is System.Collections.ICollection)
                    return ((System.Collections.ICollection)hits).Count;
                return -1;
            }
            var list = value as System.Collections.ICollection;
            return list != null ? list.Count : -1;
        }

        /// <summary>
        /// Locates the Siemens Openness assemblies on the machine that runs the CLI.
        /// V21+ ships split assemblies (Siemens.Engineering.Base/Step7/WinCCUnified) under
        /// PublicAPI\V21\net48; V19/V20 shipped the monolithic Siemens.Engineering.dll.
        /// Order: TIA_ENGINEERING_DIR env var → exe folder → standard install paths.
        /// </summary>
        private static Assembly ResolveSiemensAssembly(object sender, ResolveEventArgs e)
        {
            var name = new AssemblyName(e.Name).Name;
            var dirs = name.StartsWith("Siemens.Engineering", StringComparison.OrdinalIgnoreCase) ? SiemensProbeDirs()
                : name.StartsWith("Siemens.Simatic.Simulation.Runtime", StringComparison.OrdinalIgnoreCase) ? PlcSimProbeDirs()
                : null;
            if (dirs == null) return null;

            var dllName = name + ".dll";
            var found = dirs.Select(d => Path.Combine(d, dllName)).FirstOrDefault(File.Exists);
            return found != null ? Assembly.LoadFrom(found) : null;
        }

        /// <summary>
        /// A API do S7-PLCSIM Advanced não vem com o Openness: mora em Common Files (x86), numa pasta
        /// por versão. Ela é resolvida em runtime como as do Openness (a DLL não é copiada pro lado do
        /// exe nem distribuída — a release proíbe qualquer `Siemens.*` no zip, INST-09). Sem PLCSIM
        /// instalado, só os verbos `sim-*` falham, e com mensagem de assembly ausente.
        /// </summary>
        private static List<string> PlcSimProbeDirs()
        {
            var dirs = new List<string> { AppDomain.CurrentDomain.BaseDirectory };
            var env = Environment.GetEnvironmentVariable("TIA_PLCSIM_DIR");
            if (!string.IsNullOrEmpty(env)) dirs.Insert(0, env);
            dirs.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "lib"));
            var api = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Common Files", "Siemens", "PLCSIMADV", "API");
            if (Directory.Exists(api))
                dirs.AddRange(Directory.GetDirectories(api).OrderByDescending(d => d));
            return dirs;
        }

        /// <summary>Onde o exe procura as assemblies do Openness, na ordem. Usado pelo resolver e por --version.</summary>
        private static List<string> SiemensProbeDirs()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var dirs = new List<string>();
            var env = Environment.GetEnvironmentVariable("TIA_ENGINEERING_DIR");
            if (!string.IsNullOrEmpty(env)) dirs.Add(env);
            dirs.Add(AppDomain.CurrentDomain.BaseDirectory);
            foreach (var version in new[] { "V21", "V20", "V19" })
            {
                var publicApi = Path.Combine(programFiles, "Siemens", "Automation", "Portal " + version, "PublicAPI", version);
                dirs.Add(Path.Combine(publicApi, "net48")); // V21+ layout
                dirs.Add(publicApi);                        // V19/V20 layout
            }
            return dirs;
        }
    }
}
