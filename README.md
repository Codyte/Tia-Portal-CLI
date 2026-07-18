# tia-cli

**JSON-in / JSON-out CLI for the Siemens TIA Portal Openness API (V19+).**
Every operation is a verb: `tia list-blocks`, `tia export-block --name FC_Pumps`,
`tia compile --apply`. stdout is always JSON, stderr is human log, exit codes are stable —
built to be driven by AI agents (Claude Code and friends) and by engineers who prefer a shell
over ClickOps.

Extracted from field-proven automation scripts for water-treatment PLC projects
(`Scripts_Siemens/FINAIS/`, kept as read-only reference).

```
> tia info
{
  "project": "SmokeTest_01",
  "plcs": [ { "device": "PLC_1", "plc": "PLC_1" } ],
  "devices": 21
}
```

## Design contract

- **Dry-run by default.** Every write verb previews its changes as JSON; nothing mutates the
  project without an explicit `--apply`. An agent cannot wreck a project by accident.
- **Attach first.** The CLI attaches to the running TIA Portal instance (opening/creating
  projects is also supported: `open-project`, `create-project`, `save-project`, `close-project`).
- **Offline only.** No go-online, no download to PLC, no Multiuser check-in — a human does that
  in the Portal. Compile is the only "heavy" operation exposed.
- **One call at a time.** Openness is not thread-safe for this use; never run two `tia`
  processes in parallel.
- **XML roundtrip as the core primitive.** Export SimaticML → transform → import. High-level
  verbs are built on top of it.

## Requirements

- Windows, TIA Portal **V19 or newer** with Openness installed (V21 tested).
- .NET Framework 4.8 (runtime) / .NET SDK 8 (build). Target is `net48` x64.
- `Siemens.Engineering.dll` is **not** in this repo (Siemens license). At build time a local
  copy under `lib/` (gitignored) is used; at runtime the exe resolves the DLL from the installed
  Portal (`TIA_ENGINEERING_DIR` env var → exe folder → default V21/V20/V19 install paths).

## Setup (the three Openness gates)

1. Your Windows user must be in the **`Siemens TIA Openness`** group — and you need a fresh
   logon after being added (an old token doesn't carry the group).
2. The exe must be **whitelisted** in the Openness registry
   (`HKLM\...\Openness\<ver>\Whitelist`): path, file hash and timestamp.
   `scripts/whitelist.ps1` writes the correct entry; re-run after every rebuild (hash changes).
   `scripts/rebuild.ps1` does build + tests + whitelist in one shot.
3. The CLI must run in the **same interactive session** as the TIA Portal UI (a service /
   scheduled-task session cannot attach).

First run against a Portal without whitelist entry triggers the Openness consent popup — allow it.

## Build & smoke

```powershell
pwsh scripts/rebuild.ps1          # dotnet build + offline tests + whitelist (UAC only if exe changed)
tia doctor                        # read-only preflight: checks templates/folders each generator needs
```

Binary: `src\Tia.Cli\bin\Debug\net48\tia.exe`. Offline tests (`Tia.Tests`, plain asserts, no
TIA required) cover the pure XML generators.

## Verbs

Run `tia --help` for the full, always-current list. Summary:

| Group | Verbs |
|-------|-------|
| Session | `open-project`, `create-project`, `save-project`, `close-project` |
| Read | `info`, `list-devices`, `list-blocks`, `list-tags`, `list-types`, `list-hmi`, `find`, `snapshot`, `xref`, `tree`, `export-block`, `export-tags`, `export-type` |
| Structure | `create-folder`, `delete-folder`, `delete-block`, `import-type` |
| Hardware | `add-device`, `set-address`, `connect-subnet`, `export-cax`, `import-cax` (AML) |
| Write | `import-block`, `import-source`, `import-ladder` (SCL subset → LAD), `import-tags`, `compile`, `diff-block` |
| Generators | `gen-profinet`, `standardize-tags`, `gen-fault-ob`, `replicate-fc`, `gen-alarm-fc`, `replicate-instruments` — ports of the field-proven scripts; `doctor` preflights them |
| Library | `list-library`, `import-master-copy` |
| Batch | `run --script ops.json` — array of verb calls, one attach for all |

Global options: `--plc NAME` (multi-PLC projects), `--out DIR` (default `workspace\exports`),
`--apply`, `--retry N` (busy retry, default 3), `--timeout SEC`.
Exit codes: `0` ok · `1` error · `2` usage · `3` file · `4` TIA/Openness · `5` timeout.

Generator configs are plain JSON files — see `docs/examples/` (`profinet.json`,
`replicate-fc.json`, `gen-all.json` batch that dry-runs all six generators in one attach).

## Workflow macros (PowerShell)

| Macro | Does |
|-------|------|
| `scripts/rebuild.ps1` | build + offline tests + whitelist refresh |
| `scripts/use-project.ps1 <Name>` | ensure a project is open (no-op if already; close current without save + open) |
| `scripts/prep-project.ps1 <Name>` | use-project + `doctor` + `compile --apply` + save — real projects often arrive uncompiled and every export dies until compiled |
| `scripts/raio-x.ps1 <Name>` | read-only X-ray → `workspace/<project>/`: doctor, snapshot, devices, tags, types, block outline, CAx AML, xref of every OB |
| `scripts/clone-hw.ps1 <From> <To> [-Apply]` | copy hardware between projects via CAx export/import |

## Limitations

- No online operations (by design, v1).
- WinCC Unified screens can't be exported/imported as XML — Openness doesn't expose SimaticML
  for Unified. `list-hmi` covers inventory only.
- Multiuser projects: attach works single-user style; check-in stays in the Portal.
- `import-ladder` converts a deliberate SCL subset (bool logic, comparators, Set/Reset/MOVE);
  it rejects anything else with a clear error.

## Docs

Project docs under `docs/` are in Portuguese (plan, real-project findings). Code and CLI are
English.

## License

[MIT](LICENSE). `Siemens.Engineering.dll` and TIA Portal are Siemens products and are not
covered or distributed by this repo.
