// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L49    class Audit
//   L75    .HasInverter
//   L82    .MissingCore
//   L90    .TagOf
//   L97    .CarriesTag
//   L104   .NormalizeArea
//   L120   .IsCallBlock
//   L130   .IsLooseScalar
//   L141   .RootMembers
//   L152   .Run
//   L257   .NonGraphicCalls
//   L277   .MisplacedCalls
//   L303   .DbGlobalCheck
//   L323   .FindGlobalDb
//   L337   .Skipped
//   L346   .CountTypes
//   L351   .CollectLanguages
//   L364   .LayerLeaks
//   L390   .IsLibrary
//   L397   .AreaConflicts
//   L424   .Check
//   L440   .CollectBlocks
//   L448   .CollectTables
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Siemens.Engineering.CrossReference;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;

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

        /// <summary>Os 6 blocos que um acionamento COM INVERSOR tem (docs/PADRAO.md, seção 4.N).</summary>
        private const int BlocksPerDrive = 6;

        /// <summary>
        /// Partida direta não tem telegrama nem referência de velocidade, então não tem os 6 blocos:
        /// o que ela tem que ter é o trio (FC PARTIDA_*, FB FALHA_TAG, FB CONDIÇÃO DE PARTIDA_TAG).
        /// Exigir 6 de todo mundo reprovava todo acionamento por contator (FP-03, tropeço 9).
        /// </summary>
        private static readonly string[] InverterMarks = { "sina_", "setpoint" };
        private static readonly string[] CoreBlockMarks = { "falha", "condicao de partida" };

        internal static bool HasInverter(IEnumerable<string> blockNames)
        {
            return blockNames.Any(n => InverterMarks.Any(m =>
                (n ?? "").IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        /// <summary>Marcas do trio que faltam na pasta (nome normalizado: acento e caixa não contam).</summary>
        internal static List<string> MissingCore(IEnumerable<string> blockNames)
        {
            var normalized = blockNames.Select(NormalizeArea).ToList();
            return CoreBlockMarks
                .Where(mark => !normalized.Any(n => n.IndexOf(mark, StringComparison.Ordinal) >= 0))
                .ToList();
        }

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

        /// <summary>Blocos de chamada (R8): têm que ser gráficos, porque é o que os geradores reescrevem.</summary>
        private static readonly string[] CallBlockMarks = { "chamada", "partida", "molde", "ob_molde" };

        /// <summary>Linguagens gráficas — só elas têm FlgNet, que é o que replicate/gen-* enxergam.</summary>
        private static readonly string[] GraphicLanguages = { "LAD", "FBD" };

        internal static bool IsCallBlock(string name)
        {
            string n = NormalizeArea(name);
            return n == "main" || CallBlockMarks.Any(m => n.StartsWith(m, StringComparison.Ordinal));
        }

        /// <summary>
        /// Membro solto na raiz da DB global (R2). UDT e Struct agrupam; escalar solto é o UDT que
        /// não foi criado. Referência a UDT vem entre aspas no SimaticML ("MotorDados").
        /// </summary>
        internal static bool IsLooseScalar(string datatype)
        {
            string t = (datatype ?? "").Trim();
            if (t.Length == 0) return false;
            if (t.StartsWith("\"", StringComparison.Ordinal)) return false;          // UDT
            if (t.StartsWith("Struct", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.StartsWith("Array", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        /// <summary>Membros da raiz (Section "Static") de uma DB exportada: nome → datatype.</summary>
        internal static List<KeyValuePair<string, string>> RootMembers(XDocument doc)
        {
            var section = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Section"
                && (string)e.Attribute("Name") == "Static");
            if (section == null) return new List<KeyValuePair<string, string>>();
            return section.Elements().Where(e => e.Name.LocalName == "Member")
                .Select(m => new KeyValuePair<string, string>(
                    (string)m.Attribute("Name"), (string)m.Attribute("Datatype")))
                .ToList();
        }

        public static object Run(PlcSoftware plc, int maxFindings, string outDir = null, string dbName = null)
        {
            var blocksByFolder = new Dictionary<string, List<string>>();
            CollectBlocks(plc.BlockGroup, "", blocksByFolder);
            var languageOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CollectLanguages(plc.BlockGroup, languageOf);
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
                if (HasInverter(drive.Value))
                {
                    if (drive.Value.Count != BlocksPerDrive)
                        wrongCount.Add(drive.Key + " → " + drive.Value.Count + " blocos (com inversor: "
                            + BlocksPerDrive + ")");
                }
                else
                {
                    var missing = MissingCore(drive.Value);
                    if (missing.Count > 0)
                        wrongCount.Add(drive.Key + " → partida direta sem " + string.Join(" nem ", missing));
                }
                foreach (string name in drive.Value.Where(n => !CarriesTag(n, tag)))
                    strayBlock.Add(drive.Key + " → " + name);
                if (!tableTags.Contains(tag))
                    noTable.Add(drive.Key + " → sem tabela (" + tag + ")");
            }

            int udts = CountTypes(plc.TypeGroup);
            var noUdt = udts > 0 ? new List<string>()
                : new List<string> { "0 UDTs no PLC — todo agrupamento de dados usado por mais de "
                    + "um bloco tem que ser UDT (R1); Struct anônima dentro de DB é o antipadrão do STEP 7 V5" };

            var checks = new List<object>
            {
                Check("(TAG) na pasta do acionamento", noTag, maxFindings),
                Check("blocos por acionamento (" + BlocksPerDrive + " com inversor, trio na partida direta)",
                    wrongCount, maxFindings),
                Check("blocos carregam o TAG do acionamento", strayBlock, maxFindings),
                Check("1 tabela de tags por acionamento", noTable, maxFindings),
                Check("numeração de área consistente entre hierarquias",
                    AreaConflicts(blocksByFolder.Keys, tablesByFolder.Keys), maxFindings),
                Check("biblioteca não depende de bloco de área",
                    LayerLeaks(plc, blocksByFolder), maxFindings),
                Check("R1 · o PLC tem UDT", noUdt, maxFindings),
                DbGlobalCheck(plc, dbName, outDir, maxFindings),
                Check("R8 · bloco de chamada em linguagem gráfica",
                    NonGraphicCalls(blocksByFolder, languageOf), maxFindings),
                Check("CHAMADA_* fora da pasta de área", MisplacedCalls(blocksByFolder), maxFindings),
            };

            // Check que nunca acusa nada é indistinguível de check que não olhou — foi o modo de
            // falha do `--folder` do list-blocks (`count: 0`, `ok: true`). `scanned` diz o tamanho
            // da população de cada um: `callBlocks: 0` reprova o R8 por vazio, não por conformidade.
            var allBlocks = blocksByFolder.SelectMany(kv => kv.Value).ToList();
            var scanned = new Dictionary<string, object>
            {
                { "folders", blocksByFolder.Count },
                { "blocks", allBlocks.Count },
                { "callBlocks", allBlocks.Count(n => IsCallBlock(n) && languageOf.ContainsKey(n)) },
                { "tagTables", tablesByFolder.Sum(kv => kv.Value.Count) },
            };

            return new Dictionary<string, object>
            {
                { "plc", plc.Name },
                { "drives", drives.Count },
                { "udts", udts },
                { "scanned", scanned },
                { "ok", checks.Cast<Dictionary<string, object>>().All(c => (bool)c["ok"]) },
                // SAFE-11: check que não pôde rodar continua ok:true (não reprova o projeto), mas
                // `ok:true` sozinho passava a impressão de conformidade provada. `complete:false` +
                // a lista do que pulou é a diferença entre check conforme e check cego.
                { "complete", checks.Cast<Dictionary<string, object>>().All(c => !c.ContainsKey("skipped")) },
                { "skippedChecks", checks.Cast<Dictionary<string, object>>()
                    .Where(c => c.ContainsKey("skipped"))
                    .Select(c => (string)c["check"]).ToList() },
                { "checks", checks },
            };
        }

        /// <summary>
        /// R8: chamada (Main, CHAMADA_*, PARTIDA_*, MOLDE_*) tem que ser LAD/FBD. O argumento não é
        /// de gosto — replicate-fc/gen-alarm-fc/gen-fault-ob reescrevem FlgNet, então um bloco de
        /// chamada em SCL está fora do alcance da ferramenta que gerou o resto do programa.
        /// DB (iDB chamado de PARTIDA_X) não tem linguagem de código: fica de fora.
        /// </summary>
        private static List<string> NonGraphicCalls(Dictionary<string, List<string>> blocksByFolder,
            Dictionary<string, string> languageOf)
        {
            var findings = new List<string>();
            foreach (var kv in blocksByFolder)
                foreach (string name in kv.Value)
                {
                    string lang;
                    if (!IsCallBlock(name) || !languageOf.TryGetValue(name, out lang)) continue;
                    if (!GraphicLanguages.Contains(lang, StringComparer.OrdinalIgnoreCase))
                        findings.Add((kv.Key.Length == 0 ? "(raiz)" : kv.Key) + " → " + name + " (" + lang + ")");
                }
            return findings;
        }

        /// <summary>
        /// CHAMADA_* mora junto do molde, um nível acima da pasta de área — a pasta de área só tem o
        /// FC da área e as DBs. Sinal: o CHAMADA divide pasta com o trabalho que ele chama
        /// (FC_ALARMES_*/ALARMES_* ou PARTIDA_*). Divergência 3 do BOAS-PRATICAS §F.
        /// </summary>
        private static List<string> MisplacedCalls(Dictionary<string, List<string>> blocksByFolder)
        {
            var findings = new List<string>();
            foreach (var kv in blocksByFolder)
            {
                var calls = kv.Value.Where(n => NormalizeArea(n).StartsWith("chamada", StringComparison.Ordinal)).ToList();
                if (calls.Count == 0) continue;
                bool areaWork = kv.Value.Any(n =>
                {
                    string x = NormalizeArea(n);
                    return x.StartsWith("fc_alarmes", StringComparison.Ordinal)
                        || x.StartsWith("alarmes_", StringComparison.Ordinal)
                        || x.StartsWith("partida", StringComparison.Ordinal);
                });
                if (!areaWork) continue;
                foreach (string call in calls)
                    findings.Add(kv.Key + " → " + call + " (divide pasta com o bloco de área que chama)");
            }
            return findings;
        }

        /// <summary>
        /// R2: DB global é agregado de UDTs. Escalar solto na raiz = UDT que não foi criado. Só o
        /// export mostra o datatype dos membros, então o check pula (não reprova) quando não há DB
        /// global identificável, quando ela está inconsistente, ou quando o audit rodou sem --out.
        /// </summary>
        private static object DbGlobalCheck(PlcSoftware plc, string dbName, string outDir, int maxFindings)
        {
            const string name = "R2 · DB global sem escalar solto na raiz";
            string target = dbName ?? FindGlobalDb(plc.BlockGroup);
            if (target == null) return Skipped(name, "nenhuma DB global com 'global' no nome; passe --db NOME");
            if (string.IsNullOrEmpty(outDir)) return Skipped(name, "precisa de --out DIR para exportar '" + target + "'");
            try
            {
                var exported = (Dictionary<string, object>)Ops.ExportBlock(plc, target, outDir);
                var doc = XDocument.Load((string)exported["file"]);
                var loose = RootMembers(doc).Where(m => IsLooseScalar(m.Value))
                    .Select(m => target + "." + m.Key + " : " + m.Value).ToList();
                var row = (Dictionary<string, object>)Check(name, loose, maxFindings);
                row["db"] = target;
                return row;
            }
            catch (Exception ex) { return Skipped(name, ex.Message); }
        }

        /// <summary>DB global = GlobalDB com 'global' no nome. Convenção do molde ("DB GLOBAL").</summary>
        private static string FindGlobalDb(PlcBlockGroup group)
        {
            foreach (PlcBlock b in group.Blocks)
                if (b is GlobalDB && NormalizeArea(b.Name).IndexOf("global", StringComparison.Ordinal) >= 0)
                    return b.Name;
            foreach (PlcBlockUserGroup sub in group.Groups)
            {
                string hit = FindGlobalDb(sub);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>Check que não pôde rodar: ok=true (não reprova o projeto) + o porquê.</summary>
        internal static object Skipped(string name, string why)
        {
            return new Dictionary<string, object>
            {
                { "check", name }, { "ok", true }, { "findings", 0 },
                { "skipped", why },
            };
        }

        private static int CountTypes(PlcTypeGroup group)
        {
            return group.Types.Count + group.Groups.Cast<PlcTypeUserGroup>().Sum(g => CountTypes(g));
        }

        private static void CollectLanguages(PlcBlockGroup group, Dictionary<string, string> into)
        {
            foreach (PlcBlock b in group.Blocks)
                if (!(b is DataBlock)) into[b.Name] = b.ProgrammingLanguage.ToString();
            foreach (PlcBlockUserGroup sub in group.Groups) CollectLanguages(sub, into);
        }

        /// <summary>
        /// Camada 1 ("1. FB Bibliotecas") é biblioteca: seus blocos podem chamar uns aos outros, mas
        /// não um bloco de área — se chamarem, a biblioteca deixa de ser instalável sozinha (é
        /// exatamente o que o `install-lib` sofre). Xref é o único jeito de ver a chamada; nome de
        /// bloco é único no PLC, então o mapa nome → pasta resolve a camada do chamado.
        /// </summary>
        private static List<string> LayerLeaks(PlcSoftware plc, Dictionary<string, List<string>> blocksByFolder)
        {
            var layerOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in blocksByFolder)
                foreach (string name in kv.Value)
                    layerOf[name] = kv.Key.Split('/')[0];

            var leaks = new List<string>();
            int scanned;
            foreach (var src in Inventory.AllSources(plc, out scanned))
            {
                string from;
                if (!layerOf.TryGetValue(src.Name ?? "", out from) || !IsLibrary(from)) continue;
                foreach (ReferenceObject r in src.References)
                {
                    string to;
                    if (r.Name == null || !layerOf.TryGetValue(r.Name, out to) || IsLibrary(to)) continue;
                    // o xref traz os dois sentidos no mesmo saco; só "Uses" é chamada de src para r
                    // (o resto é "UsedBy": quem chama a biblioteca, que é justamente o certo)
                    if (!r.Locations.Any(l => l.ReferenceType.ToString() == "Uses")) continue;
                    leaks.Add(src.Name + " → " + r.Name + " (" + to + ")");
                }
            }
            return leaks;
        }

        private static bool IsLibrary(string layer)
        {
            return layer.StartsWith("1.", StringComparison.Ordinal)
                || layer.StartsWith("1 ", StringComparison.Ordinal);
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

        internal static object Check(string name, List<string> findings, int max)
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
