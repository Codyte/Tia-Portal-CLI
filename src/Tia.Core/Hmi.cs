using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Tia.Core
{
    /// <summary>WinCC Unified read-only inventory. Openness V19 não exporta telas Unified — só enumeração.</summary>
    public static class Hmi
    {
        public static IEnumerable<KeyValuePair<string, HmiSoftware>> Targets(TiaSession session)
        {
            foreach (Device device in session.AllDevices())
                foreach (DeviceItem item in device.DeviceItems)
                {
                    var sw = item.GetService<SoftwareContainer>()?.Software as HmiSoftware;
                    if (sw != null)
                        yield return new KeyValuePair<string, HmiSoftware>(device.Name, sw);
                }
        }

        public static object List(TiaSession session, string deviceName)
        {
            var targets = Targets(session)
                .Where(t => deviceName == null ||
                    t.Key.Equals(deviceName, System.StringComparison.OrdinalIgnoreCase))
                .Select(t => new Dictionary<string, object>
                {
                    { "device", t.Key },
                    { "hmi", t.Value.Name },
                    { "screens", t.Value.Screens.Select(s => s.Name).ToList() },
                    { "tagTables", t.Value.TagTables.Select(tt => new Dictionary<string, object>
                        { { "table", tt.Name }, { "tagCount", tt.Tags.Count } }).ToList() },
                }).ToList();
            if (targets.Count == 0)
                throw new System.InvalidOperationException(deviceName == null
                    ? "No WinCC Unified HMI target in the project."
                    : "HMI device '" + deviceName + "' not found.");
            return targets;
        }
    }
}
