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
            InitializeServerState();

            Run("Flag upload rejects unsafe names", FlagUploadRejectsUnsafeNames);
            Run("Flag delete rejects unsafe names", FlagDeleteRejectsUnsafeNames);
            Run("Kerbal proto saves safe names", KerbalProtoSavesSafeNames);
            Run("Kerbal proto rejects unsafe names", KerbalProtoRejectsUnsafeNames);
            Run("Lock acquire mutates locks for owner", LockAcquireMutatesLocksForOwner);
            Run("Lock release mutates locks for owner", LockReleaseMutatesLocksForOwner);
            Run("Lock acquire spoof does not mutate locks", LockAcquireSpoofDoesNotMutateLocks);
            Run("Lock release spoof does not mutate locks", LockReleaseSpoofDoesNotMutateLocks);
            Run("Compression round-trips byte arrays", CompressionRoundTripsByteArrays);
            Run("Compression round-trips recycled byte arrays", CompressionRoundTripsRecycledByteArrays);
            Run("Message size validation rejects invalid lengths", MessageSizeValidationRejectsInvalidLengths);
            Run("Split message rejects oversized declared length", SplitMessageRejectsOversizedDeclaredLength);
            Run("Split message rejects oversized first chunk", SplitMessageRejectsOversizedFirstChunk);
            Run("Agency progression disabled clears objectives", AgencyProgressionDisabledClearsObjectives);
            Run("Agency progression enabled creates default objectives", AgencyProgressionEnabledCreatesDefaultObjectives);
            Run("Agency progression skips invalid objective IDs", AgencyProgressionSkipsInvalidObjectiveIds);
            Run("Agency evidence disabled is ignored", AgencyEvidenceDisabledIsIgnored);
            Run("Agency evidence enabled records audit log", AgencyEvidenceEnabledRecordsAuditLog);
            Run("Agency science evidence records audit log", AgencyScienceEvidenceRecordsAuditLog);
            Run("Agency vessel evidence records audit log", AgencyVesselEvidenceRecordsAuditLog);
            Run("Agency docking evidence records audit log", AgencyDockingEvidenceRecordsAuditLog);
            Run("Agency evidence query returns records", AgencyEvidenceQueryReturnsRecords);
            Run("Agency evidence completes matching objective", AgencyEvidenceCompletesMatchingObjective);
            Run("Agency objective completion queues reward", AgencyObjectiveCompletionQueuesReward);
            Run("Agency evidence rejects invalid IDs", AgencyEvidenceRejectsInvalidIds);

            if (failures == 0)
            {
                Console.WriteLine("All server validation tests passed.");
            }
            return failures;
        }

        private static void InitializeServerState()
        {
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-config-" + Guid.NewGuid().ToString("N"));
            Settings.Reset();
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

        private static void KerbalProtoSavesSafeNames()
        {
            string universe = CreateUniverse();
            string kerbalFile = Path.Combine(universe, "Kerbals", "Jebediah Kerman.txt");
            ClientObject sender = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<double>(0);
                mw.Write<string>("Jebediah Kerman");
                mw.Write<byte[]>(new byte[] { 1, 2, 3 });
                DarkMultiPlayerServer.Messages.KerbalProto.HandleKerbalProto(sender, mw.GetMessageBytes());
            }

            Assert(File.Exists(kerbalFile), "safe kerbal proto was not saved");
            Assert(File.ReadAllBytes(kerbalFile).Length == 3, "safe kerbal proto saved unexpected data");
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

        private static void LockAcquireMutatesLocksForOwner()
        {
            DarkMultiPlayerServer.LockSystem.Reset();
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)LockMessageType.ACQUIRE);
                mw.Write<string>("Alice");
                mw.Write<string>("control-vessel-test");
                mw.Write<bool>(false);
                DarkMultiPlayerServer.Messages.LockSystem.HandleLockSystemMessage(client, mw.GetMessageBytes());
            }

            Assert(DarkMultiPlayerServer.LockSystem.fetch.GetLockList()["control-vessel-test"] == "Alice", "owner lock acquire did not mutate lock state");
        }

        private static void LockReleaseMutatesLocksForOwner()
        {
            DarkMultiPlayerServer.LockSystem.Reset();
            DarkMultiPlayerServer.LockSystem.fetch.AcquireLock("control-vessel-test", "Alice", false);
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)LockMessageType.RELEASE);
                mw.Write<string>("Alice");
                mw.Write<string>("control-vessel-test");
                DarkMultiPlayerServer.Messages.LockSystem.HandleLockSystemMessage(client, mw.GetMessageBytes());
            }

            Assert(!DarkMultiPlayerServer.LockSystem.fetch.GetLockList().ContainsKey("control-vessel-test"), "owner lock release did not mutate lock state");
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

        private static void CompressionRoundTripsByteArrays()
        {
            bool previousCompressionEnabled = Compression.compressionEnabled;
            Compression.compressionEnabled = true;
            try
            {
                byte[] input = RepeatingBytes(Compression.COMPRESSION_THRESHOLD * 2);
                byte[] compressed = Compression.CompressIfNeeded(input);
                byte[] decompressed = Compression.DecompressIfNeeded(compressed);
                Assert(Compression.ByteCompare(input, decompressed), "byte[] compression round-trip changed data");
            }
            finally
            {
                Compression.compressionEnabled = previousCompressionEnabled;
            }
        }

        private static void CompressionRoundTripsRecycledByteArrays()
        {
            bool previousCompressionEnabled = Compression.compressionEnabled;
            Compression.compressionEnabled = true;
            ByteArray input = ByteRecycler.GetObject(Compression.COMPRESSION_THRESHOLD * 2);
            ByteArray compressed = null;
            ByteArray decompressed = null;
            try
            {
                byte[] source = RepeatingBytes(input.Length);
                Array.Copy(source, input.data, source.Length);
                compressed = Compression.CompressIfNeeded(input);
                decompressed = Compression.DecompressIfNeeded(compressed);
                Assert(decompressed.Length == source.Length, "ByteArray compression round-trip changed length");
                for (int i = 0; i < source.Length; i++)
                {
                    if (decompressed.data[i] != source[i])
                    {
                        throw new Exception("ByteArray compression round-trip changed data at byte " + i);
                    }
                }
            }
            finally
            {
                Compression.compressionEnabled = previousCompressionEnabled;
                ByteRecycler.ReleaseObject(input);
                if (compressed != null)
                {
                    ByteRecycler.ReleaseObject(compressed);
                }
                if (decompressed != null)
                {
                    ByteRecycler.ReleaseObject(decompressed);
                }
            }
        }

        private static void MessageSizeValidationRejectsInvalidLengths()
        {
            Assert(!Common.IsValidMessageSize(-1), "negative message length was accepted");
            Assert(!Common.IsValidMessageSize(0), "zero message length was accepted as an allocation size");
            Assert(!Common.IsValidMessageSize(Common.MAX_MESSAGE_SIZE), "maximum boundary message length was accepted");
            Assert(Common.IsValidMessageSize(Common.MAX_MESSAGE_SIZE - 1), "largest valid message length was rejected");
        }

        private static void SplitMessageRejectsOversizedDeclaredLength()
        {
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)ClientMessageType.CHAT_MESSAGE);
                mw.Write<int>(Common.MAX_MESSAGE_SIZE);
                mw.Write<byte[]>(new byte[] { 1 });
                DarkMultiPlayerServer.Messages.SplitMessage.HandleSplitMessage(client, mw.GetMessageBytes());
            }

            AssertConnectionEndQueued(client);
            Assert(client.receiveSplitMessage == null, "oversized split message allocated a receive buffer");
            Assert(!client.isReceivingSplitMessage, "oversized split message left split receive state active");
        }

        private static void SplitMessageRejectsOversizedFirstChunk()
        {
            ClientObject client = CreateClient("Alice");

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)ClientMessageType.CHAT_MESSAGE);
                mw.Write<int>(1);
                mw.Write<byte[]>(new byte[] { 1, 2 });
                DarkMultiPlayerServer.Messages.SplitMessage.HandleSplitMessage(client, mw.GetMessageBytes());
            }

            AssertConnectionEndQueued(client);
            Assert(client.receiveSplitMessage == null, "invalid split chunk left a receive buffer");
            Assert(!client.isReceivingSplitMessage, "invalid split chunk left split receive state active");
        }

        private static void AgencyProgressionDisabledClearsObjectives()
        {
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));

            AgencyProgression.Load(false);

            Assert(AgencyProgression.Objectives.Length == 0, "disabled agency progression loaded objectives");
            Assert(!File.Exists(Path.Combine(Server.configDirectory, "AgencyProgression.json")), "disabled agency progression created a config file");
        }

        private static void AgencyProgressionEnabledCreatesDefaultObjectives()
        {
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));

            AgencyProgression.Load(true);

            Assert(File.Exists(Path.Combine(Server.configDirectory, "AgencyProgression.json")), "enabled agency progression did not create a default config file");
            Assert(AgencyProgression.PackName == "Server Agency", "default agency pack name was not loaded");
            Assert(AgencyProgression.Objectives.Length == 2, "default agency objectives were not loaded");
        }

        private static void AgencyProgressionSkipsInvalidObjectiveIds()
        {
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"\",\"title\":\"Bad\",\"description\":\"Ignored\",\"status\":\"Available\",\"scope\":\"Personal\"},{\"id\":\"valid-objective\",\"title\":\"Valid\",\"description\":\"Kept\",\"status\":\"Available\",\"scope\":\"Server\"}]}");

            AgencyProgression.Load(true);

            Assert(AgencyProgression.Objectives.Length == 1, "invalid agency objective id was not skipped");
            Assert(AgencyProgression.Objectives[0].id == "valid-objective", "valid agency objective was not loaded");
        }

        private static void AgencyEvidenceDisabledIsIgnored()
        {
            string universe = CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = false;
            ClientObject client = CreateClient("Alice");

            SendAgencyEvidence(client, AgencyEvidenceType.TECHNOLOGY_RESEARCHED, "basicRocketry");

            Assert(!Directory.Exists(Path.Combine(universe, "AgencyEvidence")), "disabled agency evidence created evidence directory");
        }

        private static void AgencyEvidenceEnabledRecordsAuditLog()
        {
            string universe = CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Alice");

            SendAgencyEvidence(client, AgencyEvidenceType.TECHNOLOGY_RESEARCHED, "basicRocketry");

            string evidenceFile = Path.Combine(universe, "AgencyEvidence", "Alice.log");
            Assert(File.Exists(evidenceFile), "enabled agency evidence did not create an audit log");
            string evidenceLog = File.ReadAllText(evidenceFile);
            Assert(evidenceLog.Contains("TECHNOLOGY_RESEARCHED"), "agency evidence log did not include evidence type");
            Assert(evidenceLog.Contains("basicRocketry"), "agency evidence log did not include evidence id");
        }

        private static void AgencyScienceEvidenceRecordsAuditLog()
        {
            string universe = CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Bob");

            SendAgencyEvidence(client, AgencyEvidenceType.SCIENCE_RECEIVED, "crewReport@KerbinSrfLandedLaunchPad");

            string evidenceFile = Path.Combine(universe, "AgencyEvidence", "Bob.log");
            Assert(File.Exists(evidenceFile), "science agency evidence did not create an audit log");
            string evidenceLog = File.ReadAllText(evidenceFile);
            Assert(evidenceLog.Contains("SCIENCE_RECEIVED"), "science evidence log did not include evidence type");
            Assert(evidenceLog.Contains("crewReport@KerbinSrfLandedLaunchPad"), "science evidence log did not include evidence id");
        }

        private static void AgencyVesselEvidenceRecordsAuditLog()
        {
            string universe = CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Carol");

            SendAgencyEvidence(client, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            string evidenceFile = Path.Combine(universe, "AgencyEvidence", "Carol.log");
            Assert(File.Exists(evidenceFile), "vessel agency evidence did not create an audit log");
            string evidenceLog = File.ReadAllText(evidenceFile);
            Assert(evidenceLog.Contains("VESSEL_ORBITED"), "vessel evidence log did not include evidence type");
            Assert(evidenceLog.Contains("orbit-Kerbin"), "vessel evidence log did not include evidence id");
        }

        private static void AgencyDockingEvidenceRecordsAuditLog()
        {
            string universe = CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Dana");

            SendAgencyEvidence(client, AgencyEvidenceType.VESSEL_DOCKED, "docked-Kerbin");

            string evidenceFile = Path.Combine(universe, "AgencyEvidence", "Dana.log");
            Assert(File.Exists(evidenceFile), "docking agency evidence did not create an audit log");
            string evidenceLog = File.ReadAllText(evidenceFile);
            Assert(evidenceLog.Contains("VESSEL_DOCKED"), "docking evidence log did not include evidence type");
            Assert(evidenceLog.Contains("docked-Kerbin"), "docking evidence log did not include evidence id");
        }

        private static void AgencyEvidenceQueryReturnsRecords()
        {
            CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Eve");

            SendAgencyEvidence(client, AgencyEvidenceType.SCIENCE_RECEIVED, "temperatureScan@MunInSpaceLow");

            AgencyEvidenceRecord[] playerRecords = AgencyProgression.GetEvidenceRecords("Eve");
            Assert(playerRecords.Length == 1, "player evidence query returned unexpected record count");
            Assert(playerRecords[0].playerName == "Eve", "player evidence query returned wrong player name");
            Assert(playerRecords[0].evidenceType == AgencyEvidenceType.SCIENCE_RECEIVED, "player evidence query returned wrong evidence type");
            Assert(playerRecords[0].evidenceId == "temperatureScan@MunInSpaceLow", "player evidence query returned wrong evidence id");

            AgencyEvidenceRecord[] matches = AgencyProgression.FindEvidence(AgencyEvidenceType.SCIENCE_RECEIVED, "temperatureScan@MunInSpaceLow");
            Assert(matches.Length == 1, "evidence search returned unexpected match count");
        }

        private static void AgencyEvidenceCompletesMatchingObjective()
        {
            string universe = CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"orbit-kerbin\",\"title\":\"Orbit Kerbin\",\"description\":\"Reach orbit.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\"}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Frank");

            AgencyProgression.Load(true);
            SendAgencyEvidence(client, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives.Length == 1, "matching objective test loaded unexpected objective count");
            Assert(objectives[0].status == "Complete", "matching evidence did not complete objective");
            Assert(objectives[0].completedBy == "Frank", "completed objective did not record completing player");
            Assert(File.Exists(Path.Combine(universe, "AgencyProgression", "Objectives.log")), "objective completion log was not written");
        }

        private static void AgencyObjectiveCompletionQueuesReward()
        {
            string universe = CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"science-test\",\"title\":\"Science Test\",\"description\":\"Do science.\",\"status\":\"Available\",\"scope\":\"Personal\",\"evidenceType\":\"SCIENCE_RECEIVED\",\"evidenceId\":\"crewReport@KerbinSrfLandedLaunchPad\",\"rewardFunds\":1000,\"rewardScience\":2.5,\"rewardReputation\":1}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Grace");

            AgencyProgression.Load(true);
            SendAgencyEvidence(client, AgencyEvidenceType.SCIENCE_RECEIVED, "crewReport@KerbinSrfLandedLaunchPad");

            Assert(File.Exists(Path.Combine(universe, "AgencyRewards", "Grace.log")), "agency reward audit log was not written");
            bool rewardQueued = false;
            ServerMessage message;
            while (client.sendMessageQueueHigh.TryDequeue(out message))
            {
                if (message.type == ServerMessageType.AGENCY_REWARD)
                {
                    rewardQueued = true;
                    break;
                }
            }
            Assert(rewardQueued, "agency reward message was not queued for completing player");
        }

        private static void AgencyEvidenceRejectsInvalidIds()
        {
            CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Alice");

            SendAgencyEvidence(client, AgencyEvidenceType.TECHNOLOGY_RESEARCHED, "../bad");

            AssertConnectionEndQueued(client);
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

        private static byte[] RepeatingBytes(int length)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(i % 251);
            }
            return bytes;
        }

        private static void SendAgencyEvidence(ClientObject client, AgencyEvidenceType evidenceType, string evidenceId)
        {
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)evidenceType);
                mw.Write<string>(evidenceId);
                mw.Write<double>(1234.5);
                DarkMultiPlayerServer.Messages.AgencyEvidence.HandleAgencyEvidence(client, mw.GetMessageBytes());
            }
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
