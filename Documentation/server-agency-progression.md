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
- [x] Rate-limit and size-limit evidence messages.
- [x] Validate evidence type and evidence IDs server-side.
- [x] Store evidence separately from rewards so mistakes are easy to inspect and roll back.
- [ ] Add additional evidence types:
  - vessel reached orbit around body
  - vessel landed on body
  - science subject recovered/transmitted
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
