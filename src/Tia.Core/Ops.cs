using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>Export / import / compile operations. Write ops take apply=false for dry-run.</summary>
    public static class Ops
    {
        // ---------- lookup ----------

        public static PlcBlock FindBlock(PlcSoftware plc, string name)
        {
            return FindBlockIn(plc.BlockGroup, name);
        }

        private static PlcBlock FindBlockIn(PlcBlockGroup group, string name)
        {
            var hit = group.Blocks.Find(name);
            if (hit != null) return hit;
            foreach (PlcBlockUserGroup sub in group.Groups)
            {
                hit = FindBlockIn(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>Folder path "A/B/C" under Program blocks. create=false throws if missing.</summary>
        public static PlcBlockGroup ResolveFolder(PlcSoftware plc, string path, bool create)
        {
            PlcBlockGroup current = plc.BlockGroup;
            if (string.IsNullOrEmpty(path)) return current;
            foreach (var part in path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var next = current.Groups.Find(part);
                if (next == null)
                {
                    if (!create)
                        throw new InvalidOperationException("Block folder not found: '" + part + "' (in '" + path + "').");
                    next = current.Groups.Create(part);
                }
                current = next;
            }
            return current;
        }

        private static PlcTagTable FindTagTable(PlcTagTableGroup group, string name)
        {
            var hit = group.TagTables.Find(name);
            if (hit != null) return hit;
            foreach (PlcTagTableUserGroup sub in group.Groups)
            {
                hit = FindTagTable(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }

        // ---------- export ----------

        public static object ExportBlock(PlcSoftware plc, string name, string outDir)
        {
            var block = FindBlock(plc, name);
            if (block == null)
                throw new InvalidOperationException("Block '" + name + "' not found.");
            var file = ExportPath(outDir, name);
            block.Export(new FileInfo(file), ExportOptions.WithDefaults);
            return new Dictionary<string, object> { { "exported", name }, { "file", file } };
        }

        public static object ExportTagTable(PlcSoftware plc, string tableName, string outDir)
        {
            var table = FindTagTable(plc.TagTableGroup, tableName);
            if (table == null)
                throw new InvalidOperationException("Tag table '" + tableName + "' not found.");
            var file = ExportPath(outDir, tableName);
            table.Export(new FileInfo(file), ExportOptions.WithDefaults);
            return new Dictionary<string, object> { { "exported", tableName }, { "file", file } };
        }

        private static string ExportPath(string outDir, string name)
        {
            Directory.CreateDirectory(outDir);
            var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var path = Path.GetFullPath(Path.Combine(outDir, safe + ".xml"));
            if (File.Exists(path)) File.Delete(path); // Openness refuses to overwrite
            return path;
        }

        // ---------- import ----------

        public static object ImportBlock(PlcSoftware plc, string file, string folderPath, bool apply)
        {
            var full = RequireFile(file);
            var name = XmlObjectName(full);
            var existing = name != null ? FindBlock(plc, name) : null;
            var result = new Dictionary<string, object>
            {
                { "file", full },
                { "block", name },
                { "folder", folderPath ?? "" },
                { "action", existing != null ? "override" : "create" },
                { "applied", apply },
            };
            if (apply)
            {
                var group = ResolveFolder(plc, folderPath, true);
                group.Blocks.Import(new FileInfo(full), ImportOptions.Override);
            }
            return result;
        }

        public static object ImportTagTable(PlcSoftware plc, string file, bool apply)
        {
            var full = RequireFile(file);
            var name = XmlObjectName(full);
            var result = new Dictionary<string, object>
            {
                { "file", full },
                { "table", name },
                { "action", name != null && FindTagTable(plc.TagTableGroup, name) != null ? "override" : "create" },
                { "applied", apply },
            };
            if (apply)
                plc.TagTableGroup.TagTables.Import(new FileInfo(full), ImportOptions.Override);
            return result;
        }

        private static string RequireFile(string file)
        {
            var full = Path.GetFullPath(file);
            if (!File.Exists(full)) throw new FileNotFoundException("Import file not found: " + full);
            return full;
        }

        /// <summary>First AttributeList/Name in a Siemens export XML = object name (block, tag table…).</summary>
        private static string XmlObjectName(string file)
        {
            var doc = XDocument.Load(file);
            return doc.Descendants()
                .Where(e => e.Name.LocalName == "AttributeList")
                .SelectMany(e => e.Elements())
                .FirstOrDefault(e => e.Name.LocalName == "Name")?.Value;
        }

        // ---------- compile ----------

        public static object Compile(PlcSoftware plc)
        {
            var result = plc.GetService<ICompilable>().Compile();
            return new Dictionary<string, object>
            {
                { "state", result.State.ToString() },
                { "errors", result.ErrorCount },
                { "warnings", result.WarningCount },
                { "messages", result.Messages.Select(MessageTree).ToList() },
            };
        }

        private static object MessageTree(CompilerResultMessage m)
        {
            return new Dictionary<string, object>
            {
                { "path", m.Path },
                { "state", m.State.ToString() },
                { "description", m.Description },
                { "messages", m.Messages.Select(MessageTree).ToList() },
            };
        }
    }
}
