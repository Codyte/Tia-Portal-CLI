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
            var device = session.AllDevices()
                .FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (device == null)
                throw new InvalidOperationException("Device '" + name + "' not found. Run tia list-devices.");
            return device;
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
        public static object AddDevice(TiaSession session, string mlfb, string name, string station, bool apply)
        {
            var typeId = mlfb.Contains(":") ? mlfb : "OrderNumber:" + mlfb;
            if (session.AllDevices().Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Device '" + name + "' already exists.");
            var result = new Dictionary<string, object>
            {
                { "typeIdentifier", typeId },
                { "device", name },
                { "station", station ?? name },
                { "applied", apply },
            };
            if (apply)
            {
                var device = session.Project.Devices.CreateWithItem(typeId, name, station ?? name);
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
