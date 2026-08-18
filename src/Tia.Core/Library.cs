// ====================== BEGIN NAV INDEX ======================
// NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
//   L34    class Library
//   L36    .Open
//   L60    .Create
//   L87    .Retrieve
//   L121   .List
//   L136   .CollectMasterCopies
//   L149   .CollectTypes
//   L167   .ImportMasterCopy
//   L255   .AddMasterCopy
//   L313   .DeleteMasterCopy
//   L332   .ResolveLibFolder
//   L345   .FindMasterCopy
//   L363   .Collect
// ======================= END NAV INDEX =======================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.Library;
using Siemens.Engineering.Library.MasterCopies;
using Siemens.Engineering.Library.Types;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;

namespace Tia.Core
{
    /// <summary>Global library verbs: list contents, instantiate master copies into a PLC.</summary>
    public static class Library
    {
        private static GlobalLibrary Open(TiaSession session, string file, bool write = false)
        {
            var full = Path.GetFullPath(file);
            if (!File.Exists(full))
                throw new FileNotFoundException("Library file not found: " + full);
            // ponytail: Open a cada verbo (read-only); cache de library aberta se ficar lento
            var already = session.Portal.GlobalLibraries
                .FirstOrDefault(l => string.Equals(l.Path.FullName, full, StringComparison.OrdinalIgnoreCase));
            if (already != null)
            {
                // library ja aberta ReadOnly (list-library antes, ou a UI): reusar mata o verbo de
                // escrita com "Cannot write to read-only libraries" — fechar e reabrir ReadWrite
                if (!write || !already.IsReadOnly) return already;
                var user = already as UserGlobalLibrary;
                if (user == null)
                    throw new InvalidOperationException("Library '" + full
                        + "' is open read-only and is not a user library (cannot reopen for write).");
                user.Close();
            }
            return session.Portal.GlobalLibraries.Open(new FileInfo(full),
                write ? OpenMode.ReadWrite : OpenMode.ReadOnly);
        }

        /// <summary>Creates an empty user global library (.al2x). bake-lib needs the file to exist.</summary>
        public static object Create(TiaSession session, string file, bool apply)
        {
            var full = Path.GetFullPath(file);
            var dir = Path.GetDirectoryName(full);
            var name = Path.GetFileNameWithoutExtension(full);
            var exists = File.Exists(full);
            var result = new Dictionary<string, object>
            {
                { "library", name }, { "path", full },
                { "action", exists ? "exists" : "create" }, { "applied", false }
            };
            if (exists || !apply) return result;
            // Create() recebe a pasta-mãe e o nome, e monta <dir>\<name>\<name>.al2x — o caminho
            // real volta em "path" (não é necessariamente o --file que entrou)
            Directory.CreateDirectory(dir);
            var lib = session.Portal.GlobalLibraries.Create<UserGlobalLibrary>(new DirectoryInfo(dir), name);
            result["path"] = lib.Path.FullName;
            result["applied"] = true;
            return result;
        }

        /// <summary>Dearquiva biblioteca .zal1x → .al2x. É o que falta para consumir biblioteca
        /// oficial da Siemens (LGF, DriveLib): o SIOS entrega arquivada, e todo o resto do CLI
        /// (list-library, import-master-copy, install-lib.ps1) só abre a dearquivada.
        /// --upgrade usa RetrieveWithUpgrade: .zal19 de biblioteca antiga vira .al21 no mesmo passo.
        /// Retrieve monta &lt;dir&gt;/&lt;nome&gt;/&lt;nome&gt;.al2x e recusa destino já existente, então
        /// destino ocupado devolve action "exists" em vez de estourar.</summary>
        public static object Retrieve(TiaSession session, string file, string dir, bool upgrade, bool apply)
        {
            var archive = Path.GetFullPath(file);
            if (!File.Exists(archive))
                throw new FileNotFoundException("Archived library not found: " + archive);
            var name = Path.GetFileNameWithoutExtension(archive);
            var target = Path.GetFullPath(dir ?? Path.GetDirectoryName(archive));
            var landing = Path.Combine(target, name);
            var existing = Directory.Exists(landing)
                ? Directory.GetFiles(landing, "*.al2*").FirstOrDefault()
                    ?? Directory.GetFiles(landing, "*.al1*").FirstOrDefault()
                : null;
            var result = new Dictionary<string, object>
            {
                { "archive", archive },
                { "library", name },
                { "targetDir", target },
                { "upgrade", upgrade },
                { "action", existing != null ? "exists" : "retrieve" },
                { "path", existing },
                { "applied", false },
            };
            if (existing != null || !apply) return result;
            Directory.CreateDirectory(target);
            var lib = upgrade
                ? session.Portal.GlobalLibraries.RetrieveWithUpgrade(
                    new FileInfo(archive), new DirectoryInfo(target), OpenMode.ReadWrite)
                : session.Portal.GlobalLibraries.Retrieve(
                    new FileInfo(archive), new DirectoryInfo(target), OpenMode.ReadWrite);
            result["path"] = lib.Path.FullName;
            result["applied"] = true;
            return result;
        }

