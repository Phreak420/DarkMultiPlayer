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
            Run("Handshake UUID normalization validates input", HandshakeUuidNormalizationValidatesInput);
            Run("Handshake identity metadata records UUID", HandshakeIdentityMetadataRecordsUuid);
            Run("Identity store queries records", IdentityStoreQueriesRecords);
            Run("Identity store records audit events", IdentityStoreRecordsAuditEvents);
            Run("Identity store attaches key with confirmation", IdentityStoreAttachesKeyWithConfirmation);
            Run("Identity store renames identity with confirmation", IdentityStoreRenamesIdentityWithConfirmation);
            Run("Identity store revokes identity with confirmation", IdentityStoreRevokesIdentityWithConfirmation);
            Run("Message size validation rejects invalid lengths", MessageSizeValidationRejectsInvalidLengths);
            Run("Split message rejects oversized declared length", SplitMessageRejectsOversizedDeclaredLength);
            Run("Split message rejects oversized first chunk", SplitMessageRejectsOversizedFirstChunk);
            Run("Agency progression disabled clears objectives", AgencyProgressionDisabledClearsObjectives);
            Run("Agency progression enabled creates default objectives", AgencyProgressionEnabledCreatesDefaultObjectives);
            Run("Agency progression skips invalid objective IDs", AgencyProgressionSkipsInvalidObjectiveIds);
            Run("Agency objective contract metadata loads", AgencyObjectiveContractMetadataLoads);
            Run("Agency evidence disabled is ignored", AgencyEvidenceDisabledIsIgnored);
            Run("Agency evidence enabled records audit log", AgencyEvidenceEnabledRecordsAuditLog);
            Run("Agency science evidence records audit log", AgencyScienceEvidenceRecordsAuditLog);
            Run("Agency vessel evidence records audit log", AgencyVesselEvidenceRecordsAuditLog);
            Run("Agency docking evidence records audit log", AgencyDockingEvidenceRecordsAuditLog);
            Run("Agency expanded vessel evidence records audit log", AgencyExpandedVesselEvidenceRecordsAuditLog);
            Run("Agency admin evidence completes matching objective", AgencyAdminEvidenceCompletesMatchingObjective);
            Run("Agency contract evidence completes matching objective", AgencyContractEvidenceCompletesMatchingObjective);
            Run("Agency evidence query returns records", AgencyEvidenceQueryReturnsRecords);
            Run("Agency evidence completes matching objective", AgencyEvidenceCompletesMatchingObjective);
            Run("Agency prerequisites unlock objectives", AgencyPrerequisitesUnlockObjectives);
            Run("Agency any prerequisite mode unlocks objectives", AgencyAnyPrerequisiteModeUnlocksObjectives);
            Run("Agency hidden objectives appear after unlock", AgencyHiddenObjectivesAppearAfterUnlock);
            Run("Agency shared progress completes objective", AgencySharedProgressCompletesObjective);
            Run("Agency shared progress reloads and resets", AgencySharedProgressReloadsAndResets);
            Run("Agency unique contributors count once", AgencyUniqueContributorsCountOnce);
            Run("Agency personal objective state is per-player", AgencyPersonalObjectiveStateIsPerPlayer);
            Run("Agency objective completion queues reward", AgencyObjectiveCompletionQueuesReward);
            Run("Agency reward query returns records", AgencyRewardQueryReturnsRecords);
            Run("Agency reward replay records duplicate reward", AgencyRewardReplayRecordsDuplicateReward);
            Run("Agency reward revoke records negative reward", AgencyRewardRevokeRecordsNegativeReward);
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

        private static void HandshakeUuidNormalizationValidatesInput()
        {
            string uuid = Guid.NewGuid().ToString("N");
            Assert(DarkMultiPlayerServer.Messages.Handshake.TryNormalizePlayerUuid(uuid, out string normalizedUuid), "valid compact uuid was rejected");
            Assert(Guid.TryParse(normalizedUuid, out Guid _), "normalized uuid was not parseable");
            Assert(!DarkMultiPlayerServer.Messages.Handshake.TryNormalizePlayerUuid("../bad", out string _), "unsafe uuid value was accepted");
            Assert(!DarkMultiPlayerServer.Messages.Handshake.TryNormalizePlayerUuid("", out string _), "empty uuid value was accepted");
        }

        private static void HandshakeIdentityMetadataRecordsUuid()
        {
            string universe = CreateUniverse();
            string uuid = Guid.NewGuid().ToString();
            ClientObject client = CreateClient("Alice");
            client.playerUuid = uuid;
            client.publicKey = "alice-public-key";

            DarkMultiPlayerServer.Messages.Handshake.RecordPlayerIdentityMetadata(client);

            string identityFile = Path.Combine(universe, "Players", "Identities", uuid + ".txt");
            Assert(File.Exists(identityFile), "identity metadata file was not written");
            string identityMetadata = File.ReadAllText(identityFile);
            Assert(identityMetadata.Contains("uuid=" + uuid), "identity metadata did not include uuid");
            Assert(identityMetadata.Contains("currentName=Alice"), "identity metadata did not include current player name");
            Assert(identityMetadata.Contains("publicKeyFingerprint="), "identity metadata did not include public key fingerprint");
            Assert(identityMetadata.Contains("firstSeenUtc="), "identity metadata did not include first seen time");
            Assert(identityMetadata.Contains("lastSeenUtc="), "identity metadata did not include last seen time");

            ClientObject renamedClient = CreateClient("AliceRenamed");
            renamedClient.playerUuid = uuid;
            renamedClient.publicKey = "alice-public-key";
            DarkMultiPlayerServer.Messages.Handshake.RecordPlayerIdentityMetadata(renamedClient);

            identityMetadata = File.ReadAllText(identityFile);
            Assert(identityMetadata.Contains("currentName=AliceRenamed"), "identity metadata did not update current player name");
            Assert(identityMetadata.Contains("previousNames=Alice"), "identity metadata did not retain previous player name");
        }

        private static void IdentityStoreQueriesRecords()
        {
            CreateUniverse();
            string aliceUuid = Guid.NewGuid().ToString();
            string bobUuid = Guid.NewGuid().ToString();

            ClientObject alice = CreateClient("Alice");
            alice.playerUuid = aliceUuid;
            alice.publicKey = "alice-public-key";
            PlayerIdentityStore.Record(alice);

            ClientObject bob = CreateClient("Bob");
            bob.playerUuid = bobUuid;
            bob.publicKey = "bob-public-key";
            PlayerIdentityStore.Record(bob);

            PlayerIdentityRecord[] records = PlayerIdentityStore.GetRecords();
            Assert(records.Length == 2, "identity store returned wrong record count");

            PlayerIdentityRecord[] aliceMatches = PlayerIdentityStore.FindRecords("Alice");
            Assert(aliceMatches.Length == 1, "identity store did not find player by current name");
            Assert(aliceMatches[0].uuid == aliceUuid, "identity store returned wrong uuid for player name");

            PlayerIdentityRecord[] uuidMatches = PlayerIdentityStore.FindRecords(bobUuid.Substring(0, 8));
            Assert(uuidMatches.Length == 1, "identity store did not find player by partial uuid");
            Assert(uuidMatches[0].currentName == "Bob", "identity store returned wrong player for uuid search");
        }

        private static void IdentityStoreRecordsAuditEvents()
        {
            CreateUniverse();
            string uuid = Guid.NewGuid().ToString();

            ClientObject alice = CreateClient("Alice");
            alice.playerUuid = uuid;
            alice.publicKey = "alice-public-key";
            PlayerIdentityStore.Record(alice);

            ClientObject renamedAlice = CreateClient("AliceRenamed");
            renamedAlice.playerUuid = uuid;
            renamedAlice.publicKey = "alice-public-key";
            PlayerIdentityStore.Record(renamedAlice);

            PlayerIdentityAuditRecord[] records = PlayerIdentityStore.GetAuditRecords(uuid);
            Assert(records.Length == 2, "identity audit returned wrong record count");
            Assert(records[0].action == "created", "identity audit did not record creation");
            Assert(records[1].action == "name-changed", "identity audit did not record name change");
            Assert(records[1].details.Contains("previousName=Alice"), "identity audit did not include previous name");

            PlayerIdentityAuditRecord[] nameMatches = PlayerIdentityStore.GetAuditRecords("AliceRenamed");
            Assert(nameMatches.Length == 1, "identity audit did not filter by current name");
        }

        private static void IdentityStoreAttachesKeyWithConfirmation()
        {
            string universe = CreateUniverse();
            string uuid = Guid.NewGuid().ToString();

            ClientObject oldAlice = CreateClient("Alice");
            oldAlice.playerUuid = uuid;
            oldAlice.publicKey = "old-alice-public-key";
            PlayerIdentityStore.Record(oldAlice);

            string playerKeyFile = Path.Combine(universe, "Players", "Alice.txt");
            File.WriteAllText(playerKeyFile, oldAlice.publicKey);

            ClientObject recoveryClient = CreateClient("AliceRecovery");
            recoveryClient.playerUuid = Guid.NewGuid().ToString();
            recoveryClient.publicKey = "new-alice-public-key";

            PlayerIdentityRecoveryResult rejected = PlayerIdentityStore.AttachKeyToIdentity(uuid, recoveryClient, false);
            Assert(!rejected.success, "identity key attach succeeded without confirmation");
            Assert(File.ReadAllText(playerKeyFile) == oldAlice.publicKey, "identity key attach without confirmation changed the player key");

            PlayerIdentityRecoveryResult result = PlayerIdentityStore.AttachKeyToIdentity(uuid, recoveryClient, true);
            Assert(result.success, "identity key attach failed with confirmation: " + result.message);
            Assert(result.targetPlayerName == "Alice", "identity key attach targeted the wrong player name");
            Assert(File.ReadAllText(playerKeyFile) == recoveryClient.publicKey, "identity key attach did not update the player key file");
            Assert(Directory.GetFiles(Path.Combine(universe, "Players"), "Alice.recovery-*.bak").Length == 1, "identity key attach did not back up the previous key file");

            PlayerIdentityAuditRecord[] auditRecords = PlayerIdentityStore.GetAuditRecords("key-attached");
            Assert(auditRecords.Length == 1, "identity key attach did not write one audit event");
            Assert(auditRecords[0].uuid == uuid, "identity key attach audit recorded wrong uuid");
            Assert(auditRecords[0].details.Contains("sourcePlayer=AliceRecovery"), "identity key attach audit did not include source player");
        }

        private static void IdentityStoreRenamesIdentityWithConfirmation()
        {
            string universe = CreateUniverse();
            string uuid = Guid.NewGuid().ToString();

            ClientObject alice = CreateClient("Alice");
            alice.playerUuid = uuid;
            alice.publicKey = "alice-public-key";
            PlayerIdentityStore.Record(alice);

            string playersDirectory = Path.Combine(universe, "Players");
            string oldKeyFile = Path.Combine(playersDirectory, "Alice.txt");
            string newKeyFile = Path.Combine(playersDirectory, "AliceRenamed.txt");
            File.WriteAllText(oldKeyFile, alice.publicKey);

            PlayerIdentityRecoveryResult rejected = PlayerIdentityStore.RenameIdentity(uuid, "AliceRenamed", false);
            Assert(!rejected.success, "identity rename succeeded without confirmation");
            Assert(File.Exists(oldKeyFile), "identity rename without confirmation moved the player key file");

            PlayerIdentityRecoveryResult result = PlayerIdentityStore.RenameIdentity(uuid, "AliceRenamed", true);
            Assert(result.success, "identity rename failed with confirmation: " + result.message);
            Assert(!File.Exists(oldKeyFile), "identity rename left the old player key file in place");
            Assert(File.Exists(newKeyFile), "identity rename did not move the player key file");
            Assert(File.ReadAllText(newKeyFile) == alice.publicKey, "identity rename changed the player key content");

            PlayerIdentityRecord[] records = PlayerIdentityStore.FindRecords(uuid);
            Assert(records.Length == 1, "identity rename did not preserve one identity record");
            Assert(records[0].currentName == "AliceRenamed", "identity rename did not update current name");
            Assert(records[0].previousNames.Contains("Alice"), "identity rename did not keep previous name");

            PlayerIdentityAuditRecord[] auditRecords = PlayerIdentityStore.GetAuditRecords("renamed");
            Assert(auditRecords.Length == 1, "identity rename did not write one audit event");
            Assert(auditRecords[0].details.Contains("previousName=Alice"), "identity rename audit did not include previous name");
        }

        private static void IdentityStoreRevokesIdentityWithConfirmation()
        {
            string universe = CreateUniverse();
            string uuid = Guid.NewGuid().ToString();

            ClientObject alice = CreateClient("Alice");
            alice.playerUuid = uuid;
            alice.publicKey = "alice-public-key";
            PlayerIdentityStore.Record(alice);

            string playersDirectory = Path.Combine(universe, "Players");
            string playerKeyFile = Path.Combine(playersDirectory, "Alice.txt");
            File.WriteAllText(playerKeyFile, alice.publicKey);

            PlayerIdentityRecoveryResult rejected = PlayerIdentityStore.RevokeIdentity(uuid, "lost key", false);
            Assert(!rejected.success, "identity revoke succeeded without confirmation");
            Assert(File.Exists(playerKeyFile), "identity revoke without confirmation moved the player key file");

            PlayerIdentityRecoveryResult result = PlayerIdentityStore.RevokeIdentity(uuid, "lost key", true);
            Assert(result.success, "identity revoke failed with confirmation: " + result.message);
            Assert(!File.Exists(playerKeyFile), "identity revoke left the player key file in place");
            Assert(Directory.GetFiles(playersDirectory, "Alice.revoked-*.bak").Length == 1, "identity revoke did not back up the previous key file");

            PlayerIdentityRecord[] records = PlayerIdentityStore.FindRecords(uuid);
            Assert(records.Length == 1, "identity revoke did not preserve one identity record");
            Assert(!string.IsNullOrEmpty(records[0].revokedUtc), "identity revoke did not record revoked time");
            Assert(records[0].revokedReason == "lost key", "identity revoke did not record reason");

            PlayerIdentityAuditRecord[] auditRecords = PlayerIdentityStore.GetAuditRecords("revoked");
            Assert(auditRecords.Length == 1, "identity revoke did not write one audit event");
            Assert(auditRecords[0].details.Contains("reason=lost key"), "identity revoke audit did not include reason");
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

        private static void AgencyObjectiveContractMetadataLoads()
        {
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"contract-metadata\",\"title\":\"Metadata\",\"description\":\"Metadata test.\",\"status\":\"Available\",\"scope\":\"Server\",\"contractType\":\"Campaign\",\"issuer\":\"Mission Control\",\"rewardFunds\":25,\"rewardScience\":3,\"rewardReputation\":1}]}");

            AgencyProgression.Load(true);

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives.Length == 1, "contract metadata test loaded unexpected objective count");
            Assert(objectives[0].contractType == "Campaign", "contract type was not loaded");
            Assert(objectives[0].issuer == "Mission Control", "issuer was not loaded");
            Assert(objectives[0].rewardFunds == 25, "reward funds were not loaded");
            Assert(objectives[0].rewardScience == 3, "reward science was not loaded");
            Assert(objectives[0].rewardReputation == 1, "reward reputation was not loaded");
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

        private static void AgencyExpandedVesselEvidenceRecordsAuditLog()
        {
            string universe = CreateUniverse();
            Settings.settingsStore.agencyProgressionEnabled = true;
            SendAgencyEvidence(CreateClient("ErinLaunch"), AgencyEvidenceType.VESSEL_LAUNCHED, "launched-Kerbin");
            SendAgencyEvidence(CreateClient("ErinEscape"), AgencyEvidenceType.VESSEL_ESCAPED, "escaped-Kerbin");
            SendAgencyEvidence(CreateClient("ErinEncounter"), AgencyEvidenceType.VESSEL_ENCOUNTERED, "encountered-Mun");
            SendAgencyEvidence(CreateClient("ErinRecover"), AgencyEvidenceType.VESSEL_RECOVERED, "recovered-Kerbin");

            AgencyEvidenceRecord[] records = AgencyProgression.GetEvidenceRecords();
            Assert(records.Length == 4, "expanded vessel evidence query returned wrong record count");
            Assert(AgencyProgression.FindEvidence(AgencyEvidenceType.VESSEL_LAUNCHED, "launched-Kerbin").Length == 1, "launch evidence was not recorded");
            Assert(AgencyProgression.FindEvidence(AgencyEvidenceType.VESSEL_ESCAPED, "escaped-Kerbin").Length == 1, "escape evidence was not recorded");
            Assert(AgencyProgression.FindEvidence(AgencyEvidenceType.VESSEL_ENCOUNTERED, "encountered-Mun").Length == 1, "encounter evidence was not recorded");
            Assert(AgencyProgression.FindEvidence(AgencyEvidenceType.VESSEL_RECOVERED, "recovered-Kerbin").Length == 1, "recovery evidence was not recorded");
            Assert(File.Exists(Path.Combine(universe, "AgencyEvidence", "ErinLaunch.log")), "expanded vessel evidence log was not written");
        }

        private static void AgencyAdminEvidenceCompletesMatchingObjective()
        {
            string universe = CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"infrastructure-alpha\",\"title\":\"Infrastructure Alpha\",\"description\":\"Admin-confirmed infrastructure milestone.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"ADMIN_CONFIRMED\",\"evidenceId\":\"infrastructure-alpha\"}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;

            AgencyProgression.Load(true);
            Assert(AgencyProgression.RecordAdminEvidence("server", (int)AgencyEvidenceType.ADMIN_CONFIRMED, "infrastructure-alpha"), "admin evidence record failed");

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "Complete", "admin evidence did not complete matching objective");
            Assert(File.Exists(Path.Combine(universe, "AgencyEvidence", "server.log")), "admin evidence audit log was not written");
            Assert(File.Exists(Path.Combine(universe, "AgencyProgression", "Objectives.log")), "admin evidence completion log was not written");
        }

        private static void AgencyContractEvidenceCompletesMatchingObjective()
        {
            string universe = CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"first-contract\",\"title\":\"Complete First Contract\",\"description\":\"Complete a stock contract.\",\"status\":\"Available\",\"scope\":\"Personal\",\"evidenceType\":\"CONTRACT_COMPLETED\",\"evidenceId\":\"contract-WorldFirstContract\"}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject alice = CreateClient("Alice");

            AgencyProgression.Load(true);
            SendAgencyEvidence(alice, AgencyEvidenceType.CONTRACT_COMPLETED, "contract-WorldFirstContract");

            AgencyObjective[] objectives = AgencyProgression.GetObjectivesForPlayer("Alice");
            Assert(objectives[0].status == "Complete", "contract evidence did not complete matching objective");
            Assert(File.Exists(Path.Combine(universe, "AgencyEvidence", "Alice.log")), "contract evidence audit log was not written");
            Assert(File.Exists(Path.Combine(universe, "AgencyProgression", "Objectives.log")), "contract evidence completion log was not written");
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

        private static void AgencyPrerequisitesUnlockObjectives()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"orbit-kerbin\",\"title\":\"Orbit Kerbin\",\"description\":\"Reach orbit.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\"},{\"id\":\"land-mun\",\"title\":\"Land on Mun\",\"description\":\"Land after orbit.\",\"status\":\"Locked\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_LANDED\",\"evidenceId\":\"landed-Mun\",\"prerequisiteObjectiveIds\":[\"orbit-kerbin\"]}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject carol = CreateClient("Carol");
            ClientObject alice = CreateClient("Alice");
            ClientObject bob = CreateClient("Bob");

            AgencyProgression.Load(true);
            SendAgencyEvidence(carol, AgencyEvidenceType.VESSEL_LANDED, "landed-Mun");

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives[1].status == "Locked", "prerequisite objective was not locked before prerequisite completion");

            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");
            objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "Complete", "prerequisite objective did not complete");
            Assert(objectives[1].status == "Available", "dependent objective did not unlock after prerequisite completion");

            SendAgencyEvidence(bob, AgencyEvidenceType.VESSEL_LANDED, "landed-Mun");
            objectives = AgencyProgression.Objectives;
            Assert(objectives[1].status == "Complete", "dependent objective did not complete after unlocking");
        }

        private static void AgencyAnyPrerequisiteModeUnlocksObjectives()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"orbit-kerbin\",\"title\":\"Orbit Kerbin\",\"description\":\"Reach orbit.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\"},{\"id\":\"encounter-mun\",\"title\":\"Encounter Mun\",\"description\":\"Reach Mun SOI.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_ENCOUNTERED\",\"evidenceId\":\"encountered-Mun\"},{\"id\":\"choose-next-step\",\"title\":\"Choose Next Step\",\"description\":\"Unlock after either milestone.\",\"status\":\"Locked\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_LANDED\",\"evidenceId\":\"landed-Mun\",\"prerequisiteObjectiveIds\":[\"orbit-kerbin\",\"encounter-mun\"],\"prerequisiteMode\":\"Any\"}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject alice = CreateClient("Alice");

            AgencyProgression.Load(true);
            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives[2].status == "Locked", "any-mode objective was not locked before prerequisite completion");

            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ENCOUNTERED, "encountered-Mun");
            objectives = AgencyProgression.Objectives;
            Assert(objectives[1].status == "Complete", "any-mode prerequisite did not complete");
            Assert(objectives[2].status == "Available", "any-mode objective did not unlock after one prerequisite completion");
            Assert(objectives[2].prerequisiteMode == "Any", "any-mode objective did not preserve prerequisite mode");
        }

        private static void AgencyHiddenObjectivesAppearAfterUnlock()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"orbit-kerbin\",\"title\":\"Orbit Kerbin\",\"description\":\"Reach orbit.\",\"status\":\"Available\",\"scope\":\"Personal\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\"},{\"id\":\"secret-mun\",\"title\":\"Secret Mun Objective\",\"description\":\"Hidden until orbit.\",\"status\":\"Locked\",\"scope\":\"Personal\",\"evidenceType\":\"VESSEL_ENCOUNTERED\",\"evidenceId\":\"encountered-Mun\",\"prerequisiteObjectiveIds\":[\"orbit-kerbin\"],\"hiddenUntilAvailable\":true}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject alice = CreateClient("Alice");

            AgencyProgression.Load(true);
            AgencyObjective[] aliceObjectives = AgencyProgression.GetObjectivesForPlayer("Alice");
            Assert(aliceObjectives.Length == 1, "hidden locked objective was visible before unlock");
            Assert(aliceObjectives[0].id == "orbit-kerbin", "visible objective was not the prerequisite");
            Assert(AgencyProgression.Objectives.Length == 2, "admin objective list did not preserve hidden objective");

            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");
            aliceObjectives = AgencyProgression.GetObjectivesForPlayer("Alice");
            Assert(aliceObjectives.Length == 2, "hidden objective did not appear after unlock");
            Assert(aliceObjectives[1].id == "secret-mun", "unlocked hidden objective was not visible");
            Assert(aliceObjectives[1].status == "Available", "unlocked hidden objective was not available");
        }

        private static void AgencyPersonalObjectiveStateIsPerPlayer()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"personal-orbit\",\"title\":\"Personal Orbit\",\"description\":\"Reach orbit.\",\"status\":\"Available\",\"scope\":\"Personal\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\"}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject alice = CreateClient("Alice");

            AgencyProgression.Load(true);
            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            AgencyObjective[] aliceObjectives = AgencyProgression.GetObjectivesForPlayer("Alice");
            AgencyObjective[] bobObjectives = AgencyProgression.GetObjectivesForPlayer("Bob");
            Assert(aliceObjectives[0].status == "Complete", "personal objective did not complete for matching player");
            Assert(bobObjectives[0].status == "Available", "personal objective completed for another player");
        }

        private static void AgencySharedProgressCompletesObjective()
        {
            string universe = CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"relay-network\",\"title\":\"Build Relay Network\",\"description\":\"Contribute relay evidence.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\",\"progressTarget\":2,\"progressPerEvidence\":1,\"rewardFunds\":500}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject alice = CreateClient("Alice");
            ClientObject bob = CreateClient("Bob");

            AgencyProgression.Load(true);
            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "In Progress 1/2", "shared progress objective did not show partial progress");
            Assert(File.Exists(Path.Combine(universe, "AgencyProgression", "Progress.log")), "shared progress log was not written");
            Assert(!File.Exists(Path.Combine(universe, "AgencyProgression", "Objectives.log")), "partial shared progress wrote a completion log too early");
            Assert(!File.Exists(Path.Combine(universe, "AgencyRewards", "Alice.log")), "partial shared progress granted a reward too early");

            SendAgencyEvidence(bob, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "Complete", "shared progress objective did not complete at target");
            Assert(File.Exists(Path.Combine(universe, "AgencyRewards", "Bob.log")), "shared progress completion did not reward completing player");
            Assert(File.Exists(Path.Combine(universe, "AgencyProgression", "Objectives.log")), "shared progress completion log was not written");
        }

        private static void AgencySharedProgressReloadsAndResets()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"relay-network\",\"title\":\"Build Relay Network\",\"description\":\"Contribute relay evidence.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\",\"progressTarget\":3,\"progressPerEvidence\":1}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject alice = CreateClient("Alice");

            AgencyProgression.Load(true);
            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "In Progress 1/3", "shared progress objective did not record partial progress");

            AgencyProgression.Load(true);
            objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "In Progress 1/3", "shared progress did not reload from disk");
            Assert(AgencyProgression.GetProgressRecords().Length == 1, "shared progress query returned wrong record count after reload");

            Assert(AgencyProgression.ResetProgress(string.Empty, "relay-network"), "shared progress reset failed");
            objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "Available", "shared progress reset did not restore available status");
            Assert(AgencyProgression.GetProgressRecords().Length == 0, "shared progress reset did not remove progress record");
        }

        private static void AgencyUniqueContributorsCountOnce()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"relay-network\",\"title\":\"Build Relay Network\",\"description\":\"Contribute relay evidence.\",\"status\":\"Available\",\"scope\":\"Server\",\"evidenceType\":\"VESSEL_ORBITED\",\"evidenceId\":\"orbit-Kerbin\",\"progressTarget\":2,\"progressPerEvidence\":1,\"uniqueContributors\":true}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject alice = CreateClient("Alice");
            ClientObject bob = CreateClient("Bob");

            AgencyProgression.Load(true);
            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            AgencyProgression.Load(true);
            SendAgencyEvidence(alice, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "In Progress 1/2", "duplicate unique contributor advanced shared progress");

            SendAgencyEvidence(bob, AgencyEvidenceType.VESSEL_ORBITED, "orbit-Kerbin");

            objectives = AgencyProgression.Objectives;
            Assert(objectives[0].status == "Complete", "second unique contributor did not complete shared progress");
        }

        private static void AgencyRewardQueryReturnsRecords()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"reward-query\",\"title\":\"Reward Query\",\"description\":\"Do science.\",\"status\":\"Available\",\"scope\":\"Personal\",\"evidenceType\":\"SCIENCE_RECEIVED\",\"evidenceId\":\"mysteryGoo@KerbinSrfLandedLaunchPad\",\"rewardFunds\":123,\"rewardScience\":4,\"rewardReputation\":5}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Heidi");

            AgencyProgression.Load(true);
            SendAgencyEvidence(client, AgencyEvidenceType.SCIENCE_RECEIVED, "mysteryGoo@KerbinSrfLandedLaunchPad");

            AgencyRewardRecord[] rewardRecords = AgencyProgression.GetRewardRecords("Heidi");
            Assert(rewardRecords.Length == 1, "reward query returned unexpected record count");
            Assert(rewardRecords[0].objectiveId == "reward-query", "reward query returned wrong objective id");
            Assert(rewardRecords[0].funds == 123, "reward query returned wrong funds");
            Assert(rewardRecords[0].science == 4, "reward query returned wrong science");
            Assert(rewardRecords[0].reputation == 5, "reward query returned wrong reputation");
        }

        private static void AgencyRewardReplayRecordsDuplicateReward()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"replay-test\",\"title\":\"Replay Test\",\"description\":\"Do science.\",\"status\":\"Available\",\"scope\":\"Personal\",\"evidenceType\":\"SCIENCE_RECEIVED\",\"evidenceId\":\"crewReport@KerbinSrfLandedLaunchPad\",\"rewardFunds\":100,\"rewardScience\":2,\"rewardReputation\":3}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;
            ClientObject client = CreateClient("Ivan");

            AgencyProgression.Load(true);
            SendAgencyEvidence(client, AgencyEvidenceType.SCIENCE_RECEIVED, "crewReport@KerbinSrfLandedLaunchPad");

            Assert(AgencyProgression.ReplayReward("Ivan", "replay-test"), "reward replay failed");
            AgencyRewardRecord[] rewardRecords = AgencyProgression.GetRewardRecords("Ivan");
            Assert(rewardRecords.Length == 2, "reward replay did not record a second reward event");
            Assert(rewardRecords[1].funds == 100 && rewardRecords[1].science == 2 && rewardRecords[1].reputation == 3, "reward replay recorded wrong values");
        }

        private static void AgencyRewardRevokeRecordsNegativeReward()
        {
            CreateUniverse();
            Server.configDirectory = Path.Combine(Path.GetTempPath(), "dmp-validation-agency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Server.configDirectory);
            File.WriteAllText(
                Path.Combine(Server.configDirectory, "AgencyProgression.json"),
                "{\"packName\":\"Test Pack\",\"objectives\":[{\"id\":\"revoke-test\",\"title\":\"Revoke Test\",\"description\":\"Do science.\",\"status\":\"Available\",\"scope\":\"Personal\",\"evidenceType\":\"SCIENCE_RECEIVED\",\"evidenceId\":\"mysteryGoo@KerbinSrfLandedLaunchPad\",\"rewardFunds\":150,\"rewardScience\":4,\"rewardReputation\":5}]}");
            Settings.settingsStore.agencyProgressionEnabled = true;

            AgencyProgression.Load(true);

            Assert(AgencyProgression.RevokeReward("Judy", "revoke-test"), "reward revoke failed");
            AgencyRewardRecord[] rewardRecords = AgencyProgression.GetRewardRecords("Judy");
            Assert(rewardRecords.Length == 1, "reward revoke did not record one reward event");
            Assert(rewardRecords[0].funds == -150 && rewardRecords[0].science == -4 && rewardRecords[0].reputation == -5, "reward revoke did not record negative values");
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
            Directory.CreateDirectory(Path.Combine(universe, "Players"));
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
