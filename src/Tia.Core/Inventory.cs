// NAV INDEX
// 29-78    Info / Devices (device items recursivos)
// 79-108   Blocks
// 109-180  Tree — outline do PLC (blocos + tabelas + UDTs) → plc-navi.md; leitura de orientação
// 181-223  TagTables / Types
// 224-280  find — wildcard sobre nomes de bloco, tabela, tag e UDT
// 281-333  snapshot — inventário completo (volume bruto, usar com --out-file)
// 334-381  ResolveSymbol / FindTag / xref — resolve nome → símbolo, cross-references
// 382-445  trace — símbolos de um equipamento + quem referencia (xref reverso)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Siemens.Engineering;
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

        /// <summary>
        /// navindex-style outline do PLC inteiro: blocos, tabelas de tag e UDTs, uma seção
        /// "## seção · pasta (n)" por pasta com os itens inline. É a leitura de orientação do
        /// agente — cabeçalho de pasta uma vez em vez de chave repetida por item, ~4,5x menor
        /// que o JSON equivalente (476 blocos: 117 KB em JSON, 26 KB aqui). O volume bruto fica
        /// no `snapshot`/`find --out-file`, que ninguém lê inteiro.
        /// </summary>
        public static object Tree(PlcSoftware plc, string outFile)
        {
            var body = new StringBuilder();
            var stats = new int[2]; // [0] folders with content, [1] blocks
            AppendTree(plc.BlockGroup, "", body, stats);

            var tables = ((List<object>)TagTables(plc)).Cast<Dictionary<string, object>>().ToList();
            var types = ((List<object>)Types(plc)).Cast<Dictionary<string, object>>().ToList();
            AppendGrouped(body, "tag tables", tables, d => d["table"] + "(" + d["tagCount"] + ")");
            AppendGrouped(body, "UDTs", types, d => (string)d["name"]);

            var sb = new StringBuilder();
            sb.AppendLine("# __navi__ · PLC " + plc.Name + " — " + stats[1] + " blocks in "
                + stats[0] + " folders · " + tables.Count + " tag tables · " + types.Count + " UDTs");
            sb.AppendLine("<!-- generated by `tia tree` · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " -->");
            sb.AppendLine();
            sb.Append(body);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile)));
            File.WriteAllText(outFile, sb.ToString());

            return new Dictionary<string, object>
            {
                { "plc", plc.Name },
                { "folders", stats[0] },
                { "blocks", stats[1] },
                { "tagTables", tables.Count },
                { "types", types.Count },
                { "file", Path.GetFullPath(outFile) },
            };
        }

        /// <summary>Seção agrupada por pasta, itens inline — mesmo formato dos blocos.</summary>
        private static void AppendGrouped(StringBuilder body, string section,
            List<Dictionary<string, object>> items, Func<Dictionary<string, object>, string> label)
        {
            foreach (var g in items.GroupBy(d => (string)d["folder"]))
            {
                body.AppendLine("## " + section + " · " + (g.Key.Length == 0 ? "(root)" : g.Key)
                    + " (" + g.Count() + ")");
                body.AppendLine(string.Join("  ", g.Select(label)));
                body.AppendLine();
            }
        }

        private static void AppendTree(PlcBlockGroup group, string path, StringBuilder body, int[] stats)
        {
            var blocks = group.Blocks.Cast<PlcBlock>().ToList();
            if (blocks.Any())
            {
                stats[0]++;
                stats[1] += blocks.Count;
                body.AppendLine("## blocks · " + (path.Length == 0 ? "(root)" : path) + " (" + blocks.Count + ")");
                body.AppendLine(string.Join("  ", blocks.Select(BlockLabel)));
                body.AppendLine();
            }
            else if (path.Length > 0 && !group.Groups.Cast<PlcBlockUserGroup>().Any())
            {
                stats[0]++;
                body.AppendLine("## blocks · " + path + " (0)");
                body.AppendLine();
            }
            foreach (PlcBlockUserGroup sub in group.Groups)
                AppendTree(sub, path.Length == 0 ? sub.Name : path + "/" + sub.Name, body, stats);
        }

        private static string BlockLabel(PlcBlock b)
        {
            string t = b.GetType().Name;
            t = t == "GlobalDB" ? "DB" : t == "InstanceDB" ? "iDB" : t;
            return b.Name + "(" + t + b.Number + ")";
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

        /// <summary>
        /// Resolve um símbolo pelo nome, na ordem bloco → tag → tabela → UDT. Todos expõem
        /// <c>CrossReferenceService</c>, então o xref serve tanto o sentido direto (bloco → o que
        /// usa) quanto o reverso (tag → quem a usa). Null se o nome não existir no PLC.
        /// </summary>
        private static IEngineeringServiceProvider ResolveSymbol(PlcSoftware plc, string name, out string kind)
        {
            var block = Ops.FindBlock(plc, name);
            if (block != null) { kind = "block"; return block; }
            var tag = FindTag(plc.TagTableGroup, name);
            if (tag != null) { kind = "tag"; return tag; }
            var table = Ops.FindTagTable(plc.TagTableGroup, name);
            if (table != null) { kind = "table"; return table; }
            var type = Ops.FindType(plc.TypeGroup, name);
            if (type != null) { kind = "type"; return type; }
            kind = null;
            return null;
        }

        private static PlcTag FindTag(PlcTagTableGroup group, string name)
        {
            foreach (PlcTagTable table in group.TagTables)
                foreach (PlcTag tag in table.Tags)
                    if (string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)) return tag;
            foreach (PlcTagTableUserGroup sub in group.Groups)
            {
                var hit = FindTag(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>Cross-references of a block, tag, tag table or UDT: what it uses / who uses it.</summary>
        public static object Xref(PlcSoftware plc, string name)
        {
            string kind;
            var target = ResolveSymbol(plc, name, out kind);
            if (target == null)
                throw new InvalidOperationException("Symbol '" + name + "' not found (block, tag, table or type).");
            var service = target.GetService<CrossReferenceService>();
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
            return new Dictionary<string, object> { { "block", name }, { "kind", kind }, { "sources", sources } };
        }

        // ---------- trace ----------

        /// <summary>
        /// Vizinhança semântica de um equipamento ("AG-01") em uma chamada: símbolos cujo nome
        /// contém o termo (tags com endereço, blocos, tabelas, UDTs) + quem referencia cada um.
        /// O Openness só dá xref no sentido direto (objeto → o que ele usa), então o lado reverso
        /// é uma varredura dos blocos de lógica. Medido em projeto real (476 blocos, 131 com
        /// lógica, 4372 tags): 3,3s de xref, 10s com o attach. Não precisa de cache.
        /// </summary>
        public static object Trace(PlcSoftware plc, string equipment)
        {
            var symbols = (Dictionary<string, object>)Find(plc, "*" + equipment + "*", "all");
            var rx = new Regex(Regex.Escape(equipment).Replace(@"\*", ".*"), RegexOptions.IgnoreCase);
            var started = DateTime.Now;

            var usedBy = new List<object>();
            int scanned;
            foreach (var src in AllSources(plc, out scanned))
                foreach (ReferenceObject r in src.References)
                {
                    if (r.Name == null || !rx.IsMatch(r.Name)) continue;
                    var at = r.Locations.Select(l => l.ReferenceLocation).Distinct().ToList();
                    usedBy.Add(new Dictionary<string, object>
                    {
                        { "block", src.Name },
                        { "blockType", src.TypeName },
                        { "uses", r.Name },
                        { "typeName", r.TypeName },
                        { "count", at.Count },
                        { "at", at.Take(8).ToList() },
                    });
                }

            return new Dictionary<string, object>
            {
                { "equipment", equipment },
                { "symbols", symbols["hits"] },
                { "symbolCount", symbols["count"] },
                { "usedBy", usedBy },
                { "blocksScanned", scanned },
                { "seconds", Math.Round((DateTime.Now - started).TotalSeconds, 1) },
            };
        }

        /// <summary>
        /// Todos os SourceObject do programa, bloco a bloco. `scanned` = blocos varridos.
        /// (V21 não expõe CrossReferenceService no BlockGroup — só nos objetos folha.)
        /// </summary>
        private static IEnumerable<SourceObject> AllSources(PlcSoftware plc, out int scanned)
        {
            var blocks = new List<PlcBlock>();
            CollectBlockObjects(plc.BlockGroup, blocks);
            scanned = 0;
            var sources = new List<SourceObject>();
            foreach (var block in blocks)
            {
                var service = block.GetService<CrossReferenceService>();
                if (service == null) continue;
                scanned++;
                sources.AddRange(service.GetCrossReferences(CrossReferenceFilter.AllObjects).Sources.Cast<SourceObject>());
            }
            return sources;
        }

        private static void CollectBlockObjects(PlcBlockGroup group, List<PlcBlock> into)
        {
            foreach (PlcBlock block in group.Blocks)
                if (!(block is DataBlock)) into.Add(block); // DB não tem lógica: xref só custa tempo
            foreach (PlcBlockUserGroup sub in group.Groups)
                CollectBlockObjects(sub, into);
        }
    }
}