        public static object List(TiaSession session, string file)
        {
            var lib = Open(session, file);
            var copies = new List<object>();
            CollectMasterCopies(lib.MasterCopyFolder, "", copies);
            var types = new List<object>();
            CollectTypes(lib.TypeFolder, "", types);
            return new Dictionary<string, object>
            {
                { "library", lib.Name },
                { "masterCopies", copies },
                { "types", types },
            };
        }

        private static void CollectMasterCopies(MasterCopyFolder folder, string path, List<object> into)
        {
            foreach (MasterCopy copy in folder.MasterCopies)
                into.Add(new Dictionary<string, object>
                {
                    { "folder", path },
                    { "name", copy.Name },
                    { "contentType", copy.ContentDescriptions.Select(d => d.ContentType.ToString()).FirstOrDefault() },
                });
            foreach (MasterCopyUserFolder sub in folder.Folders)
                CollectMasterCopies(sub, path.Length == 0 ? sub.Name : path + "/" + sub.Name, into);
        }

        private static void CollectTypes(LibraryTypeFolder folder, string path, List<object> into)
        {
            foreach (LibraryType type in folder.Types)
                into.Add(new Dictionary<string, object>
                {
                    { "folder", path },
                    { "name", type.Name },
                });
            foreach (LibraryTypeUserFolder sub in folder.Folders)
                CollectTypes(sub, path.Length == 0 ? sub.Name : path + "/" + sub.Name, into);
        }

