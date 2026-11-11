<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill: https://github.com/Codyte/navindex) -->
<!--   L15     507B  tia-cli capability map -->
<!--   L25     729B  Operating contract -->
<!--   L37     565B  Project discovery and analysis -->
<!--   L49     565B  PLC software and data -->
<!--   L58     497B  Hardware, networks and drives -->
<!--   L67     570B  HMI and WinCC Unified -->
<!--   L76     642B  Safety, motion, libraries and collaboration -->
<!--   L86     488B  Online and simulation boundary -->
<!--   L96     658B  Validation snapshot -->
<!--   L110    698B  What is deliberately not included -->
<!-- ======================= END NAV INDEX ======================= -->

# tia-cli capability map

This document describes the public product surface of the tia-cli v3 product line. It is a
capability overview, not an installable command reference: the current source, generated verb
catalog and binaries are kept in the private product repository. The exact supplied build is
reported by `tia --version`.

The public showcase is maintained under Codyte. The commercial evaluation package and licence
identify **START Automação Industrial Ltda.** as the supplier and licensor.

## Operating contract

- 346 unique verbs across 347 generated help signatures (`find` has two forms), with JSON output
  and stable exit codes.
- Windows x64; TIA Portal V19, V20 and V21 workers built against the locally installed PublicAPI.
- Read and diagnostic commands do not mutate the open project.
- Project writes are previews unless the command explicitly includes `--apply`.
- Openness work is serialized because Portal exposes one engineering session for this use.
- Batch files support offline checking, fail-fast execution, bounded digests and transactional
  rehearsal when every included operation supports undo.
- Unsupported product options, Portal versions and API services return explicit capability errors.

## Project discovery and analysis

An agent can start without dumping the whole project into its context:

- `env` inventories running Portal processes, installed products/options and active attachments
  without consuming an Openness session.
- `tree` and `hmi-tree` create compact navigation maps for PLC and HMI objects.
- `find`, `xref`, `reachable`, `unused` and `trace` answer location, dependency, orphan and
  equipment-scope questions.
- `info`, `list-*`, `snapshot`, `doctor` and `audit` provide identity, inventory, preflight and
  acceptance evidence.

## PLC software and data

- Export/import blocks, sources, documentation, tags, tag tables and PLC data types.
- Create and organize folders, blocks, instance DBs, interfaces, DB members and calls.
- Read or write block/project documentation and compile with structured messages.
- Generate fault handling, alarms, instrumentation, PROFINET and repeatable equipment families.
- Compare blocks structurally or exercise compatible FBs against the same PLCSIM scenario.
- Build watch/force table definitions while leaving live force values to the engineer in Portal.

## Hardware, networks and drives

- Inventory stations, racks, modules, interfaces, I/O maps, attributes and network topology.
- Add/delete devices and modules, set addresses and connect supported subnets.
- Export/import hardware through CAx/AML and clone hardware between projects.
- Inspect and configure supported SINAMICS telegrams and drive parameters.
- Plan a new area or drive from declarative requirements, preview the generated files, then apply
  and compile the same reviewed batch.

## HMI and WinCC Unified

- WinCC Classic screens, tags, scripts, templates, pop-ups, connections, text lists and screen
  objects can be inventoried, exported/imported where Openness supports it, edited and audited.
- WinCC Unified has typed operations for screens, items, tags, connections, alarms, events,
  dynamizations, named objects, runtime settings and validation.
- Unified screen work uses the typed object API because Openness does not expose Unified screens as
  SimaticML. Unsupported operations remain explicit instead of falling back to GUI automation.

## Safety, motion, libraries and collaboration

- Safety capabilities cover F-program information, runtime groups, supported settings, signatures,
  printouts, F-BaseID and validation tests when the installed option and CPU expose them.
- Motion capabilities cover technology objects, cams, interpreter programs and mappings.
- Library workflows cover global libraries, master copies, types, package installation and the
  inverse PLC-to-library bake process.
- Multiuser workflows cover Project Server discovery and supported local-session lifecycle,
  marking, commit and check-in. Authentication is the Windows identity used by Openness.

## Online and simulation boundary

- Target discovery and state inspection are read-only.
- Online/offline, download and upload operations are explicit verbs; write operations require
  `--apply`.
- A physical interface additionally requires `--allow-physical`; there is no implicit path from a
  project-editing command to a plant CPU.
- `sim-run` is PLCSIM Advanced-only and refuses a real CPU. Simulation scenarios can write inputs,
  wait, read outputs and assert expected behavior.

## Validation snapshot

The generated coverage record dated 2026-10-19 reports:

- 230 verbs with successful live V21 evidence; write verbs in this status were exercised with
  `--apply`.
- 100 write verbs exercised on V21 in preview only, without `--apply`.
- 43 verbs with direct V19 evidence and 41 with direct V20 evidence; these overlap the V21 groups.
- 16 verbs with no recorded live evidence. They may have offline tests, but are not presented as
  proven in Portal until evidence is recorded.

The snapshot is evidence of tested operations, not a promise that every Portal version, installed
option, device or project exposes the same API surface.

## What is deliberately not included

- No Siemens DLL, Portal installer, licence key or customer project data is distributed.
- No cloud execution service receives project content.
- No UI scraping or protection bypass is used as a substitute for an unavailable Openness API.
- No claim is made that automation replaces engineering review, project backups, compile results,
  simulation or commissioning procedures.

For the free 5-day self-service trial see [TRIAL.md](TRIAL.md) and the
[product page](https://startautomation.com.br/tia-portal-cli/). For source access, a commercial licence or a demonstration,
[contact START Automação](https://startautomation.com.br/contato/?origem=tia-cli).
