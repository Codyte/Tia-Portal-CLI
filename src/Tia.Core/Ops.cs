using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;

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

        /// <summary>Busca recursiva de pasta de blocos por nome (case-insensitive).</summary>
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

        /// <summary>Busca recursiva de pasta de tag tables por nome (case-insensitive).</summary>
        internal static PlcTagTableUserGroup FindTagGroup(PlcTagTableGroup start, string name)
        {
            var user = start as PlcTagTableUserGroup;
            if (user != null && user.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return user;
            foreach (PlcTagTableUserGroup sub in start.Groups)
            {
                var found = FindTagGroup(sub, name);
                if (found != null) return found;
            }
            return null;
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

        internal static PlcTagTable FindTagTable(PlcTagTableGroup group, string name)
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

        /// <summary>Tag table folder path "A/B" under PLC tags. create=false throws if missing.</summary>
        public static PlcTagTableGroup ResolveTagFolder(PlcSoftware plc, string path, bool create)
        {
            PlcTagTableGroup current = plc.TagTableGroup;
            if (string.IsNullOrEmpty(path)) return current;
            foreach (var part in path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var next = current.Groups.Find(part);
                if (next == null)
                {
                    if (!create)
                        throw new InvalidOperationException("Tag folder not found: '" + part + "' (in '" + path + "').");
                    next = current.Groups.Create(part);
                }
                current = next;
            }
            return current;
        }

        internal static PlcType FindType(PlcTypeGroup group, string name)
        {
            var hit = group.Types.Find(name);
            if (hit != null) return hit;
            foreach (PlcTypeUserGroup sub in group.Groups)
            {
                hit = FindType(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }

        // ---------- structure ----------

        public static object CreateFolder(PlcSoftware plc, string path, bool tags, bool apply)
        {
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("--path required.");
            bool exists;
            try
            {
                if (tags) ResolveTagFolder(plc, path, false); else ResolveFolder(plc, path, false);
                exists = true;
            }
            catch (InvalidOperationException) { exists = false; }
            var result = new Dictionary<string, object>
            {
                { "path", path },
                { "kind", tags ? "tag-folder" : "block-folder" },
                { "action", exists ? "none (exists)" : "create" },
                { "applied", apply },
            };
            if (apply && !exists)
            {
                if (tags) ResolveTagFolder(plc, path, true); else ResolveFolder(plc, path, true);
            }
            return result;
        }

        public static object DeleteFolder(PlcSoftware plc, string path, bool tags, bool apply)
        {
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("--path required (refusing to delete root).");
            var result = new Dictionary<string, object>
            {
                { "path", path },
                { "kind", tags ? "tag-folder" : "block-folder" },
                { "applied", apply },
            };
            if (tags)
            {
                var group = (PlcTagTableUserGroup)ResolveTagFolder(plc, path, false);
                result["tables"] = CountTables(group);
                if (apply) group.Delete();
            }
            else
            {
                var group = (PlcBlockUserGroup)ResolveFolder(plc, path, false);
                result["blocks"] = CountBlocks(group);
                if (apply) group.Delete();
            }
            return result;
        }

        private static int CountBlocks(PlcBlockGroup group)
        {
            return group.Blocks.Count + group.Groups.Cast<PlcBlockUserGroup>().Sum(g => CountBlocks(g));
        }

        private static int CountTables(PlcTagTableGroup group)
        {
            return group.TagTables.Count + group.Groups.Cast<PlcTagTableUserGroup>().Sum(g => CountTables(g));
        }

        public static object DeleteBlock(PlcSoftware plc, string name, bool apply)
        {
            var block = FindBlock(plc, name);
            if (block == null)
                throw new InvalidOperationException("Block '" + name + "' not found.");
            var result = new Dictionary<string, object>
            {
                { "block", name },
                { "type", block.GetType().Name },
                { "applied", apply },
            };
            if (apply) block.Delete();
            return result;
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

        public static object ExportType(PlcSoftware plc, string name, string outDir)
        {
            var type = FindType(plc.TypeGroup, name);
            if (type == null)
                throw new InvalidOperationException("UDT '" + name + "' not found.");
            var file = ExportPath(outDir, name);
            type.Export(new FileInfo(file), ExportOptions.WithDefaults);
            return new Dictionary<string, object> { { "exported", name }, { "file", file } };
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
            RequireRootType(full, "SW.Blocks.");
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

        public static object ImportTagTable(PlcSoftware plc, string file, string folderPath, bool apply)
        {
            var full = RequireFile(file);
            RequireRootType(full, "SW.Tags.PlcTagTable");
            var name = XmlObjectName(full);
            var result = new Dictionary<string, object>
            {
                { "file", full },
                { "table", name },
                { "folder", folderPath ?? "" },
                { "action", name != null && FindTagTable(plc.TagTableGroup, name) != null ? "override" : "create" },
                { "applied", apply },
            };
            if (apply)
                ResolveTagFolder(plc, folderPath, true).TagTables.Import(new FileInfo(full), ImportOptions.Override);
            return result;
        }

        public static object ImportType(PlcSoftware plc, string file, bool apply)
        {
            var full = RequireFile(file);
            RequireRootType(full, "SW.Types.");
            var name = XmlObjectName(full);
            var result = new Dictionary<string, object>
            {
                { "file", full },
                { "type", name },
                { "action", name != null && FindType(plc.TypeGroup, name) != null ? "override" : "create" },
                { "applied", apply },
            };
            if (apply)
                plc.TypeGroup.Types.Import(new FileInfo(full), ImportOptions.Override);
            return result;
        }

        /// <summary>SCL/AWL/DB/UDT source → blocks via ExternalSourceGroup + GenerateBlocksFromSource.</summary>
        public static object ImportSource(PlcSoftware plc, string file, bool apply)
        {
            var full = RequireFile(file);
            var ext = Path.GetExtension(full).ToLowerInvariant();
            var known = new[] { ".scl", ".awl", ".st", ".db", ".udt" };
            if (!known.Contains(ext))
                throw new InvalidOperationException(
                    "Unsupported source extension '" + ext + "'. Use: " + string.Join(", ", known));

            var declared = SourceBlockNames(full);
            var result = new Dictionary<string, object>
            {
                { "file", full },
                { "blocks", declared },
                { "applied", apply },
            };
            if (apply)
            {
                var sources = plc.ExternalSourceGroup.ExternalSources;
                var sourceName = Path.GetFileName(full); // extension on the name tells Openness the source type
                sources.Find(sourceName)?.Delete();
                var source = sources.CreateFromFile(sourceName, full);
                try
                {
                    source.GenerateBlocksFromSource();
                }
                finally
                {
                    source.Delete(); // source is scaffolding; generated blocks stay
                }
                result["generated"] = declared.Where(n => FindBlock(plc, n) != null).ToList();
            }
            return result;
        }

        /// <summary>Block/type names declared in a source file (dry-run report; not a full parser).</summary>
        private static List<string> SourceBlockNames(string file)
        {
            var rx = new Regex(
                @"^\s*(?:FUNCTION_BLOCK|ORGANIZATION_BLOCK|DATA_BLOCK|FUNCTION|TYPE)\s+(?:""([^""]+)""|([^\s:]+))",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return rx.Matches(File.ReadAllText(file)).Cast<Match>()
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Distinct().ToList();
        }

        private static string RequireFile(string file)
        {
            var full = Path.GetFullPath(file);
            if (!File.Exists(full)) throw new FileNotFoundException("Import file not found: " + full);
            return full;
        }

        /// <summary>Root object type of a Siemens export XML (SW.Blocks.FC, SW.Tags.PlcTagTable, SW.Types.PlcStruct…).</summary>
        public static string XmlRootType(string file)
        {
            var root = XDocument.Load(file).Root;
            if (root == null) return null;
            return root.Elements()
                .Select(e => e.Name.LocalName)
                .FirstOrDefault(n => n != "Engineering" && n != "DocumentInfo");
        }

        /// <summary>Dry-run must fail on the wrong XML kind — Openness only rejects it at Import().</summary>
        public static void RequireRootType(string file, string expectedPrefix)
        {
            var root = XmlRootType(file);
            if (root == null || !root.StartsWith(expectedPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "XML root object is '" + (root ?? "none") + "', expected '" + expectedPrefix + "*': " + file);
        }

        /// <summary>Culturas dos textos multilíngues de um export XML (elemento &lt;Culture&gt;pt-BR&lt;/Culture&gt;).</summary>
        internal static IEnumerable<string> XmlCultures(string file)
        {
            return XDocument.Load(file).Descendants()
                .Where(e => e.Name.LocalName == "Culture")
                .Select(e => e.Value.Trim())
                .Where(c => c.Length > 0)
                .Distinct();
        }

        /// <summary>
        /// Culturas do XML que o projeto ainda não tem ativas (apply=true ativa). Sem isso o import
        /// morre com "Cannot import multilingual text with culture 'pt-BR' ... does not exist within
        /// the current project" — projeto novo nasce só com a cultura de instalação do TIA.
        /// </summary>
        public static List<string> EnsureCultures(ProjectBase project, IEnumerable<string> cultures, bool apply)
        {
            var missing = new List<string>();
            var settings = project.LanguageSettings;
            foreach (var name in cultures.Distinct())
            {
                CultureInfo culture;
                try { culture = CultureInfo.GetCultureInfo(name); }
                catch (CultureNotFoundException) { continue; } // cultura do XML que o Windows não conhece
                if (settings.ActiveLanguages.Find(culture) != null) continue;
                missing.Add(name);
                var language = settings.Languages.Find(culture);
                if (language == null)
                    throw new InvalidOperationException(
                        "Culture '" + name + "' is not available in this TIA installation; install the "
                        + "language pack or export the XMLs in a culture the project supports.");
                if (apply) settings.ActiveLanguages.Add(language);
            }
            return missing;
        }

        /// <summary>First AttributeList/Name in a Siemens export XML = object name (block, tag table…).</summary>
        internal static string XmlObjectName(string file)
        {
            var doc = XDocument.Load(file);
            return doc.Descendants()
                .Where(e => e.Name.LocalName == "AttributeList")
                .SelectMany(e => e.Elements())
                .FirstOrDefault(e => e.Name.LocalName == "Name")?.Value;
        }

        // ---------- diff ----------

        /// <summary>Compares an export XML against the project block, normalized (UIds/IDs, informative addresses).</summary>
        public static object DiffBlock(PlcSoftware plc, string name, string file)
        {
            var full = RequireFile(file);
            name = name ?? XmlObjectName(full);
            if (name == null)
                throw new InvalidOperationException("Could not read block name from XML; pass --name.");
            var block = FindBlock(plc, name);
            if (block == null)
                throw new InvalidOperationException("Block '" + name + "' not found.");
            return new Dictionary<string, object>
            {
                { "block", name },
                { "file", full },
                { "identical", BlocksIdentical(block, full, false) },
            };
        }

        /// <summary>Normalized XML compare: strips UId/ID, informative addresses, optionally comments.</summary>
        public static bool BlocksIdentical(PlcBlock existing, string newXmlPath, bool ignoreComments)
        {
            string tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
            try
            {
                existing.Export(new FileInfo(tmp), ExportOptions.None);
                var generated = XDocument.Load(newXmlPath).Descendants("ObjectList").FirstOrDefault();
                var current = XDocument.Load(tmp).Descendants("ObjectList").FirstOrDefault();
                if (generated == null || current == null) return false;
                var a = new XElement(generated);
                var b = new XElement(current);
                foreach (var container in new[] { a, b })
                {
                    // DeepEquals is namespace-sensitive and TIA re-serializes FlgNet children with
                    // shifting xmlns="" declarations — compare by local name only
                    foreach (var e in container.DescendantsAndSelf())
                    {
                        e.Name = e.Name.LocalName;
                        e.Attributes().Where(x => x.IsNamespaceDeclaration).Remove();
                        e.Attribute("UId")?.Remove();
                        e.Attribute("ID")?.Remove();
                    }
                    // informative elements (addresses, BlockNumber on CallInfo, …) are ignored on
                    // import and TIA exports omit some of them — never a real difference
                    container.DescendantsAndSelf()
                        .Where(x => x.Attribute("Informative")?.Value == "true").Remove();
                    if (ignoreComments)
                        container.Descendants("MultilingualText")
                            .Where(x => x.Attribute("CompositionName")?.Value == "Comment").Remove();
                    // TIA exports reorder ObjectList children (Title first vs last);
                    // stable sort by element name — same-name siblings (CompileUnits) keep order
                    foreach (var ol in container.DescendantsAndSelf()
                        .Where(x => x.Name.LocalName == "ObjectList"))
                        ol.ReplaceNodes(ol.Elements()
                            .OrderBy(x => x.Name.LocalName, StringComparer.Ordinal).ToList());
                }
                return XNode.DeepEquals(a, b);
            }
            catch { return false; }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }

        // ---------- compile ----------

        /// <summary>Compile whole PLC, one block (--block) or a folder (--folder).</summary>
        public static object Compile(PlcSoftware plc, string blockName, string folderPath)
        {
            IEngineeringServiceProvider target = plc;
            string scope = "plc " + plc.Name;
            if (blockName != null)
            {
                var block = FindBlock(plc, blockName);
                if (block == null)
                    throw new InvalidOperationException("Block '" + blockName + "' not found.");
                target = block;
                scope = "block " + blockName;
            }
            else if (folderPath != null)
            {
                target = ResolveFolder(plc, folderPath, false);
                scope = "folder " + folderPath;
            }
            var compiler = target.GetService<ICompilable>();
            if (compiler == null)
                throw new InvalidOperationException(scope + " is not compilable.");
            var result = compiler.Compile();
            return new Dictionary<string, object>
            {
                { "scope", scope },
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
