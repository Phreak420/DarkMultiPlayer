# DarkMultiPlayer: MMO Edition Agency Gameplay Test Checklist

Use the `experiment/server-agency-contracts` build for these tests.

These tests cover the optional MMO Edition agency progression experiment. Existing
`DarkMultiPlayer` install paths and DMP naming remain unchanged for compatibility.

## Test Setup

1. Install the client bundle into KSP:
   - `GameData/DarkMultiPlayer/Plugins/DarkMultiPlayer.dll`
2. Start the experimental server build.
3. Edit `Config/Settings.txt` and set:
   - `gameplayProfile = Agency`
   - `agencyProgressionEnabled = True`
   - `gameMode = CAREER` for reward testing
4. Restart the server after changing settings.
5. Confirm the server logs:
   - `Gameplay profile: Agency; agency progression enabled.`
   - `Loaded agency progression pack 'Server Agency' with ... objectives.`
6. Confirm `Config/AgencyProgression.json` exists after first enabled startup.

## Identity Visibility Smoke Test

1. Start KSP with the MMO Edition client installed.
2. Open DMP Options.
3. Open the `Player` tab.
4. Click `Copy ID`.
5. Click `Copy` beside the UUID row.
6. Click `Backup ID`.
7. Click `Copy Path`.
8. Restart KSP and reopen the `Player` tab.

Expected:

- The `Identity` row shows a short public-key fingerprint.
- The `UUID` row shows a stable compact UUID.
- The UUID copy button places the full UUID on the clipboard.
- The identity copy button places the displayed fingerprint on the clipboard.
- `Backup ID` refreshes the identity backup under `saves/DarkMultiPlayer`.
- `Copy Path` places the backup folder path on the clipboard.
- The UUID remains the same after restart.
- No Agency mode is required for this test.
- After connecting to a compatible server, `Universe/Players/Identities/<uuid>.txt` is written
  with the UUID, current display name, public-key fingerprint, first seen time, and last seen time.
- Existing name/public-key authentication remains authoritative.
- Run `/identity list` and confirm the connected player appears.
- Run `/identity show <uuid>` and confirm the full metadata is displayed.
- Run `/identity find <playerName>` and confirm the player can be found by display name.
- Run `/identity audit <uuid>` and confirm identity creation or metadata-change audit entries are
  displayed when present.
- For controlled recovery testing only, connect as a temporary online player and run
  `/identity attachkey <uuid> <temporaryPlayerName> confirm`; confirm
  `Universe/Players/<currentName>.txt` is updated and `/identity audit <uuid>` shows
  `key-attached`. Confirm a timestamped `Universe/Players/<currentName>.recovery-*.bak` file was
  written if the previous key file existed.
- For controlled rename testing only, run `/identity rename <uuid> <newPlayerName> confirm`;
  confirm `Universe/Players/<oldName>.txt` moves to `Universe/Players/<newPlayerName>.txt`,
  `/identity show <uuid>` lists the new current name and previous name, and `/identity audit <uuid>`
  shows `renamed`.
- For controlled revoke testing only, run `/identity revoke <uuid> <reason> confirm`; confirm the
  current key file moves to `Universe/Players/<currentName>.revoked-*.bak`, `/identity show <uuid>`
  lists the revoked timestamp and reason, and `/identity audit <uuid>` shows `revoked`.

## Suggested Test Campaign Config

Use this small config to exercise objective completion and rewards quickly:

