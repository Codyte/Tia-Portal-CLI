using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering.HW;
using Siemens.Engineering.MC.Drives;
using Siemens.Engineering.MC.Drives.Enums;

namespace Tia.Core
{
    /// <summary>
    /// SINAMICS/Startdrive verbs: list-telegrams, insert-telegram.
    /// A drive telegram is NOT a catalog submodule you plug — `plug-module` can never place one,
    /// because the drive object owns its own <c>TelegramComposition</c>. That is why the catalog
    /// TypeIdentifier for "Standard telegram 20" is nowhere in the help: it does not exist.
    /// Assembly: Siemens.Engineering.Startdrive.dll (namespace Siemens.Engineering.MC.Drives).
    /// </summary>
    public static class Drives
    {
        /// <summary>Every drive object reachable from a device, with the item that hosts it.</summary>
        private static List<KeyValuePair<string, DriveObject>> DriveObjects(Device device)
        {
            var found = new List<KeyValuePair<string, DriveObject>>();
            Collect(device.DeviceItems, device.Name, found);
            return found;
        }

        private static void Collect(DeviceItemComposition items, string path,
            List<KeyValuePair<string, DriveObject>> into)
        {
            foreach (DeviceItem item in items)
            {
                var here = path + "/" + item.Name;
                var container = item.GetService<DriveObjectContainer>();
                if (container != null)
                    foreach (DriveObject drive in container.DriveObjects)
                        into.Add(new KeyValuePair<string, DriveObject>(here, drive));
                Collect(item.DeviceItems, here, into);
            }
        }

        /// <summary>
        /// Reading a drive attribute can throw instead of returning a value — a G120 with no
        /// commissioning data answers `DriveObjectNumber` with
        /// "Drive object number could not be retrieved". One unreadable drive must not take the
        /// whole listing down, so every read here degrades to null.
        /// </summary>
        private static object Try<T>(Func<T> read)
        {
            try { return read(); }
            catch (Exception e) { return "unavailable: " + e.Message.Split('\n')[0].Trim(); }
        }

        private static Dictionary<string, object> Describe(string itemPath, DriveObject drive)
        {
            var telegrams = new List<object>();
            var listing = Try(() =>
            {
                foreach (Telegram telegram in drive.Telegrams)
                    telegrams.Add(new Dictionary<string, object>
                    {
                        { "number", Try(() => telegram.TelegramNumber) },
                        { "type", Try(() => telegram.Type.ToString()) },
                        { "inputBytes", Try(() => telegram.GetSizeInBytes(AddressIoType.Input)) },
                        { "outputBytes", Try(() => telegram.GetSizeInBytes(AddressIoType.Output)) },
                    });
                return telegrams.Count;
            });
            var result = new Dictionary<string, object>
            {
                { "item", itemPath },
                { "driveObject", Try(() => (object)drive.DriveObjectNumber) },
                { "telegrams", telegrams },
            };
            if (!(listing is int)) result["telegramsError"] = listing;
            return result;
        }

        // ---------- list-telegrams ----------

        public static object ListTelegrams(TiaSession session, string deviceName)
        {
            var device = Hardware.FindDevice(session, deviceName);
            var drives = DriveObjects(device);
            return new Dictionary<string, object>
            {
                { "device", device.Name },
                { "driveObjects", drives.Select(d => (object)Describe(d.Key, d.Value)).ToList() },
            };
        }

        // ---------- insert-telegram ----------

        /// <summary>
        /// Adds a telegram to a drive object. Dry-run reports <c>canInsert</c> (<c>CanInsertTelegram</c>),
        /// which is how a number/type pair gets confirmed against the drive before writing.
        /// </summary>
        public static object InsertTelegram(TiaSession session, string deviceName, string itemName,
            int number, string typeName, int? driveObjectNumber, bool apply)
        {
            var device = Hardware.FindDevice(session, deviceName);
            var type = ParseType(typeName);
            var drives = DriveObjects(device);
            if (drives.Count == 0)
                throw new InvalidOperationException("Device '" + device.Name + "' has no drive object. "
                    + "Telegrams only exist on SINAMICS devices handled by Startdrive (the GSD family "
                    + "carries them as plugged submodules instead — use plug-module there).");

            if (itemName != null)
                drives = drives.Where(d => d.Key.Split('/').Last()
                    .Equals(itemName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (driveObjectNumber.HasValue)
                drives = drives.Where(d => Equals(Try(() => (object)d.Value.DriveObjectNumber),
                                                  driveObjectNumber.Value)).ToList();

            if (drives.Count == 0)
                throw new InvalidOperationException("No drive object in '" + device.Name
                    + "' matches --item/--drive-object. Run tia list-telegrams --device " + device.Name + ".");
            if (drives.Count > 1)
                throw new InvalidOperationException("Device '" + device.Name + "' has " + drives.Count
                    + " drive objects (" + string.Join(", ", drives.Select(d => d.Key + "#"
                        + Try(() => (object)d.Value.DriveObjectNumber))) + "). Narrow it with --item or --drive-object.");

            var target = drives[0];
            var telegrams = target.Value.Telegrams;
            var existing = telegrams.Find(type);
            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "item", target.Key },
                { "driveObject", Try(() => (object)target.Value.DriveObjectNumber) },
                { "number", number },
                { "type", type.ToString() },
                { "applied", apply },
            };

            // idempotent: the same telegram already sitting there is a skip, not a failure
            if (existing != null && existing.TelegramNumber == number)
            {
                result["status"] = "skip (already present)";
                return result;
            }
            if (existing != null)
            {
                result["status"] = "conflict";
                result["presentNumber"] = existing.TelegramNumber;
                result["canChangeTelegram"] = existing.CanChangeTelegram(number);
                return result;   // changing an existing telegram is a different decision — not implicit
            }

            result["canInsert"] = telegrams.CanInsertTelegram(number, type);
            if (!apply) return result;
            telegrams.InsertTelegram(number, type);
            var inserted = telegrams.Find(type);
            result["status"] = "inserted";
            result["inputBytes"] = inserted.GetSizeInBytes(AddressIoType.Input);
            result["outputBytes"] = inserted.GetSizeInBytes(AddressIoType.Output);
            return result;
        }

        /// <summary>"Main" / "MainTelegram" / "safety" → TelegramType. Defaults to MainTelegram.</summary>
        private static TelegramType ParseType(string name)
        {
            if (string.IsNullOrEmpty(name)) return TelegramType.MainTelegram;
            var wanted = name.EndsWith("Telegram", StringComparison.OrdinalIgnoreCase) ? name : name + "Telegram";
            TelegramType parsed;
            if (Enum.TryParse(wanted, true, out parsed) && Enum.IsDefined(typeof(TelegramType), parsed))
                return parsed;
            throw new InvalidOperationException("Unknown telegram type '" + name + "'. Known: "
                + string.Join(", ", Enum.GetNames(typeof(TelegramType))));
        }
    }
}
