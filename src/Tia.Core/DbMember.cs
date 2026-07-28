// NAV INDEX
//   1-25    usings, namespace, tipos primitivos conhecidos
//   27-75   DbMember.Add — export do DB, edição do XML, import Override
//   77-125  AddToXml — núcleo puro (testável offline): resolve seção, clona/insere Member
//   127-160 helpers: ResolveSection, NameOf, Datatype, Safe, Report
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Siemens.Engineering;
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

            var db = ReplicateFc.FindDataBlock(plc.BlockGroup, dbName);
            if (db == null)
                throw new InvalidOperationException("Data block '" + dbName + "' not found.");

            // Import Override descarta o objeto atual: ler nome/grupo ANTES de importar
            var dbLabel = db.Name;
            var group = db.Parent as PlcBlockGroup ?? plc.BlockGroup;

            Directory.CreateDirectory(outDir);
            var file = Path.GetFullPath(Path.Combine(outDir, "addmember_" + Safe(dbLabel) + ".xml"));
            if (File.Exists(file)) File.Delete(file);          // Openness recusa sobrescrever
            db.Export(new FileInfo(file), ExportOptions.WithDefaults);

            var doc = XDocument.Load(file);
            var edit = AddToXml(doc, path, name, type, like);
            if (edit.Action == "exists")
                return Report(dbLabel, path, name, edit.Datatype, "exists", file, false);

            doc.Save(file);
            if (apply)
                group.Blocks.Import(new FileInfo(file), ImportOptions.Override);
            return Report(dbLabel, path, name, edit.Datatype, "create", file, apply);
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
            if (File.Exists(file)) File.Delete(file);
            db.Export(new FileInfo(file), ExportOptions.WithDefaults);

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
                group.Blocks.Import(new FileInfo(file), ImportOptions.Override);
            return result;
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
        }

        /// <summary>
        /// Núcleo puro da edição — sem Openness, testável offline.
        /// `path` = caminho pontilhado de structs sob a Section "Static" (vazio = raiz).
        /// `like` = membro irmão cujo nó é clonado (mantém atributos/comentário) e renomeado.
        /// </summary>
        internal static Edit AddToXml(XDocument doc, string path, string name, string type, string like)
        {
            var section = ResolveSection(doc, path);
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
                return new Edit { Action = "create", Datatype = clone.Attribute("Datatype")?.Value };
            }

            var datatype = Datatype(type);
            section.Add(new XElement(section.Name.Namespace + "Member",
                new XAttribute("Name", name), new XAttribute("Datatype", datatype)));
            return new Edit { Action = "create", Datatype = datatype };
        }

        private static XElement ResolveSection(XDocument doc, string path)
        {
            var section = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Section"
                && (string)e.Attribute("Name") == "Static");
            if (section == null)
                throw new InvalidOperationException("'Static' section not found in the DB XML.");

            foreach (var segment in (path ?? "").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var member = section.Elements().FirstOrDefault(e => e.Name.LocalName == "Member"
                    && NameOf(e).Equals(segment, StringComparison.OrdinalIgnoreCase));
                if (member == null)
                    throw new InvalidOperationException("Member '" + segment + "' not found while walking path '" +
                        path + "'. Known members: " + string.Join(", ",
                        section.Elements().Where(e => e.Name.LocalName == "Member").Select(NameOf)));
                // Struct nativo aninha <Member> direto; instância de UDT expande em <Sections><Section>
                section = member.Elements().FirstOrDefault(e => e.Name.LocalName == "Sections")
                    ?.Elements().FirstOrDefault(e => e.Name.LocalName == "Section") ?? member;
                if (!section.Elements().Any(e => e.Name.LocalName == "Member"))
                    throw new InvalidOperationException("Member '" + segment + "' has no nested members — not a struct.");
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
