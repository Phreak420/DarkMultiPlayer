using System;
using System.Collections.Generic;
using Contracts;
using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayer
{
    public class AgencyProgressionWorker
    {
        private const int MaxEvidenceIdLength = 128;
        private readonly List<AgencyObjectiveSummary> objectives = new List<AgencyObjectiveSummary>();
        private readonly List<CampaignMetricSummary> campaignMetrics = new List<CampaignMetricSummary>();
        private readonly List<CampaignEventSummary> campaignEvents = new List<CampaignEventSummary>();
        private readonly List<EconomyResourceSummary> economyResources = new List<EconomyResourceSummary>();
        private readonly HashSet<string> sentEvidenceIds = new HashSet<string>();
        private readonly Dictionary<Guid, string> vesselBodies = new Dictionary<Guid, string>();
        private readonly Queue<AgencyRewardSummary> pendingRewards = new Queue<AgencyRewardSummary>();
        private readonly DMPGame dmpGame;
        private readonly NetworkWorker networkWorker;
        private readonly NamedAction updateAction;

        public string PackName { get; private set; }
        public string OnboardingText { get; private set; }
        public string CampaignName { get; private set; }
        public string CampaignPhaseId { get; private set; }
        public string CampaignPhaseTitle { get; private set; }
        public string CampaignPhaseDescription { get; private set; }
        public string EconomyName { get; private set; }

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
            GameEvents.Contract.onCompleted.Add(OnContractCompleted);
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

        public CampaignMetricSummary[] CampaignMetrics
        {
            get
            {
                lock (campaignMetrics)
                {
                    return campaignMetrics.ToArray();
                }
            }
        }

        public CampaignEventSummary[] CampaignEvents
        {
            get
            {
                lock (campaignEvents)
                {
                    return campaignEvents.ToArray();
                }
            }
        }

        public EconomyResourceSummary[] EconomyResources
        {
            get
            {
                lock (economyResources)
                {
                    return economyResources.ToArray();
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
                string[] categories = new string[objectiveCount];
                OnboardingText = string.Empty;
                string campaignName = string.Empty;
                string campaignPhaseId = string.Empty;
                string campaignPhaseTitle = string.Empty;
                string campaignPhaseDescription = string.Empty;
                string[] metricIds = new string[0];
                string[] metricTitles = new string[0];
                string[] metricCategories = new string[0];
                string[] metricUnits = new string[0];
                double[] metricValues = new double[0];
                double[] metricTargets = new double[0];
                string[] metricContributionIds = new string[objectiveCount];
                double[] metricContributionAmounts = new double[objectiveCount];
                double[] metricContributionMaxes = new double[objectiveCount];
                string[] objectiveEconomyResourceIds = new string[objectiveCount];
                double[] objectiveEconomyResourceDeltas = new double[objectiveCount];
                string[] objectiveRewardModifierResourceIds = new string[objectiveCount];
                bool[] objectiveAllowScarcityRewardBonuses = new bool[objectiveCount];
                bool[] objectiveAllowAbundanceRewardReductions = new bool[objectiveCount];
                double[] objectiveMaxRewardModifierOverrides = new double[objectiveCount];
                bool[] objectiveRequiresAcceptances = new bool[objectiveCount];
                string[] objectiveAcceptedBy = new string[objectiveCount];
                string[] objectiveAcceptedAtUtc = new string[objectiveCount];
                string[] progressUnits = new string[objectiveCount];
                string[] contributionLabels = new string[objectiveCount];
                int[] contributorCounts = new int[objectiveCount];
                string[] contributors = new string[objectiveCount];
                double[] progressPerEvidence = new double[objectiveCount];
                bool[] uniqueContributors = new bool[objectiveCount];
                string[] eventIds = new string[0];
                string[] eventTitles = new string[0];
                string[] eventDescriptions = new string[0];
                string[] eventStatuses = new string[0];
                string economyName = string.Empty;
                string[] economyResourceIds = new string[0];
                string[] economyResourceTitles = new string[0];
                string[] economyResourceCategories = new string[0];
                string[] economyResourceUnits = new string[0];
                string[] economyResourceStates = new string[0];
                double[] economyResourceValues = new double[0];
                double[] economyResourceMaxValues = new double[0];
                double[] economyResourceModifiers = new double[0];
                try
                {
                    categories = mr.Read<string[]>();
                    OnboardingText = mr.Read<string>();
                    campaignName = mr.Read<string>();
                    campaignPhaseId = mr.Read<string>();
                    campaignPhaseTitle = mr.Read<string>();
                    campaignPhaseDescription = mr.Read<string>();
                    metricIds = mr.Read<string[]>();
                    metricTitles = mr.Read<string[]>();
                    metricCategories = mr.Read<string[]>();
                    metricUnits = mr.Read<string[]>();
                    metricValues = mr.Read<double[]>();
                    metricTargets = mr.Read<double[]>();
                    try
                    {
                        metricContributionIds = mr.Read<string[]>();
                        metricContributionAmounts = mr.Read<double[]>();
                        metricContributionMaxes = mr.Read<double[]>();
                        try
                        {
                            progressUnits = mr.Read<string[]>();
                            contributionLabels = mr.Read<string[]>();
                            contributorCounts = mr.Read<int[]>();
                            contributors = mr.Read<string[]>();
                            progressPerEvidence = mr.Read<double[]>();
                            uniqueContributors = mr.Read<bool[]>();
                            try
                            {
                                eventIds = mr.Read<string[]>();
                                eventTitles = mr.Read<string[]>();
                                eventDescriptions = mr.Read<string[]>();
                                eventStatuses = mr.Read<string[]>();
                                try
                                {
                                    objectiveEconomyResourceIds = mr.Read<string[]>();
                                    objectiveEconomyResourceDeltas = mr.Read<double[]>();
                                    economyName = mr.Read<string>();
                                    economyResourceIds = mr.Read<string[]>();
                                    economyResourceTitles = mr.Read<string[]>();
                                    economyResourceCategories = mr.Read<string[]>();
                                    economyResourceUnits = mr.Read<string[]>();
                                    economyResourceStates = mr.Read<string[]>();
                                    economyResourceValues = mr.Read<double[]>();
                                    economyResourceMaxValues = mr.Read<double[]>();
                                    economyResourceModifiers = mr.Read<double[]>();
                                    try
                                    {
                                        objectiveRewardModifierResourceIds = mr.Read<string[]>();
                                        objectiveAllowScarcityRewardBonuses = mr.Read<bool[]>();
                                        objectiveAllowAbundanceRewardReductions = mr.Read<bool[]>();
                                        objectiveMaxRewardModifierOverrides = mr.Read<double[]>();
                                        try
                                        {
                                            objectiveRequiresAcceptances = mr.Read<bool[]>();
                                            objectiveAcceptedBy = mr.Read<string[]>();
                                            objectiveAcceptedAtUtc = mr.Read<string[]>();
                                        }
                                        catch (Exception)
                                        {
                                            objectiveRequiresAcceptances = new bool[objectiveCount];
                                            objectiveAcceptedBy = new string[objectiveCount];
                                            objectiveAcceptedAtUtc = new string[objectiveCount];
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        objectiveRewardModifierResourceIds = new string[objectiveCount];
                                        objectiveAllowScarcityRewardBonuses = new bool[objectiveCount];
                                        objectiveAllowAbundanceRewardReductions = new bool[objectiveCount];
                                        objectiveMaxRewardModifierOverrides = new double[objectiveCount];
                                        objectiveRequiresAcceptances = new bool[objectiveCount];
                                        objectiveAcceptedBy = new string[objectiveCount];
                                        objectiveAcceptedAtUtc = new string[objectiveCount];
                                    }
                                }
                                catch (Exception)
                                {
                                    economyName = string.Empty;
                                    economyResourceIds = new string[0];
                                    economyResourceTitles = new string[0];
                                    economyResourceCategories = new string[0];
                                    economyResourceUnits = new string[0];
                                    economyResourceStates = new string[0];
                                    economyResourceValues = new double[0];
                                    economyResourceMaxValues = new double[0];
                                    economyResourceModifiers = new double[0];
                                    objectiveRewardModifierResourceIds = new string[objectiveCount];
                                    objectiveAllowScarcityRewardBonuses = new bool[objectiveCount];
                                    objectiveAllowAbundanceRewardReductions = new bool[objectiveCount];
                                    objectiveMaxRewardModifierOverrides = new double[objectiveCount];
                                    objectiveRequiresAcceptances = new bool[objectiveCount];
                                    objectiveAcceptedBy = new string[objectiveCount];
                                    objectiveAcceptedAtUtc = new string[objectiveCount];
                                }
                            }
                            catch (Exception)
                            {
                                eventIds = new string[0];
                                eventTitles = new string[0];
                                eventDescriptions = new string[0];
                                eventStatuses = new string[0];
                            }
                        }
                        catch (Exception)
                        {
                            objectiveEconomyResourceIds = new string[objectiveCount];
                            objectiveEconomyResourceDeltas = new double[objectiveCount];
                            objectiveRewardModifierResourceIds = new string[objectiveCount];
                            objectiveAllowScarcityRewardBonuses = new bool[objectiveCount];
                            objectiveAllowAbundanceRewardReductions = new bool[objectiveCount];
                            objectiveMaxRewardModifierOverrides = new double[objectiveCount];
                            objectiveRequiresAcceptances = new bool[objectiveCount];
                            objectiveAcceptedBy = new string[objectiveCount];
                            objectiveAcceptedAtUtc = new string[objectiveCount];
                            progressUnits = new string[objectiveCount];
                            contributionLabels = new string[objectiveCount];
                            contributorCounts = new int[objectiveCount];
                            contributors = new string[objectiveCount];
                            progressPerEvidence = new double[objectiveCount];
                            uniqueContributors = new bool[objectiveCount];
                        }
                    }
                    catch (Exception)
                    {
                        metricContributionIds = new string[objectiveCount];
                        metricContributionAmounts = new double[objectiveCount];
                        metricContributionMaxes = new double[objectiveCount];
                        objectiveEconomyResourceIds = new string[objectiveCount];
                        objectiveEconomyResourceDeltas = new double[objectiveCount];
                        objectiveRewardModifierResourceIds = new string[objectiveCount];
                        objectiveAllowScarcityRewardBonuses = new bool[objectiveCount];
                        objectiveAllowAbundanceRewardReductions = new bool[objectiveCount];
                        objectiveMaxRewardModifierOverrides = new double[objectiveCount];
                        objectiveRequiresAcceptances = new bool[objectiveCount];
                        objectiveAcceptedBy = new string[objectiveCount];
                        objectiveAcceptedAtUtc = new string[objectiveCount];
                        progressUnits = new string[objectiveCount];
                        contributionLabels = new string[objectiveCount];
                        contributorCounts = new int[objectiveCount];
                        contributors = new string[objectiveCount];
                        progressPerEvidence = new double[objectiveCount];
                        uniqueContributors = new bool[objectiveCount];
                    }
                }
                catch (Exception)
                {
                    categories = new string[objectiveCount];
                    OnboardingText = string.Empty;
                }

                if (titles.Length != objectiveCount || descriptions.Length != objectiveCount || statuses.Length != objectiveCount || scopes.Length != objectiveCount || contractTypes.Length != objectiveCount || issuers.Length != objectiveCount || progressValues.Length != objectiveCount || progressTargets.Length != objectiveCount || rewardFunds.Length != objectiveCount || rewardScience.Length != objectiveCount || rewardReputation.Length != objectiveCount || categories.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency progression data from server.");
                    return;
                }
                if (metricTitles.Length != metricIds.Length || metricCategories.Length != metricIds.Length || metricUnits.Length != metricIds.Length || metricValues.Length != metricIds.Length || metricTargets.Length != metricIds.Length)
                {
                    DarkLog.Debug("Received invalid campaign metric data from server.");
                    metricIds = new string[0];
                    metricTitles = new string[0];
                    metricCategories = new string[0];
                    metricUnits = new string[0];
                    metricValues = new double[0];
                    metricTargets = new double[0];
                }
                if (metricContributionIds.Length != objectiveCount || metricContributionAmounts.Length != objectiveCount || metricContributionMaxes.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency metric contribution data from server.");
                    metricContributionIds = new string[objectiveCount];
                    metricContributionAmounts = new double[objectiveCount];
                    metricContributionMaxes = new double[objectiveCount];
                }
                if (objectiveEconomyResourceIds.Length != objectiveCount || objectiveEconomyResourceDeltas.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency economy contribution data from server.");
                    objectiveEconomyResourceIds = new string[objectiveCount];
                    objectiveEconomyResourceDeltas = new double[objectiveCount];
                }
                if (objectiveRewardModifierResourceIds.Length != objectiveCount || objectiveAllowScarcityRewardBonuses.Length != objectiveCount || objectiveAllowAbundanceRewardReductions.Length != objectiveCount || objectiveMaxRewardModifierOverrides.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency reward modifier data from server.");
                    objectiveRewardModifierResourceIds = new string[objectiveCount];
                    objectiveAllowScarcityRewardBonuses = new bool[objectiveCount];
                    objectiveAllowAbundanceRewardReductions = new bool[objectiveCount];
                    objectiveMaxRewardModifierOverrides = new double[objectiveCount];
                }
                if (objectiveRequiresAcceptances.Length != objectiveCount || objectiveAcceptedBy.Length != objectiveCount || objectiveAcceptedAtUtc.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency objective lifecycle data from server.");
                    objectiveRequiresAcceptances = new bool[objectiveCount];
                    objectiveAcceptedBy = new string[objectiveCount];
                    objectiveAcceptedAtUtc = new string[objectiveCount];
                }
                if (progressUnits.Length != objectiveCount || contributionLabels.Length != objectiveCount || contributorCounts.Length != objectiveCount || contributors.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency contribution summary data from server.");
                    progressUnits = new string[objectiveCount];
                    contributionLabels = new string[objectiveCount];
                    contributorCounts = new int[objectiveCount];
                    contributors = new string[objectiveCount];
                }
                if (progressPerEvidence.Length != objectiveCount || uniqueContributors.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency contribution rule data from server.");
                    progressPerEvidence = new double[objectiveCount];
                    uniqueContributors = new bool[objectiveCount];
                }
                if (eventTitles.Length != eventIds.Length || eventDescriptions.Length != eventIds.Length || eventStatuses.Length != eventIds.Length)
                {
                    DarkLog.Debug("Received invalid campaign event data from server.");
                    eventIds = new string[0];
                    eventTitles = new string[0];
                    eventDescriptions = new string[0];
                    eventStatuses = new string[0];
                }
                if (economyResourceTitles.Length != economyResourceIds.Length || economyResourceCategories.Length != economyResourceIds.Length || economyResourceUnits.Length != economyResourceIds.Length || economyResourceStates.Length != economyResourceIds.Length || economyResourceValues.Length != economyResourceIds.Length || economyResourceMaxValues.Length != economyResourceIds.Length || economyResourceModifiers.Length != economyResourceIds.Length)
                {
                    DarkLog.Debug("Received invalid economy resource data from server.");
                    economyName = string.Empty;
                    economyResourceIds = new string[0];
                    economyResourceTitles = new string[0];
                    economyResourceCategories = new string[0];
                    economyResourceUnits = new string[0];
                    economyResourceStates = new string[0];
                    economyResourceValues = new double[0];
                    economyResourceMaxValues = new double[0];
                    economyResourceModifiers = new double[0];
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
                            category = categories[i],
                            progressValue = progressValues[i],
                            progressTarget = progressTargets[i],
                            rewardFunds = rewardFunds[i],
                            rewardScience = rewardScience[i],
                            rewardReputation = rewardReputation[i],
                            metricContributionId = metricContributionIds[i],
                            metricContributionAmount = metricContributionAmounts[i],
                            metricContributionMax = metricContributionMaxes[i],
                            economyResourceId = objectiveEconomyResourceIds[i],
                            economyResourceDelta = objectiveEconomyResourceDeltas[i],
                            rewardModifierResourceId = objectiveRewardModifierResourceIds[i],
                            allowScarcityRewardBonus = objectiveAllowScarcityRewardBonuses[i],
                            allowAbundanceRewardReduction = objectiveAllowAbundanceRewardReductions[i],
                            maxRewardModifierOverride = objectiveMaxRewardModifierOverrides[i],
                            requiresAcceptance = objectiveRequiresAcceptances[i],
                            acceptedBy = objectiveAcceptedBy[i],
                            acceptedAtUtc = objectiveAcceptedAtUtc[i],
                            progressUnit = progressUnits[i],
                            contributionLabel = contributionLabels[i],
                            contributorCount = contributorCounts[i],
                            contributors = contributors[i],
                            progressPerEvidence = progressPerEvidence[i],
                            uniqueContributors = uniqueContributors[i]
                        });
                    }
                }
                lock (campaignMetrics)
                {
                    campaignMetrics.Clear();
                    CampaignName = campaignName;
                    CampaignPhaseId = campaignPhaseId;
                    CampaignPhaseTitle = campaignPhaseTitle;
                    CampaignPhaseDescription = campaignPhaseDescription;
                    for (int i = 0; i < metricIds.Length; i++)
                    {
                        campaignMetrics.Add(new CampaignMetricSummary
                        {
                            id = metricIds[i],
                            title = metricTitles[i],
                            category = metricCategories[i],
                            unit = metricUnits[i],
                            value = metricValues[i],
                            target = metricTargets[i]
                        });
                    }
                }
                lock (campaignEvents)
                {
                    campaignEvents.Clear();
                    for (int i = 0; i < eventIds.Length; i++)
                    {
                        campaignEvents.Add(new CampaignEventSummary
                        {
                            id = eventIds[i],
                            title = eventTitles[i],
                            description = eventDescriptions[i],
                            status = eventStatuses[i]
                        });
                    }
                }
                lock (economyResources)
                {
                    economyResources.Clear();
                    EconomyName = economyName;
                    for (int i = 0; i < economyResourceIds.Length; i++)
                    {
                        economyResources.Add(new EconomyResourceSummary
                        {
                            id = economyResourceIds[i],
                            title = economyResourceTitles[i],
                            category = economyResourceCategories[i],
                            unit = economyResourceUnits[i],
                            state = economyResourceStates[i],
                            value = economyResourceValues[i],
                            maxValue = economyResourceMaxValues[i],
                            boundedModifier = economyResourceModifiers[i]
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
            GameEvents.Contract.onCompleted.Remove(OnContractCompleted);
            lock (objectives)
            {
                objectives.Clear();
                PackName = null;
                OnboardingText = null;
            }
            lock (campaignMetrics)
            {
                campaignMetrics.Clear();
                CampaignName = null;
                CampaignPhaseId = null;
                CampaignPhaseTitle = null;
                CampaignPhaseDescription = null;
            }
            lock (campaignEvents)
            {
                campaignEvents.Clear();
            }
            lock (economyResources)
            {
                economyResources.Clear();
                EconomyName = null;
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

        private void OnContractCompleted(Contract contract)
        {
            if (!dmpGame.serverAgencyProgressionEnabled || contract == null)
            {
                return;
            }

            ConfigNode contractNode = new ConfigNode();
            contract.Save(contractNode);
            string contractType = contractNode.GetValue("type");
            if (string.IsNullOrEmpty(contractType))
            {
                contractType = contract.GetType().Name;
            }
            SendEvidenceOnce(AgencyEvidenceType.CONTRACT_COMPLETED, BuildEvidenceId("contract", contractType));
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
            PostAgencyNotification("Agency objective complete: " + objectiveId + BuildRewardNotice(funds, science, reputation));
            DarkLog.Debug("Applied agency reward for " + objectiveId + ": funds=" + funds + ", science=" + science + ", reputation=" + reputation);
        }

        private void PostAgencyNotification(string message)
        {
            try
            {
                ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            catch (Exception e)
            {
                DarkLog.Debug("Failed to post agency notification, exception: " + e);
            }
        }

        private string BuildRewardNotice(double funds, float science, float reputation)
        {
            string rewards = string.Empty;
            if (funds != 0)
            {
                rewards = "Funds " + funds.ToString("0.##");
            }
            if (science != 0)
            {
                rewards = AppendRewardNotice(rewards, "Science " + science.ToString("0.##"));
            }
            if (reputation != 0)
            {
                rewards = AppendRewardNotice(rewards, "Rep " + reputation.ToString("0.##"));
            }
            return string.IsNullOrEmpty(rewards) ? string.Empty : "\nRewards: " + rewards;
        }

        private string AppendRewardNotice(string rewards, string reward)
        {
            if (string.IsNullOrEmpty(rewards))
            {
                return reward;
            }
            return rewards + ", " + reward;
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
        public string category;
        public double progressValue;
        public double progressTarget;
        public double rewardFunds;
        public float rewardScience;
        public float rewardReputation;
        public string metricContributionId;
        public double metricContributionAmount;
        public double metricContributionMax;
        public string economyResourceId;
        public double economyResourceDelta;
        public string rewardModifierResourceId;
        public bool allowScarcityRewardBonus;
        public bool allowAbundanceRewardReduction;
        public double maxRewardModifierOverride;
        public bool requiresAcceptance;
        public string acceptedBy;
        public string acceptedAtUtc;
        public string progressUnit;
        public string contributionLabel;
        public int contributorCount;
        public string contributors;
        public double progressPerEvidence;
        public bool uniqueContributors;
    }

    public class AgencyRewardSummary
    {
        public string objectiveId;
        public double funds;
        public float science;
        public float reputation;
    }

    public class CampaignMetricSummary
    {
        public string id;
        public string title;
        public string category;
        public string unit;
        public double value;
        public double target;
    }

    public class CampaignEventSummary
    {
        public string id;
        public string title;
        public string description;
        public string status;
    }

    public class EconomyResourceSummary
    {
        public string id;
        public string title;
        public string category;
        public string unit;
        public string state;
        public double value;
        public double maxValue;
        public double boundedModifier;
    }
}
