# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning is
[SemVer](https://semver.org/spec/v2.0.0.html) over the **CLI contract** — verb names, flags, JSON
shapes and exit codes. A breaking change to any of those bumps MAJOR.

## [Unreleased]

<!-- Add entries here as they land; they move into the next version's section at release. -->

## [1.0.0] — 2026-08-11

First tagged release. The CLI has been exercised end to end against real TIA Portal V21 projects
and through three blind end-to-end tests (`docs/teste-cego/`), where a session receives only a
fictional machine spec and has to deliver a compiling PLC program.

### Added — the surface

- **77 verbs**, JSON in and JSON out, grouped as session · read · structure · hardware · write ·
  generators · library · multiuser · batch. Full signatures in [`docs/VERBS.md`](docs/VERBS.md).
- **`tia --version`** — CLI version plus the Openness installation this exe will load. First line
  of any bug report.
- **Six field-proven generators** (`gen-profinet`, `standardize-tags`, `gen-fault-ob`,
  `replicate-fc`, `gen-alarm-fc`, `replicate-instruments`), each with a `doctor` preflight, plus
  `audit` — project checked against a written naming and structure law.
- **Block editing by XML** (`add-db-member`, `edit-db-member`, `delete-db-member`, `add-call`,
  `delete-network`, `set-retain`) that imports with `Override`, compiles the target and re-exports
  to prove the patch landed — without that proof two consecutive writes to the same block would
  silently lose the first one.
- **`import-ladder`** — an SCL subset converted to real LAD networks.
- **Hardware**: `add-device`, `plug-module`, `set-address`, `connect-subnet`, CAx/AML
  import/export, and `insert-telegram` for SINAMICS drives (System family — telegrams there are
  not catalog submodules).
- **Installable block library**: a whole library travels as a single `.al21` and installs into a
  bare CPU with one command (`scripts/install-lib.ps1`), skipping whatever already exists.
- **`run --script ops.json`** — batch of verb calls with a single attach; a failing step becomes
  `{ok:false,error}` and the batch keeps going.

### Added — operating it without wrecking a session

- **Dry-run by default** on every write verb; nothing mutates without an explicit `--apply`.
- **Output budget**: any read verb takes `--out-file`, and output above `TIA_MAX_STDOUT`
  (60 000 chars) spills to a file on its own instead of flooding the caller.
- **`tia tree`** — whole-PLC outline as markdown, 39 KB for 476 blocks, against ~150 KB for the
  equivalent JSON.
- **`scripts/init.ps1`** — one-shot bootstrap on a new machine (three Openness gates, DLL copy
  from the local install, build, whitelist, PATH), idempotent, with `-Check` as a read-only report.
- **Windows session routing** — a shell born in session 0 cannot attach to a Portal in session 1;
  the shim routes through a scheduled task so the caller never sees the difference.
- Exit codes: `0` ok · `1` error · `2` usage · `3` file · `4` TIA/Openness · `5` timeout.

### Not included, deliberately

- **No online operations.** No go-online, no download to a PLC, no Multiuser check-in. `--apply`
  protects a project; it cannot protect a running plant. This is a closed decision, not a roadmap
  item.
- **No Siemens binaries.** `Siemens.Engineering.*` is licensed and never ships here — the build
  sources it from the local TIA install and the runtime resolves it from there.

### Known limits

- Developed and exercised against **V21 only**. The API surface used exists since V19 and
  `init.ps1` discovers V19/V20 installs, but neither has been run end to end — treat them as
  unverified. `set-tag --rename` is known to need Openness V20+.
- Windows x64 only, .NET Framework 4.8.
- The C# build cannot run in CI (it needs the licensed Openness assemblies); CI checks scripts,
  JSON and the licensing/privacy guard, while the build and offline tests run via
  `pwsh scripts/rebuild.ps1` on a machine with TIA Portal.

[1.0.0]: https://github.com/Codyte/Tia-Portal-CLI/releases/tag/v1.0.0
