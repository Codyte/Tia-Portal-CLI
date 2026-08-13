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
using System.Linq;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.TechnologicalObjects;

namespace Tia.Core
{
    /// <summary>
    /// Objetos tecnológicos de movimento (eixo, came, cinemática) — read-only.
    ///
    /// Por que só leitura: a composição do Openness (`TechnologicalInstanceDBComposition`) não expõe
    /// Create. TO nasce na GUI ou vem junto no import de projeto/biblioteca; o que o CLI faz é dizer
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
