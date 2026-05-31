using System;
using System.IO;
using DarkMultiPlayerCommon;
using DarkMultiPlayerServer;
using MessageStream2;

namespace ServerValidationTests
{
    public static class Program
    {
        private static int failures;

        public static int Main()
        {
            Run("Flag upload rejects unsafe names", FlagUploadRejectsUnsafeNames);
            Run("Flag delete rejects unsafe names", FlagDeleteRejectsUnsafeNames);
            Run("Kerbal proto rejects unsafe names", KerbalProtoRejectsUnsafeNames);
            Run("Lock acquire spoof does not mutate locks", LockAcquireSpoofDoesNotMutateLocks);
            Run("Lock release spoof does not mutate locks", LockReleaseSpoofDoesNotMutateLocks);

            if (failures == 0)
            {
                Console.WriteLine("All server validation tests passed.");
            }
            return failures;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS " + name);
            }
            catch (Exception e)
            {
                failures++;
                Console.WriteLine("FAIL " + name + ": " + e.Message);
            }
        }

        private static void FlagUploadRejectsUnsafeNames()
        {
            string universe = CreateUniverse();
            string outsideFile = Path.Combine(universe, "outside.png");
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)FlagMessageType.UPLOAD_FILE);
                mw.Write<string>("Alice");
                mw.Write<string>("../outside.png");
                mw.Write<byte[]>(PngBytes());
                DarkMultiPlayerServer.Messages.FlagSync.HandleFlagSync(client, mw.GetMessageBytes());
            }

            AssertConnectionEndQueued(client);
            Assert(!File.Exists(outsideFile), "unsafe flag upload wrote outside the player flag directory");
        }

        private static void FlagDeleteRejectsUnsafeNames()
        {
            string universe = CreateUniverse();
            string outsideFile = Path.Combine(universe, "outside.png");
            File.WriteAllText(outsideFile, "do not delete");
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)FlagMessageType.DELETE_FILE);
                mw.Write<string>("Alice");
                mw.Write<string>("../outside.png");
                DarkMultiPlayerServer.Messages.FlagSync.HandleFlagSync(client, mw.GetMessageBytes());
            }

            AssertConnectionEndQueued(client);
            Assert(File.Exists(outsideFile), "unsafe flag delete removed a file outside the player flag directory");
        }

        private static void KerbalProtoRejectsUnsafeNames()
        {
            string universe = CreateUniverse();
            string outsideFile = Path.Combine(universe, "escaped.txt");
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<double>(0);
                mw.Write<string>("../escaped");
                mw.Write<byte[]>(new byte[] { 1, 2, 3 });
                DarkMultiPlayerServer.Messages.KerbalProto.HandleKerbalProto(client, mw.GetMessageBytes());
            }

            AssertConnectionEndQueued(client);
            Assert(!File.Exists(outsideFile), "unsafe kerbal name wrote outside the kerbal directory");
        }

        private static void LockAcquireSpoofDoesNotMutateLocks()
        {
            DarkMultiPlayerServer.LockSystem.Reset();
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)LockMessageType.ACQUIRE);
                mw.Write<string>("Bob");
                mw.Write<string>("control-vessel-test");
                mw.Write<bool>(true);
                DarkMultiPlayerServer.Messages.LockSystem.HandleLockSystemMessage(client, mw.GetMessageBytes());
            }

            AssertConnectionEndQueued(client);
            Assert(!DarkMultiPlayerServer.LockSystem.fetch.GetLockList().ContainsKey("control-vessel-test"), "spoofed lock acquire mutated lock state");
        }

        private static void LockReleaseSpoofDoesNotMutateLocks()
        {
            DarkMultiPlayerServer.LockSystem.Reset();
            DarkMultiPlayerServer.LockSystem.fetch.AcquireLock("control-vessel-test", "Bob", false);
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)LockMessageType.RELEASE);
                mw.Write<string>("Bob");
                mw.Write<string>("control-vessel-test");
                DarkMultiPlayerServer.Messages.LockSystem.HandleLockSystemMessage(client, mw.GetMessageBytes());
            }

            AssertConnectionEndQueued(client);
            Assert(DarkMultiPlayerServer.LockSystem.fetch.GetLockList()["control-vessel-test"] == "Bob", "spoofed lock release mutated lock state");
        }

        private static string CreateUniverse()
        {
            string universe = Path.Combine(Path.GetTempPath(), "dmp-validation-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(universe, "Flags"));
            Directory.CreateDirectory(Path.Combine(universe, "Kerbals"));
            Server.universeDirectory = universe;
            return universe;
        }

        private static ClientObject CreateClient(string playerName)
        {
            return new ClientObject
            {
                authenticated = true,
                connectionStatus = ConnectionStatus.CONNECTED,
                playerName = playerName
            };
        }

        private static byte[] PngBytes()
        {
            return new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1 };
        }

        private static void AssertConnectionEndQueued(ClientObject client)
        {
            Assert(client.sendMessageQueueHigh.TryDequeue(out ServerMessage message), "no high-priority response was queued");
            Assert(message.type == ServerMessageType.CONNECTION_END, "expected CONNECTION_END but received " + message.type);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }
}
