// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L46    class LadConverter
//   L48    .Convert
//   L91    lexer
//   L93    class Tok
//   L99    .Lex
//   L143   AST
//   L145   class Node
//   L146   class Leaf
//   L147   class CmpN
//   L148   class Group
//   L149   class NotN
//   L150   class Operand
//   L153   class Stmt
//   L161   parser (recursive descent)
//   L163   class Parser
//   L304   normalize: push NOT down to leaves (De Morgan; comparators invert)
//   L306   .Normalize
//   L321   FlgNet emitter
//   L327   class Net
//   L334   class Emitter
//   L450   .EmitNetwork
//   L476   block XML assembly
//   L478   .BuildBlockXml
//   L516   .Hex
//   L518   .Mlt
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Tia.Core
{
    /// <summary>
    /// SCL (subset booleano) → bloco LAD (SimaticML FlgNet) pronto para import-block.
    /// Sem tipos Siemens.Engineering de propósito: a geração (dry-run) roda sem TIA instalado.
    /// Suportado: atribuição booleana (AND/OR/NOT/parênteses), comparadores (= &lt;&gt; &lt; &gt; &lt;= &gt;=),
    /// IF cond THEN x := TRUE|FALSE|literal; END_IF (Set/Reset/MOVE), IF/ELSE TRUE/FALSE (Coil).
    /// Rejeitado com erro claro: FOR/WHILE/REPEAT/CASE, aritmética, #locais (sem equivalente LAD → use import-source).
    /// </summary>
    public static class LadConverter
    {
        public static Dictionary<string, object> Convert(string sclFile, string blockName, string outDir)
        {
            var full = Path.GetFullPath(sclFile);
            if (!File.Exists(full)) throw new FileNotFoundException("Source file not found: " + full);
            var text = File.ReadAllText(full);

            var header = Regex.Match(text, @"FUNCTION(?:_BLOCK)?\s+""([^""]+)""");
            var name = blockName ?? (header.Success ? header.Groups[1].Value
                : Path.GetFileNameWithoutExtension(full));
            if (header.Success && text.IndexOf("FUNCTION_BLOCK", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("FUNCTION_BLOCK not supported (needs interface/instance) — use FC or import-source.");

            var body = text;
            var begin = Regex.Match(text, @"\bBEGIN\b", RegexOptions.IgnoreCase);
            if (begin.Success)
            {
                var end = Regex.Match(text, @"\bEND_FUNCTION\b", RegexOptions.IgnoreCase);
                body = text.Substring(begin.Index + begin.Length,
                    (end.Success ? end.Index : text.Length) - begin.Index - begin.Length);
            }
            else if (Regex.IsMatch(text, @"\bVAR(_INPUT|_OUTPUT|_TEMP)?\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException("VAR sections without BEGIN — malformed source.");

            var stmts = new Parser(Lex(body)).ParseAll();
            if (stmts.Count == 0) throw new InvalidOperationException("No statements found in source body.");

            var xml = BuildBlockXml(name, stmts);
            Directory.CreateDirectory(outDir);
            var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var xmlFile = Path.GetFullPath(Path.Combine(outDir, safe + ".xml"));
            if (File.Exists(xmlFile)) File.Delete(xmlFile);
            xml.Save(xmlFile);

            return new Dictionary<string, object>
            {
                { "block", name },
                { "language", "LAD" },
                { "networks", stmts.Count },
                { "coils", stmts.Select(s => s.Target).ToList() },
                { "xmlFile", xmlFile },
            };
        }

        // ---------- lexer ----------

        private class Tok
        {
            public string Kind; // id | tag | num | sym
            public string Text;
        }

        private static List<Tok> Lex(string s)
        {
            s = Regex.Replace(s, @"//[^\n]*|\(\*.*?\*\)", " ", RegexOptions.Singleline);
            var syms = new[] { ":=", "<=", ">=", "<>", "(", ")", ";", "<", ">", "=" };
            var toks = new List<Tok>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '"')
                {
                    int j = s.IndexOf('"', i + 1);
                    if (j < 0) throw new InvalidOperationException("Unterminated tag quote.");
                    toks.Add(new Tok { Kind = "tag", Text = s.Substring(i + 1, j - i - 1) });
                    i = j + 1; continue;
                }
                if (c == '#')
                    throw new InvalidOperationException("Local variables (#) not supported in LAD conversion — use global tags or import-source.");
                if (char.IsDigit(c))
                {
                    int j = i; while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '.')) j++;
                    toks.Add(new Tok { Kind = "num", Text = s.Substring(i, j - i) });
                    i = j; continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int j = i; while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_')) j++;
                    var word = s.Substring(i, j - i);
                    // unquoted identifier that is not a keyword = global tag written without quotes
                    var up = word.ToUpperInvariant();
                    var keywords = new[] { "IF", "THEN", "ELSE", "END_IF", "AND", "OR", "NOT", "TRUE", "FALSE",
                        "FOR", "WHILE", "REPEAT", "CASE", "XOR", "MOD" };
                    toks.Add(new Tok { Kind = keywords.Contains(up) ? "id" : "tag", Text = keywords.Contains(up) ? up : word });
                    i = j; continue;
                }
                var sym = syms.FirstOrDefault(x => string.CompareOrdinal(s, i, x, 0, x.Length) == 0);
                if (sym == null) throw new InvalidOperationException("Unexpected character '" + c + "' in source.");
                toks.Add(new Tok { Kind = "sym", Text = sym });
                i += sym.Length;
            }
            return toks;
        }

        // ---------- AST ----------

        private abstract class Node { }
        private class Leaf : Node { public string Tag; public bool Neg; }
        private class CmpN : Node { public string Op; public Operand L; public Operand R; }
        private class Group : Node { public bool IsAnd; public List<Node> Kids = new List<Node>(); }
        private class NotN : Node { public Node Kid; }
        private class Operand { public string Text; public bool IsTag; }

        /// <summary>One network: coil (Coil/SCoil/RCoil) or MOVE of a literal.</summary>
        private class Stmt
        {
            public string Target;
            public string CoilKind;     // Coil | SCoil | RCoil | null (=MOVE)
            public Node Cond;           // null only for unconditional MOVE
            public Operand MoveValue;   // set when CoilKind == null
        }

        // ---------- parser (recursive descent) ----------

        private class Parser
        {
            private readonly List<Tok> _t;
            private int _p;
            public Parser(List<Tok> toks) { _t = toks; }

            private Tok Peek() { return _p < _t.Count ? _t[_p] : null; }
            private Tok Next() { return _p < _t.Count ? _t[_p++] : null; }

            private void Expect(string kind, string text)
            {
                var t = Next();
                if (t == null || t.Kind != kind || t.Text != text)
                    throw new InvalidOperationException("Expected '" + text + "' but got '" + (t == null ? "<eof>" : t.Text) + "'.");
            }

            public List<Stmt> ParseAll()
            {
                var stmts = new List<Stmt>();
                while (Peek() != null) stmts.Add(ParseStmt());
                return stmts;
            }

            private Stmt ParseStmt()
            {
                var t = Peek();
                if (t.Kind == "id" && (t.Text == "FOR" || t.Text == "WHILE" || t.Text == "REPEAT" || t.Text == "CASE"))
                    throw new InvalidOperationException(t.Text + " has no LAD equivalent — keep this block in SCL (import-source).");
                if (t.Kind == "id" && t.Text == "IF") return ParseIf();
                if (t.Kind == "tag") return ParseAssign(null);
                throw new InvalidOperationException("Unexpected token '" + t.Text + "' at statement start.");
            }

            /// <summary>IF cond THEN x := TRUE|FALSE|num; [ELSE x := TRUE|FALSE;] END_IF;</summary>
            private Stmt ParseIf()
            {
                Expect("id", "IF");
                var cond = ParseExpr();
                Expect("id", "THEN");
                var target = Next();
                if (target == null || target.Kind != "tag") throw new InvalidOperationException("Expected tag after THEN.");
                Expect("sym", ":=");
                var val = Next();
                Expect("sym", ";");

                string thenKind =
                    val.Kind == "id" && val.Text == "TRUE" ? "SCoil" :
                    val.Kind == "id" && val.Text == "FALSE" ? "RCoil" :
                    val.Kind == "num" ? null :
                    ThrowBadIfValue(val);

                var stmt = new Stmt { Target = target.Text, Cond = cond, CoilKind = thenKind };
                if (thenKind == null) stmt.MoveValue = new Operand { Text = val.Text, IsTag = false };

                if (Peek() != null && Peek().Kind == "id" && Peek().Text == "ELSE")
                {
                    Next();
                    var t2 = Next(); Expect("sym", ":="); var v2 = Next(); Expect("sym", ";");
                    bool coilPattern = thenKind == "SCoil" && t2 != null && t2.Kind == "tag" && t2.Text == stmt.Target
                        && v2 != null && v2.Kind == "id" && v2.Text == "FALSE";
                    if (!coilPattern)
                        throw new InvalidOperationException("Only 'ELSE same_tag := FALSE;' supported (becomes a plain coil).");
                    stmt.CoilKind = "Coil";
                }
                Expect("id", "END_IF");
                if (Peek() != null && Peek().Kind == "sym" && Peek().Text == ";") Next();
                return stmt;
            }

            private string ThrowBadIfValue(Tok val)
            {
                throw new InvalidOperationException("IF body must assign TRUE, FALSE or a numeric literal (got '"
                    + (val == null ? "<eof>" : val.Text) + "').");
            }

            private Stmt ParseAssign(Node cond)
            {
                var target = Next();
                Expect("sym", ":=");
                var next = Peek();
                if (next != null && next.Kind == "num") // literal → MOVE
                {
                    var v = Next(); Expect("sym", ";");
                    return new Stmt { Target = target.Text, Cond = cond, MoveValue = new Operand { Text = v.Text, IsTag = false } };
                }
                var expr = ParseExpr();
                Expect("sym", ";");
                return new Stmt { Target = target.Text, Cond = expr, CoilKind = "Coil" };
            }

            private Node ParseExpr() // OR level
            {
                var kids = new List<Node> { ParseAnd() };
                while (Peek() != null && Peek().Kind == "id" && Peek().Text == "OR") { Next(); kids.Add(ParseAnd()); }
                return kids.Count == 1 ? kids[0] : new Group { IsAnd = false, Kids = kids };
            }

            private Node ParseAnd()
            {
                var kids = new List<Node> { ParseUnary() };
                while (Peek() != null && Peek().Kind == "id" && Peek().Text == "AND") { Next(); kids.Add(ParseUnary()); }
                return kids.Count == 1 ? kids[0] : new Group { IsAnd = true, Kids = kids };
            }

            private Node ParseUnary()
            {
                var t = Peek();
                if (t != null && t.Kind == "id" && t.Text == "NOT") { Next(); return new NotN { Kid = ParseUnary() }; }
                if (t != null && t.Kind == "id" && (t.Text == "XOR" || t.Text == "MOD"))
                    throw new InvalidOperationException(t.Text + " not supported in LAD conversion.");
                if (t != null && t.Kind == "sym" && t.Text == "(")
                {
                    Next();
                    var e = ParseExpr();
                    Expect("sym", ")");
                    return e;
                }
                return ParsePrimary();
            }

            private Node ParsePrimary()
            {
                var t = Next();
                if (t == null || (t.Kind != "tag" && t.Kind != "num"))
                    throw new InvalidOperationException("Expected tag or number, got '" + (t == null ? "<eof>" : t.Text) + "'.");
                var left = new Operand { Text = t.Text, IsTag = t.Kind == "tag" };
                var op = Peek();
                var cmpOps = new[] { "=", "<>", "<", ">", "<=", ">=" };
                if (op != null && op.Kind == "sym" && cmpOps.Contains(op.Text))
                {
                    Next();
                    var r = Next();
                    if (r == null || (r.Kind != "tag" && r.Kind != "num"))
                        throw new InvalidOperationException("Expected tag or number after comparator.");
                    return new CmpN { Op = op.Text, L = left, R = new Operand { Text = r.Text, IsTag = r.Kind == "tag" } };
                }
                if (!left.IsTag) throw new InvalidOperationException("Bare numeric literal is not a boolean expression.");
                return new Leaf { Tag = left.Text };
            }
        }

        // ---------- normalize: push NOT down to leaves (De Morgan; comparators invert) ----------

        private static Node Normalize(Node n, bool neg)
        {
            if (n is NotN) return Normalize(((NotN)n).Kid, !neg);
            if (n is Leaf) { var l = (Leaf)n; return new Leaf { Tag = l.Tag, Neg = neg ^ l.Neg }; }
            if (n is CmpN)
            {
                var c = (CmpN)n;
                var inv = new Dictionary<string, string> { { "=", "<>" }, { "<>", "=" },
                    { "<", ">=" }, { ">=", "<" }, { ">", "<=" }, { "<=", ">" } };
                return new CmpN { Op = neg ? inv[c.Op] : c.Op, L = c.L, R = c.R };
            }
            var g = (Group)n;
            return new Group { IsAnd = g.IsAnd ^ neg, Kids = g.Kids.Select(k => Normalize(k, neg)).ToList() };
        }

        // ---------- FlgNet emitter ----------

        private static readonly XNamespace FlgNs = "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v4";
        private static readonly XNamespace IfaceNs = "http://www.siemens.com/automation/Openness/SW/Interface/v5";

        /// <summary>Pins on the same electric net; one Wire per net (sources first, sinks after).</summary>
        private class Net
        {
            public bool Rail;
            public List<Tuple<int, string>> Sources = new List<Tuple<int, string>>();
            public List<Tuple<int, string>> Sinks = new List<Tuple<int, string>>();
        }

        private class Emitter
        {
            private int _uid = 21; // Openness exports start part UIds at 21 per network
            public List<XElement> Parts = new List<XElement>();
            public List<XElement> OperandWires = new List<XElement>();
            public List<Net> Nets = new List<Net>();

            public int NextUid() { return _uid++; }

            public int TagAccess(string tag)
            {
                int uid = NextUid();
                Parts.Add(new XElement(FlgNs + "Access", new XAttribute("Scope", "GlobalVariable"), new XAttribute("UId", uid),
                    new XElement(FlgNs + "Symbol", new XElement(FlgNs + "Component", new XAttribute("Name", tag)))));
                return uid;
            }

            public int ConstAccess(string value)
            {
                int uid = NextUid();
                Parts.Add(new XElement(FlgNs + "Access", new XAttribute("Scope", "LiteralConstant"), new XAttribute("UId", uid),
                    new XElement(FlgNs + "Constant",
                        new XElement(FlgNs + "ConstantType", value.Contains(".") ? "Real" : "DInt"),
                        new XElement(FlgNs + "ConstantValue", value))));
                return uid;
            }

            public void Operand(int accessUid, int partUid, string port)
            {
                OperandWires.Add(new XElement(FlgNs + "Wire", new XAttribute("UId", 0), // renumbered at the end
                    new XElement(FlgNs + "IdentCon", new XAttribute("UId", accessUid)),
                    new XElement(FlgNs + "NameCon", new XAttribute("UId", partUid), new XAttribute("Name", port))));
            }

            public Net NewNet() { var n = new Net(); Nets.Add(n); return n; }

            public void Compile(Node n, Net inNet, Net outNet)
            {
                if (n is Leaf)
                {
                    var l = (Leaf)n;
                    int c = NextUid();
                    var part = new XElement(FlgNs + "Part", new XAttribute("Name", "Contact"), new XAttribute("UId", c));
                    if (l.Neg) part.Add(new XElement(FlgNs + "Negated", new XAttribute("Name", "operand")));
                    Parts.Add(part);
                    Operand(TagAccess(l.Tag), c, "operand");
                    inNet.Sinks.Add(Tuple.Create(c, "in"));
                    outNet.Sources.Add(Tuple.Create(c, "out"));
                    return;
                }
                if (n is CmpN)
                {
                    var cmp = (CmpN)n;
                    var partName = new Dictionary<string, string> { { "=", "Eq" }, { "<>", "Ne" },
                        { ">", "Gt" }, { ">=", "Ge" }, { "<", "Lt" }, { "<=", "Le" } }[cmp.Op];
                    var srcType = !cmp.R.IsTag && cmp.R.Text.Contains(".") || !cmp.L.IsTag && cmp.L.Text.Contains(".")
                        ? "Real" : "DInt"; // ponytail: tag types unknown offline; DInt default, smoke will confirm
                    int c = NextUid();
                    Parts.Add(new XElement(FlgNs + "Part", new XAttribute("Name", partName), new XAttribute("UId", c),
                        new XElement(FlgNs + "TemplateValue", new XAttribute("Name", "SrcType"), new XAttribute("Type", "Type"), srcType)));
                    // pinos conforme export real (docs/examples/BombaTemplateFc.xml:1044-1058): pre/in1/in2/out
                    Operand(cmp.L.IsTag ? TagAccess(cmp.L.Text) : ConstAccess(cmp.L.Text), c, "in1");
                    Operand(cmp.R.IsTag ? TagAccess(cmp.R.Text) : ConstAccess(cmp.R.Text), c, "in2");
                    inNet.Sinks.Add(Tuple.Create(c, "pre"));
                    outNet.Sources.Add(Tuple.Create(c, "out"));
                    return;
                }
                var g = (Group)n;
                if (g.IsAnd)
                {
                    var cur = inNet;
                    for (int i = 0; i < g.Kids.Count; i++)
                    {
                        var next = i == g.Kids.Count - 1 ? outNet : NewNet();
                        Compile(g.Kids[i], cur, next);
                        cur = next;
                    }
                }
                else if (g.Kids.Count == 1)
                    Compile(g.Kids[0], inNet, outNet);
                else
                {
                    // paralelo = parte "O" explícita (BombaTemplateFc.xml:346 + wires 391-404): cada ramo
                    // entra num pino in1..inN próprio. Juntar dois "out" no mesmo fio o import recusa.
                    int o = NextUid();
                    Parts.Add(new XElement(FlgNs + "Part", new XAttribute("Name", "O"), new XAttribute("UId", o),
                        new XElement(FlgNs + "TemplateValue", new XAttribute("Name", "Card"),
                            new XAttribute("Type", "Cardinality"), g.Kids.Count.ToString())));
                    for (int i = 0; i < g.Kids.Count; i++)
                    {
                        var branch = NewNet();
                        branch.Sinks.Add(Tuple.Create(o, "in" + (i + 1)));
                        Compile(g.Kids[i], inNet, branch);
                    }
                    outNet.Sources.Add(Tuple.Create(o, "out"));
                }
            }

            public XElement ToFlgNet()
            {
                var wires = new List<XElement>(OperandWires);
                foreach (var net in Nets)
                {
                    var w = new XElement(FlgNs + "Wire", new XAttribute("UId", 0));
                    if (net.Rail) w.Add(new XElement(FlgNs + "Powerrail"));
                    foreach (var p in net.Sources.Concat(net.Sinks))
                        w.Add(new XElement(FlgNs + "NameCon", new XAttribute("UId", p.Item1), new XAttribute("Name", p.Item2)));
                    wires.Add(w);
                }
                foreach (var w in wires) w.Attribute("UId").Value = NextUid().ToString();
                return new XElement(FlgNs + "FlgNet",
                    new XElement(FlgNs + "Parts", Parts),
                    new XElement(FlgNs + "Wires", wires));
            }
        }

        private static XElement EmitNetwork(Stmt s)
        {
            var em = new Emitter();
            var rail = em.NewNet(); rail.Rail = true;
            var end = s.Cond != null ? em.NewNet() : rail;
            if (s.Cond != null) em.Compile(Normalize(s.Cond, false), rail, end);

            if (s.CoilKind != null)
            {
                int coil = em.NextUid();
                em.Parts.Add(new XElement(FlgNs + "Part", new XAttribute("Name", s.CoilKind), new XAttribute("UId", coil)));
                em.Operand(em.TagAccess(s.Target), coil, "operand");
                end.Sinks.Add(Tuple.Create(coil, "in"));
            }
            else // MOVE literal → target
            {
                int mv = em.NextUid();
                em.Parts.Add(new XElement(FlgNs + "Part", new XAttribute("Name", "Move"), new XAttribute("UId", mv),
                    new XElement(FlgNs + "TemplateValue", new XAttribute("Name", "Card"), new XAttribute("Type", "Cardinality"), "1")));
                em.Operand(em.ConstAccess(s.MoveValue.Text), mv, "in");
                em.Operand(em.TagAccess(s.Target), mv, "out1");
                end.Sinks.Add(Tuple.Create(mv, "en"));
            }
            return em.ToFlgNet();
        }

        // ---------- block XML assembly ----------

        private static XDocument BuildBlockXml(string name, List<Stmt> stmts)
        {
            int id = 1;
            var objects = new List<XElement> { Mlt("Comment", "", ref id) };
            int net = 0;
            foreach (var s in stmts)
            {
                net++;
                objects.Add(new XElement("SW.Blocks.CompileUnit",
                    new XAttribute("ID", Hex(ref id)), new XAttribute("CompositionName", "CompileUnits"),
                    new XElement("AttributeList",
                        new XElement("NetworkSource", EmitNetwork(s)),
                        new XElement("ProgrammingLanguage", "LAD")),
                    new XElement("ObjectList",
                        Mlt("Comment", "", ref id),
                        Mlt("Title", "Rede " + net + " — " + s.Target, ref id))));
            }
            objects.Add(Mlt("Title", "", ref id));

            var iface = new XElement("Interface", new XElement(IfaceNs + "Sections",
                new[] { "Input", "Output", "InOut", "Temp", "Constant" }
                    .Select(sec => new XElement(IfaceNs + "Section", new XAttribute("Name", sec)))
                    .Concat(new[] { new XElement(IfaceNs + "Section", new XAttribute("Name", "Return"),
                        new XElement(IfaceNs + "Member", new XAttribute("Name", "Ret_Val"), new XAttribute("Datatype", "Void"))) })));

            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Document",
                    new XElement("Engineering", new XAttribute("version", "V19")),
                    new XElement("SW.Blocks.FC", new XAttribute("ID", "0"),
                        new XElement("AttributeList",
                            iface,
                            new XElement("Name", name),
                            new XElement("Namespace"),
                            new XElement("ProgrammingLanguage", "LAD")),
                        new XElement("ObjectList", objects))));
        }

        private static string Hex(ref int id) { return (id++).ToString("X"); }

        private static XElement Mlt(string composition, string text, ref int id)
        {
            return new XElement("MultilingualText",
                new XAttribute("ID", Hex(ref id)), new XAttribute("CompositionName", composition),
                new XElement("ObjectList",
                    new XElement("MultilingualTextItem",
                        new XAttribute("ID", Hex(ref id)), new XAttribute("CompositionName", "Items"),
                        new XElement("AttributeList",
                            new XElement("Culture", "en-US"),
                            new XElement("Text", text)))));
        }
    }
}
