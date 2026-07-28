using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Cax;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Tia.Core
{
    /// <summary>Hardware verbs: add-device (MLFB), set-address, connect-subnet/IO-system, CAx AML export/import.</summary>
    public static class Hardware
    {
        public static Device FindDevice(TiaSession session, string name)
        {
            var devices = session.AllDevices();
            var device = devices.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                // info/doctor report the CPU item name ("CPU CCO"), not the station ("…station_1") — accept both
                ?? devices.FirstOrDefault(d => HasItemNamed(d.DeviceItems, name));
            if (device == null)
                throw new InvalidOperationException("Device '" + name + "' not found. Known devices: "
                    + string.Join(", ", devices.Select(d => "'" + d.Name + "'")) + ". Run tia list-devices.");
            return device;
        }

        private static bool HasItemNamed(DeviceItemComposition items, string name)
        {
            foreach (DeviceItem item in items)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
                if (HasItemNamed(item.DeviceItems, name)) return true;
            }
            return false;
        }

        private static NetworkInterface Interface(Device device)
        {
            var item = Profinet.FindNetworkItem(device.DeviceItems);
            if (item == null)
                throw new InvalidOperationException("Device '" + device.Name + "' has no network interface.");
            return item.GetService<NetworkInterface>();
        }

        // ---------- add-device ----------

        /// <summary>MLFB "6ES7 512-1DK01-0AB0/V2.8" → "OrderNumber:..." (or pass a full TypeIdentifier with ':').</summary>
        public static object AddDevice(TiaSession session, string mlfb, string name, string station, string group, bool apply)
        {
            var typeId = mlfb.Contains(":") ? mlfb : "OrderNumber:" + mlfb;
            if (session.AllDevices().Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Device '" + name + "' already exists.");
            var result = new Dictionary<string, object>
            {
                { "typeIdentifier", typeId },
                { "device", name },
                { "station", station ?? name },
                { "group", group ?? "" },
                { "applied", apply },
            };
            if (apply)
            {
                var devices = group == null
                    ? session.Project.Devices
                    : (session.Project.DeviceGroups.Find(group)
                        ?? session.Project.DeviceGroups.Create(group)).Devices;
                var device = devices.CreateWithItem(typeId, name, station ?? name);
                result["created"] = device.Name;
            }
            return result;
        }

        // ---------- set-address ----------

        public static object SetAddress(TiaSession session, string deviceName, string ip, string mask,
            string pnName, bool apply)
        {
            if (ip == null && mask == null && pnName == null)
                throw new InvalidOperationException("Nothing to set: pass --ip, --mask and/or --pn-name.");
            var device = FindDevice(session, deviceName);
            var node = Interface(device).Nodes.First();
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "currentAddress", TryGet(node, "Address") },
                { "applied", apply },
            };
            if (ip != null) result["ip"] = ip;
            if (mask != null) result["mask"] = mask;
            if (pnName != null) result["pnName"] = pnName;
            if (apply)
            {
                if (ip != null) node.SetAttribute("Address", ip);
                if (mask != null) node.SetAttribute("SubnetMask", mask);
                if (pnName != null)
                {
                    node.SetAttribute("PnDeviceNameAutoGeneration", false);
                    node.SetAttribute("PnDeviceName", pnName);
                }
            }
            return result;
        }

        private static object TryGet(IEngineeringObject obj, string attribute)
        {
            try { return obj.GetAttribute(attribute); }
            catch { return null; }
        }

        // ---------- set-memory-bytes ----------

        /// <summary>
        /// Habilita os bytes de system/clock memory da CPU (sem eles, `FirstScan`, `AlwaysTRUE` e
        /// `Clock_1Hz` não existem e meia biblioteca não compila num projeto novo).
        /// Dry-run lista os atributos encontrados com o valor atual — é a sonda.
        /// ponytail: nome do atributo descoberto por substring (varia entre V19–V21); se o Portal
        /// renomear, o dry-run mostra o que existe e o mapeamento se ajusta aqui.
        /// </summary>
        public static object SetMemoryBytes(TiaSession session, string deviceName, int? systemByte,
            int? clockByte, bool apply)
        {
            var device = FindDevice(session, deviceName);
            var cpu = FindMemoryItem(device.DeviceItems);
            if (cpu == null)
                throw new InvalidOperationException("Device '" + device.Name
                    + "' has no device item with system/clock memory attributes (is it a CPU?).");

            var found = new List<object>();
            var changed = new List<object>();
            foreach (var info in cpu.GetAttributeInfos())
            {
                var attr = info.Name;
                if (!IsMemoryAttribute(attr)) continue;
                var current = TryGet(cpu, attr);
                found.Add(new Dictionary<string, object> { { "attribute", attr }, { "current", current } });

                object target = null;
                if (current is bool) target = true;                                   // Enable*/`*Enabled`
                else if (attr.IndexOf("Clock", StringComparison.OrdinalIgnoreCase) >= 0)
                    target = clockByte;
                else target = systemByte;
                if (target == null) continue;
                // o Portal devolve o endereço como UInt32/Byte conforme a versão — comparar já convertido,
                // senão int 0 != byte 0 e a chamada idempotente reporta mudança que não existe
                target = Convert.ChangeType(target, current?.GetType() ?? target.GetType());
                if (Equals(target, current)) continue;

                changed.Add(new Dictionary<string, object>
                    { { "attribute", attr }, { "from", current }, { "to", target } });
                if (apply) cpu.SetAttribute(attr, target);
            }
            return new Dictionary<string, object>
            {
                { "device", device.Name },
                { "item", cpu.Name },
                { "systemByte", systemByte },
                { "clockByte", clockByte },
                { "attributes", found },
                { "changes", changed },
                { "applied", apply },
            };
        }

        private static bool IsMemoryAttribute(string name)
        {
            return (name.IndexOf("MemoryByte", StringComparison.OrdinalIgnoreCase) >= 0)
                || (name.IndexOf("ClockMemory", StringComparison.OrdinalIgnoreCase) >= 0)
                || (name.IndexOf("SystemMemory", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>Primeiro device item que expõe atributo de system/clock memory (a CPU).</summary>
        private static DeviceItem FindMemoryItem(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                try
                {
                    if (item.GetAttributeInfos().Any(i => IsMemoryAttribute(i.Name))) return item;
                }
                catch { /* item sem atributos legíveis */ }
                var nested = FindMemoryItem(item.DeviceItems);
                if (nested != null) return nested;
            }
            return null;
        }

        // ---------- connect-subnet ----------

        /// <summary>
        /// Connects the device's interface to a subnet (created if missing). With --io-system:
        /// IO controller interface creates/owns it, IO device interface joins an existing one.
        /// </summary>
        public static object ConnectSubnet(TiaSession session, string deviceName, string subnetName,
            string ioSystemName, bool apply)
        {
            var device = FindDevice(session, deviceName);
            var itf = Interface(device);
            var node = itf.Nodes.First();
            var subnet = session.Project.Subnets
                .FirstOrDefault(s => s.Name.Equals(subnetName, StringComparison.OrdinalIgnoreCase));
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "subnet", subnetName },
                { "subnetAction", subnet == null ? "create" : "reuse" },
                { "applied", apply },
            };
            if (ioSystemName != null) result["ioSystem"] = ioSystemName;
            if (!apply) return result;

            if (subnet == null)
                subnet = session.Project.Subnets.Create("System:Subnet.Ethernet", subnetName);
            if (node.ConnectedSubnet == null)
                node.ConnectToSubnet(subnet);

            if (ioSystemName != null)
            {
                var controller = itf.IoControllers.FirstOrDefault();
                if (controller != null)
                {
                    var io = subnet.IoSystems.FirstOrDefault(
                        s => s.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase));
                    if (io == null)
                    {
                        controller.CreateIoSystem(ioSystemName);
                        result["ioSystemAction"] = "created";
                    }
                    else result["ioSystemAction"] = "exists";
                }
                else
                {
                    var connector = itf.IoConnectors.FirstOrDefault();
                    if (connector == null)
                        throw new InvalidOperationException(
                            "Interface of '" + device.Name + "' has neither IoController nor IoConnector.");
                    var io = subnet.IoSystems.FirstOrDefault(
                        s => s.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase));
                    if (io == null)
                        throw new InvalidOperationException(
                            "IO system '" + ioSystemName + "' not found on subnet '" + subnetName +
                            "'. Connect the IO controller first.");
                    connector.ConnectToIoSystem(io);
                    result["ioSystemAction"] = "joined";
                }
            }
            return result;
        }

        // ---------- CAx (AML) ----------

        public static object CaxExport(TiaSession session, string outDir)
        {
            var provider = session.Project.GetService<CaxProvider>();
            if (provider == null)
                throw new InvalidOperationException("CAx provider unavailable on this project.");
            Directory.CreateDirectory(outDir);
            var aml = Path.GetFullPath(Path.Combine(outDir, session.Project.Name + ".aml"));
            var log = Path.ChangeExtension(aml, ".log");
            if (File.Exists(aml)) File.Delete(aml);
            provider.Export(session.Project, new FileInfo(aml), new FileInfo(log));
            return new Dictionary<string, object> { { "exported", session.Project.Name }, { "file", aml }, { "log", log } };
        }

        public static object CaxImport(TiaSession session, string file, bool apply)
        {
            var full = Path.GetFullPath(file);
            if (!File.Exists(full))
                throw new FileNotFoundException("AML file not found: " + full);
            var result = new Dictionary<string, object> { { "file", full }, { "applied", apply } };
            if (apply)
            {
                var provider = session.Project.GetService<CaxProvider>();
                if (provider == null)
                    throw new InvalidOperationException("CAx provider unavailable on this project.");
                provider.Import(new FileInfo(full), CaxImportOptions.RetainTiaDevice);
            }
            return result;
        }
    }
}
