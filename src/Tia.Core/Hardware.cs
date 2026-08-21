// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L61    class Hardware
//   L67    .FindDevice
//   L79    .HasItemNamed
//   L93    .SingleInterface
//   L115   .CollectInterfaces
//   L127   .Interface
//   L135   add-device
//   L138   .AddDevice
//   L163   delete-device
//   L165   .DeleteDevice
//   L178   plug-module
//   L186   .PlugModule
//   L272   .CollectSlots
//   L290   .FindItem
//   L309   .CollectMatches
//   L323   set-address
//   L325   .SetAddress
//   L358   set-io-address
//   L366   .SetIoAddress
//   L426   .CollectAddresses
//   L432   list-io-map
//   L442   .ListIoMap
//   L494   .ListIoMapRows
//   L507   .CollectMap
//   L535   .CollectTelegramMap
//   L573   .Range
//   L579   list-attrs
//   L586   .ListAttrs
//   L614   set-attr
//   L622   .SetAttr
//   L665   .Coerce
//   L679   .TryGet
//   L685   .TryGet
//   L692   set-memory-bytes
//   L701   .SetMemoryBytes
//   L766   .IsEnableAttribute
//   L773   .IsMemoryAttribute
//   L781   .FindMemoryItem
//   L796   connect-subnet
//   L802   .ConnectSubnet
//   L920   CAx (AML)
//   L922   .CaxExport
//   L935   .CaxImport
// ======================= END NAV INDEX =======================

using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Cax;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.MC.Drives;

namespace Tia.Core
{
    /// <summary>Hardware verbs: add-device (MLFB), set-address, connect-subnet/IO-system, CAx AML export/import.</summary>
    public static class Hardware
    {
        /// <summary>Sufixos de firmware que o `plug-module` sonda quando o MLFB vem sem versão.</summary>
        private static readonly string[] FirmwareVersions =
            { "0.0", "1.0", "1.1", "2.0", "2.1", "2.2", "3.0", "3.1", "4.0", "4.1", "4.2" };

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

