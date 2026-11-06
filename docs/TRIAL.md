<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L9      1.3K  tia-cli assisted trial -->
<!--   L29     584B  Before the licence is issued -->
<!--   L42     656B  Process -->
<!--   L54     757B  Safety and privacy -->
<!-- ======================= END NAV INDEX ======================= -->

# tia-cli assisted trial

The tia-cli v3 product line is available for an assisted **2–3 day evaluation** on one engineering
machine. The trial is designed to answer one practical question: can the CLI complete a small,
agreed task safely on a copy of the evaluator's real project?

The public showcase is maintained under Codyte. The commercial evaluation package and licence
identify **START Automação Industrial Ltda.** as the supplier and licensor.

The evidenced v3 surface contains **346 unique command-line verbs** across **347 generated help
signatures** (`find` has two forms). The validation snapshot dated 2026-10-19 records 230 verbs with
successful live V21 evidence, 100 write verbs with V21 preview evidence, 43 with direct V19 evidence,
41 with direct V20 evidence and 16 with no recorded live evidence; version counts overlap.

[![REQUEST A 2–3 DAY TRIAL](https://img.shields.io/badge/REQUEST_A_2%E2%80%933_DAY_TRIAL-0A7F3F?style=for-the-badge)](https://startautomation.com.br/contato/?origem=tia-cli)

The button opens START Automação's contact page with the engineering-automation service selected and
a ready-to-edit trial request. It does not read browser or machine data; the evaluator completes the
requested fields before copying the message into their webmail.

## Before the licence is issued

The evaluator needs:

- Windows x64 with a licensed TIA Portal V19, V20 or V21 installation and Openness enabled;
- PowerShell 7, Python 3 and a local AI agent that can run shell commands;
- a test project or a copy of a project — never the production copy;
- a successful read-only `pwsh scripts/init.ps1 -Check -Json` report;
- one small success criterion agreed in writing.

The exact Portal version, installed options and proposed task are confirmed before issuance because
Siemens exposes different optional APIs across products and versions.

## Process

1. Email [contato@startautomation.com.br](mailto:contato@startautomation.com.br) with the TIA Portal version and the task you
   want to evaluate.
2. Receive the evaluation package and run its read-only installation check.
3. Send only the 16-character `license.machineId` reported by `tia --version` after the package is
   installed. Do not send the project.
4. Agree on the success criterion. The 2–3 day licence is issued only then, so setup time does not
   consume the evaluation window.
5. Work on the test-project copy. Support follows up during the evaluation and asks for error text
   when troubleshooting, not for project files.

## Safety and privacy

- Project changes are previews unless the command explicitly includes `--apply`.
- Physical online writes require both `--apply` and the additional `--allow-physical` opt-in.
- The CLI and project remain on the engineering machine; no project content or usage telemetry is
  sent to a hosted service.
- The trial licence is tied to the agreed machine and expiration date.
- A successful trial does not replace engineering review, backups, compilation, simulation or
  commissioning procedures.

The licence terms delivered with the evaluation package govern the supplied build. Product access,
commercial licensing and live demonstrations are available through
[contato@startautomation.com.br](mailto:contato@startautomation.com.br).