        /// <summary>Instantiates a block master copy into the PLC (--folder A/B optional).
        /// --force = reinstalar por cima: CreateFrom recusa nome que já existe no PLC (não tem
        /// Override como o import de XML), então apaga o objeto de mesmo nome e cria de novo.
        /// Num pacote isso apaga a pasta inteira com seus blocos. Quebra o vínculo chamada↔iDB de
        /// quem chamava o bloco — em CPU virgem não custa nada, num PLC populado o chamador precisa
        /// ser reimportado depois (mesma cicatriz do move-block, PLANO).</summary>
        public static object ImportMasterCopy(TiaSession session, PlcSoftware plc, string file,
            string copyName, string folderPath, bool apply, bool force = false)
        {
            var lib = Open(session, file);
            var copy = FindMasterCopy(lib.MasterCopyFolder, copyName);
            if (copy == null)
                throw new InvalidOperationException(
                    "Master copy '" + copyName + "' not found in library '" + lib.Name + "'.");
            var result = new Dictionary<string, object>
            {
                { "library", lib.Name },
                { "masterCopy", copyName },
                { "folder", folderPath ?? "" },
                { "applied", apply },
            };
            var content = copy.ContentDescriptions.Select(d => d.ContentType).FirstOrDefault();
            var isFolder = content == typeof(PlcBlockUserGroup);
            var isType = content != null && typeof(PlcType).IsAssignableFrom(content);
            var isTable = content != null && typeof(PlcTagTable).IsAssignableFrom(content);
            result["contentType"] = content == null ? "" : content.Name;
            if (apply)
            {
                // UDT e tabela de tag também são IMasterCopySource e podem estar soltos na library
                // (foi assim que "Aferição CMD" apareceu). Cada um mora na sua composition — mandar
                // tudo pra Blocks.CreateFrom só dá erro de cast.
                var segments = string.IsNullOrEmpty(folderPath)
                    ? new List<string>() : folderPath.Trim('/').Split('/').ToList();
                var group = isType || isTable ? null : Ops.ResolveFolder(plc, folderPath, true);
                Func<string> create;
                Action deleteExisting;
                if (isType)
                {
                    create = () => Scaffold.ResolveTypePath(plc, segments, true).Types.CreateFrom(copy).Name;
                    deleteExisting = () =>
                    {
                        var old = Ops.FindType(plc.TypeGroup, copyName);
                        if (old != null) { Ops.Backup(old); old.Delete(); }
                    };
                }
                else if (isTable)
                {
                    create = () => Scaffold.ResolveTagPath(plc, segments, true).TagTables.CreateFrom(copy).Name;
                    deleteExisting = () =>
                    {
                        var old = Ops.FindTagTable(plc.TagTableGroup, copyName);
                        if (old != null) { Ops.Backup(old); old.Delete(); }
                    };
                }
                else if (isFolder)
                {
                    create = () => group.Groups.CreateFrom(copy).Name;   // pasta = pacote inteiro
                    deleteExisting = () =>
                    {
                        var old = group.Groups.Find(copyName);
                        if (old != null) { Ops.Backup(old); old.Delete(); }
                    };
                }
                else
                {
                    create = () => group.Blocks.CreateFrom(copy).Name;
                    deleteExisting = () =>
                    {
                        var old = Ops.FindBlock(plc, copyName);
                        if (old != null) { Ops.Backup(old); old.Delete(); }
                    };
                }
                result["action"] = "created";
                // --force apaga ANTES de criar: CreateFrom de pasta/bloco que já existe no alvo não
                // levanta — o Portal batiza "1.5 Diagnóstico_1" em silêncio e o PLC fica com o pacote
                // duplicado (medido: 34 → 68 blocos, compile Error). O catch abaixo nunca disparava
                // nesse caso; segue valendo pra colisão de nome em OUTRA pasta, que aí sim levanta.
                if (force) { deleteExisting(); result["action"] = "deleted+created"; }
                try { result["created"] = create(); }
                catch (Exception ex) when (force && Scaffold.AlreadyInAnotherFolder(ex))
                {
                    // colisão de nome: o alvo de mesmo nome sai da frente e o pacote entra inteiro.
                    // Se o que colide for outro objeto (bloco do pacote parado noutra pasta), não há
                    // o que apagar aqui e o retry levanta o mesmo erro — que é o certo a mostrar.
                    deleteExisting();
                    result["created"] = create();
                    result["action"] = "deleted+created";
                }
            }
            return result;
        }

        /// <summary>Cria master copy na global library a partir de um bloco ou de uma pasta do PLC
        /// (pasta = pacote: o grupo inteiro vira um master copy só).</summary>
        public static object AddMasterCopy(TiaSession session, PlcSoftware plc, string file,
            string blockName, string folderPath, string libFolder, bool apply)
        {
            if (string.IsNullOrEmpty(blockName) == string.IsNullOrEmpty(folderPath))
                throw new InvalidOperationException("pass exactly one of --name (bloco) or --folder (pasta do PLC).");

            IMasterCopySource source;
            string sourceName;
            if (!string.IsNullOrEmpty(blockName))
            {
                // bloco, UDT ou tabela de tag — os três são IMasterCopySource, e o molde precisa
                // dos três: sem UDT e tag na .al21, install-lib depende de XML solto fora do Git
                object found = (object)Ops.FindBlock(plc, blockName)
                    ?? (object)Ops.FindType(plc.TypeGroup, blockName)
                    ?? (object)Ops.FindTagTable(plc.TagTableGroup, blockName);
                if (found == null)
                    throw new InvalidOperationException("'" + blockName
                        + "' not found as block, PLC data type or tag table.");
                source = found as IMasterCopySource;
                if (source == null)
                    throw new InvalidOperationException("'" + blockName + "' is not a master copy source.");
                sourceName = ((IEngineeringObject)found).GetAttribute("Name") as string;
            }
            else
            {
                var group = Ops.ResolveFolder(plc, folderPath, false) as PlcBlockUserGroup;
                if (group == null)
                    throw new InvalidOperationException("Folder '" + folderPath + "' not found (raiz não é master copy source).");
                source = group;
                sourceName = group.Name;
            }

            var result = new Dictionary<string, object>
            {
                { "file", Path.GetFullPath(file) },
                { "source", sourceName },
                { "kind", string.IsNullOrEmpty(blockName) ? "folder" : "block" },
                { "libraryFolder", libFolder ?? "" },
                { "applied", apply },
            };
            if (!apply) return result;

            var lib = Open(session, file, true);
            result["library"] = lib.Name;
            var target = ResolveLibFolder(lib.MasterCopyFolder, libFolder);
            var existing = target.MasterCopies.Find(sourceName);
            if (existing != null) existing.Delete();
            // o Portal batiza sozinho ("Copy of Function blocks in X") — renomeia pro nome da fonte
            var copy = target.MasterCopies.Create(source);
            copy.Name = sourceName;
            var user = lib as UserGlobalLibrary;
            if (user != null) user.Save();   // global library só grava em disco no Save explícito
            result["created"] = copy.Name;
            result["replaced"] = existing != null;
            return result;
        }

