// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L48    class DbMember
//   L57    .Add
//   L106   .Change
//   L152   .Remove
//   L182   coreografia comum: export → patch → Import Override → prova
//   L191   .ExportFresh
//   L197   .MemberOf
//   L209   .RemoveFromXml
//   L222   struct Delta
//   L230   .ChangeInXml
//   L268   struct Edit
//   L281   .AddToXml
//   L320   .ResolveSection
//   L355   .NameOf
//   L361   .Datatype
//   L369   .Safe
//   L374   .Report
// ======================= END NAV INDEX =======================

// NAV INDEX
//   1-32     usings, namespace, tipos primitivos conhecidos
//   34-72    DbMember.Add — export do DB, edição do XML, import com prova
//   74-119   Change (edit-db-member) — troca tipo e/ou nome
//   121-158  Remove (delete-db-member)
//   160-186  ExportFresh (compila o alvo sujo antes de exportar) + MemberOf
//   188-199  RemoveFromXml — núcleo puro
//   201-245  ChangeInXml — núcleo puro do edit
//   247-286  AddToXml — núcleo puro: resolve seção, clona/insere Member
//   288-345  helpers: ResolveSection, NameOf, Datatype, Safe, Report
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

        public static object Add(PlcSoftware plc, string dbName, string path, string name,
            string type, string like, string outDir, bool apply)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("--name is required.");
            if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(like))
                throw new ArgumentException("Pass --type <Udt|Bool|...> or --like <existing sibling member>.");
            // Struct vazio é inválido ("A structure without components is not allowed") e deixa o DB
            // inconsistente — daí em diante todo verbo que exporta o bloco morre, inclusive o
            // add-db-member seguinte que criaria o primeiro membro. Sem saída sem reimportar o DB.
            if (string.Equals(type, "Struct", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("--type Struct cria uma estrutura vazia, que deixa o DB "
                    + "inconsistente e trava os próximos verbos (o Openness não exporta bloco inconsistente). "
                    + "Ramo com membro dentro sai do próprio --path: `--path AREA.ALARMES --name X "
                    + "--type Bool` cria AREA e ALARMES como Struct e põe X dentro, num import só.");

            var db = ReplicateFc.FindDataBlock(plc.BlockGroup, dbName);
            if (db == null)
                throw new InvalidOperationException("Data block '" + dbName + "' not found.");

            // Import Override descarta o objeto atual: ler nome/grupo ANTES de importar
            var dbLabel = db.Name;
            var group = db.Parent as PlcBlockGroup ?? plc.BlockGroup;

            Directory.CreateDirectory(outDir);
            var file = Path.GetFullPath(Path.Combine(outDir, "addmember_" + Safe(dbLabel) + ".xml"));
            ExportFresh(db, file);

            var doc = XDocument.Load(file);
            var edit = AddToXml(doc, path, name, type, like);
            if (edit.Action == "exists")
                return Report(dbLabel, path, name, edit.Datatype, "exists", file, false);

            doc.Save(file);
            if (apply)
                Ops.ImportAndProve(plc, group, dbLabel, file, "o membro '" + name + "'",
                    d => MemberOf(d, path, name) != null);
            var report = Report(dbLabel, path, name, edit.Datatype, "create", file, apply);
            if (edit.StructsCreated != null && edit.StructsCreated.Count > 0)
                report["structsCreated"] = edit.StructsCreated;
            return report;
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
            var dbLabel = db.Name;
            var group = db.Parent as PlcBlockGroup ?? plc.BlockGroup;

            Directory.CreateDirectory(outDir);
            var file = Path.GetFullPath(Path.Combine(outDir, "editmember_" + Safe(dbLabel) + ".xml"));
            ExportFresh(db, file);

            var doc = XDocument.Load(file);
            var changes = ChangeInXml(doc, path, name, type, rename);
            var result = Report(dbLabel, path, name, changes.Datatype, changes.Action, file,
                apply && changes.Action == "update");
            result["changes"] = changes.Changes;
            if (changes.Action == "update" && !string.IsNullOrEmpty(rename))
                result["warning"] = "renaming a member does not fix its references — check `xref --name " + dbLabel + "`.";
            if (changes.Action != "update") return result;

            doc.Save(file);
            if (apply)
            {
                string after = string.IsNullOrEmpty(rename) ? name : rename;
                Ops.ImportAndProve(plc, group, dbLabel, file, "o membro '" + after + "'", d =>
                {
                    var m = MemberOf(d, path, after);
                    return m != null && (string.IsNullOrEmpty(type)
                        || Datatype(type) == (string)m.Attribute("Datatype"));
                });
            }
            return result;
        }

        /// <summary>
        /// delete-db-member: tira um membro do DB. Mesma coreografia do Add/Change.
        /// Idempotente: membro ausente é no-op. Como no rename, o código que referencia o membro
        /// NÃO é corrigido — fica inconsistente até alguém mexer (`xref --name DB` mostra quem).
        /// </summary>
        public static object Remove(PlcSoftware plc, string dbName, string path, string name,
            string outDir, bool apply)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("--name is required.");

            var db = ReplicateFc.FindDataBlock(plc.BlockGroup, dbName);
            if (db == null)
                throw new InvalidOperationException("Data block '" + dbName + "' not found.");
            var dbLabel = db.Name;
            var group = db.Parent as PlcBlockGroup ?? plc.BlockGroup;

            Directory.CreateDirectory(outDir);
            var file = Path.GetFullPath(Path.Combine(outDir, "delmember_" + Safe(dbLabel) + ".xml"));
            ExportFresh(db, file);

            var doc = XDocument.Load(file);
            var edit = RemoveFromXml(doc, path, name);
            var result = Report(dbLabel, path, name, edit.Datatype, edit.Action, file,
                apply && edit.Action == "delete");
            if (edit.Action != "delete") return result;

            result["warning"] = "deleting a member does not fix its references — check `xref --name " + dbLabel + "`.";
            doc.Save(file);
            if (apply)
                Ops.ImportAndProve(plc, group, dbLabel, file, "a remoção de '" + name + "'",
                    d => MemberOf(d, path, name) == null);
            return result;
        }

        // ---------- coreografia comum: export → patch → Import Override → prova ----------

        /// <summary>
        /// Export pronto para patch. Bloco recém-importado por outro verbo chega
        /// modificado-não-compilado, e nesse estado o export devolve conteúdo defasado (ou recusa):
        /// o patch sairia calculado em cima de XML de outra época. Compilar antes é mais barato que
        /// descobrir depois. A política mora em <see cref="Ops.ExportFresh(PlcBlock,string,ExportOptions)"/>,
        /// uma só para os 16 exports do repo.
        /// </summary>
        private static void ExportFresh(DataBlock db, string file)
        {
            Ops.ExportFresh(db, file, ExportOptions.WithDefaults);
        }

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

        private static string Safe(string name)
        {
            return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        }

        private static Dictionary<string, object> Report(string db, string path, string name, string datatype,
            string action, string file, bool applied)
        {
            return new Dictionary<string, object>
            {
                { "db", db },
                { "path", string.IsNullOrEmpty(path) ? "Static" : path },
                { "member", name },
                { "datatype", datatype },
                { "action", action },
                { "file", file },
                { "applied", applied },
            };
        }
    }
}
