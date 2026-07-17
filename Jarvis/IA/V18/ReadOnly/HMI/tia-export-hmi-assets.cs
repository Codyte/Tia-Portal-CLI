using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi;
using Siemens.Engineering.Hmi.Screen;
using Siemens.Engineering.Hmi.Tag;

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

            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var hmiName = args.Length > 0 ? args[0] : "HMI_RT_5";
            var mode = args.Length > 1 ? args[1] : "screen";
            var assetName = args.Length > 2 ? args[2] : string.Empty;

            var project = tia.LocalSessions[0].Project;
            var hmi = project.Devices
                .SelectMany(device => device.DeviceItems)
                .Select(item =>
                {
                    var container = item.GetService<SoftwareContainer>();
                    return container != null ? container.Software : null;
                })
                .OfType<HmiTarget>()
                .FirstOrDefault(item => string.Equals(item.Name, hmiName, StringComparison.OrdinalIgnoreCase));

            if (hmi == null)
            {
                Console.Error.WriteLine("A HMI " + hmiName + " nao foi encontrada.");
                return 1;
            }

            Console.WriteLine("PROJECT=" + project.Name);
            Console.WriteLine("HMI=" + hmi.Name);
            Console.WriteLine("MODE=" + mode);

            if (string.Equals(mode, "screen", StringComparison.OrdinalIgnoreCase))
            {
                var screen = FindScreen(hmi.ScreenFolder, assetName);
                if (screen == null)
                {
                    Console.Error.WriteLine("A tela " + assetName + " nao foi encontrada.");
                    return 1;
                }

                var exportFile = BuildExportPath("HMI", hmi.Name, "Screens", screen.Name + ".xml");
                screen.Export(new FileInfo(exportFile), ExportOptions.WithDefaults);
                Console.WriteLine("SCREEN=" + screen.Name);
                Console.WriteLine("FILE=" + exportFile);
                return 0;
            }

            if (string.Equals(mode, "tagtable", StringComparison.OrdinalIgnoreCase))
            {
                var table = FindTagTable(hmi.TagFolder, assetName);
                if (table == null)
                {
                    Console.Error.WriteLine("A tabela de tags " + assetName + " nao foi encontrada.");
                    return 1;
                }

                var exportFile = BuildExportPath("HMI", hmi.Name, "Tags", table.Name + ".xml");
                table.Export(new FileInfo(exportFile), ExportOptions.WithDefaults);
                Console.WriteLine("TAG_TABLE=" + table.Name);
                Console.WriteLine("FILE=" + exportFile);
                return 0;
            }

            Console.Error.WriteLine("Modo invalido. Use screen ou tagtable.");
            return 1;
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

    private static Screen FindScreen(ScreenFolder folder, string name)
    {
        foreach (Screen screen in folder.Screens)
        {
            if (string.Equals(screen.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return screen;
            }
        }

        foreach (ScreenFolder subfolder in folder.Folders)
        {
            var found = FindScreen(subfolder, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static TagTable FindTagTable(TagFolder folder, string name)
    {
        foreach (TagTable table in folder.TagTables)
        {
            if (string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return table;
            }
        }

        foreach (TagFolder subfolder in folder.Folders)
        {
            var found = FindTagTable(subfolder, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static string BuildExportPath(string area, string assetOwner, string category, string fileName)
    {
        var root = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..",
            "Lab",
            "Exports",
            area,
            Sanitize(assetOwner),
            category);

        var fullRoot = Path.GetFullPath(root);
        Directory.CreateDirectory(fullRoot);
        return Path.Combine(fullRoot, Sanitize(fileName));
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}
