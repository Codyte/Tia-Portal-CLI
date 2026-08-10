using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>Config for gen-alarm-fc (port of "Replicador de FC Alarmes" / Arquiteto de Lógica de Alarmes).</summary>
    public class AlarmFcConfig
    {
        public string TargetRootFolder { get; set; } = "3.1 Alarmes Words";
        public string TemplateFc { get; set; } = "FC_Modelo";
        public string TemplateFolder { get; set; } = "3.1.0 Modelo";
        public string ObTemplate { get; set; } = "OB_MOLDE_ALARMES";
        public string GlobalDb { get; set; } = "DB GLOBAL";
        public string AlarmTagsFolder { get; set; } = "2. Alarmes";
        public string StartTagsFolder { get; set; } = "3. Partidas";
        public string MasterFb { get; set; } = "FB BITS TO WORD";
        public string CallObName { get; set; } = "CHAMADA_ALARMES";
        public int CallObNumber { get; set; } = 1;
        public List<string> IgnoreFolders { get; set; } = new List<string>();
        /// <summary>Area base name -> global-DB top struct. Overrides the tag-name heuristic.</summary>
        public Dictionary<string, string> Structs { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Per plant area, packs every alarm bit (instrument alarms + equipment _FALHA tags) into
    /// WORD_ALARMES_n words of the global DB via generated bits-to-word FCs, updates the DB word
    /// comments, and regenerates the OB that calls all alarm FCs.
    /// ponytail: no on-disk template cache fallback like the original — template blocks must exist
    /// in the project; add the cache back if offline runs become a need.
    /// </summary>
    public static class AlarmFc
    {
        private static int _uid = 10000; // single-shot CLI process (D9)
        private static string[] _cultures = { "en-US" }; // set per-run in Generate

        private static readonly XNamespace FlgNs =
            "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v5";
        private static readonly XNamespace IntfNs =
            "http://www.siemens.com/automation/Openness/SW/Interface/v5";

        private class TagRef
        {
            public string Name;
            public string SourceArea;
        }

        public static object Generate(TiaSession session, PlcSoftware plc, AlarmFcConfig config, string outDir, bool apply)
        {
            // an XML MultilingualTextItem whose culture the project doesn't have fails the whole import
            _cultures = session.Project.LanguageSettings.ActiveLanguages
                .Select(l => l.Culture.Name).ToArray();
            if (_cultures.Length == 0) _cultures = new[] { "en-US" };

            var warnings = new List<string>();
            Directory.CreateDirectory(outDir);

            // templates + global DB exported up-front (read-only operations)
            var templateFolder = Ops.FindGroup(plc.BlockGroup, config.TemplateFolder);
            if (templateFolder == null)
                throw new InvalidOperationException("Template folder '" + config.TemplateFolder + "' not found.");
            var templateFc = templateFolder.Blocks.Find(config.TemplateFc);
            if (templateFc == null)
                throw new InvalidOperationException("Template FC '" + config.TemplateFc + "' not found in '" + config.TemplateFolder + "'.");
            var obTemplate = Ops.FindBlock(plc, config.ObTemplate);
            if (obTemplate == null)
                throw new InvalidOperationException("OB template '" + config.ObTemplate + "' not found.");

            string fcTemplatePath = ExportTo(templateFc, outDir, "alarm_fc_template.xml");
            string obTemplatePath = ExportTo(obTemplate, outDir, "alarm_ob_template.xml");

            var globalDbBlock = Ops.FindBlock(plc, config.GlobalDb);
            if (globalDbBlock == null)
                throw new InvalidOperationException("Global DB '" + config.GlobalDb + "' not found.");
            string dbXmlPath = ExportTo(globalDbBlock, outDir, "alarm_globaldb.xml");
            var dbXml = XDocument.Load(dbXmlPath);

            var alarmsRoot = Ops.FindTagGroup(plc.TagTableGroup, config.AlarmTagsFolder);
            if (alarmsRoot == null)
                throw new InvalidOperationException("Tag folder '" + config.AlarmTagsFolder + "' not found.");
            var startsRoot = Ops.FindTagGroup(plc.TagTableGroup, config.StartTagsFolder);
            if (startsRoot == null)
                throw new InvalidOperationException("Tag folder '" + config.StartTagsFolder + "' not found.");

            PlcBlockUserGroup targetRoot = Ops.FindGroup(plc.BlockGroup, config.TargetRootFolder);
            if (targetRoot == null && apply)
                targetRoot = plc.BlockGroup.Groups.Create(config.TargetRootFolder);

            var areas = new List<object>();
            var commentTasks = new List<(string ParentStruct, string Member, string Comment)>();
            var documentation = new Dictionary<string, List<string>>();

            foreach (PlcTagTableUserGroup alarmGroup in alarmsRoot.Groups)
            {
                if (config.IgnoreFolders.Any(f => alarmGroup.Name.Equals(f, StringComparison.OrdinalIgnoreCase)))
                    continue;
                string areaBase = GetBaseName(alarmGroup.Name);
                var startGroup = startsRoot.Groups.Cast<PlcTagTableUserGroup>()
                    .FirstOrDefault(p => GetBaseName(p.Name).Equals(areaBase, StringComparison.OrdinalIgnoreCase));
                if (startGroup == null) continue;

                var instrumentTags = CollectTags(alarmGroup)
                    .Where(t => !t.Name.ToUpper().Contains("RESERVA")).ToList();
                var equipmentTags = CollectTags(startGroup)
                    .Where(t => t.Name.EndsWith("_FALHA", StringComparison.OrdinalIgnoreCase)
                        && !t.Name.ToUpper().Contains("_CMD_") && !t.Name.ToUpper().Contains("_RESET_"))
                    .ToList();
                if (!instrumentTags.Any() && !equipmentTags.Any()) continue;

                var allTags = new List<TagRef>();
                allTags.AddRange(instrumentTags);
                allTags.AddRange(equipmentTags.OrderBy(t => t.Name));
                var variables = allTags.Select(t => t.Name).ToList();

                string areaClean = CleanName(areaBase);
                string fcName = "FC_ALARMES_" + areaClean;
                string subFolderName = TargetSubFolderName(alarmGroup.Name);
                string structName;
                if (config.Structs.TryGetValue(areaBase, out var mappedStruct))
                {
                    // heurística de nome de tag erra quando o projeto não repete o nome da área na DB
                    if (!TopStructNames(dbXml).Contains(mappedStruct, StringComparer.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Struct '" + mappedStruct + "' (config 'Structs' -> '"
                            + areaBase + "') not found in global DB '" + config.GlobalDb + "'.");
                    structName = mappedStruct;
                }
                else structName = FindParentStruct(dbXml, equipmentTags.Select(t => t.Name).ToList(),
                    instrumentTags.Select(t => t.Name).ToList(), areaBase);
                if (string.IsNullOrEmpty(structName))
                {
                    structName = areaClean;
                    warnings.Add("Area '" + areaBase + "': global-DB struct not mapped automatically, using fallback '" + structName + "'.");
                }

                int wordCount = Math.Max(1, (int)Math.Ceiling(variables.Count / 16.0));
                var instanceDbs = Enumerable.Range(1, wordCount)
                    .Select(w => "DB_BITS_TO_WORD_" + areaClean + "_W" + w).ToList();

                string fcXmlPath = BuildFcXml(fcTemplatePath, fcName, structName, areaClean, variables, outDir);
                var subFolder = targetRoot?.Groups.Find(subFolderName);
                var existing = subFolder?.Blocks.Find(fcName);
                string action = existing == null ? "create"
                    : BlocksIdentical(existing, fcXmlPath) ? "in-sync" : "update";

                if (apply)
                {
                    subFolder = subFolder ?? targetRoot.Groups.Create(subFolderName);
                    if (action == "update")
                    {
                        existing.Delete();
                        subFolder.Blocks.Import(new FileInfo(fcXmlPath), ImportOptions.None);
                    }
                    else if (action == "create")
                    {
                        subFolder.Blocks.Import(new FileInfo(fcXmlPath), ImportOptions.None);
                    }
                    foreach (var dbName in instanceDbs)
                        if (subFolder.Blocks.Find(dbName) == null)
                            subFolder.Blocks.CreateInstanceDB(dbName, true, 1, config.MasterFb);
                }

                for (int w = 1; w <= wordCount; w++)
                {
                    var wordVars = variables.Skip((w - 1) * 16).Take(16).ToList();
                    var sources = allTags.Where(t => wordVars.Contains(t.Name))
                        .Select(t => GetBaseName(t.SourceArea)).Distinct();
                    string comment = "Compila alarmes de: " + string.Join(", ", sources.Select(s => "\"" + s + "\""));
                    if (comment.Length > 250) comment = comment.Substring(0, 247) + "...";
                    commentTasks.Add((structName, "WORD_ALARMES_" + w, comment));
                }

                documentation[areaBase] = variables;
                areas.Add(new Dictionary<string, object>
                {
                    { "area", areaBase },
                    { "fc", fcName },
                    { "folder", config.TargetRootFolder + "/" + subFolderName },
                    { "struct", structName },
                    { "variables", variables.Count },
                    { "words", wordCount },
                    { "instanceDbs", instanceDbs },
                    { "action", action },
                    { "xml", fcXmlPath },
                });
            }

            // word comments in the global DB (rewrite + reimport only when comments changed —
            // delete/reimport of the central DB is the riskiest step of the run)
            object globalDbAction = null;
            if (commentTasks.Any())
            {
                WriteDbComments(dbXmlPath, commentTasks);
                bool dbInSync = Ops.BlocksIdentical(globalDbBlock, dbXmlPath, false);
                globalDbAction = dbInSync ? "in-sync" : (apply ? "updated" : "update");
                if (apply && !dbInSync)
                {
                    var parent = globalDbBlock.Parent as PlcBlockGroup;
                    if (parent != null)
                    {
                        globalDbBlock.Delete();
                        parent.Blocks.Import(new FileInfo(dbXmlPath), ImportOptions.None);
                    }
                    else
                    {
                        warnings.Add("Could not resolve the folder of '" + config.GlobalDb + "'; comment reimport skipped.");
                    }
                }
            }

            // OB that calls every alarm FC under the target root
            object callOb = null;
            var fcRoot = targetRoot ?? Ops.FindGroup(plc.BlockGroup, config.TargetRootFolder);
            if (fcRoot != null)
            {
                var fcs = CollectFcs(fcRoot, config.TemplateFolder)
                    .OrderBy(fc => ((PlcBlockUserGroup)fc.Parent).Name, new NaturalStringComparer()).ToList();
                // on dry-run the FCs may not exist yet: fall back to the planned FC list
                var callNames = fcs.Any()
                    ? fcs.Select(f => new { Name = f.Name, Number = (int?)f.Number, Folder = ((PlcBlockUserGroup)f.Parent).Name }).ToList()
                    : areas.Cast<Dictionary<string, object>>()
                        .Select(a => new { Name = (string)a["fc"], Number = (int?)null, Folder = (string)a["folder"] }).ToList();
                if (callNames.Any())
                {
                    string obXmlPath = BuildCallObXml(obTemplatePath, config.CallObName, config.CallObNumber,
                        callNames.Select(c => (c.Name, c.Number, c.Folder)).ToList(), outDir);
                    var existingOb = plc.BlockGroup.Blocks.Find(config.CallObName)
                        ?? Ops.FindBlock(plc, config.CallObName);
                    bool obInSync = existingOb != null && BlocksIdentical(existingOb, obXmlPath);
                    if (apply && !obInSync)
                    {
                        existingOb?.Delete();
                        fcRoot.Blocks.Import(new FileInfo(obXmlPath), ImportOptions.None);
                    }
                    callOb = new Dictionary<string, object>
                    {
                        { "ob", config.CallObName },
                        { "calls", callNames.Select(c => c.Name).ToList() },
                        { "xml", obXmlPath },
                        { "action", obInSync ? "in-sync"
                            : existingOb == null ? (apply ? "created" : "create")
                            : (apply ? "updated" : "update") },
                    };
                }
            }

            string csvPath = WriteCsv(documentation, outDir);

            return new Dictionary<string, object>
            {
                { "applied", apply },
                { "areas", areas },
                { "globalDb", globalDbAction },
                { "callOb", callOb },
                { "csv", csvPath },
                { "warnings", warnings },
            };
        }

        // ---------- FC XML ----------

        internal static string BuildFcXml(string templatePath, string fcName, string structName,
            string areaClean, List<string> variables, string outDir)
        {
            var doc = XDocument.Load(templatePath);
            var networkTemplate = doc.Descendants("SW.Blocks.CompileUnit")
                .FirstOrDefault(cu => cu.Descendants(FlgNs + "FlgNet").Any());
            if (networkTemplate == null)
                throw new InvalidOperationException("Template FC contains no valid network.");

            doc.Descendants("AttributeList").Elements("Name").First().Value = fcName;
            var objectList = doc.Descendants("ObjectList").First();
            objectList.Elements("SW.Blocks.CompileUnit").Remove();
            _uid = doc.Descendants().SelectMany(d => d.Attributes())
                .Where(a => a.Name == "UId" || a.Name == "ID")
                .Select(a => int.TryParse(a.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v) ? v : 0)
                .DefaultIfEmpty(0).Max() + 1;

            int wordCount = Math.Max(1, (int)Math.Ceiling(variables.Count / 16.0));
            for (int i = 0; i < wordCount; i++)
            {
                int wordNumber = i + 1;
                var wordVars = variables.Skip(i * 16).Take(16).ToList();
                string instanceDb = "DB_BITS_TO_WORD_" + areaClean + "_W" + wordNumber;
                string outputPath = "\"" + "DB GLOBAL" + "\".\"" + structName + "\".ALARMES.WORD_ALARMES_" + wordNumber;
                var network = new XElement(networkTemplate);
                ReassignUids(network);
                RewireWordNetwork(network, instanceDb, wordVars, outputPath);
                objectList.Add(network);
            }

            string path = Path.GetFullPath(Path.Combine(outDir, fcName + ".xml"));
            if (File.Exists(path)) File.Delete(path);
            doc.Save(path);
            return path;
        }

        /// <summary>Wires up to 16 alarm bits into the FB call, points the output at the DB word.</summary>
        private static void RewireWordNetwork(XElement network, string instanceName,
            List<string> variables, string outputPath)
        {
            var callInfo = network.Descendants(FlgNs + "CallInfo").FirstOrDefault();
            if (callInfo == null) return;

            var instanceComponent = callInfo.Element(FlgNs + "Instance")?.Element(FlgNs + "Component");
            if (instanceComponent != null) instanceComponent.Attribute("Name").Value = instanceName;

            var titleText = network.Descendants("MultilingualTextItem")
                .FirstOrDefault(m => m.Parent?.Parent?.Attribute("CompositionName")?.Value == "Title")
                ?.Element("AttributeList")?.Element("Text");
            if (titleText != null) titleText.Value = "Word de Alarmes: " + instanceName;

            string callUId = callInfo.Parent.Attribute("UId").Value;
            for (int i = 0; i < 16; i++)
            {
                string paramName = "SIGNAL_Bit" + i;
                var wire = network.Descendants(FlgNs + "Wire").FirstOrDefault(w =>
                    w.Elements(FlgNs + "NameCon").Any(nc => nc.Attribute("UId")?.Value == callUId
                        && nc.Attribute("Name")?.Value == paramName));
                if (wire == null) continue;
                var identCon = wire.Element(FlgNs + "IdentCon");
                var accessUId = identCon?.Attribute("UId")?.Value;
                var access = accessUId != null
                    ? network.Descendants(FlgNs + "Access").FirstOrDefault(a => a.Attribute("UId").Value == accessUId)
                    : null;
                if (i < variables.Count)
                {
                    wire.Elements(FlgNs + "OpenCon").Remove();
                    if (identCon == null)
                        wire.AddFirst(new XElement(FlgNs + "IdentCon", new XAttribute("UId", (++_uid).ToString())));
                    var symbol = access?.Element(FlgNs + "Symbol");
                    if (symbol != null)
                    {
                        symbol.RemoveAll();
                        symbol.Add(new XElement(FlgNs + "Component", new XAttribute("Name", variables[i])));
                    }
                }
                else
                {
                    if (identCon != null)
                    {
                        access?.Remove();
                        identCon.Remove();
                    }
                    if (wire.Element(FlgNs + "OpenCon") == null)
                        wire.AddFirst(new XElement(FlgNs + "OpenCon", new XAttribute("UId", (++_uid).ToString())));
                }
            }

            var outputWire = network.Descendants(FlgNs + "Wire").FirstOrDefault(w =>
                w.Descendants(FlgNs + "NameCon").Any(nc => nc.Attribute("Name")?.Value == "BITS_TO_WORD"));
            if (outputWire != null)
            {
                var outputAccess = network.Descendants(FlgNs + "Access").FirstOrDefault(a =>
                    a.Attribute("UId").Value == outputWire.Element(FlgNs + "IdentCon")?.Attribute("UId").Value);
                var outputSymbol = outputAccess?.Element(FlgNs + "Symbol");
                if (outputSymbol != null)
                {
                    outputSymbol.RemoveAll();
                    foreach (var part in outputPath.Replace("\"", "").Split('.'))
                        outputSymbol.Add(new XElement(FlgNs + "Component", new XAttribute("Name", part)));
                }
            }

            // per-network comment mapping every bit to its variable
            var networkObjectList = network.Element("ObjectList");
            if (networkObjectList == null) return;
            var comment = networkObjectList.Elements("MultilingualText")
                .FirstOrDefault(m => m.Attribute("CompositionName")?.Value == "Comment");
            if (comment == null)
            {
                comment = new XElement("MultilingualText",
                    new XAttribute("ID", (++_uid).ToString("X")),
                    new XAttribute("CompositionName", "Comment"),
                    new XElement("ObjectList"));
                networkObjectList.Add(comment);
            }
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append("Bit " + i.ToString().PadRight(2) + ": \t");
                sb.AppendLine(i < variables.Count ? variables[i] : "");
            }
            foreach (var culture in _cultures)
            {
                var item = comment.Descendants("MultilingualTextItem")
                    .FirstOrDefault(x => x.Element("AttributeList")?.Element("Culture")?.Value == culture);
                if (item == null)
                {
                    item = new XElement("MultilingualTextItem",
                        new XAttribute("ID", (++_uid).ToString("X")),
                        new XAttribute("CompositionName", "Items"),
                        new XElement("AttributeList",
                            new XElement("Culture", culture),
                            new XElement("Text", "")));
                    comment.Element("ObjectList")?.Add(item);
                }
                var text = item.Element("AttributeList")?.Element("Text");
                if (text == null)
                {
                    text = new XElement("Text");
                    item.Element("AttributeList")?.Add(text);
                }
                text.Value = sb.ToString();
            }
        }

        // ---------- call OB ----------

        internal static string BuildCallObXml(string obTemplatePath, string obName, int obNumber,
            List<(string Name, int? Number, string Folder)> fcs, string outDir)
        {
            var doc = XDocument.Load(obTemplatePath);
            doc.Descendants("AttributeList").Elements("Name").First().Value = obName;
            doc.Descendants("AttributeList").Elements("Number").First().Value = obNumber.ToString();
            var objectList = doc.Descendants("ObjectList").First();
            objectList.Elements("SW.Blocks.CompileUnit").Remove();

            int uid = 100;
            foreach (var fc in fcs)
            {
                string folderTitle = Regex.Replace(fc.Folder, @"^[\d\.]+\s*", "").Trim();
                var callInfo = new XElement(FlgNs + "CallInfo",
                    new XAttribute("Name", fc.Name), new XAttribute("BlockType", "FC"));
                if (fc.Number.HasValue)
                    callInfo.Add(new XElement("IntegerAttribute",
                        new XAttribute("Name", "BlockNumber"), new XAttribute("Informative", "true"), fc.Number.Value));
                var network = new XElement("SW.Blocks.CompileUnit",
                    new XAttribute("ID", (++uid).ToString("X")),
                    new XAttribute("CompositionName", "CompileUnits"),
                    new XElement("AttributeList",
                        new XElement("NetworkSource",
                            new XElement(FlgNs + "FlgNet",
                                new XElement(FlgNs + "Parts",
                                    new XElement(FlgNs + "Call", new XAttribute("UId", (++uid).ToString()), callInfo)),
                                new XElement(FlgNs + "Wires",
                                    new XElement(FlgNs + "Wire", new XAttribute("UId", (++uid).ToString()),
                                        new XElement("Powerrail"),
                                        new XElement(FlgNs + "NameCon", new XAttribute("UId", (uid - 1).ToString()),
                                            new XAttribute("Name", "en")))))),
                        new XElement("ProgrammingLanguage", "LAD")),
                    new XElement("ObjectList",
                        new XElement("MultilingualText", new XAttribute("ID", (++uid).ToString("X")),
                            new XAttribute("CompositionName", "Title"),
                            new XElement("ObjectList",
                                new XElement("MultilingualTextItem", new XAttribute("ID", (++uid).ToString("X")),
                                    new XAttribute("CompositionName", "Items"),
                                    new XElement("AttributeList",
                                        new XElement("Culture", _cultures[0]),
                                        new XElement("Text", "FC Alarmes: " + folderTitle)))))));
                objectList.Add(network);
            }

            string path = Path.GetFullPath(Path.Combine(outDir, obName + ".xml"));
            if (File.Exists(path)) File.Delete(path);
            doc.Save(path);
            return path;
        }

        // ---------- global DB comments ----------

        private static void WriteDbComments(string dbXmlPath,
            List<(string ParentStruct, string Member, string Comment)> tasks)
        {
            var doc = XDocument.Load(dbXmlPath);
            foreach (var task in tasks)
            {
                var parentStruct = doc.Descendants(IntfNs + "Member")
                    .FirstOrDefault(m => m.Attribute("Name")?.Value == task.ParentStruct);
                var alarms = parentStruct?.Elements(IntfNs + "Member")
                    .FirstOrDefault(m => m.Attribute("Name")?.Value == "ALARMES");
                var word = alarms?.Elements(IntfNs + "Member")
                    .FirstOrDefault(m => m.Attribute("Name")?.Value == task.Member);
                if (word == null) continue;
                word.Elements(IntfNs + "Comment").Remove();
                word.Add(new XElement(IntfNs + "Comment",
                    _cultures.Select(c => new XElement(IntfNs + "MultiLanguageText",
                        new XAttribute("Lang", c), task.Comment))));
            }
            doc.Save(dbXmlPath);
        }

        private static string FindParentStruct(XDocument dbXml, List<string> equipmentTags,
            List<string> instrumentTags, string areaName)
        {
            var allMembers = dbXml.Descendants(IntfNs + "Member").Where(m => m.Attribute("Name") != null).ToList();
            foreach (var tag in equipmentTags)
            {
                string baseTag = tag.Replace("_FALHA", "").Trim();
                if (string.IsNullOrEmpty(baseTag)) continue;
                var hit = allMembers.FirstOrDefault(m => m.Attribute("Name").Value.ToUpper().Contains(baseTag.ToUpper()));
                if (hit != null) return TopStructName(hit);
            }
            foreach (var tag in instrumentTags)
            {
                var match = Regex.Match(tag, @"\((.*?)\)");
                if (!match.Success) continue;
                string baseTag = match.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(baseTag)) continue;
                var hit = allMembers.FirstOrDefault(m => m.Attribute("Name").Value.ToUpper().Contains(baseTag.ToUpper()));
                if (hit != null) return TopStructName(hit);
            }
            if (!string.IsNullOrEmpty(areaName))
            {
                string cleanArea = CleanName(areaName);
                foreach (var name in TopStructNames(dbXml))
                    if (cleanArea.Contains(CleanName(name)))
                        return name;
            }
            return null;
        }

        private static List<string> TopStructNames(XDocument dbXml)
        {
            return (dbXml.Descendants(IntfNs + "Section")
                    .FirstOrDefault(s => s.Attribute("Name")?.Value == "Static")
                    ?.Elements(IntfNs + "Member")
                    .Select(m => m.Attribute("Name")?.Value)
                    .Where(n => !string.IsNullOrEmpty(n))
                ?? Enumerable.Empty<string>()).ToList();
        }

        private static string TopStructName(XElement member)
        {
            var current = member.Parent;
            while (current != null)
            {
                if (current.Parent?.Name.LocalName == "Section"
                    && current.Parent?.Attribute("Name")?.Value == "Static")
                    return current.Attribute("Name")?.Value;
                current = current.Parent;
            }
            return null;
        }

        // ---------- misc helpers ----------

        private static string ExportTo(PlcBlock block, string outDir, string fileName)
        {
            string path = Path.GetFullPath(Path.Combine(outDir, fileName));
            if (File.Exists(path)) File.Delete(path);
            block.Export(new FileInfo(path), ExportOptions.None);
            return path;
        }

        private static bool BlocksIdentical(PlcBlock existing, string newXmlPath)
        {
            return Ops.BlocksIdentical(existing, newXmlPath, true);
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

        private static List<TagRef> CollectTags(PlcTagTableUserGroup group)
        {
            // só Bool: tabela de instrumento mistura o valor Real com os bits de alarme, e o
            // FB BITS TO WORD só aceita Bool (o compile reprova longe daqui)
            var tags = group.TagTables.SelectMany(t => t.Tags)
                .Where(t => "Bool".Equals(t.DataTypeName, StringComparison.OrdinalIgnoreCase))
                .Select(t => new TagRef { Name = t.Name, SourceArea = group.Name }).ToList();
            foreach (PlcTagTableUserGroup sub in group.Groups)
                tags.AddRange(CollectTags(sub));
            return tags;
        }

        /// <summary>FCs under the target root, minus the mold folder — o molde mora dentro do root
        /// alvo e senão entraria na lista de chamadas do OB.</summary>
        private static List<FC> CollectFcs(PlcBlockUserGroup group, string skipFolder)
        {
            var fcs = group.Blocks.OfType<FC>().ToList();
            foreach (PlcBlockUserGroup sub in group.Groups)
                if (!sub.Name.Equals(skipFolder, StringComparison.OrdinalIgnoreCase))
                    fcs.AddRange(CollectFcs(sub, skipFolder));
            return fcs;
        }

        /// <summary>Consolidated word/bit -> description CSV for field documentation.</summary>
        private static string WriteCsv(Dictionary<string, List<string>> documentation, string outDir)
        {
            var sb = new StringBuilder();
            foreach (var area in documentation)
            {
                sb.AppendLine("\"-- " + area.Key.ToUpper() + " --\";\"\";\"\"");
                sb.AppendLine("\"Word\";\"Bit\";\"Descrição\"");
                int count = area.Value.Any() ? area.Value.Count : 16;
                for (int i = 0; i < count; i++)
                {
                    string word = i % 16 == 0 ? "WORD " + (i / 16 + 1) : "";
                    string description = i < area.Value.Count ? Describe(area.Value[i]) : "";
                    sb.AppendLine("\"" + word + "\";\"Bit " + i % 16 + " :\";\"" + description + "\"");
                }
                sb.AppendLine();
            }
            string path = Path.GetFullPath(Path.Combine(outDir, "alarm-words.csv"));
            if (File.Exists(path)) File.Delete(path);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static string Describe(string variableName)
        {
            if (variableName.EndsWith("_FALHA", StringComparison.OrdinalIgnoreCase))
                return "Status: Falha geral do equipamento " +
                    variableName.Substring(0, variableName.Length - 6);
            if (variableName.Contains("_STS_"))
            {
                var parts = variableName.Split(new[] { "_STS_" }, StringSplitOptions.None);
                string text;
                switch (parts[1].ToUpper())
                {
                    case "LEITURA_MUITO_ALTA": text = "Alarme: Leitura Muito Alta"; break;
                    case "LEITURA_ALTA": text = "Aviso: Leitura Alta"; break;
                    case "LEITURA_BAIXA": text = "Aviso: Leitura Baixa"; break;
                    case "LEITURA_MUITO_BAIXA": text = "Alarme: Leitura Muito Baixa"; break;
                    case "SEM_4MA": text = "Alarme: Sem Comunicação com Equipamento"; break;
                    default: text = parts[1].Replace("_", " "); break;
                }
                return text + " " + parts[0];
            }
            return variableName;
        }

        /// <summary>"2.3 Captação" -> "Captação" (numeric prefix stripped).</summary>
        private static string GetBaseName(string folderName)
        {
            var match = Regex.Match(folderName, @"^\d+(\.\d+)*\s*(.*)");
            return match.Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value)
                ? match.Groups[2].Value.Trim() : folderName.Trim();
        }

        /// <summary>Uppercase, accents stripped, non [A-Z0-9_] removed — safe for block names.</summary>
        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string decomposed = name.Normalize(NormalizationForm.FormD);
            var filtered = new string(decomposed
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());
            string sanitized = Regex.Replace(filtered.Trim().ToUpper(), @"[\s-]+", "_");
            return Regex.Replace(sanitized, @"[^A-Z0-9_]", "");
        }

        /// <summary>"2.3 Captação" -> "3.1.3 Captação" (alarm source folder to target folder).</summary>
        private static string TargetSubFolderName(string sourceFolderName)
        {
            var match = Regex.Match(sourceFolderName, @"^2\.(\d+)(.*)");
            return match.Success ? "3.1." + match.Groups[1].Value + " " + match.Groups[2].Value.Trim() : sourceFolderName;
        }
    }
}
