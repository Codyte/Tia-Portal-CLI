// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L48    class Hmi
//   L51    .Targets
//   L67    .ExportTagTable
//   L86    .ResolveTagFolder
//   L101   .List
//   L115   .Describe
//   L156   .Tree
//   L208   .SplitPath
//   L218   .Row
//   L225   roundtrip SimaticML de tela (só clássico)
//   L228   .ExportScreen
//   L245   .ImportScreen
//   L291   .StripScreenNumber
//   L308   .DeleteScreen
//   L321   .FindScreen
//   L336   .ClassicTarget
//   L355   .ResolveScreenFolder
//   L377   .CollectScreens
//   L386   .CollectTables
// ======================= END NAV INDEX =======================

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Classic = Siemens.Engineering.Hmi;
using Unified = Siemens.Engineering.HmiUnified;

namespace Tia.Core
{
    /// <summary>
    /// Inventário read-only das duas famílias de HMI, que são APIs distintas e não intercambiáveis:
    ///
    ///   classic — WinCC Comfort/Advanced/Professional, assembly Siemens.Engineering.WinCC.
    ///             `HmiTarget`, com árvore de pastas (ScreenSystemFolder/ScreenUserFolder). Tela tem
    ///             roundtrip SimaticML: Screen.Export + ScreenComposition.Import.
    ///   unified — WinCC Unified, assembly Siemens.Engineering.WinCCUnified.
    ///             `HmiSoftware`, lista plana de telas. Tela NÃO exporta SimaticML; é modelo de
    ///             objetos tipado (HmiScreenBase/HmiScreenItemBase). Só tag e script exportam.
    ///
    /// Por isso `api` sai em toda linha do JSON: é ela que decide qual caminho de escrita existe.
    /// Ver docs/LIMITES.md, seção HMI.
    /// </summary>
    public static class Hmi
    {
        /// <summary>Todo software de HMI do projeto, das duas famílias, com o device que o carrega.</summary>
        public static IEnumerable<KeyValuePair<string, object>> Targets(TiaSession session)
        {
            foreach (Device device in session.AllDevices())
                foreach (DeviceItem item in device.DeviceItems)
                {
                    var software = item.GetService<SoftwareContainer>()?.Software;
                    if (software is Unified.HmiSoftware || software is Classic.HmiTarget)
                        yield return new KeyValuePair<string, object>(device.Name, software);
                }
        }

        /// <summary>
        /// Export SimaticML de uma tabela de tags de HMI. É por aqui que se vê o vínculo com o PLC —
        /// `GetAttributeInfos` numa tag de HMI devolve só "Name" (medido 2026-08-17), então dump de
        /// atributo não responde de onde a tag lê. Exige `--table`: são 4354 tags no projeto real.
        /// </summary>
        public static object ExportTagTable(TiaSession session, string deviceName, string tablePath,
            string outDir)
        {
            var target = ClassicTarget(session, deviceName);
            var cut = tablePath.LastIndexOf('/');
            var tables = ResolveTagFolder(target.Value, cut < 0 ? null : tablePath.Substring(0, cut));
            var table = tables == null ? null : tables.Find(tablePath.Substring(cut + 1));
            if (table == null)
                throw new System.InvalidOperationException("Tag table '" + tablePath + "' not found in '"
                    + target.Value.Name + "'.");
            var file = Ops.ExportPath(outDir, tablePath);
            table.Export(new FileInfo(file), Siemens.Engineering.ExportOptions.WithDefaults);
            return new Dictionary<string, object>
            {
                { "device", target.Key }, { "hmi", target.Value.Name },
                { "table", tablePath }, { "file", file },
            };
        }

        static Classic.Tag.TagTableComposition ResolveTagFolder(Classic.HmiTarget target, string path)
        {
            var tables = target.TagFolder.TagTables;
            var folders = target.TagFolder.Folders;
            if (string.IsNullOrEmpty(path)) return tables;
            foreach (var part in path.Trim('/').Split('/'))
            {
                var next = folders.Find(part);
                if (next == null) return null;
                tables = next.TagTables;
                folders = next.Folders;
            }
            return tables;
        }

