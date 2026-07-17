using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi;
using Siemens.Engineering.Multiuser;
using Siemens.Engineering.SW;

internal static class Program
{
    private static int Main()
    {
        TiaPortal tia = null;

        try
        {
            var process = TiaPortal.GetProcesses().FirstOrDefault();
            if (process == null)
            {
                Console.Error.WriteLine("Nenhuma instancia do TIA Portal foi encontrada.");
                return 1;
            }

            tia = process.Attach();

            if (tia.LocalSessions.Count == 0)
            {
                Console.Error.WriteLine("Nenhuma sessao local Multiuser foi encontrada nesta instancia.");
                return 1;
            }

            var project = tia.LocalSessions[0].Project;
            if (project == null)
            {
                Console.Error.WriteLine("A sessao local nao contem um projeto valido.");
                return 1;
            }

            var targets = GetTargets(project).ToList();
            if (targets.Count == 0)
            {
                Console.Error.WriteLine("Nenhum alvo compilavel foi encontrado.");
                return 1;
            }

            var selectedTarget = SelectTarget(targets);
            if (selectedTarget == null)
            {
                Console.Error.WriteLine("Nenhum alvo corresponde ao filtro informado.");
                Console.Error.WriteLine("Use: tia-test-compilation-modes.exe <nome-do-objeto> [PLC|HMI]");
                return 1;
            }

            Console.WriteLine("PROJECT=" + project.Name);
            Console.WriteLine("TARGET_TYPE=" + selectedTarget.Type);
            Console.WriteLine("TARGET_NAME=" + selectedTarget.Name);

            RunPass(tia, "PASS_1_APOS_ALTERACOES", selectedTarget);
            RunPass(tia, "PASS_2_SEM_NOVAS_ALTERACOES", selectedTarget);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.GetBaseException().Message);
            return 1;
        }
        finally
        {
            if (tia != null)
            {
                tia.Dispose();
            }
        }
    }

    private static IEnumerable<CompileTarget> GetTargets(MultiuserProject project)
    {
        foreach (var item in project.Devices.SelectMany(device => device.DeviceItems))
        {
            var container = item.GetService<SoftwareContainer>();
            if (container == null || container.Software == null)
            {
                continue;
            }

            if (container.Software is PlcSoftware plc)
            {
                var compilable = plc.GetService<ICompilable>();
                if (compilable != null)
                {
                    yield return new CompileTarget("PLC", plc.Name, compilable);
                }
            }
            else if (container.Software is HmiTarget hmi)
            {
                var compilable = hmi.GetService<ICompilable>();
                if (compilable != null)
                {
                    yield return new CompileTarget("HMI", hmi.Name, compilable);
                }
            }
        }
    }

    private static CompileTarget SelectTarget(IList<CompileTarget> targets)
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Length == 0)
        {
            return targets.FirstOrDefault();
        }

        var targetName = args[0];
        var targetType = args.Length > 1 ? args[1] : null;

        return targets.FirstOrDefault(target =>
            string.Equals(target.Name, targetName, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(targetType) || string.Equals(target.Type, targetType, StringComparison.OrdinalIgnoreCase)));
    }

    private static void RunPass(TiaPortal tia, string passName, CompileTarget target)
    {
        Console.WriteLine("BEGIN_" + passName);

        using (tia.ExclusiveAccess("Executar " + passName + " no projeto aberto"))
        {
            var stopwatch = Stopwatch.StartNew();
            var result = target.Compilable.Compile();
            stopwatch.Stop();

            Console.WriteLine(
                passName +
                "|TYPE=" + target.Type +
                "|NAME=" + target.Name +
                "|STATE=" + result.State +
                "|ERRORS=" + result.ErrorCount +
                "|WARNINGS=" + result.WarningCount +
                "|DURATION_MS=" + stopwatch.ElapsedMilliseconds);

            DumpMessages(passName, target, result.Messages, 0);
        }

        Console.WriteLine("END_" + passName);
    }

    private static void DumpMessages(string passName, CompileTarget target, CompilerResultMessageComposition messages, int depth)
    {
        foreach (CompilerResultMessage message in messages)
        {
            Console.WriteLine(
                passName +
                "|MESSAGE" +
                "|TYPE=" + target.Type +
                "|NAME=" + target.Name +
                "|DEPTH=" + depth +
                "|STATE=" + message.State +
                "|ERRORS=" + message.ErrorCount +
                "|WARNINGS=" + message.WarningCount +
                "|PATH=" + Sanitize(message.Path) +
                "|TEXT=" + Sanitize(message.Description));

            DumpMessages(passName, target, message.Messages, depth + 1);
        }
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
    }

    private sealed class CompileTarget
    {
        public CompileTarget(string type, string name, ICompilable compilable)
        {
            Type = type;
            Name = name;
            Compilable = compilable;
        }

        public string Type { get; private set; }
        public string Name { get; private set; }
        public ICompilable Compilable { get; private set; }
    }
}
