// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L27    class Motion
//   L31    .List
//   L43    .Collect
//   L67    .Parameters
//   L80    .Safe
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.TechnologicalObjects;

namespace Tia.Core
{
    /// <summary>
    /// Objetos tecnológicos de movimento (eixo, came, cinemática) — read-only.
    ///
    /// Por que só leitura: a composição do Openness (`TechnologicalInstanceDBComposition`) não expõe
    /// Create, e `TechnologicalParameter.Value` é read-only — `set_Value` levanta
    /// `EngineeringNotSupportedException`, e `SetAttribute("Value", x)` cai no mesmo setter
    /// (medido 2026-08-21 num PID_Compact V3.0; `docs/LIMITES.md`). Não há verbo de escrita aqui. TO nasce na GUI ou vem junto no import de projeto/biblioteca; o que o CLI faz é dizer
    /// o que existe, de que tipo, em que versão e com que parâmetros — que é o que um agente precisa
    /// antes de escrever `MC_*`. O vínculo TO↔drive se faz por
    /// `AxisEncoderHardwareConnection.Connect(Telegram)` (assembly Startdrive), fora deste verbo.
    /// </summary>
    public static class Motion
    {
        /// <summary>--like filtra por nome ou tipo (`TO_PositioningAxis`); --params inclui os
        /// parâmetros do TO, que são centenas por eixo — sem ele sai só o cabeçalho.</summary>
        public static object List(TiaSession session, PlcSoftware plc, string like, bool withParams)
        {
            var objects = new List<object>();
            Collect(plc.TechnologicalObjectGroup, "", like, withParams, objects);
            return new Dictionary<string, object>
            {
                { "plc", plc.Name },
                { "count", objects.Count },
                { "technologyObjects", objects },
            };
        }

        private static void Collect(TechnologicalInstanceDBGroup group, string path, string like,
            bool withParams, List<object> into)
        {
            foreach (TechnologicalInstanceDB to in group.TechnologicalObjects)
            {
                var kind = Safe(() => to.OfSystemLibElement);
                if (like != null
                    && to.Name.IndexOf(like, StringComparison.OrdinalIgnoreCase) < 0
                    && (kind == null || kind.IndexOf(like, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;
                var row = new Dictionary<string, object>
                {
                    { "folder", path },
                    { "name", to.Name },
                    { "type", kind },
                    { "version", Safe(() => Convert.ToString(to.OfSystemLibVersion)) },
                };
                if (withParams) row["parameters"] = Parameters(to);
                into.Add(row);
            }
            foreach (TechnologicalInstanceDBUserGroup sub in group.Groups)
                Collect(sub, path.Length == 0 ? sub.Name : path + "/" + sub.Name, like, withParams, into);
        }

        /// <summary>
        /// Escreve um parâmetro do TO. A doc oficial (TOOpennessenUS/.../95763532171) faz exatamente
        /// isso — `parameter.Value = value` — e avisa: parâmetro **sem acesso de escrita** levanta.
        /// Medido num PID_Compact V3.0: `Retain.CtrlParams.Gain` recusa (`set_Value ... read-only`),
        /// e a recusa só aparece na tentativa — não há atributo que declare a gravabilidade antes.
        /// Daí a mensagem traduzir o erro do Openness em vez de prometer no dry-run.
        /// É valor de projeto: chega ao PLC no download, e o TO fica inconsistente até o compile.
        /// </summary>
        public static object SetParam(TiaSession session, PlcSoftware plc, string toName,
            string parameterName, string value, bool apply)
        {
            var to = Find(plc.TechnologicalObjectGroup, toName);
            if (to == null)
                throw new InvalidOperationException("Technology object '" + toName + "' not found in '"
                    + plc.Name + "'. Run tia list-motion.");

            TechnologicalParameter found = null;
            foreach (TechnologicalParameter p in to.Parameters)
                if (string.Equals(Safe(() => p.Name), parameterName, StringComparison.OrdinalIgnoreCase))
                { found = p; break; }
            if (found == null)
                throw new InvalidOperationException("Parameter '" + parameterName + "' not found in '"
                    + to.Name + "'. Run tia list-motion --like " + to.Name + " --params.");

            object current;
            try { current = found.Value; }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Parameter '" + parameterName + "' is unreadable ("
                    + (ex.InnerException ?? ex).Message + "). Without the current value the target type "
                    + "cannot be proven — refusing to set.");
            }
            if (current == null)
                throw new InvalidOperationException("Parameter '" + parameterName
                    + "' reads null — no current value, so no type to write against.");

            var parsed = Hardware.Coerce(value, current);
            var same = Equals(parsed, current);
            var result = new Dictionary<string, object>
            {
                { "plc", plc.Name },
                { "technologyObject", to.Name },
                { "type", Safe(() => to.OfSystemLibElement) },
                { "parameter", parameterName },
                { "from", current },
                { "to", parsed },
                { "action", same ? "none (already set)" : "set" },
                { "applied", apply },
            };
            if (apply && !same)
            {
                try { found.Value = parsed; }
                catch (Exception ex)
                {
                    var message = (ex.InnerException ?? ex).Message;
                    throw new InvalidOperationException("Parameter '" + parameterName + "' of '" + to.Name
                        + "' does not provide write access: " + message
                        + " Nem todo parametro de TO e' gravavel (a doc do Openness diz que a recusa so'"
                        + " aparece na tentativa); os de configuracao aceitam, os de Retain/runtime nao.");
                }
                result["verified"] = Safe(() => Convert.ToString(found.Value, CultureInfo.InvariantCulture));
                result["note"] = "TO inconsistente ate `tia compile --apply`; valor chega ao PLC no download.";
            }
            return result;
        }

        private static TechnologicalInstanceDB Find(TechnologicalInstanceDBGroup group, string name)
        {
            foreach (TechnologicalInstanceDB to in group.TechnologicalObjects)
                if (string.Equals(to.Name, name, StringComparison.OrdinalIgnoreCase)) return to;
            foreach (TechnologicalInstanceDBUserGroup sub in group.Groups)
            {
                var hit = Find(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static List<object> Parameters(TechnologicalInstanceDB to)
        {
            var rows = new List<object>();
            foreach (TechnologicalParameter p in to.Parameters)
                rows.Add(new Dictionary<string, object>
                {
                    { "name", Safe(() => p.Name) },
                    { "value", Safe(() => Convert.ToString(p.Value)) },
                });
            return rows;
        }

        // atributo que a família de CPU não implementa lança em vez de devolver null
        private static string Safe(Func<string> read)
        {
            try { return read(); }
            catch (Exception) { return null; }
        }
    }
}
