// NAV INDEX
//   1-22    usings, namespace
//   24-95   Clone.Run — localiza bloco/tabela, exporta, reescreve, importa
//   97-135  Rewrite — substituição textual no XML (núcleo puro, testável offline)
//  137-175  Readdress — reendereça tags Bool a partir de %M<byte>.<bit>
//  177-200  helpers: ParseReplaces, ObjectName
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;

namespace Tia.Core
{
    /// <summary>
    /// clone: duplica um bloco ou tabela de tags trocando um identificador
    /// (ex.: BH-01B=BH-01C) em todo o XML — nome, símbolos, caminhos do DB global,
    /// instance DBs. Com --at, as tags Bool ganham endereços %M novos e sequenciais.
    /// </summary>
    public static class Clone
    {
        public static object Run(PlcSoftware plc, string blockName, string tableName,
            IEnumerable<string> replaceArgs, string at, string folder, string outDir, bool apply)
        {
            if (string.IsNullOrEmpty(blockName) == string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Pass exactly one of --block <name> / --table <name>.");
            var replaces = ParseReplaces(replaceArgs);
            if (!replaces.Any())
                throw new ArgumentException("Pass at least one --replace OLD=NEW.");
            if (at != null && blockName != null)
                throw new ArgumentException("--at only applies to --table (tag addresses).");

            bool isTable = !string.IsNullOrEmpty(tableName);
            string sourceName = isTable ? tableName : blockName;

            Directory.CreateDirectory(outDir);
            object exported = isTable
                ? Ops.ExportTagTable(plc, sourceName, outDir)
                : Ops.ExportBlock(plc, sourceName, outDir);
            var sourceFile = (string)((Dictionary<string, object>)exported)["file"];

            var doc = XDocument.Load(sourceFile);
            int hits = Rewrite(doc, replaces);
            var addresses = at != null ? Readdress(doc, at) : new List<string>();

            string newName = ObjectName(doc);
            if (string.Equals(newName, sourceName, StringComparison.Ordinal))
                throw new InvalidOperationException("Clone name is identical to the source ('" + sourceName +
                    "') — the --replace pairs do not touch its name. Refusing to overwrite the source.");

            var targetFile = Path.GetFullPath(Path.Combine(outDir,
                string.Join("_", newName.Split(Path.GetInvalidFileNameChars())) + ".xml"));
            if (File.Exists(targetFile)) File.Delete(targetFile);
            doc.Save(targetFile);

            var result = new Dictionary<string, object>
            {
                { "source", sourceName },
                { "clone", newName },
                { "kind", isTable ? "tagTable" : "block" },
                { "replacements", hits },
                { "file", targetFile },
                { "folder", folder ?? "" },
                { "applied", apply },
            };
            if (addresses.Any()) result["addresses"] = addresses;

            if (apply)
            {
                if (isTable) Ops.ImportTagTable(plc, targetFile, folder, true);
                else Ops.ImportBlock(plc, targetFile, folder, true);
            }
            return result;
        }

        /// <summary>Troca os pares em atributos e em texto de nós folha. Núcleo puro — sem Openness.</summary>
        internal static int Rewrite(XDocument doc, IEnumerable<KeyValuePair<string, string>> replaces)
        {
            int hits = 0;
            var pairs = replaces.ToList();
            foreach (var node in doc.Descendants())
            {
                foreach (var attribute in node.Attributes())
                    foreach (var pair in pairs)
                        if (attribute.Value.IndexOf(pair.Key, StringComparison.Ordinal) >= 0)
                        {
                            attribute.Value = attribute.Value.Replace(pair.Key, pair.Value);
                            hits++;
                        }
                if (node.HasElements || string.IsNullOrEmpty(node.Value)) continue;
                foreach (var pair in pairs)
                    if (node.Value.IndexOf(pair.Key, StringComparison.Ordinal) >= 0)
                    {
                        node.Value = node.Value.Replace(pair.Key, pair.Value);
                        hits++;
                    }
            }
            return hits;
        }

        /// <summary>
        /// Reendereça os LogicalAddress a partir de `at` (%M&lt;byte&gt;.&lt;bit&gt;), na ordem do XML.
        /// ponytail: só bits — tag de largura maior aborta em vez de virar endereço sobreposto.
        /// </summary>
        internal static List<string> Readdress(XDocument doc, string at)
        {
            var start = Regex.Match((at ?? "").Trim(), @"^%M(\d+)\.(\d+)$", RegexOptions.IgnoreCase);
            if (!start.Success)
                throw new ArgumentException("--at must look like %M432.0 (got '" + at + "').");

            var allocator = new BoolAddressAllocator(int.Parse(start.Groups[1].Value, CultureInfo.InvariantCulture));
            for (int skip = int.Parse(start.Groups[2].Value, CultureInfo.InvariantCulture); skip > 0; skip--)
                allocator.Skip();

            var assigned = new List<string>();
            foreach (var node in doc.Descendants().Where(e => e.Name.LocalName == "LogicalAddress").ToList())
            {
                var current = (node.Value ?? "").Trim();
                if (!Regex.IsMatch(current, @"^%M\d+\.\d+$", RegexOptions.IgnoreCase))
                    throw new InvalidOperationException("Tag address '" + current +
                        "' is not a %M bit — --at only reassigns bit tags. Remove --at and set it by hand.");
                node.Value = allocator.Next();
                assigned.Add(node.Value);
            }
            return assigned;
        }

        internal static List<KeyValuePair<string, string>> ParseReplaces(IEnumerable<string> args)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var raw in args ?? Enumerable.Empty<string>())
            {
                var i = (raw ?? "").IndexOf('=');
                if (i <= 0 || i == raw.Length - 1)
                    throw new ArgumentException("--replace expects OLD=NEW (got '" + raw + "').");
                pairs.Add(new KeyValuePair<string, string>(raw.Substring(0, i), raw.Substring(i + 1)));
            }
            return pairs;
        }

        private static string ObjectName(XDocument doc)
        {
            // bloco: <AttributeList><Name>; tabela de tags: <SW.Tags.PlcTagTable><AttributeList><Name>
            var name = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "AttributeList")
                ?.Elements().FirstOrDefault(e => e.Name.LocalName == "Name");
            if (name == null)
                throw new InvalidOperationException("Exported XML has no <AttributeList><Name>.");
            return name.Value;
        }
    }
}
