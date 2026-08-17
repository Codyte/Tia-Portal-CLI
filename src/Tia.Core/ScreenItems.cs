// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L55    class ScreenItems
//   L57    class Item
//   L68    núcleo puro (sem Openness, testável offline)
//   L71    .Parse
//   L98    .Groups
//   L128   .Patch
//   L156   case "x"
//   L157   case "y"
//   L158   case "w"
//   L159   case "h"
//   L180   .CopyInto
//   L245   verbos
//   L248   .List
//   L276   .Set
//   L302   .Copy
//   L327   utilidades
//   L330   .Layers
//   L338   .LayerIndex
//   L345   .FolderOf
//   L352   .Coords
//   L360   .FirstTag
//   L368   .Attr
//   L374   .Num
//   L381   .SetNum
//   L388   .ParseId
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Tia.Core
{
    /// <summary>
    /// Objetos de dentro de uma tela: listar, mover/redimensionar e copiar um grupo de uma tela para
    /// outra. Trabalha sempre no SimaticML exportado — a API do WinCC clássico não expõe o objeto de
    /// tela, e o roundtrip export→edita→import (Override) é o mesmo caminho de todo verbo de escrita
    /// do repo.
    ///
    /// Por que existe: a tela real de área é XML de 500-800 KB, e a mesma informação em forma de
    /// tabela cabe em 7 KB (medido: 150 objetos, 798 KB → 7,4 KB). Sem isso, olhar tela custa o
    /// arquivo inteiro no contexto.
    ///
    /// **O molde é o projeto, não este código.** Não há catálogo de estampas aqui: cada tela da casa
    /// tem seu dialeto (medido 2026-08-17 — o cartão de motor do Biofiltro usa símbolo 77x63 e
    /// `Switch` 94x36; o do Gradeamento, 87x58 e `Button` 93x39). O grupo a replicar sai da tela que
    /// serve de molde, em runtime.
    /// </summary>
    public static class ScreenItems
    {
        public sealed class Item
        {
            public string Name, Kind, Tag;
            public int X, Y, W, H;
        }

        // Primeiro código de equipamento da tag: `..._BIOFILTRO_EXAUSTOR_CENTRIFUGO_BF-02-EC-01_STS_x`
        // → BF-02 (o skid), que é a unidade que se replica. Tag sem código não agrupa.
        static readonly Regex EquipRe = new Regex(@"[A-Z]{1,4}-[0-9]{1,3}[A-Z]?",
            RegexOptions.CultureInvariant);

        // ---------- núcleo puro (sem Openness, testável offline) ----------

        /// <summary>Todo objeto de tela com geometria, na ordem do arquivo.</summary>
        internal static List<Item> Parse(XDocument doc)
        {
            var rows = new List<Item>();
            foreach (var el in doc.Descendants().Where(e => e.Name.LocalName.StartsWith("Hmi.Screen.")))
            {
                var attrs = el.Elements().FirstOrDefault(c => c.Name.LocalName == "AttributeList");
                if (attrs == null) continue;
                var left = attrs.Elements().FirstOrDefault(c => c.Name.LocalName == "Left");
                if (left == null) continue; // a própria tela e os objetos sem geometria caem fora
                rows.Add(new Item
                {
                    Name = Attr(attrs, "ObjectName"),
                    Kind = el.Name.LocalName.Substring("Hmi.Screen.".Length),
                    X = Num(attrs, "Left"), Y = Num(attrs, "Top"),
                    W = Num(attrs, "Width"), H = Num(attrs, "Height"),
                    Tag = FirstTag(el),
                });
            }
            return rows;
        }

        /// <summary>
        /// Agrupa por equipamento lido do nome da tag e devolve o bbox de cada um — que é a região
        /// pronta para o `copy-screen-items`. **Bbox é dos objetos COM tag**: fundo e rótulo não
        /// carregam equipamento, então a região quase sempre precisa ser alargada à mão até o
        /// retângulo de fundo (que aparece na lista).
        /// </summary>
        internal static List<object> Groups(List<Item> items)
        {
            var by = new Dictionary<string, List<Item>>(StringComparer.Ordinal);
            foreach (var it in items)
            {
                if (string.IsNullOrEmpty(it.Tag)) continue;
                var m = EquipRe.Match(it.Tag);
                if (!m.Success) continue;
                List<Item> bucket;
                if (!by.TryGetValue(m.Value, out bucket)) by[m.Value] = bucket = new List<Item>();
                bucket.Add(it);
            }
            return by.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (object)new Dictionary<string, object>
            {
                { "equipment", p.Key },
                { "items", p.Value.Count },
                { "region", string.Join(",", new[]
                    {
                        p.Value.Min(i => i.X), p.Value.Min(i => i.Y),
                        p.Value.Max(i => i.X + i.W) - p.Value.Min(i => i.X),
                        p.Value.Max(i => i.Y + i.H) - p.Value.Min(i => i.Y),
                    }.Select(n => n.ToString(CultureInfo.InvariantCulture))) },
            }).ToList();
        }

        /// <summary>
        /// Aplica `Nome:x=530,y=356,w=90,h=36` (qualquer subconjunto dos 4) no XML. Nome que não
        /// existe entra em `missing` e os outros seguem — mesma política de step do `run --script`.
        /// Nome repetido na tela é erro: mover "um dos dois" seria adivinhação.
        /// </summary>
        internal static Dictionary<string, object> Patch(XDocument doc, IEnumerable<string> sets)
        {
            var applied = new List<object>();
            var missing = new List<string>();
            foreach (var raw in sets)
            {
                var cut = raw.LastIndexOf(':');
                if (cut < 0) throw new ArgumentException("--set expects \"<ObjectName>:x=..,y=..,w=..,h=..\", got: " + raw);
                var name = raw.Substring(0, cut);
                var hits = doc.Descendants()
                    .Where(e => e.Name.LocalName == "AttributeList" && Attr(e, "ObjectName") == name)
                    .ToList();
                if (hits.Count == 0) { missing.Add(name); continue; }
                if (hits.Count > 1)
                    throw new InvalidOperationException("ObjectName '" + name + "' appears "
                        + hits.Count + " times in this screen — cannot tell which one to move.");
                var attrs = hits[0];
                var row = new Dictionary<string, object> { { "item", name } };
                foreach (var pair in raw.Substring(cut + 1).Split(','))
                {
                    if (pair.Length == 0) continue;
                    var eq = pair.IndexOf('=');
                    if (eq < 0) throw new ArgumentException("--set field expects k=v, got: " + pair);
                    var key = pair.Substring(0, eq).Trim().ToLowerInvariant();
                    var value = int.Parse(pair.Substring(eq + 1).Trim(), CultureInfo.InvariantCulture);
                    string tagName;
                    switch (key)
                    {
                        case "x": tagName = "Left"; break;
                        case "y": tagName = "Top"; break;
                        case "w": tagName = "Width"; break;
                        case "h": tagName = "Height"; break;
                        default: throw new ArgumentException("--set field must be x, y, w or h, got: " + key);
                    }
                    var el = attrs.Elements().FirstOrDefault(c => c.Name.LocalName == tagName);
                    if (el == null) { attrs.Add(new XElement(attrs.Name.Namespace + tagName, value)); row[key] = value; }
                    else { row[key] = el.Value + "->" + value; el.Value = value.ToString(CultureInfo.InvariantCulture); }
                }
                applied.Add(row);
            }
            return new Dictionary<string, object> { { "applied", applied }, { "missing", missing } };
        }

        /// <summary>
        /// Copia para `to` todo objeto de `from` **inteiramente contido** na região, deslocado por
        /// `at`. Conter inteiro é o critério porque é o que faz "o cartão do motor" virar uma seleção
        /// sem nomear 13 objetos — objeto que atravessa a borda fica de fora, de propósito.
        ///
        /// Renumerar `ID` não é opcional: é contador local do arquivo, e o precedente do `&lt;Number&gt;`
        /// de tela mostra que colisão de identificador nesta engine derruba o Portal inteiro.
        /// `ObjectName` repetido também é rejeitado pelo Portal, então colisão ganha sufixo.
        /// </summary>
        internal static Dictionary<string, object> CopyInto(XDocument from, XDocument to,
            int[] region, int[] at, IEnumerable<string> replaces)
        {
            // O objeto de tela não fica na ObjectList da tela: fica dentro de uma <ScreenLayer>.
            // Colar no nível errado passa no XML e o Portal recusa no import — "The 'ScreenItems'
            // composition ... is not supported" (medido 2026-08-17). Cola-se na camada de MESMO
            // índice do destino, que é o que preserva visibilidade e ordem de desenho.
            var srcLayers = Layers(from);
            var dstLayers = Layers(to);
            if (dstLayers.Count == 0)
                throw new InvalidOperationException("Target screen has no <Hmi.Screen.ScreenLayer> to paste into.");

            var names = new HashSet<string>(to.Descendants()
                .Where(e => e.Name.LocalName == "ObjectName").Select(e => e.Value), StringComparer.Ordinal);
            long nextId = to.Descendants().Select(e => (string)e.Attribute("ID"))
                .Where(v => v != null).Select(ParseId).DefaultIfEmpty(0).Max() + 1;

            int dx = at[0] - region[0], dy = at[1] - region[1];
            var copied = new List<object>();
            foreach (var el in from.Descendants().Where(e => e.Name.LocalName.StartsWith("Hmi.Screen.")).ToList())
            {
                var attrs = el.Elements().FirstOrDefault(c => c.Name.LocalName == "AttributeList");
                if (attrs == null || attrs.Elements().All(c => c.Name.LocalName != "Left")) continue;
                int x = Num(attrs, "Left"), y = Num(attrs, "Top"), w = Num(attrs, "Width"), h = Num(attrs, "Height");
                if (x < region[0] || y < region[1] || x + w > region[0] + region[2] || y + h > region[1] + region[3])
                    continue;

                var clone = new XElement(el);
                foreach (var pair in replaces)
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0) throw new ArgumentException("--replace expects OLD=NEW, got: " + pair);
                    var old = pair.Substring(0, eq); var now = pair.Substring(eq + 1);
                    foreach (var node in clone.DescendantNodesAndSelf().OfType<XText>())
                        if (node.Value.Contains(old)) node.Value = node.Value.Replace(old, now);
                }
                var cAttrs = clone.Elements().First(c => c.Name.LocalName == "AttributeList");
                SetNum(cAttrs, "Left", x + dx);
                SetNum(cAttrs, "Top", y + dy);

                var name = Attr(cAttrs, "ObjectName");
                var final = name;
                for (int n = 1; final != null && names.Contains(final); n++) final = name + "_" + n;
                if (final != name) cAttrs.Elements().First(c => c.Name.LocalName == "ObjectName").Value = final;
                if (final != null) names.Add(final);

                foreach (var node in clone.DescendantsAndSelf().Where(e => e.Attribute("ID") != null))
                    node.Attribute("ID").Value = nextId++.ToString("X", CultureInfo.InvariantCulture);

                var layer = LayerIndex(srcLayers, el);
                if (layer >= dstLayers.Count)
                    throw new InvalidOperationException("Source item '" + name + "' is on screen layer "
                        + layer + ", and the target screen has only " + dstLayers.Count + " layer(s).");
                dstLayers[layer].Add(clone);
                copied.Add(new Dictionary<string, object>
                {
                    { "item", final }, { "kind", clone.Name.LocalName.Substring("Hmi.Screen.".Length) },
                    { "from", x + "," + y }, { "to", (x + dx) + "," + (y + dy) },
                    { "layer", layer },
                    { "renamedFrom", final == name ? null : name },
                });
            }
            return new Dictionary<string, object> { { "copied", copied }, { "count", copied.Count } };
        }

        // ---------- verbos ----------

        /// <summary>Tabela de objetos da tela. `--group` acrescenta a região por equipamento.</summary>
        public static object List(TiaSession session, string device, string screenPath, string like,
            bool group, string outDir)
        {
            var export = (Dictionary<string, object>)Hmi.ExportScreen(session, device, screenPath, outDir);
            var doc = XDocument.Load((string)export["file"]);
            var items = Parse(doc);
            var shown = like == null ? items
                : items.Where(i => (i.Name ?? "").IndexOf(like, StringComparison.OrdinalIgnoreCase) >= 0
                                || (i.Tag ?? "").IndexOf(like, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var result = new Dictionary<string, object>
            {
                { "device", export["device"] }, { "screen", screenPath },
                { "count", shown.Count }, { "total", items.Count },
                { "items", shown.Select(i => (object)new Dictionary<string, object>
                    {
                        { "item", i.Name }, { "kind", i.Kind },
                        { "x", i.X }, { "y", i.Y }, { "w", i.W }, { "h", i.H },
                        { "tag", i.Tag },
                    }).ToList() },
            };
            if (group) result["equipment"] = Groups(items);
            return result;
        }

        /// <summary>
        /// Move/redimensiona objetos da tela. Um export e um import para N objetos — por objeto seria
        /// inviável (import de tela mediu ~20 s).
        /// </summary>
        public static object Set(TiaSession session, string device, string screenPath,
            List<string> sets, bool apply, string outDir)
        {
            if (sets.Count == 0) throw new ArgumentException("set-screen-items requires at least one --set.");
            var export = (Dictionary<string, object>)Hmi.ExportScreen(session, device, screenPath, outDir);
            var file = (string)export["file"];
            var doc = XDocument.Load(file);
            var patch = Patch(doc, sets);
            var edited = Path.Combine(Path.GetDirectoryName(file),
                Path.GetFileNameWithoutExtension(file) + ".edit.xml");
            doc.Save(edited);

            var result = new Dictionary<string, object>
            {
                { "device", export["device"] }, { "screen", screenPath },
                { "file", edited }, { "applied", apply },
            };
            foreach (var kv in patch) result[kv.Key] = kv.Value;
            if (apply) result["import"] = Hmi.ImportScreen(session, device, edited, FolderOf(screenPath), true);
            return result;
        }

        /// <summary>
        /// Estampa: copia o grupo de objetos de uma região da tela molde para outra tela, deslocado e
        /// com os nomes trocados (`--replace BF-01=BF-05`).
        /// </summary>
        public static object Copy(TiaSession session, string device, string fromScreen, string toScreen,
            int[] region, int[] at, List<string> replaces, bool apply, string outDir)
        {
            var src = (Dictionary<string, object>)Hmi.ExportScreen(session, device, fromScreen, outDir);
            var dst = (Dictionary<string, object>)Hmi.ExportScreen(session, device, toScreen, outDir);
            var to = XDocument.Load((string)dst["file"]);
            var copy = CopyInto(XDocument.Load((string)src["file"]), to, region, at, replaces);
            var edited = Path.Combine(Path.GetDirectoryName((string)dst["file"]),
                Path.GetFileNameWithoutExtension((string)dst["file"]) + ".edit.xml");
            to.Save(edited);

            var result = new Dictionary<string, object>
            {
                { "device", dst["device"] }, { "from", fromScreen }, { "screen", toScreen },
                { "region", string.Join(",", region.Select(n => n.ToString(CultureInfo.InvariantCulture))) },
                { "at", string.Join(",", at.Select(n => n.ToString(CultureInfo.InvariantCulture))) },
                { "file", edited }, { "applied", apply },
            };
            foreach (var kv in copy) result[kv.Key] = kv.Value;
            if ((int)copy["count"] == 0)
                result["note"] = "nothing fully inside the region — check it with list-screen-items --group";
            if (apply) result["import"] = Hmi.ImportScreen(session, device, edited, FolderOf(toScreen), true);
            return result;
        }

        // ---------- utilidades ----------

        /// <summary>A `ObjectList` de cada `ScreenLayer`, na ordem do arquivo (índice = camada).</summary>
        static List<XElement> Layers(XDocument doc)
        {
            return doc.Descendants().Where(e => e.Name.LocalName == "Hmi.Screen.ScreenLayer")
                .Select(l => l.Elements().FirstOrDefault(c => c.Name.LocalName == "ObjectList"))
                .Where(l => l != null).ToList();
        }

        /// <summary>Camada onde o objeto está na origem; 0 quando não há camada acima dele.</summary>
        static int LayerIndex(List<XElement> layers, XElement item)
        {
            for (int i = 0; i < layers.Count; i++)
                if (layers[i].Descendants().Any(e => ReferenceEquals(e, item))) return i;
            return 0;
        }

        static string FolderOf(string screenPath)
        {
            var cut = screenPath.LastIndexOf('/');
            return cut < 0 ? null : screenPath.Substring(0, cut);
        }

        /// <summary>`--region x,y,w,h` / `--at x,y`. Erro de forma é de uso, então falha claro.</summary>
        public static int[] Coords(string text, int count, string option)
        {
            var parts = (text ?? "").Split(',');
            if (parts.Length != count)
                throw new ArgumentException(option + " expects " + count + " numbers separated by comma, got: " + text);
            return parts.Select(p => int.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();
        }

        static string FirstTag(XElement el)
        {
            var tag = el.Descendants().FirstOrDefault(e => e.Name.LocalName == "Tag");
            if (tag == null) return null;
            var name = tag.Elements().FirstOrDefault(e => e.Name.LocalName == "Name");
            return name == null ? null : name.Value;
        }

        static string Attr(XElement attrs, string name)
        {
            var el = attrs.Elements().FirstOrDefault(c => c.Name.LocalName == name);
            return el == null ? null : el.Value;
        }

        static int Num(XElement attrs, string name)
        {
            var v = Attr(attrs, name);
            int n;
            return v != null && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : 0;
        }

        static void SetNum(XElement attrs, string name, int value)
        {
            var el = attrs.Elements().FirstOrDefault(c => c.Name.LocalName == name);
            if (el == null) attrs.Add(new XElement(attrs.Name.Namespace + name, value));
            else el.Value = value.ToString(CultureInfo.InvariantCulture);
        }

        static long ParseId(string text)
        {
            long n;
            return long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out n) ? n : 0;
        }
    }
}
