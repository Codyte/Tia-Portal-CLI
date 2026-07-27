using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;

namespace Tia.Core
{
    /// <summary>Config for gen-profinet (deserialized from the user's JSON by the CLI).</summary>
    public class ProfinetConfig
    {
        public List<ProfinetMapping> Devices { get; set; }
        public int StartByte { get; set; } = 13000;
        public string TagFolder { get; set; } = "4. Comm";
        public string TagTable { get; set; } = "DISPOSITIVOS_PROFINET";
    }

    public class ProfinetMapping
    {
        public string Hardware { get; set; }
        public string EquipmentTag { get; set; }
        public int DeviceNumber { get; set; }
    }

    /// <summary>Creates one BOOL comm tag per mapped Profinet IO-Device (port of "Gerador de Conexão Profinet").</summary>
    public static class Profinet
    {
        public static object Generate(TiaSession session, PlcSoftware plc, ProfinetConfig config, bool apply)
        {
            if (config?.Devices == null || config.Devices.Count == 0)
                throw new ArgumentException("Config must contain a non-empty 'Devices' list.");

            var ioDevices = FindIoDeviceNames(session);
            var table = apply ? ResolveTable(plc, config) : FindTable(plc, config);
            var allocator = new BoolAddressAllocator(config.StartByte);
            var actions = new List<object>();

            foreach (var mapping in config.Devices.OrderBy(m => m.DeviceNumber))
            {
                string tagName = TagName(mapping);
                string action;
                string address = null;

                if (!ioDevices.Contains(mapping.Hardware, StringComparer.OrdinalIgnoreCase))
                {
                    action = "hardware-not-found";
                }
                else if (table != null && table.Tags.Find(tagName) != null)
                {
                    action = "exists";
                    allocator.Skip(); // keep addresses aligned with the original sequential layout
                }
                else
                {
                    address = allocator.Next();
                    action = "create";
                    if (apply)
                    {
                        var tag = table.Tags.Create(tagName, "Bool", address);
                        var comment = tag.Comment.Items.FirstOrDefault();
                        if (comment != null)
                            comment.Text = "Conexão " + mapping.DeviceNumber + " com o equipamento " + mapping.EquipmentTag;
                    }
                }

                actions.Add(new Dictionary<string, object>
                {
                    { "hardware", mapping.Hardware },
                    { "tag", tagName },
                    { "address", address },
                    { "action", action },
                });
            }

            return new Dictionary<string, object>
            {
                { "table", config.TagFolder + "/" + config.TagTable },
                { "ioDevicesInProject", ioDevices.Count },
                { "applied", apply },
                { "tags", actions },
            };
        }

        /// <summary>
        /// Nome da tag de conexão. Só o espaço vira '_': as 45 tags de DISPOSITIVOS_PROFINET do
        /// projeto de referência mantêm hífen e ponto (COMM_60_INVERSOR_AG-02_CCM3,
        /// COMM_1_REM_RM1.0). O script FINAL também troca '-' e '.', e com --apply criaria uma
        /// tag nova ao lado de cada existente em vez de reconhecê-la.
        /// </summary>
        internal static string TagName(ProfinetMapping mapping)
        {
            return "COMM_" + mapping.DeviceNumber + "_" + mapping.EquipmentTag.Replace(" ", "_");
        }

        private static PlcTagTable FindTable(PlcSoftware plc, ProfinetConfig config)
        {
            var group = Ops.FindTagGroup(plc.TagTableGroup, config.TagFolder);
            return group?.TagTables.Find(config.TagTable);
        }

        private static PlcTagTable ResolveTable(PlcSoftware plc, ProfinetConfig config)
        {
            var group = Ops.FindTagGroup(plc.TagTableGroup, config.TagFolder)
                ?? plc.TagTableGroup.Groups.Create(config.TagFolder);
            return group.TagTables.Find(config.TagTable) ?? group.TagTables.Create(config.TagTable);
        }

        /// <summary>Names of devices whose network interface operates as Profinet IO-Device.</summary>
        internal static List<string> FindIoDeviceNames(TiaSession session)
        {
            var names = new List<string>();
            foreach (var device in session.AllDevices())
            {
                var netItem = FindNetworkItem(device.DeviceItems);
                if (netItem == null) continue;
                try
                {
                    var mode = (InterfaceOperatingModes)netItem.GetService<NetworkInterface>()
                        .GetAttribute("InterfaceOperatingMode");
                    if (mode == InterfaceOperatingModes.IoDevice)
                        names.Add(device.Name);
                }
                catch { /* device without the attribute */ }
            }
            return names;
        }

        internal static DeviceItem FindNetworkItem(DeviceItemComposition items)
        {
            foreach (DeviceItem item in items)
            {
                if (item.GetService<NetworkInterface>() != null) return item;
                var sub = FindNetworkItem(item.DeviceItems);
                if (sub != null) return sub;
            }
            return null;
        }
    }

    /// <summary>%M bit allocator. ponytail: BOOL-only — original also packed words; add when a non-bool consumer appears.</summary>
    public class BoolAddressAllocator
    {
        private int _byte;
        private int _bit;

        public BoolAddressAllocator(int startByte)
        {
            _byte = startByte;
        }

        public string Next()
        {
            if (_bit > 7) { _bit = 0; _byte++; }
            var address = "%M" + _byte + "." + _bit;
            _bit++;
            return address;
        }

        public void Skip()
        {
            Next();
        }
    }
}
