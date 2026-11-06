<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L9      1.0K  tia-cli assisted trial -->
<!--   L25     584B  Before the licence is issued -->
<!--   L38     656B  Process -->
<!--   L50     1.4K  Safety and privacy -->
<!-- ======================= END NAV INDEX ======================= -->

# tia-cli assisted trial

The tia-cli v3 product line is available for an assisted **2–3 day evaluation** on one engineering
machine. The trial is designed to answer one practical question: can the CLI complete a small,
agreed task safely on a copy of the evaluator's real project?

The public showcase is maintained under Codyte. The commercial evaluation package and licence
identify **START Automação Industrial Ltda.** as the supplier and licensor.

[![REQUEST A 2–3 DAY TRIAL](https://img.shields.io/badge/REQUEST_A_2%E2%80%933_DAY_TRIAL-0A7F3F?style=for-the-badge)][trial-request]
[![REQUEST VIA START WEBSITE](https://img.shields.io/badge/REQUEST_VIA_START_WEBSITE-145DA0?style=for-the-badge)](https://startautomation.com.br/contato/?origem=tia-cli)

The green button opens the configured email application with the recipient, subject and checklist
filled in. The blue button opens START Automação's contact page. Neither reads browser or machine
data; the evaluator completes the requested fields before sending.

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

[trial-request]: mailto:contato@startautomation.com.br?subject=tia-cli%20assisted%20trial%20request&body=Hello%2C%0D%0A%0D%0AI%20would%20like%20to%20request%20the%202%E2%80%933%20day%20assisted%20tia-cli%20trial.%0D%0A%0D%0ASource%3A%20GitHub%20public%20showcase%0D%0AName%3A%20%0D%0ACompany%3A%20%0D%0AReply%20email%3A%20%0D%0ACountry%20and%20time%20zone%3A%20%0D%0ATIA%20Portal%20version%20%28V19%2FV20%2FV21%29%3A%20%0D%0AWindows%20version%3A%20%0D%0AAI%20agent%20%28Codex%2FClaude%20Code%2FCursor%2Fother%29%3A%20%0D%0ATest-project%20copy%20ready%20%28yes%2Fno%29%3A%20%0D%0ATask%20I%20want%20to%20evaluate%3A%20%0D%0A%0D%0AI%20will%20not%20send%20project%20files%20or%20credentials.
