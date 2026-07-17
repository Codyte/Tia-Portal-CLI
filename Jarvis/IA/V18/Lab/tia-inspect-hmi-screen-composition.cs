using System;
using System.Linq;
using System.Reflection;
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
            var process = TiaPortal.GetProcesses().FirstOrDefault();
            if (process == null)
            {
                Console.Error.WriteLine("Nenhuma instancia do TIA Portal foi encontrada.");
                return 1;
            }

            tia = process.Attach();
            var project = tia.LocalSessions[0].Project;

            var hmi = project.Devices
                .SelectMany(device => device.DeviceItems)
                .Select(item =>
                {
                    var container = item.GetService<SoftwareContainer>();
                    return container != null ? container.Software : null;
                })
                .OfType<HmiTarget>()
                .FirstOrDefault(item => string.Equals(item.Name, "HMI_RT_1", StringComparison.OrdinalIgnoreCase));

            var screen = FindScreen(hmi.ScreenFolder, "TEST");
            var parent = screen.Parent;

            Console.WriteLine("PARENT_TYPE=" + parent.GetType().FullName);

            var screensProperty = parent.GetType().GetProperty("Screens", BindingFlags.Public | BindingFlags.Instance);
            if (screensProperty != null)
            {
                var screens = screensProperty.GetValue(parent, null);
                Console.WriteLine("SCREENS_COMPOSITION_TYPE=" + screens.GetType().FullName);

                foreach (var method in screens.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.Name.Contains("Create") || method.Name.Contains("Import") || method.Name.Contains("Export") || method.Name.Contains("Find")))
                {
                    Console.WriteLine("SCREENS_METHOD=" + method);
                }
            }

            var foldersProperty = parent.GetType().GetProperty("Folders", BindingFlags.Public | BindingFlags.Instance);
            if (foldersProperty != null)
            {
                var folders = foldersProperty.GetValue(parent, null);
                Console.WriteLine("FOLDERS_COMPOSITION_TYPE=" + folders.GetType().FullName);

                foreach (var method in folders.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.Name.Contains("Create") || method.Name.Contains("Import") || method.Name.Contains("Export") || method.Name.Contains("Find")))
                {
                    Console.WriteLine("FOLDERS_METHOD=" + method);
                }
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
}
