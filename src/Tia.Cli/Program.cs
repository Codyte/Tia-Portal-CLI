using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                Print(new Dictionary<string, object>
                {
                    { "usage", "tia <verb> [--plc NAME] [--apply]" },
                    { "session", new[] { "open-project --file X.ap21 [--no-ui]",
                        "create-project --dir D --name N [--no-ui]",
                        "save-project", "close-project [--save]" } },
                    { "read", new[] { "info", "list-devices", "list-blocks", "list-tags", "list-types",
                        "find --pattern P* [--kind block|table|tag|type]",
                        "snapshot  (inventário completo)", "xref --name BLOCK",
                        "trace --equipment BH-01A  (símbolos do equipamento + quem referencia; varre todos os blocos)",
                        "tree [--out DIR]  (outline navindex dos Program blocks → plc-navi.md)",
                        "list-hmi [--device X]  (WinCC Unified: telas + tag tables)",
                        "export-block --name X [--out DIR]", "export-tags --table X [--out DIR]",
                        "explain-block --name X | --file F.xml  (LAD/FBD → texto compacto; --file roda sem TIA)",
                        "export-type --name X [--out DIR]",
                        "free-memory [--bytes N] [--from B] [--count K]  (buracos livres na área %M; length -1 = até o fim)" } },
                    { "structure", new[] { "create-folder --path A/B [--tags] [--apply]",
                        "delete-folder --path A/B [--tags] [--apply]",
                        "delete-block --name X [--apply]",
                        "import-type --file F.xml [--apply]",
                        "scaffold --manifest F.json [--apply] [--force]  (árvore da lei + moldes num projeto novo)" } },
                    { "hardware", new[] { "add-device --mlfb \"6ES7 ...\" --name X [--station S] [--group G] [--apply]",
                        "set-address --device X [--ip A.B.C.D] [--mask M] [--pn-name N] [--apply]",
                        "connect-subnet --device X --subnet S [--io-system IO] [--apply]",
                        "export-cax [--out DIR]", "import-cax --file F.aml [--apply]" } },
                    { "write", new[] { "import-block --file F [--folder A/B] [--apply]",
                        "import-source --file F.scl [--apply]",
                        "import-ladder --file F.scl [--name N] [--folder A/B] [--apply]  (SCL subset → LAD; dry-run works without TIA)",
                        "import-tags --file F [--folder A/B] [--apply]",
                        "clone --block N | --table T --replace OLD=NEW [--replace ...] [--at %M432.0] [--folder A/B] [--apply]",
                        "add-db-member --db X --name M [--path A.B] [--type T | --like SIBLING] [--out DIR] [--apply]",
                        "compile [--block X | --folder A/B] [--apply]",
                        "diff-block --file F.xml [--name X]  (read-only, normalized compare)",
                        "doctor [--verb V] [--config F]  (read-only preflight dos verbos geradores)",
                        "audit [--plc N] [--max 50]  (read-only: projeto × lei de nomenclatura do PADRAO)",
                        "gen-profinet --config F [--apply]",
                        "standardize-tags [--config F] [--apply]",
                        "gen-fault-ob [--config F] [--out DIR] [--apply]",
                        "replicate-fc --config F [--out DIR] [--apply] [--force]  (--force: sobrescreve pasta já populada)",
                        "gen-alarm-fc [--config F] [--out DIR] [--apply]",
                        "replicate-instruments --config F [--out DIR] [--apply]" } },
                    { "library", new[] { "list-library --file X.al19",
                        "import-master-copy --file X.al19 --name M [--folder A/B] [--apply]" } },
                    { "multiuser", new[] { "list-server-projects --server HOST [--port N] [--http] [--keep-connection]" +
                        "  (read-only: projetos do TIA Project Server, lock e sessões locais)" } },
                    { "batch", new[] { "run --script ops.json  (JSON array de arg-arrays, uma sessão só)" } },
                    { "notes", "write verbs are dry-run unless --apply; default --out is .\\workspace\\exports; " +
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

                var timeout = OptionValue(args, "--timeout");
                if (timeout == null) return Run(args);
                var task = System.Threading.Tasks.Task.Run(() => Run(args));
                if (!task.Wait(TimeSpan.FromSeconds(int.Parse(timeout))))
                {
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

        // Must not be inlined into Main: Siemens types may only be JITted after AssemblyResolve is hooked.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run(string[] args)
        {
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

            using (var session = Core.TiaSession.Attach())
            {
                // batch: list of verbs in one attach — [["list-blocks"],["compile","--apply"]]
                if (args[0] == "run")
                {
                    var steps = JsonConvert.DeserializeObject<List<string[]>>(
                        File.ReadAllText(Require(args, "--script")));
                    if (steps == null || steps.Count == 0)
                        throw new ArgumentException(
                            "Script must be a JSON array of arg arrays, e.g. [[\"list-blocks\"],[\"compile\",\"--apply\"]].");
                    var results = new List<object>();
                    foreach (var step in steps)
                    {
                        if (step == null || step.Length == 0 || step[0] == "run"
                            || step[0] == "open-project" || step[0] == "create-project")
                            throw new ArgumentException("Each step must be a verb (not 'run'/'open-project'/'create-project').");
                        results.Add(new Dictionary<string, object>
                            { { "verb", step[0] }, { "result", DispatchWithRetry(session, step, args) } });
                    }
                    Print(new Dictionary<string, object> { { "steps", results.Count }, { "results", results } });
                    return 0;
                }
                Print(DispatchWithRetry(session, args, args));
                return 0;
            }
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
                        result = Core.Inventory.Blocks(session.GetPlc(plcName));
                        break;
                    case "list-tags":
                        result = Core.Inventory.TagTables(session.GetPlc(plcName));
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
                    case "import-block":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportBlock(session.GetPlc(plcName), Require(args, "--file"),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "import-ladder":
                        var lad = Core.LadConverter.Convert(Require(args, "--file"), OptionValue(args, "--name"), outDir);
                        using (WriteLock(session, true, verb))
                            Core.Ops.ImportBlock(session.GetPlc(plcName), (string)lad["xmlFile"],
                                OptionValue(args, "--folder"), true);
                        lad["applied"] = true;
                        result = lad;
                        break;
                    case "import-source":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportSource(session.GetPlc(plcName), Require(args, "--file"), apply);
                        break;
                    case "create-folder":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.CreateFolder(session.GetPlc(plcName), Require(args, "--path"),
                                args.Contains("--tags"), apply);
                        break;
                    case "delete-folder":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.DeleteFolder(session.GetPlc(plcName), Require(args, "--path"),
                                args.Contains("--tags"), apply);
                        break;
                    case "delete-block":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.DeleteBlock(session.GetPlc(plcName), Require(args, "--name"), apply);
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
                                Path.GetDirectoryName(manifestFile), apply, args.Contains("--force"));
                        break;
                    case "clone":
                        using (WriteLock(session, apply, verb))
                            result = Core.Clone.Run(session.GetPlc(plcName), OptionValue(args, "--block"),
                                OptionValue(args, "--table"), OptionValues(args, "--replace"),
                                OptionValue(args, "--at"), OptionValue(args, "--folder"), outDir, apply);
                        break;
                    case "add-db-member":
                        using (WriteLock(session, apply, verb))
                            result = Core.DbMember.Add(session.GetPlc(plcName), Require(args, "--db"),
                                OptionValue(args, "--path"), Require(args, "--name"),
                                OptionValue(args, "--type"), OptionValue(args, "--like"), outDir, apply);
                        break;
                    case "import-tags":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportTagTable(session.GetPlc(plcName), Require(args, "--file"), OptionValue(args, "--folder"), apply);
                        break;
                    case "list-library":
                        result = Core.Library.List(session, Require(args, "--file"));
                        break;
                    case "import-master-copy":
                        using (WriteLock(session, apply, verb))
                            result = Core.Library.ImportMasterCopy(session, session.GetPlc(plcName),
                                Require(args, "--file"), Require(args, "--name"),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "add-device":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.AddDevice(session, Require(args, "--mlfb"),
                                Require(args, "--name"), OptionValue(args, "--station"), OptionValue(args, "--group"), apply);
                        break;
                    case "set-address":
                        using (WriteLock(session, apply, verb))
                            result = Core.Hardware.SetAddress(session, Require(args, "--device"),
                                OptionValue(args, "--ip"), OptionValue(args, "--mask"),
                                OptionValue(args, "--pn-name"), apply);
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
                                result = Core.Ops.Compile(plc, scopeBlock, scopeFolder);
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
                            int.Parse(OptionValue(args, "--max") ?? "50"));
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

        private static void Print(object value)
        {
            Console.WriteLine(JsonConvert.SerializeObject(value, Formatting.Indented));
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

            var found = dirs.Select(d => Path.Combine(d, dllName)).FirstOrDefault(File.Exists);
            return found != null ? Assembly.LoadFrom(found) : null;
        }
    }
}
