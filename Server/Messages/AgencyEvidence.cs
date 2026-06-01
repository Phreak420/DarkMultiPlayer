using MessageStream2;

namespace DarkMultiPlayerServer.Messages
{
    public class AgencyEvidence
    {
        public static void HandleAgencyEvidence(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                int evidenceType = mr.Read<int>();
                string evidenceId = mr.Read<string>();
                double gameTime = mr.Read<double>();
                DarkMultiPlayerServer.AgencyProgression.RecordEvidence(client, evidenceType, evidenceId, gameTime);
            }
        }
    }
}
