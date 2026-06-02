# DarkMultiPlayer: MMO Edition Roadmap

DarkMultiPlayer: MMO Edition is a maintained fork of godarklight's DarkMultiPlayer project. The
fork's public purpose is to preserve the original multiplayer sandbox while adding optional,
server-controlled systems for long-running multiplayer space programs.

This roadmap does not replace the existing agency progression plan. It summarizes the broader MMO
Edition direction and points back to the detailed implementation notes.

DarkMultiPlayer: MMO Edition should be a platform, not a forced modpack. The long-term direction
is to support multiple server-selected gameplay profiles while keeping vanilla multiplayer viable.
Optional mod compatibility should add depth for servers that want it, without becoming required
for every player or every server.

## Attribution And Positioning

DarkMultiPlayer: MMO Edition exists because godarklight and the original DarkMultiPlayer
contributors built the foundation first. The original project solved the hardest multiplayer
problems: subspace sync, vessel sharing, server state, mod control, career/science support, and
practical server administration.

This fork should always be described as a fork, not the official continuation, unless the project
status changes explicitly. Public text should credit godarklight and original contributors
prominently and preserve original license and copyright notices.

## Roadmap

- [x] ~~Preserve the original DMP multiplayer sandbox by default.~~
- [x] ~~Gate agency progression behind `agencyProgressionEnabled`.~~
- [x] ~~Advertise agency capability from server to client.~~
- [x] ~~Show agency UI only when the server enables it.~~
- [x] ~~Load agency objectives from server configuration.~~
- [x] ~~Record objective evidence with server-side validation and audit logs.~~
- [x] ~~Complete objectives from matching evidence.~~
- [x] ~~Support personal and server-scoped objective completion.~~
- [x] ~~Grant server-approved science, funds, and reputation rewards.~~
- [x] ~~Add admin inspection, reward replay, and reward revoke commands.~~
- [x] ~~Add prerequisite-based objective unlocks.~~
- [x] ~~Add basic progress targets for shared community objectives.~~
- [x] ~~Add admin progress inspection/reset and clearer progress display.~~
- [x] ~~Add optional unique-contributor tracking for shared objectives.~~
- [x] ~~Start DMP-owned contract-like objective presentation.~~
- [x] ~~Expand vessel objective evidence to cover more progression milestones.~~
- [ ] Expand non-vessel objective evidence to cover infrastructure and campaign milestones.
- [ ] Add richer objective chains and unlock rules beyond completed-objective prerequisites.
- [ ] Add server-driven agency contracts or contract-like client experiences.
- [ ] Add richer global community objectives that multiple players can contribute toward.
- [ ] Add shared economy and resource pressure with strong safety limits.
- [ ] Add story/event-driven campaigns with configurable phases.
- [ ] Add seasonal campaign archives, historical statistics, and Hall of Fame records.
- [ ] Improve identity visibility, migration, and recovery while preserving existing auth.
- [ ] Add gameplay profiles that let servers choose between vanilla multiplayer, stock-friendly
  agency progression, and deeper optional MMO campaign integrations.
- [ ] Add compatibility hooks for supported colony, exploration, construction, tourism,
  infrastructure, and hard-mode survival mods without making them required dependencies.

## Gameplay Profiles

MMO Edition should support distinct server profiles instead of assuming every server wants the
same amount of progression or mod integration.

### Vanilla Mode

Vanilla Mode should remain closest to original DarkMultiPlayer behavior:

- Original DMP-style multiplayer sandbox.
- Existing server, save, protocol, and install naming preserved.
- No required campaign layer.
- No required economy simulation.
- No required third-party gameplay mods.
- Agency progression disabled unless the server owner explicitly enables it.

This mode exists to honor the original DarkMultiPlayer experience and to keep the fork usable for
players and server owners who only want reliable multiplayer KSP.

### Agency Mode

Agency Mode should be stock-friendly and server-controlled:

- Server-authored objectives.
- Server-controlled science, funds, reputation, contracts, and progression.
- Personal and server-scoped objectives.
- Optional global community goals.
- Audit logs for evidence, rewards, and progression.
- No required third-party gameplay mods.

This is the current experimental track. It should stay compatible with mostly stock installs and
should remain the preferred path for broad multiplayer accessibility.

### MMO Campaign Mode

MMO Campaign Mode is a future profile for servers that intentionally choose deeper progression
and supported mod integration:

- Campaign phases, server events, and long-term world state.
- Colony, logistics, survey, manufacturing, tourism, infrastructure, and survival hooks.
- Optional mod-aware objective evidence and reward logic.
- Server-configured safety rules for inactivity, economy pressure, and background simulation.

This mode should be opt-in. Supported mods should be compatibility targets and recommended
integrations, not hard dependencies of MMO Edition itself.

## Optional Mod Compatibility Roadmap

These integrations are aspirational compatibility targets. They are not implemented yet and
should not become required dependencies until a specific profile, release plan, and compatibility
strategy exist.

