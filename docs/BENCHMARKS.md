<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L12    Measured behaviour -->
<!--   L22    Time to answer, per verb -->
<!--   L37    One attach instead of five -->
<!--   L52    Output volume — the constraint nobody expects -->
<!--   L67    The write path — cost is the block, not the edit -->
<!--   L155   What is not measured here -->
<!--   L170   A real cycle, captured -->
<!-- ======================= END NAV INDEX ======================= -->

# Measured behaviour

Numbers taken from real runs, not estimates. Everything here is reproducible with the commands
shown — if a figure cannot be reproduced, it is a bug in this document.

**Method.** Windows 10 x64, TIA Portal V21, `tia.exe` net48 Debug build, attaching to a Portal
already open with the project loaded. Wall clock via PowerShell `Measure-Command` around the same
shim a human would type (`scripts/tia.ps1`), so process start, session routing and attach are all
inside the number — this is time-to-answer, not time-in-API.

## Time to answer, per verb

Project `Base_tia_cli`, PLC `PLC_TESTE` (small: 3 global DBs, 1 CPU).

| Command | Wall clock |
|---|---|
| `xref --name "DB GLOBAL" --out-file …` | 2.0 s |
| `tree` | 3.0 s |
| `list-blocks --count` | 3.0 s |
| `audit --max 5` | 4.0 s |
| `snapshot --out-file …` | 8.1 s |

Roughly 2 s of every call is process start plus Openness attach. That fixed cost is why batch mode
exists.

## One attach instead of five

The same five commands above, run two ways:

| Shape | Wall clock |
|---|---|
| Five separate `tia` calls | 20.1 s |
| `run --script bench-batch.json --summary` | 8.1 s |

**2.5× on five steps**, and the ratio grows with the step count because the saving is one attach
per call avoided. A real generator batch is dozens of steps.

A failing step becomes `{ok:false,error,type}` and the batch continues; the process exits 1 if any
step failed. So a batch where some failures are expected still runs in one attach.

## Output volume — the constraint nobody expects

An LLM driving this CLI pays for every byte it reads, and re-pays each turn. Volume is a
first-class design constraint, not a nicety.

| Same question, two ways | Bytes |
|---|---|
| `snapshot` on `PLC_TESTE` (full JSON inventory) | 219.5 KB |
| `tree` on `PLC_TESTE` (markdown outline) | 2.6 KB |

On a real project (476 blocks, 194 tag tables, 13 UDTs) `tree` produces a 39 KB / 309-line outline
in about 4 s, against roughly 150 KB for the equivalent JSON. `find --pattern "*" --kind tag` on
that project is 821 KB — which is why every read verb accepts `--out-file F.json`: the full JSON
goes to the file and stdout returns `{file,bytes,count,head}`.

## The write path — cost is the block, not the edit

Measured 2026-08-19 on the real template project, against its global DB. Every XML-editing verb
(`add/edit/delete-db-member`, `add-call`, `delete-network`, `set-retain`) runs the same envelope:
export a freshly compiled block, patch the XML, import with `Override`, compile, re-export to prove
the patch landed.

| Call | Wall clock |
|---|---|
| `info` (control — proves no authorization dialog is hanging) | 3.4 s |
| `add-db-member` dry-run (compile + export of the whole DB) | 9.5 s |
| `add-db-member --apply` (one Bool member under a new struct) | 47.9 s |
| `delete-db-member --apply` (removing that struct) | 25.6 s |
| `compile --block --apply` afterwards | Success, 0 errors / 0 warnings |

**The control call is not optional.** When the `tia.exe` hash changes with the Portal already open,
Openness raises a modal authorization dialog in the interactive session; every call then hangs at
~0 % CPU until a human clicks it. A number captured in that state measures the dialog, not the API —
an earlier internal note recorded ">600 s" for this exact verb for that reason. If `info`, the
cheapest call there is, does not answer in seconds, the environment is what you are timing.

**Since F16 the result says so itself.** Every verb returns `ms` — the work alone — plus
`attachMs`, the attach, which is where that dialog hangs. Total is `ms + attachMs`, and no control
call is needed to tell the two apart. First `info` after a rebuild with the Portal open:
`{"ms": 507, "attachMs": 33465}` — half a second of verb, 33 s waiting for a click. The very next
call: `{"ms": 571, "attachMs": 310}`. A step inside `run --script` has always reported work-only
`ms`; a standalone verb now agrees with it.

**The cost scales with the block, not with the number of edits.** Ten members added one call at a
time are ten full round-trips of the same DB. Since F16 (2026-08-19) the envelope takes N edits, and
the projection above is now measured — same project, same DB, same session:

| Call | Wall clock (`ms` from the verb itself) |
|---|---|
| `info` (control) | 0.9 s |
| `add-db-member` dry-run, 3 members | 1.3 s |
| `add-db-member --apply`, **1** member (new struct) | 23.4 s |
| `add-db-member --apply`, **5** members (one of them under a second new struct) | 23.9 s |
| `add-db-member --apply`, 2 members **that already exist** | 1.3 s |
| `delete-network --apply --index 2 --index 4` (6-network OB clone) | 1.8 s |
| `add-call --apply --fb A --fb B` (two FC calls into the same OB) | 6.8 s |
| `delete-block` + `delete-db-member` + `compile --block` (cleanup batch) | 25.0 s, 0 errors |

Five members in one call cost the same as one (23.9 s vs 23.4 s) — **4.9×** against the five
sequential calls the CLI used to require. And a call whose edits all turn out to be no-ops now
compares the XML and imports nothing (1.3 s instead of ~23 s): before F16, idempotence was
functional, so the second `--apply` still reimported and recompiled an identical block.

