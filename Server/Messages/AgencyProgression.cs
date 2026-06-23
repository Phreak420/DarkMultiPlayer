using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayerServer.Messages
{
    public class AgencyProgression
    {
        public static void SendAgencyProgression(ClientObject client)
        {
            if (!Settings.IsAgencyProgressionActive())
            {
                return;
            }

            DarkMultiPlayerServer.AgencyObjective[] objectives = DarkMultiPlayerServer.AgencyProgression.GetObjectivesForPlayer(client.playerName);
            string[] objectiveIds = new string[objectives.Length];
            string[] objectiveTitles = new string[objectives.Length];
            string[] objectiveDescriptions = new string[objectives.Length];
            string[] objectiveStatuses = new string[objectives.Length];
            string[] objectiveScopes = new string[objectives.Length];
            string[] objectiveContractTypes = new string[objectives.Length];
            string[] objectiveIssuers = new string[objectives.Length];
            string[] objectiveCategories = new string[objectives.Length];
            double[] objectiveProgressValues = new double[objectives.Length];
            double[] objectiveProgressTargets = new double[objectives.Length];
            double[] objectiveProgressPerEvidence = new double[objectives.Length];
            double[] objectiveRewardFunds = new double[objectives.Length];
            float[] objectiveRewardScience = new float[objectives.Length];
            float[] objectiveRewardReputation = new float[objectives.Length];
            string[] objectiveMetricContributionIds = new string[objectives.Length];
            double[] objectiveMetricContributionAmounts = new double[objectives.Length];
            double[] objectiveMetricContributionMaxes = new double[objectives.Length];
            string[] objectiveEconomyResourceIds = new string[objectives.Length];
            double[] objectiveEconomyResourceDeltas = new double[objectives.Length];
            string[] objectiveRewardModifierResourceIds = new string[objectives.Length];
            bool[] objectiveAllowScarcityRewardBonuses = new bool[objectives.Length];
            bool[] objectiveAllowAbundanceRewardReductions = new bool[objectives.Length];
            double[] objectiveMaxRewardModifierOverrides = new double[objectives.Length];
            bool[] objectiveRequiresAcceptances = new bool[objectives.Length];
            string[] objectiveAcceptedBy = new string[objectives.Length];
            string[] objectiveAcceptedAtUtc = new string[objectives.Length];
            string[] objectiveProgressUnits = new string[objectives.Length];
            string[] objectiveContributionLabels = new string[objectives.Length];
            int[] objectiveContributorCounts = new int[objectives.Length];
            string[] objectiveContributors = new string[objectives.Length];
            bool[] objectiveUniqueContributors = new bool[objectives.Length];
            CampaignPhase currentPhase = DarkMultiPlayerServer.CampaignState.CurrentPhase;
            CampaignMetric[] campaignMetrics = DarkMultiPlayerServer.CampaignState.Metrics;
            string[] metricIds = new string[campaignMetrics.Length];
            string[] metricTitles = new string[campaignMetrics.Length];
            string[] metricCategories = new string[campaignMetrics.Length];
            string[] metricUnits = new string[campaignMetrics.Length];
            double[] metricValues = new double[campaignMetrics.Length];
            double[] metricTargets = new double[campaignMetrics.Length];
            CampaignEvent[] campaignEvents = DarkMultiPlayerServer.CampaignState.Events;
            string[] eventIds = new string[campaignEvents.Length];
            string[] eventTitles = new string[campaignEvents.Length];
            string[] eventDescriptions = new string[campaignEvents.Length];
            string[] eventStatuses = new string[campaignEvents.Length];
            EconomyResource[] economyResources = DarkMultiPlayerServer.EconomyState.Resources;
            string[] economyResourceIds = new string[economyResources.Length];
            string[] economyResourceTitles = new string[economyResources.Length];
            string[] economyResourceCategories = new string[economyResources.Length];
            string[] economyResourceUnits = new string[economyResources.Length];
            string[] economyResourceStates = new string[economyResources.Length];
            double[] economyResourceValues = new double[economyResources.Length];
            double[] economyResourceMaxValues = new double[economyResources.Length];
            double[] economyResourceModifiers = new double[economyResources.Length];
            AgencyJournalRecord[] journalRecords = DarkMultiPlayerServer.AgencyProgression.GetRecentJournalRecords(client.playerName, 8);
            string[] journalTimes = new string[journalRecords.Length];
            string[] journalActions = new string[journalRecords.Length];
            string[] journalObjectiveIds = new string[journalRecords.Length];
            string[] journalPlayerNames = new string[journalRecords.Length];
            string[] journalActors = new string[journalRecords.Length];
            string[] journalDetails = new string[journalRecords.Length];

            for (int i = 0; i < objectives.Length; i++)
            {
                objectiveIds[i] = objectives[i].id;
                objectiveTitles[i] = objectives[i].title;
                objectiveDescriptions[i] = objectives[i].description;
                objectiveStatuses[i] = objectives[i].status;
                objectiveScopes[i] = objectives[i].scope;
                objectiveContractTypes[i] = objectives[i].contractType;
                objectiveIssuers[i] = objectives[i].issuer;
                objectiveCategories[i] = objectives[i].category;
                objectiveProgressValues[i] = objectives[i].progressValue;
                objectiveProgressTargets[i] = objectives[i].progressTarget;
                objectiveProgressPerEvidence[i] = objectives[i].progressPerEvidence;
                objectiveRewardFunds[i] = objectives[i].rewardFunds;
                objectiveRewardScience[i] = objectives[i].rewardScience;
                objectiveRewardReputation[i] = objectives[i].rewardReputation;
                objectiveMetricContributionIds[i] = objectives[i].metricContributionId;
                objectiveMetricContributionAmounts[i] = objectives[i].metricContributionAmount;
                objectiveMetricContributionMaxes[i] = objectives[i].metricContributionMax;
                objectiveEconomyResourceIds[i] = objectives[i].economyResourceId;
                objectiveEconomyResourceDeltas[i] = objectives[i].economyResourceDelta;
                objectiveRewardModifierResourceIds[i] = objectives[i].rewardModifierResourceId;
                objectiveAllowScarcityRewardBonuses[i] = objectives[i].allowScarcityRewardBonus;
                objectiveAllowAbundanceRewardReductions[i] = objectives[i].allowAbundanceRewardReduction;
                objectiveMaxRewardModifierOverrides[i] = objectives[i].maxRewardModifierOverride;
                objectiveRequiresAcceptances[i] = objectives[i].requiresAcceptance;
                objectiveAcceptedBy[i] = objectives[i].acceptedBy;
                objectiveAcceptedAtUtc[i] = objectives[i].acceptedAtUtc;
                objectiveProgressUnits[i] = objectives[i].progressUnit;
                objectiveContributionLabels[i] = objectives[i].contributionLabel;
                objectiveContributorCounts[i] = objectives[i].contributorCount;
                objectiveContributors[i] = objectives[i].contributors;
                objectiveUniqueContributors[i] = objectives[i].uniqueContributors;
            }
            for (int i = 0; i < campaignMetrics.Length; i++)
            {
                metricIds[i] = campaignMetrics[i].id;
                metricTitles[i] = campaignMetrics[i].title;
                metricCategories[i] = campaignMetrics[i].category;
                metricUnits[i] = campaignMetrics[i].unit;
                metricValues[i] = campaignMetrics[i].value;
                metricTargets[i] = campaignMetrics[i].target;
            }
            for (int i = 0; i < campaignEvents.Length; i++)
            {
                eventIds[i] = campaignEvents[i].id;
                eventTitles[i] = campaignEvents[i].title;
                eventDescriptions[i] = campaignEvents[i].description;
                eventStatuses[i] = campaignEvents[i].status;
            }
            for (int i = 0; i < economyResources.Length; i++)
            {
                economyResourceIds[i] = economyResources[i].id;
                economyResourceTitles[i] = economyResources[i].title;
                economyResourceCategories[i] = economyResources[i].category;
                economyResourceUnits[i] = economyResources[i].unit;
                economyResourceStates[i] = economyResources[i].state;
                economyResourceValues[i] = economyResources[i].value;
                economyResourceMaxValues[i] = economyResources[i].maxValue;
                economyResourceModifiers[i] = economyResources[i].boundedModifier;
            }
            for (int i = 0; i < journalRecords.Length; i++)
            {
                journalTimes[i] = journalRecords[i].occurredAtUtc.ToString("u");
                journalActions[i] = journalRecords[i].action;
                journalObjectiveIds[i] = journalRecords[i].objectiveId;
                journalPlayerNames[i] = journalRecords[i].playerName;
                journalActors[i] = journalRecords[i].actor;
                journalDetails[i] = journalRecords[i].details;
            }

            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.AGENCY_PROGRESS;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(DarkMultiPlayerServer.AgencyProgression.PackName);
                mw.Write<string[]>(objectiveIds);
                mw.Write<string[]>(objectiveTitles);
                mw.Write<string[]>(objectiveDescriptions);
                mw.Write<string[]>(objectiveStatuses);
                mw.Write<string[]>(objectiveScopes);
                mw.Write<string[]>(objectiveContractTypes);
                mw.Write<string[]>(objectiveIssuers);
                mw.Write<double[]>(objectiveProgressValues);
                mw.Write<double[]>(objectiveProgressTargets);
                mw.Write<double[]>(objectiveRewardFunds);
                mw.Write<float[]>(objectiveRewardScience);
                mw.Write<float[]>(objectiveRewardReputation);
                mw.Write<string[]>(objectiveCategories);
                mw.Write<string>(DarkMultiPlayerServer.AgencyProgression.OnboardingText);
                mw.Write<string>(DarkMultiPlayerServer.CampaignState.CampaignName);
                mw.Write<string>(currentPhase == null ? string.Empty : currentPhase.id);
                mw.Write<string>(currentPhase == null ? string.Empty : currentPhase.title);
                mw.Write<string>(currentPhase == null ? string.Empty : currentPhase.description);
                mw.Write<string[]>(metricIds);
                mw.Write<string[]>(metricTitles);
                mw.Write<string[]>(metricCategories);
                mw.Write<string[]>(metricUnits);
                mw.Write<double[]>(metricValues);
                mw.Write<double[]>(metricTargets);
                mw.Write<string[]>(objectiveMetricContributionIds);
                mw.Write<double[]>(objectiveMetricContributionAmounts);
                mw.Write<double[]>(objectiveMetricContributionMaxes);
                mw.Write<string[]>(objectiveProgressUnits);
                mw.Write<string[]>(objectiveContributionLabels);
                mw.Write<int[]>(objectiveContributorCounts);
                mw.Write<string[]>(objectiveContributors);
                mw.Write<double[]>(objectiveProgressPerEvidence);
                mw.Write<bool[]>(objectiveUniqueContributors);
                mw.Write<string[]>(eventIds);
                mw.Write<string[]>(eventTitles);
                mw.Write<string[]>(eventDescriptions);
                mw.Write<string[]>(eventStatuses);
                mw.Write<string[]>(objectiveEconomyResourceIds);
                mw.Write<double[]>(objectiveEconomyResourceDeltas);
                mw.Write<string>(DarkMultiPlayerServer.EconomyState.EconomyName);
                mw.Write<string[]>(economyResourceIds);
                mw.Write<string[]>(economyResourceTitles);
                mw.Write<string[]>(economyResourceCategories);
                mw.Write<string[]>(economyResourceUnits);
                mw.Write<string[]>(economyResourceStates);
                mw.Write<double[]>(economyResourceValues);
                mw.Write<double[]>(economyResourceMaxValues);
                mw.Write<double[]>(economyResourceModifiers);
                mw.Write<string[]>(objectiveRewardModifierResourceIds);
                mw.Write<bool[]>(objectiveAllowScarcityRewardBonuses);
                mw.Write<bool[]>(objectiveAllowAbundanceRewardReductions);
                mw.Write<double[]>(objectiveMaxRewardModifierOverrides);
                mw.Write<bool[]>(objectiveRequiresAcceptances);
                mw.Write<string[]>(objectiveAcceptedBy);
                mw.Write<string[]>(objectiveAcceptedAtUtc);
                mw.Write<string[]>(journalTimes);
                mw.Write<string[]>(journalActions);
                mw.Write<string[]>(journalObjectiveIds);
                mw.Write<string[]>(journalPlayerNames);
                mw.Write<string[]>(journalActors);
                mw.Write<string[]>(journalDetails);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void SendAgencyProgressionToAll()
        {
            if (!Settings.IsAgencyProgressionActive())
            {
                return;
            }

            foreach (ClientObject client in ClientHandler.GetClients())
            {
                if (client.authenticated)
                {
                    SendAgencyProgression(client);
                }
            }
        }
    }
}
