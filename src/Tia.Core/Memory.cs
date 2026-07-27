// NAV INDEX
//   1-20    usings, namespace
//   22-60   Memory.FreeM — varre tags do PLC, devolve mapa de %M livre
//   62-95   Occupied — parse de LogicalAddress + largura por tipo (núcleo puro, testável offline)
//   97-130  helpers: Gaps, Width, CollectTags
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>
    /// free-memory: mapa de ocupação da área %M do PLC. Read-only — responde
    /// "onde cabe um bloco de N bytes" antes de criar tags novas de acionamento.
    /// </summary>
    public static class Memory
    {
        public static object FreeM(PlcSoftware plc, int bytes, int from, int count)
        {
            if (bytes < 1) bytes = 1;
            var addresses = new List<KeyValuePair<string, string>>();
            CollectTags(plc.TagTableGroup, addresses);

            var used = Occupied(addresses);
            var gaps = Gaps(used, bytes, from).Take(count < 1 ? 5 : count).ToList();

            return new Dictionary<string, object>
            {
                { "tags", addresses.Count },
                { "usedBytes", used.Count },
                { "highestUsedByte", used.Count == 0 ? -1 : used.Max() },
                { "wanted", bytes },
                { "free", gaps.Select(g => new Dictionary<string, object>
                    {
                        { "start", g.Key },
                        { "length", g.Value },
                        { "address", "%M" + g.Key + ".0" },
                    }).ToList() },
            };
        }

        /// <summary>Bytes de %M ocupados por (address, dataType). Núcleo puro — sem Openness.</summary>
        internal static SortedSet<int> Occupied(IEnumerable<KeyValuePair<string, string>> tags)
        {
            var used = new SortedSet<int>();
            foreach (var tag in tags)
            {
                var address = (tag.Key ?? "").Trim();
                // %M430.0 (bit) | %MB10 | %MW20 | %MD24 — qualquer outra área é ignorada
                var m = Regex.Match(address, @"^%M([BWDX]?)(\d+)(?:\.(\d+))?$", RegexOptions.IgnoreCase);
                if (!m.Success) continue;
                int start = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                int width = Width(m.Groups[1].Value, m.Groups[3].Success, tag.Value);
                for (int i = 0; i < width; i++) used.Add(start + i);
            }
            return used;
        }

        /// <summary>Largura em bytes: prefixo do endereço manda; sem prefixo, o tipo da tag decide.</summary>
        private static int Width(string prefix, bool hasBit, string dataType)
        {
            switch ((prefix ?? "").ToUpperInvariant())
            {
                case "X": return 1;
                case "B": return 1;
                case "W": return 2;
                case "D": return 4;
            }
            if (hasBit) return 1;                       // %M430.0 = bit isolado
            switch ((dataType ?? "").ToUpperInvariant())
            {
                case "BOOL": case "BYTE": case "SINT": case "USINT": case "CHAR": return 1;
                case "WORD": case "INT": case "UINT": case "S5TIME": case "DATE": return 2;
                case "DWORD": case "DINT": case "UDINT": case "REAL": case "TIME": case "TIME_OF_DAY": return 4;
                case "LWORD": case "LINT": case "ULINT": case "LREAL": case "LTIME": return 8;
                default: return 1;
            }
        }

        /// <summary>Buracos com pelo menos `bytes` bytes livres consecutivos, a partir de `from`.</summary>
        private static IEnumerable<KeyValuePair<int, int>> Gaps(SortedSet<int> used, int bytes, int from)
        {
            int limit = (used.Count == 0 ? from : Math.Max(used.Max(), from)) + bytes + 1;
            int runStart = -1;
            for (int b = Math.Max(0, from); b <= limit; b++)
            {
                if (used.Contains(b))
                {
                    if (runStart >= 0 && b - runStart >= bytes)
                        yield return new KeyValuePair<int, int>(runStart, b - runStart);
                    runStart = -1;
                    continue;
                }
                if (runStart < 0) runStart = b;
            }
            if (runStart >= 0)
                yield return new KeyValuePair<int, int>(runStart, -1);   // cauda: livre até o fim da área M
        }

        private static void CollectTags(PlcTagTableGroup group, List<KeyValuePair<string, string>> into)
        {
            foreach (PlcTagTable table in group.TagTables)
                foreach (PlcTag tag in table.Tags)
                    into.Add(new KeyValuePair<string, string>(tag.LogicalAddress, tag.DataTypeName));
            foreach (PlcTagTableUserGroup sub in group.Groups)
                CollectTags(sub, into);
        }
    }
}
