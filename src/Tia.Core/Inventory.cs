using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>Read-only project inventory as plain dictionaries (CLI serializes them to JSON).</summary>
    public static class Inventory
    {
        public static object Info(TiaSession session)
        {
            var plcs = session.Plcs()
                .Select(p => new Dictionary<string, object>
                {
                    { "device", p.Key },
                    { "plc", p.Value.Name },
                }).ToList();
            return new Dictionary<string, object>
            {
                { "project", session.Project.Name },
                { "path", session.Project.Path != null ? session.Project.Path.FullName : null },
                { "plcs", plcs },
                { "devices", session.AllDevices().Count },
            };
        }

        public static object Devices(TiaSession session)
        {
            var result = new List<object>();
            foreach (Device device in session.AllDevices())
            {
                var items = new List<object>();
                CollectDeviceItems(device.DeviceItems, items);
                result.Add(new Dictionary<string, object>
                {
                    { "device", device.Name },
                    { "typeIdentifier", device.TypeIdentifier },
                    { "items", items },
                });
            }
            return result;
        }

        private static void CollectDeviceItems(DeviceItemComposition items, List<object> into)
        {
            foreach (DeviceItem item in items)
            {
                var software = item.GetService<SoftwareContainer>()?.Software;
                into.Add(new Dictionary<string, object>
                {
                    { "name", item.Name },
                    { "typeIdentifier", item.TypeIdentifier },
                    { "software", software != null ? software.GetType().Name : null },
                });
                CollectDeviceItems(item.DeviceItems, into);
            }
        }

        public static object Blocks(PlcSoftware plc)
        {
            var result = new List<object>();
            CollectBlocks(plc.BlockGroup, "", result);
            return result;
        }

        private static void CollectBlocks(PlcBlockGroup group, string folder, List<object> into)
        {
            foreach (PlcBlock block in group.Blocks)
                into.Add(new Dictionary<string, object>
                {
                    { "folder", folder },
                    { "name", block.Name },
                    { "type", block.GetType().Name },
                    { "number", block.Number },
                    { "language", block.ProgrammingLanguage.ToString() },
                    { "isConsistent", block.IsConsistent },
                });
            foreach (PlcBlockUserGroup sub in group.Groups)
                CollectBlocks(sub, folder.Length == 0 ? sub.Name : folder + "/" + sub.Name, into);
        }

        public static object TagTables(PlcSoftware plc)
        {
            var result = new List<object>();
            CollectTagTables(plc.TagTableGroup, "", result);
            return result;
        }

        private static void CollectTagTables(PlcTagTableGroup group, string folder, List<object> into)
        {
            foreach (PlcTagTable table in group.TagTables)
                into.Add(new Dictionary<string, object>
                {
                    { "folder", folder },
                    { "table", table.Name },
                    { "tagCount", table.Tags.Count },
                });
            foreach (PlcTagTableUserGroup sub in group.Groups)
                CollectTagTables(sub, folder.Length == 0 ? sub.Name : folder + "/" + sub.Name, into);
        }
    }
}