```json
{
  "packName": "DMP Test Agency",
  "onboardingText": "Mission Control is coordinating shared progression for this server. Review objectives, contribute evidence, and collect server-approved rewards.",
  "objectives": [
    {
      "id": "orbit-kerbin",
      "title": "Reach Kerbin Orbit",
      "description": "Place a vessel into Kerbin orbit.",
      "status": "Available",
      "scope": "Personal",
      "contractType": "Milestone",
      "issuer": "Server Agency",
      "category": "Exploration",
      "evidenceType": "VESSEL_ORBITED",
      "evidenceId": "orbit-Kerbin",
      "rewardFunds": 5000,
      "rewardScience": 5,
      "rewardReputation": 2
    },
    {
      "id": "launchpad-science",
      "title": "Run Launchpad Science",
      "description": "Recover or transmit a crew report from the launchpad.",
      "status": "Available",
      "scope": "Personal",
      "contractType": "Science",
      "issuer": "Research Division",
      "category": "Science",
      "evidenceType": "SCIENCE_RECEIVED",
      "evidenceId": "crewReport@KerbinSrfLandedLaunchPad",
      "rewardFunds": 1000,
      "rewardScience": 2,
      "rewardReputation": 1
    },
    {
      "id": "dock-kerbin",
      "title": "Dock Around Kerbin",
      "description": "Dock two vessels around Kerbin.",
      "status": "Available",
      "scope": "Personal",
      "contractType": "Operations",
      "issuer": "Mission Control",
      "category": "Operations",
      "evidenceType": "VESSEL_DOCKED",
      "evidenceId": "docked-Kerbin",
      "rewardFunds": 2500,
      "rewardScience": 3,
      "rewardReputation": 1
    },
    {
      "id": "mun-landing-chain",
      "title": "Land on the Mun",
      "description": "Land on the Mun after proving Kerbin orbital capability.",
      "status": "Locked",
      "scope": "Server",
      "contractType": "Campaign",
      "issuer": "Server Agency",
      "category": "Exploration",
      "evidenceType": "VESSEL_LANDED",
      "evidenceId": "landed-Mun",
      "prerequisiteObjectiveIds": [
        "orbit-kerbin"
      ],
      "prerequisiteMode": "All",
      "rewardFunds": 12000,
      "rewardScience": 8,
      "rewardReputation": 3
    },
    {
      "id": "mun-encounter",
      "title": "Encounter the Mun",
      "description": "Enter the Mun's sphere of influence.",
      "status": "Available",
      "scope": "Personal",
      "contractType": "Milestone",
      "issuer": "Mission Control",
      "evidenceType": "VESSEL_ENCOUNTERED",
      "evidenceId": "encountered-Mun",
      "rewardFunds": 3000,
      "rewardScience": 2,
      "rewardReputation": 1
    },
    {
      "id": "kerbin-relay-network",
      "title": "Build Kerbin Relay Network",
      "description": "Have multiple players contribute Kerbin orbit evidence toward a shared relay objective.",
      "status": "Available",
      "scope": "Server",
      "contractType": "Community",
      "issuer": "Network Planning Office",
      "category": "Infrastructure",
      "evidenceType": "VESSEL_ORBITED",
      "evidenceId": "orbit-Kerbin",
      "progressTarget": 2,
      "progressPerEvidence": 1,
      "uniqueContributors": true,
      "rewardFunds": 15000,
      "rewardScience": 4,
      "rewardReputation": 2
    }
  ]
}
```

## Space Agency Window Smoke Test

1. Enable Agency systems on the server with either `gameplayProfile = Agency` or
   `agencyProgressionEnabled = True`.
2. Use an `AgencyProgression.json` file with `onboardingText` and a mix of categories, available,
   completed, locked, personal, and server-scoped objectives.
3. Connect with the client and open DMP Options.
4. Open the `Agency` tab and enable `Show Space Agency Window`.
5. Confirm the standalone `Server Space Agency` window appears.
6. Click `All`, `Open`, `Active`, `Done`, `Locked`, and `Shared`.

Expected:

- The Options tab only controls whether the standalone Space Agency window is visible.
- The standalone window shows the server pack name and onboarding text.
- `All` shows every objective sent by the server.
- `Open` shows objectives whose status is available.
- `Active` shows active or in-progress objectives when present.
- `Done` shows completed objectives.
- `Locked` shows locked or hidden objectives when present.
- `Shared` shows server, shared, or community-scoped objectives.
- Empty filters show `No objectives match this filter.` without throwing errors.
- Objective details show category, type, scope, issuer, description, rewards, and progress.
- Objectives with progress targets show `Progress: current / target`, including completed progress.
- Objective details show a short status guidance line explaining whether the mission is available,
  active, locked, or complete.
- Selecting an objective with journal records shows compact mission history in the details panel.
- If `CampaignState.json` exists, the window shows current campaign phase and read-only global
  metrics above the objective filters.
- Completing a rewarded objective posts a concise Agency completion notification; UI notification
  failures should be logged without crashing KSP.

## Objective Acceptance Lifecycle Smoke Test

Use an objective with `requiresAcceptance` enabled:

