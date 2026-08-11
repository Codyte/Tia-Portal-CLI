// NAV INDEX
//   1-40     usings, namespace, constantes
//   42-96    BlockEdit.DeleteNetwork — verbo delete-network
//   98-170   AddCall — verbo add-call (interface do FB vira os pinos)
//  172-214   SetRetain — verbo set-retain (Remanence na declaração do FB)
//  216-250   Patch — coreografia comum: export → patch → import com prova
//  252-330   núcleo puro: RemoveNetworkFromXml, InsertCallInXml, SetRetainInXml
//  332-380   helpers de FlgNet: Access, Wire, Escape, NextId
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Tia.Core
{
    /// <summary>
    /// Cirurgia em bloco LAD/FBD por XML: apagar rede, inserir chamada de FB, marcar retentividade.
    /// O Openness não expõe nenhuma das três como API — e sem elas a R8 da lei de construção
    /// ("chamada em LAD") só era alcançável escrevendo FlgNet na mão, que foi o maior custo da FP-03
    /// (docs/teste-cego/resultado-FP-03.md §5.2). Núcleos puros no fim do arquivo: rodam sem TIA.
    /// </summary>
    public static class BlockEdit
    {
        /// <summary>Namespace do FlgNet — o export usa v5 e o import recusa outro.</summary>
        private const string FlgNetNs = "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v5";

        /// <summary>Constante em vez de símbolo: T#3S, 100, 1.5, TRUE, 'A'.</summary>
        private static readonly Regex ConstantValue =
            new Regex(@"^(-?\d|TRUE$|FALSE$|'|[A-Za-z0-9_]+#)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ---------- delete-network ----------

        /// <summary>
        /// Apaga a rede de índice N (1-based, a mesma numeração do `explain-block`). Sem isso,
        /// trocar duas redes de um molde exigia editar o XML fora do CLI.
        /// </summary>
        public static object DeleteNetwork(PlcSoftware plc, string blockName, int index,
            string outDir, bool apply)
        {
            string removed = null;
            int before = 0;
            var result = Patch(plc, blockName, "delnet_", outDir, apply,
                doc => { before = CountNetworks(doc); removed = RemoveNetworkFromXml(doc, index); },
                () => "a remoção da rede " + index,
                doc => CountNetworks(doc) == before - 1);
            result["network"] = index;
            result["title"] = removed;
            return result;
        }

        // ---------- add-call ----------

        /// <summary>
        /// Insere uma rede com a chamada de um FB (EN direto do powerrail). Os pinos saem da
        /// interface do FB — daí o <see cref="BlockInterface"/> ser o mesmo leitor do
        /// `list-interface`. Input e InOut sem valor são erro: pino de entrada solto não compila.
        /// ponytail: rede incondicional (sem contatos em série). Condição continua sendo cirurgia
        /// manual ou clone de um molde que já a tenha.
        /// </summary>
        public static object AddCall(PlcSoftware plc, string blockName, string fbName, string instance,
            IEnumerable<string> paramArgs, int after, string title, string comment,
            string outDir, bool apply)
        {
            var fb = Ops.FindBlock(plc, fbName) as FB;
            if (fb == null)
            {
                var near = Ops.FbsLike(plc, fbName);
                if (near.Count != 1)
                    throw new InvalidOperationException("FB '" + fbName + "' not found.");
                fb = near[0];
            }
            Directory.CreateDirectory(outDir);
            var ifaceFile = Path.GetFullPath(Path.Combine(outDir, "iface_" + Safe(fb.Name) + ".xml"));
            if (File.Exists(ifaceFile)) File.Delete(ifaceFile);
            fb.Export(new FileInfo(ifaceFile), ExportOptions.None);
            var iface = BlockInterface.FromXml(XDocument.Load(ifaceFile));
            if (iface.Count == 0)
                throw new InvalidOperationException("FB '" + fb.Name + "' has no Input/Output/InOut.");

            var values = ParseParams(paramArgs);
            var unknown = values.Keys.Where(k => !iface.Any(p => p.Name == k)).ToList();
            if (unknown.Count > 0)
                throw new ArgumentException("Parâmetro inexistente em '" + fb.Name + "': "
                    + string.Join(", ", unknown) + ". Pinos: " + string.Join(", ", iface.Select(p => p.Name)));
            var missing = iface.Where(p => p.Section != "Output" && !values.ContainsKey(p.Name))
                .Select(p => p.Name + " : " + p.Datatype).ToList();
            if (missing.Count > 0)
                throw new ArgumentException("Pino de entrada sem valor (não compila): "
                    + string.Join(", ", missing));

            var spec = new CallSpec
            {
                Fb = fb.Name,
                Instance = instance,
                Title = title ?? ("Function Block " + Regex.Replace(fb.Name, @"^FB\s+", "")),
                Comment = comment,
                Params = iface,
                Values = values,
            };

            int after1 = 0;
            var result = Patch(plc, blockName, "addcall_", outDir, apply,
                doc => { after1 = CountNetworks(doc) + 1; InsertCallInXml(doc, spec, after); },
                () => "a chamada de '" + fb.Name + "'",
                doc => doc.Descendants().Any(e => e.Name.LocalName == "CallInfo"
                    && (string)e.Attribute("Name") == fb.Name));
            result["fb"] = fb.Name;
            result["instance"] = instance;
            result["parameters"] = iface.Count;
            result["networks"] = after1;
            return result;
        }

        // ---------- set-retain ----------

        /// <summary>
        /// Marca (ou desmarca) `Remanence` de um membro Static do FB. O atributo NÃO pode ser setado
        /// no iDB — só na declaração do bloco — e `import-source` não expressa retentividade, então
        /// entregar um horímetro retentivo obrigava a exportar, editar o XML na mão e reimportar
        /// (FP-03, tropeços 3 e 4).
        /// </summary>
        public static object SetRetain(PlcSoftware plc, string blockName, string member, bool retain,
            string outDir, bool apply)
        {
            var block = Ops.FindBlock(plc, blockName);
            if (block == null)
                throw new InvalidOperationException("Block '" + blockName + "' not found.");
            if (block is InstanceDB)
                throw new InvalidOperationException("'" + block.Name + "' é um iDB: o Openness recusa "
                    + "`Remanence` em instância (\"The attribute 'Remanence' cannot be set\"). "
                    + "Retentividade se declara no FB — passe --block <FB>.");

            string was = null;
            var result = Patch(plc, blockName, "retain_", outDir, apply,
                doc => { was = SetRetainInXml(doc, member, retain); },
                () => "a retentividade de '" + member + "'",
                doc => RetainOf(doc, member) == (retain ? "Retain" : "NonRetain"));
            result["member"] = member;
            result["was"] = was;
            result["now"] = retain ? "Retain" : "NonRetain";
            return result;
        }

        // ---------- coreografia ----------

        /// <summary>
        /// export → patch → Import Override com prova (<see cref="Ops.ImportAndProve"/>). O bloco
        /// volta para a MESMA pasta: o grupo sai do `Parent` antes do import, que descarta o objeto.
        /// </summary>
        private static Dictionary<string, object> Patch(PlcSoftware plc, string blockName, string prefix,
            string outDir, bool apply, Action<XDocument> patch, Func<string> what,
            Func<XDocument, bool> proof)
        {
            var block = Ops.FindBlock(plc, blockName);
            if (block == null)
                throw new InvalidOperationException("Block '" + blockName + "' not found.");
            var label = block.Name;
            var group = block.Parent as PlcBlockGroup ?? plc.BlockGroup;

            Directory.CreateDirectory(outDir);
            var file = Path.GetFullPath(Path.Combine(outDir, prefix + Safe(label) + ".xml"));
            if (File.Exists(file)) File.Delete(file);
            block.Export(new FileInfo(file), ExportOptions.WithDefaults);

            var doc = XDocument.Load(file);
            patch(doc);
            doc.Save(file);

            if (apply) Ops.ImportAndProve(plc, group, label, file, what(), proof);
            return new Dictionary<string, object>
            {
                { "block", label }, { "file", file }, { "applied", apply },
            };
        }

        // ---------- núcleo puro (sem Openness: testável offline) ----------

        public sealed class CallSpec
        {
            public string Fb;
            public string Instance;
            public string Title;
            public string Comment;
            public List<Param> Params;
            public Dictionary<string, string> Values;
        }

        internal static int CountNetworks(XDocument doc)
        {
            return doc.Descendants().Count(e => e.Name.LocalName == "SW.Blocks.CompileUnit");
        }

        /// <summary>Tira a rede de índice N (1-based) e devolve o título dela.</summary>
        internal static string RemoveNetworkFromXml(XDocument doc, int index)
        {
            var units = doc.Descendants().Where(e => e.Name.LocalName == "SW.Blocks.CompileUnit").ToList();
            if (index < 1 || index > units.Count)
                throw new ArgumentException("Rede " + index + " não existe (o bloco tem " + units.Count + ").");
            var unit = units[index - 1];
            string title = unit.Descendants().Where(e => e.Name.LocalName == "MultilingualText"
                    && (string)e.Attribute("CompositionName") == "Title")
                .SelectMany(t => t.Descendants().Where(x => x.Name.LocalName == "Text"))
                .Select(t => t.Value).FirstOrDefault();
            unit.Remove();
            return title;
        }

        /// <summary>
        /// Monta a CompileUnit da chamada como texto e insere depois da rede `after`
        /// (0 = antes de todas, negativo ou maior que o total = no fim). Texto e não XElement de
        /// propósito: é o mesmo FlgNet que o Portal aceitou na FP-03, e a v5 do namespace erra
        /// fácil montando nó a nó.
        /// </summary>
        internal static void InsertCallInXml(XDocument doc, CallSpec spec, int after)
        {
            var units = doc.Descendants().Where(e => e.Name.LocalName == "SW.Blocks.CompileUnit").ToList();

            int uid = 21;
            var parts = new StringBuilder();
            var wires = new StringBuilder();
            var accessOf = new Dictionary<string, int>();
            foreach (var p in spec.Params)
            {
                string value;
                if (!spec.Values.TryGetValue(p.Name, out value)) continue;
                accessOf[p.Name] = uid;
                parts.Append(Access(uid++, value));
            }
            int callUid = uid++;
            int instUid = uid++;
            int wireUid = uid + 20;

            parts.Append("                <Call UId=\"" + callUid + "\">\n")
                 .Append("                  <CallInfo Name=\"" + Escape(spec.Fb) + "\" BlockType=\"FB\">\n")
                 .Append("                    <Instance Scope=\"GlobalVariable\" UId=\"" + instUid + "\">\n")
                 .Append("                      <Component Name=\"" + Escape(spec.Instance) + "\" />\n")
                 .Append("                    </Instance>\n");
            foreach (var p in spec.Params)
                parts.Append("                    <Parameter Name=\"" + Escape(p.Name) + "\" Section=\""
                    + p.Section + "\" Type=\"" + Escape((p.Datatype ?? "").Trim('"')) + "\" />\n");
            parts.Append("                  </CallInfo>\n                </Call>\n");

            // EN vem do powerrail: chamada incondicional
            wires.Append("                <Wire UId=\"" + wireUid++ + "\">\n")
                 .Append("                  <Powerrail />\n")
                 .Append("                  <NameCon UId=\"" + callUid + "\" Name=\"en\" />\n")
                 .Append("                </Wire>\n");
            foreach (var p in spec.Params)
            {
                int acc;
                if (!accessOf.TryGetValue(p.Name, out acc)) continue;
                wires.Append(Wire(wireUid++, acc, callUid, p.Name, p.Section == "Output"));
            }

            string id = NextId(doc);
            var unit = "<SW.Blocks.CompileUnit ID=\"" + id + "\" CompositionName=\"CompileUnits\">\n"
                + "        <AttributeList>\n"
                + "          <NetworkSource>\n"
                + "            <FlgNet xmlns=\"" + FlgNetNs + "\">\n"
                + "              <Parts>\n" + parts + "              </Parts>\n"
                + "              <Wires>\n" + wires + "              </Wires>\n"
                + "            </FlgNet>\n"
                + "          </NetworkSource>\n"
                + "          <ProgrammingLanguage>LAD</ProgrammingLanguage>\n"
                + "        </AttributeList>\n"
                + "        <ObjectList>\n"
                + Text(id + "1", "Comment", spec.Comment ?? "")
                + Text(id + "2", "Title", spec.Title ?? "")
                + "        </ObjectList>\n"
                + "      </SW.Blocks.CompileUnit>";

            var node = XElement.Parse(unit);
            if (units.Count == 0)
            {
                // FC recém-criado (import-ladder/import-source) pode não ter rede nenhuma
                var block = doc.Root.Elements().First(e => e.Name.LocalName.StartsWith("SW.Blocks."));
                var list = block.Elements().FirstOrDefault(e => e.Name.LocalName == "ObjectList");
                if (list == null) { list = new XElement("ObjectList"); block.Add(list); }
                list.Add(node);
            }
            else if (after == 0) units[0].AddBeforeSelf(node);
            else if (after < 0 || after >= units.Count) units[units.Count - 1].AddAfterSelf(node);
            else units[after - 1].AddAfterSelf(node);
        }

        /// <summary>Remanence do membro (Static) — devolve o valor anterior.</summary>
        internal static string SetRetainInXml(XDocument doc, string member, bool retain)
        {
            var target = FindMember(doc, member);
            string was = (string)target.Attribute("Remanence") ?? "NonRetain";
            target.SetAttributeValue("Remanence", retain ? "Retain" : "NonRetain");
            return was;
        }

        internal static string RetainOf(XDocument doc, string member)
        {
            var target = FindMember(doc, member);
            return (string)target.Attribute("Remanence") ?? "NonRetain";
        }

        private static XElement FindMember(XDocument doc, string member)
        {
            var iface = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Interface");
            if (iface == null) throw new InvalidOperationException("Bloco sem Interface no XML.");
            var section = iface.Descendants().FirstOrDefault(e => e.Name.LocalName == "Section"
                && (string)e.Attribute("Name") == "Static");
            if (section == null)
                throw new InvalidOperationException("Bloco sem seção Static: retentividade só existe em "
                    + "variável estática de FB.");
            var hit = section.Elements().FirstOrDefault(e => e.Name.LocalName == "Member"
                && ((string)e.Attribute("Name") ?? "").Equals(member, StringComparison.OrdinalIgnoreCase));
            if (hit == null)
                throw new InvalidOperationException("Membro '" + member + "' não existe em Static. Conhecidos: "
                    + string.Join(", ", section.Elements().Where(e => e.Name.LocalName == "Member")
                        .Select(e => (string)e.Attribute("Name"))));
            return hit;
        }

        // ---------- helpers de FlgNet ----------

        internal static Dictionary<string, string> ParseParams(IEnumerable<string> args)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var raw in args ?? Enumerable.Empty<string>())
            {
                int eq = (raw ?? "").IndexOf('=');
                if (eq <= 0) throw new ArgumentException("--param espera NOME=VALOR, veio '" + raw + "'.");
                map[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
            }
            return map;
        }

        /// <summary>Símbolo (`tag` ou `DB.caminho.membro`) ou constante tipada.</summary>
        private static string Access(int uid, string value)
        {
            if (value.StartsWith("%", StringComparison.Ordinal))
                throw new ArgumentException("Endereço absoluto ('" + value + "') não entra em chamada: "
                    + "use o nome simbólico da tag.");
            var sb = new StringBuilder();
            if (ConstantValue.IsMatch(value))
            {
                sb.Append("                <Access Scope=\"TypedConstant\" UId=\"" + uid + "\">\n")
                  .Append("                  <Constant>\n")
                  .Append("                    <ConstantValue>" + Escape(value) + "</ConstantValue>\n")
                  .Append("                  </Constant>\n")
                  .Append("                </Access>\n");
                return sb.ToString();
            }
            sb.Append("                <Access Scope=\"GlobalVariable\" UId=\"" + uid + "\">\n")
              .Append("                  <Symbol>\n");
            foreach (var part in value.Split('.'))
                sb.Append("                    <Component Name=\"" + Escape(part.Trim().Trim('"')) + "\" />\n");
            sb.Append("                  </Symbol>\n                </Access>\n");
            return sb.ToString();
        }

        /// <summary>Entrada: Access → pino. Saída: pino → Access (a ordem importa no FlgNet).</summary>
        private static string Wire(int uid, int access, int call, string pin, bool isOutput)
        {
            string ident = "                  <IdentCon UId=\"" + access + "\" />\n";
            string name = "                  <NameCon UId=\"" + call + "\" Name=\"" + Escape(pin) + "\" />\n";
            return "                <Wire UId=\"" + uid + "\">\n"
                + (isOutput ? name + ident : ident + name)
                + "                </Wire>\n";
        }

        private static string Text(string id, string composition, string body)
        {
            return "          <MultilingualText ID=\"" + id + "\" CompositionName=\"" + composition + "\">\n"
                + "            <ObjectList>\n"
                + "              <MultilingualTextItem ID=\"" + id + "F\" CompositionName=\"Items\">\n"
                + "                <AttributeList>\n"
                + "                  <Culture>en-US</Culture>\n"
                + "                  <Text>" + Escape(body) + "</Text>\n"
                + "                </AttributeList>\n"
                + "              </MultilingualTextItem>\n"
                + "            </ObjectList>\n"
                + "          </MultilingualText>\n";
        }

        /// <summary>IDs do export são hex e únicos no documento — pega o maior e anda.</summary>
        private static string NextId(XDocument doc)
        {
            int max = 0;
            foreach (var e in doc.Descendants())
            {
                var id = (string)e.Attribute("ID");
                int n;
                if (id != null && int.TryParse(id, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out n))
                    max = Math.Max(max, n);
            }
            return (max + 16).ToString("X", CultureInfo.InvariantCulture);
        }

        private static string Escape(string s)
        {
            return (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string Safe(string name)
        {
            return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
