using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;

namespace Tia.Core
{
    /// <summary>
    /// Read-only preflight for the F3 generator verbs: verifies every folder, template block,
    /// DB and tag group a verb needs before any dry-run/apply. Mutates nothing. Without --verb
    /// runs all verbs (config-required ones are skipped unless --config is given with --verb).
    /// </summary>
    public static class Doctor
    {
        private static readonly string[] AllVerbs =
        {
            "gen-profinet", "standardize-tags", "gen-fault-ob",
            "replicate-fc", "gen-alarm-fc", "replicate-instruments",
        };

        // deserialize: JSON path + type -> config object (lives in the CLI; Core stays JSON-free)
        public static object Run(TiaSession session, PlcSoftware plc, string verb, string configPath,
            Func<string, Type, object> deserialize)
        {
            if (verb != null && !AllVerbs.Contains(verb))
                throw new ArgumentException("Unknown verb '" + verb + "' for doctor. Supported: " + string.Join(", ", AllVerbs) + ".");

            var reports = new List<object>();
            foreach (var v in verb != null ? new[] { verb } : AllVerbs)
                reports.Add(CheckVerb(session, plc, v, configPath, deserialize));

            bool allOk = reports.Cast<Dictionary<string, object>>()
                .All(r => (string)r["status"] != "fail");
            return new Dictionary<string, object>
            {
                { "plc", plc.Name },
                { "ok", allOk },
                { "reports", reports },
            };
        }

