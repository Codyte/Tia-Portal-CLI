// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L57    class BlockEdit
//   L75    delete-network
//   L82    .DeleteNetwork
//   L122   add-call
//   L135   .AddCall
//   L169   class CallRequest
//   L180   class Prepared
//   L188   .Describe
//   L202   .Prepare
//   L272   set-retain
//   L280   .SetRetain
//   L302   coreografia
//   L308   .Patch
//   L318   núcleo puro (sem Openness: testável offline)
//   L320   class CallSpec
//   L333   .StripTypePrefix
//   L343   .DeleteOrder
//   L348   .CountNetworks
//   L354   .RemoveNetworkFromXml
//   L374   .InsertCallInXml
//   L459   .SetRetainInXml
//   L467   .RetainOf
//   L473   .FindMember
//   L491   helpers de FlgNet
//   L493   .ParseParams
//   L506   .Access
//   L536   .Wire
//   L545   .Text
//   L560   .NextId
//   L573   .Escape
//   L579   .Safe
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
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

        /// <summary>
        /// Constante que já carrega o próprio tipo no texto (`T#2S`, `W#16#F`, `'A'`). O Portal
        /// escreve essas como `TypedConstant` sem `ConstantType`; as demais (`TRUE`, `300`) como
        /// `LiteralConstant` + `ConstantType` do tipo do pino — e recusa a primeira forma para Bool
        /// (`'ConstantValue' has the invalid value 'TRUE'`, FP-04, T3).
        /// </summary>
        private static readonly Regex SelfTypedConstant =
            new Regex(@"^([A-Za-z0-9_]+#|')", RegexOptions.Compiled);

        // ---------- delete-network ----------

        /// <summary>
        /// Apaga as redes de índice N (1-based, a mesma numeração do `explain-block`). Sem isso,
        /// trocar duas redes de um molde exigia editar o XML fora do CLI. `--index` é repetível:
        /// N redes num round-trip só (F16).
        /// </summary>
        public static object DeleteNetwork(PlcSoftware plc, string blockName, IList<int> indexes,
            string outDir, bool apply)
        {
            if (indexes == null || indexes.Count == 0)
                throw new ArgumentException("--index is required.");
            var dup = indexes.GroupBy(i => i).FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
                throw new ArgumentException("Rede " + dup.Key + " repetida em --index: só existe uma.");

            int? before = null;
            var titles = new Dictionary<int, string>();
            var order = DeleteOrder(indexes);
            var steps = order.Select((index, n) => new Ops.BlockEditStep
            {
                Label = "a remoção da rede " + index,
                Apply = doc =>
                {
                    if (before == null) before = CountNetworks(doc);
                    titles[index] = RemoveNetworkFromXml(doc, index);
                },
                // Rede apagada não deixa marca própria — quem prova é a contagem, e ela só fecha
                // depois da última remoção. Daí a prova morar num step só.
                Proof = n < order.Count - 1 ? null
                    : (Func<XDocument, bool>)(doc => CountNetworks(doc) == before - order.Count),
            }).ToList();

            var result = Ops.EditBlock(plc, blockName, "delnet_", outDir, apply, steps);
            result["networksBefore"] = before ?? 0;
            result["networksAfter"] = (before ?? 0) - order.Count;
            if (indexes.Count == 1)
            {
                result["network"] = indexes[0];
                result["title"] = titles.TryGetValue(indexes[0], out var t) ? t : null;
            }
            else
                result["networks"] = indexes.Select(i => new Dictionary<string, object>
                    { { "network", i }, { "title", titles.TryGetValue(i, out var t) ? t : null } }).ToList();
            return result;
        }

        // ---------- add-call ----------

        /// <summary>
        /// Insere uma rede com a chamada de um FB ou de um FC (EN direto do powerrail). Os pinos
        /// saem da interface do bloco chamado — daí o <see cref="BlockInterface"/> ser o mesmo
        /// leitor do `list-interface`. InOut sem valor é erro (referência sem fio não compila);
        /// Input sem valor sai como `warning` e fica solto, que é o que o molde da casa faz.
        /// FC não tem instância: `--inst` é exigido para FB e recusado para FC — sem isso o
        /// verbo não montava o bloco `CHAMADA_*` do padrão da casa, que é sequência de chamadas de
        /// FC (FP-04, T1).
        /// ponytail: rede incondicional (sem contatos em série). Condição continua sendo cirurgia
        /// manual ou clone de um molde que já a tenha.
        /// </summary>
        public static object AddCall(PlcSoftware plc, string blockName, IList<CallRequest> calls,
            string outDir, bool apply)
        {
            if (calls == null || calls.Count == 0)
                throw new ArgumentException("--fb is required.");
            var prepared = calls.Select(c => Prepare(plc, c, outDir)).ToList();

            // networksBefore/After: o --index do delete-network é às cegas sem isso, e clone de molde
            // com rede vazia chega sem rede nenhuma — foi como a FP-05 apagou a rede errada (T7).
            int before = 0;
            var steps = prepared.Select((p, n) => new Ops.BlockEditStep
            {
                Label = "a chamada de '" + p.Spec.Fb + "'",
                Apply = doc =>
                {
                    if (n == 0) before = CountNetworks(doc);
                    InsertCallInXml(doc, p.Spec, p.After);
                },
                Proof = doc => doc.Descendants().Any(e => e.Name.LocalName == "CallInfo"
                    && (string)e.Attribute("Name") == p.Spec.Fb),
            }).ToList();

            var result = Ops.EditBlock(plc, blockName, "addcall_", outDir, apply, steps);
            result["networksBefore"] = before;
            result["networksAfter"] = before + prepared.Count;
            result["networks"] = before + prepared.Count;
            if (prepared.Count == 1)
                foreach (var kv in Describe(prepared[0])) result[kv.Key] = kv.Value;
            else
                result["calls"] = prepared.Select(Describe).ToList();
            return result;
        }

        /// <summary>Uma chamada a inserir. `--fb` abre uma; o que vier depois dela é dela.</summary>
        public sealed class CallRequest
        {
            public string Fb;
            public string Instance;
            public List<string> Params;
            /// <summary>Posição no documento COMO ELE ESTÁ nesta hora; -1 = no fim.</summary>
            public int After = -1;
            public string Title;
            public string Comment;
        }

        private sealed class Prepared
        {
            public CallSpec Spec;
            public int After;
            public int Parameters;
            public List<string> Unwired;
        }

        private static Dictionary<string, object> Describe(Prepared p)
        {
            var d = new Dictionary<string, object>
            {
                { "fb", p.Spec.Fb }, { "blockType", p.Spec.BlockType },
                { "instance", p.Spec.Instance }, { "parameters", p.Parameters },
            };
            if (p.Unwired.Count > 0)
                d["warning"] = "pino de entrada sem valor (fica solto na rede, como no molde da "
                    + "casa): " + string.Join(", ", p.Unwired) + ". Confira no compile.";
            return d;
        }

        /// <summary>Resolve o bloco chamado e monta o <see cref="CallSpec"/> — sem tocar no alvo.</summary>
        private static Prepared Prepare(PlcSoftware plc, CallRequest call, string outDir)
        {
            string fbName = call.Fb, instance = call.Instance;
            var called = Ops.FindBlock(plc, fbName);
            if (!(called is FB) && !(called is FC))
            {
                // o help escreve `--fb "FB Y|FC Y"` e isso se lê como "passe o tipo junto" (FP-06, T2):
                // aceitar o prefixo em vez de perder o batch inteiro
                var bare = StripTypePrefix(fbName);
                if (bare != fbName) called = Ops.FindBlock(plc, bare);
                if (!(called is FB) && !(called is FC))
                {
                    var near = Ops.FbsLike(plc, bare);
                    if (near.Count != 1)
                        throw new InvalidOperationException("FB/FC '" + fbName + "' not found.");
                    called = near[0];
                }
            }
            bool isFb = called is FB;
            if (isFb && string.IsNullOrEmpty(instance))
                throw new ArgumentException("'" + called.Name + "' é FB: chamada de FB exige instância — "
                    + "passe --inst <iDB>.");
            if (!isFb && !string.IsNullOrEmpty(instance))
                throw new ArgumentException("'" + called.Name + "' é FC: chamada de FC não tem instância — "
                    + "tire o --inst.");

            Directory.CreateDirectory(outDir);
            var ifaceFile = Path.GetFullPath(Path.Combine(outDir, "iface_" + Safe(called.Name) + ".xml"));
            Ops.ExportFresh(called, ifaceFile, ExportOptions.None);
            // FB sem pino é chamável: o Call carrega só o <Instance>. Era erro até 2026-08-12 e
            // obrigava a inventar um pino de entrada em bloco de área que só usa tag global e
            // estática retentiva (FP-05, T5).
            var iface = BlockInterface.FromXml(XDocument.Load(ifaceFile));

            var values = ParseParams(call.Params);
            var unknown = values.Keys.Where(k => !iface.Any(p => p.Name == k)).ToList();
            if (unknown.Count > 0)
                throw new ArgumentException("Parâmetro inexistente em '" + called.Name + "': "
                    + string.Join(", ", unknown) + ". Pinos: " + string.Join(", ", iface.Select(p => p.Name)));
            // InOut sem fio não compila mesmo (é referência, não valor) — continua erro. Input solto,
            // não: o molde da casa (`PARTIDA_BOMBA (B-10A)`) tem pino de entrada sem fio e compila,
            // então a régua do verbo era mais estrita que o projeto de referência (FP-05, T6). Sai
            // como aviso e o compile é quem julga.
            var missingInOut = iface.Where(p => p.Section == "InOut" && !values.ContainsKey(p.Name))
                .Select(p => p.Name + " : " + p.Datatype).ToList();
            if (missingInOut.Count > 0)
                throw new ArgumentException("Pino InOut sem valor (não compila — InOut é referência): "
                    + string.Join(", ", missingInOut));
            var unwiredInputs = iface.Where(p => p.Section == "Input" && !values.ContainsKey(p.Name))
                .Select(p => p.Name + " : " + p.Datatype).ToList();

            return new Prepared
            {
                Spec = new CallSpec
                {
                    Fb = called.Name,
                    BlockType = isFb ? "FB" : "FC",
                    Instance = instance,
                    Title = call.Title ?? ((isFb ? "Function Block " : "Function ")
                        + Regex.Replace(called.Name, @"^F[BC]\s+", "")),
                    Comment = call.Comment,
                    Params = iface,
                    Values = values,
                },
                After = call.After,
                Parameters = iface.Count,
                Unwired = unwiredInputs,
            };
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
        /// Uma edição pelo envelope de N (<see cref="Ops.EditBlock"/>): export → patch →
        /// Import Override com prova. O bloco volta para a MESMA pasta.
        /// </summary>
        private static Dictionary<string, object> Patch(PlcSoftware plc, string blockName, string prefix,
            string outDir, bool apply, Action<XDocument> patch, Func<string> what,
            Func<XDocument, bool> proof)
        {
            return Ops.EditBlock(plc, blockName, prefix, outDir, apply, new[]
            {
                new Ops.BlockEditStep { Label = what(), Apply = patch, Proof = proof },
            });
        }

        // ---------- núcleo puro (sem Openness: testável offline) ----------

        public sealed class CallSpec
        {
            public string Fb;
            /// <summary>"FB" (com `Instance`) ou "FC" (sem).</summary>
            public string BlockType = "FB";
            public string Instance;
            public string Title;
            public string Comment;
            public List<Param> Params;
            public Dictionary<string, string> Values;
        }

        /// <summary>"FC PARTIDA_BOMBA (BEF-01)" -> "PARTIDA_BOMBA (BEF-01)" (o help sugere o prefixo).</summary>
        internal static string StripTypePrefix(string name)
        {
            return name == null ? null : Regex.Replace(name, @"^F[BC]\s+", "");
        }

        /// <summary>
        /// Ordem de remoção: do maior índice para o menor. O `--index` é 1-based sobre o documento
        /// ANTES da edição — apagar em ordem crescente desloca as redes seguintes e a 2ª remoção
        /// pega a errada. Decrescente mantém válidos justo os índices que ainda faltam apagar.
        /// </summary>
        internal static List<int> DeleteOrder(IList<int> indexes)
        {
            return indexes.OrderByDescending(i => i).ToList();
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
                parts.Append(Access(uid++, value, p.Datatype));
            }
            int callUid = uid++;
            int instUid = uid++;
            int wireUid = uid + 20;

            bool isFb = spec.BlockType != "FC";
            // Só entra como <Parameter> o pino que tem fio: o Portal recusa o import de pino
            // declarado e não ligado ("The connection with the name 'N' is not connected to the
            // object with the UID 'M'"), e todo Call de export real tem params == wires − 1 (o `en`).
            // O que cai aqui é Output não usado e Input sem valor: ambos ficam soltos na rede.
            var declared = spec.Params.Where(p => accessOf.ContainsKey(p.Name)).ToList();
            // FC sem pino: <CallInfo ... /> e só. FB sem pino ainda abre a tag — o <Instance> mora lá.
            bool selfClose = !isFb && declared.Count == 0;
            parts.Append("                <Call UId=\"" + callUid + "\">\n")
                 .Append("                  <CallInfo Name=\"" + Escape(spec.Fb) + "\" BlockType=\""
                    + spec.BlockType + "\"" + (selfClose ? " />\n" : ">\n"));
            if (isFb)
                parts.Append("                    <Instance Scope=\"GlobalVariable\" UId=\"" + instUid + "\">\n")
                     .Append("                      <Component Name=\"" + Escape(spec.Instance) + "\" />\n")
                     .Append("                    </Instance>\n");
            foreach (var p in declared)
                parts.Append("                    <Parameter Name=\"" + Escape(p.Name) + "\" Section=\""
                    + p.Section + "\" Type=\"" + Escape((p.Datatype ?? "").Trim('"')) + "\" />\n");
            if (!selfClose)
                parts.Append("                  </CallInfo>\n");
            parts.Append("                </Call>\n");

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

        /// <summary>Símbolo (`tag` ou `DB.caminho.membro`) ou constante — tipada pelo pino.</summary>
        private static string Access(int uid, string value, string datatype)
        {
            if (value.StartsWith("%", StringComparison.Ordinal))
                throw new ArgumentException("Endereço absoluto ('" + value + "') não entra em chamada: "
                    + "use o nome simbólico da tag.");
            var sb = new StringBuilder();
            if (ConstantValue.IsMatch(value))
            {
                // `T#2S` se basta; `TRUE`/`300` precisam do tipo do pino, senão o Portal recusa
                string type = (datatype ?? "").Trim().Trim('"');
                bool literal = !SelfTypedConstant.IsMatch(value) && type.Length > 0;
                sb.Append("                <Access Scope=\"" + (literal ? "LiteralConstant" : "TypedConstant")
                    + "\" UId=\"" + uid + "\">\n")
                  .Append("                  <Constant>\n");
                if (literal)
                    sb.Append("                    <ConstantType>" + Escape(type) + "</ConstantType>\n");
                sb.Append("                    <ConstantValue>" + Escape(value) + "</ConstantValue>\n")
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