The dry-run figures are low because the DB was already compiled; the 9.5 s above includes the
compile that `ExportFresh` does when the block arrives dirty.

### The 17-minute delete: hypothesis refuted, and what the floor really is

`delete-db-member` of one mid-DB member during the 2026-08-19 clean-up cost **1 009 968 ms
(17 min)**. It was not a modal dialog (`attachMs: 324`) and not the expensive `ImportAndProve`
branch (no `workspace/telemetry.log` was ever written, so the whole-PLC compile never ran). The
first hypothesis — non-optimized DB, so deleting in the middle makes the Portal re-address every
member below — **is wrong**. Measured against it, same project family, same `DB GLOBAL`
(`MemoryLayout: Standard`, ~5 558 members, 862 KB of XML), one batch, one attach:

| Call (`--apply`, `ZZ_TESTE_*` members) | `ms` | `importMs` | `compileMs` |
|---|---|---|---|
| `add`, **6** members inside `PAINEIS` (the DB's first struct) | 38.4 s | 26.2 s | 11.1 s |
| `delete`, **1** member in that same middle position | 44.2 s | 34.9 s | 8.2 s |
| `delete`, **5** members in the middle, one call | 46.3 s | 34.4 s | 10.7 s |
| `add`, 1 member **at the end** of the DB | 43.6 s | 34.3 s | 8.2 s |
| `delete`, that member **at the end** | 44.1 s | 34.3 s | 8.3 s |
| `compile --apply --errors` (whole PLC, already clean) | 11.1 s | — | — |

Batch total 228 s, `attachMs` 369, `errors: 0`.

Three things fall out, and only the third is a guess:

1. **Position does not matter.** Middle and end cost the same, within noise. The offset story is dead.
2. **Count is nearly free.** Five deletions in one call cost 2 s more than one — ~0.5 s per extra
   member against a ~44 s floor. Deleting everything in a single call is the right move, just not
   for the reason first assumed.
3. **The floor is the round-trip, and 77 % of it is `importMs`** — `Blocks.Import(Override)` of an
   862 KB DB, ~34 s, before any compile. `compileMs` is 8–11 s, export and patch are ~0.1 s
   together. Nothing in the CLI can shrink the import; the only lever is calling it fewer times.

So the 17 min came from something outside the member itself. The open hypothesis — **not measured
yet** — is the state of the PLC at that moment: that call came right after F1/F2 had re-imported
ten tag tables and deleted folders, so `compiler.Compile()` on the DB may have dragged a dirty
program along with it. Here the project was already clean and `compileMs` stayed at 8–11 s. The
test that would settle it is to dirty the PLC on purpose (e.g. edit a widely used UDT), then time
one `delete-db-member`.

## What is not measured here

**The manual baseline.** The honest comparison — "this took N hours in the GUI" — has to come from
the engineer who did it by hand, with a stopwatch, on the same project. It is deliberately left
blank rather than estimated:

| Flow | By hand, in the Portal | With `tia-cli` |
|---|---|---|
| Install the block library into a bare CPU (blocks, UDTs, tag tables, instance DBs, clock byte, hardware) | _to be measured_ | one `install-lib` call, re-runnable as a no-op |
| Replicate the FC set for a new instrument | _to be measured_ | one `replicate-instruments` call |
| Read-only X-ray of an unfamiliar project | _to be measured_ | one `raio-x.ps1` call |

**A screen recording.** Still needs a human at the keyboard; the transcript below is real captured
output, but it is not a demo video.

## A real cycle, captured

Adding and removing a member of a global DB, as a batch. This is verbatim output from
`run --script`, trimmed to the fields that matter.

```jsonc
// tia run --script del-member-test.json
{ "steps": 6, "failed": 0, "results": [
  { "verb": "delete-db-member", "result": { "action": "missing (no-op)", "applied": false }},
  { "verb": "delete-db-member", "result": { "action": "missing (no-op)", "applied": false }},
  { "verb": "add-db-member",    "result": { "action": "create",  "applied": true }},
  { "verb": "compile",          "result": { "state": "Success", "errors": 0 }},
  { "verb": "delete-db-member", "result": { "action": "delete",  "applied": true,
      "warning": "deleting a member does not fix its references — check `xref --name DB_DUMMY`." }},
  { "verb": "compile",          "result": { "state": "Success", "errors": 0 }}
]}
```

Three things in that output are the whole design contract:

1. **`applied: false` is the default.** The dry-run reports the exact same action it would take.
2. **Absent means no-op, not error.** Re-running a batch is safe, which is what makes it a
   reconciliation tool rather than a script.
3. **The warning is part of the result.** Deleting a member does not fix the code referencing it,
   and the tool says so in the payload instead of leaving it to be discovered at compile time.

### The failure the CLI now absorbs

An older run of the same batch, without a `compile` between the import and the next export:

```
"error": "Error when calling method 'Export' of type 'Siemens.Engineering.SW.Blocks.GlobalDB'.
          Inconsistent blocks and PLC data types (UDT) cannot be exported.",
"type": "EngineeringTargetInvocationException"
```

Openness refuses to export an inconsistent block, and every import leaves its target inconsistent.
The rule used to be "run `compile --apply` on the whole PLC between steps" — which in a real
project is minutes per step, and was 20 of the 49 minutes of one measured end-to-end run.

Since 2026-08-13 every export in the CLI goes through one helper that compiles **the target block
alone** and continues; a block compile is seconds. The expensive path is still there for the case
that needs it: an inconsistency coming from *outside* the block (a UDT or DB it uses) is not
cleared by compiling the block, and then the error message names the `compile --apply` to run.
Paying the cheap cost always and the expensive one rarely is the whole change.
