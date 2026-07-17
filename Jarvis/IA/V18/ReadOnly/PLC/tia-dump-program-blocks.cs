using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

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
            Console.WriteLine("PROGRAM_BLOCKS_ROOT=" + plc.BlockGroup.Name);

            DumpSystemGroup(plc.BlockGroup, 0);
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

    private static void DumpSystemGroup(PlcBlockSystemGroup group, int depth)
    {
        Console.WriteLine(new string(' ', depth * 2) + "[SYSTEM_GROUP] " + group.Name);

        foreach (PlcBlock block in group.Blocks)
        {
            Console.WriteLine(new string(' ', (depth + 1) * 2) + "- " + block.Name);
        }

        foreach (PlcBlockUserGroup subgroup in group.Groups)
        {
            DumpUserGroup(subgroup, depth + 1);
        }
    }

    private static void DumpUserGroup(PlcBlockUserGroup group, int depth)
    {
        Console.WriteLine(new string(' ', depth * 2) + "[GROUP] " + group.Name);

        foreach (PlcBlock block in group.Blocks)
        {
            Console.WriteLine(new string(' ', (depth + 1) * 2) + "- " + block.Name);
        }

        foreach (PlcBlockUserGroup subgroup in group.Groups)
        {
            DumpUserGroup(subgroup, depth + 1);
        }
    }
}
