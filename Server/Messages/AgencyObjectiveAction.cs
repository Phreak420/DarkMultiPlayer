using MessageStream2;

namespace DarkMultiPlayerServer.Messages
{
    public class AgencyObjectiveAction
    {
        public static void HandleAgencyObjectiveAction(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string action = mr.Read<string>();
                string objectiveId = mr.Read<string>();
                if (action == "accept")
                {
                    DarkMultiPlayerServer.AgencyProgression.AcceptObjective(client.playerName, objectiveId);
                }
                else if (action == "abandon")
                {
                    DarkMultiPlayerServer.AgencyProgression.UnacceptObjective(client.playerName, objectiveId, false);
                }
            }
        }
    }
}
