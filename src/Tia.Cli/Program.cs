// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L115   class Program
//   L117   .Main
//   L312   .ExitCodeFor
//   L323   .RunLadderDryRun
//   L333   .RunExplainFile
//   L341   .RunInterfaceFile
//   L355   .Run
//   L446   .ParseScript
//   L466   .DispatchWithRetry
//   L480   .IsBusy
//   L486   .Dispatch
//   L496   case "save-project"
//   L499   case "close-project"
//   L502   case "info"
//   L505   case "list-devices"
//   L508   case "list-blocks"
//   L512   case "list-tags"
//   L515   case "tree"
//   L519   case "list-types"
//   L522   case "find"
//   L526   case "snapshot"
//   L529   case "xref"
//   L532   case "trace"
//   L535   case "list-hmi"
//   L538   case "free-memory"
//   L544   case "export-block"
//   L547   case "explain-block"
//   L552   case "export-tags"
//   L555   case "list-interface"
//   L561   case "import-block"
//   L568   case "import-ladder"
//   L576   case "import-source"
//   L581   case "create-folder"
//   L586   case "delete-folder"
//   L591   case "delete-block"
//   L595   case "move-block"
//   L600   case "delete-type"
//   L604   case "export-type"
//   L607   case "import-type"
//   L611   case "scaffold"
//   L620   case "clone"
//   L627   case "add-call"
//   L634   case "delete-network"
//   L639   case "set-retain"
//   L644   case "add-db-member"
//   L650   case "import-tags"
//   L657   case "create-library"
//   L661   case "list-library"
//   L664   case "import-master-copy"
//   L670   case "add-master-copy"
//   L676   case "create-instance-db"
//   L681   case "delete-master-copy"
//   L686   case "add-device"
//   L691   case "delete-device"
//   L695   case "add-tag"
//   L701   case "delete-tag"
//   L706   case "edit-db-member"
//   L712   case "delete-db-member"
//   L717   case "rename-block"
//   L722   case "set-tag"
//   L729   case "set-attr"
//   L735   case "list-attrs"
//   L739   case "plug-module"
//   L745   case "list-telegrams"
//   L748   case "insert-telegram"
//   L755   case "set-address"
//   L761   case "set-io-address"
//   L767   case "list-io-map"
//   L771   case "set-memory-bytes"
//   L777   case "connect-subnet"
//   L782   case "export-cax"
//   L785   case "import-cax"
//   L789   case "compile"
//   L804   case "diff-block"
//   L808   case "audit"
//   L813   case "doctor"
//   L821   case "gen-profinet"
//   L827   case "standardize-tags"
//   L835   case "gen-fault-ob"
//   L843   case "replicate-fc"
//   L850   case "gen-alarm-fc"
//   L858   case "replicate-instruments"
//   L871   .OptionValue
//   L877   .ParseInt
//   L883   .ParseByte
//   L890   .OptionValues
//   L899   .WriteLock
//   L904   .Require
//   L936   .Print
//   L952   .Sanitize
//   L959   .WriteOut
//   L978   .CountOf
//   L1000  .ResolveSiemensAssembly
//   L1011  .SiemensProbeDirs
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
            _outFile = OptionValue(args, "--out-file"); // antes do --help: vale pra toda saída, inclusive ele
            _full = Array.IndexOf(args, "--full") >= 0;
            _verb = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "out";
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
                        "list-hmi [--device X]  (WinCC Unified: telas + tag tables)",
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
                            + "(sem --type: lista slots livres; com --type: canPlug e, com --apply, pluga)",
                        "list-telegrams --device X  (read-only: drive objects SINAMICS e telegramas de cada um)",
                        "insert-telegram --device X --number N [--type Main|Supplementary|Safety|Torque|Edge] "
                            + "[--item I] [--drive-object D] [--change] [--apply]  "
                            + "(--change troca o telegrama presente: G120 novo já vem com o 1)  "
                            + "(telegrama de drive NÃO é submódulo de catálogo — plug-module não coloca)",
                        "set-address --device X [--ip A.B.C.D] [--mask M] [--pn-name N] [--apply]",
                        "set-io-address --device X [--item I] [--io Input|Output] [--start N] [--apply]  "
                            + "(endereço inicial do módulo de I/O; não é atributo — set-attr não alcança, "
                            + "e o import-cax ignora. Sem --item: varre o device (sonda). Sem --start: só lista)",
                        "list-io-map [--device X] [--io Input|Output]  (read-only: todo endereço de I/O "
                            + "do projeto — device/item, %IB..%QB e o próximo byte livre por tipo; "
                            + "é onde se lê o endereço do telegrama de drive, que list-telegrams não traz)",
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
                            + "[--with-instances] [--apply]  (--with-instances cria os iDBs que o clone passa a "
                            + "referenciar; sem eles o compile morre em 'Missing instance DB')",
                        "add-call --block X --fb \"FB Y\" --inst iDB --param P=<tag|DB.caminho.membro|const> [--param ...] "
                            + "[--after N] [--title T] [--comment C] [--out DIR] [--apply]  "
                            + "(rede LAD com a chamada, EN no powerrail; os pinos saem da interface do FB. "
                            + "--after 0 = primeira rede, omitido = no fim)",
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
                        "replicate-fc --config F [--out DIR] [--apply] [--force]  (--force: sobrescreve pasta já populada)",
                        "gen-alarm-fc [--config F] [--out DIR] [--apply]",
                        "replicate-instruments --config F [--out DIR] [--apply]" } },
                    { "library", new[] { "list-library --file X.al19",
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
                        "--summary = só {steps,failed,errors[]}, sem o resultado de cada step. " +
                        "--plc/--out-file do processo NÃO descem pros steps: cada step carrega os seus. " +
                        "Exige projeto JÁ aberto: o attach é 1x, antes do 1º step, então open-project/create-project " +
                        "(e list-server-projects, que roda sem projeto) não podem ser step — chamar antes, sozinhos)" } },
                    { "meta", new[] { "--version  (versão do CLI + qual instalação do Openness este exe carrega; " +
                        "é a 1ª linha de qualquer bug report)" } },
                    { "notes", "write verbs are dry-run unless --apply; default --out is .\\workspace\\exports; " +
                        "saída acima de 60k chars (TIA_MAX_STDOUT) derrama SOZINHA em workspace/auto-<verbo>.json e " +
                        "o stdout recebe o stub {file,bytes,count,head,autoSpill} — --full desliga e dumpa tudo no " +
                        "stdout (use em script que faz ConvertFrom-Json); " +
                        "--out-file F.json (qualquer verbo: JSON completo no arquivo escolhido, stdout só o stub); " +
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
            if (ex.Message.Contains("Siemens.Engineering")) return 4; // DLL missing = TIA env, not user file
            if (ex is FileNotFoundException) return 3;
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
            // com mais de um portal aberto, escolhe qual (senão o attach falha alto)
            Core.TiaSession.PortalFilter = OptionValue(args, "--portal");

            // run before Attach: may start the portal themselves
            if (args[0] == "open-project")
            {
                Print(Core.TiaSession.OpenProject(Require(args, "--file"), !args.Contains("--no-ui")));
                return 0;
            }
            if (args[0] == "create-project")
            {
                Print(Core.TiaSession.CreateProject(Require(args, "--dir"), Require(args, "--name"),
                    !args.Contains("--no-ui")));
                return 0;
            }

            // read-only no servidor: precisa de portal aberto, mas NÃO de projeto aberto
            if (args[0] == "list-server-projects")
            {
                Print(Core.Multiuser.ListServerProjects(Require(args, "--server"),
                    int.Parse(OptionValue(args, "--port") ?? "0"),
                    args.Contains("--http"), args.Contains("--keep-connection")));
                return 0;
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
                    foreach (var step in steps)
                    {
                        var entry = new Dictionary<string, object> { { "verb", step[0] } };
                        try
                        {
                            var value = DispatchWithRetry(session, step, args);
                            // --out-file do step: cada leitura pesada do batch vai pro seu arquivo,
                            // e o resultado no batch fica sendo o stub (o do processo vale só p/ o batch todo)
                            var stepOut = OptionValue(step, "--out-file");
                            entry["result"] = stepOut == null ? value : WriteOut(value, stepOut);
                            entry["ok"] = true;
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
                        results.Add(entry);
                    }
                    // --summary: 98 steps × resultado completo é o dump que estoura contexto do agente.
                    // Só o que muda decisão: contagem, e o erro dos que falharam.
                    if (args.Contains("--summary"))
                        Print(new Dictionary<string, object>
                        {
                            { "steps", results.Count },
                            { "failed", failed },
                            { "errors", results.Cast<Dictionary<string, object>>()
                                .Select((e, i) => new { e, i })
                                .Where(x => !(bool)x.e["ok"])
                                .Select(x => new Dictionary<string, object>
                                    { { "step", x.i }, { "verb", x.e["verb"] }, { "error", x.e["error"] } })
                                .ToList() },
                        });
                    else
                        Print(new Dictionary<string, object>
                            { { "steps", results.Count }, { "failed", failed }, { "results", results } });
                    return failed > 0 ? 1 : 0;
                }
                Print(DispatchWithRetry(session, args, args));
                return 0;
            }
        }

        /// <summary>
        /// Lê e valida o script do batch. Roda antes do Attach: erro de uso não deve custar sessão
        /// nem exigir portal aberto. open-project/create-project não podem ser step — o attach é 1x,
        /// antes do 1º step, então o batch não abre o projeto em que ele mesmo trabalha.
        /// </summary>
        private static List<string[]> ParseScript(string file)
        {
            var steps = JsonConvert.DeserializeObject<List<string[]>>(File.ReadAllText(file));
            if (steps == null || steps.Count == 0)
                throw new ArgumentException(
                    "Script must be a JSON array of arg arrays, e.g. [[\"list-blocks\"],[\"compile\",\"--apply\"]].");
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
                                Require(args, "--fb"), Require(args, "--inst"), OptionValues(args, "--param"),
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
                                OptionValue(args, "--pn-name"), apply);
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
                            (path, type) => JsonConvert.DeserializeObject(File.ReadAllText(path), type));
                        break;
                    case "gen-profinet":
                        var config = JsonConvert.DeserializeObject<Core.ProfinetConfig>(
                            File.ReadAllText(Require(args, "--config")));
                        using (WriteLock(session, apply, verb))
                            result = Core.Profinet.Generate(session, session.GetPlc(plcName), config, apply);
                        break;
                    case "standardize-tags":
                        var stdPath = OptionValue(args, "--config");
                        var stdConfig = stdPath != null
                            ? JsonConvert.DeserializeObject<Core.StandardizeConfig>(File.ReadAllText(stdPath))
                            : new Core.StandardizeConfig();
                        using (WriteLock(session, apply, verb))
                            result = Core.Standardize.Run(session.GetPlc(plcName), stdConfig, apply);
                        break;
                    case "gen-fault-ob":
                        var fobPath = OptionValue(args, "--config");
                        var fobConfig = fobPath != null
                            ? JsonConvert.DeserializeObject<Core.FaultObConfig>(File.ReadAllText(fobPath))
                            : new Core.FaultObConfig();
                        using (WriteLock(session, apply, verb))
                            result = Core.FaultOb.Generate(session, session.GetPlc(plcName), fobConfig, outDir, apply);
                        break;
                    case "replicate-fc":
                        var repConfig = JsonConvert.DeserializeObject<Core.ReplicateFcConfig>(
                            File.ReadAllText(Require(args, "--config")));
                        using (WriteLock(session, apply, verb))
                            result = Core.ReplicateFc.Run(session.GetPlc(plcName), repConfig, outDir, apply,
                                args.Contains("--force"));
                        break;
                    case "gen-alarm-fc":
                        var almPath = OptionValue(args, "--config");
                        var almConfig = almPath != null
                            ? JsonConvert.DeserializeObject<Core.AlarmFcConfig>(File.ReadAllText(almPath))
                            : new Core.AlarmFcConfig();
                        using (WriteLock(session, apply, verb))
                            result = Core.AlarmFc.Generate(session, session.GetPlc(plcName), almConfig, outDir, apply);
                        break;
                    case "replicate-instruments":
                        var insConfig = JsonConvert.DeserializeObject<Core.InstrumentFcConfig>(
                            File.ReadAllText(Require(args, "--config")));
                        using (WriteLock(session, apply, verb))
                            result = Core.InstrumentFc.Run(session, session.GetPlc(plcName), insConfig, outDir, apply);
                        break;
                    default:
                        throw new ArgumentException("Unknown verb '" + verb + "'. Run tia --help.");
                }
                return result;
            }
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
                { "bytes", json.Length },
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
            if (!e.Name.StartsWith("Siemens.Engineering", StringComparison.OrdinalIgnoreCase))
                return null;

            var dllName = new AssemblyName(e.Name).Name + ".dll";
            var found = SiemensProbeDirs().Select(d => Path.Combine(d, dllName)).FirstOrDefault(File.Exists);
            return found != null ? Assembly.LoadFrom(found) : null;
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
