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
        private static GlobalLibrary Open(TiaSession session, string file)
        {
            var full = Path.GetFullPath(file);
            if (!File.Exists(full))
                throw new FileNotFoundException("Library file not found: " + full);
            // ponytail: Open a cada verbo (read-only); cache de library aberta se ficar lento
            return session.Portal.GlobalLibraries.Open(new FileInfo(full), OpenMode.ReadOnly);
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

        /// <summary>Instantiates a block master copy into the PLC (--folder A/B optional).</summary>
        public static object ImportMasterCopy(TiaSession session, PlcSoftware plc, string file,
            string copyName, string folderPath, bool apply)
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
                var block = group.Blocks.CreateFrom(copy);
                result["created"] = block.Name;
            }
            return result;
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
