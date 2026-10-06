# Security policy

## Scope

This public repository contains product documentation and demonstration media only. Security reports
about the current tia-cli product are still welcome here.

The relevant trust boundaries are:

- a project-changing verb acting without its documented explicit `--apply`;
- physical online access that does not require the additional `--allow-physical` opt-in;
- `sim-run` reaching hardware that is not an S7-PLCSIM Advanced instance;
- project content, equipment names, addresses or credentials leaving the operator's machine without
  an explicit destination and action;
- command injection through CLI arguments or PowerShell workflow scripts;
- executable-whitelist or scheduled-task behavior that lets an unprivileged process replace or run
  arbitrary code as the trusted worker;
- secrets, Siemens-licensed binaries or customer artifacts included in a distribution.

Project mutation after a deliberate `--apply` is not by itself a vulnerability. The flag authorizes
the documented engineering change; users must keep project backups and validate compile/simulation
results before commissioning.

## Reporting

Please use a [private GitHub security advisory](https://github.com/Codyte/Tia-Portal-CLI/security/advisories/new)
or email [contato@codyte.com](mailto:contato@codyte.com). Do not open a public issue for a path that
could expose project data, alter a project without intent or reach a physical controller without the
documented guards.

Include the product version, TIA Portal major, affected verb and arguments, observed result and a
minimal reproduction with customer identifiers removed. This is a single-maintainer project; the
initial response target is two weeks.

## Supported versions

Security fixes target the current managed release. Historical public releases through v2.0.0 remain
under their original MIT terms but do not receive backports.