        /// <summary>Apaga um master copy da global library (rebake deixa lixo com nome antigo).</summary>
        public static object DeleteMasterCopy(TiaSession session, string file, string name, bool apply)
        {
            var lib = Open(session, file, apply);
            var copy = FindMasterCopy(lib.MasterCopyFolder, name);
            if (copy == null)
                throw new InvalidOperationException(
                    "Master copy '" + name + "' not found in library '" + lib.Name + "'.");
            var result = new Dictionary<string, object>
            {
                { "library", lib.Name }, { "masterCopy", name }, { "applied", apply },
            };
            if (!apply) return result;
            copy.Delete();
            var user = lib as UserGlobalLibrary;
            if (user != null) user.Save();
            result["deleted"] = name;
            return result;
        }

        private static MasterCopyFolder ResolveLibFolder(MasterCopyFolder root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            var folder = root;
            foreach (var part in path.Trim('/').Split('/'))
                folder = (MasterCopyFolder)folder.Folders.Find(part) ?? folder.Folders.Create(part);
            return folder;
        }

        /// <summary>Busca por nome em toda a library. Aceita "PASTA/NOME" pra desempatar: o mesmo
        /// nome pode existir em níveis diferentes (arrastar o bloco pra raiz e pra dentro de uma
        /// pasta cria dois master copies homônimos), e devolver o primeiro achado escolheria em
        /// silêncio. Mesma política do --portal: ambíguo recusa e lista.</summary>
        private static MasterCopy FindMasterCopy(MasterCopyFolder root, string name)
        {
            var slash = name.LastIndexOf('/');
            var wantFolder = slash < 0 ? null : name.Substring(0, slash);
            var wantName = slash < 0 ? name : name.Substring(slash + 1);

            var hits = new List<KeyValuePair<string, MasterCopy>>();
            Collect(root, "", wantName, hits);
            if (wantFolder != null)
                hits = hits.Where(h => string.Equals(h.Key, wantFolder, StringComparison.OrdinalIgnoreCase)).ToList();
            if (hits.Count == 0) return null;
            if (hits.Count > 1)
                throw new InvalidOperationException("Master copy '" + wantName + "' existe em "
                    + hits.Count + " pastas da library (" + string.Join(", ",
                        hits.Select(h => h.Key.Length == 0 ? "(raiz)" : h.Key)) + "). Passe --name \"PASTA/NOME\".");
            return hits[0].Value;
        }

        private static void Collect(MasterCopyFolder folder, string path, string name,
            List<KeyValuePair<string, MasterCopy>> into)
        {
            var hit = folder.MasterCopies.Find(name);
            if (hit != null) into.Add(new KeyValuePair<string, MasterCopy>(path, hit));
            foreach (MasterCopyUserFolder sub in folder.Folders)
                Collect(sub, path.Length == 0 ? sub.Name : path + "/" + sub.Name, name, into);
        }
    }
}
