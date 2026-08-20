// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L39    class Drives
//   L42    .DriveObjects
//   L49    .Collect
//   L69    .Try
//   L75    .Describe
//   L123   list-telegrams
//   L125   .ListTelegrams
//   L136   list-drive-params
//   L146   .ListParams
//   L206   set-drive-param
//   L217   .SetParam
//   L283   .Matches
//   L298   .Scalar
//   L311   .OutOfRange
//   L320   .TryNumber
//   L328   insert-telegram
//   L334   .InsertTelegram
//   L423   .ParseType
// ======================= END NAV INDEX =======================

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
        internal static List<KeyValuePair<string, DriveObject>> DriveObjects(Device device)
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
                        // endereço no process image do CLP: o telegrama não aparece em
                        // DeviceItem.Addresses — por isso `list-io-map --device <drive>` devolve
                        // `addresses: 0` (FP-04, T7). O endereço vive em Telegram.Addresses, que é
                        // outra composição, e só aqui dá para lê-lo.
                        { "addresses", Try(() =>
                            {
                                var rows = new List<object>();
                                foreach (Address a in telegram.Addresses)
                                {
                                    int bytes = (a.Length + 7) / 8;   // Length é em bits
                                    rows.Add(new Dictionary<string, object>
                                    {
                                        { "ioType", a.IoType.ToString() },
                                        { "startByte", a.StartAddress },
                                        { "lengthBits", a.Length },
                                        { "lengthBytes", bytes },
                                        { "range", Hardware.Range(a.IoType.ToString(), a.StartAddress, bytes) },
                                    });
                                }
                                return (object)rows;
                            }) },
                        { "attributes", Try(() => (object)telegram.GetAttributeInfos()
                            .ToDictionary(i => i.Name, i => Try(() => telegram.GetAttribute(i.Name)))) },
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

        // ---------- list-drive-params ----------

        /// <summary>
        /// Parameters (p/r) of every drive object on a device. These are NOT DeviceItem attributes:
        /// `list-attrs`/`set-attr` walk <c>DeviceItem.GetAttributeInfos</c> and can never see them,
        /// because the parameter set hangs off <c>DriveObject.Parameters</c>
        /// (<c>DriveParameterComposition</c>) instead. A commissioned G120 answers with thousands
        /// of them, so <paramref name="like"/> (substring of name or number) and
        /// <paramref name="countOnly"/> exist to keep the payload readable.
        /// </summary>
        public static object ListParams(TiaSession session, string deviceName, string itemName,
            int? driveObjectNumber, string like, bool countOnly)
        {
            var device = Hardware.FindDevice(session, deviceName);
            var objects = new List<object>();
            foreach (var pair in DriveObjects(device))
            {
                if (itemName != null && !pair.Key.EndsWith("/" + itemName, StringComparison.OrdinalIgnoreCase))
                    continue;
                var drive = pair.Value;
                var number = Try(() => (object)drive.DriveObjectNumber);
                if (driveObjectNumber.HasValue && !Equals(number, driveObjectNumber.Value)) continue;

                var rows = new List<object>();
                int total = 0, matched = 0;
                var listing = Try(() =>
                {
                    foreach (DriveParameter parameter in drive.Parameters)
                    {
                        total++;
                        var name = Try(() => parameter.Name) as string;
                        var num = Try(() => (object)parameter.Number);
                        var text = Try(() => parameter.ParameterText) as string;
                        if (like != null && !Matches(like, name, Convert.ToString(num), text)) continue;
                        matched++;
                        // --count é a sonda barata antes do dump: sem contar o que casa, `--like X
                        // --count` respondia só o total do drive, que é a pergunta que ninguém fez.
                        if (countOnly) continue;
                        rows.Add(new Dictionary<string, object>
                        {
                            { "name", name },
                            { "number", num },
                            { "value", Scalar(Try(() => parameter.Value)) },
                            { "unit", Try(() => parameter.Unit) },
                            { "min", Scalar(Try(() => parameter.MinValue)) },
                            { "max", Scalar(Try(() => parameter.MaxValue)) },
                            { "text", text },
                        });
                    }
                    return total;
                });
                var described = new Dictionary<string, object>
                {
                    { "item", pair.Key },
                    { "driveObject", number },
                    { "parameters", total },
                    { "matched", matched },
                };
                if (!countOnly) described["values"] = rows;
                if (!(listing is int)) described["parametersError"] = listing;
                objects.Add(described);
            }
            return new Dictionary<string, object>
            {
                { "device", device.Name },
                { "like", like },
                { "driveObjects", objects },
            };
        }

        // ---------- set-drive-param ----------

        /// <summary>
        /// Writes one drive parameter offline (the project value, not the drive on the wire — a
        /// download is what carries it to the hardware). <c>DriveParameter.Value</c> is the only
        /// settable member of the type: name, unit and the limits are read-only, which is why the
        /// dry-run can prove the range before <c>--apply</c> touches anything.
        /// An array parameter answers on its indexed element (<c>p1082[0]</c>), never on the parent
        /// (<c>p1082</c> reads null) — a null current value proves no type, so it is refused here
        /// for the same reason <see cref="Hardware.SetAttr"/> refuses it.
        /// </summary>
        public static object SetParam(TiaSession session, string deviceName, string itemName,
            string parameterName, string value, bool apply)
        {
            var device = Hardware.FindDevice(session, deviceName);
            DriveParameter found = null;
            string foundItem = null;
            foreach (var pair in DriveObjects(device))
            {
                if (itemName != null && !pair.Key.EndsWith("/" + itemName, StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (DriveParameter parameter in pair.Value.Parameters)
                {
                    var name = Try(() => parameter.Name) as string;
                    if (!string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (found != null)
                        throw new InvalidOperationException("Parameter '" + parameterName
                            + "' is ambiguous in '" + device.Name
                            + "': more than one drive object answers to it. Narrow with --item.");
                    found = parameter;
                    foundItem = pair.Key;
                }
            }
            if (found == null)
                throw new InvalidOperationException("Parameter '" + parameterName + "' not found in '"
                    + (itemName ?? device.Name) + "'. Run tia list-drive-params --device " + deviceName
                    + " --like " + parameterName + ".");

            var current = Try(() => found.Value);
            if (current is string && ((string)current).StartsWith("unavailable: "))
                throw new InvalidOperationException("Parameter '" + parameterName + "' is unreadable ("
                    + current + "). Without the current value the target type cannot be proven — refusing to set.");
            if (current == null)
                throw new InvalidOperationException("Parameter '" + parameterName
                    + "' reads null — an array parent carries no value of its own. Set the indexed "
                    + "element instead (e.g. '" + parameterName + "[0]').");
            if (!Equals(Scalar(current), current))
                throw new InvalidOperationException("Parameter '" + parameterName
                    + "' holds an interconnection (BICO), not a scalar: " + Scalar(current)
                    + ". Openness writes the value, not the wiring — refusing to set.");
            var parsed = Hardware.Coerce(value, current);

            var min = Try(() => found.MinValue);
            var max = Try(() => found.MaxValue);
            var outOfRange = OutOfRange(parsed, min, max);
            if (outOfRange != null)
                throw new InvalidOperationException("Value " + value + " for '" + parameterName
                    + "' is " + outOfRange + " (min " + min + ", max " + max + ").");

            var result = new Dictionary<string, object>
            {
                { "device", device.Name },
                { "item", foundItem },
                { "parameter", parameterName },
                { "from", current },
                { "to", parsed },
                { "unit", Try(() => found.Unit) },
                { "min", min },
                { "max", max },
                { "action", Equals(parsed, current) ? "none (already set)" : "set" },
                { "applied", apply },
            };
            if (apply && !Equals(parsed, current)) found.Value = parsed;
            return result;
        }

        /// <summary>Substring match against any of the fields, case- and accent-blind enough for p/r names.</summary>
        private static bool Matches(string like, params string[] fields)
        {
            foreach (var field in fields)
                if (field != null && field.IndexOf(like, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        /// <summary>
        /// A BICO parameter answers <c>Value</c> with another <see cref="DriveParameter"/>
        /// (p840 pointing at r722.0), and that object walks back to its own composition — the JSON
        /// serializer hit "Self referencing loop detected" on the 452nd parameter of a real G120.
        /// Anything that is not a scalar is flattened to its text here, which is also what makes
        /// <see cref="SetParam"/> able to tell an interconnection from a value it may write.
        /// </summary>
        private static object Scalar(object value)
        {
            if (value == null || value is string || value is bool || value is decimal
                || value is DateTime || value.GetType().IsPrimitive || value.GetType().IsEnum)
                return value;
            // DriveParameter não sobrescreve ToString, então o BICO saía como o nome da classe.
            // O que interessa é para onde ele aponta: p840[0] = "r722[0]".
            var wired = value as DriveParameter;
            if (wired != null) return Try(() => wired.Name);
            return Convert.ToString(value);
        }

        /// <summary>null when inside the limits (or when a limit is missing/not numeric).</summary>
        private static string OutOfRange(object parsed, object min, object max)
        {
            double v, limit;
            if (!TryNumber(parsed, out v)) return null;
            if (TryNumber(min, out limit) && v < limit) return "below the minimum";
            if (TryNumber(max, out limit) && v > limit) return "above the maximum";
            return null;
        }

        private static bool TryNumber(object value, out double number)
        {
            number = 0;
            if (value == null || value is string || value is bool) return false;
            try { number = Convert.ToDouble(value); return true; }
            catch { return false; }
        }

        // ---------- insert-telegram ----------

        /// <summary>
        /// Adds a telegram to a drive object. Dry-run reports <c>canInsert</c> (<c>CanInsertTelegram</c>),
        /// which is how a number/type pair gets confirmed against the drive before writing.
        /// </summary>
        public static object InsertTelegram(TiaSession session, string deviceName, string itemName,
            int number, string typeName, int? driveObjectNumber, bool change, bool apply)
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
                // a fresh G120 already ships with main telegram 1, so "change" is the normal path —
                // but it throws the current telegram away, so it stays behind an explicit --change
                result["presentNumber"] = existing.TelegramNumber;
                var canChange = existing.CanChangeTelegram(number);
                result["canChangeTelegram"] = canChange;
                if (!change)
                {
                    result["status"] = "conflict (pass --change to replace)";
                    return result;
                }
                if (!canChange)
                {
                    result["status"] = "cannot change";
                    return result;
                }
                if (!apply) { result["status"] = "would change"; return result; }
                existing.TelegramNumber = number;   // in place: a main telegram cannot be erased
                var changed = telegrams.Find(type);
                result["status"] = "changed";
                result["inputBytes"] = changed.GetSizeInBytes(AddressIoType.Input);
                result["outputBytes"] = changed.GetSizeInBytes(AddressIoType.Output);
                return result;
            }

            var canInsert = telegrams.CanInsertTelegram(number, type);
            result["canInsert"] = canInsert;
            if (!apply) return result;
            if (!canInsert)
            {
                // InsertTelegram would throw an opaque "attribute Telegram (N) is not supported" here
                result["status"] = "cannot insert";
                return result;
            }
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
