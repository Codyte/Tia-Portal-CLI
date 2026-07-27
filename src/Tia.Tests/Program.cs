// NAV INDEX
// 12-46    infra: Check/Fail, repo root, out dir
// 48-67    Main — roda todos os testes, resume pass/fail
// 68-95    AlarmFc_BuildFcXml (3 vars → 1 word; 17 vars → 2 words)
// 96-117   AlarmFc_BuildCallObXml (2 FCs, número, título sem prefixo numérico)
// 118-143  FaultOb_BuildObXml (ordena por HW id, troca 999, slice x0/x1)
// 144-172  InstrumentFc_BuildAreaFcXml (8888/9999, instance DB, path global-DB, prefixo tag)
// 173-182  LadConverter_Convert (ladder.scl → XML LAD)
// 183-196  Ops_RequireRootType (root do XML valida kind no dry-run) + Throws helper
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Tia.Core;

namespace Tia.Tests
{
    /// <summary>Testes offline (sem TIA) dos geradores XML puros: assert-based, exit 1 se falhar.</summary>
    internal static class Program
    {
        private static int _failures;

        private static void Check(bool cond, string msg)
        {
            if (cond) { Console.WriteLine("  ok  " + msg); return; }
            _failures++;
            Console.WriteLine("  FAIL " + msg);
        }

