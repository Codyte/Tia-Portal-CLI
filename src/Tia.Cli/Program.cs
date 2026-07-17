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
                    { "read", new[] { "info", "list-devices", "list-blocks", "list-tags",
                        "export-block --name X [--out DIR]", "export-tags --table X [--out DIR]" } },
                    { "write", new[] { "import-block --file F [--folder A/B] [--apply]",
                        "import-tags --file F [--apply]", "compile [--apply]",
                        "gen-profinet --config F [--apply]" } },
                    { "notes", "write verbs are dry-run unless --apply; default --out is .\\workspace\\exports" },
                });
                return args.Length == 0 ? 1 : 0;
            }
            try
            {
                return Run(args);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Print(new Dictionary<string, object>
                {
                    { "error", inner.Message },
                    { "type", inner.GetType().Name },
                });
                return 1;
            }
        }

        // Must not be inlined into Main: Siemens types may only be JITted after AssemblyResolve is hooked.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run(string[] args)
        {
            string verb = args[0];
            string plcName = OptionValue(args, "--plc");
            string outDir = OptionValue(args, "--out") ?? Path.Combine("workspace", "exports");
            bool apply = args.Contains("--apply");

            using (var session = Core.TiaSession.Attach())
            {
                object result;
                switch (verb)
                {
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
                    case "export-block":
                        result = Core.Ops.ExportBlock(session.GetPlc(plcName), Require(args, "--name"), outDir);
                        break;
                    case "export-tags":
                        result = Core.Ops.ExportTagTable(session.GetPlc(plcName), Require(args, "--table"), outDir);
                        break;
                    case "import-block":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportBlock(session.GetPlc(plcName), Require(args, "--file"),
                                OptionValue(args, "--folder"), apply);
                        break;
                    case "import-tags":
                        using (WriteLock(session, apply, verb))
                            result = Core.Ops.ImportTagTable(session.GetPlc(plcName), Require(args, "--file"), apply);
                        break;
                    case "compile":
                        var plc = session.GetPlc(plcName);
                        if (apply)
                            using (WriteLock(session, true, verb))
                                result = Core.Ops.Compile(plc);
                        else
                            result = new Dictionary<string, object> { { "wouldCompile", plc.Name }, { "applied", false } };
                        break;
                    case "gen-profinet":
                        var config = JsonConvert.DeserializeObject<Core.ProfinetConfig>(
                            File.ReadAllText(Require(args, "--config")));
                        using (WriteLock(session, apply, verb))
                            result = Core.Profinet.Generate(session, session.GetPlc(plcName), config, apply);
                        break;
                    default:
                        throw new ArgumentException("Unknown verb '" + verb + "'. Run tia --help.");
                }
                Print(result);
                return 0;
            }
        }

        private static string OptionValue(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
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
        /// Locates Siemens.Engineering.dll on the machine that runs the CLI:
        /// TIA_ENGINEERING_DLL env var → exe folder → standard V19/V20 install paths.
        /// </summary>
        private static Assembly ResolveSiemensAssembly(object sender, ResolveEventArgs e)
        {
            if (!e.Name.StartsWith("Siemens.Engineering,", StringComparison.OrdinalIgnoreCase))
                return null;

            var candidates = new List<string>();
            var env = Environment.GetEnvironmentVariable("TIA_ENGINEERING_DLL");
            if (!string.IsNullOrEmpty(env)) candidates.Add(env);
            candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Siemens.Engineering.dll"));
            foreach (var version in new[] { "V20", "V19" })
                candidates.Add(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Siemens", "Automation", "Portal " + version, "PublicAPI", version, "Siemens.Engineering.dll"));

            var found = candidates.FirstOrDefault(File.Exists);
            return found != null ? Assembly.LoadFrom(found) : null;
        }
    }
}
