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

            var nodes = doc.Descendants().Where(e => e.Name.LocalName == "LogicalAddress").ToList();
            var parsed = new List<KeyValuePair<XElement, Match>>();
            foreach (var node in nodes)
            {
                var current = (node.Value ?? "").Trim();
                var m = Regex.Match(current, @"^%M([BWDX]?)(\d+)(?:\.(\d+))?$", RegexOptions.IgnoreCase);
                if (!m.Success)
                    throw new InvalidOperationException("Tag address '" + current +
                        "' is not a %M address — --at only reassigns %M tags. Remove --at and set it by hand.");
                parsed.Add(new KeyValuePair<XElement, Match>(node, m));
            }

            var assigned = new List<string>();
            // tabela só de bits: alocação sequencial e densa, que é o caso do clone de acionamento simples
            if (parsed.All(p => p.Value.Groups[1].Value.Length == 0))
            {
                foreach (var p in parsed) { p.Key.Value = allocator.Next(); assigned.Add(p.Key.Value); }
                return assigned;
            }

            // tabela mista (a da casa tem %MB/%MW/%MD junto dos bits): não dá para alocar densamente
            // sem quebrar alinhamento, então o bloco inteiro é DESLOCADO preservando o layout relativo.
            if (start.Groups[2].Value != "0")
                throw new ArgumentException("--at must end in .0 for a table with %MB/%MW/%MD tags (got '" + at + "').");
            int startByte = int.Parse(start.Groups[1].Value, CultureInfo.InvariantCulture);
            int minByte = parsed.Min(p => int.Parse(p.Value.Groups[2].Value, CultureInfo.InvariantCulture));
            int delta = startByte - minByte;
            foreach (var p in parsed)
            {
                string width = p.Value.Groups[1].Value.ToUpperInvariant();
                int b = int.Parse(p.Value.Groups[2].Value, CultureInfo.InvariantCulture) + delta;
                p.Key.Value = "%M" + width + b + (p.Value.Groups[3].Success ? "." + p.Value.Groups[3].Value : "");
                assigned.Add(p.Key.Value);
            }
            return assigned;
        }

        /// <summary>
        /// Aplica os pares num XML de export *antes* do import (offline) e grava a cópia em outDir.
        /// Sem pares, devolve o próprio arquivo — nada é copiado.
        /// </summary>
        public static string RewriteFile(string file, IList<KeyValuePair<string, string>> replaces, string outDir)
        {
            if (replaces == null || replaces.Count == 0) return file;
            var doc = XDocument.Load(file);
            Rewrite(doc, replaces);
            Directory.CreateDirectory(outDir);
            var name = Path.GetFileName(file);
            foreach (var pair in replaces) name = name.Replace(pair.Key, pair.Value);
            var target = Path.GetFullPath(Path.Combine(outDir, name));
            if (string.Equals(target, Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("--replace would overwrite the source XML: " + file);
            doc.Save(target);
            return target;
        }

        public static List<KeyValuePair<string, string>> ParseReplaces(IEnumerable<string> args)
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
