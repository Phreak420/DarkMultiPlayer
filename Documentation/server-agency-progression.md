# Server Agency Progression Experiment

Branch: `experiment/server-agency-contracts`

This branch explores an optional server-controlled progression layer where a DarkMultiPlayer
server can act as the space agency: offering goals, awarding science/funds/reputation, and
unlocking later stages of a server-authored storyline.

The default behavior must stay unchanged. Experimental controls should be hidden unless the
server advertises this mode, and server owners should be able to disable the feature completely.

## Design Signals

Community feedback points to a few consistent problems in stock career and multiplayer career:

- Stock contract progression can feel random, especially when exploration offers jump ahead of
  a player's expected Mun/Minmus/body progression.
  Source: https://forum.kerbalspaceprogram.com/topic/135422-early-career-mode-missions/
- Players like career as a guide, but dislike repetitive grind for science and funds.
  Source: https://www.reddit.com/r/KerbalSpaceProgram/comments/1sff17u/career_mod_recommendations/
- Mainline milestone contracts work best when they form a logical path and can be combined with
  optional side contracts for money.
  Source: https://www.reddit.com/r/KerbalAcademy/comments/1sqkt6y/tips_for_career_mode/
- Contract decline penalties make more sense when the server creates urgency or budget pressure,
  not when players are simply waiting for the random generator to behave.
  Source: https://forum.kerbalspaceprogram.com/topic/198756-question-about-career-mode-contracts-milestones/
- Multiplayer players may want opt-in shared collaboration instead of syncing every personal
  career decision globally.
  Source: https://www.reddit.com/r/KerbalSpaceProgram/comments/1sx2rye/partial_multiplayer/
- Contract Configurator and contract packs show that KSP players already expect authored
  progression packs, contract groups, explicit requirements, and custom reward tuning.
  Sources:
  - https://forum.kerbalspaceprogram.com/topic/91625-1101-contract-configurator-v1305-2020-10-05/
  - https://spacedock.info/mod/17/SETI-Contracts
  - https://github.com/KSP-RO/RP-1

## Existing DMP Hooks

Useful existing systems:

- `Server/Settings.cs` already controls `gameMode`, `gameDifficulty`, and server-level toggles.
- `Server/GameplaySettings.cs` already exposes career reward multipliers and starting resources.
- `Server/Messages/ServerSettings.cs` already sends server-selected career/science settings to
  clients during connection setup.
- `Server/Messages/ScenarioData.cs` persists per-player scenario modules under
  `Universe/Scenarios/<player>/`.
- `Client/ScenarioWorker.cs` already syncs scenario modules and listens for contract, science,
  funds, reputation, and technology events.
- `Client/ScenarioWorker.cs` already includes special fixups for rescue and tourism contracts,
  which means contract-specific multiplayer handling is not new to the codebase.

## Proposed Shape

The experiment should be feature-gated in two layers:

- Server gate: a setting such as `agencyProgressionEnabled = false`.
- Client gate: a hidden UI section that appears only when the server advertises agency
  progression support.

Server control should be authoritative for:

- Whether agency progression is enabled.
- Which storyline/quest pack is active.
- Which objectives are available, active, complete, or locked.
- Which resource rewards are granted.
- Whether objectives are personal, group-wide, or opt-in shared.

Client control should be limited to:

- Viewing available agency objectives.
- Accepting optional objectives when the server allows it.
- Reporting objective evidence based on KSP events and scenario state.

The server should validate reported completion instead of trusting client-side reward state.
Early phases can start with conservative evidence checks and avoid direct resource mutation until
the protocol is stable.

## Phased Checklist

### Phase 1: Discovery and Gates

- [x] Add an experimental server setting, default `false`.
- [x] Add protocol fields for "server supports agency progression" without changing behavior.
- [x] Add a hidden client UI toggle/section that appears only when the server setting is enabled.
- [x] Log clear server/client startup messages when the experimental mode is enabled.
- [x] No resource rewards, no new contracts, no scenario mutation beyond settings sync.

Implemented setting: `agencyProgressionEnabled`.

### Phase 2: Read-Only Agency State

- [x] Define an agency progression data model on the server.
- [x] Load a simple server-authored quest file from config.
- [x] Send read-only objective summaries to clients.
- [x] Show objective status in the hidden client UI.
- [x] Keep all objectives informational; no acceptance or completion effects.

