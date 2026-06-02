using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayerServer.Messages
{
    public class AgencyProgression
    {
        public static void SendAgencyProgression(ClientObject client)
        {
            if (!Settings.settingsStore.agencyProgressionEnabled)
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
            double[] objectiveProgressValues = new double[objectives.Length];
            double[] objectiveProgressTargets = new double[objectives.Length];
            double[] objectiveRewardFunds = new double[objectives.Length];
            float[] objectiveRewardScience = new float[objectives.Length];
            float[] objectiveRewardReputation = new float[objectives.Length];

            for (int i = 0; i < objectives.Length; i++)
            {
                objectiveIds[i] = objectives[i].id;
                objectiveTitles[i] = objectives[i].title;
                objectiveDescriptions[i] = objectives[i].description;
                objectiveStatuses[i] = objectives[i].status;
                objectiveScopes[i] = objectives[i].scope;
                objectiveContractTypes[i] = objectives[i].contractType;
                objectiveIssuers[i] = objectives[i].issuer;
                objectiveProgressValues[i] = objectives[i].progressValue;
                objectiveProgressTargets[i] = objectives[i].progressTarget;
                objectiveRewardFunds[i] = objectives[i].rewardFunds;
                objectiveRewardScience[i] = objectives[i].rewardScience;
                objectiveRewardReputation[i] = objectives[i].rewardReputation;
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
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void SendAgencyProgressionToAll()
        {
            if (!Settings.settingsStore.agencyProgressionEnabled)
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
