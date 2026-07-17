using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Multiuser;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

internal static class Program
{
    private const string DefaultGroupPrefix = "4.4";

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

            var plc = project.Devices
                .SelectMany(device => device.DeviceItems)
                .Select(item =>
                {
                    var container = item.GetService<SoftwareContainer>();
                    return container != null ? container.Software : null;
                })
                .OfType<PlcSoftware>()
                .FirstOrDefault();

            if (plc == null)
            {
                Console.Error.WriteLine("Nenhum CLP foi encontrado no projeto aberto.");
                return 1;
            }

            var groupPrefix = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(groupPrefix))
            {
                groupPrefix = DefaultGroupPrefix;
            }

            var group = FindUserGroup(plc.BlockGroup, groupPrefix);
            if (group == null)
            {
                Console.Error.WriteLine("A pasta " + groupPrefix + " nao foi encontrada em Program blocks.");
                return 1;
            }

            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Lab", "Exports", "ProgramBlocks", groupPrefix);
            var exportRoot = Path.GetFullPath(root);
            Directory.CreateDirectory(exportRoot);

            Console.WriteLine("PROJECT=" + project.Name);
            Console.WriteLine("PLC=" + plc.Name);
            Console.WriteLine("GROUP=" + group.Name);
            Console.WriteLine("EXPORT_ROOT=" + exportRoot);

            ExportGroup(group, exportRoot, group.Name);
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

    private static PlcBlockUserGroup FindUserGroup(PlcBlockSystemGroup root, string groupPrefix)
    {
        foreach (PlcBlockUserGroup group in root.Groups)
        {
            var found = FindUserGroup(group, groupPrefix);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static PlcBlockUserGroup FindUserGroup(PlcBlockUserGroup group, string groupPrefix)
    {
        if (group.Name.StartsWith(groupPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return group;
        }

        foreach (PlcBlockUserGroup subgroup in group.Groups)
        {
            var found = FindUserGroup(subgroup, groupPrefix);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void ExportGroup(PlcBlockUserGroup group, string exportRoot, string relativePath)
    {
        var groupDir = Path.Combine(exportRoot, SanitizePathSegment(relativePath));
        Directory.CreateDirectory(groupDir);

        Console.WriteLine("GROUP_PATH=" + relativePath);

        foreach (PlcBlock block in group.Blocks)
        {
            var exportFile = Path.Combine(groupDir, SanitizePathSegment(block.Name) + ".xml");
            block.Export(new FileInfo(exportFile), ExportOptions.WithDefaults);
            Console.WriteLine("BLOCK=" + relativePath + "\\" + block.Name);
            Console.WriteLine("FILE=" + exportFile);
        }

        foreach (PlcBlockUserGroup subgroup in group.Groups)
        {
            ExportGroup(subgroup, exportRoot, relativePath + "\\" + subgroup.Name);
        }
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return sanitized.Replace('.', '_').Trim();
    }
}