Implemented config file: `Config/AgencyProgression.json`.

### Phase 3: Objective Evidence

- [x] Add client reports for bounded evidence types, starting with:
  - technology node researched
  - science subject received
- [x] Rate-limit and size-limit evidence messages.
- [x] Validate evidence type and evidence IDs server-side.
- [x] Store evidence separately from rewards so mistakes are easy to inspect and roll back.
- [ ] Add additional evidence types:
  - vessel reached orbit around body
  - vessel landed on body
  - vessel docked with another vessel

Implemented evidence log: `Universe/AgencyEvidence/<player>.log`.

### Phase 4: Server-Awarded Rewards

- Grant server-approved science/funds/reputation rewards.
- Start with personal rewards only.
- Add audit logs for every reward event.
- Add admin commands to list, replay, or revoke agency reward events.

### Phase 5: Shared Storyline Progression

- Add group/server-wide milestones that unlock later objectives.
- Support opt-in shared projects such as stations, relays, and bases.
- Let server owners choose whether completion is first-to-complete, all-players, team-based, or
  server-wide.

### Phase 6: Contract Integration

- Decide whether to generate real KSP contracts, mirror KSP contracts in a DMP UI, or integrate
  with Contract Configurator when installed.
- Avoid hard dependency on Contract Configurator in the core mod.
- Treat stock KSP contract state as client UI/experience, not as authoritative server truth.

## Open Questions

- Should agency progression require Career mode, or also support Science/Sandbox servers with
  DMP-owned rewards?
- Should rewards modify KSP scenario modules directly, or should DMP own a server ledger and apply
  deltas at sync points?
- Should storyline progress be per-player, per-group, global, or configurable per objective?
- What is the smallest evidence model that feels useful without trusting clients too much?
- How should server owners write quests: text config, JSON, YAML-like config, or KSP `ConfigNode`?

## Initial Bias

Start with a server ledger and read-only UI. Do not generate stock contracts first. The current
scenario sync path is useful but broad, and KSP contracts are single-player state with many
side-effects. A DMP-owned quest ledger gives us a safer rollback path and lets the server act as
the authority before we integrate deeper with KSP's contract UI.

## Future Engagement and Campaign Director Roadmap

This section describes long-term extension points that can build on the existing agency
progression model. It does not replace the current implementation roadmap. The immediate priority
remains evidence collection, objective completion, rewards, and administration. Future systems
must stay optional, backwards-compatible, and gated behind `agencyProgressionEnabled` or a more
specific child setting.

### Campaign / Story Arc Framework

Agency progression should eventually support server-authored campaigns that give a multiplayer
server a persistent direction without hardcoding specific stories into DMP. Examples of campaign
themes server owners may define include:

- Kerbin resource crisis.
- Colony survival initiative.
- Duna colonization effort.
- Asteroid defense program.
- Deep space exploration initiative.

The framework should treat those as data, not code. A campaign definition should describe its
name, premise, phases, objectives, unlock rules, event hooks, rewards, and safety limits through
configuration files. DMP should provide the generic campaign engine, validation, sync, audit, and
admin tooling. Server owners should provide the story content.

### Global Community Objectives

The objective system should support community-wide progress in addition to personal objectives.
Future objective scopes may include personal, team, agency, server-wide, and opt-in shared
objectives. Multiple players should be able to contribute evidence and progress toward one shared
objective without overwriting each other's work.

Examples of community objectives:

- Build relay networks.
- Deliver resources to stations, depots, or colonies.
- Establish colonies or surface bases.
- Discover anomalies.
- Reach specific celestial bodies.
- Construct infrastructure such as stations, refueling depots, launch sites, or deep-space relays.

The server should own contribution accounting. Clients may report evidence, but the server should
decide whether that evidence contributes to an objective, how much progress it grants, and whether
the contribution is personal, team-wide, or global.

### Campaign Phases

Campaigns should support phased progression. A phase is a group of objectives with shared unlock
conditions, narrative context, and optional reward rules. Completing one phase can unlock later
phases while preserving the evidence and audit history that led to the unlock.

Example phase chain:

- Phase 1: Survey Duna.
- Phase 2: Land crew.
- Phase 3: Establish colony.
- Phase 4: Create self-sustaining infrastructure.

