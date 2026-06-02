using System;
using System.Collections.Generic;
using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayer
{
    public class AgencyProgressionWorker
    {
        private const int MaxEvidenceIdLength = 128;
        private readonly List<AgencyObjectiveSummary> objectives = new List<AgencyObjectiveSummary>();
        private readonly HashSet<string> sentEvidenceIds = new HashSet<string>();
        private readonly Dictionary<Guid, string> vesselBodies = new Dictionary<Guid, string>();
        private readonly Queue<AgencyRewardSummary> pendingRewards = new Queue<AgencyRewardSummary>();
        private readonly DMPGame dmpGame;
        private readonly NetworkWorker networkWorker;
        private readonly NamedAction updateAction;

        public string PackName { get; private set; }

        public AgencyProgressionWorker(DMPGame dmpGame, NetworkWorker networkWorker)
        {
            this.dmpGame = dmpGame;
            this.networkWorker = networkWorker;
            updateAction = new NamedAction(Update);
            dmpGame.updateEvent.Add(updateAction);
            GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
            GameEvents.OnScienceRecieved.Add(OnScienceRecieved);
            GameEvents.onPartCouple.Add(OnVesselDocked);
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
        }

        public AgencyObjectiveSummary[] Objectives
        {
            get
            {
                lock (objectives)
                {
                    return objectives.ToArray();
                }
            }
        }

        public void HandleAgencyProgression(ByteArray messageData)
        {
            using (MessageReader mr = new MessageReader(messageData.data))
            {
                string packName = mr.Read<string>();
                string[] ids = mr.Read<string[]>();
                string[] titles = mr.Read<string[]>();
                string[] descriptions = mr.Read<string[]>();
                string[] statuses = mr.Read<string[]>();
                string[] scopes = mr.Read<string[]>();
                string[] contractTypes = mr.Read<string[]>();
                string[] issuers = mr.Read<string[]>();
                double[] progressValues = mr.Read<double[]>();
                double[] progressTargets = mr.Read<double[]>();
                double[] rewardFunds = mr.Read<double[]>();
                float[] rewardScience = mr.Read<float[]>();
                float[] rewardReputation = mr.Read<float[]>();

                int objectiveCount = ids.Length;
                if (titles.Length != objectiveCount || descriptions.Length != objectiveCount || statuses.Length != objectiveCount || scopes.Length != objectiveCount || contractTypes.Length != objectiveCount || issuers.Length != objectiveCount || progressValues.Length != objectiveCount || progressTargets.Length != objectiveCount || rewardFunds.Length != objectiveCount || rewardScience.Length != objectiveCount || rewardReputation.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency progression data from server.");
                    return;
                }

                lock (objectives)
                {
                    objectives.Clear();
                    PackName = packName;
                    for (int i = 0; i < objectiveCount; i++)
                    {
                        objectives.Add(new AgencyObjectiveSummary
                        {
                            id = ids[i],
                            title = titles[i],
                            description = descriptions[i],
                            status = statuses[i],
                            scope = scopes[i],
                            contractType = contractTypes[i],
                            issuer = issuers[i],
                            progressValue = progressValues[i],
                            progressTarget = progressTargets[i],
                            rewardFunds = rewardFunds[i],
                            rewardScience = rewardScience[i],
                            rewardReputation = rewardReputation[i]
                        });
                    }
                }
                DarkLog.Debug("Received " + objectiveCount + " agency progression objectives.");
            }
        }

        public void HandleAgencyReward(ByteArray messageData)
        {
            if (!dmpGame.serverAgencyProgressionEnabled)
            {
                return;
            }

            using (MessageReader mr = new MessageReader(messageData.data))
            {
                string objectiveId = mr.Read<string>();
                double funds = mr.Read<double>();
                float science = mr.Read<float>();
                float reputation = mr.Read<float>();

                lock (pendingRewards)
                {
                    pendingRewards.Enqueue(new AgencyRewardSummary
                    {
                        objectiveId = objectiveId,
                        funds = funds,
                        science = science,
                        reputation = reputation
                    });
                }
            }
        }

        public void Stop()
        {
            dmpGame.updateEvent.Remove(updateAction);
            GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
            GameEvents.OnScienceRecieved.Remove(OnScienceRecieved);
            GameEvents.onPartCouple.Remove(OnVesselDocked);
            GameEvents.onVesselRecovered.Remove(OnVesselRecovered);
            lock (objectives)
            {
                objectives.Clear();
                PackName = null;
            }
            sentEvidenceIds.Clear();
            vesselBodies.Clear();
            lock (pendingRewards)
            {
                pendingRewards.Clear();
            }
        }

        private void Update()
        {
            ApplyPendingRewards();

            if (!dmpGame.serverAgencyProgressionEnabled || !HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || FlightGlobals.fetch == null || FlightGlobals.fetch.activeVessel == null)
            {
                return;
            }

            Vessel activeVessel = FlightGlobals.fetch.activeVessel;
            if (activeVessel.mainBody == null)
            {
                return;
            }

            string bodyName = activeVessel.mainBody.bodyName;
            if (string.IsNullOrEmpty(bodyName))
            {
                return;
            }

            if (activeVessel.situation == Vessel.Situations.ORBITING)
            {
                SendEvidenceOnce(AgencyEvidenceType.VESSEL_ORBITED, BuildEvidenceId("orbit", bodyName));
            }
            if (activeVessel.situation == Vessel.Situations.FLYING || activeVessel.situation == Vessel.Situations.SUB_ORBITAL)
            {
                SendEvidenceOnce(AgencyEvidenceType.VESSEL_LAUNCHED, BuildEvidenceId("launched", bodyName));
            }
            if (activeVessel.situation == Vessel.Situations.ESCAPING)
            {
                SendEvidenceOnce(AgencyEvidenceType.VESSEL_ESCAPED, BuildEvidenceId("escaped", bodyName));
            }
            if (activeVessel.situation == Vessel.Situations.LANDED || activeVessel.situation == Vessel.Situations.SPLASHED)
            {
                SendEvidenceOnce(AgencyEvidenceType.VESSEL_LANDED, BuildEvidenceId("landed", bodyName));
            }

            TrackBodyEncounter(activeVessel, bodyName);
        }

        private void OnTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> data)
        {
            if (!dmpGame.serverAgencyProgressionEnabled || data.host == null || string.IsNullOrEmpty(data.host.techID))
            {
                return;
            }
            SendEvidenceOnce(AgencyEvidenceType.TECHNOLOGY_RESEARCHED, data.host.techID);
        }

        private void OnScienceRecieved(float science, ScienceSubject subject, ProtoVessel vessel, bool reverseEngineered)
        {
            if (!dmpGame.serverAgencyProgressionEnabled || subject == null || string.IsNullOrEmpty(subject.id))
            {
                return;
            }
            SendEvidenceOnce(AgencyEvidenceType.SCIENCE_RECEIVED, subject.id);
        }

        private void OnVesselDocked(GameEvents.FromToAction<Part, Part> partAction)
        {
            if (!dmpGame.serverAgencyProgressionEnabled || partAction.from == null || partAction.from.vessel == null || partAction.from.vessel.mainBody == null)
            {
                return;
            }
            string bodyName = partAction.from.vessel.mainBody.bodyName;
            if (string.IsNullOrEmpty(bodyName))
            {
                return;
            }
            SendEvidenceOnce(AgencyEvidenceType.VESSEL_DOCKED, BuildEvidenceId("docked", bodyName));
        }

        private void OnVesselRecovered(ProtoVessel recoveredVessel, bool recovered)
        {
            if (!dmpGame.serverAgencyProgressionEnabled || recoveredVessel == null || recoveredVessel.orbitSnapShot == null)
            {
                return;
            }
            string bodyName = GetBodyName(recoveredVessel.orbitSnapShot.ReferenceBodyIndex);
            if (!string.IsNullOrEmpty(bodyName))
            {
                SendEvidenceOnce(AgencyEvidenceType.VESSEL_RECOVERED, BuildEvidenceId("recovered", bodyName));
            }
            vesselBodies.Remove(recoveredVessel.vesselID);
        }

        private void TrackBodyEncounter(Vessel vessel, string bodyName)
        {
            string previousBodyName;
            if (!vesselBodies.TryGetValue(vessel.id, out previousBodyName))
            {
                vesselBodies[vessel.id] = bodyName;
                return;
            }
            if (previousBodyName == bodyName)
            {
                return;
            }
            vesselBodies[vessel.id] = bodyName;
            SendEvidenceOnce(AgencyEvidenceType.VESSEL_ENCOUNTERED, BuildEvidenceId("encountered", bodyName));
        }

        private string GetBodyName(int bodyIndex)
        {
            if (FlightGlobals.Bodies == null || bodyIndex < 0 || bodyIndex >= FlightGlobals.Bodies.Count || FlightGlobals.Bodies[bodyIndex] == null)
            {
                return string.Empty;
            }
            return FlightGlobals.Bodies[bodyIndex].bodyName;
        }

        private void SendEvidenceOnce(AgencyEvidenceType evidenceType, string evidenceId)
        {
            if (string.IsNullOrEmpty(evidenceId))
            {
                return;
            }
            string evidenceKey = evidenceType.ToString() + ":" + evidenceId;
            if (sentEvidenceIds.Contains(evidenceKey))
            {
                return;
            }
            sentEvidenceIds.Add(evidenceKey);
            networkWorker.SendAgencyEvidence(evidenceType, evidenceId);
        }

        private string BuildEvidenceId(string prefix, string value)
        {
            string evidenceId = prefix + "-" + value;
            char[] chars = evidenceId.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c < 32 || c == '<' || c == '>' || c == ':' || c == '"' || c == '/' || c == '\\' || c == '|' || c == '?' || c == '*' || c == '$')
                {
                    chars[i] = '_';
                }
            }
            evidenceId = new string(chars).Trim();
            if (evidenceId.Length > MaxEvidenceIdLength)
            {
                evidenceId = evidenceId.Substring(0, MaxEvidenceIdLength);
            }
            return evidenceId;
        }

        private void ApplyPendingRewards()
        {
            while (true)
            {
                AgencyRewardSummary reward;
                lock (pendingRewards)
                {
                    if (pendingRewards.Count == 0)
                    {
                        return;
                    }
                    reward = pendingRewards.Dequeue();
                }
                try
                {
                    ApplyAgencyReward(reward.objectiveId, reward.funds, reward.science, reward.reputation);
                }
                catch (Exception e)
                {
                    DarkLog.Debug("Failed to apply agency reward for " + reward.objectiveId + ", exception: " + e);
                }
            }
        }

        private void ApplyAgencyReward(string objectiveId, double funds, float science, float reputation)
        {
            if (funds != 0 && Funding.Instance != null)
            {
                Funding.Instance.AddFunds(funds, TransactionReasons.ContractReward);
            }
            if (science != 0 && ResearchAndDevelopment.Instance != null)
            {
                ResearchAndDevelopment.Instance.AddScience(science, TransactionReasons.ContractReward);
            }
            if (reputation != 0 && Reputation.Instance != null)
            {
                Reputation.Instance.AddReputation(reputation, TransactionReasons.ContractReward);
            }
            ScreenMessages.PostScreenMessage("Agency objective complete: " + objectiveId, 5f, ScreenMessageStyle.UPPER_CENTER);
            DarkLog.Debug("Applied agency reward for " + objectiveId + ": funds=" + funds + ", science=" + science + ", reputation=" + reputation);
        }
    }

    public class AgencyObjectiveSummary
    {
        public string id;
        public string title;
        public string description;
        public string status;
        public string scope;
        public string contractType;
        public string issuer;
        public double progressValue;
        public double progressTarget;
        public double rewardFunds;
        public float rewardScience;
        public float rewardReputation;
    }

    public class AgencyRewardSummary
    {
        public string objectiveId;
        public double funds;
        public float science;
        public float reputation;
    }
}
