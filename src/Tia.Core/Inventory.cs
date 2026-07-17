using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Siemens.Engineering.CrossReference;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;

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

        public static object Types(PlcSoftware plc)
        {
            var result = new List<object>();
            CollectTypes(plc.TypeGroup, "", result);
            return result;
        }

        private static void CollectTypes(PlcTypeGroup group, string folder, List<object> into)
        {
            foreach (PlcType type in group.Types)
                into.Add(new Dictionary<string, object>
                {
                    { "folder", folder },
                    { "name", type.Name },
                    { "isConsistent", type.IsConsistent },
                });
            foreach (PlcTypeUserGroup sub in group.Groups)
                CollectTypes(sub, folder.Length == 0 ? sub.Name : folder + "/" + sub.Name, into);
        }

        // ---------- find ----------

        /// <summary>Wildcard search (* ?) over block/table/tag/type names. kind: block|table|tag|type|all.</summary>
        public static object Find(PlcSoftware plc, string pattern, string kind)
        {
            kind = (kind ?? "all").ToLowerInvariant();
            var known = new[] { "all", "block", "table", "tag", "type" };
            if (!known.Contains(kind))
                throw new InvalidOperationException("Unknown --kind '" + kind + "'. Use: " + string.Join("|", known));
            var rx = new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase);
            var hits = new List<object>();

            if (kind == "all" || kind == "block")
                foreach (Dictionary<string, object> b in (List<object>)Blocks(plc))
                    if (rx.IsMatch((string)b["name"]))
                        hits.Add(new Dictionary<string, object>
                            { { "kind", "block" }, { "name", b["name"] }, { "folder", b["folder"] }, { "type", b["type"] } });

            if (kind == "all" || kind == "table" || kind == "tag")
                FindInTagTables(plc.TagTableGroup, "", rx, kind, hits);

            if (kind == "all" || kind == "type")
                foreach (Dictionary<string, object> t in (List<object>)Types(plc))
                    if (rx.IsMatch((string)t["name"]))
                        hits.Add(new Dictionary<string, object>
                            { { "kind", "type" }, { "name", t["name"] }, { "folder", t["folder"] } });

            return new Dictionary<string, object>
            {
                { "pattern", pattern },
                { "kind", kind },
                { "count", hits.Count },
                { "hits", hits },
            };
        }

        private static void FindInTagTables(PlcTagTableGroup group, string folder, Regex rx, string kind, List<object> hits)
        {
            foreach (PlcTagTable table in group.TagTables)
            {
                if ((kind == "all" || kind == "table") && rx.IsMatch(table.Name))
                    hits.Add(new Dictionary<string, object>
                        { { "kind", "table" }, { "name", table.Name }, { "folder", folder } });
                if (kind == "all" || kind == "tag")
                    foreach (PlcTag tag in table.Tags)
                        if (rx.IsMatch(tag.Name))
                            hits.Add(new Dictionary<string, object>
                            {
                                { "kind", "tag" }, { "name", tag.Name }, { "table", table.Name },
                                { "address", tag.LogicalAddress }, { "dataType", tag.DataTypeName },
                            });
            }
            foreach (PlcTagTableUserGroup sub in group.Groups)
                FindInTagTables(sub, folder.Length == 0 ? sub.Name : folder + "/" + sub.Name, rx, kind, hits);
        }

        // ---------- snapshot ----------

        /// <summary>Full read-only inventory: devices + per-PLC blocks, tag tables and UDTs.</summary>
        public static object Snapshot(TiaSession session)
        {
            var plcs = session.Plcs().Select(p => new Dictionary<string, object>
            {
                { "device", p.Key },
                { "plc", p.Value.Name },
                { "blocks", Blocks(p.Value) },
                { "tagTables", TagTables(p.Value) },
                { "types", Types(p.Value) },
            }).ToList();
            return new Dictionary<string, object>
            {
                { "project", session.Project.Name },
                { "devices", Devices(session) },
                { "plcs", plcs },
            };
        }

        // ---------- cross-references ----------

        /// <summary>Cross-references of a block: what it uses and where.</summary>
        public static object Xref(PlcSoftware plc, string name)
        {
            var block = Ops.FindBlock(plc, name);
            if (block == null)
                throw new InvalidOperationException("Block '" + name + "' not found.");
            var service = block.GetService<CrossReferenceService>();
            if (service == null)
                throw new InvalidOperationException("Cross-reference service unavailable for '" + name + "'.");
            var result = service.GetCrossReferences(CrossReferenceFilter.AllObjects);
            var sources = new List<object>();
            foreach (SourceObject src in result.Sources)
            {
                var refs = new List<object>();
                foreach (ReferenceObject r in src.References)
                {
                    var locations = r.Locations.Select(l => (object)new Dictionary<string, object>
                    {
                        { "reference", l.ReferenceLocation },
                        { "address", l.Address },
                        { "access", l.ReferenceType.ToString() },
                    }).ToList();
                    refs.Add(new Dictionary<string, object>
                    {
                        { "name", r.Name },
                        { "typeName", r.TypeName },
                        { "locations", locations },
                    });
                }
                sources.Add(new Dictionary<string, object>
                {
                    { "name", src.Name },
                    { "typeName", src.TypeName },
                    { "references", refs },
                });
            }
            return new Dictionary<string, object> { { "block", name }, { "sources", sources } };
        }
    }
}
