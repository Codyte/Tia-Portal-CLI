using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>
    /// Config for standardize-tags (port of "Padronizador de Variável").
    /// All defaults mirror the original script; keyword/comment values are domain data (PT).
    /// </summary>
    public class StandardizeConfig
    {
        public string RootFolder { get; set; } = "3. Partidas";

        /// <summary>Memory set name -> start byte of its %M area.</summary>
        public Dictionary<string, int> MemorySets { get; set; } = new Dictionary<string, int>
        {
            { "MEDIDOR", 1000 }, { "MOTOR_", 2000 }, { "BOMBA", 3000 }, { "AGITADOR", 5000 },
            { "SOPRADOR", 5500 }, { "EXAUSTOR", 6000 }, { "MISTURADOR", 7000 }, { "ROSCA", 7500 },
            { "PENEIRA", 8000 }, { "VALVULA_MOTORIZADA", 9000 }, { "VALVULA", 9500 }, { "INSTRUMENTOS", 10000 },
        };

        /// <summary>Table-name keyword -> memory set. First match wins (insertion order).</summary>
        public Dictionary<string, string> SetMapping { get; set; } = new Dictionary<string, string>
        {
            { "MEDIDOR", "MEDIDOR" }, { "MOTOR_", "MOTOR_" }, { "BOMBA", "BOMBA" },
            { "AGITADOR", "AGITADOR" }, { "SOPRADOR", "SOPRADOR" }, { "EXAUSTOR", "EXAUSTOR" },
            { "MISTURADOR", "MISTURADOR" }, { "ROSCA", "ROSCA" },
            { "PENEIRA", "PENEIRA" }, { "VALVULA_SOLENOIDE", "VALVULA" }, { "VALVULA_LIMPEZA", "VALVULA" },
            { "VALVULA_MOTORIZADA", "VALVULA" }, { "VALVULA_ELETRICA", "VALVULA" },
            { "INSTRUMENTOS", "INSTRUMENTOS" },
        };

        /// <summary>Tag-name keyword -> functional prefix (CMD/STS/PV/SP...). First match wins.</summary>
        public List<PrefixMapping> PrefixMappings { get; set; } = new List<PrefixMapping>
        {
            new PrefixMapping { Keyword = "HORIMETRO", Prefix = "HORIMETRO" }, new PrefixMapping { Keyword = "TOTALIZA", Prefix = "TOTALIZACAO" },
            new PrefixMapping { Keyword = "SETPOINT", Prefix = "SP" }, new PrefixMapping { Keyword = "LIGA_", Prefix = "CMD" }, new PrefixMapping { Keyword = "FALHA_TORQUE", Prefix = "STS" },
            new PrefixMapping { Keyword = "RESETA_", Prefix = "CMD" }, new PrefixMapping { Keyword = "RESET_", Prefix = "CMD" },
            new PrefixMapping { Keyword = "ABERTA", Prefix = "STS" }, new PrefixMapping { Keyword = "FECHADA", Prefix = "STS" },
            new PrefixMapping { Keyword = "ABRE", Prefix = "CMD" }, new PrefixMapping { Keyword = "FECHA", Prefix = "CMD" },
            new PrefixMapping { Keyword = "LIGADO", Prefix = "STS" }, new PrefixMapping { Keyword = "FALHA", Prefix = "STS" },
            new PrefixMapping { Keyword = "MODO_", Prefix = "STS" }, new PrefixMapping { Keyword = "OK", Prefix = "STS" },
            new PrefixMapping { Keyword = "AUTO", Prefix = "STS" }, new PrefixMapping { Keyword = "MANUAL", Prefix = "STS" },
            new PrefixMapping { Keyword = "REMOTO", Prefix = "STS" }, new PrefixMapping { Keyword = "LOCAL", Prefix = "STS" },
            new PrefixMapping { Keyword = "NIVEL", Prefix = "PV" }, new PrefixMapping { Keyword = "VAZAO", Prefix = "PV" },
            new PrefixMapping { Keyword = "CORRENTE", Prefix = "PV" }, new PrefixMapping { Keyword = "VELOCIDADE", Prefix = "PV" },
            new PrefixMapping { Keyword = "PRESSAO", Prefix = "PV" }, new PrefixMapping { Keyword = "TURBIDEZ", Prefix = "PV" },
            new PrefixMapping { Keyword = "LEITURA", Prefix = "STS" }, new PrefixMapping { Keyword = "INDICAÇÃO", Prefix = "STS" },
            new PrefixMapping { Keyword = "FEEDBACK", Prefix = "STS" },
        };

        /// <summary>Tag-name keyword -> comment template ({ID} replaced). Longest key wins.</summary>
        public Dictionary<string, string> CommentMappings { get; set; } = new Dictionary<string, string>
        {
            { "FALHA_TORQUE_ABERTURA", "Status: Falha de Abertura do equipamento {ID}" },
            { "FALHA_TORQUE_FECHAMENTO", "Status: Falha de Fechamento do equipamento {ID}" },
            { "FALHA_NAO_LIGOU", "Status: Falha de partida do equipamento {ID}" },
            { "FALHA_PROFINET", "Status: Falha de comunicação Profinet com o equipamento {ID}" },
            { "LIGADO_PROFINET", "Status: Comunicação Profinet OK com o equipamento {ID}" },
            { "FALHA", "Status: Falha geral do equipamento {ID}" },
            { "LIGADO", "Status: Equipamento {ID} em operação" },
            { "HABILITA", "Status: Condições para partida do equipamento {ID} estão OK" },
            { "MODO_LOCAL", "Status: Chave seletora do equipamento {ID} em Local" },
            { "MODO_REMOTO", "Status: Chave seletora do equipamento {ID} em Remoto" },
            { "LOCAL_AUTO", "Status: Comando Local em modo Automático para o equipamento {ID}" },
            { "LOCAL_MANUAL", "Status: Comando Local em modo Manual para o equipamento {ID}" },
            { "REMOTO_AUTO", "Status: Comando Remoto em modo Automático para o equipamento {ID}" },
            { "REMOTO_MANUAL", "Status: Comando Remoto em modo Manual para o equipamento {ID}" },
            { "SUCCAO_OK", "Status: Condição de sucção OK para a bomba {ID}" },
            { "VAR_MONITORADA", "Status: Variável monitorada (Ex: vibração, temperatura) do equipamento {ID}" },
            { "BOTAO_RESET_FALHA", "Comando: Botão de reset de falhas para o equipamento {ID}" },
            { "HABILITA_LIGA_AUTO", "Comando: Habilitação para partida em modo automático para o equipamento {ID}" },
            { "LIGA_BOMBA", "Comando: Ligar/Partir a bomba {ID}" },
            { "LIGA_MOTOR", "Comando: Ligar/Partir o motor {ID}" },
            { "CMD_RESET_LIGA", "Comando: Reset do comando Ligar o equipamento {ID}" },
            { "LIGA_PROFINET", "Comando: Partida via Profinet para o equipamento {ID}" },
            { "RESET_FALHA_PROFINET", "Comando: Reset de falha Profinet para o equipamento {ID}" },
            { "SETPOINT_AUTO", "Setpoint: Valor de referência em modo automático para o equipamento {ID}" },
            { "SETPOINT_MANUAL", "Setpoint: Valor de referência em modo manual para o equipamento {ID}" },
            { "SP_CADASTRO_LIGA", "Setpoint: Tempo de cadastro para comando de LIGA do equipamento {ID}" },
            { "SP_CADASTRO_DESLIGA", "Setpoint: Tempo de cadastro para comando de DESLIGA do equipamento {ID}" },
            { "SP_CADASTRO_TEMPO_LIGADO_AUTO", "Setpoint: Tempo programado de ligado automático do equipamento {ID}" },
            { "SP_CADASTRO_TEMPO_DESLIGADO_AUTO", "Setpoint: Tempo programado de desligado automático do equipamento {ID}" },
            { "SP_TEMPO_LIGADO_AUTO", "Setpoint: Tempo total de operação automática (ligado) do equipamento {ID}" },
            { "SP_TEMPO_DESLIGADO_AUTO", "Setpoint: Tempo total de espera automática (desligado) do equipamento {ID}" },
            { "SP_TEMPO_ROTATIVIDADE", "Setpoint: Tempo de rotatividade entre partidas do equipamento {ID}" },
            { "SP_TEMPO_ESPERA_AUTO", "Setpoint: Tempo de espera para nova tentativa automática do equipamento {ID}" },
            { "RESERVA", "Tag de reserva para futuras implementações" },
            { "_STS_SEM_", "Alarme: Sem Comunicação com Equipamento {ID}" },
            { "LEITURA_MUITO_ALTA", "Alarme: Leitura Muito Alta do Equipamento {ID}" },
            { "LEITURA_ALTA", "Aviso: Leitura Alta do Equipamento {ID}" },
            { "LEITURA_BAIXA", "Aviso: Leitura Baixa do Equipamento {ID}" },
            { "LEITURA_MUITO_BAIXA", "Alarme: Leitura Muito Baixa do Equipamento {ID}" },
            { "PORTA_ABERTA", "Alerta: Porta do painel {ID} aberta" },
            { "RELE_FALTA_DE_FASE", "Alarme: Falta de Fase no {ID}" },
            { "BOTAO_DE_EMERGENCIA_ATUADO", "Alarme: Botão de Emergência no {ID} atuado" },
            { "DISJUNTOR_GERAL_ATUADO", "Alarme: Disjuntor Geral do {ID} Atuado" },
            { "ENERGIZADO", "Status: Quadro de Automação {ID} Energizado" },
            { "FONTE_1_24V_OK", "Status: Fonte 1 (24V) do {ID} OK" },
            { "FONTE_2_24V_OK", "Status: Fonte 2 (24V) do {ID} OK" },
            { "INDICACAO_DPS_01_ATUADO", "Alarme: Indicação do DPS 01 do {ID} Atuado" },
            { "INDICACAO_DPS_02_ATUADO", "Alarme: Indicação do DPS 02 do {ID} Atuado" },
            { "INDICACAO_DPS_03_ATUADO", "Alarme: Indicação do DPS 03 do {ID} Atuado" },
            { "INDICACAO_DPS_04_ATUADO", "Alarme: Indicação do DPS 04 do {ID} Atuado" },
            { "MODULO_DE_REDUNDANCIA", "Falha: Módulo de Redundância do {ID}" },
            { "MODULO_UPS_24V_OK", "Status: Módulo UPS (24V) do {ID} OK" },
            { "MODULO_UPS_BAT_85%", "Alerta: Carga da bateria UPS do {ID} abaixo de 85%" },
            { "RESET_REMOTA", "Comando: Reset Geral dos Equipamentos Referentes ao {ID}" },
            { "CMD_ABRE", "Comando: Comando de Abertura do Equipamento {ID}" },
            { "CMD_VALVULA_ABRE", "Comando: Comando de Abertura da Válvula {ID}" },
            { "CMD_VALVULA_FECHA", "Comando: Comando de Fechamento da Válvula {ID}" },
            { "STS_VALVULA_FECHADA", "Status: Feedback da Válvula {ID} Fechada" },
            { "STS_VALVULA_ABERTA", "Status: Feedback da Válvula {ID} Aberta" },
            { "VALVULA_FEEDBACK_POSICAO", "Status: Feedback da Atual Posição da Válvula {ID}" },
        };

        /// <summary>Alarm suffixes sorted before everything else, in this order.</summary>
        public List<string> AlarmOrder { get; set; } = new List<string>
        {
            "LEITURA_MUITO_ALTA", "LEITURA_ALTA", "LEITURA_BAIXA", "LEITURA_MUITO_BAIXA", "STS_SEM_4MA",
        };
    }

    public class PrefixMapping
    {
        public string Keyword { get; set; }
        public string Prefix { get; set; }
    }

    internal class TagTemplate
    {
        public string Name;
        public string DataTypeName;
    }

    /// <summary>Alphanumeric sort that orders embedded numbers numerically (10 after 2).</summary>
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (string.Equals(x, y, StringComparison.OrdinalIgnoreCase)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            string[] xp = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
            string[] yp = Regex.Split(y.Replace(" ", ""), "([0-9]+)");
            int min = Math.Min(xp.Length, yp.Length);
            for (int i = 0; i < min; i++)
            {
                if (i % 2 == 0)
                {
                    int c = string.Compare(xp[i], yp[i], StringComparison.OrdinalIgnoreCase);
                    if (c != 0) return c;
                }
                else
                {
                    int.TryParse(xp[i], out int nx);
                    int.TryParse(yp[i], out int ny);
                    if (nx != ny) return nx.CompareTo(ny);
                }
            }
            return x.Length.CompareTo(y.Length);
        }
    }

    /// <summary>Sorts alarm-suffix tags in a fixed order, everything else naturally.</summary>
    public class AlarmTagComparer : IComparer<string>
    {
        private readonly List<string> _order;
        private readonly NaturalStringComparer _fallback = new NaturalStringComparer();

        public AlarmTagComparer(List<string> order)
        {
            _order = order ?? new List<string>();
        }

        public int Compare(string x, string y)
        {
            string kx = Key(x), ky = Key(y);
            int ix = _order.IndexOf(kx), iy = _order.IndexOf(ky);
            if (ix != -1 && iy != -1) return ix.CompareTo(iy);
            return _fallback.Compare(x, y);
        }

        private string Key(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return string.Empty;
            // longest key first so "LEITURA_MUITO_ALTA" wins over "LEITURA_ALTA"
            foreach (var key in _order.OrderByDescending(s => s.Length))
                if (tagName.Contains(key)) return key;
            return tagName;
        }
    }

    /// <summary>Sequential %M allocator: BOOLs bit-packed, larger types byte/word/dword aligned.</summary>
    public class AddressAllocator
    {
        private int _byte;
        private int _bit;
        public int CurrentByte => _byte;
        public int CurrentBit => _bit;

        public AddressAllocator(int startByte)
        {
            _byte = startByte;
        }

        /// <summary>Next %M address for the data type; null for unsupported types.</summary>
        public string Next(string dataTypeName)
        {
            string type = dataTypeName?.ToUpper() ?? "BOOL";
            if (type == "BOOL")
            {
                if (_bit > 7) { _bit = 0; _byte++; }
                string a = "%M" + _byte + "." + _bit;
                _bit++;
                return a;
            }
            if (_bit > 0) { _byte++; _bit = 0; }
            string addr;
            int size;
            switch (type)
            {
                case "BYTE": addr = "%MB" + _byte; size = 1; break;
                case "WORD": case "UINT": case "INT": addr = "%MW" + _byte; size = 2; break;
                case "DWORD": case "DINT": case "REAL": addr = "%MD" + _byte; size = 4; break;
                default: addr = null; size = 0; break;
            }
            _byte += size;
            return addr;
        }
    }

    /// <summary>Hands out %M blocks per memory set, rounding the next start up to a multiple of 10.</summary>
    internal class MemoryManager
    {
        private readonly Dictionary<string, double> _next;

        public MemoryManager(Dictionary<string, int> sets)
        {
            _next = sets.ToDictionary(k => k.Key, k => (double)k.Value);
        }

        public int AllocateBlock(string set, List<TagTemplate> tags)
        {
            if (!_next.ContainsKey(set)) set = "INSTRUMENTOS";
            if (!_next.ContainsKey(set))
                throw new ArgumentException("Memory set '" + set + "' missing from MemorySets (and no INSTRUMENTOS fallback).");
            double start = _next[set];
            var probe = new AddressAllocator((int)start);
            foreach (var t in tags) probe.Next(t.DataTypeName);
            probe.Next("Bool");
            probe.Next("Bool");
            probe.Next("DInt");
            double end = probe.CurrentByte;
            double offset = end - start;
            if (probe.CurrentBit > 0) offset = Math.Ceiling(end - start + probe.CurrentBit / 8.0);
            offset += 4.0;
            _next[set] = Math.Ceiling((start + offset) / 10.0) * 10.0;
            return (int)start;
        }
    }

    /// <summary>
    /// Standardizes tag tables under a root folder: names get ID + functional prefix, addresses
    /// re-allocated per memory set, comments regenerated; empty tables filled from the best
    /// earlier template (port of "Padronizador de Variável").
    /// </summary>
    public static class Standardize
    {
        public static object Run(PlcSoftware plc, StandardizeConfig config, bool apply)
        {
            var warnings = new List<string>();
            var mem = new MemoryManager(config.MemorySets);
            var golden = new Dictionary<string, List<TagTemplate>>();
            var alarmComparer = new AlarmTagComparer(config.AlarmOrder);

            var root = Profinet.FindTagGroup(plc.TagTableGroup, config.RootFolder);
            if (root == null)
                throw new ArgumentException("Root tag folder '" + config.RootFolder + "' not found.");

            var tables = new List<PlcTagTable>();
            CollectTables(root, tables);

            var actions = new List<object>();
            foreach (var table in tables)
            {
                string subtype = GetSubtype(table.Name);
                if (string.IsNullOrEmpty(subtype))
                {
                    warnings.Add("Could not determine subtype of table '" + table.Name + "'. Skipped.");
                    actions.Add(TableAction(table.Name, subtype, "skip-no-subtype"));
                    continue;
                }

                try
                {
                    if (golden.ContainsKey(subtype))
                    {
                        if (!table.Tags.Any())
                            actions.Add(GenerateFromTemplate(table, subtype, golden[subtype], tables, config, mem, alarmComparer, warnings, apply));
                        else
                            actions.Add(AuditTable(table, subtype, config, mem, alarmComparer, warnings, apply));
                    }
                    else if (!table.Tags.Any())
                    {
                        // first exemplar of the subtype and it is empty: memorize an empty mold
                        golden[subtype] = new List<TagTemplate>();
                        actions.Add(TableAction(table.Name, subtype, "memorize-empty-mold"));
                    }
                    else
                    {
                        // first exemplar with content: audit it, then memorize it as the golden mold
                        actions.Add(AuditTable(table, subtype, config, mem, alarmComparer, warnings, apply));
                        golden[subtype] = TemplatesOf(table, alarmComparer);
                    }
                }
                catch (Exception ex)
                {
                    // a failed table must not abort the run mid-way (may already be half-rebuilt)
                    warnings.Add("Table '" + table.Name + "': " + ex.Message + ". Continuing with next table.");
                    actions.Add(TableAction(table.Name, subtype, "error"));
                }
            }

            return new Dictionary<string, object>
            {
                { "rootFolder", config.RootFolder },
                { "tablesFound", tables.Count },
                { "applied", apply },
                { "tables", actions },
                { "warnings", warnings },
            };
        }

        private static Dictionary<string, object> GenerateFromTemplate(PlcTagTable emptyTable, string subtype,
            List<TagTemplate> goldenMold, List<PlcTagTable> allTables, StandardizeConfig config,
            MemoryManager mem, AlarmTagComparer alarmComparer, List<string> warnings, bool apply)
        {
            var best = FindBestTemplate(emptyTable, allTables);
            List<TagTemplate> mold;
            string source;
            if (best != null) { mold = TemplatesOf(best, alarmComparer); source = best.Name; }
            else if (goldenMold.Any()) { mold = goldenMold; source = "golden:" + subtype; }
            else
            {
                warnings.Add("No compatible template found for empty table '" + emptyTable.Name + "'. Skipped.");
                return TableAction(emptyTable.Name, subtype, "skip-no-template");
            }

            string id = ExtractId(emptyTable.Name);
            if (id == null)
            {
                warnings.Add("Could not extract (ID) from table name '" + emptyTable.Name + "'. Skipped.");
                return TableAction(emptyTable.Name, subtype, "skip-no-id");
            }

            // copy + rename with the target id, then re-sort: mold is shared (golden) and the
            // generated table must come out already in its own audited order
            mold = mold
                .Select(t => new TagTemplate { Name = StandardizeName(t.Name, id, config), DataTypeName = t.DataTypeName })
                .OrderBy(t => t.Name, alarmComparer)
                .ToList();

            string set = IdentifySet(emptyTable.Name, config);
            int startByte = mem.AllocateBlock(set, mold);
            var allocator = new AddressAllocator(startByte);
            var tags = new List<object>();
            foreach (var t in mold)
            {
                string name = StandardizeName(t.Name, id, config);
                string address = allocator.Next(t.DataTypeName);
                tags.Add(new Dictionary<string, object> { { "tag", name }, { "type", t.DataTypeName }, { "address", address } });
                if (apply) CreateTag(emptyTable, name, t.DataTypeName, address, GenerateComment(name, id, config), warnings);
            }
            AddReserves(emptyTable, id, allocator, tags, warnings, apply);

            var action = TableAction(emptyTable.Name, subtype, apply ? "generated" : "generate");
            action["templateSource"] = source;
            action["memorySet"] = set;
            action["startAddress"] = "%M" + startByte + ".0";
            action["tags"] = tags;
            return action;
        }

        private static Dictionary<string, object> AuditTable(PlcTagTable table, string subtype,
            StandardizeConfig config, MemoryManager mem, AlarmTagComparer alarmComparer,
            List<string> warnings, bool apply)
        {
            string masterId = ExtractId(table.Name);
            if (masterId == null)
            {
                warnings.Add("Table '" + table.Name + "' does not follow the '(ID)' naming pattern. Skipped.");
                return TableAction(table.Name, subtype, "skip-no-id");
            }

            // standardize names BEFORE ordering: the next audit sorts the renamed tags, so the
            // layout must be allocated in renamed order or apply never converges
            var ideal = TemplatesOf(table, alarmComparer)
                .Select(t => new TagTemplate { Name = StandardizeName(t.Name, masterId, config), DataTypeName = t.DataTypeName })
                .OrderBy(t => t.Name, alarmComparer)
                .ToList();
            string set = IdentifySet(table.Name, config);
            int startByte = mem.AllocateBlock(set, ideal);

            // conformity probe: every ideal (name, address) pair must already exist
            var probe = new AddressAllocator(startByte);
            var current = table.Tags.ToDictionary(t => t.Name);
            bool rebuild = false;
            foreach (var t in ideal)
            {
                string name = StandardizeName(t.Name, masterId, config);
                string address = probe.Next(t.DataTypeName);
                if (!current.ContainsKey(name) || current[name].LogicalAddress?.ToString() != address)
                {
                    rebuild = true;
                    break;
                }
            }

            var action = TableAction(table.Name, subtype, null);
            action["memorySet"] = set;
            action["startAddress"] = "%M" + startByte + ".0";

            if (rebuild)
            {
                var tags = new List<object>();
                if (apply)
                {
                    foreach (var old in table.Tags.ToList()) old.Delete();
                }
                var allocator = new AddressAllocator(startByte);
                foreach (var t in ideal)
                {
                    string name = StandardizeName(t.Name, masterId, config);
                    string address = allocator.Next(t.DataTypeName);
                    tags.Add(new Dictionary<string, object> { { "tag", name }, { "type", t.DataTypeName }, { "address", address } });
                    if (apply) CreateTag(table, name, t.DataTypeName, address, GenerateComment(name, masterId, config), warnings);
                }
                AddReserves(table, masterId, allocator, tags, warnings, apply);
                action["action"] = apply ? "rebuilt" : "rebuild";
                action["tags"] = tags;
                return action;
            }

            // names and addresses conform: only sync comments
            int commentFixes = 0;
            foreach (var tag in table.Tags.Where(t => !t.Name.ToUpper().Contains("RESERVA")))
            {
                string idealComment = GenerateComment(tag.Name, masterId, config);
                var item = tag.Comment.Items.FirstOrDefault();
                if ((item?.Text ?? "") != idealComment)
                {
                    commentFixes++;
                    if (apply && item != null) item.Text = idealComment;
                }
            }
            action["action"] = commentFixes == 0 ? "ok" : (apply ? "comments-updated" : "update-comments");
            action["commentFixes"] = commentFixes;
            return action;
        }

        private static void AddReserves(PlcTagTable table, string id, AddressAllocator allocator,
            List<object> tags, List<string> warnings, bool apply)
        {
            var reserves = new[]
            {
                new { Name = id + "_RESERVA_01", Type = "Bool" },
                new { Name = id + "_RESERVA_02", Type = "Bool" },
                new { Name = id + "_RESERVA_DINT_01", Type = "DInt" },
            };
            foreach (var r in reserves)
            {
                string address = allocator.Next(r.Type);
                tags.Add(new Dictionary<string, object> { { "tag", r.Name }, { "type", r.Type }, { "address", address } });
                if (apply) CreateTag(table, r.Name, r.Type, address, "Tag de reserva para futuras implementações", warnings);
            }
        }

        private static void CreateTag(PlcTagTable table, string name, string type, string address,
            string comment, List<string> warnings)
        {
            try
            {
                var tag = table.Tags.Create(name, type, address);
                var item = tag.Comment.Items.FirstOrDefault();
                if (item != null) item.Text = comment;
            }
            catch (Exception ex)
            {
                warnings.Add("Failed to create tag '" + name + "' in '" + table.Name + "': " + ex.Message);
            }
        }

        private static Dictionary<string, object> TableAction(string table, string subtype, string action)
        {
            return new Dictionary<string, object> { { "table", table }, { "subtype", subtype }, { "action", action } };
        }

        private static List<TagTemplate> TemplatesOf(PlcTagTable table, AlarmTagComparer comparer)
        {
            return table.Tags
                .Where(t => !t.Name.ToUpper().Contains("RESERVA"))
                .OrderBy(t => t.Name, comparer)
                .Select(t => new TagTemplate { Name = t.Name, DataTypeName = t.DataTypeName })
                .ToList();
        }

        /// <summary>Best earlier non-empty table whose subtype shares the most words with the target's.</summary>
        private static PlcTagTable FindBestTemplate(PlcTagTable target, List<PlcTagTable> allTables)
        {
            string clean = GetSubtype(target.Name);
            var terms = clean.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            string[] targetWords = clean.Split('_');
            int targetIndex = allTables.IndexOf(target);
            while (terms.Any())
            {
                string search = string.Join("_", terms);
                var candidates = allTables.Take(targetIndex)
                    .Where(t => t.Tags.Any() && GetSubtype(t.Name).ToUpper().Contains(search.ToUpper()));
                PlcTagTable bestMatch = null;
                int bestScore = -1;
                foreach (var candidate in candidates)
                {
                    string[] words = GetSubtype(candidate.Name).Split('_');
                    int score = targetWords.Intersect(words, StringComparer.OrdinalIgnoreCase).Count();
                    if (score > bestScore) { bestScore = score; bestMatch = candidate; }
                }
                if (bestMatch != null) return bestMatch;
                terms.RemoveAt(terms.Count - 1);
            }
            return null;
        }

        private static void CollectTables(PlcTagTableGroup group, List<PlcTagTable> into)
        {
            var natural = new NaturalStringComparer();
            into.AddRange(group.TagTables.OrderBy(t => t.Name, natural));
            foreach (var sub in group.Groups.Cast<PlcTagTableUserGroup>().OrderBy(g => g.Name, natural))
                CollectTables(sub, into);
        }

        /// <summary>ID_FUNCTION -> masterId_PREFIX_FUNCTION per the prefix mappings.</summary>
        private static string StandardizeName(string original, string masterId, StandardizeConfig config)
        {
            var knownPrefixes = new HashSet<string>(
                config.PrefixMappings.Select(m => m.Prefix), StringComparer.OrdinalIgnoreCase);
            var functional = original.Split('_')
                .Where(p => !IsIdentifierPattern(p) && !knownPrefixes.Contains(p))
                .ToList();
            string pure = string.Join("_", functional);
            pure = Regex.Replace(pure, @"\(\d+\)$", "").Trim();
            if (string.IsNullOrWhiteSpace(pure)) pure = "FUNCAO_DESCONHECIDA";
            foreach (var map in config.PrefixMappings)
            {
                if (pure.ToUpper().Contains(map.Keyword))
                {
                    if (pure.Equals(map.Keyword, StringComparison.OrdinalIgnoreCase))
                        return masterId + "_" + pure;
                    return masterId + "_" + map.Prefix + "_" + pure;
                }
            }
            return masterId + "_STS_" + pure;
        }

        /// <summary>Equipment-tag shape like ETA-B01 or FIT-100A.</summary>
        private static bool IsIdentifierPattern(string part)
        {
            return Regex.IsMatch(part, @"^[A-Z0-9]+(-[A-Z0-9]+)+[A-Z]?$");
        }

        private static string GenerateComment(string tagName, string id, StandardizeConfig config)
        {
            foreach (var kvp in config.CommentMappings.OrderByDescending(x => x.Key.Length))
                if (tagName.ToUpper().Contains(kvp.Key))
                    return kvp.Value.Replace("{ID}", id);
            return "Tag de controle/status/falha para o equipamento " + id;
        }

        private static string IdentifySet(string tableName, StandardizeConfig config)
        {
            foreach (var kvp in config.SetMapping)
                if (tableName.ToUpper().Contains(kvp.Key)) return kvp.Value;
            return "INSTRUMENTOS";
        }

        /// <summary>Table name minus "(ID)", trailing _PRINCIPAL/_RESERVA/_N; '-' -> '_'.</summary>
        private static string GetSubtype(string tableName)
        {
            string s = Regex.Replace(tableName, @"\s*\([^)]*\)", "").Trim();
            s = Regex.Replace(s, @"_PRINCIPAL$|_RESERVA$|_\d+$", "", RegexOptions.IgnoreCase).Trim();
            return s.Replace('-', '_');
        }

        /// <summary>The "(ID)" part of a table name, or null.</summary>
        private static string ExtractId(string tableName)
        {
            var match = Regex.Match(tableName, @"\(([^)]+)\)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}
