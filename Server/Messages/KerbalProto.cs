using System;
using System.IO;
using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayerServer.Messages
{
    public class KerbalProto
    {
        public static void HandleKerbalProto(ClientObject client, byte[] messageData)
        {
            if (!TryReadKerbalProto(client, messageData, out string kerbalName, out byte[] kerbalData))
            {
                return;
            }

            SaveKerbalProto(client, kerbalName, kerbalData);
            RelayKerbalProto(client, messageData);
        }

        private static bool TryReadKerbalProto(ClientObject client, byte[] messageData, out string kerbalName, out byte[] kerbalData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                //Don't care about subspace / send time.
                mr.Read<double>();
                kerbalName = mr.Read<string>();
                if (!SafeFile.IsNameSafe(kerbalName))
                {
                    Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for an invalid kerbal name");
                    kerbalData = null;
                    return false;
                }
                kerbalData = mr.Read<byte[]>();
                return true;
            }
        }

        private static void SaveKerbalProto(ClientObject client, string kerbalName, byte[] kerbalData)
        {
            DarkLog.Debug("Saving kerbal " + kerbalName + " from " + client.playerName);
            lock (Server.universeSizeLock)
            {
                File.WriteAllBytes(Path.Combine(Server.universeDirectory, "Kerbals", kerbalName + ".txt"), kerbalData);
            }
        }

        private static void RelayKerbalProto(ClientObject client, byte[] messageData)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.KERBAL_REPLY;
            newMessage.data = messageData;
            ClientHandler.SendToAll(client, newMessage, false);
        }
    }
}
