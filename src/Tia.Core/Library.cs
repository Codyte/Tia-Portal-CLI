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
            if (already != null) return already;
            return session.Portal.GlobalLibraries.Open(new FileInfo(full),
                write ? OpenMode.ReadWrite : OpenMode.ReadOnly);
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
            if (apply)
            {
                var group = Ops.ResolveFolder(plc, folderPath, true);
                var isFolder = copy.ContentDescriptions
                    .Any(d => d.ContentType == typeof(PlcBlockUserGroup));
                Func<string> create = () => isFolder
                    ? group.Groups.CreateFrom(copy).Name   // master copy de pasta = pacote inteiro
                    : group.Blocks.CreateFrom(copy).Name;
                result["action"] = "created";
                try { result["created"] = create(); }
                catch (Exception ex) when (force && Scaffold.AlreadyInAnotherFolder(ex))
                {
                    // colisão de nome: o alvo de mesmo nome sai da frente e o pacote entra inteiro.
                    // Se o que colide for outro objeto (bloco do pacote parado noutra pasta), não há
                    // o que apagar aqui e o retry levanta o mesmo erro — que é o certo a mostrar.
                    if (isFolder)
                    {
                        var old = group.Groups.Find(copyName);
                        if (old != null) old.Delete();
                    }
                    else
                    {
                        var old = Ops.FindBlock(plc, copyName);
                        if (old != null) old.Delete();
                    }
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
                var block = Ops.FindBlock(plc, blockName);
                if (block == null) throw new InvalidOperationException("Block '" + blockName + "' not found.");
                source = block as IMasterCopySource;
                if (source == null)
                    throw new InvalidOperationException("Block '" + blockName + "' is not a master copy source.");
                sourceName = block.Name;
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

        private static MasterCopy FindMasterCopy(MasterCopyFolder folder, string name)
        {
            var hit = folder.MasterCopies.Find(name);
            if (hit != null) return hit;
            foreach (MasterCopyUserFolder sub in folder.Folders)
            {
                hit = FindMasterCopy(sub, name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
