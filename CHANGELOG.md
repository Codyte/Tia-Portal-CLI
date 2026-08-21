<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L9     Changelog -->
<!--   L16    [Unreleased] -->
<!--   L63    [2.0.0] — 2026-08-21 -->
<!--   L385   [1.0.0] — 2026-08-11 -->
<!-- ======================= END NAV INDEX ======================= -->

# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning is
[SemVer](https://semver.org/spec/v2.0.0.html) over the **CLI contract** — verb names, flags, JSON
shapes and exit codes. A breaking change to any of those bumps MAJOR.

## [Unreleased]

99 verbs, against 95 at 2.0.0.

### Added

- **`import-library-type --file X.al21 --name T [--folder A/B] [--apply]`** — instantiates a
  library *type* into the PLC. Siemens' own libraries keep their blocks as types, not as master
  copies: LGF V5.4.0 ships **195 types and 13 master copies**, so `import-master-copy` could not
  reach a single LGF block. The verb resolves the type by name (folder-qualified when ambiguous),
  takes the highest **committed** version and calls
  `PlcBlockComposition.CreateFrom(CodeBlockLibraryTypeVersion)` — or
  `PlcTypeComposition.CreateFrom(PlcTypeLibraryTypeVersion)` for a UDT — letting the Portal pull the
  type's dependencies along. Measured against LGF V5.4.0 in the reference project:
  `LGF_ScaleLinear` (version 3.0.1) installed as an SCL FC, PLC compile Success, 0 errors,
  0 warnings. DriveLib V7.1.0 needs none of this — its 19 objects (`SINA_POS`, `SINA_SPEED`,
  `SINA_PARA`…) are master copies, and `import-master-copy` already covers them.

- **`create-motion --name X --type T [--version V] [--folder A/B] [--apply]`,
  `delete-motion --name X [--apply] [--no-backup]` and
  `set-motion-param --name X --param P --value V [--apply]`** — technology objects are created,
  deleted and parameterised from the CLI. `TechnologicalInstanceDBComposition.Create(name, type,
  Version)` accepts only the pairs of the *Overview of technology objects and versions* table
  (`TOOpennessenUS/.../95673198603`); the API exposes no catalogue to consult beforehand, so
  without `--version` the verb inherits the version of a TO of the same type already in the PLC,
  which is the replicate-the-GUI-model case. `delete-motion` exports the instance DB to
  `workspace/recovery/` first, like every other `--force`-class delete.
- **`--search` ranks a title that starts with the term first** (`scripts/tia-help.py`). Nothing is
  filtered and `hits` is still the total: `TON` matched inside "Button" and "autonegotiation" and
  the right topic fell outside the first 15 of 828 hits. The instruction topics were always in the
  F1 index, under the language packages — `ProgKOP2MenUS` (LAD, 192 topics), `ProgSCL2MenUS` (154),
  `ProgFUP2MenUS` (197), `ProgAWL15enUS` (292).
- **`--study` knows "editor instruction"** (`docs/study-map.json`, 23 domains).

### Fixed

- **A decimal `--value` was parsed with the machine's culture** — `2.5` was written as **25** under
  pt-BR. `Hardware.Coerce` now parses and formats with `InvariantCulture`; it is the shared coercion
  of `set-attr`, `set-drive-param` and `set-motion-param`. Offline test in `Tia.Tests`.
- **Correction to 2.0.0:** the `list-motion` entry there states that a technology object cannot be
  created by the API. That was wrong, and it came from a `--sdk` search truncated at 15 hits —
  `Create` was below the cut. Measured 2026-08-21: `Create` exists and works. `Config.*` parameters
  are writable (`InputUpperLimit` 120 → 3 → 42.5, always re-read); `Retain.*` refuses with
  `EngineeringNotSupportedException` (`'set_Value' is not supported … read-only`), and
  `SetAttribute("Value", x)` lands in the same setter. The refusal only shows up on the attempt —
  no attribute declares it beforehand.

## [2.0.0] — 2026-08-21

95 verbs, against 77 at 1.0.0. The release opens three fronts the first tag did not have — HMI
screens, running the program on a simulated PLC, and SINAMICS drive parameters — and closes the
correction queue the blind tests opened (`docs/BOAS-PRATICAS.md` §3, items 3–6).

**Why MAJOR.** SemVer here is over the CLI contract, and five changes break it: a top-level `error`
now exits 1 (it exited 0), an unknown option exits 2 before the attach, `ms` no longer includes the
attach (`attachMs` carries it), `--timeout` is refused together with `--apply`, and `sim-run` no
longer runs its steps when the download failed. A caller that read `ms` as total time, or that
treated exit 0 as success while the result carried an embedded failure, needs a change.

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
- **HMI became a first-class device.** `list-hmi` (WinCC classic and Unified, with `api` saying
  which one answers), `hmi-tree` emitting `hmi-navi.md` as the sibling of `plc-navi.md`,
  `export-hmi-tags` / `import-hmi-tags` for tag tables, and the screen roundtrip
  `export-screen` / `import-screen [--replace]` / `delete-screen`. Area screens replicate through
  `import-screen --replace`; the HMI *tags* still have no import verb, which `audit-screen` reports
  one by one.
- **Objects inside a screen: `list-screen-items`, `copy-screen-items`, `set-screen-items`.** The
  list is one line per object (150 objects = 7.4 KB against 798 KB of XML) and `--group` aggregates
  by the first equipment code in the tag, returning a `region` ready to stamp. `copy-screen-items`
  copies what is *entirely* contained in the region, offsets it, renumbers `ID` and de-duplicates
  `ObjectName`. `set-screen-items` also deletes, renames and groups — `--set`, `--remove`,
  `--rename`, `--rename-from-tag`, `--group`, all repeatable and all in one export/import, because
  a screen import costs 58–123 s. Fixed order **set → remove → rename → group**, so a group's
  region is checked against geometry already corrected by `--set`. Screen objects live inside
  `Hmi.Screen.ScreenLayer`, not in the screen's `ObjectList`: pasting at the wrong level produces
  valid-looking XML that the Portal refuses on import.
- **`audit-screen`** crosses each object's tag with the HMI's own tags: does it exist, and does it
  carry an equipment code (which is how the editor's `tag1` placeholder shows up by name). Without
  `--screen` it sweeps every screen of the device at ~9.5 s each — 9 screens and 591 objects took
  86 s — so iteration goes with `--screen`. Crossing with the *PLC* tag comes back `skipped`: a
  classic HMI tag exposes only `Name`, and the table's SimaticML carries only the `Connection`.
- **The program runs and is observed.** `sim-run` attaches to S7-PLCSIM Advanced, downloads the
  program through Openness and runs the steps of a `--script`
  (`write`/`read`/`wait`/`run`/`stop`/`state`/`tags`). `scripts/sim-host.ps1`
  (`-Start`/`-Stop`/`-Status`/`-Ui`) keeps the instance alive, because the Runtime Manager comes up
  in-process and an instance registered inside `tia.exe` dies with it; the host must live in
  session 1, so `-Start` routes through the `TiaSimHost` task when the shell is born in session 0.
  `sim-diag` reads the instance with no Portal open at all. `--no-download` skips the download,
  which is ~91% of the verb, and an instance answering with 0 tags is an error, not a success.
- **`python scripts/tia-help.py --study "<task>"`** — the first stop of any PLC engineering task,
  and it runs with no Portal open. For the subject it returns the F1 topics to read, the Openness
  members, **the official Siemens library that already solves it**, the hardware restriction that
  sinks the project when discovered late, the applicable R1–R9 rules and the next verb. The curated
  knowledge is data, not code: `docs/study-map.json`. Alongside it, `--sdk` searches the 31448
  documented members of the Openness IntelliSense XML (exact signature, matches in the body, no
  service required) and `--deep` downloads and greps topic bodies, which is what answers a question
  written in prose. Two new documents: **`docs/LIMITES.md`** (what Openness and PLCSIM do *not* do,
  each line with the probe, the exact message and the way out) and **`docs/GUIA-SIEMENS.md`**
  (official guide, the free libraries — LGF, DriveLib — and where the house standard is stricter
  on purpose).
- **`list-drive-params` / `set-drive-param` — the p/r parameters of a SINAMICS drive.** They are not
  device-item attributes: `list-attrs` and `set-attr` walk `DeviceItem.GetAttributeInfos` and never
  see a drive parameter, which lives in `DriveObject.Parameters` (`DriveParameterComposition`,
  Startdrive assembly). A configured G120 answers with **5149 parameters** read offline in ~2 s, but
  the full dump is 1.1 MB of JSON, so the query is `--like` — which matches the name, the number
  *and* the parameter's description (`ParameterText`), the only way to ask for "the ramp parameter"
  without knowing it is `p1120`. `--count` is the cheap probe. A BICO parameter answers with its
  source parameter (`p840[0]` = `r2090.0`) and is refused for writing — Openness writes the value,
  not the wiring — as is the parent of an array parameter, whose value is null and therefore proves
  no type. `--value` is checked against `MinValue`/`MaxValue` in the dry run.
- **`retrieve-library --file X.zal19 [--dir D] [--upgrade]`** — the SIOS ships libraries archived
  (`.zal1x`) and every other verb here opens only `.al2x`. The Portal builds
  `<dir>/<name>/<name>.al2x` and refuses an existing destination, so an occupied path comes back as
  `action: exists` instead of throwing. `--upgrade` raises the library version in the same step,
  which is the `.zal19`-on-V21 case.
- **`list-motion [--like X] [--params]`** — technology objects (axis, cam, kinematics, PID): name,
  type and version. Read-only by API limit, not by choice: `TechnologicalInstanceDBComposition` has
  no `Create`, so a TO is born in the GUI or arrives with a project import.
- **UDTs live in folders too: `import-type --folder A/B` and `move-type --name X --folder A/B`.**
  Without `--folder` an imported UDT lands at the root. `move-type` is the same
  `export → delete → import` as `move-block`, and it puts the UDT back in its source folder if the
  import at the destination fails.
- **`set-io-address` checks `--start` against the map in the dry run** — `conflictCheck: occupied`
  plus `conflictsWith`, or `free (pelo mapa)`. It is a reading check: `free` is the absence of a
  conflict in what the map sees, not a guarantee. The authority remains the Portal's
  `Next free address: N`.
- **`audit` reports `scanned`** (`folders`, `blocks`, `callBlocks`, `tagTables`) — the size of the
  population each check walked, which is how a conforming check is told apart from a blind one.
- **`workspace/console.json`** configures the console window the task opens in session 1: `window` =
  `default` · `remember` (the default) · `hidden` · `"X,Y,W,H"`, and `show` = `none` (the default) ·
  `command` · `all`. `show` stays at `none` for a reason: a console with QuickEdit — the Windows
  default — blocks whoever writes to it while a mouse selection exists, so one click would hang the
  runner, and with it the `busy.lock` and the next call.

### Security

- **What `--force` deletes is exported first.** `import-master-copy --force`, `scaffold --force`,
  `replicate-fc` and `standardize-tags` delete before they create; the deleted object (a block, a
  UDT, a tag table, or a whole package folder) now goes to `workspace/recovery/<verb>-<timestamp>/`
  before it dies, and the path comes back in `recoveryDir`. Fail-closed: a backup that cannot be
  written aborts the delete. `--no-backup` is the explicit opt-out. There is no automatic rollback —
  Openness has no transaction, and the saved XML is what `import-block` reads back.
- **`sim-run` stops when the download fails.** `DownloadProvider.Download` returns an error count
  instead of throwing, and the code read it as information: the steps then ran against a program
  that never loaded and "passed" reading nothing. `ErrorCount > 0` or a non-`Success` state is now
  a top-level `error` (exit 1), the steps do not run, and the download messages are kept in full
  instead of the usual first 20.
- **`sim-run` no longer goes offline on its own against a non-simulated target.** `GoOffline()` is
  needed for the download, but under `--allow-physical` the online session may be the engineer's
  connection to a real CPU. It is now called only when the download target is a PLCSIM access
  point; otherwise the verb stops and asks for the human action.
- **`run --script --fail-fast`** stops at the first failing step and reports `aborted` with the
  count left. Isolating steps stays the default — it is what makes a diagnostic battery worth one
  attach — but in a chain of writes the next step works on top of what the previous one did not do.
- **The Openness whitelist is checked against the version the loader will use.** `Test-Whitelisted`
  passed if the hash matched under *any* installed version key, so a stale entry for the effective
  version read as green (V19 and V21 coexist). It now checks that version, both entries
  (`Entry`/`EntryLocal`) and the recorded `Path` — the latter resolved through junctions on both
  sides, or a correct checkout reached through a link would fail. `whitelist.ps1` also stopped
  writing under Siemens' own `AllowList` key, which is not a version, and removes that bogus entry.
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

- **`--retry` recognises a busy Portal in the language it speaks.** Busy was detected by looking
  for the English word `busy` in the message, so with the Portal in pt-BR or de-DE the retry never
  fired. There is no exception type or HResult for it (no member matching `busy` exists in the 14
  Openness assemblies), so the message is still the only signal — now matched by stem in six
  languages, and through the whole `InnerException` chain.
- **`lib/*.dll` is refreshed when the installed Openness changes.** The DLLs were copied only when
  missing, so switching the Portal Update (or going back a major) left the build referencing an API
  that is not the one the loader loads at runtime — a late failure with no symptom at install time.
  `init.ps1` now compares hashes with the installation and re-copies what differs.
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
- **A BICO parameter took `list-drive-params` down with it.** `DriveParameter` does not override
  `ToString`, and a BICO parameter's `Value` is another `DriveParameter`: the serializer walked into
  `Self referencing loop detected` at the 452nd parameter of a real G120 and the whole verb died.
  The value now comes back as the source parameter's name (`p840[0]` = `r2090.0`), which is the
  useful information anyway.
- **`sim-host.ps1` checks the host's pid**, not just the last line of the log — a stale log line
  reported a host that was no longer running.
- **The task's console window reopens where it was left.** Windows stores console position *per
  shortcut*, and a scheduled task does not run from one, so the window kept coming back to the
  host's default position. `taskrun.ps1` now writes the geometry to
  `workspace/taskio/console-rect.txt` (state, not configuration) and reads it on the next call.
- **`gen-verbs.ps1` reapplies the NAV INDEX header to `VERBS.md`**, which the regeneration used to
  strip.

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
- **`ms` is the work; `attachMs` is the attach. Breaking.** Every verb returns both, not only the
  steps of `run --script`. Total is `ms + attachMs`. The attach is where the Openness authorization
  dialog hangs, so folding it into `ms` made an environment problem read as an API cost — measured
  on the first `info` after a `rebuild.ps1` with the Portal open: `ms: 507, attachMs: 33465`, and
  `attachMs: 310` on the next call. Adding is trivial; subtracting would require remembering the
  modal existed.
- **Editing by XML is plural: N edits, one round-trip.** `add-db-member --member "A.B.NAME:Type"`,
  `delete-db-member --member "A.B.NAME"`, `delete-network --index N --index M` and
  `add-call --fb A ... --fb B ...` all repeat, and each `--inst`/`--param`/`--after`/`--title`/
  `--comment` belongs to the `--fb` before it. The cost is the size of the block, not the number of
  edits: five members in the global DB cost 23.9 s against 23.4 s for one. A patch that fails aborts
  before the import — nothing lands half-applied — and identical XML after the patches means **no
  import at all** (`changed: false`, 1.3 s instead of 23 s), so idempotence stopped being merely
  functional. Every XML-editing verb now returns **`phases`**
  (`exportMs`/`patchMs`/`importMs`/`compileMs`/`proofMs`), which is how one learns that 77% of a
  global-DB edit is `Blocks.Import(Override)` of 862 KB of XML.
- **Exports compile only their own block.** All 16 exports go through `Ops.ExportFresh`, which
  compiles the target and moves on: `clone`, `diff-block`, `explain-block`, `list-interface` and the
  four generators no longer require a `compile --apply` of the whole PLC between steps — that was
  ~20 of the 49 minutes of the FP-06 blind test. What is left is the rare case: an inconsistency
  coming from *outside* (a UDT or DB the block uses) is not cleared by compiling the block, and the
  message then asks for `compile --apply`.
- **The repo says `1. FB Bilbiotecas`**, with the project's typo, instead of silently correcting it
  in prose — the folder name is what the verbs take.

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
[2.0.0]: https://github.com/Codyte/Tia-Portal-CLI/releases/tag/v2.0.0
