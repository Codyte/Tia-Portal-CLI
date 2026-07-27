using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Tia.Core
{
    /// <summary>Config for gen-fault-ob (port of "Gerador de OB Falha Módulos").</summary>
    public class FaultObConfig
    {
        /// <summary>Device groups named "&lt;GroupPrefix&gt;xx" are scanned; "HW_" is stripped for the QA name.</summary>
        public string GroupPrefix { get; set; } = "HW_QA-";
        public string TemplateOb { get; set; } = "MODULE_ERROR_MOLDE";
        public string ObNamePrefix { get; set; } = "OB_DIAG_";
        /// <summary>
        /// Struct whose WORD_n bits receive the per-module alarm. NOT a block: no padrão real vive
        /// dentro da DB global (DB GLOBAL.HARDWARE_INTERRUPT.ALARMES_MODULOS.QA-xx.WORD_n.xB) —
        /// só existe como Component do Symbol no FlgNet do template OB.
        /// </summary>
        public string AlarmDb { get; set; } = "ALARMES_MODULOS";
        public List<string> CommentCultures { get; set; } = new List<string> { "pt-BR", "en-US" };
    }

    /// <summary>
    /// Generates one diagnostics OB per QA device group: exports the template OB, clones its
    /// network per hardware module (HW ID + alarm bit rewired), writes the XMLs + a CSV bit map,
    /// and with apply imports the OBs (overriding old versions) into the template's folder.
    /// </summary>
    public static class FaultOb
    {
        private static int _uidCounter = 1000; // single-shot CLI process, no concurrency (D9)

        private static readonly XNamespace FlgNetNs =
            "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v5";

        internal class Module
        {
            public string Name;
            public uint HardwareId;
            public string QaOwner;
        }

        public static object Generate(TiaSession session, PlcSoftware plc, FaultObConfig config,
            string outDir, bool apply)
        {
            // an XML MultilingualTextItem whose culture the project doesn't have fails the whole import
            var projectCultures = session.Project.LanguageSettings.ActiveLanguages
                .Select(l => l.Culture.Name).ToList();
            config.CommentCultures = config.CommentCultures
                .Where(c => projectCultures.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
            if (config.CommentCultures.Count == 0)
                config.CommentCultures = projectCultures.Take(1).ToList();

            var tasks = DiscoverTasks(session, config);
            if (tasks.Count == 0)
                throw new InvalidOperationException(
                    "No device group matching '" + config.GroupPrefix + "*' with modules found.");

            var template = Ops.FindBlock(plc, config.TemplateOb);
            if (template == null)
                throw new InvalidOperationException("Template OB '" + config.TemplateOb + "' not found.");
            var targetGroup = template.Parent as PlcBlockGroup;
            if (targetGroup == null)
                throw new InvalidOperationException(
                    "Could not resolve the folder containing template '" + config.TemplateOb + "'.");

            Directory.CreateDirectory(outDir);
            var templatePath = Path.GetFullPath(Path.Combine(outDir, "template_cache.xml"));
            if (File.Exists(templatePath)) File.Delete(templatePath);
            template.Export(new FileInfo(templatePath), ExportOptions.None);
            var templateXml = XDocument.Load(templatePath);

            var warnings = new List<string>();
            var obs = new List<object>();
            foreach (var task in tasks.OrderBy(t => t.Key))
            {
                string obName = config.ObNamePrefix + task.Key.Replace('-', '_');
                string xmlPath = Path.GetFullPath(Path.Combine(outDir, obName + ".xml"));
                BuildObXml(templateXml, obName, task.Value, config).Save(xmlPath);

                bool exists = targetGroup.Blocks.Find(obName) != null;
                string action = exists ? "override" : "create";
                if (apply)
                {
                    try
                    {
                        var old = targetGroup.Blocks.Find(obName);
                        old?.Delete();
                        targetGroup.Blocks.Import(new FileInfo(xmlPath), ImportOptions.None);
                    }
                    catch (Exception ex)
                    {
                        // a failed OB must not abort the remaining OBs; XML stays on disk for retry
                        action = "error";
                        warnings.Add("OB '" + obName + "': import failed — " + ex.Message);
                    }
                }

                obs.Add(new Dictionary<string, object>
                {
                    { "ob", obName },
                    { "qa", task.Key },
                    { "modules", task.Value.Count },
                    { "xml", xmlPath },
                    { "action", action },
                });
            }

            var csvPath = WriteCsv(tasks, outDir);

            return new Dictionary<string, object>
            {
                { "template", config.TemplateOb },
                { "targetFolder", targetGroup.Name },
                { "applied", apply },
                { "obs", obs },
                { "csv", csvPath },
                { "warnings", warnings },
            };
        }

        /// <summary>QA name -> its modules (every DeviceItem with a HwIdentifier, recursively).</summary>
        internal static Dictionary<string, List<Module>> DiscoverTasks(TiaSession session, FaultObConfig config)
        {
            var tasks = new Dictionary<string, List<Module>>();
            foreach (var group in AllDeviceGroups(session))
            {
                if (!group.Name.StartsWith(config.GroupPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                string qaName = group.Name.StartsWith("HW_", StringComparison.OrdinalIgnoreCase)
                    ? group.Name.Substring(3) : group.Name;
                var modules = new List<Module>();
                foreach (var device in group.Devices)
                    CollectModules(device.DeviceItems, qaName, modules);
                if (modules.Any())
                    tasks[qaName] = modules;
            }
            return tasks;
        }

        private static IEnumerable<DeviceUserGroup> AllDeviceGroups(TiaSession session)
        {
            foreach (DeviceUserGroup group in session.Project.DeviceGroups)
                foreach (var g in WithSubGroups(group))
                    yield return g;
        }

        private static IEnumerable<DeviceUserGroup> WithSubGroups(DeviceUserGroup group)
        {
            yield return group;
            foreach (DeviceUserGroup sub in group.Groups)
                foreach (var g in WithSubGroups(sub))
                    yield return g;
        }

        private static void CollectModules(DeviceItemComposition items, string qaName, List<Module> into)
        {
            foreach (DeviceItem item in items)
            {
                var hwId = item.HwIdentifiers.FirstOrDefault();
                if (hwId != null)
                    into.Add(new Module { Name = item.Name, HardwareId = (uint)hwId.Identifier, QaOwner = qaName });
                CollectModules(item.DeviceItems, qaName, into);
            }
        }

        // ---------- XML generation ----------

        internal static XDocument BuildObXml(XDocument templateXml, string obName,
            List<Module> modules, FaultObConfig config)
        {
            var doc = new XDocument(templateXml);
            var root = doc.Root;

            root.Descendants("AttributeList").Elements("Name").First().Value = obName;
            var objectList = root.Descendants("ObjectList").First();
            var networkTemplate = templateXml.Descendants("SW.Blocks.CompileUnit").First();

            objectList.Elements().Remove();
            var masterComment = new XElement("MultilingualText",
                new XAttribute("ID", "1"), new XAttribute("CompositionName", "Comment"),
                new XElement("ObjectList"));
            objectList.Add(masterComment);

            int bitIndex = 0;
            foreach (var module in modules.OrderBy(m => m.HardwareId))
            {
                var network = new XElement(networkTemplate);
                ReassignUids(network, bitIndex * 100);
                RewireNetwork(network, module, bitIndex, config);
                objectList.Add(network);
                AddMasterCommentEntry(masterComment, module, bitIndex, config);
                bitIndex++;
            }
            return doc;
        }

        /// <summary>Retitles the cloned network, swaps the 999 HW ID placeholder, points the alarm bit.</summary>
        private static void RewireNetwork(XElement network, Module module, int bitIndex, FaultObConfig config)
        {
            var title = network.Descendants("MultilingualTextItem")
                .FirstOrDefault(x => x.Parent?.Parent?.Attribute("CompositionName")?.Value == "Title");
            if (title != null)
                title.Descendants("Text").First().Value =
                    module.Name + " " + module.QaOwner + " (" + ModuleType(module.Name) + ")";

            // placeholder 999 works for any constant type (UInt, HW_ANY, DInt...)
            foreach (var node in network.Descendants(FlgNetNs + "ConstantValue")
                         .Where(cv => cv.Value == "999").ToList())
                node.Value = module.HardwareId.ToString();

            int wordNumber = bitIndex / 16 + 1;
            int bitNumber = bitIndex % 16;
            var alarmAccess = network.Descendants(FlgNetNs + "Symbol")
                .FirstOrDefault(s => s.Elements(FlgNetNs + "Component")
                    .Any(c => c.Attribute("Name")?.Value == config.AlarmDb));
            if (alarmAccess == null)
                throw new InvalidOperationException(
                    "template OB '" + config.TemplateOb + "' has no access to '" + config.AlarmDb +
                    "' — every generated OB would keep the template's alarm bit. Check alarmDb in the config.");
            {
                var qa = alarmAccess.Elements(FlgNetNs + "Component")
                    .FirstOrDefault(c => c.Attribute("Name")?.Value.StartsWith("QA-") ?? false);
                if (qa != null) qa.Attribute("Name").Value = module.QaOwner;

                var word = alarmAccess.Elements(FlgNetNs + "Component")
                    .FirstOrDefault(c => c.Attribute("Name")?.Value.StartsWith("WORD_") ?? false);
                if (word != null)
                {
                    word.Attribute("Name").Value = "WORD_" + wordNumber;
                    word.Attribute("SliceAccessModifier").Value = "x" + bitNumber;
                }
            }
        }

        private static void AddMasterCommentEntry(XElement masterComment, Module module,
            int bitIndex, FaultObConfig config)
        {
            int wordNumber = bitIndex / 16 + 1;
            int bitNumber = bitIndex % 16;
            string comment = module.Name + " (" + module.QaOwner + ") = " +
                config.AlarmDb + "_WORD_" + wordNumber + " Bit " + bitNumber;

            var objectList = masterComment.Element("ObjectList");
            foreach (var culture in config.CommentCultures)
            {
                var item = objectList.Elements("MultilingualTextItem")
                    .FirstOrDefault(i => i.Descendants("Culture").FirstOrDefault()?.Value == culture);
                if (item == null)
                {
                    item = new XElement("MultilingualTextItem",
                        new XAttribute("ID", (++_uidCounter).ToString("X")),
                        new XAttribute("CompositionName", "Items"),
                        new XElement("AttributeList",
                            new XElement("Culture", culture),
                            new XElement("Text", "")));
                    objectList.Add(item);
                }
                var text = item.Descendants("Text").First();
                text.Value += (string.IsNullOrEmpty(text.Value) ? "" : "\n") + comment;
            }
        }

        /// <summary>Rewrites every UId/ID in the cloned network so networks don't collide.</summary>
        private static void ReassignUids(XElement element, int baseOffset)
        {
            var idMap = new Dictionary<string, string>();
            _uidCounter = 1000 + baseOffset;

            foreach (var d in element.DescendantsAndSelf())
            {
                var uid = d.Attribute("UId");
                if (uid != null && !idMap.ContainsKey(uid.Value))
                    idMap.Add(uid.Value, (++_uidCounter).ToString());
                var id = d.Attribute("ID");
                if (id != null && !idMap.ContainsKey(id.Value))
                    idMap.Add(id.Value, (++_uidCounter).ToString("X"));
            }
            foreach (var d in element.DescendantsAndSelf())
                foreach (var attr in d.Attributes())
                    if (idMap.TryGetValue(attr.Value, out string newId))
                        attr.Value = newId;
        }

        private static string ModuleType(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return "Módulo Desconhecido";
            if (moduleName.StartsWith("DI")) return "Módulo de Entradas Digitais";
            if (moduleName.StartsWith("DQ")) return "Módulo de Saídas Digitais";
            if (moduleName.StartsWith("AI")) return "Módulo de Entradas Analógicas";
            if (moduleName.StartsWith("AQ")) return "Módulo de Saídas Analógicas";
            if (moduleName.StartsWith("CPU")) return "CPU";
            return "Módulo de Sistema";
        }

        /// <summary>CSV bit map (QA; module; HW ID; alarm word; bit) for field documentation.</summary>
        private static string WriteCsv(Dictionary<string, List<Module>> tasks, string outDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("\"QA\";\"Nome do Módulo\";\"Hardware ID\";\"Word de Alarme\";\"Bit de Alarme\"");
            foreach (var task in tasks.OrderBy(t => t.Key))
            {
                int bitIndex = 0;
                foreach (var module in task.Value.OrderBy(m => m.HardwareId))
                {
                    int wordNumber = bitIndex / 16 + 1;
                    sb.AppendLine("\"" + task.Key + "\";\"" + module.Name + "\";" + module.HardwareId +
                        ";\"WORD_" + wordNumber + "\";" + bitIndex % 16);
                    bitIndex++;
                }
            }
            var path = Path.GetFullPath(Path.Combine(outDir, "fault-ob-bitmap.csv"));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }
    }
}