        /// <summary>
        /// A interface de rede quando ela é única. Device com duas (CPU X1/X2) falha com os nomes,
        /// porque escrever IP na interface errada é invisível até a rede não subir.
        /// </summary>
        private static KeyValuePair<string, NetworkInterface> SingleInterface(Device device, string itemName)
        {
            var found = new List<KeyValuePair<string, NetworkInterface>>();
            CollectInterfaces(device.DeviceItems, "", found);
            if (itemName != null)
            {
                var picked = found.Where(f => f.Key.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                    || f.Key.EndsWith("/" + itemName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (picked.Count == 0)
                    throw new InvalidOperationException("Device '" + device.Name + "' has no network interface '"
                        + itemName + "' (has: " + string.Join(", ", found.Select(f => f.Key)) + ").");
                found = picked;
            }
            if (found.Count == 0)
                throw new InvalidOperationException("Device '" + device.Name + "' has no network interface.");
            if (found.Count > 1)
                throw new InvalidOperationException("Device '" + device.Name + "' has " + found.Count
                    + " network interfaces (" + string.Join(", ", found.Select(f => f.Key))
                    + "): pass --item with the one you mean.");
            return found[0];
        }

        private static void CollectInterfaces(DeviceItemComposition items, string prefix,
            List<KeyValuePair<string, NetworkInterface>> into)
        {
            foreach (DeviceItem item in items)
            {
                var path = prefix.Length == 0 ? item.Name : prefix + "/" + item.Name;
                var itf = item.GetService<NetworkInterface>();
                if (itf != null && itf.Nodes.Count > 0) into.Add(new KeyValuePair<string, NetworkInterface>(path, itf));
                CollectInterfaces(item.DeviceItems, path, into);
            }
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

        // ---------- delete-device ----------

        public static object DeleteDevice(TiaSession session, string name, bool apply)
        {
            var device = FindDevice(session, name);
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "items", device.DeviceItems.Select(i => i.Name).ToList() },
                { "applied", apply },
            };
            if (apply) device.Delete();
            return result;
        }

        // ---------- plug-module ----------

        /// <summary>
        /// Plugs a new device item (module/submodule) into a device or into one of its items.
        /// Dry-run is the probe: reports the free slots of the target and whether the given
        /// typeIdentifier can be plugged — the catalog string for drive telegrams is not in the
        /// official help, so `CanPlugNew` is how it gets confirmed before `--apply`.
        /// </summary>
        public static object PlugModule(TiaSession session, string deviceName, string itemName,
            string typeId, string name, int? position, bool apply)
        {
            var device = FindDevice(session, deviceName);
            if (itemName == null && typeId == null)
            {
                // probe: onde dá pra plugar alguma coisa neste device (nome de item se repete —
                // varrer tudo é mais barato que adivinhar qual "INVERSOR_X" é o drive object)
                var slots = new List<object>();
                CollectSlots(device.DeviceItems, device.Name, slots);
                return new Dictionary<string, object>
                {
                    { "device", device.Name },
                    { "deviceSlots", device.GetPlugLocations()
                        .Select(l => (object)(l.PositionNumber + ":" + l.Label)).ToList() },
                    { "itemSlots", slots },
                };
            }
            HardwareObject target = itemName == null ? (HardwareObject)device : FindItem(device, itemName);
            var pos = position ?? 1;
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "target", itemName ?? device.Name },
                { "applied", apply },
            };
            if (typeId == null)
            {
                // probe only: which slots are free. Com --type os freeSlots são ruído — sondar 9 MLFBs
                // custava ~330 linhas de JSON para 9 `canPlug` (FP-04, T8)
                result["freeSlots"] = target.GetPlugLocations()
                    .Select(l => (object)new Dictionary<string, object>
                        { { "position", l.PositionNumber }, { "label", l.Label } }).ToList();
                return result;
            }
            // nome do módulo sai do MLFB pedido, não do normalizado: senão o item plugado nasceria
            // chamado "OrderNumber:6ES7 …"
            var plugName = name ?? typeId;
            // MLFB puro (sem o prefixo `OrderNumber:`) devolvia `canPlug: false` mudo — o mesmo valor
            // de "este slot não aceita este módulo" (FP-06, T1)
            if (typeId.IndexOf(':') < 0 && !target.CanPlugNew(typeId, plugName, pos))
            {
                var prefixed = "OrderNumber:" + typeId;
                if (target.CanPlugNew(prefixed, plugName, pos)) typeId = prefixed;
            }
            result["typeIdentifier"] = typeId;
            result["name"] = plugName;
            result["position"] = pos;
            result["canPlug"] = target.CanPlugNew(typeId, plugName, pos);
            if (!(bool)result["canPlug"])
                result["reason"] = "CanPlugNew disse não — pode ser o slot (ocupado/incompatível) ou o "
                    + "typeIdentifier (forma esperada: \"OrderNumber:6ES7 131-6BH00-0BA0/V1.1\", com "
                    + "versão). O Openness não expõe catálogo: copie o typeIdentifier de um item igual "
                    + "já plugado (tia list-devices).";
            // MLFB sem sufixo de versão é recusado, e não há regra: o mesmo ET200SP quer /V0.0 no DI,
            // /V2.0 no AI e /V1.0 no módulo servidor. O Openness não expõe o catálogo do slot
            // (`CanPlugNew` é a única pergunta que ele responde), então sondar aqui é o que resta —
            // 11 tentativas locais contra a bateria manual da FP-04, T9.
            if (!(bool)result["canPlug"] && typeId.IndexOf("/V", StringComparison.OrdinalIgnoreCase) < 0)
            {
                var bases = typeId.IndexOf(':') < 0
                    ? new[] { typeId, "OrderNumber:" + typeId }
                    : new[] { typeId };
                var hit = bases.SelectMany(b => FirmwareVersions.Select(v => b + "/V" + v))
                    .FirstOrDefault(candidate => target.CanPlugNew(candidate, plugName, pos));
                if (hit != null) result["plugAs"] = hit;   // repassar em --type; --apply não adivinha
            }
            if (apply)
            {
                // "Unknown TypeIdentifer" sozinho manda adivinhar MLFB — e o Openness não expõe
                // busca no catálogo. O caminho que funciona é copiar o typeIdentifier de um item
                // igual já plugado em qualquer projeto (list-devices / list-attrs mostram).
                if (!(bool)result["canPlug"])
                    throw new InvalidOperationException("CanPlugNew disse não para '" + typeId
                        + "' no slot " + pos + ". "
                        + (result.ContainsKey("plugAs")
                            ? "Com versão passa: repita com --type \"" + result["plugAs"] + "\"."
                            : "Confira o MLFB: o Openness não tem busca de catálogo, "
                              + "então o typeIdentifier tem que vir de um item igual já plugado "
                              + "(tia list-devices num projeto que tenha o módulo) e costuma exigir a versão "
                              + "no fim (ex.: \"6ES7 155-6AU01-0BN0/V4.2\")."));
                result["plugged"] = target.PlugNew(typeId, plugName, pos).Name;
            }
            return result;
        }

