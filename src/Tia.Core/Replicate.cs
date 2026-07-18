using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>Config for replicate-fc (port of "Replicador de FC Acionamentos V3").</summary>
    public class ReplicateFcConfig
    {
        /// <summary>Folder under Program blocks whose subfolders hold one equipment each ("NAME (ID)").</summary>
        public string BlocksFolder { get; set; }
        /// <summary>Folder-name keywords that define each equipment group (e.g. "BOMBA").</summary>
        public List<string> EquipmentTypes { get; set; }
        /// <summary>UDT type names whose instances in the global DB identify an equipment.</summary>
        public List<string> UdtNames { get; set; }
        /// <summary>Literal numbers in the template blocks replaced by each target's assigned number.</summary>
        public List<string> SourceNumbersToReplace { get; set; } = new List<string>();
        public string GlobalDb { get; set; } = "DB GLOBAL";
        /// <summary>First assigned equipment number; increments per target across all groups.</summary>
        public int StartNumber { get; set; } = 300;
    }

    /// <summary>
    /// Replicates the first populated equipment folder of each type onto every sibling folder:
    /// blocks are exported, IDs / numbers / global-DB paths / IO tags rewired in XML, and
    /// re-imported over the target folder's blocks (port of "Replicador de FC Acionamentos V3").
    /// </summary>
    public static class ReplicateFc
    {
        public static object Run(PlcSoftware plc, ReplicateFcConfig config, string outDir, bool apply)
        {
            if (string.IsNullOrEmpty(config?.BlocksFolder))
                throw new ArgumentException("Config must set 'BlocksFolder'.");
            if (config.EquipmentTypes == null || !config.EquipmentTypes.Any())
                throw new ArgumentException("Config must contain a non-empty 'EquipmentTypes' list.");
            if (config.UdtNames == null || !config.UdtNames.Any())
                throw new ArgumentException("Config must contain a non-empty 'UdtNames' list.");

            var warnings = new List<string>();
            Directory.CreateDirectory(outDir);

            var globalDb = FindDataBlock(plc.BlockGroup, config.GlobalDb);
            if (globalDb == null)
                throw new InvalidOperationException("Global DB '" + config.GlobalDb + "' not found.");
            var dbXmlPath = Path.GetFullPath(Path.Combine(outDir, "replicate_globaldb_cache.xml"));
            if (File.Exists(dbXmlPath)) File.Delete(dbXmlPath);
            globalDb.Export(new FileInfo(dbXmlPath), ExportOptions.None);
            var dbXml = XDocument.Load(dbXmlPath);

            // TIA folder names may contain a literal '/' — exact-name match anywhere in the tree
            // first, then "A/B" as a path under Program blocks
            var workingFolder = FindGroup(plc.BlockGroup, config.BlocksFolder);
            if (workingFolder == null && config.BlocksFolder.Contains("/"))
                try { workingFolder = Ops.ResolveFolder(plc, config.BlocksFolder, false) as PlcBlockUserGroup; }
                catch (InvalidOperationException) { }
            if (workingFolder == null)
                throw new InvalidOperationException("Blocks folder '" + config.BlocksFolder + "' not found.");
            var allSubFolders = DescendantGroups(workingFolder);

            int numberCounter = config.StartNumber;
            var natural = new NaturalStringComparer();
            var groups = new List<object>();

            foreach (var equipmentType in config.EquipmentTypes)
            {
                var folders = allSubFolders
                    .Where(f => f.Name.IndexOf(equipmentType, StringComparison.OrdinalIgnoreCase) >= 0
                                && !string.IsNullOrEmpty(ExtractId(f.Name)))
                    .OrderBy(f => f.Name, natural).ToList();
                int noId = allSubFolders.Count(f =>
                    f.Name.IndexOf(equipmentType, StringComparison.OrdinalIgnoreCase) >= 0
                    && string.IsNullOrEmpty(ExtractId(f.Name)));
                if (noId > 0)
                    warnings.Add("Type '" + equipmentType + "': " + noId +
                        " folder(s) match the keyword but have no '(ID)' in the name. Skipped.");
                var templateFolder = folders.FirstOrDefault(f => f.Blocks.Any());
                if (templateFolder == null)
                {
                    if (folders.Any())
                        warnings.Add("No populated template folder for type '" + equipmentType + "'.");
                    continue;
                }

                string sourceId = ExtractId(templateFolder.Name);
                string sourceDbPath;
                try { sourceDbPath = FindPathInDbXml(dbXml, sourceId, config.UdtNames); }
                catch (Exception ex)
                {
                    warnings.Add("Type '" + equipmentType + "': template '" + templateFolder.Name +
                        "' has no global-DB path (" + ex.Message + "). Skipped.");
                    continue;
                }

                var templates = new List<KeyValuePair<string, string>>(); // name -> xml (loaded lazily on apply)
                foreach (var block in templateFolder.Blocks.OrderBy(b => b.Name))
                    templates.Add(new KeyValuePair<string, string>(block.Name, null));

                var targets = new List<object>();
                foreach (var folder in folders)
                {
                    string targetId = ExtractId(folder.Name);
                    string targetDbPath;
                    try { targetDbPath = FindPathInDbXml(dbXml, targetId, config.UdtNames); }
                    catch
                    {
                        warnings.Add("Target '" + folder.Name + "' has no global-DB instance. Skipped.");
                        continue;
                    }
                    int assignedNumber = numberCounter++;
                    var ccm = FindCcmInfo(folder);

                    var blockNames = templates
                        .Select(t => ProposedBlockName(t.Key, templateFolder.Name, folder.Name, sourceId, targetId))
                        .ToList();

                    if (apply && folder != templateFolder)
                        ReplicateInto(plc, config, templateFolder, folder, sourceId, targetId,
                            sourceDbPath, targetDbPath, assignedNumber, ccm, warnings);

                    targets.Add(new Dictionary<string, object>
                    {
                        { "folder", folder.Name },
                        { "id", targetId },
                        { "number", assignedNumber },
                        { "ccm", ccm.CcmName },
                        { "action", folder == templateFolder ? "template" : (folder.Blocks.Any() ? "overwrite" : "create") },
                        { "blocks", blockNames },
                    });
                }

                groups.Add(new Dictionary<string, object>
                {
                    { "type", equipmentType },
                    { "template", templateFolder.Name },
                    { "sourceId", sourceId },
                    { "sourceDbPath", sourceDbPath },
                    { "targets", targets },
                });
            }

            return new Dictionary<string, object>
            {
                { "blocksFolder", config.BlocksFolder },
                { "applied", apply },
                { "groups", groups },
                { "warnings", warnings },
            };
        }

        private static void ReplicateInto(PlcSoftware plc, ReplicateFcConfig config,
            PlcBlockUserGroup templateFolder, PlcBlockUserGroup target, string sourceId, string targetId,
            string sourceDbPath, string targetDbPath, int assignedNumber,
            (string CcmName, string QaFolderName) ccm, List<string> warnings)
        {
            var templateXmls = new List<KeyValuePair<string, string>>();
            foreach (var block in templateFolder.Blocks.OrderBy(b => b.Name))
            {
                string tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
                try
                {
                    block.Export(new FileInfo(tmp), ExportOptions.None);
                    templateXmls.Add(new KeyValuePair<string, string>(block.Name, File.ReadAllText(tmp)));
                }
                catch (Exception ex)
                {
                    warnings.Add("Failed to export template block '" + block.Name + "': " + ex.Message);
                }
                finally { if (File.Exists(tmp)) File.Delete(tmp); }
            }

            foreach (var old in target.Blocks.ToList()) old.Delete();

            foreach (var template in templateXmls)
            {
                string newName = ProposedBlockName(template.Key, templateFolder.Name, target.Name, sourceId, targetId);
                string tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
                try
                {
                    var doc = XDocument.Parse(template.Value);
                    doc.Descendants("AttributeList").Elements("Name").First().Value = newName;
                    RewireXml(plc, doc, config, sourceId, targetId, sourceDbPath, targetDbPath,
                        assignedNumber, ccm, warnings);
                    doc.Save(tmp);
                    target.Blocks.Import(new FileInfo(tmp), ImportOptions.Override);
                }
                catch (Exception ex)
                {
                    warnings.Add("Failed to import block '" + newName + "' into '" + target.Name + "': " +
                        ex.GetBaseException().Message);
                }
                finally { if (File.Exists(tmp)) File.Delete(tmp); }
            }
        }

        /// <summary>Global text swap (IDs + numbers) then targeted fixes: INVERSOR constants, IO tags, DB path.</summary>
        private static void RewireXml(PlcSoftware plc, XDocument doc, ReplicateFcConfig config,
            string sourceId, string targetId, string sourceDbPath, string targetDbPath,
            int targetNumber, (string CcmName, string QaFolderName) ccm, List<string> warnings)
        {
            string sourceIdU = sourceId.Replace("-", "_");
            string targetIdU = targetId.Replace("-", "_");
            string numberText = targetNumber.ToString();
            var sourceNumbers = config.SourceNumbersToReplace ?? new List<string>();

            foreach (var node in doc.Descendants())
            {
                foreach (var attribute in node.Attributes())
                {
                    foreach (var n in sourceNumbers)
                        if (attribute.Value.Contains(n))
                            attribute.Value = attribute.Value.Replace(n, numberText);
                    if (attribute.Value.Contains(sourceId) || attribute.Value.Contains(sourceIdU))
                        attribute.Value = attribute.Value.Replace(sourceId, targetId).Replace(sourceIdU, targetIdU);
                }
                if (!node.HasElements && !string.IsNullOrEmpty(node.Value))
                {
                    foreach (var n in sourceNumbers)
                        if (node.Value.Contains(n))
                            node.Value = node.Value.Replace(n, numberText);
                    if (node.Value.Contains(sourceId) || node.Value.Contains(sourceIdU))
                        node.Value = node.Value.Replace(sourceId, targetId).Replace(sourceIdU, targetIdU);
                }
            }

            // INVERSOR_<id>_CCMn~... constants must carry the target's own CCM
            foreach (var element in doc.Descendants().Where(e => e.Name.LocalName == "Constant"))
            {
                var nameAttr = element.Attribute("Name");
                if (nameAttr == null || !nameAttr.Value.StartsWith("INVERSOR_")) continue;
                var match = Regex.Match(nameAttr.Value, @"^(INVERSOR_)(.+?)(_CCM\d+)(~.*)$", RegexOptions.IgnoreCase);
                if (match.Success)
                    nameAttr.Value = match.Groups[1].Value + targetId + "_" + ccm.CcmName + match.Groups[4].Value;
            }

            // MODO_LOCAL / MODO_REMOTO IO tags: point at the target's real tag in its QA folder
            var qaTagFolder = ccm.QaFolderName != "QA_INDEFINIDO"
                ? Profinet.FindTagGroup(plc.TagTableGroup, ccm.QaFolderName) : null;
            if (qaTagFolder == null)
            {
                warnings.Add("Tag folder '" + ccm.QaFolderName + "' not found; IO tag fix skipped for '" + targetId + "'.");
            }
            else
            {
                var modoLocal = FindTag(qaTagFolder, targetId, "MODO_LOCAL");
                var modoRemoto = FindTag(qaTagFolder, targetId, "MODO_REMOTO");
                if (modoLocal != null || modoRemoto != null)
                {
                    XNamespace ns = "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v5";
                    foreach (var component in doc.Descendants(ns + "Component"))
                    {
                        var nameAttr = component.Attribute("Name");
                        if (nameAttr == null || !nameAttr.Value.StartsWith(targetId, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (nameAttr.Value.IndexOf("MODO_LOCAL", StringComparison.OrdinalIgnoreCase) >= 0
                            && nameAttr.Value.Length > targetId.Length + 15 && modoLocal != null)
                            nameAttr.Value = modoLocal.Name;
                        else if (nameAttr.Value.IndexOf("MODO_REMOTO", StringComparison.OrdinalIgnoreCase) >= 0
                            && nameAttr.Value.Length > targetId.Length + 16 && modoRemoto != null)
                            nameAttr.Value = modoRemoto.Name;
                    }
                }
            }

            // global DB symbol paths: source instance path -> target instance path
            XNamespace dbNs = "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v5";
            var symbols = doc.Descendants(dbNs + "Symbol")
                .Where(s => s.Elements().FirstOrDefault()?.Attribute("Name")?.Value
                    .Equals(config.GlobalDb, StringComparison.OrdinalIgnoreCase) ?? false)
                .ToList();
            foreach (var symbol in symbols)
            {
                var parts = symbol.Elements().Skip(1).Select(c => c.Attribute("Name").Value).ToList();
                string rewiredSourcePath = sourceDbPath.Replace(sourceId, targetId).Replace(sourceIdU, targetIdU);
                int sourceDepth = rewiredSourcePath.Split('.').Length;
                string currentPath = string.Join(".", parts.Take(sourceDepth));
                if (!currentPath.Equals(rewiredSourcePath, StringComparison.OrdinalIgnoreCase)) continue;

                var suffix = parts.Skip(sourceDepth).ToList();
                symbol.RemoveAll();
                symbol.Add(new XElement(dbNs + "Component", new XAttribute("Name", config.GlobalDb)));
                foreach (var part in targetDbPath.Split('.'))
                    symbol.Add(new XElement(dbNs + "Component", new XAttribute("Name", part)));
                foreach (var part in suffix)
                    symbol.Add(new XElement(dbNs + "Component", new XAttribute("Name", part)));
            }
        }

        /// <summary>Static member of one of the UDT types whose name contains the equipment ID -> dotted path.</summary>
        internal static string FindPathInDbXml(XDocument dbXml, string equipmentId, List<string> udtNames)
        {
            string idU = equipmentId.Replace("-", "_");
            XNamespace ns = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
            var section = dbXml.Descendants(ns + "Section").FirstOrDefault(s => s.Attribute("Name")?.Value == "Static");
            if (section == null) throw new InvalidOperationException("'Static' section not found in global DB XML.");

            var member = section.Descendants(ns + "Member").FirstOrDefault(m =>
            {
                string name = m.Attribute("Name")?.Value;
                if (string.IsNullOrEmpty(name)) return false;
                bool idMatch = name.IndexOf(equipmentId, StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf(idU, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!idMatch) return false;
                string type = m.Attribute("Datatype")?.Value;
                if (string.IsNullOrEmpty(type)) return false;
                type = type.Replace("\"", "");
                if (type.Contains('.')) type = type.Substring(type.LastIndexOf('.') + 1);
                return udtNames.Contains(type, StringComparer.OrdinalIgnoreCase);
            });
            if (member == null)
                throw new InvalidOperationException("No instance of [" + string.Join(", ", udtNames) +
                    "] found in the global DB for ID '" + equipmentId + "'.");

            var parts = new Stack<string>();
            var current = member;
            while (current != null && current.Name == ns + "Member")
            {
                parts.Push(current.Attribute("Name").Value);
                current = current.Parent;
            }
            return string.Join(".", parts);
        }

        // ---------- naming ----------

        internal static string ExtractId(string name)
        {
            return Regex.Match(name, @"\(([^)]+)\)").Groups[1].Value;
        }

        /// <summary>Template's main block gets "PARTIDA_<target folder>", others get plain ID substitution.</summary>
        private static string ProposedBlockName(string original, string sourceFolder, string targetFolder,
            string sourceId, string targetId)
        {
            string baseName = FolderBaseName(sourceFolder).ToUpper().Replace(" ", "_");
            if (original.ToUpper().Replace(" ", "_").Contains(baseName))
                return MainBlockName(targetFolder);
            return original.Replace(sourceId, targetId)
                .Replace(sourceId.Replace("-", "_"), targetId.Replace("-", "_"));
        }

        private static string MainBlockName(string folderName)
        {
            var m = Regex.Match(folderName, @"^(.*?)\s*\((.*?)\)$");
            if (!m.Success) return folderName;
            return "PARTIDA_" + m.Groups[1].Value.Trim().ToUpper().Replace(" ", "_") + " (" + m.Groups[2].Value + ")";
        }

        private static string FolderBaseName(string folderName)
        {
            string beforeParen = Regex.Match(folderName, @"^(.*?)\s*\(.*\)$").Groups[1].Value.Trim();
            string baseName = Regex.Replace(beforeParen, @"\s*\d+\s*$", "").Trim();
            return string.IsNullOrEmpty(baseName) ? beforeParen : baseName;
        }

        /// <summary>Nearest ancestor folder named CCMn -> ("CCMn", "QA-0n").</summary>
        internal static (string CcmName, string QaFolderName) FindCcmInfo(PlcBlockUserGroup folder)
        {
            PlcBlockGroup current = folder;
            while (current != null)
            {
                var match = Regex.Match(current.Name, @"CCM(\d+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string ccm = match.Groups[1].Value;
                    string qa = int.TryParse(ccm, out int n) ? n.ToString("D2") : ccm;
                    return ("CCM" + ccm, "QA-" + qa);
                }
                current = current.Parent as PlcBlockGroup;
            }
            return ("CCM_INDEFINIDO", "QA_INDEFINIDO");
        }

        // ---------- lookups ----------

        internal static PlcBlockUserGroup FindGroup(PlcBlockGroup start, string name)
        {
            if (start is PlcBlockUserGroup user && user.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return user;
            foreach (PlcBlockUserGroup sub in start.Groups)
            {
                var found = FindGroup(sub, name);
                if (found != null) return found;
            }
            return null;
        }

        internal static List<PlcBlockUserGroup> DescendantGroups(PlcBlockUserGroup root)
        {
            var list = new List<PlcBlockUserGroup>();
            var queue = new Queue<PlcBlockUserGroup>();
            foreach (PlcBlockUserGroup sub in root.Groups) queue.Enqueue(sub);
            while (queue.Count > 0)
            {
                var g = queue.Dequeue();
                list.Add(g);
                foreach (PlcBlockUserGroup sub in g.Groups) queue.Enqueue(sub);
            }
            return list;
        }

        internal static DataBlock FindDataBlock(PlcBlockGroup group, string name)
        {
            var hit = group.Blocks.FirstOrDefault(b => b is DataBlock
                && b.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) as DataBlock;
            if (hit != null) return hit;
            foreach (PlcBlockUserGroup sub in group.Groups)
            {
                hit = FindDataBlock(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static PlcTag FindTag(PlcTagTableUserGroup group, string idPrefix, string keyword)
        {
            foreach (PlcTagTable table in group.TagTables)
            {
                var hit = table.Tags.FirstOrDefault(t =>
                    t.Name.StartsWith(idPrefix, StringComparison.OrdinalIgnoreCase) &&
                    t.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                if (hit != null) return hit;
            }
            foreach (PlcTagTableUserGroup sub in group.Groups)
            {
                var hit = FindTag(sub, idPrefix, keyword);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
