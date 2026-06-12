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
            double[] objectiveRewardFunds = new double[objectives.Length];
            float[] objectiveRewardScience = new float[objectives.Length];
            float[] objectiveRewardReputation = new float[objectives.Length];
            string[] objectiveMetricContributionIds = new string[objectives.Length];
            double[] objectiveMetricContributionAmounts = new double[objectives.Length];
            double[] objectiveMetricContributionMaxes = new double[objectives.Length];
            CampaignPhase currentPhase = DarkMultiPlayerServer.CampaignState.CurrentPhase;
            CampaignMetric[] campaignMetrics = DarkMultiPlayerServer.CampaignState.Metrics;
            string[] metricIds = new string[campaignMetrics.Length];
            string[] metricTitles = new string[campaignMetrics.Length];
            string[] metricCategories = new string[campaignMetrics.Length];
            string[] metricUnits = new string[campaignMetrics.Length];
            double[] metricValues = new double[campaignMetrics.Length];
            double[] metricTargets = new double[campaignMetrics.Length];

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
                objectiveRewardFunds[i] = objectives[i].rewardFunds;
                objectiveRewardScience[i] = objectives[i].rewardScience;
                objectiveRewardReputation[i] = objectives[i].rewardReputation;
                objectiveMetricContributionIds[i] = objectives[i].metricContributionId;
                objectiveMetricContributionAmounts[i] = objectives[i].metricContributionAmount;
                objectiveMetricContributionMaxes[i] = objectives[i].metricContributionMax;
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
