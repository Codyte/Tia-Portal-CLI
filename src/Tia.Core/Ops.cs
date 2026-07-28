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
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; )
            {
                // nome de pasta pode conter '/' ("3. Alarmes/Eventos/Falhas") — casa o mais longo primeiro
                PlcBlockGroup next = null;
                int taken = 1;
                for (int j = parts.Length; j > i; j--)
                {
                    next = current.Groups.Find(string.Join("/", parts, i, j - i));
                    if (next != null) { taken = j - i; break; }
                }
                if (next == null)
                {
                    if (!create)
                        throw new InvalidOperationException("Block folder not found: '" + parts[i] + "' (in '" + path + "').");
                    next = current.Groups.Create(parts[i]);
                }
                current = next;
                i += taken;
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

        /// <summary>Pasta de UDT "A/B" sob PLC data types. Espelha ResolveFolder/ResolveTagFolder.</summary>
        public static PlcTypeGroup ResolveTypeFolder(PlcSoftware plc, string path, bool create)
        {
            PlcTypeGroup current = plc.TypeGroup;
            if (string.IsNullOrEmpty(path)) return current;
            foreach (var part in path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var next = current.Groups.Find(part);
                if (next == null)
                {
                    if (!create)
                        throw new InvalidOperationException("Type folder not found: '" + part + "' (in '" + path + "').");
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

        public static object CreateFolder(PlcSoftware plc, string path, bool tags, bool apply, bool types = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("--path required.");
            if (types) return TypeFolderAction(plc, path, apply, false);
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

        public static object DeleteFolder(PlcSoftware plc, string path, bool tags, bool apply, bool types = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("--path required (refusing to delete root).");
            if (types) return TypeFolderAction(plc, path, apply, true);
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

        // create-folder/delete-folder --types: pasta de UDT era o único dos três tipos de pasta sem verbo.
        private static object TypeFolderAction(PlcSoftware plc, string path, bool apply, bool delete)
        {
            bool exists;
            try { ResolveTypeFolder(plc, path, false); exists = true; }
            catch (InvalidOperationException) { if (delete) throw; exists = false; }
            var result = new Dictionary<string, object>
            {
                { "path", path }, { "kind", "type-folder" },
                { "action", delete ? "delete" : (exists ? "none (exists)" : "create") },
                { "applied", apply },
            };
            if (delete)
            {
                var group = (PlcTypeUserGroup)ResolveTypeFolder(plc, path, false);
                result["types"] = CountTypes(group);
                if (apply) group.Delete();
            }
            else if (apply && !exists) ResolveTypeFolder(plc, path, true);
            return result;
        }

        private static int CountTypes(PlcTypeGroup group)
        {
            return group.Types.Count + group.Groups.Cast<PlcTypeUserGroup>().Sum(g => CountTypes(g));
        }

        private static int CountBlocks(PlcBlockGroup group)
        {
            return group.Blocks.Count + group.Groups.Cast<PlcBlockUserGroup>().Sum(g => CountBlocks(g));
        }

        private static int CountTables(PlcTagTableGroup group)
        {
            return group.TagTables.Count + group.Groups.Cast<PlcTagTableUserGroup>().Sum(g => CountTables(g));
        }

        /// <summary>
        /// Cria o DB de instância de uma chamada de FB. Molde importado por XML chega sem os iDBs
        /// (o Portal os cria no editor, o export não os leva) → "Missing instance DB" no compile.
        /// </summary>
        public static object CreateInstanceDb(PlcSoftware plc, string name, string ofBlock,
            string folderPath, bool apply)
        {
            var fb = FindBlock(plc, ofBlock) as FB;
            if (fb == null)
                throw new InvalidOperationException("FB '" + ofBlock + "' not found (instance DB needs an FB).");
            var existing = FindBlock(plc, name);
            var result = new Dictionary<string, object>
            {
                { "name", name }, { "instanceOf", fb.Name }, { "folder", folderPath ?? "" },
                { "action", existing == null ? "create" : "skip (exists)" }, { "applied", apply },
            };
            if (!apply || existing != null) return result;
            var group = ResolveFolder(plc, folderPath, true);
            var db = group.Blocks.CreateInstanceDB(name, true, 1, fb.Name);
            result["created"] = db.Name;
            result["number"] = db.Number;
            return result;
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

        public static object DeleteType(PlcSoftware plc, string name, bool apply)
        {
            var type = FindType(plc.TypeGroup, name);
            if (type == null)
                throw new InvalidOperationException("UDT '" + name + "' not found.");
            var result = new Dictionary<string, object>
            {
                { "type", name },
                { "applied", apply },
            };
            if (apply) type.Delete();
            return result;
        }

        // ---------- export ----------

        public static object ExportBlock(PlcSoftware plc, string name, string outDir)
        {
            var block = FindBlock(plc, name);
            if (block == null)
                throw new InvalidOperationException("Block '" + name + "' not found.");
            // export-block, explain-block, diff-block e clone passam por aqui; sem o guard o Openness
            // devolve só "Inconsistent blocks and PLC data types (UDT) cannot be exported."
            if (!block.IsConsistent)
                throw new InvalidOperationException("Block '" + name + "' is inconsistent (imported or edited, "
                    + "never compiled). Run: tia compile --block \"" + name + "\" --apply");
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

        /// <summary>
        /// Move bloco(s) de pasta. O Openness não tem move: é export → delete → import --folder,
        /// e nessa ordem — importar com o original ainda no lugar falha com "A program element with
        /// this fully qualified name already exists in this CPU". Exporta TODOS antes de apagar o
        /// primeiro: o delete deixa quem referencia inconsistente, e bloco inconsistente não exporta.
        /// Os XMLs ficam em outDir (default %TEMP%\tia-move) — se um import falhar, o bloco está lá.
        /// </summary>
        public static object MoveBlock(PlcSoftware plc, string name, string pattern, string folderPath,
            string outDir, bool apply)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new InvalidOperationException("--folder required (destino do move).");
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(pattern))
                throw new InvalidOperationException("--name X or --pattern P* required.");
            var target = folderPath.Trim('/');
            var rx = new Regex("^" + Regex.Escape(name ?? pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase);

            var hits = ((List<object>)Inventory.Blocks(plc)).Cast<Dictionary<string, object>>()
                .Where(b => rx.IsMatch((string)b["name"]))
                .ToList();
            if (hits.Count == 0)
                throw new InvalidOperationException("No block matches '" + (name ?? pattern) + "'.");
            var todo = hits.Where(b => !string.Equals((string)b["folder"], target, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var dir = string.IsNullOrEmpty(outDir) ? Path.Combine(Path.GetTempPath(), "tia-move") : outDir;
            var moves = todo.Select(b => new Dictionary<string, object>
            {
                { "block", b["name"] }, { "from", b["folder"] }, { "to", target },
            }).ToList();
            var result = new Dictionary<string, object>
            {
                { "matched", hits.Count },
                { "alreadyThere", hits.Count - todo.Count },
                { "moves", moves },
                { "xmlDir", dir },
                { "applied", apply },
            };
            if (!apply || todo.Count == 0) return result;

            // 1) exporta tudo primeiro — depois do primeiro delete, quem referencia fica inconsistente
            var files = new List<string>();
            foreach (var b in todo)
                files.Add((string)((Dictionary<string, object>)ExportBlock(plc, (string)b["name"], dir))["file"]);

            // 2) só então apaga e reimporta no destino
            var failed = new List<object>();
            for (int i = 0; i < todo.Count; i++)
            {
                var blockName = (string)todo[i]["name"];
                try
                {
                    DeleteBlock(plc, blockName, true);
                    ImportBlock(plc, files[i], target, true);
                }
                catch (Exception ex)
                {
                    failed.Add(new Dictionary<string, object>
                    {
                        { "block", blockName }, { "xml", files[i] }, { "error", ex.Message },
                    });
                }
            }
            result["moved"] = todo.Count - failed.Count;
            result["failed"] = failed;
            if (failed.Count > 0)
                result["recover"] = "reimportar à mão: tia import-block --file <xml> --folder \"" + target + "\" --apply";
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

        /// <summary>
        /// Cria uma tag numa tabela existente. Sem isso, acrescentar uma tag exige montar o XML da
        /// tabela inteira e reimportar (foi como a `Genericos.xml` nasceu). Idempotente: tag que já
        /// existe é `skip`, com o tipo/endereço atual no resultado pra conferência.
        /// </summary>
        public static object AddTag(PlcSoftware plc, string tableName, string name, string dataType,
            string address, string comment, bool apply)
        {
            var table = FindTagTable(plc.TagTableGroup, tableName);
            if (table == null)
                throw new InvalidOperationException("Tag table '" + tableName + "' not found.");
            var existing = table.Tags.Find(name);
            var result = new Dictionary<string, object>
            {
                { "table", table.Name },
                { "tag", name },
                { "action", existing == null ? "create" : "skip (exists)" },
                { "applied", apply },
            };
            if (existing != null)
            {
                result["currentType"] = existing.DataTypeName;
                result["currentAddress"] = existing.LogicalAddress;
                return result;
            }
            if (dataType == null || address == null)
                throw new InvalidOperationException(
                    "--type and --address are required to create tag '" + name
                    + "' (o Openness não escolhe endereço: PlcTagComposition.Create(name, type, address)). "
                    + "Buraco livre em %M: tia free-memory.");
            result["type"] = dataType;
            result["address"] = address;
            if (!apply) return result;

            var tag = table.Tags.Create(name, dataType, address);
            if (comment != null) tag.Comment.Items[0].Text = comment;
            result["created"] = tag.Name;
            result["addressCreated"] = tag.LogicalAddress;
            return result;
        }

        public static object DeleteTag(PlcSoftware plc, string tableName, string name, bool apply)
        {
            var table = FindTagTable(plc.TagTableGroup, tableName);
            if (table == null)
                throw new InvalidOperationException("Tag table '" + tableName + "' not found.");
            var tag = table.Tags.Find(name);
            if (tag == null)
                throw new InvalidOperationException("Tag '" + name + "' not found in '" + table.Name + "'.");
            var result = new Dictionary<string, object>
            {
                { "table", table.Name }, { "tag", tag.Name },
                { "type", tag.DataTypeName }, { "address", tag.LogicalAddress },
                { "applied", apply },
            };
            if (apply) tag.Delete();
            return result;
        }

        /// <summary>
        /// Edita uma tag existente: tipo, endereço, comentário, nome. Todos os campos são opcionais —
        /// o que vier null fica como está. Sem `--apply` só mostra o antes/depois.
        /// (PlcTag: DataTypeName/LogicalAddress/Name são Read+Write; Name só a partir do Openness V20.)
        /// </summary>
        public static object SetTag(PlcSoftware plc, string tableName, string name, string dataType,
            string address, string comment, string rename, bool apply)
        {
            var table = FindTagTable(plc.TagTableGroup, tableName);
            if (table == null)
                throw new InvalidOperationException("Tag table '" + tableName + "' not found.");
            var tag = table.Tags.Find(name);
            if (tag == null)
                throw new InvalidOperationException("Tag '" + name + "' not found in '" + table.Name + "'.");
            if (dataType == null && address == null && comment == null && rename == null)
                throw new ArgumentException("Nothing to change: pass --type, --address, --comment or --rename.");

            var changes = new Dictionary<string, object>();
            if (dataType != null && dataType != tag.DataTypeName) changes["type"] = tag.DataTypeName + " -> " + dataType;
            if (address != null && address != tag.LogicalAddress) changes["address"] = tag.LogicalAddress + " -> " + address;
            if (comment != null) changes["comment"] = tag.Comment.Items[0].Text + " -> " + comment;
            if (rename != null && rename != tag.Name) changes["name"] = tag.Name + " -> " + rename;

            var result = new Dictionary<string, object>
            {
                { "table", table.Name }, { "tag", tag.Name },
                { "changes", changes },
                { "action", changes.Count == 0 ? "skip (no change)" : "update" },
                { "applied", apply && changes.Count > 0 },
            };
            if (!apply || changes.Count == 0) return result;

            if (changes.ContainsKey("type")) tag.DataTypeName = dataType;
            if (changes.ContainsKey("address")) tag.LogicalAddress = address;
            if (changes.ContainsKey("comment")) tag.Comment.Items[0].Text = comment;
            if (changes.ContainsKey("name")) tag.Name = rename;
            result["now"] = new Dictionary<string, object>
            {
                { "name", tag.Name }, { "type", tag.DataTypeName }, { "address", tag.LogicalAddress },
            };
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
        public static object Compile(PlcSoftware plc, string blockName, string folderPath,
            bool errorsOnly = false)
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
            var head = new Dictionary<string, object>
            {
                { "scope", scope },
                { "state", result.State.ToString() },
                { "errors", result.ErrorCount },
                { "warnings", result.WarningCount },
            };
            if (!errorsOnly)
            {
                head["messages"] = result.Messages.Select(MessageTree).ToList();
                return head;
            }
            // a árvore tem 15-18 KB de aninhamento; o que se lê é sempre "qual bloco, qual erro, quantas vezes"
            var flat = new List<Dictionary<string, object>>();
            foreach (var m in result.Messages) FlattenErrors(m, null, flat);
            head["list"] = flat.OrderByDescending(e => (int)e["count"]).ToList();
            return head;
        }

        /// <summary>Folhas em estado Error, agrupadas por (ancestral mais fundo com Path, texto).</summary>
        private static void FlattenErrors(CompilerResultMessage m, string owner,
            List<Dictionary<string, object>> acc)
        {
            var where = string.IsNullOrEmpty(m.Path) ? owner : m.Path;
            if (m.Messages.Count > 0)
            {
                foreach (var child in m.Messages) FlattenErrors(child, where, acc);
                return;
            }
            if (m.State != CompilerResultState.Error || string.IsNullOrEmpty(m.Description)) return;
            var hit = acc.FirstOrDefault(e => (string)e["where"] == (owner ?? "")
                && (string)e["message"] == m.Description);
            if (hit == null)
            {
                hit = new Dictionary<string, object>
                {
                    { "where", owner ?? "" }, { "message", m.Description }, { "count", 0 },
                };
                acc.Add(hit);
            }
            hit["count"] = (int)hit["count"] + 1;
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