```json
{
  "id": "accepted-orbit",
  "title": "Accepted Orbit",
  "description": "Accept this mission before Kerbin orbit evidence can complete it.",
  "status": "Available",
  "scope": "Personal",
  "contractType": "Milestone",
  "issuer": "Server Agency",
  "category": "Exploration",
  "evidenceType": "VESSEL_ORBITED",
  "evidenceId": "orbit-Kerbin",
  "requiresAcceptance": true,
  "rewardFunds": 5000,
  "rewardScience": 5,
  "rewardReputation": 2
}
```

Steps:

1. Connect to the Agency-enabled server.
2. Open the standalone Space Agency window.
3. Select the objective and confirm it shows as `Available`.
4. Enter Kerbin orbit before accepting the objective.
5. Confirm the objective does not complete.
6. Click `Accept`.
7. Confirm the objective moves to the `Active` filter.
8. Click `Abandon`.
9. Confirm the objective returns to `Available`.
10. Run `/agency accepted <player>`.
11. Click `Accept` again.
12. Enter Kerbin orbit again or run `/agency record <player> VESSEL_ORBITED orbit-Kerbin`.
13. Run `/agency unaccept <player> accepted-orbit` after completion.
14. Run `/agency journal <player>`.
15. Run `/agency objective accepted-orbit`.

Expected:

- The objective shows an `Accept` button only while it requires acceptance and is available.
- Accepted personal objectives show an `Abandon` button while active.
- `Universe/AgencyProgression/Accepted.log` is written after acceptance.
- `/agency accepted <player>` shows the accepted objective.
- Evidence before acceptance is audited but does not complete the objective or grant rewards.
- Abandoning removes only accepted mission state; evidence history remains on disk.
- After abandon, matching evidence still does not complete the objective until it is accepted again.
- Evidence after acceptance completes the objective and grants configured rewards once.
- `/agency unaccept <player> accepted-orbit` fails after completion because completed objectives
  cannot be unaccepted.
- `Universe/AgencyProgression/Journal.log` records accepted, abandoned, completed, and
  reward-granted events.
- `/agency journal <player>` shows recent lifecycle and reward events.
- The Space Agency window shows compact recent activity after the server sends journal records.
- The selected mission detail panel shows recent history for `accepted-orbit`.
- `/agency objective accepted-orbit` shows status, evidence, reward, acceptance, and recent journal
  context for the mission.
- Objectives without `requiresAcceptance` keep the existing auto-completion behavior.

## Campaign State Smoke Test

1. Enable Agency systems on the server.
2. Confirm `Config/CampaignState.json` is created on startup.
3. Run `/campaign status`.
4. Run `/campaign set survey-progress 25`.
5. Run `/campaign advance mun-expansion`.
6. Reopen the Space Agency window.
7. Run `/campaign reset confirm` only after confirming you want to reset test campaign state.

Expected:

- `Universe/CampaignState/WorldState.txt` is created.
- `/campaign status` lists the current phase and configured metrics.
- `/campaign set survey-progress 25` updates the stored metric and may auto-advance to
  `mun-expansion` when the default sample phase automation is present.
- `/campaign advance mun-expansion` manually changes the current phase.
- `Universe/CampaignState/CampaignAudit.log` records metric and phase changes.
- The Space Agency window updates for connected clients after metric/phase changes.
- `/campaign reset confirm` writes a `WorldState.reset-*.bak` backup before reloading defaults.

## Campaign Event Smoke Test

Use campaign event fields such as:

```json
{
  "id": "relay-buildout",
  "title": "Kerbin Relay Buildout",
  "description": "Mission Control is prioritizing communications infrastructure around Kerbin.",
  "startsAtPhase": "kerbin-foundation",
  "requiredMetricId": "communications-strength",
  "requiredMetricMinimum": 10,
  "objectiveIds": ["reach-orbit"]
}
```

Optionally gate an objective with:

```json
{
  "id": "relay-followup",
  "title": "Relay Follow-up",
  "description": "Unlocked by the relay buildout event.",
  "status": "Locked",
  "scope": "Server",
  "evidenceType": "ADMIN_CONFIRMED",
  "evidenceId": "relay-followup",
  "requiredCampaignEventId": "relay-buildout"
}
```

Expected:

- `/campaign events` lists configured events and their status.
- When the phase, metric, and objective requirements are met, the event becomes `Available`.
- `/campaign activate relay-buildout` changes the event to `Active`.
- `/campaign complete relay-buildout` changes the event to `Complete`.
- Objectives with `requiredCampaignEventId` unlock when the event is `Active` or `Complete`.
- Event changes are written to `Universe/CampaignState/CampaignAudit.log`.

