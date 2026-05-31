using System;
using System.Collections.Generic;
using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayerServer.Messages
{
    public class LockSystem
    {
        public static void SendAllLocks(ClientObject client)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.LOCK_SYSTEM;
            //Send the dictionary as 2 string[]'s.
            Dictionary<string,string> lockList = DarkMultiPlayerServer.LockSystem.fetch.GetLockList();
            List<string> lockKeys = new List<string>(lockList.Keys);
            List<string> lockValues = new List<string>(lockList.Values);
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write((int)LockMessageType.LIST);
                mw.Write<string[]>(lockKeys.ToArray());
                mw.Write<string[]>(lockValues.ToArray());
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void HandleLockSystemMessage(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                //Read the lock-system message type
                LockMessageType lockMessageType = (LockMessageType)mr.Read<int>();
                switch (lockMessageType)
                {
                    case LockMessageType.ACQUIRE:
                        {
                            if (!TryReadOwnedLockMessage(client, mr, out string playerName, out string lockName))
                            {
                                return;
                            }
                            bool force = mr.Read<bool>();
                            HandleAcquireLock(playerName, lockName, force);
                        }
                        break;
                    case LockMessageType.RELEASE:
                        {
                            if (!TryReadOwnedLockMessage(client, mr, out string playerName, out string lockName))
                            {
                                return;
                            }
                            HandleReleaseLock(client, playerName, lockName);
                        }
                        break;
                }
            }
        }

        private static bool TryReadOwnedLockMessage(ClientObject client, MessageReader mr, out string playerName, out string lockName)
        {
            playerName = mr.Read<string>();
            lockName = mr.Read<string>();
            if (playerName != client.playerName)
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending a lock message for another player");
                return false;
            }
            return true;
        }

        private static void HandleAcquireLock(string playerName, string lockName, bool force)
        {
            bool lockResult = DarkMultiPlayerServer.LockSystem.fetch.AcquireLock(lockName, playerName, force);
            SendLockResult(LockMessageType.ACQUIRE, playerName, lockName, lockResult);
            if (lockResult)
            {
                DarkLog.Debug(playerName + " acquired lock " + lockName);
            }
            else
            {
                DarkLog.Debug(playerName + " failed to acquire lock " + lockName);
            }
        }

        private static void HandleReleaseLock(ClientObject client, string playerName, string lockName)
        {
            bool lockResult = DarkMultiPlayerServer.LockSystem.fetch.ReleaseLock(lockName, playerName);
            if (!lockResult)
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for releasing a lock you do not own");
            }
            else
            {
                SendLockResult(LockMessageType.RELEASE, playerName, lockName, lockResult);
            }
            if (lockResult)
            {
                DarkLog.Debug(playerName + " released lock " + lockName);
            }
            else
            {
                DarkLog.Debug(playerName + " failed to release lock " + lockName);
            }
        }

        private static void SendLockResult(LockMessageType lockMessageType, string playerName, string lockName, bool lockResult)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.LOCK_SYSTEM;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write((int)lockMessageType);
                mw.Write(playerName);
                mw.Write(lockName);
                mw.Write(lockResult);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(null, newMessage, true);
        }
    }
}
