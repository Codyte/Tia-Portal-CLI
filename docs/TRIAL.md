<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill: https://github.com/Codyte/navindex) -->
<!--   L9      1.1K  tia-cli 5-day free trial -->
<!--   L28     464B  Before you start -->
<!--   L39     1.2K  Process -->
<!--   L55     765B  Safety and privacy -->
<!-- ======================= END NAV INDEX ======================= -->

# tia-cli 5-day free trial

The tia-cli v3 product line has a free, self-service **5-day trial** on one engineering machine.
The trial is designed to answer one practical question: can the CLI complete a small, concrete task
safely on a copy of your real project?

The public showcase is maintained under Codyte. The commercial package and licence identify
**START Automação Industrial Ltda.** as the supplier and licensor.

The evidenced v3 surface contains **346 unique command-line verbs** across **347 generated help
signatures** (`find` has two forms). The validation snapshot dated 2026-10-19 records 230 verbs with
successful live V21 evidence, 100 write verbs with V21 preview evidence, 43 with direct V19 evidence,
41 with direct V20 evidence and 16 with no recorded live evidence; version counts overlap.

[![START A 5-DAY FREE TRIAL](https://img.shields.io/badge/START_A_5--DAY_FREE_TRIAL-0A7F3F?style=for-the-badge)](https://startautomation.com.br/tia-portal-cli/)

The button opens the product page with the trial form. Nothing is read from your browser or machine
beyond what you type there.

## Before you start

You need:

- Windows x64 with a licensed TIA Portal V19, V20 or V21 installation;
- your Windows user in the local group **Siemens TIA Openness**;
- PowerShell 7 and Python 3, and a local AI agent that can run shell commands;
- a test project or a copy of a project, never the production copy;
- a corporate email address (free and disposable providers are refused);
- one small success criterion for yourself, written down before you start.

## Process

1. **Request.** On https://startautomation.com.br/tia-portal-cli/ enter your corporate email and the 6-digit code sent to it (valid for
   15 minutes). The page then shows your licence (`tia.lic`) and the zip download. One trial per
   email; if you lose the tab, the same email gets the same licence back.
2. **Install.** In PowerShell run `irm https://startautomation.com.br/tia-portal-cli/install.ps1 | iex`. It downloads the package, checks its
   SHA-256, installs to `~/.agents/skills/tia-portal-cli` and runs `init` with one UAC prompt.
   Alternatively extract the zip and double-click `install.cmd`. Both find a `tia.lic` saved in
   your Downloads folder.
3. **Run.** Work on the test-project copy. The 5 days count from first use, and the licence binds
   to that one machine at first use, so install on the machine you will evaluate on. Check your
   success criterion at the end.
4. **Day 5 and after.** The trial licence expires. Nothing is deleted from your project. To continue, buy the perpetual licence on the same page;
   online sales open soon, and until then [contato@startautomation.com.br](mailto:contato@startautomation.com.br)
   sells it.

## Safety and privacy

- Project changes are previews unless the command explicitly includes `--apply`.
- Physical online writes require both `--apply` and the additional `--allow-physical` opt-in.
- The CLI and project remain on the engineering machine; no project content or usage telemetry is
  sent to a hosted service.
- The trial licence is tied to the machine of first use and to the 5-day window.
- A successful trial does not replace engineering review, backups, compilation, simulation or
  commissioning procedures.

The licence terms delivered with the package govern the supplied build. For source access, a
commercial licence, integration work or a live demonstration, write to
[contato@startautomation.com.br](mailto:contato@startautomation.com.br).
