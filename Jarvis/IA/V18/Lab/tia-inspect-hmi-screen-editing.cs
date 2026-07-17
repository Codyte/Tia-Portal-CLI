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
                .FirstOrDefault(item => string.Equals(item.Name, "HMI_RT_1", StringComparison.OrdinalIgnoreCase));

            if (hmi == null)
            {
                Console.Error.WriteLine("HMI_RT_1 nao encontrada.");
                return 1;
            }

            Console.WriteLine("SCREEN_FOLDER_TYPE=" + hmi.ScreenFolder.GetType().FullName);
            foreach (var property in hmi.ScreenFolder.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Console.WriteLine("SCREEN_FOLDER_PROPERTY=" + property.PropertyType.FullName + "|" + property.Name);
            }

            foreach (var method in hmi.ScreenFolder.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.Name.Contains("Import") || method.Name.Contains("Create") || method.Name.Contains("Export") || method.Name.Contains("GetService")))
            {
                Console.WriteLine("SCREEN_FOLDER_METHOD=" + method);
            }

            var screen = FindScreen(hmi.ScreenFolder, "TEST");
            if (screen == null)
            {
                Console.Error.WriteLine("Tela TEST nao encontrada.");
                return 1;
            }

            Console.WriteLine("SCREEN_TYPE=" + screen.GetType().FullName);
            foreach (var property in screen.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Console.WriteLine("SCREEN_PROPERTY=" + property.PropertyType.FullName + "|" + property.Name);
            }

            foreach (var method in screen.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.Name == "GetService" || method.Name == "Export" || method.Name == "Delete"))
            {
                Console.WriteLine("SCREEN_METHOD=" + method);
            }

            var parent = screen.Parent;
            if (parent != null)
            {
                Console.WriteLine("PARENT_TYPE=" + parent.GetType().FullName);

                foreach (var property in parent.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    Console.WriteLine("PARENT_PROPERTY=" + property.PropertyType.FullName + "|" + property.Name);
                }

                foreach (var method in parent.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.Name == "Create" || method.Name == "Import" || method.Name == "CreateFrom" || method.Name == "GetService"))
                {
                    Console.WriteLine("PARENT_METHOD=" + method);
                }
            }

            var layersProperty = screen.GetType().GetProperty("Layers");
            if (layersProperty == null)
            {
                Console.WriteLine("LAYERS_PROPERTY=NOT_FOUND");
                return 0;
            }

            var layers = layersProperty.GetValue(screen, null) as System.Collections.IEnumerable;
            if (layers == null)
            {
                Console.WriteLine("LAYERS_ENUM=NOT_FOUND");
                return 0;
            }

            foreach (var layer in layers)
            {
                var layerNameProperty = layer.GetType().GetProperty("Name");
                var layerName = layerNameProperty != null ? Convert.ToString(layerNameProperty.GetValue(layer, null)) : string.Empty;

                Console.WriteLine("LAYER=" + layerName + "|TYPE=" + layer.GetType().FullName);

                foreach (var property in layer.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    Console.WriteLine("LAYER_PROPERTY=" + property.PropertyType.FullName + "|" + property.Name);
                }

                var screenItemsProperty = layer.GetType().GetProperty("ScreenItems");
                if (screenItemsProperty == null)
                {
                    continue;
                }

                var composition = screenItemsProperty.GetValue(layer, null);
                if (composition == null)
                {
                    continue;
                }

                Console.WriteLine("SCREEN_ITEMS_TYPE=" + composition.GetType().FullName);

                foreach (var method in composition.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.Name == "Create"))
                {
                    Console.WriteLine("CREATE_METHOD=" + method);
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
