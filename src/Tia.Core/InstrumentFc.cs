using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>Config for replicate-instruments (port of "Replicador de FC Instrumentos").</summary>
    public class InstrumentFcConfig
    {
        /// <summary>Tag folder whose subfolders are plant areas holding instrument tags.</summary>
        public string SourceTagsFolder { get; set; }
        /// <summary>Block folder that receives one subfolder + FC per area.</summary>
        public string TargetBlocksFolder { get; set; }
        public string GlobalDb { get; set; } = "DB GLOBAL";
        /// <summary>OB that calls every generated FC; skipped when empty.</summary>
        public string TargetOb { get; set; }
        /// <summary>
        /// Sufixo do FC gerado por área. O projeto de referência tem duas famílias:
        /// "5.1 Aferição Analógica" → PRELIMINAR_ANALOGS, "5.2 Totalizadores" →
        /// PRELIMINAR_TOTALIZADOR. O script FINAL hardcoda "_ANALOGS" (linha 961) e geraria
        /// o nome errado — e uma chamada duplicada no OB — ao rodar sobre os totalizadores.
        /// </summary>
        public string FcSuffix { get; set; } = "_ANALOGS";
        public List<string> IgnoreFolders { get; set; } = new List<string>();
        /// <summary>Only tags whose name contains one of these substrings; empty = all.</summary>
        public List<string> TagFilters { get; set; } = new List<string>();
        /// <summary>Next free command number per kind (defaults mirror the original script).</summary>
        public Dictionary<string, int> NextCommandIds { get; set; } = new Dictionary<string, int>
        {
            { "Afericao", 100 }, { "Limites", 200 },
        };
    }

    /// <summary>
    /// Replicates the template FC's networks once per instrument of each area: instance DBs,
    /// global-DB paths, tag prefixes and the 8888/9999 command placeholders are rewired, one FC
    /// per area is imported and the call OB updated (port of "Replicador de FC Instrumentos").
    /// ponytail: template must be an FC inside TargetBlocksFolder (first found, natural order) —
    /// the original's on-disk contingency mold is dropped; add back if offline runs become a need.
    /// </summary>
    public static class InstrumentFc
    {
        private static int _uid = 20000; // single-shot CLI process (D9)
        private static string[] _cultures = { "en-US" }; // set per-run in Run

        private static readonly XNamespace FlgNs =
            "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v5";
        private static readonly XNamespace IntfNs =
            "http://www.siemens.com/automation/Openness/SW/Interface/v5";

        internal class Instrument
        {
            public string Id;
            public string GlobalDbPath;
            public int TagCount;
            public int CmdAfericao;
            public int CmdLimites;
            public List<string> InstanceDbs = new List<string>();
        }

        internal class AreaTask
        {
            public string AreaName;
            public string TargetFolderName;
            public string TargetFcName;
            public List<Instrument> Instruments = new List<Instrument>();
        }

        public static object Run(TiaSession session, PlcSoftware plc, InstrumentFcConfig config, string outDir, bool apply)
        {
            // an XML MultilingualTextItem whose culture the project doesn't have fails the whole import
            _cultures = session.Project.LanguageSettings.ActiveLanguages
                .Select(l => l.Culture.Name).ToArray();
            if (_cultures.Length == 0) _cultures = new[] { "en-US" };

            if (string.IsNullOrEmpty(config?.SourceTagsFolder))
                throw new ArgumentException("Config must set 'SourceTagsFolder'.");
            if (string.IsNullOrEmpty(config.TargetBlocksFolder))
                throw new ArgumentException("Config must set 'TargetBlocksFolder'.");

            var warnings = new List<string>();
            Directory.CreateDirectory(outDir);
            var natural = new NaturalStringComparer();

            // global DB map
            var globalDb = ReplicateFc.FindDataBlock(plc.BlockGroup, config.GlobalDb);
            if (globalDb == null)
                throw new InvalidOperationException("Global DB '" + config.GlobalDb + "' not found.");
            string dbXmlPath = Path.GetFullPath(Path.Combine(outDir, "instr_globaldb_cache.xml"));
            if (File.Exists(dbXmlPath)) File.Delete(dbXmlPath);
            globalDb.Export(new FileInfo(dbXmlPath), ExportOptions.None);
            var dbXml = XDocument.Load(dbXmlPath);

            // template = first FC found under the destination tree
            var destRoot = Ops.FindGroup(plc.BlockGroup, config.TargetBlocksFolder);
            if (destRoot == null)
                throw new InvalidOperationException("Target folder '" + config.TargetBlocksFolder + "' not found.");
            // root first — sorting it with descendants let generated area folders win on re-runs
            var searchFolders = ReplicateFc.DescendantGroups(destRoot)
                .OrderBy(f => f.Name, natural).ToList();
            searchFolders.Insert(0, destRoot);
            FC templateFc = null;
            foreach (var folder in searchFolders)
            {
                templateFc = folder.Blocks.OfType<FC>().FirstOrDefault();
                if (templateFc != null) break;
            }
            if (templateFc == null)
                throw new InvalidOperationException(
                    "No template FC found under '" + config.TargetBlocksFolder + "'.");
            string templatePath = Path.GetFullPath(Path.Combine(outDir, "instr_template_cache.xml"));
            if (File.Exists(templatePath)) File.Delete(templatePath);
            templateFc.Export(new FileInfo(templatePath), ExportOptions.None);
            var templateXml = XDocument.Load(templatePath);

            // discovery: areas -> instruments (+ command numbering)
            var sourceRoot = Ops.FindTagGroup(plc.TagTableGroup, config.SourceTagsFolder);
            if (sourceRoot == null)
                throw new InvalidOperationException("Tag folder '" + config.SourceTagsFolder + "' not found.");
            var nextCmd = new Dictionary<string, int>(config.NextCommandIds);
            if (!nextCmd.ContainsKey("Afericao")) nextCmd["Afericao"] = 100;
            if (!nextCmd.ContainsKey("Limites")) nextCmd["Limites"] = 200;

            var tasks = new List<AreaTask>();
            var commandMap = new List<(int Id, string Equipment, string Kind)>();
            var areaFolders = sourceRoot.Groups.Cast<PlcTagTableUserGroup>()
                .Where(g => !config.IgnoreFolders.Any(i => g.Name.Equals(i, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(g => !g.Name.Contains("Preliminar")).ThenBy(g => g.Name, natural)
                .ToList();

            foreach (var areaFolder in areaFolders)
            {
                var task = new AreaTask
                {
                    AreaName = GetBaseName(areaFolder.Name),
                    TargetFolderName = TargetFolderName(areaFolder.Name, config.TargetBlocksFolder),
                };
                task.TargetFcName = FcName(task.AreaName, config.FcSuffix);

                var tags = CollectTags(areaFolder);
                if (config.TagFilters.Any())
                    tags = tags.Where(t => config.TagFilters.Any(f =>
                        t.Name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

                var instrumentGroups = tags
                    .Select(t => new { Tag = t, Id = ExtractId(t.Name) })
                    .Where(x => !string.IsNullOrEmpty(x.Id))
                    .GroupBy(x => x.Id)
                    .OrderBy(g => g.Key != "FQIT-01").ThenBy(g => g.Key, natural);

                foreach (var group in instrumentGroups)
                {
                    string path;
                    try { path = FindPathInDbXml(dbXml, group.Key); }
                    catch { continue; } // instrument absent from the global DB
                    int cmdA = nextCmd["Afericao"]++;
                    int cmdL = nextCmd["Limites"]++;
                    task.Instruments.Add(new Instrument
                    {
                        Id = group.Key,
                        GlobalDbPath = path,
                        TagCount = group.Count(),
                        CmdAfericao = cmdA,
                        CmdLimites = cmdL,
                    });
                    commandMap.Add((cmdA, group.Key, "Aferição"));
                    commandMap.Add((cmdL, group.Key, "Limites"));
                }
                if (task.Instruments.Any()) tasks.Add(task);
            }

            // mold instrument = the known instrument most referenced inside the template XML
            var componentNames = new HashSet<string>(templateXml.Descendants(FlgNs + "Component")
                .Select(c => c.Attribute("Name")?.Value).Where(n => !string.IsNullOrEmpty(n)));
            Instrument source = null;
            int best = 0;
            foreach (var instrument in tasks.SelectMany(t => t.Instruments))
            {
                int count = componentNames.Count(n => n.Contains(instrument.Id));
                if (count > best) { best = count; source = instrument; }
            }
            if (source == null)
                throw new InvalidOperationException(
                    "Could not identify the template's mold instrument — the template FC must reference " +
                    "at least one instrument that exists in the source tag folders and the global DB.");

            // instance DB names each instrument will need
            var fbCalls = templateXml.Descendants(FlgNs + "CallInfo")
                .Where(ci => ci.Attribute("BlockType")?.Value == "FB").ToList();
            foreach (var instrument in tasks.SelectMany(t => t.Instruments))
                foreach (var call in fbCalls)
                {
                    string instanceName = call.Element(FlgNs + "Instance")?.Element(FlgNs + "Component")?.Attribute("Name")?.Value;
                    if (!string.IsNullOrEmpty(instanceName) && instanceName.Contains(source.Id))
                        instrument.InstanceDbs.Add(instanceName.Replace(source.Id, instrument.Id));
                }

            // build + (apply) import per area
            var areas = new List<object>();
            var obCalls = new List<(string Fc, string Area)>();
            foreach (var task in tasks)
            {
                bool complete = IsTaskComplete(plc, task);
                obCalls.Add((task.TargetFcName, task.AreaName));
                string action = complete ? "in-sync" : "generate";
                string xmlPath = null;

                if (!complete)
                {
                    xmlPath = BuildAreaFcXml(templateXml, task, source, config, outDir);
                    if (apply)
                    {
                        var targetSubFolder = destRoot.Groups.Find(task.TargetFolderName)
                            ?? destRoot.Groups.Create(task.TargetFolderName);
                        ImportAreaFc(plc, targetSubFolder, task, xmlPath, warnings, out action);
                    }
                    else
                    {
                        action = "generate";
                    }
                }

                areas.Add(new Dictionary<string, object>
                {
                    { "area", task.AreaName },
                    { "fc", task.TargetFcName },
                    { "folder", config.TargetBlocksFolder + "/" + task.TargetFolderName },
                    { "instruments", task.Instruments.Select(i => new Dictionary<string, object>
                        {
                            { "id", i.Id },
                            { "tags", i.TagCount },
                            { "dbPath", i.GlobalDbPath },
                            { "cmdAfericao", i.CmdAfericao },
                            { "cmdLimites", i.CmdLimites },
                            { "instanceDbs", i.InstanceDbs },
                        }).ToList() },
                    { "action", action },
                    { "xml", xmlPath },
                });
            }

            // call OB
            object callOb = null;
            if (!string.IsNullOrWhiteSpace(config.TargetOb))
                callOb = UpdateCallOb(plc, config, destRoot, obCalls, outDir, apply, warnings);

            string csvPath = WriteCommandCsv(commandMap, outDir);

            return new Dictionary<string, object>
            {
                { "template", templateFc.Name },
                { "moldInstrument", source.Id },
                { "applied", apply },
                { "areas", areas },
                { "callOb", callOb },
                { "csv", csvPath },
                { "warnings", warnings },
            };
        }

        // ---------- generation ----------

        internal static string BuildAreaFcXml(XDocument templateXml, AreaTask task, Instrument source,
            InstrumentFcConfig config, string outDir)
        {
            var doc = new XDocument(templateXml);
            doc.Descendants("AttributeList").Elements("Name").First().Value = task.TargetFcName;
            var objectList = doc.Descendants("ObjectList").First();
            var networkTemplates = templateXml.Descendants("SW.Blocks.CompileUnit").ToList();
            objectList.Elements("SW.Blocks.CompileUnit").Remove();
            if (!networkTemplates.Any())
                throw new InvalidOperationException("Template FC XML contains no networks ('SW.Blocks.CompileUnit').");

            var natural = new NaturalStringComparer();
            foreach (var instrument in task.Instruments.OrderBy(i => i.Id, natural))
                foreach (var networkTemplate in networkTemplates)
                {
                    var network = new XElement(networkTemplate);
                    ReassignUids(network);
                    RewireNetwork(network, source, instrument, config);
                    objectList.Add(network);
                }

            string path = Path.GetFullPath(Path.Combine(outDir, task.TargetFcName + ".xml"));
            if (File.Exists(path)) File.Delete(path);
            doc.Save(path);
            return path;
        }

        /// <summary>Swaps IDs, 8888/9999 command placeholders, FB instances and global-DB symbol paths.</summary>
        private static void RewireNetwork(XElement network, Instrument source, Instrument target,
            InstrumentFcConfig config)
        {
            foreach (var node in network.DescendantsAndSelf())
            {
                if (node.Name.LocalName == "Text")
                {
                    node.Value = node.Value
                        .Replace("(" + source.Id + ")", "(" + target.Id + ")")
                        .Replace("8888", target.CmdAfericao.ToString())
                        .Replace("9999", target.CmdLimites.ToString());
                }
                else if (node.Name.LocalName == "ConstantValue")
                {
                    if (node.Value.Trim() == "8888") node.Value = target.CmdAfericao.ToString();
                    else if (node.Value.Trim() == "9999") node.Value = target.CmdLimites.ToString();
                }
            }

            foreach (var callInfo in network.Descendants(FlgNs + "CallInfo"))
            {
                var nameAttr = callInfo.Element(FlgNs + "Instance")?.Element(FlgNs + "Component")?.Attribute("Name");
                if (nameAttr != null && nameAttr.Value.Contains(source.Id))
                    nameAttr.Value = nameAttr.Value.Replace(source.Id, target.Id);
            }

            foreach (var symbol in network.Descendants(FlgNs + "Symbol"))
            {
                var components = symbol.Elements(FlgNs + "Component").ToList();
                var first = components.FirstOrDefault();
                if (first == null) continue;
                if (first.Attribute("Name")?.Value.Equals(config.GlobalDb, StringComparison.OrdinalIgnoreCase) == true)
                {
                    var fullPath = string.Join(".", components.Select(c => "\"" + c.Attribute("Name").Value + "\""));
                    if (fullPath.IndexOf(source.GlobalDbPath, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var newPath = fullPath.Replace(source.GlobalDbPath, target.GlobalDbPath);
                    symbol.Elements().Remove();
                    foreach (var part in newPath.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim('"')))
                        symbol.Add(new XElement(FlgNs + "Component", new XAttribute("Name", part)));
                }
                else
                {
                    var nameAttr = first.Attribute("Name");
                    if (nameAttr != null && nameAttr.Value.StartsWith(source.Id, StringComparison.OrdinalIgnoreCase))
                        nameAttr.Value = target.Id + nameAttr.Value.Substring(source.Id.Length);
                }
            }
        }

        private static void ImportAreaFc(PlcSoftware plc, PlcBlockUserGroup targetSubFolder,
            AreaTask task, string xmlPath, List<string> warnings, out string action)
        {
            // instance DBs first — the FC references them
            var doc = XDocument.Load(xmlPath);
            var fbCallsInFinal = doc.Descendants()
                .Where(x => x.Name.LocalName == "CallInfo" && (string)x.Attribute("BlockType") == "FB")
                .Select(x => new
                {
                    FbName = (string)x.Attribute("Name"),
                    InstanceDb = x.Element(x.Name.Namespace + "Instance")
                        ?.Element(x.Name.Namespace + "Component")?.Attribute("Name")?.Value,
                })
                .Where(x => !string.IsNullOrEmpty(x.FbName) && !string.IsNullOrEmpty(x.InstanceDb))
                .Distinct().ToList();
            foreach (var call in fbCallsInFinal)
            {
                try
                {
                    if (Ops.FindBlock(plc, call.InstanceDb) != null) continue;
                    targetSubFolder.Blocks.CreateInstanceDB(call.InstanceDb, true, 1, call.FbName);
                }
                catch (Exception ex)
                {
                    warnings.Add("Failed to create instance DB '" + call.InstanceDb + "': " +
                        ex.GetBaseException().Message);
                }
            }

            var existing = targetSubFolder.Blocks.Find(task.TargetFcName) as FC;
            if (existing != null && BlocksIdentical(existing, xmlPath))
            {
                action = "in-sync";
                return;
            }
            existing?.Delete();
            targetSubFolder.Blocks.Import(new FileInfo(xmlPath), ImportOptions.None);
            action = existing != null ? "updated" : "created";
        }

        // ---------- call OB ----------

        private static object UpdateCallOb(PlcSoftware plc, InstrumentFcConfig config,
            PlcBlockUserGroup destRoot, List<(string Fc, string Area)> calls, string outDir,
            bool apply, List<string> warnings)
        {
            var targetOb = FindOb(plc.BlockGroup, config.TargetOb);
            XDocument obDoc;
            if (targetOb != null)
            {
                string cache = Path.GetFullPath(Path.Combine(outDir, config.TargetOb + "_ob_cache.xml"));
                if (File.Exists(cache)) File.Delete(cache);
                targetOb.Export(new FileInfo(cache), ExportOptions.None);
                obDoc = XDocument.Load(cache);
            }
            else
            {
                obDoc = XDocument.Parse(EmptyObXml(config.TargetOb));
            }

            var objectList = obDoc.Descendants("ObjectList").FirstOrDefault();
            if (objectList == null)
            {
                obDoc.Root.Add(new XElement("ObjectList"));
                objectList = obDoc.Root.Element("ObjectList");
            }
            var existingCalls = new HashSet<string>(obDoc.Descendants(FlgNs + "CallInfo")
                .Where(ci => ci.Attribute("BlockType")?.Value == "FC")
                .Select(ci => ci.Attribute("Name")?.Value)
                .Where(n => !string.IsNullOrEmpty(n)));

            var added = new List<string>();
            foreach (var call in calls)
                if (!existingCalls.Contains(call.Fc))
                {
                    objectList.Add(CallNetworkXml(call.Fc, call.Area));
                    added.Add(call.Fc);
                }

            string xmlPath = Path.GetFullPath(Path.Combine(outDir, config.TargetOb + "_final.xml"));
            if (File.Exists(xmlPath)) File.Delete(xmlPath);
            obDoc.Save(xmlPath);

            if (apply && added.Any())
            {
                var parent = targetOb != null
                    ? (targetOb.Parent as PlcBlockGroup ?? plc.BlockGroup)
                    : (PlcBlockGroup)destRoot;
                if (targetOb != null)
                {
                    targetOb.Delete();
                    // original forced a sync compile between delete and import; we import directly —
                    // run `tia compile --apply` afterwards (deviation: no auto-compile in verbs)
                }
                parent.Blocks.Import(new FileInfo(xmlPath), ImportOptions.None);
            }

            return new Dictionary<string, object>
            {
                { "ob", config.TargetOb },
                { "existingCalls", existingCalls.Count },
                { "addedCalls", added },
                { "xml", xmlPath },
            };
        }

        private static XElement CallNetworkXml(string fcName, string areaName)
        {
            string title = "Chamada Bloco Aferição Analógica " + areaName;
            return new XElement("SW.Blocks.CompileUnit",
                new XAttribute("ID", (++_uid).ToString("X")), new XAttribute("CompositionName", "CompileUnits"),
                new XElement("AttributeList",
                    new XElement("NetworkSource",
                        new XElement(FlgNs + "FlgNet",
                            new XElement(FlgNs + "Parts",
                                new XElement(FlgNs + "Call", new XAttribute("UId", (++_uid).ToString()),
                                    new XElement(FlgNs + "CallInfo",
                                        new XAttribute("Name", fcName), new XAttribute("BlockType", "FC")))),
                            new XElement(FlgNs + "Wires",
                                new XElement(FlgNs + "Wire", new XAttribute("UId", (++_uid).ToString()),
                                    new XElement(FlgNs + "Powerrail"),
                                    new XElement(FlgNs + "NameCon", new XAttribute("UId", (_uid - 1).ToString()),
                                        new XAttribute("Name", "en")))))),
                    new XElement("ProgrammingLanguage", "LAD")),
                new XElement("ObjectList",
                    new XElement("MultilingualText", new XAttribute("ID", (++_uid).ToString("X")),
                        new XAttribute("CompositionName", "Comment"), new XElement("ObjectList")),
                    new XElement("MultilingualText", new XAttribute("ID", (++_uid).ToString("X")),
                        new XAttribute("CompositionName", "Title"),
                        new XElement("ObjectList",
                            _cultures.Select(c => new XElement("MultilingualTextItem",
                                new XAttribute("ID", (++_uid).ToString("X")),
                                new XAttribute("CompositionName", "Items"),
                                new XElement("AttributeList",
                                    new XElement("Culture", c), new XElement("Text", title))))))));
        }

        private static string EmptyObXml(string obName)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Document>\n" +
                "  <Engineering version=\"V19\" />\n" +
                "  <SW.Blocks.OB ID=\"0\">\n" +
                "    <AttributeList>\n" +
                "      <Interface>\n" +
                "        <Sections xmlns=\"http://www.siemens.com/automation/Openness/SW/Interface/v5\">\n" +
                "          <Section Name=\"Input\">\n" +
                "            <Member Name=\"Initial_Call\" Datatype=\"Bool\" Informative=\"true\" />\n" +
                "            <Member Name=\"Remanence\" Datatype=\"Bool\" Informative=\"true\" />\n" +
                "          </Section>\n" +
                "          <Section Name=\"Temp\" />\n" +
                "          <Section Name=\"Constant\" />\n" +
                "        </Sections>\n" +
                "      </Interface>\n" +
                "      <MemoryLayout>Optimized</MemoryLayout>\n" +
                "      <Name>" + obName + "</Name>\n" +
                "      <Namespace />\n" +
                "      <Number>1</Number>\n" +
                "      <ProgrammingLanguage>LAD</ProgrammingLanguage>\n" +
                "      <SecondaryType>ProgramCycle</SecondaryType>\n" +
                "      <SetENOAutomatically>false</SetENOAutomatically>\n" +
                "    </AttributeList>\n" +
                "    <ObjectList />\n" +
                "  </SW.Blocks.OB>\n" +
                "</Document>";
        }

        // ---------- checks + helpers ----------

        /// <summary>Area complete when its FC exists and every required instance DB already exists.</summary>
        private static bool IsTaskComplete(PlcSoftware plc, AreaTask task)
        {
            var folder = Ops.FindGroup(plc.BlockGroup, task.TargetFolderName);
            if (folder == null) return false;
            if (!(folder.Blocks.Find(task.TargetFcName) is FC)) return false;
            // global lookup — ImportAreaFc also skips creation when the DB exists anywhere
            foreach (var instrument in task.Instruments)
                foreach (var dbName in instrument.InstanceDbs)
                    if (Ops.FindBlock(plc, dbName) == null) return false;
            return true;
        }

        private static bool BlocksIdentical(PlcBlock existing, string newXmlPath)
        {
            return Ops.BlocksIdentical(existing, newXmlPath, false);
        }

        private static void ReassignUids(XElement element)
        {
            var idMap = new Dictionary<string, string>();
            foreach (var d in element.DescendantsAndSelf())
            {
                var uid = d.Attribute("UId");
                if (uid != null && !idMap.ContainsKey(uid.Value)) idMap.Add(uid.Value, (++_uid).ToString());
                var id = d.Attribute("ID");
                if (id != null && !idMap.ContainsKey(id.Value)) idMap.Add(id.Value, (++_uid).ToString("X"));
            }
            foreach (var d in element.DescendantsAndSelf())
                foreach (var attr in d.Attributes())
                    if (idMap.TryGetValue(attr.Value, out string v)) attr.Value = v;
        }

        /// <summary>Quoted dotted path of the first Static member containing the instrument ID.</summary>
        private static string FindPathInDbXml(XDocument dbXml, string instrumentId)
        {
            var staticSection = dbXml.Descendants(IntfNs + "Section")
                .FirstOrDefault(s => s.Attribute("Name")?.Value == "Static");
            if (staticSection == null)
                throw new InvalidOperationException("'Static' section not found in global DB XML.");
            var member = staticSection.Descendants(IntfNs + "Member")
                .FirstOrDefault(m => m.Attribute("Name")?.Value
                    .IndexOf(instrumentId, StringComparison.OrdinalIgnoreCase) >= 0);
            if (member == null)
                throw new InvalidOperationException(
                    "No global DB structure contains the ID '" + instrumentId + "'.");
            var parts = new Stack<string>();
            var current = member;
            while (current != null && current.Name == IntfNs + "Member")
            {
                parts.Push("\"" + current.Attribute("Name").Value + "\"");
                current = current.Parent;
            }
            return string.Join(".", parts);
        }

        private static List<PlcTag> CollectTags(PlcTagTableUserGroup group)
        {
            var tags = group.TagTables.SelectMany(t => t.Tags).ToList();
            foreach (PlcTagTableUserGroup sub in group.Groups)
                tags.AddRange(CollectTags(sub));
            return tags;
        }

        private static OB FindOb(PlcBlockGroup group, string name)
        {
            var hit = group.Blocks.FirstOrDefault(b => b is OB
                && b.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) as OB;
            if (hit != null) return hit;
            foreach (PlcBlockUserGroup sub in group.Groups)
            {
                hit = FindOb(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>Leading equipment-tag shape of a tag name, e.g. "FIT-100" of "FIT-100_STS_...".</summary>
        private static string ExtractId(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return null;
            var match = Regex.Match(tagName, @"^([A-Z0-9\.-]+(?:-[A-Z0-9\.-]+)*)");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>"1.3 Captação (X)" -> "Captação" (prefix + parenthetical stripped).</summary>
        private static string GetBaseName(string folderName)
        {
            var match = Regex.Match(folderName, @"^\d+(\.\d+)*\s*(.*)");
            string name = match.Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value)
                ? match.Groups[2].Value.Trim() : folderName.Trim();
            return Regex.Replace(name, @"\s*\([^)]*\)", "").Trim();
        }

        internal static string FcName(string areaName, string suffix)
        {
            string sanitized = Regex.Replace(areaName.Trim().ToUpper(), @"[\s-]+", "_");
            return Regex.Replace(sanitized, @"[^A-Z0-9_]", "") + suffix;
        }

        /// <summary>"1.3 Captação" under dest "5.2 Instrumentos" -> "5.2.3 Captação".</summary>
        private static string TargetFolderName(string sourceFolderName, string destRootName)
        {
            string prefix = Regex.Match(destRootName, @"^\d+(\.\d+)*").Value;
            var match = Regex.Match(sourceFolderName, @"^\d+\.(\d+)(.*)");
            return match.Success
                ? prefix + "." + match.Groups[1].Value + " " + match.Groups[2].Value.Trim()
                : sourceFolderName;
        }

        private static string WriteCommandCsv(List<(int Id, string Equipment, string Kind)> mappings, string outDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Comando;Equipamento;Tipo");
            foreach (var m in mappings.OrderBy(m => m.Id))
                sb.AppendLine(m.Id + ";" + m.Equipment + ";" + m.Kind);
            string path = Path.GetFullPath(Path.Combine(outDir, "instrument-commands.csv"));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }
    }
}
