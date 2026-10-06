<!-- ====================== BEGIN NAV INDEX ====================== -->
<!-- NAV INDEX — auto-generated symbol map (refresh via the navindex skill) -->
<!--   L14     264B  tia-cli capability map -->
<!--   L20     659B  Operating contract -->
<!--   L31     565B  Project discovery and analysis -->
<!--   L43     565B  PLC software and data -->
<!--   L52     497B  Hardware, networks and drives -->
<!--   L61     570B  HMI and WinCC Unified -->
<!--   L70     642B  Safety, motion, libraries and collaboration -->
<!--   L80     488B  Online and simulation boundary -->
<!--   L90     556B  What is deliberately not included -->
<!-- ======================= END NAV INDEX ======================= -->

# tia-cli capability map

This document describes the public product surface of tia-cli v3.0.0. It is a capability overview,
not an installable command reference: the current source, generated verb catalog and binaries are
kept in the private product repository.

## Operating contract

- 347 verbs with JSON output and stable exit codes.
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

## What is deliberately not included

- No Siemens DLL, Portal installer, licence key or customer project data is distributed.
- No cloud execution service receives project content.
- No UI scraping or protection bypass is used as a substitute for an unavailable Openness API.
- No claim is made that automation replaces engineering review, project backups, compile results,
  simulation or commissioning procedures.

For an evaluation build, source access, a commercial licence or a demonstration, contact
[contato@codyte.com](mailto:contato@codyte.com).
