// NAV INDEX
// 23-74    Explain — XML exportado → linhas compactas (+ arquivo .explain.txt)
// 76-116   interface do bloco (IN/OUT/InOut) e árvore de membros de DB
// 118-152  rede: título, comentário, statements
// 156-241  Net: índice de Parts/Access/Wires; Statements/Statement/CallLines (bobina, MOVE, chamada)
// 243-300  Operand/Source/Expr/PartExpr/Group — expressão reconstruída dos Wires
// 302-320  Label — Access → "DB".A.B, constante, endereço
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Tia.Core
{
    /// <summary>
    /// XML exportado (LAD/FBD) → texto compacto: uma linha por bobina/chamada, expressão booleana
    /// reconstruída dos Wires. ~200KB de XML viram ~3KB. Inverso do <see cref="LadConverter"/>.
    /// Offline de propósito: nenhum tipo Siemens.Engineering, roda e testa sem TIA instalado.
    /// </summary>
    public static class BlockExplain
    {
        public static Dictionary<string, object> Explain(string xmlFile, string outDir)
        {
            var full = Path.GetFullPath(xmlFile);
            if (!File.Exists(full)) throw new FileNotFoundException("XML not found: " + full);
            var doc = XDocument.Load(full);
            var root = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName.StartsWith("SW."));
            if (root == null) throw new InvalidOperationException("Not an Openness export (no SW.* root object).");

            var kind = root.Name.LocalName.Replace("SW.Blocks.", "").Replace("SW.Types.", "").Replace("SW.Tags.", "");
            var attrs = root.Elements().FirstOrDefault(e => e.Name.LocalName == "AttributeList");
            var name = Val(attrs, "Name") ?? Path.GetFileNameWithoutExtension(full);
            var lang = Val(attrs, "ProgrammingLanguage");
            var units = root.Descendants().Where(e => e.Name.LocalName == "SW.Blocks.CompileUnit").ToList();

            var lines = new List<string>();
            lines.Add(kind + " \"" + name + "\"" + (lang == null ? "" : " (" + lang + ")")
                + (units.Count > 0 ? " · " + units.Count + " redes" : ""));
            lines.AddRange(Interface(attrs));

            int n = 0;
            foreach (var u in units) { n++; lines.AddRange(Network(u, n)); }
            if (units.Count == 0) lines.AddRange(Members(attrs));

            Directory.CreateDirectory(outDir);
            var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var txt = Path.GetFullPath(Path.Combine(outDir, safe + ".explain.txt"));
            File.WriteAllText(txt, string.Join(Environment.NewLine, lines));

            return new Dictionary<string, object>
            {
                { "block", name },
                { "kind", kind },
                { "language", lang },
                { "networks", units.Count },
                { "file", txt },
                { "chars", lines.Sum(l => l.Length + 1) },
                { "text", lines },
            };
        }

        private static string Val(XElement parent, string local)
        {
            if (parent == null) return null;
            var e = parent.Elements().FirstOrDefault(x => x.Name.LocalName == local);
            return e == null ? null : e.Value;
        }

        private static IEnumerable<XElement> Kids(XElement p, string local)
        {
            return p == null ? Enumerable.Empty<XElement>()
                : p.Elements().Where(x => x.Name.LocalName == local);
        }

        // ---------- interface / membros de DB ----------

        /// <summary>Seções de parâmetro do bloco. Temp/Constant ficam de fora: ruído para diagnóstico.</summary>
        private static IEnumerable<string> Interface(XElement attrs)
        {
            var iface = Kids(attrs, "Interface").FirstOrDefault();
            var sections = iface == null ? null : iface.Descendants().Where(e => e.Name.LocalName == "Section");
            if (sections == null) yield break;
            foreach (var s in sections)
            {
                var sec = (string)s.Attribute("Name");
                if (sec == "Temp" || sec == "Constant" || sec == "Static") continue;
                var mem = Kids(s, "Member").Select(m => (string)m.Attribute("Name") + " : " + (string)m.Attribute("Datatype")).ToList();
                if (mem.Count == 1 && mem[0] == "Ret_Val : Void") continue; // FC sem retorno: ruído
                if (mem.Count > 0) yield return sec.ToUpperInvariant() + " " + string.Join(", ", mem);
            }
        }

        /// <summary>DB/UDT: árvore de membros (2 níveis; o resto vira contagem — ponytail: fundo do DB raramente importa no diagnóstico).</summary>
        private static IEnumerable<string> Members(XElement attrs)
        {
            var iface = Kids(attrs, "Interface").FirstOrDefault();
            if (iface == null) yield break;
            foreach (var s in iface.Descendants().Where(e => e.Name.LocalName == "Section"))
                foreach (var line in Member(Kids(s, "Member"), ""))
                    yield return line;
        }

        private static IEnumerable<string> Member(IEnumerable<XElement> members, string indent)
        {
            foreach (var m in members)
            {
                var kids = Kids(m, "Member").ToList();
                var start = Val(m, "StartValue");
                yield return indent + (string)m.Attribute("Name") + " : " + (string)m.Attribute("Datatype")
                    + (string.IsNullOrEmpty(start) ? "" : " = " + start)
                    + (kids.Count > 0 && indent.Length >= 4 ? "  (" + kids.Count + " membros)" : "");
                if (kids.Count > 0 && indent.Length < 4)
                    foreach (var line in Member(kids, indent + "  ")) yield return line;
            }
        }

        // ---------- rede ----------

        private static IEnumerable<string> Network(XElement unit, int n)
        {
            var attrs = Kids(unit, "AttributeList").FirstOrDefault();
            var title = Text(unit, "Title");
            var comment = Text(unit, "Comment");
            yield return "N" + n + (title == null ? "" : " · " + title);
            if (comment != null) yield return "   # " + Cut(comment, 200);

            var flg = attrs == null ? null : attrs.Descendants().FirstOrDefault(e => e.Name.LocalName == "FlgNet");
            if (flg == null)
            {
                var st = attrs == null ? null : attrs.Descendants().FirstOrDefault(e => e.Name.LocalName == "StructuredText");
                yield return st != null ? "   " + Cut(Collapse(st.Value), 400) : "   (rede vazia)";
                yield break;
            }
            foreach (var line in new Net(flg).Statements()) yield return "   " + line;
        }

        /// <summary>Texto multilíngue: pt-BR quando existe, senão o primeiro.</summary>
        private static string Text(XElement unit, string composition)
        {
            var mlt = unit.Elements().Where(e => e.Name.LocalName == "ObjectList")
                .SelectMany(o => o.Elements().Where(e => e.Name.LocalName == "MultilingualText"))
                .FirstOrDefault(m => (string)m.Attribute("CompositionName") == composition);
            if (mlt == null) return null;
            var items = mlt.Descendants().Where(e => e.Name.LocalName == "AttributeList").ToList();
            var pick = items.FirstOrDefault(a => Val(a, "Culture") == "pt-BR") ?? items.FirstOrDefault();
            var t = pick == null ? null : Val(pick, "Text");
            return string.IsNullOrWhiteSpace(t) ? null : Collapse(t);
        }

        private static string Collapse(string s) { return Regex.Replace(s, @"\s+", " ").Trim(); }
        private static string Cut(string s, int max) { return s.Length <= max ? s : s.Substring(0, max) + "…"; }

        // ---------- FlgNet → expressão ----------

        private class Net
        {
            private readonly Dictionary<string, XElement> _parts = new Dictionary<string, XElement>();
            private readonly Dictionary<string, string> _access = new Dictionary<string, string>();
            private readonly List<XElement> _wires;

            public Net(XElement flg)
            {
                var parts = flg.Elements().FirstOrDefault(e => e.Name.LocalName == "Parts");
                foreach (var p in parts == null ? Enumerable.Empty<XElement>() : parts.Elements())
                {
                    var uid = (string)p.Attribute("UId");
                    if (uid == null) continue;
                    if (p.Name.LocalName == "Access") _access[uid] = Label(p);
                    else _parts[uid] = p;
                }
                foreach (var call in _parts.Values.Where(p => p.Name.LocalName == "Call"))
                    foreach (var inst in call.Descendants().Where(e => e.Name.LocalName == "Instance"))
                    {
                        var uid = (string)inst.Attribute("UId");
                        if (uid != null) _access[uid] = Label(inst);
                    }
                var wires = flg.Elements().FirstOrDefault(e => e.Name.LocalName == "Wires");
                _wires = (wires == null ? Enumerable.Empty<XElement>() : wires.Elements()).ToList();
            }

            /// <summary>Uma linha por consumidor (bobina, MOVE, chamada), na ordem do XML.</summary>
            public IEnumerable<string> Statements()
            {
                bool any = false;
                foreach (var kv in _parts.OrderBy(k => Num(k.Key)))
                {
                    var line = Statement(kv.Key, kv.Value);
                    if (line == null) continue;
                    any = true;
                    foreach (var l in line) yield return l;
                }
                if (!any) yield return "(sem bobina/chamada)";
            }

            private static int Num(string uid) { int v; return int.TryParse(uid, out v) ? v : 0; }

            private IEnumerable<string> Statement(string uid, XElement part)
            {
                var kind = part.Name.LocalName == "Call" ? "Call" : (string)part.Attribute("Name");
                var sinks = new[] { "Coil", "SCoil", "RCoil", "Move", "Call" };
                if (!sinks.Contains(kind)) return null; // contatos/comparadores aparecem dentro da expressão
                var cond = Expr(uid, kind == "Coil" || kind == "SCoil" || kind == "RCoil" ? "in" : "en", 0);
                switch (kind)
                {
                    case "Coil": return One(Operand(uid, "operand") + " := " + (cond ?? "TRUE"));
                    case "SCoil": return One("SET " + Operand(uid, "operand") + " IF " + (cond ?? "TRUE"));
                    case "RCoil": return One("RESET " + Operand(uid, "operand") + " IF " + (cond ?? "TRUE"));
                    case "Move":
                        return One((cond == null ? "" : "IF " + cond + " THEN ")
                            + Operand(uid, "out1") + " := " + (Source(uid, "in", 0) ?? "?"));
                    default: return CallLines(uid, part, cond);
                }
            }

            private static IEnumerable<string> One(string s) { yield return s; }

            private IEnumerable<string> CallLines(string uid, XElement call, string cond)
            {
                var info = call.Elements().FirstOrDefault(e => e.Name.LocalName == "CallInfo");
                var inst = info == null ? null : info.Elements().FirstOrDefault(e => e.Name.LocalName == "Instance");
                var instUid = inst == null ? null : (string)inst.Attribute("UId");
                yield return "CALL " + (info == null ? "?" : (string)info.Attribute("BlockType") + " \"" + (string)info.Attribute("Name") + "\"")
                    + (instUid != null && _access.ContainsKey(instUid) ? " inst " + _access[instUid] : "")
                    + (cond == null ? "" : " IF " + cond);
                foreach (var p in info == null ? Enumerable.Empty<XElement>() : info.Elements().Where(e => e.Name.LocalName == "Parameter"))
                {
                    var pin = (string)p.Attribute("Name");
                    var sec = (string)p.Attribute("Section");
                    if (sec == "Output")
                    {
                        var dst = Operand(uid, pin);
                        if (dst != null) yield return "  " + pin + " => " + dst;
                    }
                    else
                    {
                        var src = Source(uid, pin, 0);
                        if (src != null) yield return "  " + pin + " := " + src;
                    }
                }
            }

            private XElement WireOf(string uid, string pin)
            {
                return _wires.FirstOrDefault(w => w.Elements().Any(c => c.Name.LocalName == "NameCon"
                    && (string)c.Attribute("UId") == uid && (string)c.Attribute("Name") == pin));
            }

            /// <summary>Tag ligada direto ao pino (IdentCon), sem lógica no meio.</summary>
            private string Operand(string uid, string pin)
            {
                var w = WireOf(uid, pin);
                var id = w == null ? null : w.Elements().FirstOrDefault(c => c.Name.LocalName == "IdentCon");
                string label;
                return id != null && _access.TryGetValue((string)id.Attribute("UId"), out label) ? label : null;
            }

            /// <summary>Valor de um pino de entrada: tag direta ou expressão da lógica que o alimenta.</summary>
            private string Source(string uid, string pin, int depth)
            {
                return Operand(uid, pin) ?? Expr(uid, pin, depth);
            }

            /// <summary>Lógica que chega no pino: paralelo = OR, série = AND. null = barramento (sempre verdadeiro).</summary>
            private string Expr(string uid, string pin, int depth)
            {
                if (depth > 40) return "…"; // ponytail: guarda contra XML ciclíco; LAD real não passa disso
                var w = WireOf(uid, pin);
                if (w == null) return null;
                if (w.Elements().Any(c => c.Name.LocalName == "Powerrail")) return null;
                var srcs = w.Elements().Where(c => c.Name.LocalName == "NameCon"
                        && !((string)c.Attribute("UId") == uid && (string)c.Attribute("Name") == pin)
                        && IsOutput((string)c.Attribute("UId"), (string)c.Attribute("Name")))
                    .Select(c => PartExpr((string)c.Attribute("UId"), (string)c.Attribute("Name"), depth + 1))
                    .Where(s => s != null).ToList();
                if (srcs.Count == 0) return null;
                return srcs.Count == 1 ? srcs[0] : "(" + string.Join(" OR ", srcs) + ")";
            }

            private bool IsOutput(string uid, string pin)
            {
                XElement p;
                if (uid == null || !_parts.TryGetValue(uid, out p)) return false;
                if (p.Name.LocalName == "Call")
                    return pin == "eno" || p.Descendants().Any(e => e.Name.LocalName == "Parameter"
                        && (string)e.Attribute("Name") == pin && (string)e.Attribute("Section") == "Output");
                return pin == "out" || pin == "eno" || pin == "out1" || pin == "Q";
            }

            /// <summary>Expressão que a parte entrega na sua saída, já com o que vem antes dela em série.</summary>
            private string PartExpr(string uid, string outPin, int depth)
            {
                XElement p;
                if (!_parts.TryGetValue(uid, out p)) return null;
                var kind = p.Name.LocalName == "Call" ? "Call" : (string)p.Attribute("Name");
                bool neg = p.Elements().Any(e => e.Name.LocalName == "Negated");
                string self;
                switch (kind)
                {
                    case "Contact":
                        self = (neg ? "NOT " : "") + (Operand(uid, "operand") ?? "?");
                        break;
                    case "Eq": case "Ne": case "Gt": case "Ge": case "Lt": case "Le":
                        var op = new Dictionary<string, string> { { "Eq", "=" }, { "Ne", "<>" }, { "Gt", ">" },
                            { "Ge", ">=" }, { "Lt", "<" }, { "Le", "<=" } }[kind];
                        // export real usa in1/in2 e 'pre' para a série; LadConverter emite operand1/operand2
                        self = (Operand(uid, "in1") ?? Operand(uid, "operand1") ?? "?") + " " + op + " "
                            + (Operand(uid, "in2") ?? Operand(uid, "operand2") ?? "?");
                        return Combine(Expr(uid, "pre", depth) ?? Expr(uid, "in", depth), self);
                    case "O":
                        return Group(uid, " OR ", depth);
                    case "A":
                        return Group(uid, " AND ", depth);
                    case "Call":
                        return "\"" + (string)p.Descendants().First(e => e.Name.LocalName == "CallInfo").Attribute("Name") + "\"." + outPin;
                    default:
                        self = kind + "(" + (Operand(uid, "operand") ?? "") + ")";
                        break;
                }
                return Combine(Expr(uid, "in", depth), self);
            }

            /// <summary>Série: o que vem antes AND a própria parte. null antes = barramento.</summary>
            private static string Combine(string pre, string self)
            {
                return pre == null ? self : pre + " AND " + self;
            }

            /// <summary>Caixa O/A: cada pino in1..inN é um ramo; barramento no pino = TRUE.</summary>
            private string Group(string uid, string sep, int depth)
            {
                var pins = _wires.SelectMany(w => w.Elements().Where(c => c.Name.LocalName == "NameCon"
                        && (string)c.Attribute("UId") == uid
                        && ((string)c.Attribute("Name")).StartsWith("in")))
                    .Select(c => (string)c.Attribute("Name")).Distinct()
                    .OrderBy(x => x, StringComparer.Ordinal).ToList();
                var branches = pins.Select(pin => Source(uid, pin, depth) ?? "TRUE").ToList();
                return branches.Count == 0 ? null : "(" + string.Join(sep, branches) + ")";
            }

            // ---------- rótulo do operando ----------

            private static string Label(XElement access)
            {
                var sym = access.Descendants().Where(e => e.Name.LocalName == "Component")
                    .Select(c => (string)c.Attribute("Name")).ToList();
                if (sym.Count > 0)
                    return "\"" + sym[0] + "\"" + string.Concat(sym.Skip(1).Select(s => "." + s));
                var val = access.Descendants().FirstOrDefault(e => e.Name.LocalName == "ConstantValue");
                if (val != null) return val.Value;
                var named = access.Descendants().FirstOrDefault(e => e.Name.LocalName == "Constant"
                    && e.Attribute("Name") != null);
                if (named != null) return "\"" + (string)named.Attribute("Name") + "\"";
                var addr = access.Descendants().FirstOrDefault(e => e.Name.LocalName == "Address");
                if (addr != null) return "%" + (string)addr.Attribute("Area") + (string)addr.Attribute("BitOffset");
                return (string)access.Attribute("Scope") ?? "?";
            }
        }
    }
}
