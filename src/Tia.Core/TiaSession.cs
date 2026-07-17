using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;

namespace Tia.Core
{
    /// <summary>Attached TIA Portal instance + open project (single-user or multiuser local session).</summary>
    public sealed class TiaSession : IDisposable
    {
        public TiaPortal Portal { get; }
        public ProjectBase Project { get; }

        private TiaSession(TiaPortal portal, ProjectBase project)
        {
            Portal = portal;
            Project = project;
        }

        public static TiaSession Attach()
        {
            var proc = TiaPortal.GetProcesses().FirstOrDefault();
            if (proc == null)
                throw new InvalidOperationException("No running TIA Portal instance found.");
            var portal = proc.Attach();

            ProjectBase project = portal.Projects.FirstOrDefault();
            if (project == null)
                project = portal.LocalSessions.FirstOrDefault()?.Project;
            if (project == null)
                throw new InvalidOperationException("TIA Portal is running but no project/session is open.");

            return new TiaSession(portal, project);
        }

        /// <summary>All PLC software targets in the project, with their device name.</summary>
        public IEnumerable<KeyValuePair<string, PlcSoftware>> Plcs()
        {
            foreach (Device device in Project.Devices)
                foreach (DeviceItem item in device.DeviceItems)
                {
                    var sw = item.GetService<SoftwareContainer>()?.Software as PlcSoftware;
                    if (sw != null)
                        yield return new KeyValuePair<string, PlcSoftware>(device.Name, sw);
                }
        }

        /// <summary>Resolve one PLC by software name; if name is null, the project must have exactly one.</summary>
        public PlcSoftware GetPlc(string name = null)
        {
            var all = Plcs().Select(p => p.Value).ToList();
            if (all.Count == 0)
                throw new InvalidOperationException("Project contains no PLC software.");
            if (name == null)
            {
                if (all.Count > 1)
                    throw new InvalidOperationException(
                        "Multiple PLCs found, pass --plc <name>. Candidates: " +
                        string.Join(", ", all.Select(p => p.Name)));
                return all[0];
            }
            var match = all.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                throw new InvalidOperationException(
                    "PLC '" + name + "' not found. Candidates: " + string.Join(", ", all.Select(p => p.Name)));
            return match;
        }

        public void Dispose()
        {
            Portal?.Dispose();
        }
    }
}
