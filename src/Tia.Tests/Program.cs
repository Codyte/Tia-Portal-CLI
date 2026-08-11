// NAV INDEX
// 24-52    infra: Check/Fail, repo root, out dir
// 54-82    Main — roda todos os testes, resume pass/fail
// 83-110   AlarmFc_BuildFcXml (3 vars → 1 word; 17 vars → 2 words)
// 111-132  AlarmFc_BuildCallObXml (2 FCs, número, título sem prefixo numérico)
// 133-158  FaultOb_BuildObXml (ordena por HW id, troca 999, slice x0/x1)
// 159-195  InstrumentFc_BuildAreaFcXml (8888/9999, instance DB, path global-DB, prefixo tag)
// 196-218  LadConverter_Convert (ladder.scl → XML LAD; pinos pre/in1/in2, OR vira parte "O")
// 219-242  BlockExplain_Explain (XML → texto compacto)
// 243-252  Ops_RequireRootType (root do XML valida kind no dry-run) + Throws helper
// 253-273  InstrumentFc_FcName / Profinet_TagName (regras de nome)
// 274-334  Audit_Naming, DbMember_AddToXml
// 335-413  Memory_Occupied, Clone_Rewrite, Scaffold_Plan
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                { "Ops.RequireUtf8Bom", Ops_RequireUtf8Bom },
                { "Ops.WalkFolders", Ops_WalkFolders },
                { "Inventory.FolderMatches", Inventory_FolderMatches },
                { "DbMember.AddToXml", DbMember_AddToXml },
                { "Memory.Occupied", Memory_Occupied },
                { "Clone.Rewrite", Clone_Rewrite },
                { "Profinet.TagName", Profinet_TagName },
                { "InstrumentFc.FcName", InstrumentFc_FcName },
                { "Audit.Naming", Audit_Naming },
                { "Scaffold.Plan", Scaffold_Plan },
                { "BlockExplain.Explain", BlockExplain_Explain },
                { "BlockInterface.FromXml", BlockInterface_FromXml },
                { "BlockEdit.InsertCallInXml", BlockEdit_InsertCallInXml },
                { "BlockEdit.RemoveNetworkFromXml", BlockEdit_RemoveNetworkFromXml },
                { "BlockEdit.SetRetainInXml", BlockEdit_SetRetainInXml },
                { "Clone.InstancesInXml", Clone_InstancesInXml },
                { "Audit.DriveShape", Audit_DriveShape },
                { "Ops.Squash", Ops_Squash },
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
            // valores reais do molde: instrumento FQIT-01 sob "DB GLOBAL"."PRELIMINAR"."INSTRUMENTACAO"
            var source = new InstrumentFc.Instrument
            {
                Id = "FQIT-01",
                GlobalDbPath = "\"PRELIMINAR\".\"INSTRUMENTACAO\".\"FQIT-01_MEDIDOR_DE_VAZAO_ULTRASSONICO\"",
            };
            var target = new InstrumentFc.Instrument
            {
                Id = "FQIT-07",
                GlobalDbPath = "\"ELEVATÓRIA_DE_LODO_DIGERIDO\".\"INSTRUMENTACAO\".\"FQIT-07_MEDIDOR_DE_VAZAO_ULTRASSONICO\"",
                CmdAfericao = 101, CmdLimites = 201,
            };
            var task = new InstrumentFc.AreaTask
            {
                AreaName = "Elevatória de Lodo Digerido", TargetFcName = "FC_INSTR_TESTE",
                Instruments = new List<InstrumentFc.Instrument> { target },
            };
            var path = InstrumentFc.BuildAreaFcXml(template, task, source,
                new InstrumentFcConfig { GlobalDb = "DB GLOBAL" }, OutDir());
            var doc = XDocument.Load(path);
            Check(doc.Descendants("AttributeList").Elements("Name").First().Value == "FC_INSTR_TESTE", "nome do FC");
            var names = doc.Descendants().Where(e => e.Name.LocalName == "Component")
                .Select(e => (string)e.Attribute("Name")).ToList();
            Check(names.Contains("FB AFERIÇÃO INSTRUMENTOS_FQIT-07"), "instance DB renomeado");
            Check(names.Contains("FQIT-07_MEDIDOR_DE_VAZAO_ULTRASSONICO")
                && !names.Contains("FQIT-01_MEDIDOR_DE_VAZAO_ULTRASSONICO"), "path do DB global reescrito");
            Check(names.Contains("FQIT-07_PV_MACRO_MEDIDOR_VAZAO_INSTANTANEA")
                && !names.Contains("FQIT-01_PV_MACRO_MEDIDOR_VAZAO_INSTANTANEA"), "prefixo de tag reescrito");
            var texts = doc.Descendants().Where(e => e.Name.LocalName == "Text").Select(t => t.Value).ToList();
            Check(texts.Any(t => t.Contains("(FQIT-07)")), "título com o id do alvo");
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
            // rede 5 do fixture usa ">" → Part Gt com pinos pre/in1/in2/out (BombaTemplateFc.xml:1044-1058)
            var cmp = doc.Descendants().First(e => e.Name.LocalName == "Part" && (string)e.Attribute("Name") == "Gt");
            var pins = doc.Descendants().Where(e => e.Name.LocalName == "NameCon"
                && (string)e.Attribute("UId") == (string)cmp.Attribute("UId"))
                .Select(e => (string)e.Attribute("Name")).ToList();
            Check(pins.Contains("in1") && pins.Contains("in2"), "comparador usa in1/in2 (" + string.Join(",", pins) + ")");
            Check(pins.Contains("pre"), "comparador recebe energia no pino pre (" + string.Join(",", pins) + ")");
            // paralelo = parte "O"; juntar dois "out" no mesmo fio o import do Portal recusa
            Check(doc.Descendants().Any(e => e.Name.LocalName == "Part" && (string)e.Attribute("Name") == "O"),
                "OR vira parte O");
            Check(!doc.Descendants().Any(e => e.Name.LocalName == "Wire"
                && e.Elements().Count(c => (string)c.Attribute("Name") == "out") > 1),
                "nenhum fio com dois pinos out");
        }

        private static void BlockExplain_Explain()
        {
            var xml = Fixture("BombaTemplateFc.xml");
            var r = BlockExplain.Explain(xml, OutDir());
            var text = string.Join("\n", (List<string>)r["text"]);
            Check((int)r["networks"] == 11, "11 redes (" + r["networks"] + ")");
            Check((int)r["chars"] < new FileInfo(xml).Length / 10, "texto < 10% do XML ("
                + r["chars"] + " de " + new FileInfo(xml).Length + ")");
            Check(text.Contains("\"S-01A_STS_MODO_LOCAL\" := \"S-01A_STS_MOTOR_MODO_LOCAL_1\""),
                "bobina simples com a tag que a alimenta");
            Check(text.Contains("CALL FB \"FB FALHA\" inst \"FB FALHA_S-01A\""), "chamada de FB com instância");
            Check(text.Contains("INPUT_FALHA := NOT \"S-01A_STS_FALHA_PROFINET\""), "parâmetro com contato negado");
            Check(text.Contains("FALHA => \"S-01A_FALHA\""), "saída da chamada ligada na tag");
            Check(Regex.IsMatch(text, @"INPUT_RESET_FALHA := \(""[^""]+""(\.[^ ]+)? OR ""[^""]+""\)"),
                "paralelo vira OR");
            Check(text.Contains("\"DB GLOBAL\".AREA_01"), "path de DB global preservado");
            Check(text.Contains("IF \"DB GLOBAL\".AFERICAO.AFERICAO_ANALOGICA.COMANDO = 300 THEN"),
                "comparador com in1/in2 resolvidos");
            Check(!text.Contains("GlobalConstant") && text.Contains("HWIDSTW := \"INVERSOR_S-01A"),
                "constante global nomeada");
            Check(!text.Contains("Ret_Val : Void"), "Ret_Val vazio fora do cabeçalho");
            Check(File.Exists((string)r["file"]), "arquivo .explain.txt escrito");
        }

        private static void Ops_RequireRootType()
        {
            Check(Ops.XmlRootType(Fixture("StdBombaA.xml")) == "SW.Tags.PlcTagTable", "root de tag table");
            Check(Ops.XmlRootType(Fixture("BombaTemplateFc.xml")) == "SW.Blocks.FC", "root de bloco");
            Check(Throws(() => Ops.RequireRootType(Fixture("StdBombaA.xml"), "SW.Blocks.")),
                "tag table recusada como bloco (era falso positivo no dry-run)");
            Check(!Throws(() => Ops.RequireRootType(Fixture("BombaTemplateFc.xml"), "SW.Blocks.")), "FC aceito como bloco");
        }

        /// <summary>Nó de pasta em memória: os delegados de WalkFolders isolam a regra do PlcSoftware.</summary>
        private sealed class Folder
        {
            public string Name;
            public readonly List<Folder> Kids = new List<Folder>();
            public Folder Add(string name) { var k = new Folder { Name = name }; Kids.Add(k); return k; }
            public string Trail() { return string.Join("|", Kids.Select(k => k.Name)); }
        }

        /// <summary>
        /// Longest-match de pasta. Sem ele, nome com '/' ("1. I/OS", "3. Alarmes/Eventos/Falhas")
        /// vira dois segmentos e o caminho não resolve. Só tinha validação em runtime.
        /// </summary>
        private static void Ops_WalkFolders()
        {
            Func<Folder> tree = () =>
            {
                var root = new Folder { Name = "" };
                var ios = root.Add("1. I/OS");           // nome com barra: 2 segmentos no split
                ios.Add("QA-00");
                var alarmes = root.Add("3. Alarmes/Eventos/Falhas");
                alarmes.Add("3.1 Alarmes Words").Add("3.1.0 Modelo");
                return root;
            };
            Func<Folder, string, Folder> find = (f, n) => f.Kids.FirstOrDefault(k => k.Name == n);
            Func<Folder, string, Folder> add = (f, n) => f.Add(n);
            Func<Folder, string, Folder> walk = (root, path) => Ops.WalkFolders(root, path, "Block", find, null);

            Check(walk(tree(), "1. I/OS").Name == "1. I/OS", "nome com '/' casa inteiro (longest-match)");
            Check(walk(tree(), "1. I/OS/QA-00").Name == "QA-00", "segue para a subpasta depois do nome com '/'");
            Check(walk(tree(), "3. Alarmes/Eventos/Falhas/3.1 Alarmes Words/3.1.0 Modelo").Name == "3.1.0 Modelo",
                "3 barras no nome da pasta + 2 níveis abaixo");
            Check(walk(tree(), "").Name == "", "path vazio devolve a raiz");
            Check(Throws(() => { walk(tree(), "3.1 Alarmes Words"); }),
                "sem create, pasta que não é filha da raiz falha (WalkFolders é caminho, não busca)");
            Check(Throws(() => { walk(tree(), "1. I/OS/NAO_EXISTE"); }), "segmento ausente lança");

            var t = tree();
            Check(Ops.WalkFolders(t, "9. Nova/Sub", "Block", find, add).Name == "Sub", "create desce criando");
            Check(t.Kids.Any(k => k.Name == "9. Nova"), "pasta nova pendurada na raiz");
            // armadilha conhecida: no create não há como saber que "1. I/OS" é UM nome — vira 2 pastas.
            var t2 = new Folder { Name = "" };
            Ops.WalkFolders(t2, "1. I/OS", "Block", find, add);
            Check(t2.Trail() == "1. I", "criar pasta com '/' no nome quebra em 2 níveis (só existente casa inteiro)");
        }

        /// <summary>
        /// Filtro de list-blocks --folder: era prefixo da raiz e devolvia count 0 silencioso para
        /// nome de folha. Casa fragmento de caminho, preso no limite de segmento.
        /// </summary>
        private static void Inventory_FolderMatches()
        {
            const string deep = "3. Alarmes/Eventos/Falhas/3.1 Alarmes Words/3.1.0 Modelo";
            Check(Inventory.FolderMatches(deep, "3.1 Alarmes Words"), "nome de folha casa no meio do caminho");
            Check(Inventory.FolderMatches(deep, "3. Alarmes/Eventos/Falhas"), "caminho da raiz casa");
            Check(Inventory.FolderMatches(deep, deep), "caminho inteiro casa");
            Check(Inventory.FolderMatches(deep, "Falhas/3.1 Alarmes Words"), "fragmento com barra casa");
            Check(Inventory.FolderMatches(deep, "/3.1 Alarmes Words/"), "barras nas pontas do filtro são toleradas");
            Check(Inventory.FolderMatches(deep, "3.1 ALARMES WORDS"), "case-insensitive");
            Check(Inventory.FolderMatches("3.1 Alarmes Words", "3.1 Alarmes Words"), "a própria pasta entra, não só subpasta");
            Check(!Inventory.FolderMatches(deep, "3.1"), "limite de segmento: '3.1' não casa '3.1 Alarmes Words'");
            Check(!Inventory.FolderMatches(deep, "Alarmes Words"), "sufixo de segmento não casa");
            Check(!Inventory.FolderMatches("", "3.1 Alarmes Words"), "bloco na raiz não casa filtro");
        }

        /// <summary>Gate de encoding do import-source: acento sem BOM vira mojibake no Openness.</summary>
        private static void Ops_RequireUtf8Bom()
        {
            Func<string, byte[], string> write = (name, bytes) =>
            {
                var p = Path.Combine(OutDir(), name);
                File.WriteAllBytes(p, bytes);
                return p;
            };
            const string scl = "FUNCTION \"Aferição CMD\" : Void\nEND_FUNCTION\n";
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };

            Check(!Throws(() => Ops.RequireUtf8Bom(write("bom.scl",
                    bom.Concat(Encoding.UTF8.GetBytes(scl)).ToArray()))),
                "UTF-8 com BOM passa");
            Check(!Throws(() => Ops.RequireUtf8Bom(write("ascii.scl",
                    Encoding.ASCII.GetBytes("FUNCTION \"CMD\" : Void\nEND_FUNCTION\n")))),
                "ASCII puro passa sem BOM (BOM não é exigido à toa)");
            Check(Throws(() => Ops.RequireUtf8Bom(write("nobom.scl", Encoding.UTF8.GetBytes(scl)))),
                "UTF-8 sem BOM recusado (era o mojibake silencioso)");
            Check(Throws(() => Ops.RequireUtf8Bom(write("latin1.scl",
                    Encoding.GetEncoding("ISO-8859-1").GetBytes(scl)))),
                "Latin-1 recusado");
            Check(Throws(() => Ops.RequireUtf8Bom(write("utf16.scl", Encoding.Unicode.GetBytes(scl)))),
                "UTF-16 recusado (BOM FF FE não é o de UTF-8)");
        }

        /// <summary>Nomes de FC conferidos contra 5.1 Aferição Analógica e 5.2 Totalizadores.</summary>
        private static void InstrumentFc_FcName()
        {
            Check(InstrumentFc.FcName("Elevatória de Purga de Lodo", "_ANALOGS")
                == "ELEVATRIA_DE_PURGA_DE_LODO_ANALOGS", "acento cai, espaço vira _");
            Check(InstrumentFc.FcName("Elevatória de Purga de Lodo", "_TOTALIZADOR")
                == "ELEVATRIA_DE_PURGA_DE_LODO_TOTALIZADOR", "sufixo da família de totalizadores");
            Check(InstrumentFc.FcName("Tanque de Aeração 01", "_ANALOGS")
                == "TANQUE_DE_AERAO_01_ANALOGS", "número mantido");
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

        // strings reais do projeto de referência (workspace/.../snapshot.json)
        private static void Audit_Naming()
        {
            Check(Audit.TagOf("Soprador 1 (S-01A)") == "S-01A", "TAG da pasta de equipamento");
            Check(Audit.TagOf("4.1.2 Dosagem Sistema Alcalinizante (RA-01)") == "RA-01", "TAG da pasta de área");
            Check(Audit.TagOf("3.1.4 Elevatória de Gordura") == null, "pasta sem (TAG)");
            Check(Audit.CarriesTag("PARTIDA_MOTOR_1 (S-01A)", "S-01A"), "TAG entre parênteses");
            Check(Audit.CarriesTag("FB FALHA_S-01A", "S-01A"), "TAG como sufixo _TAG");
            Check(Audit.CarriesTag("FB SETPOINT MANUAL S-01A", "S-01A"), "TAG separado por espaço");
            Check(!Audit.CarriesTag("FB FALHA_S-01B", "S-01A"), "bloco de outro equipamento reprova");
            // 3.1.15 'Elevatória Agua de Serviço' × 2.15 'Elevatória Água de Serviço' = mesma área
            Check(Audit.NormalizeArea("Elevatória Agua de Serviço") == Audit.NormalizeArea("Elevatória Água de Serviço"),
                "acento não separa área");
            Check(Audit.NormalizeArea("Preliminar (P-GM-01)") == Audit.NormalizeArea("Preliminar"),
                "(TAG) na pasta de área não separa");
            Check(Audit.NormalizeArea("Desarenador") != Audit.NormalizeArea("Casa de Cloro"), "áreas distintas");
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

            // edit-db-member
            var d4 = db();
            var c1 = DbMember.ChangeInXml(d4, "AREA", "BOMBA_A", "Real", "BOMBA_Z");
            Check(c1.Action == "update" && c1.Datatype == "Real", "muda tipo e nome de uma vez");
            Check((string)areaMembers(d4).Single().Attribute("Name") == "BOMBA_Z", "nome trocado no XML");
            Check(DbMember.ChangeInXml(db(), "AREA", "BOMBA_A", "MotorDados", null).Action == "skip (no change)",
                "mesmo tipo = no-op");
            Check(Throws(() => DbMember.ChangeInXml(db(), "AREA", "NAO_EXISTE", "Real", null)),
                "membro inexistente falha");
            Check(DbMember.ChangeInXml(db(), "AREA", "BOMBA_A", null, "BOMBA_A").Action == "skip (no change)",
                "renomear pro mesmo nome = no-op");

            // delete-db-member
            var d5 = db();
            var r1 = DbMember.RemoveFromXml(d5, "AREA", "BOMBA_A");
            Check(r1.Action == "delete" && r1.Datatype == "\"MotorDados\"", "delete devolve o tipo removido");
            Check(!areaMembers(d5).Any(), "membro sai do XML");
            Check(DbMember.RemoveFromXml(d5, "AREA", "BOMBA_A").Action == "missing (no-op)",
                "membro ausente = no-op (idempotente)");
            Check(Throws(() => DbMember.RemoveFromXml(db(), "NAO_EXISTE", "BOMBA_A")), "--path inexistente falha");
            Check(DbMember.AddToXml(d5, "AREA", "BOMBA_B", "Real", null).Action == "create",
                "struct esvaziado pelo delete continua struct");
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

            // tabela mista (a da casa tem %MB/%MW/%MD junto dos bits): deslocamento em bloco,
            // preservando o layout relativo — alocar denso quebraria alinhamento de word/dword
            Func<XDocument> mixed = () => XDocument.Parse("<Document xmlns='" + ns + "'>" +
                "<SW.Tags.PlcTag><AttributeList><LogicalAddress>%M20.3</LogicalAddress></AttributeList></SW.Tags.PlcTag>" +
                "<SW.Tags.PlcTag><AttributeList><LogicalAddress>%MW22</LogicalAddress></AttributeList></SW.Tags.PlcTag>" +
                "<SW.Tags.PlcTag><AttributeList><LogicalAddress>%MD24</LogicalAddress></AttributeList></SW.Tags.PlcTag>" +
                "</Document>");
            var shifted = Clone.Readdress(mixed(), "%M432.0");
            Check(shifted.SequenceEqual(new[] { "%M432.3", "%MW434", "%MD436" }),
                "tabela mista desloca em bloco (" + string.Join(",", shifted) + ")");
            Check(Throws(() => Clone.Readdress(mixed(), "%M432.6")), "--at com bit != 0 em tabela mista falha");
        }

        private static void Scaffold_Plan()
        {
            var manifest = new ScaffoldManifest
            {
                Source = "",
                Items = new List<ScaffoldItem>
                {
                    new ScaffoldItem { File = "ObMoldeAlarmes.xml",
                        Folder = new List<string> { "3. Alarmes/Eventos/Falhas", "3.1 Alarmes Words" } },
                    new ScaffoldItem { File = "FcModeloAlarmes.xml" },
                    new ScaffoldItem { File = "SmokeTags.xml" },
                },
            };
            var plan = Scaffold.Plan(manifest, Path.Combine(RepoRoot(), "docs", "examples"));
            Check(plan.Select(p => p.RootType).SequenceEqual(
                    new[] { "SW.Tags.PlcTagTable", "SW.Blocks.FC", "SW.Blocks.OB" }),
                "ordem de import: tabela → FC → OB (" + string.Join(", ", plan.Select(p => p.RootType)) + ")");
            Check(plan[2].Folder.Count == 2 && plan[2].Folder[0] == "3. Alarmes/Eventos/Falhas",
                "'/' no nome da pasta continua um segmento só");
            Check(plan.All(p => !string.IsNullOrEmpty(p.Name)), "nome do objeto lido de cada XML");
            Check(Scaffold.SameFamily("S7-1500", "S71500") && Scaffold.SameFamily("s7 1500", "S71500"),
                "família da CPU compara sem hífen/espaço/caixa");
            Check(!Scaffold.SameFamily("S7-1500", "S71200"), "família diferente reprova");
            // <Culture> é elemento, não atributo — ler errado deixa o projeto novo sem a língua do XML
            var cultures = Ops.XmlCultures(Fixture("ObMoldeAlarmes.xml")).ToList();
            Check(cultures.Contains("pt-BR") && cultures.Contains("en-US"),
                "culturas lidas do XML (" + string.Join(", ", cultures) + ")");

            // --replace: sanitiza o XML e os segmentos de pasta antes do import (offline)
            var reps = Clone.ParseReplaces(new[] { "Modelo=Generico", "3. Alarmes=3 Alarmes" });
            var tmp = Path.Combine(Path.GetTempPath(), "tia-tests-scaffold");
            var planR = Scaffold.Plan(manifest, Path.Combine(RepoRoot(), "docs", "examples"), reps, tmp);
            Check(planR[2].Folder[0] == "3 Alarmes/Eventos/Falhas", "--replace troca segmento de pasta");
            Check(planR.All(p => p.File.StartsWith(tmp, StringComparison.OrdinalIgnoreCase)),
                "--replace importa a cópia reescrita, não o XML de origem");
            Check(!File.ReadAllText(planR[1].File).Contains("Modelo"), "token trocado no XML reescrito");
            Check(plan[1].File != planR[1].File && File.ReadAllText(plan[1].File).Contains("Modelo"),
                "XML de origem intacto");

            Check(Throws(() => Scaffold.Plan(new ScaffoldManifest
                {
                    Items = new List<ScaffoldItem> { new ScaffoldItem { File = "nao-existe.xml" } },
                }, ".")),
                "arquivo ausente falha no plano, antes de tocar o projeto");
        }

        // ---------- list-interface / add-call / delete-network / set-retain ----------

        /// <summary>FC de 2 redes, no formato do export (Document sem namespace default).</summary>
        private static XDocument Fc(int units)
        {
            var sb = new StringBuilder("<Document><SW.Blocks.FC ID=\"1\"><AttributeList>"
                + "<Name>FC TESTE</Name><ProgrammingLanguage>LAD</ProgrammingLanguage>"
                + "<Interface><Sections><Section Name=\"Input\">"
                + "<Member Name=\"LIGADO\" Datatype=\"Bool\" /></Section>"
                + "<Section Name=\"Static\"><Member Name=\"HORIMETRO\" Datatype=\"DInt\" />"
                + "<Member Name=\"AUX\" Datatype=\"Bool\" Remanence=\"NonRetain\" /></Section>"
                + "</Sections></Interface></AttributeList><ObjectList>");
            for (int i = 0; i < units; i++)
                sb.Append("<SW.Blocks.CompileUnit ID=\"" + (i + 3) + "\" CompositionName=\"CompileUnits\">"
                    + "<ObjectList><MultilingualText ID=\"" + (i + 30) + "\" CompositionName=\"Title\">"
                    + "<ObjectList><MultilingualTextItem ID=\"" + (i + 60) + "\" CompositionName=\"Items\">"
                    + "<AttributeList><Culture>en-US</Culture><Text>Rede " + (i + 1) + "</Text>"
                    + "</AttributeList></MultilingualTextItem></ObjectList></MultilingualText>"
                    + "</ObjectList></SW.Blocks.CompileUnit>");
            return XDocument.Parse(sb.Append("</ObjectList></SW.Blocks.FC></Document>").ToString());
        }

        private static void BlockInterface_FromXml()
        {
            var doc = XDocument.Parse("<Document><SW.Blocks.FB><AttributeList><Name>FB X</Name>"
                + "<ProgrammingLanguage>SCL</ProgrammingLanguage><Interface><Sections>"
                + "<Section Name=\"Input\"><Member Name=\"CORRENTE\" Datatype=\"Real\" /></Section>"
                + "<Section Name=\"Output\"><Member Name=\"PARTIDAS\" Datatype=\"DInt\" /></Section>"
                + "<Section Name=\"InOut\"><Member Name=\"DADOS\" Datatype=\"&quot;AgitadorDados&quot;\" /></Section>"
                + "<Section Name=\"Static\"><Member Name=\"NAO_E_PINO\" Datatype=\"Bool\" /></Section>"
                + "</Sections></Interface></AttributeList></SW.Blocks.FB></Document>");
            var ps = BlockInterface.FromXml(doc);
            Check(ps.Count == 3, "Static fica de fora: 3 pinos, veio " + ps.Count);
            Check(ps[0].Section == "Input" && ps[2].Name == "DADOS", "ordem do XML preservada");
            var row = BlockInterface.Describe(doc);
            Check((string)row["block"] == "FB X" && row.ContainsKey("inout"), "Describe traz nome e seções");
        }

        private static void BlockEdit_InsertCallInXml()
        {
            var iface = new List<Param>
            {
                new Param { Section = "Input", Name = "CORRENTE", Datatype = "Real" },
                new Param { Section = "Input", Name = "TEMPO", Datatype = "Time" },
                new Param { Section = "Output", Name = "PARTIDAS", Datatype = "DInt" },
                new Param { Section = "InOut", Name = "DADOS", Datatype = "\"AgitadorDados\"" },
            };
            Func<BlockEdit.CallSpec> spec = () => new BlockEdit.CallSpec
            {
                Fb = "FB SUPERVISAO",
                Instance = "FB SUPERVISAO_AG-05",
                Title = "Function Block SUPERVISAO",
                Comment = "supervisão de corrente",
                Params = iface,
                Values = new Dictionary<string, string>
                {
                    { "CORRENTE", "DB GLOBAL.AREA.INSTR.STS_VALOR" },
                    { "TEMPO", "T#3S" },
                    { "PARTIDAS", "AG-05_STS_PARTIDAS" },
                    { "DADOS", "DB GLOBAL.AREA.AG_05" },
                },
            };

            var doc = Fc(2);
            BlockEdit.InsertCallInXml(doc, spec(), -1);
            Check(BlockEdit.CountNetworks(doc) == 3, "rede nova entrou");
            var unit = doc.Descendants().Last(e => e.Name.LocalName == "SW.Blocks.CompileUnit");
            var call = unit.Descendants().Single(e => e.Name.LocalName == "CallInfo");
            Check((string)call.Attribute("Name") == "FB SUPERVISAO"
                && (string)call.Attribute("BlockType") == "FB", "CallInfo aponta o FB");
            Check(call.Elements().Count(e => e.Name.LocalName == "Parameter") == 4,
                "todos os pinos declarados, mesmo os não ligados");
            Check(call.Elements().Where(e => e.Name.LocalName == "Parameter")
                .Any(e => (string)e.Attribute("Type") == "AgitadorDados"), "UDT sai sem as aspas do XML");

            var accesses = unit.Descendants().Where(e => e.Name.LocalName == "Access").ToList();
            Check(accesses.Count == 4, "1 Access por valor, veio " + accesses.Count);
            Check(accesses.Any(a => (string)a.Attribute("Scope") == "TypedConstant"), "T#3S vira constante");
            Check(accesses.First(a => (string)a.Attribute("Scope") == "GlobalVariable")
                .Descendants().Count(e => e.Name.LocalName == "Component") == 4,
                "caminho de DB vira 4 Components");

            var wires = unit.Descendants().Where(e => e.Name.LocalName == "Wire").ToList();
            Check(wires.Count == 5, "1 wire de powerrail + 4 de pino, veio " + wires.Count);
            Check(wires[0].Elements().Any(e => e.Name.LocalName == "Powerrail"), "EN sai do powerrail");
            var outWire = wires.First(w => w.Descendants().Any(e => e.Name.LocalName == "NameCon"
                && (string)e.Attribute("Name") == "PARTIDAS"));
            Check(outWire.Elements().First().Name.LocalName == "NameCon",
                "saída liga pino → Access (ordem invertida em relação à entrada)");

            // FlgNet tem que ficar no namespace v5, senão o import recusa
            Check(unit.Descendants().First(e => e.Name.LocalName == "FlgNet").Name.NamespaceName
                .EndsWith("/FlgNet/v5"), "FlgNet no namespace v5");

            var first = Fc(2);
            BlockEdit.InsertCallInXml(first, spec(), 0);
            Check(first.Descendants().First(e => e.Name.LocalName == "SW.Blocks.CompileUnit")
                .Descendants().Any(e => e.Name.LocalName == "CallInfo"), "--after 0 entra na frente");

            var middle = Fc(3);
            BlockEdit.InsertCallInXml(middle, spec(), 1);
            Check(middle.Descendants().Where(e => e.Name.LocalName == "SW.Blocks.CompileUnit")
                .ToList()[1].Descendants().Any(e => e.Name.LocalName == "CallInfo"), "--after 1 vira rede 2");

            var empty = Fc(0);
            BlockEdit.InsertCallInXml(empty, spec(), -1);
            Check(BlockEdit.CountNetworks(empty) == 1, "FC sem rede nenhuma ganha a primeira");
        }

        private static void BlockEdit_RemoveNetworkFromXml()
        {
            var doc = Fc(3);
            string title = BlockEdit.RemoveNetworkFromXml(doc, 2);
            Check(title == "Rede 2", "título da rede removida volta ('" + title + "')");
            Check(BlockEdit.CountNetworks(doc) == 2, "sobraram 2 redes");
            Check(!doc.Descendants().Any(e => e.Name.LocalName == "Text" && e.Value == "Rede 2"),
                "a rede certa saiu");
            Check(Throws(() => BlockEdit.RemoveNetworkFromXml(Fc(2), 3)), "índice fora da faixa falha");
            Check(Throws(() => BlockEdit.RemoveNetworkFromXml(Fc(2), 0)), "índice 0 falha (a numeração é 1-based)");
        }

        private static void BlockEdit_SetRetainInXml()
        {
            var doc = Fc(1);
            Check(BlockEdit.SetRetainInXml(doc, "HORIMETRO", true) == "NonRetain", "valor anterior volta");
            Check(BlockEdit.RetainOf(doc, "HORIMETRO") == "Retain", "membro ficou retentivo");
            Check(BlockEdit.SetRetainInXml(doc, "HORIMETRO", false) == "Retain", "--off desfaz");
            Check(BlockEdit.RetainOf(doc, "HORIMETRO") == "NonRetain", "voltou a NonRetain");
            Check(Throws(() => BlockEdit.SetRetainInXml(doc, "NAO_EXISTE", true)), "membro inexistente falha");
        }

        private static void Clone_InstancesInXml()
        {
            var doc = XDocument.Parse("<Document><Call><CallInfo Name=\"FB FALHA\" BlockType=\"FB\">"
                + "<Instance Scope=\"GlobalVariable\"><Component Name=\"FB FALHA_AG-05\" /></Instance>"
                + "</CallInfo></Call><Call><CallInfo Name=\"FB TIMER\" BlockType=\"FB\">"
                + "<Instance Scope=\"LocalVariable\"><Component Name=\"MULTI\" /></Instance>"
                + "</CallInfo></Call><Call><CallInfo Name=\"FC AUX\" BlockType=\"FC\" /></Call></Document>");
            var found = Clone.InstancesInXml(doc);
            Check(found.Count == 1, "multi-instância e FC ficam de fora, veio " + found.Count);
            Check(found[0].Key == "FB FALHA" && found[0].Value == "FB FALHA_AG-05", "par (FB, iDB)");
        }

        private static void Audit_DriveShape()
        {
            var comInversor = new[] { "PARTIDA_MOTOR_1 (M-01)", "FB FALHA_M-01", "FB CONDIÇÃO DE PARTIDA_M-01",
                "SINA_SPEED_TLG20_M-01", "FB SETPOINT MANUAL M-01", "FB SETPOINT ESCALONAMENTO M-01" };
            var partidaDireta = new[] { "PARTIDA_AGITADOR_5 (AG-05)", "FB FALHA_AG-05",
                "FB CONDIÇÃO DE PARTIDA_AG-05", "FB SUPERVISAO AGITADOR_AG-05" };
            Check(Audit.HasInverter(comInversor), "telegrama/setpoint marca acionamento com inversor");
            Check(!Audit.HasInverter(partidaDireta), "partida direta não tem marca de inversor");
            Check(Audit.MissingCore(partidaDireta).Count == 0, "partida direta com o trio passa");
            Check(Audit.MissingCore(new[] { "PARTIDA_X (X-01)" }).Count == 2, "sem FALHA nem CONDIÇÃO reprova");
        }

        private static void Ops_Squash()
        {
            Check(Ops.Squash("FB FILTRO DE AMOSTRAGEM  ANALÍTICA") == Ops.Squash("fb filtro de amostragem analitica"),
                "acento, caixa e espaço duplo não contam");
            Check(Ops.Squash("FB LIMITES_OPERACAO_SENSOR") == Ops.Squash("FB LIMITES OPERACAO SENSOR"),
                "underscore vale espaço");
            Check(Ops.Squash("FB FALHA") != Ops.Squash("FB FALHAS"), "nome diferente continua diferente");
        }

        private static bool Throws(Action a)
        {
            try { a(); return false; } catch { return true; }
        }
    }
}
