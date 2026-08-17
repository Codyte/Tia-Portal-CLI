// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L65    class Sim
//   L73    .Run
//   L188   .RegisteredInstances
//   L198   .WaitReady
//   L215   .Execute
//   L225   case "write"
//   L229   case "read"
//   L233   case "wait"
//   L237   case "run"
//   L241   case "stop"
//   L245   case "state"
//   L248   case "tags"
//   L276   .Write
//   L300   .ParseBool
//   L308   .Plain
//   L329   class Target
//   L340   .FindTarget
//   L352   .Interfaces
//   L366   .DeviceItemOf
//   L393   .Resolve
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Siemens.Engineering.Connection;
using Siemens.Engineering.Download;
using Siemens.Engineering.Download.Configurations;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Online;
using Siemens.Engineering.SW;
using Siemens.Simatic.Simulation.Runtime;

namespace Tia.Core
{
    /// <summary>
    /// Executar o programa num PLC virtual (S7-PLCSIM Advanced) e observar o comportamento:
    /// escrever entrada, ler saída, avançar o tempo. Fecha o ciclo que `compile` + `audit` não
    /// fecham — os dois medem forma, este mede comportamento.
    ///
    /// A instância **não é deste processo**: registrar aqui dentro não funciona porque ela morre com
    /// o `tia.exe` (o Runtime Manager sobe in-proc, não há serviço). Quem a segura é um processo
    /// longevo da sessão 1 — o `scripts/sim-host.ps1` (task `TiaSimHost`) ou, à mão, o control panel
    /// do PLCSIM Advanced. Nada distingue os dois: o host do repo sobe o Runtime Manager sozinho e o
    /// control panel não precisa estar aberto (medido 2026-08-17, com control panel e manager mortos).
    /// Este verbo **pega emprestado**: attach, download, passos — e não desliga o que não ligou.
    ///
    /// A parede é a **sessão do Windows**, a mesma do attach do Openness: da sessão 0 o manager some
    /// da API (`SimulationRuntimeManager.Version` volta vazio e `RegisterInstance` dá
    /// `-1, InvalidErrorCode`) mesmo com ele vivo na sessão 1.
    ///
    /// **O PLCSIM clássico tem que estar fechado.** Ele toma o canal (a API do Advanced devolve
    /// `-48, CommunicationInterfaceNotAvailable`) e o access point `PLCSIM` do S7ONLINE passa a ser
    /// dele: o download sai `Success` e a instância Advanced continua vazia. Fechado o clássico, o
    /// mesmo access point é a rota do Advanced — daí `--pc-interface PLCSIM`.
    ///
    /// O download é do Openness (`DownloadProvider`), não da API de simulação: quem tem o programa é
    /// o projeto.
    /// </summary>
    public static class Sim
    {
        /// <summary>
        /// dry: enumera o que existe (instâncias registradas, interfaces de PC do download, passos do
        /// script) sem baixar nada. --apply: attach + download + passos.
        /// noDownload: pula o download e roda os passos no programa que já está na instância — é o
        /// modo de iterar observação (o download mediu ~91% do tempo do verbo: 45-52 s de 49-57 s).
        /// </summary>
        public static object Run(TiaSession session, PlcSoftware plc, string instanceName,
            string pcInterfaceLike, List<string[]> steps, bool apply, bool noDownload = false)
        {
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var cpu = DeviceItemOf(session, plc);
            var provider = cpu.GetService<DownloadProvider>();
            if (provider == null)
                throw new InvalidOperationException(
                    "PLC '" + plc.Name + "' does not expose a DownloadProvider (device item: " + cpu.Name + ").");

            var plan = new Dictionary<string, object>
            {
                { "plc", plc.Name },
                { "instance", instanceName },
                { "steps", steps.Count },
                { "apply", apply },
                // nome que não casa devolve o que existe. Lista vazia com a instância ligada na tela =
                // Runtime Manager de outra sessão do Windows, não nome errado.
                { "registeredInstances", RegisteredInstances() },
            };
            if (!apply)
            {
                // a interface `PLCSIM` só entra na configuração de download com instância ligada
                plan["availableInterfaces"] = Interfaces(provider);
                plan["note"] = "dry-run: nothing downloaded. Power on the instance first "
                    + "(pwsh scripts/sim-host.ps1 -Start) and close the classic PLCSIM, then add --apply.";
                return plan;
            }

            IInstance instance;
            try { instance = SimulationRuntimeManager.CreateInterface(instanceName); }
            catch (Exception ex)
            {
                plan["error"] = "No powered-on PLCSIM Advanced instance named '" + instanceName + "'. Start one with "
                    + "'pwsh scripts/sim-host.ps1 -Start' (or the PLCSIM Advanced control panel) and close the classic "
                    + "PLCSIM, which takes the same channel. API said: " + (ex.InnerException ?? ex).Message;
                return plan;
            }

            try
            {
                plan["controller"] = instance.ControllerName;
                plan["articleNumber"] = instance.ArticleNumber;

                if (noDownload)
                {
                    // programa já está lá: o caso de iterar observação (escrever, ler, esperar) sem
                    // pagar o download de novo. Instância vazia aqui aparece como tagCount 0.
                    plan["downloadSkipped"] = true;
                }
                else
                {
                    var target = FindTarget(provider, pcInterfaceLike);
                    if (target == null)
                    {
                        plan["error"] = "No PC interface matching '" + pcInterfaceLike
                            + "' in the download configuration, with the instance powered on.";
                        plan["availableInterfaces"] = Interfaces(provider);
                        return plan;
                    }
                    plan["pcInterface"] = target.PcInterface;
                    plan["targetInterface"] = target.Name;

                    // projeto online recusa download: "The operation is not permitted in online mode".
                    // Cair offline é a única saída pela API — a alternativa é clicar "Go offline" na GUI.
                    var online = cpu.GetService<OnlineProvider>();
                    if (online != null && online.State != OnlineState.Offline)
                    {
                        online.GoOffline();
                        plan["wentOffline"] = true;
                    }

                    plan["stateBeforeDownload"] = WaitReady(instance);
                    var swDownload = System.Diagnostics.Stopwatch.StartNew();
                    var result = provider.Download(target.Configuration, Resolve, Resolve,
                        DownloadOptions.Hardware | DownloadOptions.Software);
                    plan["download"] = new Dictionary<string, object>
                    {
                        { "state", result.State.ToString() },
                        { "warnings", result.WarningCount },
                        { "errors", result.ErrorCount },
                        { "ms", swDownload.ElapsedMilliseconds },
                        { "messages", result.Messages.Select(m => m.Message).Take(20).ToList() },
                    };

                    // o download reinicia a CPU virtual: o estado só estabiliza alguns segundos depois, e
                    // Run/UpdateTagList em cima de instância ainda booting devolve "-52, IsEmpty"
                    plan["stateAfterDownload"] = WaitReady(instance);
                    // o download recria a CPU virtual: o handle antigo continua respondendo estado, mas
                    // pedir RUN nele devolve "-52, IsEmpty". O handle novo é o que enxerga o programa.
                    instance = SimulationRuntimeManager.CreateInterface(instanceName);
                }
                try { instance.UpdateTagList(); } catch (Exception ex) { plan["tagListError"] = ex.Message; }
                plan["tagCount"] = instance.TagInfos.Length;
                // RUN que falha não aborta a rodada: em STOP ainda se lê a imagem de processo, e o
                // erro do RUN é dado de diagnóstico, não motivo para jogar fora os passos
                try { instance.Run(); } catch (Exception ex) { plan["runError"] = ex.Message; }
                plan["state"] = instance.OperatingState.ToString();
                plan["results"] = Execute(instance, steps);
                plan["ms"] = swTotal.ElapsedMilliseconds;
                return plan;
            }
            catch (Exception ex)
            {
                // o que já andou vale mais que a exceção sozinha: sem isso, "Error Code: -52, IsEmpty"
                // não diz se morreu no download ou no Run
                var inner = ex.InnerException ?? ex;
                plan["error"] = inner.Message;
                plan["errorType"] = inner.GetType().Name;
                return plan;
            }
            // sem finally: a instância é do control panel do usuário, e desligá-la aqui derrubaria a
            // simulação que ele abriu.
        }