## Economy State Smoke Test

1. Enable Agency systems on the server.
2. Confirm `Config/EconomyState.json` is created on startup.
3. Run `/economy status`.
4. Run `/economy set fuel-reserve 10`.
5. Run `/economy adjust fuel-reserve 500`.
6. Reopen the Space Agency window.
7. Run `/economy reset confirm` only after confirming you want to reset test economy state.

Expected:

- `Universe/EconomyState/EconomyState.txt` is created.
- `/economy status` lists configured resources, state, and bounded modifier values.
- `/economy set fuel-reserve 10` marks the sample fuel reserve as scarce.
- `/economy adjust fuel-reserve 500` clamps to the configured maximum rather than exceeding it.
- `Universe/EconomyState/EconomyAudit.log` records set, adjust, and reset actions.
- The Space Agency window shows read-only economy resource summaries.
- `/economy reset confirm` writes an `EconomyState.reset-*.bak` backup before reloading defaults.

Optional objective economy fields:

```json
{
  "id": "fuel-delivery",
  "title": "Fuel Delivery",
  "description": "Deliver fuel to replenish agency reserves.",
  "status": "Available",
  "scope": "Server",
  "evidenceType": "ADMIN_CONFIRMED",
  "evidenceId": "fuel-delivery",
  "economyResourceId": "fuel-reserve",
  "economyResourceDelta": 15
}
```

Expected:

- Completing the objective adjusts `fuel-reserve` once.
- Resource values remain clamped by configured min/max bounds.
- The mission detail panel shows the objective's economy effect.

## Economy Reward Modifier Smoke Test

Use an Agency objective with explicit reward modifier fields:

```json
{
  "id": "fuel-recovery",
  "title": "Fuel Recovery",
  "description": "Recover fuel while reserves are scarce.",
  "status": "Available",
  "scope": "Server",
  "evidenceType": "ADMIN_CONFIRMED",
  "evidenceId": "fuel-recovery",
  "rewardFunds": 10000,
  "rewardScience": 5,
  "rewardReputation": 2,
  "rewardModifierResourceId": "fuel-reserve",
  "allowScarcityRewardBonus": true,
  "allowAbundanceRewardReduction": false
}
```

Steps:

1. Run `/economy set fuel-reserve 10`.
2. Reload Agency state or restart the server after adding the objective.
3. Reopen the Space Agency window and select the objective.
4. Complete it with `/agency record Alice ADMIN_CONFIRMED fuel-recovery`.
5. Run `/agency rewards Alice`.
6. Repeat with a separate test objective and `fuel-reserve` set above the abundance threshold.

Expected:

- The mission detail panel shows `Reward Mod` for `fuel-reserve`.
- Scarce resources increase the configured reward by the resource's bounded positive modifier.
- Abundant resources do not reduce rewards unless `allowAbundanceRewardReduction` is `true`.
- `Universe/AgencyRewards/Alice.log` records effective reward values plus modifier context when
  a modifier applies.
- `/agency rewards Alice` shows modifier resource/state and base reward values when a modifier
  applied.
- No passive economy decay or inactivity punishment is introduced by this feature.

## Campaign Unlock Condition Smoke Test

Use objective fields such as:

```json
{
  "id": "mun-expansion-objective",
  "title": "Begin Mun Expansion",
  "description": "Unlocks after the server reaches the Mun expansion phase and survey threshold.",
  "status": "Locked",
  "scope": "Server",
  "category": "Campaign",
  "evidenceType": "ADMIN_CONFIRMED",
  "evidenceId": "mun-expansion-objective",
  "requiredCampaignPhaseId": "mun-expansion",
  "requiredMetricId": "survey-progress",
  "requiredMetricMinimum": 25,
  "hiddenUntilAvailable": true
}
```

Expected:

- The objective remains locked while the campaign phase is not `mun-expansion`.
- The objective remains locked while `survey-progress` is below `25`.
- With `hiddenUntilAvailable = true`, the objective is hidden from players until both campaign
  conditions are met.
- `/campaign advance mun-expansion` and `/campaign set survey-progress 25` unlock the objective.

## Objective Metric Contribution Smoke Test

Use an agency objective with metric contribution fields:

