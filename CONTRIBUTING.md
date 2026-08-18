# Contributing

Bug reports and small, verified patches are welcome. Read this first — this repo has one unusual
constraint that shapes everything.

## The constraint

**You cannot build or test this project without a licensed TIA Portal installation.** `Tia.Core`
compiles against `Siemens.Engineering.*`, which is licensed, is not in this repo, and does not
exist on any CI runner. There is no way around it, and no maintainer can validate a change for you
on a machine that lacks the Portal.

Consequence: **CI cannot tell you whether your PR works.** It only checks that scripts parse, JSON
is valid, no licensed or customer-owned file got committed, and the version has a changelog entry.
The real verification is what you ran locally, and you have to say what that was.

## Setup

```powershell
git clone https://github.com/Codyte/Tia-Portal-CLI.git tia-cli && cd tia-cli
pwsh scripts/init.ps1          # gates, DLL copy from your install, build, whitelist, PATH
pwsh scripts/init.ps1 -Check   # read-only: what is and isn't in place
```

`init.ps1` stops and tells you which gate needs a human — Windows group membership (plus a fresh
logon), the .NET SDK, or a TIA Portal V19+ install to source the Openness DLLs from.

## Before opening a PR

1. `pwsh scripts/rebuild.ps1` — build, offline tests, whitelist refresh. It must end green.
2. Add a case to `src/Tia.Tests/Program.cs` for any pure logic you touched (XML transforms, naming
   rules, planners). It is a plain assert harness with no framework; follow the file.
3. Exercise the verb against a **test project**, never a production one, and never online.
4. If you changed the help text, `rebuild.ps1` regenerates `docs/VERBS.md` — commit it.
5. **A new option needs an entry in `Program.KnownOptions`.** Unknown options fail with exit 2
   before the attach, and the offline test `Cli.KnownOptions` greps the sources for `"--x"`
   literals — a new option without an entry fails the build, not a user's project.
6. If you changed C# structure, `python ~/.claude/skills/navindex/scripts/navindex.py src`
   regenerates the `src/*/__navi__.md` maps and the in-file NAV INDEX headers — commit them.
7. Say in the PR **which TIA Portal version you ran against** and what you actually executed. A
   change nobody ran against a Portal cannot be merged, however obviously correct it looks.

## What will be rejected

- **Anything that ships a Siemens binary**, or any file under `lib/`. CI enforces this.
- **Anything carrying customer project data** — exported XML/AML carries equipment names, tags and
  DB structure. Contribute sanitized or original material only. CI enforces the paths.
- **Online operations** — go-online, download to PLC, Multiuser check-in. This is a closed design
  decision (see the *Design contract* in the README), not a missing feature.
- **A write verb without a dry-run path.** Every write previews as JSON and mutates only under
  `--apply`. No exceptions.
- Verbs that parallelize `tia` calls. Openness is single-session.

## Style

- C# targets `net48`, `LangVersion 7.3` — no newer language features, they will not compile.
- Comments explain *why*, and especially why an obvious alternative does not work. This codebase
  is full of Openness behaviour that costs a day to rediscover; write it down at the call site.
- Verb output is JSON on stdout, errors as `{"error": ...}` with the right exit code.

## Reporting a bug

Use the issue templates. `tia --version` output is the first thing asked for — it reports the CLI
version and which Openness installation the exe loads, which is half of most diagnoses.
