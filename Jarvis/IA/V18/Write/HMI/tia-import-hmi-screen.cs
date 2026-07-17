using System;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi;
using Siemens.Engineering.Hmi.Screen;

internal static class Program
{
    private static int Main()
    {
        TiaPortal tia = null;

        try
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var hmiName = args.Length > 0 ? args[0] : "HMI_RT_1";
            var screenName = args.Length > 1 ? args[1] : "TEST";
            var importFile = args.Length > 2
                ? args[2]
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Lab", "Exports", "HMI", hmiName, "Screens", screenName + ".xml");

            var importPath = Path.GetFullPath(importFile);
            if (!File.Exists(importPath))
            {
                Console.Error.WriteLine("Arquivo de importacao nao encontrado: " + importPath);
                return 1;
            }

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

            var ownerFolder = FindParentFolder(hmi.ScreenFolder, screenName);
            if (ownerFolder == null)
            {
                Console.Error.WriteLine("A tela " + screenName + " nao foi encontrada.");
                return 1;
            }

            Console.WriteLine("PROJECT=" + project.Name);
            Console.WriteLine("HMI=" + hmi.Name);
            Console.WriteLine("SCREEN=" + screenName);
            Console.WriteLine("FILE=" + importPath);

            ownerFolder.Screens.Import(new FileInfo(importPath), ImportOptions.Override);

            var updated = FindScreen(hmi.ScreenFolder, screenName);
            Console.WriteLine(updated != null ? "RESULT=IMPORTED" : "RESULT=NOT_FOUND_AFTER_IMPORT");
            return updated != null ? 0 : 1;
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

    private static ScreenFolder FindParentFolder(ScreenFolder folder, string screenName)
    {
        foreach (Screen screen in folder.Screens)
        {
            if (string.Equals(screen.Name, screenName, StringComparison.OrdinalIgnoreCase))
            {
                return folder;
            }
        }

        foreach (ScreenFolder subfolder in folder.Folders)
        {
            var found = FindParentFolder(subfolder, screenName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Screen FindScreen(ScreenFolder folder, string screenName)
    {
        foreach (Screen screen in folder.Screens)
        {
            if (string.Equals(screen.Name, screenName, StringComparison.OrdinalIgnoreCase))
            {
                return screen;
            }
        }

        foreach (ScreenFolder subfolder in folder.Folders)
        {
            var found = FindScreen(subfolder, screenName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
