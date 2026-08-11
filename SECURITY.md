# Security Policy

## Scope

`tia-cli` drives an engineering tool, not a plant. It is **offline by design**: it never goes
online, never downloads to a PLC, and never performs a Multiuser check-in. The worst thing it can
do is modify a TIA Portal project file on the machine that runs it.

What still matters here, and what a report should be about:

- A **write verb mutating a project without `--apply`** — the dry-run contract is the main safety
  property of this tool.
- Anything that could **exfiltrate project content** (equipment names, tags, DB structure, IP
  addresses) off the machine. This CLI makes no network calls; a change that introduces one is a
  finding.
- **Openness whitelist or scheduled-task handling** that could let another process run arbitrary
  code as the whitelisted `tia.exe`, or elevate through the `TiaWhitelist` / `TiaSmokeRun` tasks.
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
