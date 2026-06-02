# DarkMultiPlayer: MMO Edition

DarkMultiPlayer: MMO Edition is a maintained fork of godarklight's DarkMultiPlayer project.
This fork preserves the original multiplayer sandbox while expanding it with server-driven
progression, agency contracts, shared objectives, economy systems, and MMO-style long-term
engagement.

This is not presented as the official continuation of DarkMultiPlayer. It exists because
godarklight and the original contributors built a remarkable multiplayer foundation for Kerbal
Space Program, and this fork is intended to build carefully on that work while keeping existing
servers, saves, clients, and tooling in mind.

## Attribution

DarkMultiPlayer was originally created and maintained by godarklight with contributions from the
DarkMultiPlayer community. The original project made multiplayer KSP practical through subspace
sync, vessel sharing, server ownership tools, mod control, career/science support, and years of
compatibility work.

All credit for the original architecture, protocol, gameplay model, and community history belongs
to godarklight and the original DarkMultiPlayer contributors. DarkMultiPlayer: MMO Edition should
be understood as a fork that depends on and honors that foundation.

The original license and copyright notices are preserved in [LICENCE.txt](LICENCE.txt).

## Project Goals

The near-term goal is to keep DarkMultiPlayer's sandbox multiplayer experience intact while adding
optional MMO-focused server systems. These systems should be disabled by default, controlled by
server owners, and implemented without disruptive package, namespace, save, protocol, or CKAN
renaming unless a future compatibility plan explicitly calls for it.

Current and planned MMO Edition work:

- [x] ~~Preserve the original DMP multiplayer sandbox as the default experience.~~
- [x] ~~Add an experimental server flag for agency progression.~~
- [x] ~~Expose a hidden client Agency UI only when the server enables it.~~
- [x] ~~Load server-authored agency objectives from configuration.~~
- [x] ~~Collect bounded objective evidence from clients and audit it server-side.~~
- [x] ~~Complete matching personal and server-scoped objectives.~~
- [x] ~~Award server-approved science, funds, and reputation rewards.~~
- [x] ~~Add admin inspection, reward replay, and reward revoke commands.~~
- [x] ~~Add prerequisite-based objective unlocks.~~
- [x] ~~Add basic progress targets for shared community objectives.~~
- [x] ~~Add admin progress inspection/reset and clearer progress display.~~
- [x] ~~Add optional unique-contributor tracking for shared objectives.~~
- [x] ~~Start DMP-owned contract-like objective presentation.~~
- [ ] Expand evidence types for more mission and infrastructure milestones.
- [ ] Add richer objective chains and unlock rules beyond completed-objective prerequisites.
- [ ] Add server-driven agency contracts or contract-like player experiences.
- [ ] Support richer global community objectives that many players can contribute toward.
- [ ] Add shared economy/resource pressure with bounded, recoverable safety rules.
- [ ] Add story and event-driven campaigns controlled by server configuration.
- [ ] Add seasonal campaign archives, historical statistics, and Hall of Fame records.
- [ ] Improve player identity visibility and migration without breaking existing auth.
- [ ] Add gameplay profiles for Vanilla Mode, Agency Mode, and optional MMO Campaign Mode.
- [ ] Add optional compatibility hooks for colony, mapping, construction, tourism,
  infrastructure, and hard-mode survival mods.

More detail is tracked in
[Documentation/server-agency-progression.md](Documentation/server-agency-progression.md).

## Gameplay Profiles

DarkMultiPlayer: MMO Edition should be a platform, not a forced modpack. The long-term design is
to let server owners choose how much progression they want:

- Vanilla Mode: mostly original DMP-style multiplayer.
- Agency Mode: stock-friendly server-controlled science, funds, contracts, and progression.
- MMO Campaign Mode: optional deeper integration with supported mods.

Future optional compatibility targets include MKS/OKS, SCANsat, Extraplanetary Launchpads,
Tourism Overhaul, Kerbal Konstructs, and Kerbalism. These are roadmap targets, not current
required dependencies. Kerbalism-style hard-mode mechanics should never be mandatory in the
default MMO profile.

The broader platform roadmap is tracked in
[Documentation/mmo-edition-roadmap.md](Documentation/mmo-edition-roadmap.md).

## Compatibility Policy

Compatibility is a core goal of this fork.

- Runtime namespaces, protocol identifiers, save paths, and server file names still use
  `DarkMultiPlayer`/`DMP` where changing them could break existing installs.
- CKAN packaging is intentionally untouched for now.
- Existing servers should continue to behave like normal DarkMultiPlayer servers when
  `agencyProgressionEnabled = False`.
- Experimental MMO systems should stay optional and auditable.

## Install

### Client

- Download or build the client bundle and extract `GameData/DarkMultiPlayer` into your KSP
  install's `GameData` folder.
- Existing DarkMultiPlayer install paths are intentionally preserved for compatibility.

### Server

The DarkMultiPlayer server is cross platform. Use the server build that matches your host platform,
then configure it by editing `Config/Settings.txt`.

If your server's game difficulty is set to `CUSTOM`, gameplay settings can be changed in
`Config/GameplaySettings.txt`.

For the MMO Edition agency experiment, set:

```txt
agencyProgressionEnabled = True
```

The feature remains disabled by default.

## Compiling

- Copy the KSP managed assemblies from `[KSP root folder]/KSP_Data/Managed` to
  `External/KSPManaged`.
- Build the solution in Release mode with your preferred .NET/MSBuild tooling.
- The client targets KSP's .NET Framework runtime; the server can also be published as a modern
  .NET self-contained executable for a specific OS.

## Mod Control

Read `DMPModControl.txt`; it is commented. The file can be copied from a development KMPServer,
as the file format is the same.

If you are running a private server, it is usually safe enough to add missing parts as needed.

The DMP client can generate a `DMPModControl.txt` file for your `GameData` directory from
`Options -> Advanced -> Mod Control -> Generate`. Whitelist mode only allows clients with the
listed mods. Blacklist mode allows clients with any mods except the blocked entries.

## Documentation

- [Agency progression roadmap](Documentation/server-agency-progression.md)
- [Agency gameplay test checklist](Documentation/server-agency-gameplay-tests.md)
- [MMO Edition roadmap](Documentation/mmo-edition-roadmap.md)
- [Subspace locking notes](Documentation/subspace-locking.txt)
- [Network message format notes](Documentation/network-message-format.txt)
