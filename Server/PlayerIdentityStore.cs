using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer
{
    public class PlayerIdentityRecord
    {
        public string uuid;
        public string currentName;
        public string publicKeyFingerprint;
        public string firstSeenUtc;
        public string lastSeenUtc;
        public string previousNames;
    }

    public class PlayerIdentityAuditRecord
    {
        public DateTime recordedAtUtc;
        public string action;
        public string uuid;
        public string playerName;
        public string publicKeyFingerprint;
        public string details;
    }

    public class PlayerIdentityRecoveryResult
    {
        public bool success;
        public string message;
        public string targetPlayerName;
        public string attachedFingerprint;
    }

    public static class PlayerIdentityStore
    {
        public static bool TryNormalizePlayerUuid(string playerUuid, out string normalizedPlayerUuid)
        {
            normalizedPlayerUuid = "";
            Guid parsedUuid;
            if (string.IsNullOrEmpty(playerUuid) || !Guid.TryParse(playerUuid, out parsedUuid))
            {
                return false;
            }
            normalizedPlayerUuid = parsedUuid.ToString();
            return true;
        }

        public static void Record(ClientObject client)
        {
            if (client == null || string.IsNullOrEmpty(client.playerUuid))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(GetIdentityDirectory());
                string identityFile = GetIdentityFile(client.playerUuid);
                Dictionary<string, string> metadata = ReadIdentityMetadata(identityFile);
                string now = DateTime.UtcNow.ToString("o");
                string previousNames = metadata.ContainsKey("previousNames") ? metadata["previousNames"] : "";
                string currentName = metadata.ContainsKey("currentName") ? metadata["currentName"] : "";
                string currentFingerprint = metadata.ContainsKey("publicKeyFingerprint") ? metadata["publicKeyFingerprint"] : "";
                string nextFingerprint = GetPublicKeyFingerprint(client.publicKey);
                bool isNewRecord = string.IsNullOrEmpty(currentName);
                bool nameChanged = !string.IsNullOrEmpty(currentName) && currentName != client.playerName;
                bool fingerprintChanged = !string.IsNullOrEmpty(currentFingerprint) && currentFingerprint != nextFingerprint;
                if (!string.IsNullOrEmpty(currentName) && currentName != client.playerName && !ContainsMetadataValue(previousNames, currentName))
                {
                    previousNames = string.IsNullOrEmpty(previousNames) ? currentName : previousNames + ";" + currentName;
                }

                metadata["uuid"] = client.playerUuid;
                metadata["currentName"] = SanitizeMetadataValue(client.playerName);
                metadata["publicKeyFingerprint"] = nextFingerprint;
                if (!metadata.ContainsKey("firstSeenUtc") || string.IsNullOrEmpty(metadata["firstSeenUtc"]))
                {
                    metadata["firstSeenUtc"] = now;
                }
                metadata["lastSeenUtc"] = now;
                metadata["previousNames"] = SanitizeMetadataValue(previousNames);

                WriteIdentityMetadata(identityFile, metadata);
                if (isNewRecord)
                {
                    RecordAudit("created", client.playerUuid, client.playerName, nextFingerprint, "identity metadata created");
                }
                if (nameChanged)
                {
                    RecordAudit("name-changed", client.playerUuid, client.playerName, nextFingerprint, "previousName=" + currentName);
                }
                if (fingerprintChanged)
                {
                    RecordAudit("fingerprint-changed", client.playerUuid, client.playerName, nextFingerprint, "previousFingerprint=" + currentFingerprint);
                }
            }
            catch (Exception e)
            {
                DarkLog.Debug("Failed to record player identity metadata for " + client.playerName + ": " + e);
            }
        }

        public static PlayerIdentityRecord[] GetRecords()
        {
            string identitiesDirectory = GetIdentityDirectory();
            if (!Directory.Exists(identitiesDirectory))
            {
                return new PlayerIdentityRecord[0];
            }

            List<PlayerIdentityRecord> records = new List<PlayerIdentityRecord>();
            foreach (string identityFile in Directory.GetFiles(identitiesDirectory, "*.txt"))
            {
                PlayerIdentityRecord record = ReadRecord(identityFile);
                if (!string.IsNullOrEmpty(record.uuid))
                {
                    records.Add(record);
                }
            }
            records.Sort((a, b) => string.Compare(a.currentName, b.currentName, StringComparison.OrdinalIgnoreCase));
            return records.ToArray();
        }

        public static PlayerIdentityRecord[] FindRecords(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return GetRecords();
            }

            List<PlayerIdentityRecord> matches = new List<PlayerIdentityRecord>();
            foreach (PlayerIdentityRecord record in GetRecords())
            {
                if (Matches(record.uuid, query) || Matches(record.currentName, query) || Matches(record.publicKeyFingerprint, query) || Matches(record.previousNames, query))
                {
                    matches.Add(record);
                }
            }
            return matches.ToArray();
        }

        public static PlayerIdentityRecoveryResult AttachKeyToIdentity(string uuid, ClientObject sourceClient, bool confirmed)
        {
            PlayerIdentityRecoveryResult result = new PlayerIdentityRecoveryResult();
            if (!confirmed)
            {
                result.message = "Confirmation required. Use: /identity attachkey <uuid> <onlinePlayerName> confirm";
                return result;
            }
            if (sourceClient == null || !sourceClient.authenticated || string.IsNullOrEmpty(sourceClient.publicKey))
            {
                result.message = "Source player must be online and authenticated.";
                return result;
            }
            string normalizedUuid;
            if (!TryNormalizePlayerUuid(uuid, out normalizedUuid))
            {
                result.message = "Invalid UUID.";
                return result;
            }

            PlayerIdentityRecord[] records = FindRecords(normalizedUuid);
            PlayerIdentityRecord targetRecord = null;
            foreach (PlayerIdentityRecord record in records)
            {
                if (record.uuid == normalizedUuid)
                {
                    targetRecord = record;
                    break;
                }
            }
            if (targetRecord == null)
            {
                result.message = "Identity UUID was not found.";
                return result;
            }
            if (string.IsNullOrEmpty(targetRecord.currentName) || !SafeFile.IsNameSafe(targetRecord.currentName))
            {
                result.message = "Identity current name is missing or unsafe.";
                return result;
            }

            try
            {
                string playersDirectory = Path.Combine(Server.universeDirectory, "Players");
                Directory.CreateDirectory(playersDirectory);
                string playerKeyFile = Path.Combine(playersDirectory, targetRecord.currentName + ".txt");
                string previousFingerprint = "";
                if (File.Exists(playerKeyFile))
                {
                    previousFingerprint = GetPublicKeyFingerprint(File.ReadAllText(playerKeyFile));
                    string backupKeyFile = Path.Combine(playersDirectory, targetRecord.currentName + ".recovery-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak");
                    File.Copy(playerKeyFile, backupKeyFile, false);
                }
                File.WriteAllText(playerKeyFile, sourceClient.publicKey);

                string attachedFingerprint = GetPublicKeyFingerprint(sourceClient.publicKey);
                RecordAudit(
                    "key-attached",
                    normalizedUuid,
                    targetRecord.currentName,
                    attachedFingerprint,
                    "sourcePlayer=" + sourceClient.playerName + ";previousFingerprint=" + previousFingerprint);

                result.success = true;
                result.targetPlayerName = targetRecord.currentName;
                result.attachedFingerprint = attachedFingerprint;
                result.message = "Attached key from online player '" + sourceClient.playerName + "' to identity '" + targetRecord.currentName + "'.";
                return result;
            }
            catch (Exception e)
            {
                result.message = "Failed to attach key: " + e.Message;
                return result;
            }
        }

        public static PlayerIdentityAuditRecord[] GetAuditRecords(string query)
        {
            string auditFile = GetAuditFile();
            if (!File.Exists(auditFile))
            {
                return new PlayerIdentityAuditRecord[0];
            }

            List<PlayerIdentityAuditRecord> records = new List<PlayerIdentityAuditRecord>();
            foreach (string line in File.ReadAllLines(auditFile))
            {
                PlayerIdentityAuditRecord record;
                if (TryParseAuditRecord(line, out record) && (string.IsNullOrEmpty(query) || MatchesAuditRecord(record, query)))
                {
                    records.Add(record);
                }
            }
            return records.ToArray();
        }

        public static void RecordAudit(string action, string uuid, string playerName, string publicKeyFingerprint, string details)
        {
            try
            {
                Directory.CreateDirectory(GetIdentityDirectory());
                using (StreamWriter sw = new StreamWriter(GetAuditFile(), true))
                {
                    sw.WriteLine(
                        DateTime.UtcNow.ToString("o") + "|" +
                        SanitizeAuditValue(action) + "|" +
                        SanitizeAuditValue(uuid) + "|" +
                        SanitizeAuditValue(playerName) + "|" +
                        SanitizeAuditValue(publicKeyFingerprint) + "|" +
                        SanitizeAuditValue(details));
                }
            }
            catch (Exception e)
            {
                DarkLog.Debug("Failed to record identity audit entry: " + e);
            }
        }

        private static PlayerIdentityRecord ReadRecord(string identityFile)
        {
            Dictionary<string, string> metadata = ReadIdentityMetadata(identityFile);
            PlayerIdentityRecord record = new PlayerIdentityRecord();
            record.uuid = GetMetadataValue(metadata, "uuid");
            record.currentName = GetMetadataValue(metadata, "currentName");
            record.publicKeyFingerprint = GetMetadataValue(metadata, "publicKeyFingerprint");
            record.firstSeenUtc = GetMetadataValue(metadata, "firstSeenUtc");
            record.lastSeenUtc = GetMetadataValue(metadata, "lastSeenUtc");
            record.previousNames = GetMetadataValue(metadata, "previousNames");
            return record;
        }

        private static string GetIdentityDirectory()
        {
            return Path.Combine(Path.Combine(Server.universeDirectory, "Players"), "Identities");
        }

        private static string GetIdentityFile(string playerUuid)
        {
            return Path.Combine(GetIdentityDirectory(), playerUuid + ".txt");
        }

        private static string GetAuditFile()
        {
            return Path.Combine(GetIdentityDirectory(), "IdentityAudit.log");
        }

        private static Dictionary<string, string> ReadIdentityMetadata(string identityFile)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();
            if (!File.Exists(identityFile))
            {
                return metadata;
            }

            foreach (string line in File.ReadAllLines(identityFile))
            {
                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }
                string key = line.Substring(0, separatorIndex);
                string value = line.Substring(separatorIndex + 1);
                metadata[key] = value;
            }
            return metadata;
        }

        private static void WriteIdentityMetadata(string identityFile, Dictionary<string, string> metadata)
        {
            string[] orderedKeys = new string[] { "uuid", "currentName", "publicKeyFingerprint", "firstSeenUtc", "lastSeenUtc", "previousNames" };
            using (StreamWriter sw = new StreamWriter(identityFile))
            {
                foreach (string key in orderedKeys)
                {
                    if (metadata.ContainsKey(key))
                    {
                        sw.WriteLine(key + "=" + metadata[key]);
                    }
                }
            }
        }

        private static string GetPublicKeyFingerprint(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
            {
                return "";
            }
            string hash = Common.CalculateSHA256Hash(Encoding.UTF8.GetBytes(publicKey));
            if (string.IsNullOrEmpty(hash) || hash.Length < 16)
            {
                return "";
            }
            return hash.Substring(0, 4) + "-" + hash.Substring(4, 4) + "-" + hash.Substring(8, 4) + "-" + hash.Substring(12, 4);
        }

        private static bool ContainsMetadataValue(string values, string value)
        {
            if (string.IsNullOrEmpty(values) || string.IsNullOrEmpty(value))
            {
                return false;
            }
            string[] splitValues = values.Split(';');
            foreach (string splitValue in splitValues)
            {
                if (splitValue == value)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Matches(string value, string query)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool MatchesAuditRecord(PlayerIdentityAuditRecord record, string query)
        {
            return Matches(record.action, query) || Matches(record.uuid, query) || Matches(record.playerName, query) || Matches(record.publicKeyFingerprint, query) || Matches(record.details, query);
        }

        private static bool TryParseAuditRecord(string line, out PlayerIdentityAuditRecord record)
        {
            record = new PlayerIdentityAuditRecord();
            string[] parts = line.Split('|');
            DateTime recordedAtUtc;
            if (parts.Length != 6 || !DateTime.TryParse(parts[0], out recordedAtUtc))
            {
                return false;
            }
            record.recordedAtUtc = recordedAtUtc;
            record.action = parts[1];
            record.uuid = parts[2];
            record.playerName = parts[3];
            record.publicKeyFingerprint = parts[4];
            record.details = parts[5];
            return true;
        }

        private static string GetMetadataValue(Dictionary<string, string> metadata, string key)
        {
            return metadata.ContainsKey(key) ? metadata[key] : "";
        }

        private static string SanitizeMetadataValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            return value.Replace("\r", "").Replace("\n", "").Replace(";", "");
        }

        private static string SanitizeAuditValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            return value.Replace("\r", "").Replace("\n", "").Replace("|", "");
        }
    }
}
