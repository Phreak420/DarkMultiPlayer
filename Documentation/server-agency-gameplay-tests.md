# Server Agency Gameplay Test Checklist

Use the `experiment/server-agency-contracts` build for these tests.

## Test Setup

1. Install the client bundle into KSP:
   - `GameData/DarkMultiPlayer/Plugins/DarkMultiPlayer.dll`
2. Start the experimental server build.
3. Edit `Config/Settings.txt` and set:
   - `agencyProgressionEnabled = True`
   - `gameMode = CAREER` for reward testing
4. Restart the server after changing settings.
5. Confirm the server logs:
   - `Experimental server agency progression is enabled.`
   - `Loaded agency progression pack 'Server Agency' with ... objectives.`
6. Confirm `Config/AgencyProgression.json` exists after first enabled startup.

## Suggested Test Campaign Config

Use this small config to exercise objective completion and rewards quickly:

```json
{
  "packName": "DMP Test Agency",
  "objectives": [
    {
      "id": "orbit-kerbin",
      "title": "Reach Kerbin Orbit",
      "description": "Place a vessel into Kerbin orbit.",
      "status": "Available",
      "scope": "Personal",
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
      "evidenceType": "VESSEL_DOCKED",
      "evidenceId": "docked-Kerbin",
      "rewardFunds": 2500,
      "rewardScience": 3,
      "rewardReputation": 1
    }
  ]
}
```

## Test Cases

### 1. Disabled Server Compatibility

1. Set `agencyProgressionEnabled = False`.
2. Start the server and connect from KSP.
3. Open DMP Options.

Expected:

- No `Agency` tab appears.
- Normal DMP connection and gameplay still work.
- No `Universe/AgencyEvidence` or `Universe/AgencyRewards` folders are created by normal play.

### 2. Agency Tab Visibility

1. Set `agencyProgressionEnabled = True`.
2. Restart server.
3. Connect from KSP.
4. Open DMP Options.

Expected:

- `Agency` tab appears.
- `Show Agency Panel` toggle appears.
- Enabling the toggle shows the configured objective titles and statuses.

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
- Run `/agency evidence <player>`.
- Run `/agency rewards <player>`.
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

### 10. Server Reload

1. Complete at least one objective.
2. Run `/agency reload`.
3. Reopen the Agency panel.

Expected:

- Completed objectives remain complete.
- Objective status is resent to clients.
- Server does not grant rewards again just because of reload.

### 11. Restart Persistence

1. Complete at least one objective.
2. Stop and restart the server.
3. Reconnect from KSP.

Expected:

- Completed objective status persists.
- Evidence and reward audit files remain on disk.
- Agency panel shows the persisted complete status.

### 12. Existing Server Safety

1. Use a normal DMP server config with `agencyProgressionEnabled = False`.
2. Connect and perform normal DMP activities: chat, launch, sync vessel, disconnect.

Expected:

- No agency UI.
- No agency reward/evidence messages.
- Existing DMP behavior remains unchanged.

## Known Limits

- Objective scope currently supports `Personal` and `Server`.
- Rewards are personal to the player whose evidence completed the objective unless an admin
  intentionally replays or revokes a reward.
- Generated stock KSP contracts are not implemented yet.
- Campaign phases and events are documented future work.
