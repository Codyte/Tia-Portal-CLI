<div align="center">

# ⚡ tia-cli

**Drive Siemens TIA Portal from the command line — JSON in, JSON out.**

*Every Openness operation as a shell verb. Built for AI agents and for engineers
who prefer a terminal over ClickOps.*

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET Framework 4.8](https://img.shields.io/badge/.NET-Framework%204.8-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![TIA Portal V19+](https://img.shields.io/badge/TIA%20Portal-V19%20%7C%20V20%20%7C%20V21-009999)](https://www.siemens.com/tia-portal)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D6?logo=windows)](#requirements)
[![Dry--run first](https://img.shields.io/badge/writes-dry--run%20by%20default-orange)](#design-contract)

```
> tia info                          > tia find --pattern "FC_Pump*"
{                                   [
  "project": "SmokeTest_01",          { "kind": "block", "name": "FC_Pump_01",
  "plcs": [ { "plc": "PLC_1" } ],       "folder": "4. Motores", "type": "FC" }
  "devices": 21                     ]
}
```

**40+ verbs** · inventory & xref · SimaticML export/import · hardware via CAx/AML ·
SCL→LAD converter · 6 field-proven code generators · batch mode · one attach

</div>

---

## Why

TIA Portal automation today means clicking, or writing a one-off C# Openness app per task.
`tia-cli` collapses that into a single whitelisted exe: stdout is always JSON, stderr is human
log, exit codes are stable, and **every write is a dry-run unless you pass `--apply`** — safe
enough to hand to an AI agent, fast enough for a human in a hurry.

```mermaid
flowchart LR
    A["🤖 AI agent / engineer<br/>(shell)"] -->|"tia &lt;verb&gt; --json args"| B["tia.exe<br/>(net48 x64, whitelisted)"]
    B -->|Openness API| C["TIA Portal V19+<br/>(running instance)"]
    B -->|SimaticML / AML / CSV| D[("workspace/<br/>exports")]
    C --> E["PLC project<br/>(offline)"]
```

Extracted from field-proven automation scripts for water-treatment PLC projects
(`Scripts_Siemens/FINAIS/`, kept as read-only reference).

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

## Verbs

Run `tia --help` for the full, always-current list.

| Group | Verbs |
|-------|-------|
| 🔌 Session | `open-project` · `create-project` · `save-project` · `close-project` |
| 🔍 Read | `info` · `list-devices` · `list-blocks` (`--folder` · `--type` · `--count`) · `list-tags` · `list-types` · `list-hmi` · `find` · `snapshot` · `xref` · `tree` · `export-block` · `export-tags` · `export-type` |
| 🗂️ Structure | `create-folder` · `delete-folder` (`--tags`/`--types`) · `delete-block` · `delete-type` ·
`move-block` (export→delete→import, o Openness não move) · `import-type` · `scaffold` (folder tree + template blocks from a manifest, idempotent) |
| 🛠️ Hardware | `add-device` · `set-address` · `connect-subnet` · `export-cax` · `import-cax` (AML) |
| ✍️ Write | `import-block` · `import-source` · `import-ladder` (SCL subset → LAD) · `import-tags` · `compile` · `diff-block` |
| ⚙️ Generators | `gen-profinet` · `standardize-tags` · `gen-fault-ob` · `replicate-fc` · `gen-alarm-fc` · `replicate-instruments` — plus `doctor`, a read-only preflight that checks every template/folder they need |
| 📚 Library | `list-library` · `import-master-copy` — installable block library in [`library/`](library/README.md) (manifest is versioned, XML payload is not) |
| 📦 Batch | `run --script ops.json` — array of verb calls, one attach for all |

Global options: `--plc NAME` (multi-PLC projects), `--out DIR` (default `workspace\exports`),
`--apply`, `--retry N` (busy retry, default 3), `--timeout SEC`.
Exit codes: `0` ok · `1` error · `2` usage · `3` file · `4` TIA/Openness · `5` timeout.

Generator configs are plain JSON — see [`docs/examples/`](docs/examples/), including
[`gen-all.json`](docs/examples/gen-all.json), a batch that dry-runs all six generators in one attach:

```powershell
tia run --script docs/examples/gen-all.json
```

## Quick start

```powershell
git clone https://github.com/Codyte/Tia-Portal-CLI.git tia-cli && cd tia-cli
pwsh scripts/init.ps1    # checks the 3 gates below, copies lib/ DLLs from your TIA install,
                          # builds, runs offline tests, whitelists, puts `tia` on PATH
                          # — one shot for a new machine
pwsh scripts/init.ps1 -Check    # read-only: what is installed, what is missing (exit 1 if any)
```

`init.ps1` is idempotent — re-run it after `git pull`. Besides the build it sets the `TIA_CLI_HOME`
user variable, adds `scripts/` to your user PATH (so `tia <verb>` works from any directory, always
through the session-routing shim — never call `tia.exe` directly) and checks that this checkout
lives at `~/.claude/skills/tia`. The repo root *is* the Claude Code skill — [`SKILL.md`](SKILL.md)
teaches any session how to drive this CLI, from any project folder. Clone it as a submodule of your
skills repo; keep a single checkout (the Openness whitelist is keyed by the `tia.exe` path).

`init.ps1` reports and stops if a gate needs a human (Windows group membership, .NET SDK, or a
TIA Portal V21+ install to source the Openness DLLs from) — fix what it flags and re-run. Once it
prints `init ok`, open TIA Portal manually with a test project (`tia` attaches to a running
instance, it doesn't launch one) and:

```powershell
tia doctor                    # preflight: is the open project ready for the generators?
tia snapshot                  # full inventory of the open project, as JSON
tia standardize-tags          # dry-run: what would change
tia standardize-tags --apply  # do it
```

First attach without a whitelist entry triggers an Openness consent popup in the Portal UI —
click allow, it won't ask again for that exe hash. After this first-time setup, use
`pwsh scripts/rebuild.ps1` for subsequent rebuilds (same build+test+whitelist, skips the gate
checks and lib/ copy).

<details>
<summary><b>Requirements</b></summary>

- Windows, TIA Portal **V19 or newer** with Openness installed (V21 tested).
- .NET Framework 4.8 (runtime) / .NET SDK 8 (build). Target is `net48` x64.
- `Siemens.Engineering.dll` is **not** in this repo (Siemens license). At build time a local
  copy under `lib/` (gitignored) is used; at runtime the exe resolves the DLL from the installed
  Portal (`TIA_ENGINEERING_DIR` env var → exe folder → default V21/V20/V19 install paths).

</details>

<details>
<summary><b>Setup — the three Openness gates</b></summary>

1. Your Windows user must be in the **`Siemens TIA Openness`** group — and you need a fresh
   logon after being added (an old token doesn't carry the group).
2. The exe must be **whitelisted** in the Openness registry
   (`HKLM\...\Openness\<ver>\Whitelist`): path, file hash and timestamp.
   `scripts/whitelist.ps1` writes the correct entry; re-run after every rebuild (hash changes).
   `scripts/rebuild.ps1` does build + tests + whitelist in one shot.
3. The CLI must run in the **same interactive session** as the TIA Portal UI (a service /
   scheduled-task session cannot attach).

First run against a Portal without whitelist entry triggers the Openness consent popup — allow it.

</details>

<details>
<summary><b>Workflow macros (PowerShell)</b></summary>

| Macro | Does |
|-------|------|
| `scripts/init.ps1` | first-time bootstrap: checks the 3 gates, copies `lib/` DLLs from the local TIA install, then rebuild |
| `scripts/rebuild.ps1` | build + offline tests + whitelist refresh |
| `scripts/use-project.ps1 <Name>` | ensure a project is open (no-op if already; close current without save + open) |
| `scripts/prep-project.ps1 <Name>` | use-project + `doctor` + `compile --apply` + save — real projects often arrive uncompiled and every export dies until compiled |
| `scripts/raio-x.ps1 <Name>` | read-only X-ray → `workspace/<project>/`: doctor, snapshot, devices, tags, types, block outline, CAx AML, xref of every OB |
| `scripts/clone-hw.ps1 <From> <To> [-Apply]` | copy hardware between projects via CAx export/import |

</details>

<details>
<summary><b>Limitations</b></summary>

- No online operations (by design, v1).
- WinCC Unified screens can't be exported/imported as XML — Openness doesn't expose SimaticML
  for Unified. `list-hmi` covers inventory only.
- Multiuser projects: attach works single-user style; check-in stays in the Portal.
- `import-ladder` converts a deliberate SCL subset (bool logic, comparators, Set/Reset/MOVE);
  it rejects anything else with a clear error.
- Openness refuses to export an inconsistent block, and every import leaves its target (and any
  block referencing it) inconsistent. So compile between steps — `clone`, `diff-block` and
  `explain-block` all export under the hood, and the generators export the global DB first.
  The CLI turns the bare Openness message into the exact `tia compile` command to run.

</details>

## Docs

Project docs under [`docs/`](docs/) are in Portuguese (plan, real-project findings). Code and
CLI are English.

## License

[MIT](LICENSE). *TIA Portal*, *Openness* and `Siemens.Engineering.dll` are Siemens products —
not affiliated with, endorsed by, or distributed by this project.