```json
{
  "id": "relay-network",
  "title": "Build Kerbin Relay Network",
  "description": "Contribute to the server communication coverage metric.",
  "status": "Available",
  "scope": "Server",
  "category": "Infrastructure",
  "evidenceType": "VESSEL_ORBITED",
  "evidenceId": "orbit-Kerbin",
  "metricContributionId": "communications-strength",
  "metricContributionAmount": 10,
  "metricContributionMax": 100
}
```

Expected:

- Completing the objective adds `10` to `communications-strength`.
- `/campaign status` shows the updated metric value.
- `Universe/CampaignState/CampaignAudit.log` records the objective-driven metric change.
- Repeating the same evidence after the objective is complete does not keep increasing the metric.
- For progress objectives, the metric changes only when the objective reaches completion.

## Space Agency Mission Board Smoke Test

1. Connect to an Agency-enabled server with several objectives configured.
2. Open the Space Agency window from the DMP UI.
3. Switch between `All`, `Open`, `Active`, `Done`, `Locked`, and `Shared`.
4. Select an objective with `progressTarget` configured.
5. Select an objective with `metricContributionId` configured.

Expected:

- The window title remains `Server Space Agency`.
- The mission board summary shows open, active, shared, and completed mission counts.
- Mission list entries show compact progress such as `(1/3)` for progress objectives.
- The detail panel shows rewards when configured.
- The detail panel shows `World State` for objectives with metric contributions.
- Progress objectives show contributor counts and contribution rules when configured.
- Filtering does not select an objective outside the current filter.
- Existing `prerequisiteObjectiveIds` behavior still works and can be combined with campaign
  conditions.

## Solo-Friendly Community Objective Smoke Test

Use a shared progress objective that allows repeat contributions:

```json
{
  "id": "supply-runs",
  "title": "Supply Runs",
  "description": "Deliver supplies for the server agency.",
  "status": "Available",
  "scope": "Server",
  "category": "Logistics",
  "evidenceType": "ADMIN_CONFIRMED",
  "evidenceId": "supply-run",
  "progressTarget": 3,
  "progressPerEvidence": 1,
  "progressUnit": "deliveries",
  "contributionLabel": "Supply delivery",
  "uniqueContributors": false
}
```

Expected:

- One player can contribute multiple times and advance the objective.
- The contributor display counts that player once even if they contribute more than once.
- The Space Agency detail panel shows `repeat contributions allowed`.
- `/agency contributions supply-runs` shows the progress value, last contributor, and unique
  contributor list.
- Setting `uniqueContributors = true` changes the behavior so the same player only counts once.

## Test Cases

### 1. Disabled Server Compatibility

1. Set `gameplayProfile = Vanilla` and `agencyProgressionEnabled = False`.
2. Start the server and connect from KSP.
3. Open DMP Options.

Expected:

- No `Agency` tab appears.
- Normal DMP connection and gameplay still work.
- No `Universe/AgencyEvidence` or `Universe/AgencyRewards` folders are created by normal play.

### 2. Agency Tab Visibility

1. Set `gameplayProfile = Agency`.
2. Restart server.
3. Connect from KSP.
4. Open DMP Options.

Expected:

- `Agency` tab appears.
- `Show Agency Panel` toggle appears.
- Enabling the toggle shows a compact mission list with configured objective titles and statuses.
- Selecting a mission updates the mission detail area.
- Visible mission details show contract type, scope, issuer, progress, and reward summary when
  configured.

### 3. Objective Completion From Orbit Evidence

1. Use the suggested test config.
2. Launch a vessel and reach stable Kerbin orbit.
3. Open the `Agency` tab after orbit is reached.

Expected:

- `orbit-kerbin` changes to `Complete`.
- Server writes `Universe/AgencyEvidence/<player>.log`.
- Server writes `Universe/AgencyProgression/Objectives.log`.
- Server writes `Universe/AgencyRewards/<player>.log`.
- Player receives configured funds/science/reputation.

Server console checks:

- Run `/agency status`.
- Run `/agency objectives`.
- Run `/agency objective orbit-kerbin`.
- Run `/agency evidence <player>`.
- Run `/agency rewards <player>`.
- Run `/agency progress`.
- Run `/agency journal <player>`.
- Run `/agency replay <player> orbit-kerbin` only if you intentionally want to apply the reward
  again for recovery testing.
- Run `/agency revoke <player> orbit-kerbin` only if you intentionally want to apply a negative
  compensating reward.

### 4. Science Evidence

1. Use the suggested test config.
2. Start a career game through DMP.
3. Run a crew report on the launchpad and recover or transmit it.