Phases should be data-driven. A campaign file should be able to express phase ordering,
prerequisites, optional side objectives, hidden future objectives, and server-wide unlocks without
requiring a new DMP build.

### Event System

A future campaign director may support temporary server events. Events should be optional,
configurable, auditable, and bounded. They should add urgency or variety without destabilizing a
server's long-term progression.

Examples:

- Asteroid threats.
- Solar storms.
- Communication outages.
- Resource shortages.
- Emergency evacuation contracts.

Events should have explicit start conditions, duration, effects, objective hooks, and recovery
paths. They should be able to trigger new objectives or modify existing objective rewards, but
they should not bypass the same validation and audit rules used by normal agency progression.

### Persistent Server Progression

The architecture should leave room for persistent server-level progression that is separate from
any single player's save state. Future concepts may include:

- Global science pools.
- Global reputation.
- Agency milestones.
- Historical achievements.
- Hall of Fame statistics.

These systems should be implemented as server-owned ledgers with clear sync points to clients.
They should not require rewriting current evidence or objective records. Evidence should remain
the raw audit trail, objective state should remain derived server state, and rewards/progression
should remain separately auditable.

### Agency / Team / Corporation Support

Future campaigns may support player organizations. The initial architecture should avoid assuming
that every objective is either personal or global.

Possible future features:

- Player agencies, teams, or corporations.
- Team objectives.
- Agency rankings.
- Cooperative objectives.
- Competitive objectives.

No implementation is required now. The main design consideration is to keep objective ownership,
contribution records, reward recipients, and visibility rules explicit. A team objective should
not be modeled as a personal objective with special cases scattered through the code.

### Dynamic Economy Safety Principles

Any future economy system must keep the server playable for returning players. Economic pressure
can create interesting choices, but it should never punish players so aggressively that coming
back to the server feels hopeless.

Design principles:

- Bound all economy modifiers.
- Never allow irreversible economic collapse.
- Avoid inactivity death spirals.
- Pause, dampen, or stabilize economy systems during low activity periods.
- Prefer recovery opportunities over punishment.
- Generate attractive contracts when resources become scarce.
- Provide admin override, reset, and repair tools.
- Keep economy state observable and auditable.
- Keep all economy systems optional and disabled by default until mature.

Bad outcome:

- Fuel prices increased 800% because nobody played for two weeks.

Better outcome:

- Fuel prices increased 15%, and the Agency is offering premium logistics contracts to replenish
  reserves.

Scarcity should usually create opportunities. If fuel, funds, reputation, or resources become
strained, the campaign director should offer useful work that helps players recover rather than
locking them out of meaningful play.

### Seasonal Campaign Support

Servers may eventually want campaigns that run for a fixed season and then archive their results.
Seasonal support should preserve history wherever possible instead of simply deleting the past.

Future seasonal features may include:

- Seasons.
- Campaign archives.
- Historical statistics.
- Hall of Fame records.
- Optional campaign resets.

Campaign resets should preserve historical achievements, audit records, player contribution
totals, and notable milestones whenever possible. A reset should start a new campaign state, not
erase the server's story.

### Configuration-First Philosophy

Campaign systems should be driven by configuration rather than code. Server owners should be able
to design campaigns, objectives, events, rewards, and safety limits without recompiling DMP.

Future configuration files may include:

- Campaign definitions.
- Objective chains.
- Event definitions.
- Reward tables.
- Economy modifiers.
- Safety limits.

Configuration should be validated on server startup, and invalid campaign content should fail
closed: log clear errors, skip unsafe entries, and keep the server playable. Future admin commands
can reload campaign configuration, but reloads should use the same validation path as startup.

### Implementation Guidance

Recommended staged approach:

- Current phase: evidence collection and objective completion.
- Next phase: rewards and administration.
- Later phase: campaigns and story arcs.
- Later phase: events and global progression.
- Later phase: economy and seasonal systems.

The core extension points to preserve are:

- Evidence remains raw, append-only, and auditable.
- Objective state remains server-owned and derived from evidence plus campaign configuration.
- Rewards remain separate from evidence and objective matching.
- Campaigns remain optional and configuration-driven.
- Dynamic economy systems remain bounded, observable, and recoverable.
- Existing DMP sandbox, science, and career servers continue to work with
  `agencyProgressionEnabled = false`.
