<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L8     Changelog -->
<!--   L15    [Unreleased] -->
<!--   L188   [1.0.0] — 2026-08-11 -->
<!-- ======================= END NAV INDEX ======================= -->

# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning is
[SemVer](https://semver.org/spec/v2.0.0.html) over the **CLI contract** — verb names, flags, JSON
shapes and exit codes. A breaking change to any of those bumps MAJOR.

## [Unreleased]

Closes the correction queue that the blind tests opened
(`docs/BOAS-PRATICAS.md` §3, items 3–6). All four are documented as either shipped or
deliberately dropped.

### Added

- **`list-io-map [--device X] [--io Input|Output]`** — every I/O address in the project:
  device, item path, `%IB…`/`%QB…` range, bit and byte length, plus the next free byte per I/O
  type. Answers the question that previously needed the GUI or an 18-call probe: `list-attrs`
  does not show addresses (they are not `DeviceItem` attributes) and `list-telegrams` does not
  carry the drive telegram's address. Walks item *and* descendants, because the `Address` objects
  live on the submodule. Addresses that exist in the model but are not assigned
  (`StartAddress == -1`, e.g. the interface and ports of an ET200SP with no cards) are counted in
  `unassigned` and kept out of the map, where they would otherwise read as `%IB-1`.
- **`audit` — four new checks**, taking it to ten: `R1 · o PLC tem UDT`, `R2 · DB global sem
  escalar solto na raiz`, `R8 · bloco de chamada em linguagem gráfica`, and `CHAMADA_* fora da
  pasta de área`. A check that cannot run reports `skipped` with the reason and does **not** fail
  the project.
- **`audit --db "DB GLOBAL"`** — names the global DB for the R2 check when the heuristic (a
  `GlobalDB` with "global" in its name) does not find it.

### Security

- **`sim-run` refuses any PC interface that is not a PLCSIM access point.** `--pc-interface` matched
  by substring and `FindTarget` took the first target under it, so a physical PN/IE interface was
  reachable — against the "never download to real hardware" decision. The name is now checked before
  the download *and* on the resolved target; `--allow-physical` is the explicit opt-in for a renamed
  PLCSIM access point.
- **Unknown options are rejected (exit 2) before the attach.** The parser read the options it knew
  and ignored the rest, so `gen-alarm-fc --ara AREA --apply` silently lost its scope and regenerated
  every area. Known options live in `Program.KnownOptions`, and the offline test `Cli.KnownOptions`
  fails when a source literal is missing from it.
- **`--timeout` is refused together with `--apply`.** A timed-out write is abandoned mid-call, with
  no cancellation and no rollback, leaving the project in an unknown state.
- **`SECURITY.md` now describes what the tool actually does.** It still claimed the CLI never goes
  online, never downloads and makes no network calls, while `sim-run` (download + `GoOffline`),
  Project Server and the local Help Viewer exist. The scope is now three explicit boundaries:
  physical CPU never, virtual PLC under `--apply`, network local/opt-in only.

- **The `TiaWhitelist` task no longer executes a script the user can rewrite.** It runs with the
  user's *elevated* token (`S4U` + `RunLevel Highest`) and is startable without a UAC prompt — by
  design, so a rebuild does not need a click. Its action pointed at `scripts/whitelist.ps1` inside
  the checkout, which lives in the user's profile and is writable without admin: the task ACL
  protects the *action*, not the *file the action runs*, so any process running as the user could
  rewrite that script, start the task and get elevated execution for free. `setup-tasks.ps1` now
  copies the script to `%ProgramData%\tia-cli` with inheritance broken, **ownership set to
  Administrators** (an object's owner can always rewrite its own DACL, so leaving the user as
  owner would hand back the write the ACL just removed) and write limited to
  Administrators/SYSTEM, then registers the task against the copy, passing the checkout in
  `-Repo`.
  `init.ps1 -Check` fails the task gate when the copy drifts from the original (a `git pull` that
  changed `whitelist.ps1`) or `-Repo` names a different checkout.
- **`smokeloop.ps1` quoted arguments naively** (`'"' + $_ + '"'`), so a verb argument containing a
  quote or ending in a backslash split the command line. Both taskio runners now share
  `ConvertTo-CmdLine` (`_common.ps1`), which follows `CommandLineToArgvW` as `taskrun.ps1` already
  did.

### Fixed

- **`move-block` puts the block back when the import fails.** It exports, deletes and imports into
  the destination; an import that failed (name clash, folder refused) left the block deleted with a
  temp XML as the only copy. Failures now try the original folder first and report `restoredTo`, and
  a partial move reports `error` (so exit is 1, not 0).
- **Ambiguous hardware item names are refused.** `FindItem` returned the first match of a recursive
  search, so a name that repeats across levels or racks (`Rack_0`, `Port_1`) could be written to on
  the wrong item. Matches are now collected with their paths and an ambiguous name fails listing
  them; `--item "Parent/Child"` disambiguates.
- **`set-address` no longer picks an interface at random.** A device with more than one network
  interface (CPU X1/X2) now requires `--item`, and the chosen one is reported as `interface`.
- **`list-io-map` reports drives it could not read.** A telegram read that threw was swallowed, so a
  map missing exactly the addresses `nextFreeByte` cannot see still looked complete. It now carries
  `unreadableDrives` / `scanErrors[]` and forces `nextFreeByteExact: false`.
- **Only one `tia` call at a time, enforced by the exe.** The cross-process lock existed only on the
  scheduled-task route; two interactive terminals could break D9 (Openness is single-session). A
  named mutex now fails the second call fast, on both routes.
- **`bytes` in the spill stub is the file's UTF-8 size** (`FileInfo.Length`), not UTF-16 char count;
  the char count is still there as `chars`.

- **`sim-run` validates the whole script before the download.** A short step array or an unknown op
  only failed while executing, after the ~91% of the verb that is the download; `wait` went straight
  into `Thread.Sleep`, so an extra zero slept for hours. Steps are now checked for shape, arity and
  wait budget (10 min per step and per script) up front.
- **Usage errors that looked like Portal errors now exit 2.** `FormatException` (a bad number in
  `--retry`/`--max`/`--pos`), `OverflowException` and invalid JSON fell through to the generic 1;
  `DirectoryNotFoundException` now exits 3 like a missing file.
- **Generator configs reject unknown properties.** Newtonsoft ignored them by default, so a typo in
  `TemplateFolder` silently fell back to the wide default — the same damage as an ignored CLI
  option, through the other door. Keys starting with `_` stay allowed (JSON has no comments).
- **`init.ps1` takes all Openness DLLs from a single PublicAPI root**, instead of finding each one
  in the first Portal that has it (which could mix majors in `lib/`), and re-checks all three tasks
  after the UAC step instead of only `TiaWhitelist`'s existence.

- **Exit code is honest for failures embedded in the result.** Verbs that caught an error into an
  `error` field and returned normally (`sim-run` above all) exited 0, and the batch marked the step
  `ok: true`. A top-level `error` is now exit 1 and counts in the batch's `failed`.
- **The PLCSIM Advanced DLL no longer lands in `bin/`.** It was `Private=true`, so every build
  copied it next to the exe and `pack.ps1` aborted on "Siemens.* in the package" — the next release
  would either ship a Siemens DLL or not build. It is now resolved at runtime from
  `Common Files\Siemens\PLCSIMADV\API` (or `TIA_PLCSIM_DIR`), like the Openness assemblies.
- **`audit` says when a check could not run.** A skipped check still reports `ok: true` (it does not
  fail the project), but the result now carries `complete` and `skippedChecks[]`, so `ok: true` with
  `complete: false` reads as unproven instead of conformant.
- **`init.ps1` no longer accepts V17/V18 as the TIA Portal gate.** Only V19+ counts; older installs
  are listed separately as unsupported.
- **`pack.ps1` fails on a dirty working tree** instead of warning. The zip copies the files on disk
  under the names Git tracks, so a dirty tree published uncommitted changes stamped with a commit
  that does not contain them.

- **`Invoke-Tia` serialises with an atomic lock** instead of a `$task.State -eq 'Running'` test.
  The test was TOCTOU: two callers could both pass it, both write the fixed-name `cmd.json`, and
  the second — whose task start the scheduler drops (`IgnoreNew`) — only found out at the 600 s
  timeout. `busy.lock` is created with `CreateNew` (fails atomically if present); an orphan lock is
  collected by mtime with the task stopped as a second witness. A timeout deliberately keeps the
  lock: the verb may still be running inside the task, and releasing it would let the next call
  land on a live Openness session (D9).
- **`taskrun.ps1` reads `cmd.json` inside the `try`.** A missing or corrupt `cmd.json` threw before
  the `try`, so `exit-<id>.txt` was never written and the client only learned at the timeout —
  exactly the failure the `catch` exists to prevent.
- **`smokeloop.ps1` no longer uses `-Wait`.** `Start-Process -Wait` waits for the process *and its
  descendants*, and the TIA Portal that `tia.exe` starts is a descendant — measured in
  `taskrun.ps1`, which switched to `WaitForExit()` for that reason. The two runners now agree.
- **`tia-help.py` anchors `workspace/` to the repo, not the cwd.** `SKILL.md` tells you to call it
  by absolute path from anywhere; with cwd-relative defaults every new working directory got its
  own index — re-streaming the ~350 MB TOC and rebuilding the 5.8 MB SDK index each time.
- **`install-lib.ps1` passes `--full` to `list-devices`.** Every other `ConvertFrom-Json` consumer
  already did: without it a large project spills to a file and the pipe parses the stub, so
  `$haveDev` came back empty and an existing device looked absent.
- **`raio-x.ps1` on a project with no OBs.** `0..($obs.Count - 1)` is `0..-1` = `@(0, -1)` in
  PowerShell, which emitted two `xref --name $null` steps.
- **`sim-host.ps1 -Start` passes `-Article` to the detached host**, quoted — an MLFB has a space in
  it, and `Start-Process -ArgumentList` with an array quotes nothing. A custom CPU silently fell
  back to the default `6ES7 515-2AN03-0AB0`.
- **`whitelist.ps1` reports a missing `tia.exe`** instead of throwing from `Get-Item`, and disposes
  the SHA256 provider.

### Changed

- **`create-folder --path` is repeatable** — `--path A/B --path C/D` builds a whole tree on one
  attach instead of one attach (~7 s) per folder. A path that fails becomes `{path, error}` and
  the rest keep going, as `run --script` does with steps. **The JSON shape changed**: the verb now
  returns `{kind, paths, created, failed, applied, folders[]}` instead of a single folder object.
- **`\/` in any folder path is a literal slash** (`--path "1. I\/OS/QA-01"`). The rule lives in
  `Ops.SplitPath`, under the longest-match of `WalkFolders`, so it applies to every verb that takes
  a folder path — not just `create-folder`. Until now longest-match could only resolve a name
  containing `/` that already existed; creating `1. I/OS` silently produced two nested folders.
- **`audit` is no longer purely read-only**: the R2 check exports the global DB to `--out`
  (default `workspace/exports`), because only the export shows each member's datatype.
- **Customer plant names removed from the docs.** The two reference projects are now named after
  the role they play: `Software de ETE Modelo_Inicial_V21` (the in-house template project) and
  `PROJETO-ASBUILT_V21` (the field as-built, outside the standard). Copying a
  command straight out of the prose therefore needs the local `.ap21` name substituted. The root
  `__navi__.md` no longer maps `proj/` and `workspace/` at all — gitignored payload whose folder
  names are customer project names. History was not rewritten and `library/*.json` keeps the
  object names that anchor `import-master-copy --force`.

- **`scripts/navi-cs.ps1` deleted — navindex indexes C# now.** One command regenerates every map:
  `python ~/.claude/skills/navindex/scripts/navindex.py <folder|.>`. Each `.cs` above 300 lines
  also gets a NAV INDEX header at the top of the file, so the full `case "verb"` table (all 78,
  with line numbers) is the first read of `src/Tia.Cli/Program.cs`; the folder maps
  (`src/Tia.Cli/__navi__.md`, `src/Tia.Core/__navi__.md`, `src/Tia.Tests/__navi__.md`) replace the
  single hand-rolled `src/__navi__.md`. The same navindex change makes the walk skip gitignored
  paths, so `proj/`, `workspace/` and `Scripts_Siemens/` can no longer put customer project names
  back into the committed tree.

### Deliberately not done

- **Teaching `import-ladder` to convert `CALL`.** R8 is already unblocked by `add-call`, which
  builds the LAD network straight into the block's XML from the FB interface. A second route to
  the same destination would duplicate the expensive part (resolving pin types, assembling
  `Access`/`Wires`), and a `#local` as a parameter stays out of reach either way.

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
  from the local install, build, whitelist, PATH), idempotent, with `-Check` as a read-only
  report of the 8 gates plus live state (which is informational and does not change the exit).
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
