// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L42    class DbMember
//   L52    class MemberSpec
//   L66    .ParseSpec
//   L92    .RejectDuplicates
//   L106   .Order
//   L115   .Add
//   L140   .Validate
//   L156   .Row
//   L174   .Rows
//   L193   .Change
//   L243   .Remove
//   L281   núcleo comum (o envelope mora em Ops.EditBlock)
//   L284   .MemberOf
//   L296   .RemoveFromXml
//   L309   struct Delta
//   L317   .ChangeInXml
//   L355   struct Edit
//   L368   .AddToXml
//   L407   .ResolveSection
//   L442   .NameOf
//   L448   .Datatype
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Tia.Core
{
    /// <summary>
    /// add-db-member: insere um membro novo (tipicamente instância de UDT) num DB global,
    /// via export → edição do XML → import Override. Idempotente: membro já existente é no-op.
    /// </summary>
    public static class DbMember
    {
        private static readonly HashSet<string> Primitives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Bool", "Byte", "Word", "DWord", "LWord", "SInt", "Int", "DInt", "LInt", "USInt", "UInt",
            "UDInt", "ULInt", "Real", "LReal", "Char", "WChar", "String", "WString", "Time", "LTime",
            "Date", "DTL", "Time_Of_Day", "TOD", "S5Time", "Struct", "Variant",
        };

        /// <summary>Um membro a criar. `--member "A.B.NOME:Tipo"` é repetível; `--name` é a forma antiga.</summary>
        public sealed class MemberSpec
        {
            public string Path;
            public string Name;
            public string Type;
            public string Like;
        }

        /// <summary>
        /// "AREA.ALARMES.ALM_X:Bool" → path AREA.ALARMES, nome ALM_X, tipo Bool. Caminho, nome e
        /// tipo no MESMO argumento porque duas listas pareadas por posição (`--name`/`--type`)
        /// desalinham e dão a um membro o tipo do vizinho, em silêncio.
        /// No delete o tipo não existe (<paramref name="needType"/> false): "AREA.ALARMES.ALM_X".
        /// </summary>
        public static MemberSpec ParseSpec(string spec, bool needType = true)
        {
            var colon = (spec ?? "").LastIndexOf(':');
            if (colon <= 0 || colon == spec.Length - 1)
            {
                if (needType)
                    throw new ArgumentException("--member \"" + spec + "\" sem tipo: a forma é "
                        + "\"A.B.NOME:Tipo\" (o caminho é opcional, o tipo não).");
                colon = -1;
            }
            var full = (colon < 0 ? (spec ?? "") : spec.Substring(0, colon)).Trim();
            if (full.Length == 0)
                throw new ArgumentException("--member vazio: a forma é \"A.B.NOME\".");
            var dot = full.LastIndexOf('.');
            return new MemberSpec
            {
                Path = dot < 0 ? null : full.Substring(0, dot),
                Name = dot < 0 ? full : full.Substring(dot + 1),
                Type = colon < 0 ? null : spec.Substring(colon + 1).Trim(),
            };
        }

        /// <summary>
        /// Membro repetido na mesma chamada é erro antes do export — deixar o segundo passar calado
        /// é o silêncio que a F16 recusou. Vale para add e delete.
        /// </summary>
        private static void RejectDuplicates(IList<MemberSpec> members)
        {
            var dup = members.GroupBy(m => (m.Path ?? "") + "." + m.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
                throw new ArgumentException("Membro repetido na mesma chamada: '" + dup.Key.TrimStart('.')
                    + "'. Um membro por edição.");
        }

        /// <summary>
        /// Ordem de aplicação: raso primeiro. Com `A.B` e `A.B.C` na mesma chamada, o pedido mais
        /// fundo criaria o ramo e o `structsCreated` do outro mentiria. OrderBy é estável — entre
        /// membros da mesma profundidade vale a ordem da linha de comando.
        /// </summary>
        internal static List<MemberSpec> Order(IList<MemberSpec> members)
        {
            return members.OrderBy(m => (m.Path ?? "").Count(c => c == '.')).ToList();
        }

        /// <summary>
        /// Cria N membros num round-trip só (F16): o custo do envelope é do tamanho do DB, não do
        /// número de membros. Membro já existente é no-op — e se TODOS existirem, nada é importado.
        /// </summary>
        public static object Add(PlcSoftware plc, string dbName, IList<MemberSpec> members,
            string outDir, bool apply)
        {
            if (members == null || members.Count == 0)
                throw new ArgumentException("--name is required.");
            foreach (var m in members) Validate(m);
            RejectDuplicates(members);

            var db = ReplicateFc.FindDataBlock(plc.BlockGroup, dbName);
            if (db == null)
                throw new InvalidOperationException("Data block '" + dbName + "' not found.");

            var order = Order(members);
            var edits = new Dictionary<MemberSpec, Edit>();
            var steps = order.Select(m => new Ops.BlockEditStep
            {
                Label = "o membro '" + m.Name + "'",
                Apply = doc => edits[m] = AddToXml(doc, m.Path, m.Name, m.Type, m.Like),
                Proof = doc => MemberOf(doc, m.Path, m.Name) != null,
            }).ToList();

            var result = Ops.EditBlock(plc, db, "addmember_", outDir, apply, steps);
            return Rows(result, members, m => Row(m, edits[m]));
        }

        private static void Validate(MemberSpec m)
        {
            if (string.IsNullOrEmpty(m.Name))
                throw new ArgumentException("--name is required.");
            if (string.IsNullOrEmpty(m.Type) && string.IsNullOrEmpty(m.Like))
                throw new ArgumentException("Pass --type <Udt|Bool|...> or --like <existing sibling member>.");
            // Struct vazio é inválido ("A structure without components is not allowed") e deixa o DB
            // inconsistente — daí em diante todo verbo que exporta o bloco morre, inclusive o
            // add-db-member seguinte que criaria o primeiro membro. Sem saída sem reimportar o DB.
            if (string.Equals(m.Type, "Struct", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("--type Struct cria uma estrutura vazia, que deixa o DB "
                    + "inconsistente e trava os próximos verbos (o Openness não exporta bloco inconsistente). "
                    + "Ramo com membro dentro sai do próprio --path: `--path AREA.ALARMES --name X "
                    + "--type Bool` cria AREA e ALARMES como Struct e põe X dentro, num import só.");
        }

        private static Dictionary<string, object> Row(MemberSpec m, Edit edit)
        {
            var row = new Dictionary<string, object>
            {
                { "path", string.IsNullOrEmpty(m.Path) ? "Static" : m.Path },
                { "member", m.Name },
                { "datatype", edit.Datatype },
                { "action", edit.Action },
            };
            if (edit.StructsCreated != null && edit.StructsCreated.Count > 0)
                row["structsCreated"] = edit.StructsCreated;
            return row;
        }

        /// <summary>
        /// Resultado do envelope + uma linha por membro. Com um membro só, as linhas sobem para o
        /// topo — é a forma que o CLI sempre devolveu, e o que já lê `action`/`member` continua valendo.
        /// </summary>
        private static Dictionary<string, object> Rows(Dictionary<string, object> result,
            IList<MemberSpec> members, Func<MemberSpec, Dictionary<string, object>> row)
        {
            result["db"] = result["block"];
            result.Remove("block");
            var rows = members.Select(row).ToList();
            if (rows.Count == 1)
                foreach (var kv in rows[0]) result[kv.Key] = kv.Value;
            else
                result["members"] = rows;
            return result;
        }

        /// <summary>
        /// edit-db-member: muda tipo e/ou nome de um membro já existente. Mesma coreografia do Add
        /// (export → XML → import Override), porque membro de DB não é atributo da API.
        /// Atenção: renomear membro NÃO reescreve quem o referencia no código — os chamadores
        /// ficam inconsistentes até serem corrigidos à mão (o `xref --name DB` mostra quem é).
        /// </summary>
        public static object Change(PlcSoftware plc, string dbName, string path, string name,
            string type, string rename, string outDir, bool apply)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("--name is required.");
            if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(rename))
                throw new ArgumentException("Nothing to change: pass --type and/or --rename.");

            var db = ReplicateFc.FindDataBlock(plc.BlockGroup, dbName);
            if (db == null)
                throw new InvalidOperationException("Data block '" + dbName + "' not found.");

            string after = string.IsNullOrEmpty(rename) ? name : rename;
            var changes = default(Delta);
            var result = Ops.EditBlock(plc, db, "editmember_", outDir, apply, new[]
            {
                new Ops.BlockEditStep
                {
                    Label = "o membro '" + after + "'",
                    Apply = doc => changes = ChangeInXml(doc, path, name, type, rename),
                    Proof = doc =>
                    {
                        var m = MemberOf(doc, path, after);
                        return m != null && (string.IsNullOrEmpty(type)
                            || Datatype(type) == (string)m.Attribute("Datatype"));
                    },
                },
            });
            var dbLabel = (string)result["block"];
            result["db"] = dbLabel;
            result.Remove("block");
            result["path"] = string.IsNullOrEmpty(path) ? "Static" : path;
            result["member"] = name;
            result["datatype"] = changes.Datatype;
            result["action"] = changes.Action;
            result["changes"] = changes.Changes;
            if (changes.Action == "update" && !string.IsNullOrEmpty(rename))
                result["warning"] = "renaming a member does not fix its references — check `xref --name " + dbLabel + "`.";
            return result;
        }

        /// <summary>
        /// delete-db-member: tira N membros do DB num round-trip só. Mesma coreografia do Add:
        /// o custo do envelope é do TAMANHO do DB (e de quantos offsets o Portal precisa
        /// recalcular), não do número de membros — apagar 1 membro no meio da `DB GLOBAL` (5 558
        /// membros, `MemoryLayout: Standard`) custou 17 min, e apagar 10 no mesmo import custa o
        /// mesmo tanto, uma vez só. Idempotente: membro ausente é no-op. Como no rename, o código
        /// que referencia o membro NÃO é corrigido — fica inconsistente até alguém mexer
        /// (`xref --name DB` mostra quem).
        /// </summary>
        public static object Remove(PlcSoftware plc, string dbName, IList<MemberSpec> members,
            string outDir, bool apply)
        {
            if (members == null || members.Count == 0)
                throw new ArgumentException("--name is required.");
            foreach (var m in members)
                if (string.IsNullOrEmpty(m.Name)) throw new ArgumentException("--name is required.");
            RejectDuplicates(members);

            var db = ReplicateFc.FindDataBlock(plc.BlockGroup, dbName);
            if (db == null)
                throw new InvalidOperationException("Data block '" + dbName + "' not found.");

            // Fundo primeiro: com A.B e A.B.C na mesma chamada, apagar A.B antes levaria C junto e
            // o passo de C sairia "missing (no-op)" — verdadeiro, mas mentiroso sobre quem o apagou.
            var order = Order(members).AsEnumerable().Reverse().ToList();
            var edits = new Dictionary<MemberSpec, Edit>();
            var steps = order.Select(m => new Ops.BlockEditStep
            {
                Label = "a remoção de '" + m.Name + "'",
                Apply = doc => edits[m] = RemoveFromXml(doc, m.Path, m.Name),
                Proof = doc => MemberOf(doc, m.Path, m.Name) == null,
            }).ToList();

            var result = Ops.EditBlock(plc, db, "delmember_", outDir, apply, steps);
            var dbLabel = (string)result["block"];
            result = Rows(result, members, m => new Dictionary<string, object>
            {
                { "path", string.IsNullOrEmpty(m.Path) ? "Static" : m.Path },
                { "member", m.Name },
                { "datatype", edits[m].Datatype },
                { "action", edits[m].Action },
            });
            if (members.Any(m => edits[m].Action == "delete"))
                result["warning"] = "deleting a member does not fix its references — check `xref --name " + dbLabel + "`.";
            return result;
        }

        // ---------- núcleo comum (o envelope mora em Ops.EditBlock) ----------

        /// <summary>Membro no caminho, ou null — caminho inexistente conta como ausente.</summary>
        private static XElement MemberOf(XDocument doc, string path, string name)
        {
            try
            {
                return ResolveSection(doc, path).Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Member"
                        && NameOf(e).Equals(name, StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidOperationException) { return null; }
        }

        /// <summary>Núcleo puro do delete — sem Openness, testável offline.</summary>
        internal static Edit RemoveFromXml(XDocument doc, string path, string name)
        {
            var section = ResolveSection(doc, path);
            var member = section.Elements().Where(e => e.Name.LocalName == "Member")
                .FirstOrDefault(m => NameOf(m).Equals(name, StringComparison.OrdinalIgnoreCase));
            if (member == null)
                return new Edit { Action = "missing (no-op)", Datatype = null };

            var datatype = member.Attribute("Datatype")?.Value;
            member.Remove();
            return new Edit { Action = "delete", Datatype = datatype };
        }

        internal struct Delta
        {
            public string Action;
            public string Datatype;
            public Dictionary<string, string> Changes;
        }

        /// <summary>Núcleo puro do edit — sem Openness, testável offline.</summary>
        internal static Delta ChangeInXml(XDocument doc, string path, string name, string type, string rename)
        {
            var section = ResolveSection(doc, path);
            var members = section.Elements().Where(e => e.Name.LocalName == "Member").ToList();
            var member = members.FirstOrDefault(m => NameOf(m).Equals(name, StringComparison.OrdinalIgnoreCase));
            if (member == null)
                throw new InvalidOperationException("Member '" + name + "' not found under '" +
                    (string.IsNullOrEmpty(path) ? "Static" : path) + "'. Known members: " +
                    string.Join(", ", members.Select(NameOf)));

            var changes = new Dictionary<string, string>();
            var current = member.Attribute("Datatype")?.Value;
            if (!string.IsNullOrEmpty(type) && Datatype(type) != current)
                changes["datatype"] = current + " -> " + Datatype(type);
            if (!string.IsNullOrEmpty(rename) && !rename.Equals(name, StringComparison.Ordinal))
            {
                if (members.Any(m => NameOf(m).Equals(rename, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Member '" + rename + "' already exists in the same section.");
                changes["name"] = name + " -> " + rename;
            }
            if (changes.Count == 0)
                return new Delta { Action = "skip (no change)", Datatype = current, Changes = changes };

            if (changes.ContainsKey("datatype"))
            {
                member.SetAttributeValue("Datatype", Datatype(type));
                // tipo novo invalida o corpo expandido da instância antiga
                member.Elements().Where(e => e.Name.LocalName == "Sections").Remove();
            }
            if (changes.ContainsKey("name")) member.SetAttributeValue("Name", rename);
            return new Delta
            {
                Action = "update",
                Datatype = member.Attribute("Datatype")?.Value,
                Changes = changes,
            };
        }

        internal struct Edit
        {
            public string Action;
            public string Datatype;
            /// <summary>Segmentos de `--path` que não existiam e nasceram como Struct.</summary>
            public List<string> StructsCreated;
        }

        /// <summary>
        /// Núcleo puro da edição — sem Openness, testável offline.
        /// `path` = caminho pontilhado de structs sob a Section "Static" (vazio = raiz).
        /// `like` = membro irmão cujo nó é clonado (mantém atributos/comentário) e renomeado.
        /// </summary>
        internal static Edit AddToXml(XDocument doc, string path, string name, string type, string like)
        {
            var created = new List<string>();
            var section = ResolveSection(doc, path, createMissing: true, created: created);
            var members = section.Elements().Where(e => e.Name.LocalName == "Member").ToList();

            var existing = members.FirstOrDefault(m => NameOf(m).Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return new Edit { Action = "exists", Datatype = existing.Attribute("Datatype")?.Value };

            if (!string.IsNullOrEmpty(like))
            {
                var model = members.FirstOrDefault(m => NameOf(m).Equals(like, StringComparison.OrdinalIgnoreCase));
                if (model == null)
                    throw new InvalidOperationException("Member '" + like + "' not found under '" +
                        (string.IsNullOrEmpty(path) ? "Static" : path) + "'. Known members: " +
                        string.Join(", ", members.Select(NameOf)));
                var clone = new XElement(model);
                clone.SetAttributeValue("Name", name);
                if (!string.IsNullOrEmpty(type))
                    clone.SetAttributeValue("Datatype", Datatype(type));
                model.AddAfterSelf(clone);
                return new Edit { Action = "create", Datatype = clone.Attribute("Datatype")?.Value,
                    StructsCreated = created };
            }

            var datatype = Datatype(type);
            section.Add(new XElement(section.Name.Namespace + "Member",
                new XAttribute("Name", name), new XAttribute("Datatype", datatype)));
            return new Edit { Action = "create", Datatype = datatype, StructsCreated = created };
        }

        /// <summary>
        /// Desce o caminho pontilhado. Com `createMissing`, segmento inexistente nasce como
        /// `<Member Datatype="Struct">` — o ramo só fica vazio entre a criação e o membro-folha que
        /// vem logo depois, no mesmo XML, então o DB nunca chega inconsistente no Portal. É o que
        /// destrava reproduzir a hierarquia de área do molde (ALARMES/EVENTOS/INSTRUMENTACAO),
        /// impossível pela CLI até 2026-08-12 (FP-05, T4).
        /// </summary>
        private static XElement ResolveSection(XDocument doc, string path, bool createMissing = false,
            List<string> created = null)
        {
            var section = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Section"
                && (string)e.Attribute("Name") == "Static");
            if (section == null)
                throw new InvalidOperationException("'Static' section not found in the DB XML.");

            foreach (var segment in (path ?? "").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var member = section.Elements().FirstOrDefault(e => e.Name.LocalName == "Member"
                    && NameOf(e).Equals(segment, StringComparison.OrdinalIgnoreCase));
                if (member == null && createMissing)
                {
                    member = new XElement(section.Name.Namespace + "Member",
                        new XAttribute("Name", segment), new XAttribute("Datatype", "Struct"));
                    section.Add(member);
                    if (created != null) created.Add(segment);
                }
                if (member == null)
                    throw new InvalidOperationException("Member '" + segment + "' not found while walking path '" +
                        path + "'. Known members: " + string.Join(", ",
                        section.Elements().Where(e => e.Name.LocalName == "Member").Select(NameOf)));
                // Struct nativo aninha <Member> direto; instância de UDT expande em <Sections><Section>
                var nested = member.Elements().FirstOrDefault(e => e.Name.LocalName == "Sections")
                    ?.Elements().FirstOrDefault(e => e.Name.LocalName == "Section");
                // "é struct" não é "tem membro": struct esvaziado por delete continua struct
                if (nested == null && !member.Elements().Any(e => e.Name.LocalName == "Member")
                    && !"Struct".Equals(member.Attribute("Datatype")?.Value, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Member '" + segment + "' is not a struct.");
                section = nested ?? member;
            }
            return section;
        }

        private static string NameOf(XElement member)
        {
            return member.Attribute("Name")?.Value ?? "";
        }

        /// <summary>UDT vira "Nome" entre aspas; primitivo e Array[...] ficam literais.</summary>
        private static string Datatype(string type)
        {
            var t = type.Trim().Trim('"');
            if (Primitives.Contains(t)) return t;
            if (t.IndexOf(' ') >= 0 || t.IndexOf('[') >= 0) return type.Trim();   // Array[0..9] of Bool, String[32]
            return "\"" + t + "\"";
        }

    }
}
