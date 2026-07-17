using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
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

            var project = tia.LocalSessions[0].Project;
            if (project == null)
            {
                Console.Error.WriteLine("A sessao local nao contem um projeto valido.");
                return 1;
            }

            Console.WriteLine("PROJECT=" + project.Name);
            DumpCompilable("PROJECT", project.GetService<ICompilable>());

            foreach (var item in project.Devices.SelectMany(device => device.DeviceItems))
            {
                var container = item.GetService<SoftwareContainer>();
                if (container == null || container.Software == null)
                {
                    continue;
                }

                if (container.Software is PlcSoftware plc)
                {
                    DumpCompilable("PLC:" + plc.Name, plc.GetService<ICompilable>());
                }
                else if (container.Software is HmiTarget hmi)
                {
                    DumpCompilable("HMI:" + hmi.Name, hmi.GetService<ICompilable>());
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

    private static void DumpCompilable(string scope, ICompilable compilable)
    {
        Console.WriteLine("SCOPE=" + scope);

        if (compilable == null)
        {
            Console.WriteLine("COMPILABLE=NULL");
            return;
        }

        Console.WriteLine("COMPILABLE_TYPE=" + compilable.GetType().FullName);

        var attributes = compilable.GetAttributeInfos();
        if (attributes == null || attributes.Count == 0)
        {
            Console.WriteLine("ATTRIBUTES=NONE");
            return;
        }

        foreach (var attribute in attributes)
        {
            Console.WriteLine("ATTRIBUTE=" + attribute.Name + "|" + attribute.AccessMode);
        }
    }
}