        private static void CollectSlots(DeviceItemComposition items, string path, List<object> into)
        {
            foreach (DeviceItem item in items)
            {
                var here = path + "/" + item.Name;
                var free = item.GetPlugLocations().Select(l => l.PositionNumber + ":" + l.Label).ToList();
                if (free.Count > 0)
                    into.Add(new Dictionary<string, object> { { "item", here }, { "freeSlots", free } });
                CollectSlots(item.DeviceItems, here, into);
            }
        }

        /// <summary>
        /// SAFE-13: nome de item de hardware se repete (Rack_0/PROFINET interface/Port_1 existem em
        /// vários níveis e devices). Pegar o primeiro da varredura recursiva escrevia no item errado
        /// em silêncio — aqui nome ambíguo falha listando os caminhos, e o caminho
        /// "Pai/Filho" (o mesmo que o `plug-module` imprime) desempata.
        /// </summary>
        private static DeviceItem FindItem(Device device, string name)
        {
            var matches = new List<KeyValuePair<string, DeviceItem>>();
            CollectMatches(device.DeviceItems, name, "", matches);
            // o próprio plug-module lista os slots como "<device>/<item>": aceitar de volta o que
            // ele imprime, senão o caminho copiado da saída dele é recusado
            if (matches.Count == 0 && name != null && name.StartsWith(device.Name + "/", StringComparison.OrdinalIgnoreCase))
                CollectMatches(device.DeviceItems, name.Substring(device.Name.Length + 1), "", matches);
            if (matches.Count == 0)
                throw new InvalidOperationException("Device item '" + name + "' not found in '"
                    + device.Name + "'. Run tia list-devices.");
            if (matches.Count > 1)
                throw new InvalidOperationException("Device item '" + name + "' is ambiguous in '" + device.Name
                    + "' (" + matches.Count + " matches): " + string.Join(", ", matches.Select(m => m.Key))
                    + ". Pass the full path (--item \"Pai/Filho\").");
            return matches[0].Value;
        }

