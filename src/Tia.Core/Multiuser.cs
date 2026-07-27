using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Multiuser;

namespace Tia.Core
{
    /// <summary>
    /// TIA Project Server (multiuser). Read-only for now: lista os projetos de um servidor,
    /// quem está com lock e quais sessões locais existem. Escrita (CreateLocalSession /
    /// CloseAndCommit) só depois que o attach estiver provado contra um servidor real.
    /// </summary>
    public static class Multiuser
    {
        /// <summary>
        /// Projetos de um Project Server + lock + sessões locais.
        /// Reusa a conexão já configurada no Portal quando existe; se precisar criar uma,
        /// remove no fim (a menos que --keep-connection), pra não sujar a config da máquina.
        /// </summary>
        public static object ListServerProjects(string host, int port, bool http, bool keepConnection)
        {
            var proc = TiaPortal.GetProcesses().FirstOrDefault();
            if (proc == null)
                throw new InvalidOperationException(
                    "No running TIA Portal instance found. Start one with: tia open-project --file <path>.");

            using (var portal = proc.Attach())
            {
                bool created;
                var server = ResolveServer(portal, host, port, http, out created);
                try
                {
                    var projects = new List<Dictionary<string, object>>();
                    foreach (var info in server.GetServerProjects())
                        projects.Add(Describe(server, info));

                    return new Dictionary<string, object>
                    {
                        { "server", server.ServerName },
                        { "host", server.Host },
                        { "port", server.Port },
                        { "connection", created ? (keepConnection ? "created-kept" : "created-temporary") : "existing" },
                        { "projects", projects },
                    };
                }
                finally
                {
                    if (created && !keepConnection) server.DeleteConnection();
                }
            }
        }

        private static ProjectServer ResolveServer(TiaPortal portal, string host, int port, bool http, out bool created)
        {
            var existing = portal.ProjectServers.FirstOrDefault(s =>
                string.Equals(s.Host, host, StringComparison.OrdinalIgnoreCase) && (port == 0 || s.Port == port));
            if (existing != null)
            {
                created = false;
                return existing;
            }
            created = true;
            return portal.ProjectServers.Create(host, http ? Protocol.Http : Protocol.Https, host,
                port == 0 ? (http ? 80 : 443) : port);
        }

        /// <summary>Um projeto do servidor. Falha de lock/sessão vira campo "error", não aborta a lista.</summary>
        private static Dictionary<string, object> Describe(ProjectServer server, ServerProjectInfo info)
        {
            var row = new Dictionary<string, object>
            {
                { "project", info.ProjectName },
                { "serverAlias", info.ServerAlias },
            };
            try
            {
                var locks = server.GetLockStateProvider(info);
                bool locked = locks.IsProjectLocked();
                row["locked"] = locked;
                if (locked) row["lockOwner"] = locks.GetLockOwner();

                row["localSessions"] = server.GetLocalSessions(info)
                    .Select(s => new Dictionary<string, object>
                    {
                        { "sessionId", s.SessionId },
                        { "path", s.ProjectFileInfo?.FullName },
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                row["error"] = (ex.InnerException ?? ex).Message;
            }
            return row;
        }
    }
}