Expected:

- `launchpad-science` completes.
- Evidence log includes `SCIENCE_RECEIVED`.
- Evidence id includes `crewReport@KerbinSrfLandedLaunchPad`.
- Reward is applied once.

### 5. Docking Evidence

1. Use the suggested test config.
2. Dock two vessels around Kerbin.

Expected:

- `dock-kerbin` completes.
- Evidence log includes `VESSEL_DOCKED`.
- Evidence id includes `docked-Kerbin`.
- Reward is applied once.

### 6. Duplicate Evidence Does Not Duplicate Rewards

1. Complete `orbit-kerbin`.
2. Stay in orbit or return to orbit again in the same session.
3. Reconnect and repeat if needed.

Expected:

- Objective remains `Complete`.
- Reward is not repeatedly granted for the already-complete objective.
- Evidence may have extra audit entries over time, but reward log should not duplicate the same
  completed objective unless future replay tooling explicitly does that.

### 7. Personal Objective Scope

1. Use the suggested test config with `scope` set to `Personal`.
2. Complete `orbit-kerbin` with player A.
3. Connect as player B.

Expected:

- Player A sees `orbit-kerbin` as `Complete`.
- Player B still sees `orbit-kerbin` as `Available`.
- Player B can complete the same personal objective independently.

### 8. Server Objective Scope

1. Change one objective to `"scope": "Server"`.
2. Complete that objective with player A.
3. Connect as player B.

Expected:

- Both players see the server-scoped objective as `Complete`.
- Reward is granted to the player whose evidence completed the objective.
- Completion persists in `Universe/AgencyProgression/Objectives.log`.

### 9. Admin Replay And Revoke

1. Complete a rewarded objective.
2. Run `/agency rewards <player>` and note the reward count.
3. Run `/agency replay <player> <objective>`.
4. Run `/agency rewards <player>` again.
5. Run `/agency revoke <player> <objective>`.

Expected:

- Replay writes an additional positive reward audit record.
- If the player is online, replay applies the reward again.
- Revoke writes an additional negative reward audit record.
- If the player is online, revoke applies the negative compensation.
- These commands do not delete evidence or objective completion history.
- `/agency journal <player>` records reward replay/revoke as reward-granted entries with positive
  or negative values in the details field.

### 10. Objective Prerequisites

1. Use the suggested test config.
2. Open the Agency panel before completing `orbit-kerbin`.
3. Confirm `mun-landing-chain` is locked.
4. Land on the Mun before completing `orbit-kerbin`, if you are testing with an existing save.
5. Complete `orbit-kerbin`.
6. Reopen the Agency panel.
7. Land on the Mun.

Expected:

- `mun-landing-chain` starts as `Locked`.
- Matching Mun landing evidence does not complete it while locked.
- Completing `orbit-kerbin` changes `mun-landing-chain` to `Available`.
- Mun landing evidence after unlock completes `mun-landing-chain`.
- Completion persists in `Universe/AgencyProgression/Objectives.log`.

Optional branching-chain check:

- Add two prerequisite objective IDs and set `prerequisiteMode` to `Any`.
- Complete either prerequisite.
- Confirm the dependent objective changes from `Locked` to `Available`.

Optional hidden-chain check:

- Set `hiddenUntilAvailable` to `true` on a locked dependent objective.
- Confirm the objective is not shown in the Agency panel before its prerequisites are complete.
- Complete the prerequisite.
- Confirm the objective appears as `Available`.
- Run `/agency objectives` and confirm admins can still see the hidden objective in the full
  server objective list.

### 11. Shared Objective Progress

1. Use the suggested test config.
2. Complete matching evidence for `kerbin-relay-network` with player A.
3. Reopen the Agency panel.
4. Run `/agency progress`.
5. Complete matching evidence for `kerbin-relay-network` with player B.
6. Reopen the Agency panel.

Expected:

- After player A contributes, `kerbin-relay-network` shows `In Progress 1/2`.
- The Agency panel shows a `Progress: 1 / 2` line for the partial objective.
- Server writes `Universe/AgencyProgression/Progress.log`.
- `/agency progress` lists the partial progress record.
- Repeating the same matching evidence with player A does not advance progress again when
  `uniqueContributors` is enabled.
- No reward is granted before the target is reached.
- After player B contributes, `kerbin-relay-network` changes to `Complete`.
- Reward is granted to the player whose contribution reaches the target.
- Completion persists in `Universe/AgencyProgression/Objectives.log`.