        /// <summary>Todo item cujo nome — ou cujo caminho "Pai/Filho" — casa, com o caminho junto.</summary>
        private static void CollectMatches(DeviceItemComposition items, string name, string prefix,
            List<KeyValuePair<string, DeviceItem>> into)
        {
            foreach (DeviceItem item in items)
            {
                var path = prefix.Length == 0 ? item.Name : prefix + "/" + item.Name;
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || path.Equals(name, StringComparison.OrdinalIgnoreCase))
                    into.Add(new KeyValuePair<string, DeviceItem>(path, item));
                else
                    CollectMatches(item.DeviceItems, name, path, into); // match não vira raiz de outro match
            }
        }

        // ---------- set-address ----------

        public static object SetAddress(TiaSession session, string deviceName, string ip, string mask,
            string pnName, bool apply, string itemName = null)
        {
            if (ip == null && mask == null && pnName == null)
                throw new InvalidOperationException("Nothing to set: pass --ip, --mask and/or --pn-name.");
            var device = FindDevice(session, deviceName);
            // SAFE-14: CPU com X1/X2 tem mais de uma interface, e "a primeira" é sorteio. Só segue
            // quando não há dúvida; com duas, o verbo diz quais são e recusa em vez de adivinhar.
            var itf = SingleInterface(device, itemName);
            var node = itf.Value.Nodes.First();
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "interface", itf.Key },
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

        // ---------- set-io-address ----------

        /// <summary>
        /// Start address of an I/O module. It is NOT an attribute of the DeviceItem (list-attrs
        /// does not show it) and the CAx import silently ignores it on an existing device — the
        /// only way in is DeviceItem.Addresses[i].StartAddress, and the Addresses live on the
        /// submodule, not on the module the user names. Search item + descendants.
        /// </summary>
        public static object SetIoAddress(TiaSession session, string deviceName, string itemName,
            string io, int? start, bool apply)
        {
            var device = FindDevice(session, deviceName);
            var addresses = new List<Address>();
            if (itemName == null)
                // sem --item: varre o device inteiro. É a sonda — nome de item se repete
                // (o drive object do G120 tem o mesmo nome do módulo) e o primeiro match perde
                // o que interessa.
                foreach (DeviceItem top in device.DeviceItems) CollectAddresses(top, addresses);
            else
                CollectAddresses(FindItem(device, itemName), addresses);
            if (io != null)
                addresses = addresses.Where(a => a.IoType.ToString()
                    .Equals(io, StringComparison.OrdinalIgnoreCase)).ToList();
            if (addresses.Count == 0)
                throw new InvalidOperationException("Item '" + itemName + "' has no "
                    + (io ?? "I/O") + " address. Is it an I/O module?");
            if (start != null && addresses.Count > 1)
                throw new InvalidOperationException("Item '" + itemName + "' has "
                    + addresses.Count + " addresses; pass --io Input or --io Output to pick one.");
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "item", itemName ?? "(device)" },
                { "addresses", addresses.Select(a => (object)new Dictionary<string, object>
                    { { "ioType", a.IoType.ToString() }, { "start", a.StartAddress },
                      { "lengthBits", a.Length } }).ToList() },
                { "applied", apply },
            };
            if (start == null) return result;
            result["start"] = start.Value;
            // O dry-run só ecoava o --start: o --apply era a primeira coisa que validava, e o erro do
            // Portal ("This address is already being used") só chegava depois de escrever (FP-05, T2).
            // Conferir contra o mapa é leitura pura — sondar de verdade exigiria escrever e reverter.
            var target = addresses[0];
            var conflicts = ListIoMapRows(session.AllDevices())
                .Where(r => (int)r["startByte"] >= 0
                    && (string)r["ioType"] == target.IoType.ToString()
                    && (int)r["startByte"] < start.Value + (target.Length + 7) / 8
                    && start.Value < (int)r["startByte"] + (int)r["lengthBytes"]
                    // o próprio alvo não conflita consigo: o item do mapa é caminho, o --item é folha
                    && !((string)r["device"] == device.Name && (itemName == null
                        || ((string)r["item"]).IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)))
                .Select(r => (object)((string)r["device"] + " " + (string)r["item"] + " " + (string)r["range"]))
                .ToList();
            if (conflicts.Count > 0)
                result["conflictsWith"] = conflicts;
            result["conflictCheck"] = conflicts.Count > 0 ? "occupied" : "free (pelo mapa)";
            if (conflicts.Count == 0)
                result["conflictNote"] = "o mapa não lê todo endereço do projeto — livre aqui não "
                    + "garante que o Portal aceite; ele responde com \"Next free address: N\".";
            if (apply)
            {
                addresses[0].StartAddress = start.Value;
                result["newStart"] = addresses[0].StartAddress;
            }
            return result;
        }

        private static void CollectAddresses(DeviceItem item, List<Address> into)
        {
            foreach (Address a in item.Addresses) into.Add(a);
            foreach (DeviceItem child in item.DeviceItems) CollectAddresses(child, into);
        }

        // ---------- list-io-map ----------

        /// <summary>
        /// Mapa de I/O do projeto inteiro, read-only. Existe porque não havia como responder "onde
        /// está mapeado isso" sem o GUI: `list-attrs` não mostra endereço (não é atributo do
        /// DeviceItem) e `list-telegrams` não traz o do telegrama. Na FP-01 o endereço do telegrama
        /// do G120 saiu de uma sonda de 18 chamadas, lendo o "Next free address" da mensagem de
        /// erro de um set_StartAddress conflitante — este verbo é a resposta direta.
        /// Varre item + descendentes, como o set-io-address, porque os Address vivem no submódulo.
        /// </summary>
        public static object ListIoMap(TiaSession session, string deviceName, string io)
        {
            var devices = deviceName == null
                ? session.AllDevices()
                : new List<Device> { FindDevice(session, deviceName) };

            var scanErrors = new List<object>();
            var rows = ListIoMapRows(devices, scanErrors);

            // StartAddress -1 = endereço existe no modelo mas não está atribuído (interface e portas
            // de um ET200SP sem cartão devolvem 4 desses). Contar não é mentira, mas entram no
            // nextFreeByte como -1 e viram "%IB-1" no mapa — some do mapa, aparece no contador.
            int unassigned = rows.Count(r => (int)r["startByte"] < 0);
            rows = rows.Where(r => (int)r["startByte"] >= 0).ToList();

            if (io != null)
                rows = rows.Where(r => ((string)r["ioType"]).Equals(io, StringComparison.OrdinalIgnoreCase)).ToList();

            rows = rows.OrderBy(r => (string)r["ioType"], StringComparer.Ordinal)
                .ThenBy(r => (int)r["startByte"]).ToList();

            // próximo byte livre por tipo: é a pergunta que a sonda do erro respondia.
            // É PISO, não garantia: sai do que este mapa leu, e o mapa não lê os `unassigned`.
            // Na FP-05 (T2) ele disse Input: 664 e o Portal recusou com "Next free address: 1062" —
            // 398 bytes que nenhuma composição visitada aqui expõe. Quem manda é o Portal.
            var nextFree = rows.GroupBy(r => (string)r["ioType"]).ToDictionary(
                g => g.Key,
                g => (object)g.Max(r => (int)r["startByte"] + (int)r["lengthBytes"]));

            var result = new Dictionary<string, object>
            {
                { "devices", devices.Count },
                { "addresses", rows.Count },
                { "unassigned", unassigned },
                { deviceName == null ? "nextFreeByte" : "nextFreeByteInDevice", nextFree },
                { "nextFreeByteExact", unassigned == 0 && deviceName == null && io == null
                    && scanErrors.Count == 0 },
                { "unreadableDrives", scanErrors.Count },
            };
            if (scanErrors.Count > 0) result["scanErrors"] = scanErrors;
            if (!(bool)result["nextFreeByteExact"])
                result["nextFreeByteNote"] = "piso, não garantia: "
                    + (deviceName != null ? "filtrado por --device (é o próximo livre DAQUELE device); " : "")
                    + (io != null ? "filtrado por --io; " : "")
                    + (scanErrors.Count > 0 ? scanErrors.Count + " drives com telegrama ilegível (scanErrors); " : "")
                    + unassigned + " itens sem endereço lido. O Portal recusa endereço ocupado "
                    + "dizendo \"Next free address: N\" — esse N é a autoridade.";
            result["map"] = rows.Cast<object>().ToList();   // por último: a nota tem que sair no head
            return result;
        }

        /// <summary>Linhas cruas do mapa (inclusive as de startByte -1), sem filtro nem ordenação.</summary>
        private static List<Dictionary<string, object>> ListIoMapRows(IEnumerable<Device> devices,
            List<object> scanErrors = null)
        {
            var rows = new List<Dictionary<string, object>>();
            foreach (var device in devices)
            {
                foreach (DeviceItem top in device.DeviceItems)
                    CollectMap(device.Name, top, top.Name, rows);
                CollectTelegramMap(device, rows, scanErrors);
            }
            return rows;
        }

        private static void CollectMap(string deviceName, DeviceItem item, string path,
            List<Dictionary<string, object>> into)
        {
            foreach (Address a in item.Addresses)
            {
                int bytes = (a.Length + 7) / 8;   // Length é em bits
                into.Add(new Dictionary<string, object>
                {
                    { "device", deviceName },
                    { "item", path },
                    { "ioType", a.IoType.ToString() },
                    { "startByte", a.StartAddress },
                    { "lengthBits", a.Length },
                    { "lengthBytes", bytes },
                    { "range", Range(a.IoType.ToString(), a.StartAddress, bytes) },
                });
            }
            foreach (DeviceItem child in item.DeviceItems)
                CollectMap(deviceName, child, path + "/" + child.Name, into);
        }

        /// <summary>
        /// Telegrama de drive SINAMICS ocupa process image mas não aparece em DeviceItem.Addresses
        /// (FP-04, T7: `--device <drive>` devolvia `addresses: 0`). O endereço vive em
        /// Telegram.Addresses, outra composição — sem varrer aqui, o `nextFreeByte` entregaria um
        /// byte já ocupado por telegrama. Drive sem dado de comissionamento pode lançar na leitura;
        /// um drive ilegível não derruba o mapa inteiro.
        /// </summary>
        private static void CollectTelegramMap(Device device, List<Dictionary<string, object>> into,
            List<object> scanErrors = null)
        {
            foreach (var pair in Drives.DriveObjects(device))
            {
                try
                {
                    foreach (Telegram telegram in pair.Value.Telegrams)
                        foreach (Address a in telegram.Addresses)
                        {
                            int bytes = (a.Length + 7) / 8;
                            into.Add(new Dictionary<string, object>
                            {
                                { "device", device.Name },
                                { "item", pair.Key + "/telegram " + telegram.TelegramNumber },
                                { "ioType", a.IoType.ToString() },
                                { "startByte", a.StartAddress },
                                { "lengthBits", a.Length },
                                { "lengthBytes", bytes },
                                { "range", Range(a.IoType.ToString(), a.StartAddress, bytes) },
                            });
                        }
                }
                catch (Exception ex)
                {
                    // SAFE-12: drive ilegível não derruba o mapa, mas engolir o erro fazia o mapa
                    // parecer completo — e é justo o telegrama que o `nextFreeByte` não vê.
                    if (scanErrors != null)
                        scanErrors.Add(new Dictionary<string, object>
                        {
                            { "device", device.Name }, { "driveObject", pair.Key },
                            { "error", (ex.InnerException ?? ex).Message },
                        });
                }
            }
        }

        /// <summary>"%IB256..267" — a forma que se lê no Portal e se escreve no programa.</summary>
        internal static string Range(string ioType, int start, int bytes)
        {
            string prefix = ioType.StartsWith("Out", StringComparison.OrdinalIgnoreCase) ? "%QB" : "%IB";
            return bytes <= 1 ? prefix + start : prefix + start + ".." + (start + bytes - 1);
        }

        // ---------- list-attrs ----------

        /// <summary>
        /// Read-only: todos os atributos (nome + valor atual) de um device item. É a sonda pra
        /// quando não se sabe se algo é submódulo plugável ou atributo — `GetAttributeInfos` é o
        /// único jeito de ver o que o Portal expõe naquela versão. `--like` filtra por substring.
        /// </summary>
        public static object ListAttrs(TiaSession session, string deviceName, string itemName, string like)
        {
            var device = FindDevice(session, deviceName);
            IEngineeringObject target = itemName == null
                ? (IEngineeringObject)device : FindItem(device, itemName);
            var attrs = new List<object>();
            foreach (var info in target.GetAttributeInfos())
            {
                if (like != null && info.Name.IndexOf(like, StringComparison.OrdinalIgnoreCase) < 0) continue;
                string readError;
                var value = TryGet(target, info.Name, out readError);
                attrs.Add(new Dictionary<string, object>
                {
                    { "attribute", info.Name },
                    { "value", value == null ? null : value.ToString() },
                    // SAFE-15: valor nulo de verdade e atributo ilegível saíam iguais na sonda.
                    { "readError", readError },
                });
            }
            return new Dictionary<string, object>
            {
                { "device", device.Name },
                { "item", itemName ?? device.Name },
                { "count", attrs.Count },
                { "attributes", attrs },
            };
        }

        // ---------- set-attr ----------

        /// <summary>
        /// Escreve um atributo qualquer de device item. `set-address`/`set-memory-bytes` cobrem os
        /// dois casos frequentes; este cobre o resto sem verbo novo por atributo (o que existe em
        /// cada versão do Portal sai do `list-attrs`). O tipo vem do valor atual — o Portal recusa
        /// int onde espera byte, e string "True" onde espera bool.
        /// </summary>
        public static object SetAttr(TiaSession session, string deviceName, string itemName,
            string attribute, string value, bool apply)
        {
            var device = FindDevice(session, deviceName);
            IEngineeringObject target = itemName == null
                ? (IEngineeringObject)device : FindItem(device, itemName);
            var infos = target.GetAttributeInfos();
            var info = infos.FirstOrDefault(i => i.Name == attribute);
            if (info == null)
                throw new InvalidOperationException("Attribute '" + attribute + "' not found in '"
                    + (itemName ?? device.Name) + "'. Run tia list-attrs --device " + deviceName + ".");
            // read-only recusado já no dry: sem isto o dry dizia "action: set" e só o --apply
            // descobria, com exceção crua do Portal. A informação vem do próprio AttributeInfo.
            if ((info.AccessMode & EngineeringAttributeAccessMode.Write) == 0)
                throw new InvalidOperationException("Attribute '" + attribute + "' is read-only ("
                    + info.AccessMode + ") in '" + (itemName ?? device.Name) + "'.");
            string readError;
            var current = TryGet(target, attribute, out readError);
            // SAFE-15: sem o valor atual não há tipo a provar, e o `Coerce` assumia string — o dry
            // prometia "action: set" e só o --apply descobria que o Portal esperava outro tipo.
            if (readError != null)
                throw new InvalidOperationException("Attribute '" + attribute + "' is unreadable ("
                    + readError + ") in '" + (itemName ?? device.Name)
                    + "'. Without the current value the target type cannot be proven — refusing to set.");
            object parsed = Coerce(value, current);
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "item", itemName ?? device.Name },
                { "attribute", attribute },
                { "from", current == null ? null : current.ToString() },
                { "to", parsed == null ? null : parsed.ToString() },
                { "action", Equals(parsed, current) ? "none (already set)" : "set" },
                // SAFE-15: valor atual nulo não prova tipo — o Coerce manda string e o Portal pode
                // recusar no apply. Declarado em vez de silencioso.
                { "typeProvenFrom", current == null ? null : current.GetType().Name },
                { "applied", apply },
            };
            if (apply && !Equals(parsed, current)) target.SetAttribute(attribute, parsed);
            return result;
        }

        /// <summary>String da linha de comando → o tipo que o atributo já tem (enum inclusive).</summary>
        public static object Coerce(string value, object current)
        {
            if (current == null) return value;
            var type = current.GetType();
            if (type.IsEnum) return Enum.Parse(type, value, true);
            return Convert.ChangeType(Decimalize(value, type), type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// CULT-01: `Convert.ChangeType` sem provider usa a cultura corrente. Em pt-BR o ponto e'
        /// separador de milhar, entao `--value 2.5` num Real virava **25** — escrita silenciosa de
        /// valor errado, medida no `set-motion-param` do PID_Compact. Numero passa a entrar sempre
        /// invariante, com a virgula decimal aceita como sinonimo do ponto (quem digita `2,5` no
        /// teclado pt-BR quer 2,5); dois separadores no mesmo numero e' ambiguidade, nao conversao.
        /// </summary>
        private static string Decimalize(string value, Type type)
        {
            var code = Type.GetTypeCode(type);
            if (code != TypeCode.Single && code != TypeCode.Double && code != TypeCode.Decimal)
                return value;
            var normalized = value.Replace(',', '.');
            if (normalized.Split('.').Length > 2)
                throw new FormatException("Value '" + value + "' has more than one decimal separator.");
            return normalized;
        }

        /// <summary>
        /// Valor do atributo, ou null. `readError` separa "o valor é nulo" de "não consegui ler" —
        /// sem essa distinção o `Coerce` assumia string e o dry-run prometia escrita impossível
        /// (SAFE-15).
        /// </summary>
        /// <summary>Quem só quer o valor (leitura de relatório, não decisão de escrita).</summary>
        private static object TryGet(IEngineeringObject obj, string attribute)
        {
            string ignored;
            return TryGet(obj, attribute, out ignored);
        }

        private static object TryGet(IEngineeringObject obj, string attribute, out string readError)
        {
            readError = null;
            try { return obj.GetAttribute(attribute); }
            catch (Exception ex) { readError = (ex.InnerException ?? ex).Message; return null; }
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
            var skipped = new List<object>();
            foreach (var info in cpu.GetAttributeInfos())
            {
                var attr = info.Name;
                if (!IsMemoryAttribute(attr)) continue;
                var current = TryGet(cpu, attr);
                found.Add(new Dictionary<string, object> { { "attribute", attr }, { "current", current } });

                object target = null;
                if (current is bool)
                {
                    // SAFE-16: o casamento é por substring, então um bool novo de nome parecido
                    // (outra versão do Portal) entrava aqui e virava true. Só liga o que se chama
                    // Enable*/*Enabled — o resto sai declarado em `skipped`, não escrito.
                    if (!IsEnableAttribute(attr))
                    {
                        skipped.Add(new Dictionary<string, object> { { "attribute", attr },
                            { "current", current }, { "reason", "bool sem Enable no nome" } });
                        continue;
                    }
                    target = true;
                }
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
                { "skipped", skipped },
                { "applied", apply },
            };
        }

        /// <summary>
        /// O bool que liga o byte. Em V21 medido no projeto-molde os nomes são exatamente
        /// `ClockMemoryByte` e `SystemMemoryByte` (o endereço vem em `*Address`) — sem "Enable"
        /// nenhum. A cláusula `Enable` fica para versão que use `EnableSystemMemoryByte` e afins.
        /// Bool fora dessa lista não é ligado no escuro (SAFE-16): sai em `skipped` com motivo.
        /// </summary>
        internal static bool IsEnableAttribute(string name)
        {
            return name.Equals("ClockMemoryByte", StringComparison.OrdinalIgnoreCase)
                || name.Equals("SystemMemoryByte", StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("Enable", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsMemoryAttribute(string name)
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

            // Prever o que o IO system vai sofrer, e dizer os nomes que existem. Sem isto o dry-run só
            // ecoava o --io-system recebido, e descobrir o nome do IO system de uma CPU custava um
            // export-cax de 1,5 MB + grep (FP-04, T6).
            var ctrl = itf.IoControllers.FirstOrDefault();
            if (ctrl != null && ctrl.IoSystem != null) result["ownedIoSystem"] = ctrl.IoSystem.Name;
            if (subnet != null)
                result["ioSystemsOnSubnet"] = subnet.IoSystems.Select(s => s.Name).ToList();
            else
                // Nome errado criava subnet paralela sem reclamar, e acertar `PN/IE_1` era adivinhação
                // (FP-05, T1). Com a lista, o "create" ou é intencional ou salta aos olhos.
                result["existingSubnets"] = session.Project.Subnets.Select(s => s.Name).ToList();
            if (ioSystemName != null)
            {
                if (ctrl != null)
                {
                    if (ctrl.IoSystem == null) result["ioSystemAction"] = "create";
                    else if (ctrl.IoSystem.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase))
                        result["ioSystemAction"] = "exists";
                    else
                        throw new InvalidOperationException("'" + device.Name + "' already owns IO system '"
                            + ctrl.IoSystem.Name + "' — a controller has only one. Pass --io-system \""
                            + ctrl.IoSystem.Name + "\".");
                }
                else
                {
                    var conn = itf.IoConnectors.FirstOrDefault();
                    var joined = conn == null ? null : conn.ConnectedToIoSystem;
                    bool exists = subnet != null && subnet.IoSystems.Any(
                        s => s.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase));
                    result["ioSystemAction"] =
                        joined != null && joined.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase)
                            ? "already"
                            : !exists ? "missing (connect the IO controller first)"
                            : joined == null ? "join" : "move";
                    if (joined != null) result["connectedTo"] = joined.Name;
                }
            }
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
                    // procurar no controlador, não na subnet: IO system de MESMO NOME pertencente a
                    // OUTRA CPU fazia o verbo responder "exists" sem ligar nada, e o drive que
                    // entrasse depois virava IO device do controlador errado — em silêncio, com a
                    // constante ..~Standard_telegram_NN faltando só na hora do compile.
                    var mine = controller.IoSystem;   // 1 por controlador
                    if (mine == null)
                    {
                        controller.CreateIoSystem(ioSystemName);
                        result["ioSystemAction"] = "created";
                    }
                    else if (mine.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase))
                        result["ioSystemAction"] = "exists";
                    else
                        throw new InvalidOperationException("'" + device.Name + "' already owns IO system '"
                            + mine.Name + "' — a controller has only one. Pass --io-system \"" + mine.Name + "\".");
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
                    // já ligado nesse IO system = no-op (o controlador acima já é idempotente):
                    // reinstalar a biblioteca repete o par connect-subnet e não pode falhar por isso.
                    // Ligado em OUTRO: o Openness recusa mover ("already connected to an io system"),
                    // então desliga antes — é o caso de reaproveitar um drive noutro controlador.
                    // comparar por nome: os wrappers EOM não são estáveis por referência, então
                    // `was == io` era sempre falso e o verbo religava o drive toda vez
                    var was = connector.ConnectedToIoSystem;
                    if (was != null && was.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase))
                        result["ioSystemAction"] = "already";
                    else
                    {
                        if (was != null)
                        {
                            connector.DisconnectFromIoSystem();
                            result["disconnectedFrom"] = was.Name;
                        }
                        connector.ConnectToIoSystem(io);
                        result["ioSystemAction"] = was == null ? "joined" : "moved";
                    }
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