        private static List<string> RegisteredInstances()
        {
            try { return SimulationRuntimeManager.RegisteredInstanceInfo.Select(i => i.Name).ToList(); }
            catch (Exception) { return new List<string>(); }
        }

        /// <summary>
        /// Espera a CPU virtual sair do boot (Stop ou Run), até 60 s. Devolve o estado alcançado —
        /// que é o que diz se o programa desceu ou se a instância continua vazia.
        /// </summary>
        private static string WaitReady(IInstance instance)
        {
            for (int i = 0; i < 60; i++)
            {
                var state = instance.OperatingState.ToString();
                if (state == "Stop" || state == "Run" || state == "StartUp") return state;
                System.Threading.Thread.Sleep(1000);
            }
            return instance.OperatingState.ToString();
        }

        /// <summary>
        /// Passos do script, na ordem: ["write","&lt;tag&gt;","&lt;valor&gt;"], ["read","&lt;tag&gt;"],
        /// ["wait","&lt;ms&gt;"], ["run"], ["stop"], ["state"], ["tags","&lt;filtro&gt;"].
        /// Passo que falha vira {ok:false,error} e a lista segue — igual ao `run --script`.
        /// Caminho de membro de DB vai com as aspas do Portal: "\"DB GLOBAL\".AREA.EQUIP.CMD_LIGA".
        /// </summary>
        private static List<object> Execute(IInstance instance, List<string[]> steps)
        {
            var rows = new List<object>();
            foreach (var step in steps)
            {
                var row = new Dictionary<string, object> { { "op", step[0] } };
                try
                {
                    switch (step[0])
                    {
                        case "write":
                            row["tag"] = step[1];
                            row["value"] = Write(instance, step[1], step[2]);
                            break;
                        case "read":
                            row["tag"] = step[1];
                            row["value"] = Plain(instance.Read(step[1]));
                            break;
                        case "wait":
                            System.Threading.Thread.Sleep(int.Parse(step[1], CultureInfo.InvariantCulture));
                            row["ms"] = step[1];
                            break;
                        case "run":
                            instance.Run();
                            row["state"] = instance.OperatingState.ToString();
                            break;
                        case "stop":
                            instance.Stop();
                            row["state"] = instance.OperatingState.ToString();
                            break;
                        case "state":
                            row["state"] = instance.OperatingState.ToString();
                            break;
                        case "tags":
                            var like = step.Length > 1 ? step[1] : null;
                            row["tags"] = instance.TagInfos
                                .Select(t => t.Name)
                                .Where(n => like == null || n.IndexOf(like, StringComparison.OrdinalIgnoreCase) >= 0)
                                .Take(200).ToList();
                            break;
                        default:
                            throw new ArgumentException(
                                "Unknown sim step '" + step[0] + "'. Valid: write, read, wait, run, stop, state, tags.");
                    }
                    row["ok"] = true;
                }
                catch (Exception ex)
                {
                    row["ok"] = false;
                    row["error"] = (ex.InnerException ?? ex).Message;
                }
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// Escrever exige tipo, e o tipo já está no PLC: lê a tag primeiro, preenche o campo do
        /// SDataValue que corresponde e devolve o valor escrito. Evita um --type por passo e evita
        /// escolher `WriteInt16` onde o programa declarou `Real`.
        /// </summary>
        private static object Write(IInstance instance, string tag, string text)
        {
            var value = instance.Read(tag);
            switch (value.Type.ToString())
            {
                case "Bool": value.Bool = ParseBool(text); break;
                case "Int8": value.Int8 = sbyte.Parse(text, CultureInfo.InvariantCulture); break;
                case "Int16": value.Int16 = short.Parse(text, CultureInfo.InvariantCulture); break;
                case "Int32": value.Int32 = int.Parse(text, CultureInfo.InvariantCulture); break;
                case "Int64": value.Int64 = long.Parse(text, CultureInfo.InvariantCulture); break;
                case "UInt8": value.UInt8 = byte.Parse(text, CultureInfo.InvariantCulture); break;
                case "UInt16": value.UInt16 = ushort.Parse(text, CultureInfo.InvariantCulture); break;
                case "UInt32": value.UInt32 = uint.Parse(text, CultureInfo.InvariantCulture); break;
                case "UInt64": value.UInt64 = ulong.Parse(text, CultureInfo.InvariantCulture); break;
                case "Float": value.Float = float.Parse(text, CultureInfo.InvariantCulture); break;
                case "Double": value.Double = double.Parse(text, CultureInfo.InvariantCulture); break;
                default:
                    throw new ArgumentException("Tag '" + tag + "' has type " + value.Type
                        + ", which this verb does not write (numeric and Bool only).");
            }
            instance.Write(tag, value);
            return Plain(value);
        }

        private static bool ParseBool(string text)
        {
            if (text == "1") return true;
            if (text == "0") return false;
            return bool.Parse(text);
        }

        /// <summary>SDataValue é struct com um campo por tipo — sai só o que o .Type diz que vale.</summary>
        private static object Plain(SDataValue value)
        {
            switch (value.Type.ToString())
            {
                case "Bool": return value.Bool;
                case "Int8": return value.Int8;
                case "Int16": return value.Int16;
                case "Int32": return value.Int32;
                case "Int64": return value.Int64;
                case "UInt8": return value.UInt8;
                case "UInt16": return value.UInt16;
                case "UInt32": return value.UInt32;
                case "UInt64": return value.UInt64;
                case "Float": return value.Float;
                case "Double": return value.Double;
                case "Char": return value.Char;
                case "WChar": return value.WChar.ToString();
                default: return value.Type.ToString();
            }
        }

        private sealed class Target
        {
            public string PcInterface;
            public string Name;
            public IConfiguration Configuration;
        }

        /// <summary>
        /// Primeira interface de destino sob a interface de PC cujo nome casa (default "PLCSIM", que
        /// é o access point do Advanced quando o PLCSIM clássico está fechado).
        /// </summary>
        private static Target FindTarget(DownloadProvider provider, string like)
        {
            foreach (ConfigurationMode mode in provider.Configuration.Modes)
                foreach (ConfigurationPcInterface pc in mode.PcInterfaces)
                {
                    if (like != null && pc.Name.IndexOf(like, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    foreach (ConfigurationTargetInterface target in pc.TargetInterfaces)
                        return new Target { PcInterface = pc.Name, Name = target.Name, Configuration = target };
                }
            return null;
        }

        private static List<object> Interfaces(DownloadProvider provider)
        {
            var rows = new List<object>();
            foreach (ConfigurationMode mode in provider.Configuration.Modes)
                foreach (ConfigurationPcInterface pc in mode.PcInterfaces)
                    rows.Add(new Dictionary<string, object>
                    {
                        { "mode", mode.Name },
                        { "pcInterface", pc.Name },
                        { "targets", pc.TargetInterfaces.Select(t => t.Name).ToList() },
                    });
            return rows;
        }

        private static DeviceItem DeviceItemOf(TiaSession session, PlcSoftware plc)
        {
            // por nome, não por referência: cada GetService devolve um proxy novo do mesmo objeto,
            // então ReferenceEquals é sempre falso
            foreach (Device device in session.AllDevices())
                foreach (DeviceItem item in device.DeviceItems)
                {
                    var software = item.GetService<SoftwareContainer>()?.Software as PlcSoftware;
                    if (software != null && software.Name == plc.Name)
                        return item;
                }
            throw new InvalidOperationException("No device item carries PLC software '" + plc.Name + "'.");
        }

        /// <summary>
        /// O download pergunta várias coisas antes de descer (parar módulos, aceitar diferenças, para
        /// qual alvo é o software). São dezenas de tipos de configuração, cada um com o seu enum de
        /// seleção; escolher por reflexão, em ordem de preferência, cabe em 15 linhas e sobrevive a
        /// tipo novo em versão nova do Portal — a alternativa é um `is X` por tipo.
        /// PlcSimulationAdvanced vem primeiro: sem isso o software desce como se fosse para CPU real.
        /// </summary>
        private static readonly string[] Preferred =
        {
            "PlcSimulationAdvanced", "StopAll", "AcceptAll", "OverwriteAll", "Overwrite",
            "DownloadToDevice", "ConsistentDownload", "DeleteAndReplace", "No", "Yes",
        };

        private static void Resolve(DownloadConfiguration configuration)
        {
            PropertyInfo selection = configuration.GetType().GetProperty("CurrentSelection");
            if (selection == null || !selection.CanWrite || !selection.PropertyType.IsEnum) return;
            var available = Enum.GetNames(selection.PropertyType);
            var pick = Preferred.FirstOrDefault(p => available.Contains(p));
            if (pick != null)
                selection.SetValue(configuration, Enum.Parse(selection.PropertyType, pick), null);
        }
    }
}