        public static object List(TiaSession session, string deviceName)
        {
            var targets = Targets(session)
                .Where(t => deviceName == null ||
                    t.Key.Equals(deviceName, System.StringComparison.OrdinalIgnoreCase))
                .Select(t => Describe(t.Key, t.Value))
                .ToList();
            if (targets.Count == 0)
                throw new System.InvalidOperationException(deviceName == null
                    ? "No HMI target in the project (neither WinCC classic nor WinCC Unified)."
                    : "HMI device '" + deviceName + "' not found.");
            return targets;
        }

        static Dictionary<string, object> Describe(string device, object software)
        {
            var unified = software as Unified.HmiSoftware;
            if (unified != null)
                return new Dictionary<string, object>
                {
                    { "device", device },
                    { "hmi", unified.Name },
                    { "api", "unified" },
                    { "screens", unified.Screens.Select(s => s.Name).ToList() },
                    { "tagTables", unified.TagTables.Select(tt => new Dictionary<string, object>
                        { { "table", tt.Name }, { "tagCount", tt.Tags.Count } }).ToList() },
                };

            var classic = (Classic.HmiTarget)software;
            var screens = new List<string>();
            CollectScreens(classic.ScreenFolder.Screens, classic.ScreenFolder.Folders, "", screens);
            var tables = new List<Dictionary<string, object>>();
            CollectTables(classic.TagFolder.TagTables, classic.TagFolder.Folders, "", tables);
            return new Dictionary<string, object>
            {
                { "device", device },
                { "hmi", classic.Name },
                { "api", "classic" },
                { "screens", screens },
                { "tagTables", tables },
                // as demais raízes de tela do target; contagem só, o detalhe fica pro hmi-tree
                { "templates", classic.ScreenTemplateFolder.ScreenTemplates.Count },
                { "popups", classic.ScreenPopupFolder.ScreenPopups.Count },
                { "slideins", classic.ScreenSlideinFolder.ScreenSlideins.Count },
                { "connections", classic.Connections.Select(c => c.Name).ToList() },
            };
        }

        /// <summary>
        /// Irmão do `tia tree`: outline de todas as IHMs em `hmi-navi.md`, agrupado por pasta.
        /// A pasta é a unidade útil porque a árvore da IHM espelha a do PLC no projeto real
        /// ("3. Partidas/3.1 Preliminar (P-GM-01)"), então o mapa já sai por área — que é como
        /// `trace --equipment` faz a pergunta. Mesmo motivo do plc-navi.md: cabeçalho de pasta
        /// uma vez em vez de chave repetida por item.
        /// </summary>
        public static object Tree(TiaSession session, string outFile)
        {
            var targets = Targets(session).Select(t => Describe(t.Key, t.Value)).ToList();
            if (targets.Count == 0)
                throw new System.InvalidOperationException("No HMI target in the project.");

            var body = new StringBuilder();
            int screens = 0, tables = 0, tags = 0;
            foreach (var hmi in targets)
            {
                var screenItems = ((List<string>)hmi["screens"]).Select(SplitPath).ToList();
                var tableItems = ((List<Dictionary<string, object>>)hmi["tagTables"])
                    .Select(t => Row((string)t["table"], t["tagCount"])).ToList();
                var deviceTags = ((List<Dictionary<string, object>>)hmi["tagTables"])
                    .Sum(t => System.Convert.ToInt32(t["tagCount"]));
                screens += screenItems.Count;
                tables += tableItems.Count;
                tags += deviceTags;
                // O device é cabeçalho `#` uma vez, não prefixo repetido em cada `##`: com 96 pastas
                // o prefixo custava ~3 KB de um arquivo de 12 KB, e o mapa só se justifica se for
                // sensivelmente menor que o JSON que ele substitui.
                body.AppendLine("# " + hmi["device"] + " [" + hmi["api"] + "] — "
                    + screenItems.Count + " screens · " + tableItems.Count + " tag tables · "
                    + deviceTags + " tags");
                body.AppendLine();
                Inventory.AppendGrouped(body, "screens", screenItems, d => (string)d["name"]);
                Inventory.AppendGrouped(body, "tag tables", tableItems,
                    d => d["name"] + "(" + d["tagCount"] + ")");
            }

