// NAV INDEX
// 20-40    ScaffoldManifest / ScaffoldItem / ScaffoldPlanItem (config do verbo scaffold)
// 42-58    Scaffold — Rank (ordem de import por tipo de objeto)
// 60-88    Plan — puro, sem TIA: resolve arquivos, lê root/nome do XML, ordena
// 90-145   Run — ativa culturas do XML, cria pastas, importa o que falta (skip se existe)
// 142-176  ResolveBlockPath / ResolveTagPath — caminho por segmentos ('/' é literal em nome de pasta)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>Config for scaffold (deserialized from the user's JSON by the CLI).</summary>
    public class ScaffoldManifest
    {
        /// <summary>Directory holding the exported XMLs; relative paths resolve against the manifest file.</summary>
        public string Source { get; set; }

        /// <summary>Block folders to create, each as path segments (a TIA folder name may contain '/').</summary>
        public List<List<string>> Folders { get; set; }

        /// <summary>Tag table folders to create, same segment form.</summary>
        public List<List<string>> TagFolders { get; set; }

        public List<ScaffoldItem> Items { get; set; }
    }

    public class ScaffoldItem
    {
        public string File { get; set; }

        /// <summary>Target folder as path segments; empty/null = root.</summary>
        public List<string> Folder { get; set; }
    }

    /// <summary>One resolved manifest item (produced without touching TIA).</summary>
    public class ScaffoldPlanItem
    {
        public string File;
        public string Name;
        public string RootType;
        public List<string> Folder;
        public int Rank;
    }

    /// <summary>
    /// Populates an empty project with the house standard: folder tree + molde blocks + tag tables.
    /// Idempotent — an object already in the project is skipped unless force.
    /// </summary>
    public static class Scaffold
    {
        /// <summary>Import order: a block only imports clean after what it references exists.</summary>
        internal static int Rank(string rootType)
        {
            if (rootType == null) return 9;
            if (rootType.StartsWith("SW.Types", StringComparison.Ordinal)) return 0; // UDTs first
            if (rootType == "SW.Tags.PlcTagTable") return 1;
            if (rootType == "SW.Blocks.FB") return 2;
            if (rootType == "SW.Blocks.GlobalDB") return 3;
            if (rootType == "SW.Blocks.InstanceDB") return 4;                        // needs its FB
            if (rootType == "SW.Blocks.FC") return 5;                                // calls FBs/DBs
            if (rootType == "SW.Blocks.OB") return 6;                                // calls the FCs
            return 7;
        }

        /// <summary>Resolves and orders the manifest. Pure: no TIA session, so it runs offline.</summary>
        public static List<ScaffoldPlanItem> Plan(ScaffoldManifest manifest, string baseDir)
        {
            if (manifest == null) throw new ArgumentException("Manifest is empty.");
            var source = manifest.Source ?? "";
            if (!Path.IsPathRooted(source)) source = Path.Combine(baseDir ?? ".", source);

            var items = new List<ScaffoldPlanItem>();
            foreach (var item in manifest.Items ?? new List<ScaffoldItem>())
            {
                if (string.IsNullOrEmpty(item.File)) throw new ArgumentException("Manifest item without 'File'.");
                var full = Path.GetFullPath(Path.Combine(source, item.File));
                if (!File.Exists(full)) throw new FileNotFoundException("Scaffold item not found: " + full);
                var root = Ops.XmlRootType(full);
                items.Add(new ScaffoldPlanItem
                {
                    File = full,
                    Name = Ops.XmlObjectName(full),
                    RootType = root,
                    Folder = item.Folder ?? new List<string>(),
                    Rank = Rank(root),
                });
            }
            return items.OrderBy(i => i.Rank).ToList(); // stable: manifest order kept inside a rank
        }

        public static object Run(TiaSession session, PlcSoftware plc, ScaffoldManifest manifest,
            string baseDir, bool apply, bool force)
        {
            var plan = Plan(manifest, baseDir);
            // projeto novo só tem a cultura de instalação do TIA; sem ativar as do XML, todo import falha
            var languages = Ops.EnsureCultures(session.Project,
                plan.SelectMany(i => Ops.XmlCultures(i.File)), apply);
            var folders = new List<object>();
            foreach (var segments in manifest.Folders ?? new List<List<string>>())
                folders.Add(FolderAction(plc, segments, false, apply));
            foreach (var segments in manifest.TagFolders ?? new List<List<string>>())
                folders.Add(FolderAction(plc, segments, true, apply));

            var items = new List<object>();
            foreach (var item in plan)
            {
                bool isTag = item.RootType == "SW.Tags.PlcTagTable";
                bool isType = item.RootType != null && item.RootType.StartsWith("SW.Types", StringComparison.Ordinal);
                bool exists = item.Name != null && (isTag
                    ? Ops.FindTagTable(plc.TagTableGroup, item.Name) != null
                    : isType
                        ? Ops.FindType(plc.TypeGroup, item.Name) != null
                        : Ops.FindBlock(plc, item.Name) != null);
                bool write = !exists || force;

                if (apply && write)
                {
                    var file = new FileInfo(item.File);
                    if (isTag) ResolveTagPath(plc, item.Folder, true).TagTables.Import(file, ImportOptions.Override);
                    else if (isType) plc.TypeGroup.Types.Import(file, ImportOptions.Override);
                    else ResolveBlockPath(plc, item.Folder, true).Blocks.Import(file, ImportOptions.Override);
                }
                items.Add(new Dictionary<string, object>
                {
                    { "name", item.Name },
                    { "kind", item.RootType },
                    { "folder", string.Join(" > ", item.Folder) },
                    { "action", exists ? (force ? "override" : "skip (exists)") : "create" },
                });
            }
            return new Dictionary<string, object>
            {
                { "languagesActivated", languages },
                { "folders", folders },
                { "items", items },
                { "created", items.Count(i => (string)((Dictionary<string, object>)i)["action"] == "create") },
                { "applied", apply },
            };
        }

        private static object FolderAction(PlcSoftware plc, List<string> segments, bool tags, bool apply)
        {
            bool exists;
            try
            {
                if (tags) ResolveTagPath(plc, segments, false); else ResolveBlockPath(plc, segments, false);
                exists = true;
            }
            catch (InvalidOperationException) { exists = false; }
            if (apply && !exists)
            {
                if (tags) ResolveTagPath(plc, segments, true); else ResolveBlockPath(plc, segments, true);
            }
            return new Dictionary<string, object>
            {
                { "path", string.Join(" > ", segments ?? new List<string>()) },
                { "kind", tags ? "tag-folder" : "block-folder" },
                { "action", exists ? "none (exists)" : "create" },
            };
        }

        // Ops.ResolveFolder splits on '/', which real folder names contain ("3. Alarmes/Eventos/Falhas").
        // The manifest gives segments, so nothing is split here.
        private static PlcBlockGroup ResolveBlockPath(PlcSoftware plc, IList<string> segments, bool create)
        {
            PlcBlockGroup current = plc.BlockGroup;
            foreach (var name in segments ?? new List<string>())
            {
                var next = current.Groups.Find(name);
                if (next == null)
                {
                    if (!create) throw new InvalidOperationException("Block folder not found: '" + name + "'.");
                    next = current.Groups.Create(name);
                }
                current = next;
            }
            return current;
        }

        private static PlcTagTableGroup ResolveTagPath(PlcSoftware plc, IList<string> segments, bool create)
        {
            PlcTagTableGroup current = plc.TagTableGroup;
            foreach (var name in segments ?? new List<string>())
            {
                var next = current.Groups.Find(name);
                if (next == null)
                {
                    if (!create) throw new InvalidOperationException("Tag folder not found: '" + name + "'.");
                    next = current.Groups.Create(name);
                }
                current = next;
            }
            return current;
        }
    }
}