        private static Dictionary<string, object> CheckVerb(TiaSession session, PlcSoftware plc,
            string verb, string configPath, Func<string, Type, object> deserialize)
        {
            var checks = new List<object>();
            Action<string, bool, string> check = (name, ok, detail) =>
            {
                var c = new Dictionary<string, object> { { "check", name }, { "ok", ok } };
                if (detail != null) c["detail"] = detail;
                checks.Add(c);
            };
            T Load<T>() where T : new() =>
                configPath != null ? (T)deserialize(configPath, typeof(T)) : new T();
            bool needsConfig = verb == "gen-profinet" || verb == "replicate-fc" || verb == "replicate-instruments";
            if (needsConfig && configPath == null)
                return new Dictionary<string, object>
                {
                    { "verb", verb }, { "status", "skipped" },
                    { "detail", "needs --verb " + verb + " --config <file.json>" },
                };

            try
            {
                switch (verb)
                {
                    case "gen-profinet":
                    {
                        var c = Load<ProfinetConfig>();
                        bool hasDevices = c.Devices != null && c.Devices.Count > 0;
                        check("config devices list", hasDevices, hasDevices ? c.Devices.Count + " mapping(s)" : "empty 'Devices'");
                        var io = Profinet.FindIoDeviceNames(session);
                        var missing = hasDevices
                            ? c.Devices.Where(d => !io.Contains(d.Hardware, StringComparer.OrdinalIgnoreCase))
                                .Select(d => d.Hardware).ToList()
                            : new List<string>();
                        check("IO devices resolve", missing.Count == 0,
                            missing.Count == 0 ? io.Count + " IO device(s) in project" : "not found: " + string.Join(", ", missing));
                        var folder = Profinet.FindTagGroup(plc.TagTableGroup, c.TagFolder);
                        check("tag folder '" + c.TagFolder + "'", folder != null,
                            folder == null ? "missing — table '" + c.TagTable + "' cannot be created" : null);
                        break;
                    }
                    case "standardize-tags":
                    {
                        var c = Load<StandardizeConfig>();
                        check("root tag folder '" + c.RootFolder + "'",
                            Profinet.FindTagGroup(plc.TagTableGroup, c.RootFolder) != null, null);
                        check("memory sets", c.MemorySets != null && c.MemorySets.Count > 0,
                            (c.MemorySets?.Count ?? 0) + " set(s)");
                        break;
                    }
                    case "gen-fault-ob":
                    {
                        var c = Load<FaultObConfig>();
                        check("template OB '" + c.TemplateOb + "'", Ops.FindBlock(plc, c.TemplateOb) != null, null);
                        check("alarm DB '" + c.AlarmDb + "'", Ops.FindBlock(plc, c.AlarmDb) != null, null);
                        var tasks = FaultOb.DiscoverTasks(session, c);
                        check("device groups '" + c.GroupPrefix + "*'", tasks.Count > 0,
                            tasks.Count + " group(s) with modules");
                        var langs = session.Project.LanguageSettings.ActiveLanguages
                            .Select(l => l.Culture.Name).ToList();
                        bool anyCulture = c.CommentCultures.Any(x => langs.Contains(x, StringComparer.OrdinalIgnoreCase));
                        check("comment cultures", true, anyCulture ? null
                            : "none of [" + string.Join(", ", c.CommentCultures) + "] active; falls back to '" + langs.FirstOrDefault() + "'");
                        break;
                    }
                    case "replicate-fc":
                    {
                        var c = Load<ReplicateFcConfig>();
                        // mirrors Run: "A/B" is a path under Program blocks, bare name searched anywhere
                        var wf = string.IsNullOrEmpty(c.BlocksFolder) ? null
                            : c.BlocksFolder.Contains("/")
                                ? Ops.ResolveFolder(plc, c.BlocksFolder, false) as PlcBlockUserGroup
                                : ReplicateFc.FindGroup(plc.BlockGroup, c.BlocksFolder);
                        check("blocks folder '" + c.BlocksFolder + "'", wf != null, null);
                        check("equipment types", c.EquipmentTypes != null && c.EquipmentTypes.Count > 0,
                            (c.EquipmentTypes?.Count ?? 0) + " type(s)");
                        check("UDT names", c.UdtNames != null && c.UdtNames.Count > 0,
                            (c.UdtNames?.Count ?? 0) + " UDT(s)");
                        check("global DB '" + c.GlobalDb + "'", Ops.FindBlock(plc, c.GlobalDb) != null, null);
                        break;
                    }
                    case "gen-alarm-fc":
                    {
                        var c = Load<AlarmFcConfig>();
                        var tf = ReplicateFc.FindGroup(plc.BlockGroup, c.TemplateFolder);
                        check("template folder '" + c.TemplateFolder + "'", tf != null, null);
                        check("template FC '" + c.TemplateFc + "'", tf?.Blocks.Find(c.TemplateFc) != null, null);
                        check("OB template '" + c.ObTemplate + "'", Ops.FindBlock(plc, c.ObTemplate) != null, null);
                        check("global DB '" + c.GlobalDb + "'", Ops.FindBlock(plc, c.GlobalDb) != null, null);
                        check("alarm tags folder '" + c.AlarmTagsFolder + "'",
                            Profinet.FindTagGroup(plc.TagTableGroup, c.AlarmTagsFolder) != null, null);
                        check("start tags folder '" + c.StartTagsFolder + "'",
                            Profinet.FindTagGroup(plc.TagTableGroup, c.StartTagsFolder) != null, null);
                        check("master FB '" + c.MasterFb + "'", Ops.FindBlock(plc, c.MasterFb) != null, null);
                        bool targetExists = ReplicateFc.FindGroup(plc.BlockGroup, c.TargetRootFolder) != null;
                        check("target root '" + c.TargetRootFolder + "'", true,
                            targetExists ? null : "missing — created on apply");
                        break;
                    }
                    case "replicate-instruments":
                    {
                        var c = Load<InstrumentFcConfig>();
                        check("source tags folder '" + c.SourceTagsFolder + "'",
                            !string.IsNullOrEmpty(c.SourceTagsFolder) && Profinet.FindTagGroup(plc.TagTableGroup, c.SourceTagsFolder) != null, null);
                        var target = string.IsNullOrEmpty(c.TargetBlocksFolder)
                            ? null : ReplicateFc.FindGroup(plc.BlockGroup, c.TargetBlocksFolder);
                        check("target blocks folder '" + c.TargetBlocksFolder + "'", target != null, null);
                        check("template FC under target", target != null && AnyFc(target),
                            target == null ? null : "first FC in natural order is the template");
                        check("global DB '" + c.GlobalDb + "'", Ops.FindBlock(plc, c.GlobalDb) != null, null);
                        if (!string.IsNullOrEmpty(c.TargetOb))
                            check("call OB '" + c.TargetOb + "'", Ops.FindBlock(plc, c.TargetOb) != null, null);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                check("exception", false, ex.Message);
            }

            bool okAll = checks.Cast<Dictionary<string, object>>().All(x => (bool)x["ok"]);
            return new Dictionary<string, object>
            {
                { "verb", verb },
                { "status", okAll ? "ok" : "fail" },
                { "checks", checks },
            };
        }

        private static bool AnyFc(PlcBlockGroup group)
        {
            if (group.Blocks.OfType<FC>().Any()) return true;
            foreach (PlcBlockGroup sub in group.Groups)
                if (AnyFc(sub)) return true;
            return false;
        }
    }
}
