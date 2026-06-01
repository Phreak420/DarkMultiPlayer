# DarkMultiPlayer: MMO Edition Roadmap

DarkMultiPlayer: MMO Edition is a maintained fork of godarklight's DarkMultiPlayer project. The
fork's public purpose is to preserve the original multiplayer sandbox while adding optional,
server-controlled systems for long-running multiplayer space programs.

This roadmap does not replace the existing agency progression plan. It summarizes the broader MMO
Edition direction and points back to the detailed implementation notes.

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
- [ ] Expand objective evidence to cover more progression milestones.
- [ ] Add objective prerequisites, chains, and unlock rules.
- [ ] Add server-driven agency contracts or contract-like client experiences.
- [ ] Add global community objectives that multiple players can contribute toward.
- [ ] Add shared economy and resource pressure with strong safety limits.
- [ ] Add story/event-driven campaigns with configurable phases.
- [ ] Add seasonal campaign archives, historical statistics, and Hall of Fame records.
- [ ] Improve identity visibility, migration, and recovery while preserving existing auth.

## Design Rules

- Keep MMO systems optional and disabled by default until mature.
- Keep server authority over objectives, rewards, campaign state, and economy state.
- Prefer auditable ledgers over hidden mutation.
- Preserve compatibility-sensitive names in code, saves, protocol messages, and packaging.
- Do not touch CKAN packaging until there is a specific release plan.
- Avoid irreversible economy failure states; pressure should create recovery opportunities.
- Keep campaign content configuration-driven rather than hardcoded.

## Detailed Plans

- Agency progression implementation:
  [server-agency-progression.md](server-agency-progression.md)
- Gameplay test checklist:
  [server-agency-gameplay-tests.md](server-agency-gameplay-tests.md)
