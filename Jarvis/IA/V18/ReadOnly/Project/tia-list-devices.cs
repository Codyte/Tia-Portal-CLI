using System;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi;
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

            Console.WriteLine("PROJECT=" + projeto.Name);

            foreach (var device in projeto.Devices)
            {
                Console.WriteLine("DEVICE=" + device.Name);

                foreach (var item in device.DeviceItems)
                {
                    var container = item.GetService<SoftwareContainer>();
                    if (container == null || container.Software == null)
                    {
                        continue;
                    }

                    if (container.Software is PlcSoftware)
                    {
                        var plc = (PlcSoftware)container.Software;
                        Console.WriteLine("  PLC=" + plc.Name);
                    }
                    else if (container.Software is HmiTarget)
                    {
                        var hmi = (HmiTarget)container.Software;
                        Console.WriteLine("  HMI=" + hmi.Name);
                    }
                    else
                    {
                        Console.WriteLine("  SOFTWARE=" + container.Software.GetType().FullName);
                    }
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
}
