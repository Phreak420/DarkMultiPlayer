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
                if (!string.IsNullOrEmpty(currentName) && currentName != client.playerName && !ContainsMetadataValue(previousNames, currentName))
                {
                    previousNames = string.IsNullOrEmpty(previousNames) ? currentName : previousNames + ";" + currentName;
                }

                metadata["uuid"] = client.playerUuid;
                metadata["currentName"] = SanitizeMetadataValue(client.playerName);
                metadata["publicKeyFingerprint"] = GetPublicKeyFingerprint(client.publicKey);
                if (!metadata.ContainsKey("firstSeenUtc") || string.IsNullOrEmpty(metadata["firstSeenUtc"]))
                {
                    metadata["firstSeenUtc"] = now;
                }
                metadata["lastSeenUtc"] = now;
                metadata["previousNames"] = SanitizeMetadataValue(previousNames);

                WriteIdentityMetadata(identityFile, metadata);
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
    }
}
