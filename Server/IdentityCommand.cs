using System;

namespace DarkMultiPlayerServer
{
    public class IdentityCommand
    {
        public static void HandleCommand(string commandArgs)
        {
            string func = commandArgs;
            string argument = string.Empty;
            if (commandArgs.Contains(" "))
            {
                func = commandArgs.Substring(0, commandArgs.IndexOf(" ", StringComparison.Ordinal));
                argument = commandArgs.Substring(func.Length + 1).Trim();
            }

            switch (func)
            {
                case "list":
                    ListIdentities();
                    break;
                case "show":
                    ShowIdentity(argument);
                    break;
                case "find":
                    FindIdentities(argument);
                    break;
                default:
                    DarkLog.Normal("Usage: /identity [list|show <uuid|name|fingerprint>|find <text>]");
                    break;
            }
        }

        private static void ListIdentities()
        {
            PlayerIdentityRecord[] records = PlayerIdentityStore.GetRecords();
            DarkLog.Normal("Player identity records: " + records.Length);
            int start = Math.Max(0, records.Length - 20);
            for (int i = start; i < records.Length; i++)
            {
                DarkLog.Normal(FormatIdentitySummary(records[i]));
            }
        }

        private static void ShowIdentity(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                DarkLog.Normal("Usage: /identity show <uuid|name|fingerprint>");
                return;
            }

            PlayerIdentityRecord[] records = PlayerIdentityStore.FindRecords(query);
            if (records.Length == 0)
            {
                DarkLog.Normal("No identity records matched '" + query + "'.");
                return;
            }

            DarkLog.Normal("Player identity matches: " + records.Length);
            foreach (PlayerIdentityRecord record in records)
            {
                DarkLog.Normal("UUID: " + record.uuid);
                DarkLog.Normal("Current name: " + record.currentName);
                DarkLog.Normal("Public key fingerprint: " + record.publicKeyFingerprint);
                DarkLog.Normal("First seen UTC: " + record.firstSeenUtc);
                DarkLog.Normal("Last seen UTC: " + record.lastSeenUtc);
                DarkLog.Normal("Previous names: " + (string.IsNullOrEmpty(record.previousNames) ? "(none)" : record.previousNames));
            }
        }

        private static void FindIdentities(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                DarkLog.Normal("Usage: /identity find <text>");
                return;
            }

            PlayerIdentityRecord[] records = PlayerIdentityStore.FindRecords(query);
            DarkLog.Normal("Player identity matches: " + records.Length);
            foreach (PlayerIdentityRecord record in records)
            {
                DarkLog.Normal(FormatIdentitySummary(record));
            }
        }

        private static string FormatIdentitySummary(PlayerIdentityRecord record)
        {
            return record.uuid + " " + record.currentName + " " + record.publicKeyFingerprint + " lastSeen=" + record.lastSeenUtc;
        }
    }
}
