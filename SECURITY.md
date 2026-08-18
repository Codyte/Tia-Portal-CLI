# Security Policy

## Scope

`tia-cli` drives an engineering tool, not a plant. The worst thing it can do by design is modify a
TIA Portal project file on the machine that runs it. Three boundaries define the scope, and they
are not the same boundary:

- **Physical CPU: never.** No verb downloads to, goes online against, or writes to real hardware.
- **Virtual CPU (S7-PLCSIM Advanced): yes, under `--apply`.** `sim-run` downloads the project
  program to a powered-on PLCSIM Advanced instance and runs it. It refuses any PC interface whose
  name is not a PLCSIM access point (`--allow-physical` is the explicit, documented opt-in for a
  renamed PLCSIM access point — it is not a way to reach a physical CPU, and a path that makes it
  one is a finding). To make the download possible, `sim-run` calls `GoOffline()` when the project
  is online.
- **Network: local and opt-in only.** `list-server-projects` connects to a TIA Project Server the
  operator names (read-only inventory; `--http` drops TLS and exists for legacy servers only), and
  `scripts/tia-help.py` talks to the TIA Portal Help Viewer on localhost. Nothing else leaves the
  machine, and no project content is ever sent anywhere.

What matters here, and what a report should be about:

- A **write verb mutating a project without `--apply`** — the dry-run contract is the main safety
  property of this tool. Known exceptions, by design: `open-project`, `create-project`,
  `save-project` and `close-project` are lifecycle verbs whose whole purpose is the effect, and
  they act without `--apply`.
- Anything that could **exfiltrate project content** (equipment names, tags, DB structure, IP
  addresses) off the machine. Beyond the three network paths listed above, a change that adds one
  is a finding.
- Anything that lets `sim-run` reach **hardware that is not a PLCSIM Advanced instance**.
- **Openness whitelist or scheduled-task handling** that could let another process run arbitrary
  code as the whitelisted `tia.exe`, or elevate through the `TiaWhitelist` / `TiaSmokeRun` tasks.
  Known boundary, so you can judge a finding against it: `TiaSmokeRun` and `TiaSimHost` run with
  the user's **limited** token — they cross Windows sessions, never a privilege level.
  `TiaWhitelist` runs with the user's **elevated** token and starts without a UAC prompt, so the
  script it executes is a copy under `%ProgramData%\tia-cli` writable only by
  Administrators/SYSTEM; the repo copy is never the task's target. A path that lets a non-admin
  process change what that task runs is a finding.
- Command injection through verb arguments into the PowerShell macros.

## Not in scope

- The Openness API itself, TIA Portal, or the Windows `Siemens TIA Openness` group model — report
  those to Siemens.
- The fact that `--apply` can destroy work in a project. That is the documented purpose of the
  flag; take backups and never point this at production, as the README says.

## Reporting

Open a [private security advisory](https://github.com/Codyte/Tia-Portal-CLI/security/advisories/new)
on the repository. Please do not open a public issue for something that lets a project be modified
or read without the operator's intent.

Include the `tia --version` output, the verb and arguments, and the TIA Portal version. Expect a
first reply within a couple of weeks — this is a single-maintainer project, not a vendor with an
on-call rotation. Be honest with yourself about that before relying on it in a critical workflow.

## Supported versions

The latest release only. There are no backports.
