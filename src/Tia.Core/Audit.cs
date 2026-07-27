using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>
    /// Read-only: confere um projeto qualquer contra a lei de nomenclatura de docs/PADRAO.md.
    /// Régua: o projeto de referência tem que passar limpo — se reprovar lá, a regra está errada.
    /// Diferente do doctor (que checa os pré-requisitos dos verbos geradores), o audit olha o
    /// projeto inteiro: acionamento = pasta de blocos que contém um FC PARTIDA_*.
    /// </summary>
    public static class Audit
    {
        /// <summary>TAG entre parênteses no fim do nome: "Soprador 1 (S-01A)" → "S-01A".</summary>
        private static readonly Regex TrailingTag = new Regex(@"\(([^()]+)\)\s*$", RegexOptions.Compiled);

        /// <summary>Segmento numerado de pasta: "3.1.15 Elevatória Agua de Serviço" → 3.1 / 15 / nome.</summary>
        private static readonly Regex AreaSegment =
            new Regex(@"^(\d+(?:\.\d+)*)\.(\d+)\s+(.+)$", RegexOptions.Compiled);

        // Só estes prefixos numeram área, e cada um vive numa árvore: 2.N/3.N em tags,
        // 3.1.N/5.1.N em blocos. Sem isso "3.2 Comunicacao Profinet" (blocos) colidiria com
        // "3.2 <Área>" (tags) e todo projeto conforme acusaria conflito.
        private static readonly string[] TagAreaPrefixes = { "2", "3" };
        private static readonly string[] BlockAreaPrefixes = { "3.1", "5.1" };

        /// <summary>Os 6 blocos que todo acionamento padrão tem (docs/PADRAO.md, seção 4.N).</summary>
        private const int BlocksPerDrive = 6;

        public static string TagOf(string folderOrName)
        {
            var m = TrailingTag.Match(folderOrName ?? "");
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        /// <summary>Bloco do acionamento carrega o TAG — como sufixo `_TAG`, ` TAG` ou `(TAG)`.</summary>
        public static bool CarriesTag(string blockName, string tag)
        {
            return blockName != null && tag != null
                && blockName.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Nome de área comparável entre hierarquias: sem (TAG), sem acento, sem caixa.</summary>
        public static string NormalizeArea(string name)
        {
            string s = TrailingTag.Replace(name ?? "", "").Trim();
            var sb = new StringBuilder();
            foreach (char c in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim().ToLowerInvariant();
        }

        public static object Run(PlcSoftware plc, int maxFindings)
        {
            var blocksByFolder = new Dictionary<string, List<string>>();
            CollectBlocks(plc.BlockGroup, "", blocksByFolder);
            var tablesByFolder = new Dictionary<string, List<string>>();
            CollectTables(plc.TagTableGroup, "", tablesByFolder);

            // acionamento = pasta com um FC PARTIDA_* dentro
            var drives = blocksByFolder
                .Where(kv => kv.Value.Any(n => n.StartsWith("PARTIDA", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var noTag = new List<string>();
            var wrongCount = new List<string>();
            var strayBlock = new List<string>();
            var noTable = new List<string>();

            var tableTags = new HashSet<string>(
                tablesByFolder.SelectMany(kv => kv.Value).Select(TagOf).Where(t => t != null),
                StringComparer.OrdinalIgnoreCase);

            foreach (var drive in drives)
            {
                string leaf = drive.Key.Split('/').Last();
                string tag = TagOf(leaf);
                if (tag == null) { noTag.Add(drive.Key); continue; }
                if (drive.Value.Count != BlocksPerDrive)
                    wrongCount.Add(drive.Key + " → " + drive.Value.Count + " blocos");
                foreach (string name in drive.Value.Where(n => !CarriesTag(n, tag)))
                    strayBlock.Add(drive.Key + " → " + name);
                if (!tableTags.Contains(tag))
                    noTable.Add(drive.Key + " → sem tabela (" + tag + ")");
            }

            var checks = new List<object>
            {
                Check("(TAG) na pasta do acionamento", noTag, maxFindings),
                Check(BlocksPerDrive + " blocos por acionamento", wrongCount, maxFindings),
                Check("blocos carregam o TAG do acionamento", strayBlock, maxFindings),
                Check("1 tabela de tags por acionamento", noTable, maxFindings),
                Check("numeração de área consistente entre hierarquias",
                    AreaConflicts(blocksByFolder.Keys, tablesByFolder.Keys), maxFindings),
            };

            return new Dictionary<string, object>
            {
                { "plc", plc.Name },
                { "drives", drives.Count },
                { "ok", checks.Cast<Dictionary<string, object>>().All(c => (bool)c["ok"]) },
                { "checks", checks },
            };
        }

        /// <summary>Mesmo N tem que ser a mesma área em 2.N/3.N (tags) e 3.1.N/5.1.N (blocos). N=0 = molde.</summary>
        private static List<string> AreaConflicts(IEnumerable<string> blockFolders, IEnumerable<string> tagFolders)
        {
            var byNumber = new Dictionary<int, Dictionary<string, string>>(); // N → nome normalizado → "3.1.15"
            Action<IEnumerable<string>, string[]> scan = (folders, prefixes) =>
            {
                foreach (string folder in folders)
                    foreach (string part in folder.Split('/'))
                    {
                        var m = AreaSegment.Match(part);
                        if (!m.Success || !prefixes.Contains(m.Groups[1].Value)) continue;
                        int n = int.Parse(m.Groups[2].Value);
                        if (n == 0) continue;
                        Dictionary<string, string> seen;
                        if (!byNumber.TryGetValue(n, out seen))
                            byNumber[n] = seen = new Dictionary<string, string>();
                        seen[NormalizeArea(m.Groups[3].Value)] = m.Groups[1].Value + "." + m.Groups[2].Value;
                    }
            };
            scan(tagFolders, TagAreaPrefixes);
            scan(blockFolders, BlockAreaPrefixes);

            return byNumber.Where(kv => kv.Value.Count > 1).OrderBy(kv => kv.Key)
                .Select(kv => "área " + kv.Key + ": " +
                    string.Join(" × ", kv.Value.Select(v => v.Value + " '" + v.Key + "'")))
                .ToList();
        }

        private static object Check(string name, List<string> findings, int max)
        {
            var row = new Dictionary<string, object>
            {
                { "check", name },
                { "ok", findings.Count == 0 },
                { "findings", findings.Count },
            };
            if (findings.Count > 0)
            {
                row["detail"] = findings.Take(max).ToList();
                if (findings.Count > max) row["truncated"] = findings.Count - max;
            }
            return row;
        }

        private static void CollectBlocks(PlcBlockGroup group, string folder, Dictionary<string, List<string>> into)
        {
            var names = group.Blocks.Cast<PlcBlock>().Select(b => b.Name).ToList();
            if (names.Count > 0) into[folder] = names;
            foreach (PlcBlockUserGroup sub in group.Groups)
                CollectBlocks(sub, folder.Length == 0 ? sub.Name : folder + "/" + sub.Name, into);
        }

        private static void CollectTables(PlcTagTableGroup group, string folder, Dictionary<string, List<string>> into)
        {
            var names = group.TagTables.Cast<PlcTagTable>().Select(t => t.Name).ToList();
            if (names.Count > 0) into[folder] = names;
            foreach (PlcTagTableUserGroup sub in group.Groups)
                CollectTables(sub, folder.Length == 0 ? sub.Name : folder + "/" + sub.Name, into);
        }
    }
}
