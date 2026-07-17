using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

internal static class Program
{
    private const string FolderName = "teste_ia";
    private const string TableName = "teste_ia_basicas";

    private static readonly TagDefinition[] Tags =
    {
        new TagDefinition("Jarvis_Test_Bool", "Bool", "%M200.0"),
        new TagDefinition("Jarvis_Test_Word", "Word", "%MW202"),
        new TagDefinition("Jarvis_Test_Int", "Int", "%MW204")
    };

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

            var plc = project.Devices
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

            var results = new List<string>();

            using (tia.ExclusiveAccess("Criar pasta, tabela e tags de teste no CLP"))
            {
                var folder = plc.TagTableGroup.Groups
                    .OfType<PlcTagTableUserGroup>()
                    .FirstOrDefault(group => string.Equals(group.Name, FolderName, StringComparison.OrdinalIgnoreCase));

                if (folder == null)
                {
                    folder = plc.TagTableGroup.Groups.Create(FolderName);
                    results.Add("FOLDER=CREATED:" + folder.Name);
                }
                else
                {
                    results.Add("FOLDER=EXISTS:" + folder.Name);
                }

                var table = folder.TagTables
                    .OfType<PlcTagTable>()
                    .FirstOrDefault(item => string.Equals(item.Name, TableName, StringComparison.OrdinalIgnoreCase));

                if (table == null)
                {
                    table = folder.TagTables.Create(TableName);
                    results.Add("TABLE=CREATED:" + table.Name);
                }
                else
                {
                    results.Add("TABLE=EXISTS:" + table.Name);
                }

                foreach (var definition in Tags)
                {
                    var existingTag = table.Tags
                        .OfType<PlcTag>()
                        .FirstOrDefault(tag => string.Equals(tag.Name, definition.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingTag != null)
                    {
                        results.Add("TAG=EXISTS:" + existingTag.Name + ":" + existingTag.DataTypeName + ":" + existingTag.LogicalAddress);
                        continue;
                    }

                    var createdTag = table.Tags.Create(definition.Name, definition.DataTypeName, definition.LogicalAddress);
                    createdTag.ExternalAccessible = true;
                    createdTag.ExternalVisible = true;
                    createdTag.ExternalWritable = true;

                    results.Add("TAG=CREATED:" + createdTag.Name + ":" + createdTag.DataTypeName + ":" + createdTag.LogicalAddress);
                }
            }

            Console.WriteLine("PROJECT=" + project.Name);
            Console.WriteLine("PLC=" + plc.Name);
            Console.WriteLine("FOLDER=" + FolderName);
            Console.WriteLine("TABLE=" + TableName);

            foreach (var line in results)
            {
                Console.WriteLine(line);
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

    private sealed class TagDefinition
    {
        public TagDefinition(string name, string dataTypeName, string logicalAddress)
        {
            Name = name;
            DataTypeName = dataTypeName;
            LogicalAddress = logicalAddress;
        }

        public string Name { get; private set; }
        public string DataTypeName { get; private set; }
        public string LogicalAddress { get; private set; }
    }
}
