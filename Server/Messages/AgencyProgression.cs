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

            DarkMultiPlayerServer.AgencyObjective[] objectives = DarkMultiPlayerServer.AgencyProgression.Objectives;
            string[] objectiveIds = new string[objectives.Length];
            string[] objectiveTitles = new string[objectives.Length];
            string[] objectiveDescriptions = new string[objectives.Length];
            string[] objectiveStatuses = new string[objectives.Length];
            string[] objectiveScopes = new string[objectives.Length];

            for (int i = 0; i < objectives.Length; i++)
            {
                objectiveIds[i] = objectives[i].id;
                objectiveTitles[i] = objectives[i].title;
                objectiveDescriptions[i] = objectives[i].description;
                objectiveStatuses[i] = objectives[i].status;
                objectiveScopes[i] = objectives[i].scope;
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
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }
    }
}