            var sb = new StringBuilder();
            sb.AppendLine("# __navi__ · HMI — " + targets.Count + " devices · " + screens
                + " screens · " + tables + " tag tables · " + tags + " tags");
            sb.AppendLine("<!-- generated by `tia hmi-tree` · "
                + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " -->");
            sb.AppendLine();
            sb.Append(body);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile)));
            File.WriteAllText(outFile, sb.ToString());

            return new Dictionary<string, object>
            {
                { "devices", targets.Select(t => t["device"]).ToList() },
                { "screens", screens },
                { "tagTables", tables },
                { "tags", tags },
                { "file", Path.GetFullPath(outFile) },
            };
        }

        // AppendGrouped agrupa pela chave "folder" e rotula pela "name" — parte o caminho completo
        // "Pasta/Sub/Tela" nessas duas metades.
        static Dictionary<string, object> SplitPath(string path)
        {
            var cut = path.LastIndexOf('/');
            return new Dictionary<string, object>
            {
                { "folder", cut < 0 ? "" : path.Substring(0, cut) },
                { "name", cut < 0 ? path : path.Substring(cut + 1) },
            };
        }

        static Dictionary<string, object> Row(string path, object tagCount)
        {
            var row = SplitPath(path);
            row["tagCount"] = tagCount;
            return row;
        }

        // ---------- roundtrip SimaticML de tela (só clássico) ----------

        /// <summary>Export de uma tela: `Pasta/Sub/Tela`, o mesmo caminho que `list-hmi`/`hmi-tree` imprimem.</summary>
        public static object ExportScreen(TiaSession session, string device, string screenPath, string outDir)
        {
            var target = ClassicTarget(session, device);
            var screen = FindScreen(target, screenPath);
            var file = Ops.ExportPath(outDir, screenPath);
            screen.Export(new FileInfo(file), Siemens.Engineering.ExportOptions.WithDefaults);
            return new Dictionary<string, object>
            {
                { "device", target.Key }, { "hmi", target.Value.Name },
                { "screen", screenPath }, { "file", file },
            };
        }

        /// <summary>
        /// Import de tela. Mesmo contrato do `import-block`: dry por padrão, `--folder` é caminho
        /// completo a partir da raiz de telas e o dry-run diz em `folderAction` se vai criar ou reusar.
        /// </summary>
        public static object ImportScreen(TiaSession session, string device, string file,
            string folderPath, bool apply)
        {
            var target = ClassicTarget(session, device);
            var full = Path.GetFullPath(file);
            if (!File.Exists(full)) throw new FileNotFoundException("Import file not found: " + full);
            Ops.RequireRootType(full, "Hmi.Screen.");
            var name = Ops.XmlObjectName(full);
            var screens = ResolveScreenFolder(target.Value, folderPath, false);
            var result = new Dictionary<string, object>
            {
                { "file", full },
                { "device", target.Key },
                { "hmi", target.Value.Name },
                { "screen", name },
                { "folder", folderPath ?? "" },
                { "action", screens != null && name != null && screens.Find(name) != null
                    ? "override" : "create" },
                { "applied", apply },
            };
            if (!string.IsNullOrEmpty(folderPath))
                result["folderAction"] = screens == null ? "create" : "reuse";
            // Tela nova com o <Number> do molde colide: número de tela é único por DEVICE, e a
            // colisão não é erro recuperável — o Import estoura NonRecoverableException e derruba
            // o Portal inteiro (medido 2026-08-17). Prevenir tirando o número: o Portal atribui.
            var importFile = full;
            if ((string)result["action"] == "create")
            {
                var stripped = StripScreenNumber(full);
                result["numberStripped"] = stripped != null;
                if (stripped != null) { importFile = stripped; result["file"] = stripped; }
            }
            if (apply)
            {
                result["languagesActivated"] =
                    Ops.EnsureCultures(session.Project, Ops.XmlCultures(importFile), true);
                ResolveScreenFolder(target.Value, folderPath, true)
                    .Import(new FileInfo(importFile), Siemens.Engineering.ImportOptions.Override);
            }
            return result;
        }

        /// <summary>
        /// Tira o &lt;Number&gt; da tela e grava a cópia ao lado (`.renum.xml`); devolve null se não houver.
        /// Puro — sem Openness, testável offline.
        /// </summary>
        internal static string StripScreenNumber(string file)
        {
            var doc = System.Xml.Linq.XDocument.Load(file);
            var number = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Number"
                && e.Parent != null && e.Parent.Name.LocalName == "AttributeList"
                && e.Parent.Parent != null && e.Parent.Parent.Name.LocalName.StartsWith("Hmi.Screen.Screen"));
            if (number == null) return null;
            number.Remove();
            var target = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file)),
                Path.GetFileNameWithoutExtension(file) + ".renum.xml");
            doc.Save(target);
            return target;
        }

        /// <summary>
        /// Delete de tela — o par que faltava do `import-screen`: sem ele, tela de smoke só sai pela GUI.
        /// </summary>
        public static object DeleteScreen(TiaSession session, string device, string screenPath, bool apply)
        {
            var target = ClassicTarget(session, device);
            var screen = FindScreen(target, screenPath);
            if (apply) screen.Delete();
            return new Dictionary<string, object>
            {
                { "device", target.Key }, { "hmi", target.Value.Name },
                { "screen", screenPath }, { "action", "delete" }, { "applied", apply },
            };
        }

        /// <summary>Resolve `Pasta/Sub/Tela` na tela; ausente é erro, como no export-block.</summary>
        static Classic.Screen.Screen FindScreen(KeyValuePair<string, Classic.HmiTarget> target, string screenPath)
        {
            var cut = screenPath.LastIndexOf('/');
            var screens = ResolveScreenFolder(target.Value, cut < 0 ? null : screenPath.Substring(0, cut), false);
            var screen = screens == null ? null : screens.Find(screenPath.Substring(cut + 1));
            if (screen == null)
                throw new System.InvalidOperationException("Screen '" + screenPath + "' not found in '"
                    + target.Value.Name + "'.");
            return screen;
        }

        /// <summary>
        /// A IHM do verbo de escrita: uma só, e clássica. Unified não tem roundtrip SimaticML de tela
        /// (docs/LIMITES.md), então recusar cedo é melhor que falhar dentro do Import.
        /// </summary>
        static KeyValuePair<string, Classic.HmiTarget> ClassicTarget(TiaSession session, string device)
        {
            var hits = Targets(session)
                .Where(t => device == null || t.Key.Equals(device, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (hits.Count == 0)
                throw new System.InvalidOperationException(device == null
                    ? "No HMI target in the project." : "HMI device '" + device + "' not found.");
            if (hits.Count > 1)
                throw new System.InvalidOperationException("--device required, candidates: "
                    + string.Join(", ", hits.Select(t => t.Key)));
            var classic = hits[0].Value as Classic.HmiTarget;
            if (classic == null)
                throw new System.InvalidOperationException("'" + hits[0].Key + "' is WinCC Unified: "
                    + "screen has no SimaticML export/import there. Ver docs/LIMITES.md, seção HMI.");
            return new KeyValuePair<string, Classic.HmiTarget>(hits[0].Key, classic);
        }

        /// <summary>Caminho de pasta de tela → a composition onde a tela vive. `create:false` devolve null se faltar.</summary>
        static Classic.Screen.ScreenComposition ResolveScreenFolder(Classic.HmiTarget target,
            string path, bool create)
        {
            var screens = target.ScreenFolder.Screens;
            var folders = target.ScreenFolder.Folders;
            if (string.IsNullOrEmpty(path)) return screens;
            foreach (var part in path.Trim('/').Split('/'))
            {
                var next = folders.Find(part);
                if (next == null)
                {
                    if (!create) return null;
                    next = folders.Create(part);
                }
                screens = next.Screens;
                folders = next.Folders;
            }
            return screens;
        }

        // A árvore do WinCC clássico é sistema-folder + pastas de usuário recursivas; o caminho
        // "Pasta/Sub/Tela" é o que identifica a tela para export/import, não o nome solto.
        static void CollectScreens(Classic.Screen.ScreenComposition screens,
            Classic.Screen.ScreenUserFolderComposition folders, string prefix, List<string> into)
        {
            foreach (var screen in screens)
                into.Add(prefix + screen.Name);
            foreach (var folder in folders)
                CollectScreens(folder.Screens, folder.Folders, prefix + folder.Name + "/", into);
        }

        static void CollectTables(Classic.Tag.TagTableComposition tables,
            Classic.Tag.TagUserFolderComposition folders, string prefix,
            List<Dictionary<string, object>> into)
        {
            foreach (var table in tables)
                into.Add(new Dictionary<string, object>
                    { { "table", prefix + table.Name }, { "tagCount", table.Tags.Count } });
            foreach (var folder in folders)
                CollectTables(folder.TagTables, folder.Folders, prefix + folder.Name + "/", into);
        }
    }
}
