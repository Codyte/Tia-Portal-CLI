using System.Collections.Generic;
using System.Linq;
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
