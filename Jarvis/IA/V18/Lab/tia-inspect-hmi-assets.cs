using System;
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

            var project = tia.LocalSessions[0].Project;
            if (project == null)
            {
                Console.Error.WriteLine("A sessao local nao contem um projeto valido.");
                return 1;
            }

            Console.WriteLine("PROJECT=" + project.Name);

            foreach (var hmi in project.Devices
                .SelectMany(device => device.DeviceItems)
                .Select(item =>
                {
                    var container = item.GetService<SoftwareContainer>();
                    return container != null ? container.Software : null;
                })
                .OfType<HmiTarget>())
            {
                Console.WriteLine("HMI=" + hmi.Name);
                Console.WriteLine("SCREEN_FOLDER_TYPE=" + hmi.ScreenFolder.GetType().FullName);
                DumpScreenFolder(hmi.ScreenFolder, 0);
                Console.WriteLine("TAG_FOLDER_TYPE=" + hmi.TagFolder.GetType().FullName);
                DumpTagFolder(hmi.TagFolder, 0);
            }

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

    private static void DumpScreenFolder(ScreenFolder folder, int depth)
    {
        Console.WriteLine(new string(' ', depth * 2) + "SCREEN_FOLDER=" + folder.Name);

        foreach (Screen screen in folder.Screens)
        {
            Console.WriteLine(new string(' ', (depth + 1) * 2) + "SCREEN=" + screen.Name + "|" + screen.GetType().FullName);
        }

        foreach (ScreenFolder subfolder in folder.Folders)
        {
            DumpScreenFolder(subfolder, depth + 1);
        }
    }

    private static void DumpTagFolder(TagFolder folder, int depth)
    {
        Console.WriteLine(new string(' ', depth * 2) + "TAG_FOLDER=" + folder.Name);

        foreach (TagTable table in folder.TagTables)
        {
            Console.WriteLine(new string(' ', (depth + 1) * 2) + "TAG_TABLE=" + table.Name + "|" + table.GetType().FullName);
        }

        foreach (TagFolder subfolder in folder.Folders)
        {
            DumpTagFolder(subfolder, depth + 1);
        }
    }
}