        private static string RepoRoot()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null && !Directory.Exists(Path.Combine(dir, "docs", "examples")))
                dir = Path.GetDirectoryName(dir);
            if (dir == null) throw new InvalidOperationException("Repo root (docs/examples) not found above " + AppDomain.CurrentDomain.BaseDirectory);
            return dir;
        }

        private static string Fixture(string name) { return Path.Combine(RepoRoot(), "docs", "examples", name); }

        private static string OutDir()
        {
            var d = Path.Combine(Path.GetTempPath(), "tia-tests");
            Directory.CreateDirectory(d);
            return d;
        }

        private static int Main()
        {
            var tests = new Dictionary<string, Action>
            {
                { "AlarmFc.BuildFcXml", AlarmFc_BuildFcXml },
                { "AlarmFc.BuildCallObXml", AlarmFc_BuildCallObXml },
                { "FaultOb.BuildObXml", FaultOb_BuildObXml },
                { "InstrumentFc.BuildAreaFcXml", InstrumentFc_BuildAreaFcXml },
                { "LadConverter.Convert", LadConverter_Convert },
                { "Ops.RequireRootType", Ops_RequireRootType },
                { "DbMember.AddToXml", DbMember_AddToXml },
                { "Memory.Occupied", Memory_Occupied },
                { "Clone.Rewrite", Clone_Rewrite },
                { "Profinet.TagName", Profinet_TagName },
            };
            foreach (var t in tests)
            {
                Console.WriteLine(t.Key);
                try { t.Value(); }
                catch (Exception ex) { _failures++; Console.WriteLine("  FAIL (exception) " + ex.GetBaseException().Message); }
            }
            Console.WriteLine(_failures == 0 ? "ALL PASS" : _failures + " FAILURE(S)");
            return _failures == 0 ? 0 : 1;
        }

        private static void AlarmFc_BuildFcXml()
        {
            var vars3 = new List<string> { "ALM_BOMBA_1", "ALM_BOMBA_2", "ALM_NIVEL" };
            var path = AlarmFc.BuildFcXml(Fixture("FcModeloAlarmes.xml"), "FC_ALARMES_TESTE", "ETA", "ETA", vars3, OutDir());
            var doc = XDocument.Load(path);
            Check(doc.Descendants("AttributeList").Elements("Name").First().Value == "FC_ALARMES_TESTE", "nome do FC trocado");
            var units = doc.Descendants("SW.Blocks.CompileUnit").ToList();
            Check(units.Count == 1, "3 vars → 1 network (era " + units.Count + ")");
            var names = doc.Descendants().Where(e => e.Name.LocalName == "Component")
                .Select(e => (string)e.Attribute("Name")).ToList();
            Check(names.Contains("DB_BITS_TO_WORD_ETA_W1"), "instance DB W1");
            Check(vars3.All(v => names.Contains(v)), "3 variáveis ligadas");
            Check(names.Contains("WORD_ALARMES_1"), "saída WORD_ALARMES_1");
            Check(new[] { "DB GLOBAL", "ETA", "ALARMES" }.All(names.Contains), "path do DB global");
            // bits não usados viram OpenCon (16 - 3 = 13)
            var openCons = doc.Descendants().Count(e => e.Name.LocalName == "OpenCon");
            Check(openCons >= 13, "bits livres com OpenCon (" + openCons + " ≥ 13)");

            var vars17 = Enumerable.Range(1, 17).Select(i => "ALM_" + i).ToList();
            var path17 = AlarmFc.BuildFcXml(Fixture("FcModeloAlarmes.xml"), "FC_ALARMES_T17", "ETA", "ETA", vars17, OutDir());
            var doc17 = XDocument.Load(path17);
            Check(doc17.Descendants("SW.Blocks.CompileUnit").Count() == 2, "17 vars → 2 networks");
            var names17 = doc17.Descendants().Where(e => e.Name.LocalName == "Component")
                .Select(e => (string)e.Attribute("Name")).ToList();
            Check(names17.Contains("DB_BITS_TO_WORD_ETA_W2"), "instance DB W2");
            Check(names17.Contains("WORD_ALARMES_2"), "saída WORD_ALARMES_2");
        }

        private static void AlarmFc_BuildCallObXml()
        {
            var fcs = new List<(string Name, int? Number, string Folder)>
            {
                ("FC_ALARMES_ETA", 123, "3.1.1 ETA"),
                ("FC_ALARMES_ETE", (int?)null, "Elevatória"),
            };
            var path = AlarmFc.BuildCallObXml(Fixture("ObMoldeAlarmes.xml"), "CHAMADA_TESTE", 123, fcs, OutDir());
            var doc = XDocument.Load(path);
            var attrs = doc.Descendants("AttributeList").First();
            Check(attrs.Element("Name").Value == "CHAMADA_TESTE", "nome do OB");
            Check(attrs.Element("Number").Value == "123", "número do OB");
            var calls = doc.Descendants().Where(e => e.Name.LocalName == "CallInfo")
                .Select(e => (string)e.Attribute("Name")).ToList();
            Check(calls.SequenceEqual(new[] { "FC_ALARMES_ETA", "FC_ALARMES_ETE" }), "2 calls na ordem");
            var blockNumbers = doc.Descendants("IntegerAttribute")
                .Where(e => (string)e.Attribute("Name") == "BlockNumber").ToList();
            Check(blockNumbers.Count == 1 && blockNumbers[0].Value == "123", "BlockNumber só no FC que tem número");
            var titles = doc.Descendants("Text").Select(t => t.Value).ToList();
            Check(titles.Contains("FC Alarmes: ETA"), "título sem prefixo numérico (3.1.1 removido)");
        }

        private static void FaultOb_BuildObXml()
        {
            var template = XDocument.Load(Fixture("ModuleErrorMolde.xml"));
            // desordenado de propósito — BuildObXml deve ordenar por HardwareId
            var modules = new List<FaultOb.Module>
            {
                new FaultOb.Module { Name = "IO device_1", HardwareId = 300, QaOwner = "QA-02" },
                new FaultOb.Module { Name = "PLC_1", HardwareId = 272, QaOwner = "QA-01" },
            };
            var doc = FaultOb.BuildObXml(template, "OB_DIAG_TESTE", modules, new FaultObConfig());
            Check(doc.Descendants("AttributeList").Elements("Name").First().Value == "OB_DIAG_TESTE", "nome do OB");
            var units = doc.Descendants("SW.Blocks.CompileUnit").ToList();
            Check(units.Count == 2, "1 network por módulo");
            var constants = units.Select(u => u.Descendants()
                .First(e => e.Name.LocalName == "ConstantValue" && (e.Value == "272" || e.Value == "300")).Value).ToList();
            Check(constants.SequenceEqual(new[] { "272", "300" }), "ordenado por HW id, 999 trocado");
            Check(!doc.Descendants().Any(e => e.Name.LocalName == "ConstantValue" && e.Value == "999"), "nenhum 999 sobrando");
            var slices = doc.Descendants().Where(e => e.Name.LocalName == "Component"
                    && ((string)e.Attribute("Name") ?? "").StartsWith("WORD_"))
                .Select(e => (string)e.Attribute("Name") + ":" + (string)e.Attribute("SliceAccessModifier")).ToList();
            Check(slices.SequenceEqual(new[] { "WORD_1:x0", "WORD_1:x1" }), "bits x0/x1 na WORD_1");
            var qas = doc.Descendants().Where(e => e.Name.LocalName == "Component")
                .Select(e => (string)e.Attribute("Name")).Where(n => n != null && n.StartsWith("QA-")).ToList();
            Check(qas.SequenceEqual(new[] { "QA-01", "QA-02" }), "QA owner por network");
        }

        private static void InstrumentFc_BuildAreaFcXml()
        {
            var template = XDocument.Load(Fixture("InstrumentTemplateFc.xml"));
            var source = new InstrumentFc.Instrument { Id = "FIT-01", GlobalDbPath = "\"ETA\".\"FIT-01\"" };
            var target = new InstrumentFc.Instrument
            {
                Id = "FIT-02", GlobalDbPath = "\"ETA\".\"FIT-02\"", CmdAfericao = 101, CmdLimites = 201,
            };
            var task = new InstrumentFc.AreaTask
            {
                AreaName = "ETA", TargetFcName = "FC_INSTR_TESTE",
                Instruments = new List<InstrumentFc.Instrument> { target },
            };
            var path = InstrumentFc.BuildAreaFcXml(template, task, source,
                new InstrumentFcConfig { GlobalDb = "DB INSTRUMENTOS" }, OutDir());
            var doc = XDocument.Load(path);
            Check(doc.Descendants("AttributeList").Elements("Name").First().Value == "FC_INSTR_TESTE", "nome do FC");
            var names = doc.Descendants().Where(e => e.Name.LocalName == "Component")
                .Select(e => (string)e.Attribute("Name")).ToList();
            Check(names.Contains("DB_BITS_TO_WORD_FIT-02"), "instance DB renomeado");
            Check(names.Contains("FIT-02") && !names.Contains("FIT-01"), "path do DB global reescrito");
            Check(names.Contains("FIT-02_STS") && !names.Contains("FIT-01_STS"), "prefixo de tag reescrito");
            var texts = doc.Descendants().Where(e => e.Name.LocalName == "Text").Select(t => t.Value).ToList();
            Check(texts.Any(t => t.Contains("(FIT-02)") && t.Contains("AF=101") && t.Contains("LIM=201")),
                "título: id e comandos 8888/9999 trocados");
            Check(!doc.Descendants().Any(e => e.Name.LocalName == "ConstantValue"
                && (e.Value.Trim() == "8888" || e.Value.Trim() == "9999")), "nenhum placeholder 8888/9999 sobrando");
        }

        private static void LadConverter_Convert()
        {
            var result = LadConverter.Convert(Fixture("ladder.scl"), null, OutDir());
            var xmlFile = (string)result["xmlFile"];
            Check(File.Exists(xmlFile), "XML gerado");
            var doc = XDocument.Load(xmlFile);
            Check((int)result["networks"] > 0, "networks > 0 (" + result["networks"] + ")");
            Check(doc.Descendants().Any(e => e.Name.LocalName == "FlgNet"), "FlgNet presente");
        }

        private static void Ops_RequireRootType()
        {
            Check(Ops.XmlRootType(Fixture("StdBombaA.xml")) == "SW.Tags.PlcTagTable", "root de tag table");
            Check(Ops.XmlRootType(Fixture("BombaTemplateFc.xml")) == "SW.Blocks.FC", "root de bloco");
            Check(Throws(() => Ops.RequireRootType(Fixture("StdBombaA.xml"), "SW.Blocks.")),
                "tag table recusada como bloco (era falso positivo no dry-run)");
            Check(!Throws(() => Ops.RequireRootType(Fixture("BombaTemplateFc.xml"), "SW.Blocks.")), "FC aceito como bloco");
        }

        /// <summary>Nomes conferidos contra as tags reais de DISPOSITIVOS_PROFINET (projeto de referência).</summary>
        private static void Profinet_TagName()
        {
            Func<string, int, string> name = (tag, n) =>
                Profinet.TagName(new ProfinetMapping { EquipmentTag = tag, DeviceNumber = n });
            Check(name("INVERSOR_AG-02 CCM3", 60) == "COMM_60_INVERSOR_AG-02_CCM3", "espaço vira _, hífen fica");
            Check(name("REM_RM1.0", 1) == "COMM_1_REM_RM1.0", "ponto fica");
            Check(name("ACB_2", 3) == "COMM_3_ACB_2", "sem separador especial");
        }

        private static void DbMember_AddToXml()
        {
            const string ns = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
            Func<XDocument> db = () => XDocument.Parse(
                "<Document xmlns='" + ns + "'><SW.Blocks.GlobalDB><Sections><Section Name='Static'>" +
                "<Member Name='AREA' Datatype='Struct'><Sections><Section Name='None'>" +
                "<Member Name='BOMBA_A' Datatype='&quot;MotorDados&quot;'><Comment>x</Comment></Member>" +
                "</Section></Sections></Member></Section></Sections></SW.Blocks.GlobalDB></Document>");
            XNamespace n = ns;
            Func<XDocument, IEnumerable<XElement>> areaMembers = d => d.Descendants(n + "Section")
                .First(s => (string)s.Attribute("Name") == "None").Elements(n + "Member");

            var d1 = db();
            var e1 = DbMember.AddToXml(d1, "AREA", "BOMBA_C", null, "BOMBA_A");
            var m1 = areaMembers(d1).ToList();
            Check(e1.Action == "create" && e1.Datatype == "\"MotorDados\"", "--like herda o Datatype do irmão");
            Check(m1.Select(m => (string)m.Attribute("Name")).SequenceEqual(new[] { "BOMBA_A", "BOMBA_C" }),
                "membro clonado entra logo após o modelo");
            Check(m1[1].Element(n + "Comment") != null, "clone leva os filhos do modelo");

            var d2 = db();
            Check(DbMember.AddToXml(d2, "AREA", "NIVEL", "Real", null).Datatype == "Real", "primitivo sem aspas");
            Check(DbMember.AddToXml(d2, "AREA", "BOMBA_D", "MotorDados", null).Datatype == "\"MotorDados\"",
                "UDT entre aspas");
            Check(DbMember.AddToXml(d2, "AREA", "BOMBA_A", "MotorDados", null).Action == "exists",
                "membro já existente = no-op (idempotente)");
            Check(areaMembers(d2).Count() == 3, "nenhum duplicado inserido");

            // Struct nativo: <Member> aninhado direto, sem <Sections><Section>
            var d3 = XDocument.Parse("<Document xmlns='" + ns + "'><SW.Blocks.GlobalDB><Sections>" +
                "<Section Name='Static'><Member Name='AREA' Datatype='Struct'><Comment>a</Comment>" +
                "<Member Name='BOMBA_A' Datatype='&quot;MotorDados&quot;' /></Member>" +
                "</Section></Sections></SW.Blocks.GlobalDB></Document>");
            Check(DbMember.AddToXml(d3, "AREA", "BOMBA_C", null, "BOMBA_A").Datatype == "\"MotorDados\"",
                "struct nativo (Member aninhado direto) aceito no path");
            Check(d3.Descendants(n + "Member").Count(m => ((string)m.Attribute("Name")).StartsWith("BOMBA")) == 2,
                "inserido dentro do struct nativo");

            Check(Throws(() => DbMember.AddToXml(db(), "AREA", "X", null, "NAO_EXISTE")), "--like inexistente falha");
            Check(Throws(() => DbMember.AddToXml(db(), "NAO_EXISTE", "X", "Bool", null)), "--path inexistente falha");
            Check(Throws(() => DbMember.AddToXml(db(), "AREA.BOMBA_A", "X", "Bool", null)),
                "path através de membro não-struct falha");
        }

        private static void Memory_Occupied()
        {
            Func<string, string, KeyValuePair<string, string>> t =
                (addr, type) => new KeyValuePair<string, string>(addr, type);

            var used = Memory.Occupied(new[]
            {
                t("%M430.0", "Bool"), t("%M431.4", "Bool"),   // bits → 1 byte cada
                t("%MW10", "Word"),                           // 10-11
                t("%MD100", "Real"),                          // 100-103
                t("%I0.0", "Bool"), t("%Q5.1", "Bool"),       // outras áreas: ignoradas
                t("DB1.DBX0.0", "Bool"), t(null, "Bool"),
            });

            Check(used.SequenceEqual(new[] { 10, 11, 100, 101, 102, 103, 430, 431 }),
                "bytes ocupados: bits, word e dword; outras áreas fora (" + string.Join(",", used) + ")");
        }

        private static void Clone_Rewrite()
        {
            const string ns = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
            Func<XDocument> table = () => XDocument.Parse(
                "<Document xmlns='" + ns + "'><SW.Tags.PlcTagTable><AttributeList>" +
                "<Name>BOMBA (BH-01B)</Name></AttributeList>" +
                "<SW.Tags.PlcTag><AttributeList><Name>MODO_LOCAL_BH-01B</Name>" +
                "<LogicalAddress>%M430.6</LogicalAddress></AttributeList></SW.Tags.PlcTag>" +
                "<SW.Tags.PlcTag><AttributeList><Name>LIGADO_BH-01B</Name>" +
                "<LogicalAddress>%M430.7</LogicalAddress></AttributeList></SW.Tags.PlcTag>" +
                "<SW.Tags.PlcTag><AttributeList><Name>FALHA_BH-01B</Name>" +
                "<LogicalAddress>%M431.0</LogicalAddress></AttributeList></SW.Tags.PlcTag>" +
                "</SW.Tags.PlcTagTable></Document>");
            XNamespace n = ns;

            var d = table();
            var hits = Clone.Rewrite(d, Clone.ParseReplaces(new[] { "BH-01B=BH-01C" }));
            var names = d.Descendants(n + "Name").Select(e => e.Value).ToList();
            Check(hits == 4, "4 ocorrências trocadas (tabela + 3 tags), era " + hits);
            Check(names.All(x => x.Contains("BH-01C")) && !names.Any(x => x.Contains("BH-01B")),
                "nenhum BH-01B sobrando");

            var addrs = Clone.Readdress(d, "%M432.6");
            Check(addrs.SequenceEqual(new[] { "%M432.6", "%M432.7", "%M433.0" }),
                "bits sequenciais com carry de byte (" + string.Join(",", addrs) + ")");

            Check(Throws(() => Clone.ParseReplaces(new[] { "SEM_IGUAL" }).ToList()), "--replace sem '=' falha");
            Check(Throws(() => Clone.Readdress(table(), "M432")), "--at fora do formato %M<b>.<bit> falha");

            var word = XDocument.Parse("<Document xmlns='" + ns + "'><SW.Tags.PlcTag><AttributeList>" +
                "<LogicalAddress>%MW20</LogicalAddress></AttributeList></SW.Tags.PlcTag></Document>");
            Check(Throws(() => Clone.Readdress(word, "%M432.0")), "tag não-bit aborta o --at (sem sobreposição)");
        }

        private static bool Throws(Action a)
        {
            try { a(); return false; } catch { return true; }
        }
    }
}
