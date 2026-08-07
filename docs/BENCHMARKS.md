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

### The failure that is not a bug

An earlier run of the same batch, without a `compile` between the import and the next export:

```
"error": "Error when calling method 'Export' of type 'Siemens.Engineering.SW.Blocks.GlobalDB'.
          Inconsistent blocks and PLC data types (UDT) cannot be exported.",
"type": "EngineeringTargetInvocationException"
```

Openness refuses to export an inconsistent block, and every import leaves its target — and anything
referencing it — inconsistent. So a write verb that exports under the hood needs `compile --apply`
before it. The CLI turns that bare message into the exact `tia compile` command to run, because
discovering this rule by trial and error costs an afternoon.