### 1. MKS/OKS

MKS/OKS is the highest-priority optional integration target because it naturally supports
long-term multiplayer investment:

- Colonies.
- Logistics.
- Resource chains.
- Off-world infrastructure.
- Long-term player investment.

Potential MMO Edition use cases:

- Server objectives for establishing self-sufficient colonies.
- Logistics network milestones.
- Resource-chain health as campaign state.
- Colony growth contributing to global progression.
- Rewards for sustaining off-world infrastructure instead of only launching from Kerbin.

### 2. SCANsat

SCANsat is a natural fit for shared exploration and campaign-driven discovery:

- Shared exploration data.
- Server-wide planetary mapping.
- Survey objectives.
- Exploration-based unlocks.

Potential MMO Edition use cases:

- Server-owned map completion metrics.
- Survey contracts that unlock new phases, bodies, or resource targets.
- Exploration objectives for anomalies, biomes, and resource-rich areas.
- Shared mapping progress that many players can contribute toward.

### 3. Extraplanetary Launchpads

Extraplanetary Launchpads can make mature colonies matter by reducing long-term dependence on
Kerbin:

- Off-world construction.
- Colony-based shipbuilding.
- Resource-driven manufacturing.
- Reduced Kerbin dependency over time.

Potential MMO Edition use cases:

- Construction milestones tied to colony development.
- Resource delivery and manufacturing chains.
- Campaign phases that unlock off-world shipbuilding.
- Strategic objectives for sustaining local production.

### 4. Tourism Overhaul

Tourism systems can give player-built infrastructure an economic purpose:

- Player-built stations and destinations gaining economic purpose.
- Tourism routes.
- Passenger transport contracts.
- Orbital hotels and colony visitation.

Potential MMO Edition use cases:

- Server-generated passenger routes to stations, bases, and colonies.
- Reputation and funds rewards for safe transport networks.
- Economic incentives for maintaining destinations.
- Community objectives around tourism capacity and route reliability.

### 5. Kerbal Konstructs

Kerbal Konstructs can support persistent surface infrastructure and server worldbuilding:

- Persistent surface infrastructure.
- Player/server-owned bases.
- New launch sites.
- Worldbuilding and faction territory.

Potential MMO Edition use cases:

- Campaign-owned launch sites.
- Faction, agency, or team territory.
- Infrastructure objectives for bases, depots, and surface facilities.
- Server-authored worldbuilding locations tied to events and campaigns.

### 6. Kerbalism

Kerbalism should be treated as optional hard-mode support:

- Life support, radiation, failures, and long-term mission risk.
- High-value realism for servers that explicitly want it.
- Significant risk of punishing casual or returning players if configured too harshly.

Kerbalism-style mechanics should never be mandatory in default MMO mode. If supported, the server
should allow soft persistence options such as:

- Paused life support decay during low-activity periods.
- Vacation protection for absent players.
- Grace periods after reconnecting or after server downtime.
- Admin-configurable background simulation.
- Bounded failure rates.
- Recovery contracts when supplies or infrastructure become strained.
- Clear observability so players and admins can see why a failure or shortage occurred.

The goal is to let hard-mode servers exist without making default MMO Edition hostile to players
who cannot log in every day.

## Server-Controlled World State

Long-term MMO gameplay should be driven by server-owned world state. The server can track global
campaign metrics such as:

- Kerbin resource depletion.
- Planetary survey completion.
- Colony population.
- Food, fuel, and material supply.
- Infrastructure coverage.
- Communications network strength.
- Agency reputation.
- Economic stability.
- Environmental crisis level.
- Evacuation or colonization progress.

These metrics should influence:

- Contract generation.
- Science, funds, and reputation rewards.
- Unlocks.
- Server events.
- Global objectives.
- Campaign progression.
- Faction or agency competition.

World state should be auditable, configurable, and bounded. It should extend the current
evidence/objective/reward architecture rather than replacing it. Server owners should be able to
inspect current values, understand why they changed, override them when needed, and disable the
systems entirely for vanilla or stock-friendly servers.

## Design Rules

- Keep MMO systems optional and disabled by default until mature.
- Treat MMO Edition as a compatibility platform, not a forced modpack.
- Support Vanilla Mode, Agency Mode, and optional MMO Campaign Mode as separate server choices.
- Keep server authority over objectives, rewards, campaign state, and economy state.
- Prefer auditable ledgers over hidden mutation.
- Preserve compatibility-sensitive names in code, saves, protocol messages, and packaging.
- Do not touch CKAN packaging until there is a specific release plan.
- Avoid irreversible economy failure states; pressure should create recovery opportunities.
- Keep campaign content configuration-driven rather than hardcoded.
- Keep third-party mod support optional and profile-driven.

## Detailed Plans

- Agency progression implementation:
  [server-agency-progression.md](server-agency-progression.md)
- Gameplay test checklist:
  [server-agency-gameplay-tests.md](server-agency-gameplay-tests.md)
