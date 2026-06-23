# Agency Progression Config Authoring

`Config/AgencyProgression.json` is strict JSON. Do not add comments directly to the live config file.
Use this guide as the commented reference for common MMO Edition objective patterns.

Run `/agency validate` after editing the config. Validation warnings do not always stop the server,
but they identify objectives that may never complete or may behave differently than intended.

## Solo-Friendly Objective

```json
{
  "id": "orbit-kerbin",
  "title": "Reach Kerbin Orbit",
  "description": "Place any vessel into Kerbin orbit.",
  "status": "Available",
  "scope": "Personal",
  "category": "Exploration",
  "evidenceType": "VESSEL_ORBITED",
  "evidenceId": "orbit-Kerbin",
  "rewardFunds": 5000,
  "rewardScience": 5,
  "rewardReputation": 2
}
```

## Accepted Mission

```json
{
  "id": "accepted-orbit",
  "title": "Accepted Orbit",
  "description": "Accept the mission before orbit evidence can complete it.",
  "status": "Available",
  "scope": "Personal",
  "category": "Exploration",
  "requiresAcceptance": true,
  "evidenceType": "VESSEL_ORBITED",
  "evidenceId": "orbit-Kerbin",
  "rewardFunds": 7500
}
```

## Shared Progress Objective

By default, one player can contribute more than once. Use `uniqueContributors` only when a server
really wants distinct players to be required.

```json
{
  "id": "relay-network",
  "title": "Build Kerbin Relay Network",
  "description": "Contribute relay launches toward shared communications coverage.",
  "status": "Available",
  "scope": "Server",
  "category": "Infrastructure",
  "evidenceType": "VESSEL_ORBITED",
  "evidenceId": "orbit-Kerbin",
  "progressTarget": 3,
  "progressPerEvidence": 1,
  "progressUnit": "relays",
  "contributionLabel": "Relay launch",
  "rewardFunds": 10000
}
```

## Chained Objective

```json
{
  "id": "mun-landing",
  "title": "Land on the Mun",
  "description": "Unlocks after Kerbin orbit is complete.",
  "status": "Locked",
  "scope": "Personal",
  "category": "Exploration",
  "prerequisiteObjectiveIds": [ "orbit-kerbin" ],
  "evidenceType": "VESSEL_LANDED",
  "evidenceId": "landed-Mun",
  "rewardFunds": 15000,
  "rewardScience": 8,
  "rewardReputation": 3
}
```

## Campaign And Economy Effects

```json
{
  "id": "fuel-recovery",
  "title": "Recover Fuel Reserves",
  "description": "Complete a server-confirmed logistics run to stabilize fuel reserves.",
  "status": "Available",
  "scope": "Server",
  "category": "Logistics",
  "evidenceType": "ADMIN_CONFIRMED",
  "evidenceId": "fuel-recovery",
  "economyResourceId": "fuel-reserve",
  "economyResourceDelta": 10,
  "rewardModifierResourceId": "fuel-reserve",
  "allowScarcityRewardBonus": true,
  "rewardFunds": 10000,
  "rewardScience": 5,
  "rewardReputation": 2
}
```

## Validation Checklist

- Every objective ID should be unique and filesystem-safe.
- Every completable objective should have a valid `evidenceType` and safe `evidenceId`.
- Every prerequisite ID should reference another objective in the same file.
- `progressTarget` should be positive only when progress is intentional.
- `progressPerEvidence` should be positive when `progressTarget` is set.
- Economy deltas should include `economyResourceId`.
- Reward modifiers should include rewards and explicit scarcity or abundance flags.
