// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L61    class ScreenItems
//   L63    class Item
//   L74    núcleo puro (sem Openness, testável offline)
//   L77    .Parse
//   L104   .Groups
//   L134   .Patch
//   L162   case "x"
//   L163   case "y"
//   L164   case "w"
//   L165   case "h"
//   L181   .Remove
//   L208   .Rename
//   L245   .Group
//   L305   .CopyInto
//   L370   verbos
//   L373   .List
//   L401   .Set
//   L434   .Copy
//   L459   utilidades
//   L462   .ScreenElements
//   L468   .NameOf
//   L475   .Inside
//   L485   .Layers
//   L493   .LayerIndex
//   L500   .FolderOf
//   L507   .Coords
//   L515   .FirstTag
//   L523   .Attr
//   L529   .Num
//   L536   .SetNum
//   L543   .ParseId
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
        /// Apaga objetos pelo `ObjectName`. Nome ausente entra em `missing`; nome repetido é erro,
        /// pela mesma razão do `Patch` — apagar "um dos dois" seria adivinhação.
        /// </summary>
        internal static Dictionary<string, object> Remove(XDocument doc, IEnumerable<string> names)
        {
            var removed = new List<object>();
            var missing = new List<string>();
            foreach (var name in names)
            {
                var hits = ScreenElements(doc).Where(e => NameOf(e) == name).ToList();
                if (hits.Count == 0) { missing.Add(name); continue; }
                if (hits.Count > 1)
                    throw new InvalidOperationException("ObjectName '" + name + "' appears "
                        + hits.Count + " times in this screen — cannot tell which one to remove.");
                removed.Add(new Dictionary<string, object>
                {
                    { "item", name },
                    { "kind", hits[0].Name.LocalName.Substring("Hmi.Screen.".Length) },
                });
                hits[0].Remove();
            }
            return new Dictionary<string, object> { { "removed", removed }, { "missingRemove", missing } };
        }

        /// <summary>
        /// `OLD=NEW` no `ObjectName`. Nome auto-descritivo (`BF-01_EC-01_CMD_LIGA`) é o que faz o
        /// `list-screen-items` ser legível sem cruzar com a tag, e o que tira `--set`/`--remove` da
        /// dependência do contador do editor (`Switch_18`). Destino já ocupado é erro: o Portal
        /// recusa `ObjectName` repetido na tela.
        /// </summary>
        internal static Dictionary<string, object> Rename(XDocument doc, IEnumerable<string> pairs)
        {
            var taken = new HashSet<string>(ScreenElements(doc).Select(NameOf).Where(n => n != null),
                StringComparer.Ordinal);
            var renamed = new List<object>();
            var missing = new List<string>();
            foreach (var pair in pairs)
            {
                var eq = pair.IndexOf('=');
                if (eq <= 0) throw new ArgumentException("--rename expects OLD=NEW, got: " + pair);
                var old = pair.Substring(0, eq); var now = pair.Substring(eq + 1);
                if (now.Length == 0) throw new ArgumentException("--rename expects OLD=NEW, got: " + pair);
                var hits = ScreenElements(doc).Where(e => NameOf(e) == old).ToList();
                if (hits.Count == 0) { missing.Add(old); continue; }
                if (hits.Count > 1)
                    throw new InvalidOperationException("ObjectName '" + old + "' appears "
                        + hits.Count + " times in this screen — cannot tell which one to rename.");
                if (taken.Contains(now))
                    throw new InvalidOperationException("ObjectName '" + now
                        + "' already exists in this screen — the Portal rejects duplicates.");
                var attrs = hits[0].Elements().First(c => c.Name.LocalName == "AttributeList");
                attrs.Elements().First(c => c.Name.LocalName == "ObjectName").Value = now;
                taken.Remove(old); taken.Add(now);
                renamed.Add(new Dictionary<string, object> { { "from", old }, { "to", now } });
            }
            return new Dictionary<string, object> { { "renamed", renamed }, { "missingRename", missing } };
        }

        /// <summary>
        /// `NOME=x,y,w,h` — embrulha num `Hmi.Screen.Group` os objetos **inteiramente contidos** na
        /// região (mesmo critério do `CopyInto`), dentro da própria camada. Só objeto de primeiro
        /// nível da camada entra: objeto que já está num grupo continua onde está.
        ///
        /// O grupo é o que faz o operador mover/copiar o skid inteiro na GUI como uma peça, e o que
        /// dá nome ao conjunto — a coordenada dos filhos é **absoluta** no SimaticML (medido na tela
        /// Gradeamento, que já vem agrupada), então embrulhar não mexe em geometria nenhuma.
        /// </summary>
        internal static Dictionary<string, object> Group(XDocument doc, IEnumerable<string> specs)
        {
            var taken = new HashSet<string>(ScreenElements(doc).Select(NameOf).Where(n => n != null),
                StringComparer.Ordinal);
            long nextId = doc.Descendants().Select(e => (string)e.Attribute("ID"))
                .Where(v => v != null).Select(ParseId).DefaultIfEmpty(0).Max() + 1;
            var grouped = new List<object>();
            foreach (var spec in specs)
            {
                var eq = spec.IndexOf('=');
                if (eq <= 0) throw new ArgumentException("--group expects NAME=x,y,w,h, got: " + spec);
                var name = spec.Substring(0, eq);
                var region = Coords(spec.Substring(eq + 1), 4, "--group");
                if (taken.Contains(name))
                    throw new InvalidOperationException("ObjectName '" + name
                        + "' already exists in this screen — pick another group name.");

                foreach (var layer in Layers(doc))
                {
                    var members = layer.Elements()
                        .Where(e => e.Name.LocalName.StartsWith("Hmi.Screen.") && Inside(e, region)).ToList();
                    if (members.Count == 0) continue;
                    var xn = layer.Name.Namespace; // export real vem sem namespace; fixture tem — segue o pai
                    var group = new XElement(xn + "Hmi.Screen.Group",
                        new XAttribute("ID", nextId++.ToString("X", CultureInfo.InvariantCulture)),
                        new XAttribute("CompositionName", "ScreenItems"),
                        new XElement(xn + "AttributeList",
                            new XElement(xn + "ObjectName", name),
                            new XElement(xn + "TabIndex", -1)),
                        new XElement(xn + "ObjectList"));
                    members[0].AddBeforeSelf(group);
                    var list = group.Elements().First(c => c.Name.LocalName == "ObjectList");
                    foreach (var m in members) { m.Remove(); list.Add(m); }
                    taken.Add(name);
                    grouped.Add(new Dictionary<string, object>
                    {
                        { "group", name }, { "items", members.Count },
                        { "members", members.Select(NameOf).ToList() },
                    });
                    break; // um grupo por spec: região que atravessa camadas seria grupo por camada
                }
                if (!taken.Contains(name))
                    grouped.Add(new Dictionary<string, object>
                    {
                        { "group", name }, { "items", 0 },
                        { "note", "nothing fully inside the region — check it with list-screen-items --group" },
                    });
            }
            return new Dictionary<string, object> { { "grouped", grouped } };
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
            List<string> sets, List<string> removes, List<string> renames, List<string> groups,
            bool apply, string outDir)
        {
            if (sets.Count + removes.Count + renames.Count + groups.Count == 0)
                throw new ArgumentException("set-screen-items requires at least one of --set, --remove, --rename, --group.");
            var export = (Dictionary<string, object>)Hmi.ExportScreen(session, device, screenPath, outDir);
            var file = (string)export["file"];
            var doc = XDocument.Load(file);
            // Ordem fixa: mover, apagar, renomear, agrupar. Agrupar por último porque a região se
            // confere contra a geometria JÁ corrigida pelos --set desta mesma chamada.
            var patch = Patch(doc, sets);
            foreach (var kv in Remove(doc, removes)) patch[kv.Key] = kv.Value;
            foreach (var kv in Rename(doc, renames)) patch[kv.Key] = kv.Value;
            foreach (var kv in Group(doc, groups)) patch[kv.Key] = kv.Value;
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

        /// <summary>Todo `Hmi.Screen.*` que tem `AttributeList` (a própria tela e camadas ficam fora).</summary>
        static IEnumerable<XElement> ScreenElements(XDocument doc)
        {
            return doc.Descendants().Where(e => e.Name.LocalName.StartsWith("Hmi.Screen.")
                && e.Elements().Any(c => c.Name.LocalName == "AttributeList")).ToList();
        }

        static string NameOf(XElement el)
        {
            var attrs = el.Elements().FirstOrDefault(c => c.Name.LocalName == "AttributeList");
            return attrs == null ? null : Attr(attrs, "ObjectName");
        }

        /// <summary>Objeto inteiramente dentro de `x,y,w,h`. Sem geometria = fora.</summary>
        static bool Inside(XElement el, int[] region)
        {
            var attrs = el.Elements().FirstOrDefault(c => c.Name.LocalName == "AttributeList");
            if (attrs == null || attrs.Elements().All(c => c.Name.LocalName != "Left")) return false;
            int x = Num(attrs, "Left"), y = Num(attrs, "Top"), w = Num(attrs, "Width"), h = Num(attrs, "Height");
            return x >= region[0] && y >= region[1]
                && x + w <= region[0] + region[2] && y + h <= region[1] + region[3];
        }

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