### 12. Expanded Vessel Evidence

Useful evidence IDs for server-authored objectives:

- Launch from Kerbin: `VESSEL_LAUNCHED` / `launched-Kerbin`
- Escape Kerbin's sphere of influence: `VESSEL_ESCAPED` / `escaped-Kerbin`
- Encounter the Mun: `VESSEL_ENCOUNTERED` / `encountered-Mun`
- Recover a vessel on Kerbin: `VESSEL_RECOVERED` / `recovered-Kerbin`

Expected:

- Each matching event writes an audit entry under `Universe/AgencyEvidence/<player>.log`.
- A configured objective using one of these evidence pairs completes when the evidence arrives.
- The evidence remains ignored when `agencyProgressionEnabled = false`.

### 13. Admin-Confirmed Evidence

Use `ADMIN_CONFIRMED` for server-reviewed milestones that do not have a safe automatic client
signal yet, such as infrastructure, colony, faction, or event progress.

Steps:

1. Add an objective with `evidenceType` set to `ADMIN_CONFIRMED`.
2. Set `evidenceId` to a safe milestone ID, such as `infrastructure-alpha`.
3. Start the server with agency progression enabled.
4. Run `/agency record server ADMIN_CONFIRMED infrastructure-alpha`.
5. Reopen the Agency panel or run `/agency objectives`.

Expected:

- Server writes `Universe/AgencyEvidence/server.log`.
- Matching objective changes to `Complete`.
- Server writes `Universe/AgencyProgression/Objectives.log`.
- Connected clients receive refreshed Agency objective state.

### 14. Contract Completion Evidence

Use `CONTRACT_COMPLETED` for stock contract completion milestones. The client reports a sanitized
evidence ID in the form `contract-<contractType>`.

Steps:

1. Add an objective with `evidenceType` set to `CONTRACT_COMPLETED`.
2. Set `evidenceId` to a known stock contract type, such as `contract-WorldFirstContract`.
3. Complete that contract in Career mode while connected to the server.
4. Reopen the Agency panel or run `/agency objectives`.

Expected:

- Server writes `Universe/AgencyEvidence/<player>.log`.
- Evidence log includes `CONTRACT_COMPLETED`.
- Matching objective changes to `Complete`.
- Rewards apply once when configured.

### 15. Admin Progress Reset

1. Use the suggested test config.
2. Complete matching evidence for `kerbin-relay-network` with player A only.
3. Run `/agency progress`.
4. Run `/agency resetprogress server kerbin-relay-network`.
5. Reopen the Agency panel.

Expected:

- Before reset, `kerbin-relay-network` shows `In Progress 1/2`.
- Reset removes the partial progress record.
- The objective returns to `Available`.
- No completion or reward is created by the reset.

### 16. Server Reload

1. Complete at least one objective.
2. Run `/agency reload`.
3. Reopen the Agency panel.

Expected:

- Completed objectives remain complete.
- Objective status is resent to clients.
- Server does not grant rewards again just because of reload.

### 17. Restart Persistence

1. Complete at least one objective.
2. Stop and restart the server.
3. Reconnect from KSP.

Expected:

- Completed objective status persists.
- Evidence and reward audit files remain on disk.
- Agency panel shows the persisted complete status.

### 18. Existing Server Safety

1. Use a normal DMP server config with `gameplayProfile = Vanilla` and
   `agencyProgressionEnabled = False`.
2. Connect and perform normal DMP activities: chat, launch, sync vessel, disconnect.

Expected:

- No agency UI.
- No agency reward/evidence messages.
- Existing DMP behavior remains unchanged.

## Known Limits

- Objective scope currently supports `Personal` and `Server`.
- Objective prerequisites currently use completed objective IDs; richer boolean conditions and
  objective chains are future work.
- Shared progress objectives currently use one configured evidence type/id and numeric progress
  target, with optional progress units, contribution labels, and unique-contributor rules.
- `uniqueContributors` can prevent one player from advancing the same progress objective more than
  once, but team/faction contribution rules are future work.
- Rewards are personal to the player whose evidence completed the objective unless an admin
  intentionally replays or revokes a reward.
- Generated stock KSP contracts are not implemented yet.
- Campaign phases, global metrics, optional phase auto-advance rules, and campaign events can be
  configured and changed by admins.
