using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayerServer.Messages
{
    public class AgencyReward
    {
        public static void SendAgencyReward(ClientObject client, string objectiveId, double funds, float science, float reputation)
        {
            if (client == null || !Settings.settingsStore.agencyProgressionEnabled)
            {
                return;
            }

            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.AGENCY_REWARD;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(objectiveId);
                mw.Write<double>(funds);
                mw.Write<float>(science);
                mw.Write<float>(reputation);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }
    }
}
