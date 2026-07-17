using System;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

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

            const string folderName = "teste_ia";
            var existing = plc.TagTableGroup.Groups
                .OfType<PlcTagTableUserGroup>()
                .FirstOrDefault(group => string.Equals(group.Name, folderName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                Console.WriteLine("PROJECT=" + projeto.Name);
                Console.WriteLine("PLC=" + plc.Name);
                Console.WriteLine("RESULT=EXISTS");
                Console.WriteLine("FOLDER=" + existing.Name);
                return 0;
            }

            using (tia.ExclusiveAccess("Criar pasta de tags internas teste_ia"))
            {
                var created = plc.TagTableGroup.Groups.Create(folderName);

                Console.WriteLine("PROJECT=" + projeto.Name);
                Console.WriteLine("PLC=" + plc.Name);
                Console.WriteLine("RESULT=CREATED");
                Console.WriteLine("FOLDER=" + created.Name);
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
