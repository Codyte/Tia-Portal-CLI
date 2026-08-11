## What and why

<!-- What changes, and what problem it solves. If it works around Openness behaviour, say which. -->

## How it was verified

CI cannot build this project (licensed Openness assemblies), so this section *is* the verification.

- [ ] `pwsh scripts/rebuild.ps1` ends green (build + offline tests + whitelist)
- [ ] Test case added to `src/Tia.Tests/Program.cs` for the pure logic touched — or N/A, because:
- [ ] Ran against a real TIA Portal, version: `______`
- [ ] Ran against a **test** project, never production, never online

Command(s) actually executed:

```powershell

```

## Contract

- [ ] Write verb previews as JSON and mutates only under `--apply` — or this is a read verb
- [ ] `docs/VERBS.md` regenerated if the help changed
- [ ] `src/*/__navi__.md` + NAV INDEX headers regenerated (`navindex.py src`) if C# structure changed
- [ ] `CHANGELOG.md` updated under *Unreleased*
- [ ] No Siemens binary and no customer project data in the diff
