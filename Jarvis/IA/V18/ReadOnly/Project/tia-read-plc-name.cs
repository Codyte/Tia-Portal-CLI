using System;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
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

            var projeto = tia.LocalSessions[0].Project;
            if (projeto == null)
            {
                Console.Error.WriteLine("A sessao local nao contem um projeto valido.");
                return 1;
            }

            var plc = projeto.Devices
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

            Console.WriteLine("PROJECT=" + projeto.Name);
            Console.WriteLine("PLC=" + plc.Name);
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
}
