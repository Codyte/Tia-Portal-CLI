// NAV INDEX
//   1-30     usings, namespace, Param
//   32-83    BlockInterface.Run — list-interface (pasta inteira num attach, ou --file offline)
//   85-120   FromXml / Describe — núcleo puro: XML → parâmetros
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Tia.Core
{
    /// <summary>Parâmetro de chamada: seção (Input/Output/InOut), nome e tipo.</summary>
    public sealed class Param
    {
        public string Section;
        public string Name;
        public string Datatype;

        public override string ToString() { return Name + " : " + Datatype; }
    }

    /// <summary>
    /// list-interface: assinatura dos blocos de uma pasta em UMA chamada. Sem ele, escrever uma
    /// chamada exigia um `export-block` por FB só para ler Input/Output/InOut — na FP-03 foram 8
    /// exports antes da primeira escrita. É também de onde o <see cref="AddCall"/> tira o tipo de
    /// cada parâmetro.
    /// </summary>
    public static class BlockInterface
    {
        /// <summary>Seções que entram numa chamada. Static/Temp/Constant não são parâmetro.</summary>
        private static readonly string[] CallSections = { "Input", "Output", "InOut" };

        public static object Run(PlcSoftware plc, string name, string folder, string file, string outDir)
        {
            if (!string.IsNullOrEmpty(file))
            {
                var full = Path.GetFullPath(file);
                if (!File.Exists(full)) throw new FileNotFoundException("XML not found: " + full);
                return new Dictionary<string, object>
                {
                    { "count", 1 },
                    { "blocks", new List<object> { Describe(XDocument.Load(full)) } },
                };
            }

            var targets = new List<PlcBlock>();
            if (!string.IsNullOrEmpty(name))
            {
                var block = Ops.FindBlock(plc, name);
                if (block == null) throw new InvalidOperationException("Block '" + name + "' not found.");
                targets.Add(block);
            }
            else
            {
                var group = string.IsNullOrEmpty(folder)
                    ? (PlcBlockGroup)plc.BlockGroup
                    : Ops.ResolveFolder(plc, folder, false);
                Collect(group, targets);
                if (targets.Count == 0)
                    throw new InvalidOperationException("No FB/FC under '" + (folder ?? "Program blocks") + "'.");
            }

            Directory.CreateDirectory(outDir);
            var rows = new List<object>();
            foreach (var block in targets)
            {
                var xml = Path.GetFullPath(Path.Combine(outDir,
                    "iface_" + string.Join("_", block.Name.Split(Path.GetInvalidFileNameChars())) + ".xml"));
                try
                {
                    Ops.ExportFresh(block, xml, ExportOptions.None);
                    rows.Add(Describe(XDocument.Load(xml)));
                }
                catch (Exception ex)
                {
                    // bloco inconsistente derruba o export — não pode levar a pasta inteira junto
                    rows.Add(new Dictionary<string, object> { { "block", block.Name }, { "error", ex.Message } });
                }
            }
            return new Dictionary<string, object> { { "count", rows.Count }, { "blocks", rows } };
        }

        private static void Collect(PlcBlockGroup group, List<PlcBlock> into)
        {
            into.AddRange(group.Blocks.Where(b => b is FB || b is FC));
            foreach (PlcBlockUserGroup sub in group.Groups) Collect(sub, into);
        }

        // ---------- núcleo puro (sem Openness: testável offline) ----------

        /// <summary>Parâmetros de chamada na ordem do XML — é a ordem que o Portal usa na rede.</summary>
        public static List<Param> FromXml(XDocument doc)
        {
            var iface = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Interface");
            var found = new List<Param>();
            if (iface == null) return found;
            foreach (var section in iface.Descendants().Where(e => e.Name.LocalName == "Section"))
            {
                var sec = (string)section.Attribute("Name");
                if (!CallSections.Contains(sec)) continue;
                foreach (var m in section.Elements().Where(e => e.Name.LocalName == "Member"))
                    found.Add(new Param
                    {
                        Section = sec,
                        Name = (string)m.Attribute("Name"),
                        Datatype = (string)m.Attribute("Datatype"),
                    });
            }
            return found;
        }

        public static Dictionary<string, object> Describe(XDocument doc)
        {
            var root = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName.StartsWith("SW."));
            var attrs = root == null ? null
                : root.Elements().FirstOrDefault(e => e.Name.LocalName == "AttributeList");
            var name = attrs == null ? null
                : (string)attrs.Elements().FirstOrDefault(e => e.Name.LocalName == "Name");
            var lang = attrs == null ? null
                : (string)attrs.Elements().FirstOrDefault(e => e.Name.LocalName == "ProgrammingLanguage");
            var ps = FromXml(doc);
            var row = new Dictionary<string, object>
            {
                { "block", name },
                { "kind", root == null ? null : root.Name.LocalName.Replace("SW.Blocks.", "") },
                { "language", lang },
            };
            foreach (string sec in CallSections)
            {
                var list = ps.Where(p => p.Section == sec).Select(p => p.ToString()).ToList();
                if (list.Count > 0) row[sec.ToLowerInvariant()] = list;
            }
            return row;
        }
    }
}
