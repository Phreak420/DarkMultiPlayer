using System;
using MessageStream2;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer.Messages
{
    public class SplitMessage
    {
        public static void HandleSplitMessage(ClientObject client, byte[] messageData)
        {
            if (!client.isReceivingSplitMessage)
            {
                //New split message
                using (MessageReader mr = new MessageReader(messageData))
                {
                    int splitMessageType = mr.Read<int>();
                    int splitMessageLength = mr.Read<int>();
                    if (!IsValidSplitMessage(client, splitMessageType, splitMessageLength))
                    {
                        return;
                    }
                    client.receiveSplitMessage = new ClientMessage();
                    client.receiveSplitMessage.type = (ClientMessageType)splitMessageType;
                    client.receiveSplitMessage.data = new byte[splitMessageLength];
                    client.receiveSplitMessageBytesLeft = client.receiveSplitMessage.data.Length;
                    byte[] firstSplitData = mr.Read<byte[]>();
                    if (!IsValidSplitChunk(client, firstSplitData.Length))
                    {
                        ResetSplitMessage(client);
                        return;
                    }
                    firstSplitData.CopyTo(client.receiveSplitMessage.data, 0);
                    client.receiveSplitMessageBytesLeft -= firstSplitData.Length;
                }
                client.isReceivingSplitMessage = true;
            }
            else
            {
                //Continued split message
                if (!IsValidSplitChunk(client, messageData.Length))
                {
                    ResetSplitMessage(client);
                    return;
                }
                messageData.CopyTo(client.receiveSplitMessage.data, client.receiveSplitMessage.data.Length - client.receiveSplitMessageBytesLeft);
                client.receiveSplitMessageBytesLeft -= messageData.Length;
            }
            if (client.receiveSplitMessageBytesLeft == 0)
            {
                ClientHandler.HandleMessage(client, client.receiveSplitMessage);
                client.receiveSplitMessage = null;
                client.isReceivingSplitMessage = false;
            }
        }

        private static bool IsValidSplitMessage(ClientObject client, int messageType, int messageLength)
        {
            if (messageType < 0 || messageType > (Enum.GetNames(typeof(ClientMessageType)).Length - 1))
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "Invalid DMP message. Disconnected.");
                return false;
            }
            if (!Common.IsValidMessageSize(messageLength))
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "Invalid DMP message. Disconnected.");
                return false;
            }
            return true;
        }

        private static bool IsValidSplitChunk(ClientObject client, int chunkLength)
        {
            if (chunkLength <= 0 || chunkLength > client.receiveSplitMessageBytesLeft)
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "Invalid DMP message. Disconnected.");
                return false;
            }
            return true;
        }

        private static void ResetSplitMessage(ClientObject client)
        {
            client.receiveSplitMessage = null;
            client.receiveSplitMessageBytesLeft = 0;
            client.isReceivingSplitMessage = false;
        }
    }
}
