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

        /// <summary>Instância a usar quando há mais de um portal aberto (--portal NOME|PID).</summary>
        public static string PortalFilter { get; set; }

        /// <summary>
        /// Escolhe o processo do portal. Com mais de um aberto, exige --portal — attachar no
        /// primeiro da lista é escrever num projeto ao acaso (o do cliente, por exemplo).
        /// </summary>
        private static TiaPortalProcess PickProcess(bool required)
        {
            var procs = TiaPortal.GetProcesses();
            if (procs.Count == 0)
            {
                if (!required) return null;
                throw new InvalidOperationException(
                    "No running TIA Portal instance found. Start one with: tia open-project --file <path>.");
            }

            var filter = PortalFilter;
            if (!string.IsNullOrEmpty(filter))
            {
                var hits = procs.Where(p => Describe(p).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                if (hits.Count == 0)
                    throw new InvalidOperationException("No TIA Portal instance matches --portal '" + filter +
                        "'. Running: " + string.Join(" | ", procs.Select(Describe)));
                if (hits.Count > 1)
                    throw new InvalidOperationException("--portal '" + filter + "' is ambiguous: " +
                        string.Join(" | ", hits.Select(Describe)));
                return hits[0];
            }

            if (procs.Count > 1)
                throw new InvalidOperationException(procs.Count +
                    " TIA Portal instances are running — pass --portal <projeto|PID> to pick one: " +
                    string.Join(" | ", procs.Select(Describe)));
            return procs[0];
        }

        private static string Describe(TiaPortalProcess p)
        {
            var proj = p.ProjectPath == null ? "(no project)" : p.ProjectPath.Name;
            return p.Id + ":" + proj;
        }

        public static TiaSession Attach()
        {
            var proc = PickProcess(true);
            var portal = proc.Attach();

            ProjectBase project = portal.Projects.FirstOrDefault();
            if (project == null)
                project = portal.LocalSessions.FirstOrDefault()?.Project;
            if (project == null)
                throw new InvalidOperationException(
                    "TIA Portal is running but no project/session is open. Use: tia open-project --file <path>.");

            return new TiaSession(portal, project);
        }

        // ---------- lifecycle (open/save/close) ----------

        /// <summary>
        /// Opens a project: attaches to a running portal, or starts a new one
        /// (--no-ui → headless TiaPortalMode.WithoutUserInterface). The portal process
        /// outlives this call; later verbs attach to it.
        /// </summary>
        public static object OpenProject(string path, bool ui)
        {
            var file = new System.IO.FileInfo(System.IO.Path.GetFullPath(path));
            if (!file.Exists)
                throw new System.IO.FileNotFoundException("Project file not found: " + file.FullName);

            var proc = PickProcess(false);
            var portal = proc != null
                ? proc.Attach()
                : new TiaPortal(ui ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface);
            bool started = proc == null;

            var open = portal.Projects.FirstOrDefault();
            if (open != null)
                throw new InvalidOperationException(
                    "A project is already open: '" + open.Name + "'. Run 'tia close-project' first.");

            var project = portal.Projects.Open(file);
            return new Dictionary<string, object>
            {
                { "opened", project.Name },
                { "path", file.FullName },
                { "portal", started ? (ui ? "started-with-ui" : "started-headless") : "attached" },
            };
        }

        /// <summary>
        /// Creates a new single-user project: attaches to a running portal (must have no
        /// open project) or starts a new one (--no-ui → headless).
        /// </summary>
        public static object CreateProject(string dir, string name, bool ui)
        {
            var target = new System.IO.DirectoryInfo(System.IO.Path.GetFullPath(dir));
            if (!target.Exists) target.Create();

            var proc = PickProcess(false);
            var portal = proc != null
                ? proc.Attach()
                : new TiaPortal(ui ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface);
            bool started = proc == null;

            var open = portal.Projects.FirstOrDefault();
            if (open != null)
                throw new InvalidOperationException(
                    "A project is already open: '" + open.Name + "'. Run 'tia close-project' first.");

            var project = portal.Projects.Create(target, name);
            return new Dictionary<string, object>
            {
                { "created", project.Name },
                { "path", project.Path.FullName },
                { "portal", started ? (ui ? "started-with-ui" : "started-headless") : "attached" },
            };
        }

        /// <summary>Single-user Project, or throws for Multiuser sessions (save/close go via TIA there).</summary>
        private Project LocalProject()
        {
            var p = Project as Project;
            if (p == null)
                throw new InvalidOperationException(
                    "Only supported for single-user projects. Multiuser: save/check-in via TIA Portal.");
            return p;
        }

        public object Save()
        {
            var p = LocalProject();
            p.Save();
            return new Dictionary<string, object> { { "saved", p.Name } };
        }

        public object CloseProject(bool save)
        {
            var p = LocalProject();
            string name = p.Name;
            if (save) p.Save();
            p.Close();
            return new Dictionary<string, object> { { "closed", name }, { "saved", save } };
        }

        /// <summary>Every device in the project, including those nested in device groups.</summary>
        public List<Device> AllDevices()
        {
            var all = new List<Device>();
            all.AddRange(Project.Devices);
            foreach (DeviceUserGroup group in Project.DeviceGroups)
                CollectDevices(group, all);
            return all;
        }

        private static void CollectDevices(DeviceUserGroup group, List<Device> into)
        {
            into.AddRange(group.Devices);
            foreach (DeviceUserGroup sub in group.Groups)
                CollectDevices(sub, into);
        }

        /// <summary>Multiuser-safe write lock; harmless on single-user projects.</summary>
        public IDisposable ExclusiveAccess(string message)
        {
            return Portal.ExclusiveAccess(message);
        }

        /// <summary>All PLC software targets in the project, with their device name.</summary>
        public IEnumerable<KeyValuePair<string, PlcSoftware>> Plcs()
        {
            foreach (Device device in AllDevices())
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
