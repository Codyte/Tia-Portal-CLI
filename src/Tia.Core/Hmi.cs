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
