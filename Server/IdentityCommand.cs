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
                case "audit":
                    ShowAudit(argument);
                    break;
                case "attachkey":
                    AttachKey(argument);
                    break;
                case "rename":
                    RenameIdentity(argument);
                    break;
                case "revoke":
                    RevokeIdentity(argument);
                    break;
                default:
                    DarkLog.Normal("Usage: /identity [list|show <uuid|name|fingerprint>|find <text>|audit [uuid|name|fingerprint]|attachkey <uuid> <onlinePlayerName> confirm|rename <uuid> <newPlayerName> confirm|revoke <uuid> <reason> confirm]");
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
                if (!string.IsNullOrEmpty(record.revokedUtc))
                {
                    DarkLog.Normal("Revoked UTC: " + record.revokedUtc);
                    DarkLog.Normal("Revoked reason: " + record.revokedReason);
                }
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

        private static void ShowAudit(string query)
        {
            PlayerIdentityAuditRecord[] records = PlayerIdentityStore.GetAuditRecords(query);
            DarkLog.Normal("Player identity audit records: " + records.Length);
            int start = Math.Max(0, records.Length - 20);
            for (int i = start; i < records.Length; i++)
            {
                PlayerIdentityAuditRecord record = records[i];
                DarkLog.Normal(record.recordedAtUtc.ToString("u") + " " + record.action + " " + record.uuid + " " + record.playerName + " " + record.publicKeyFingerprint + " " + record.details);
            }
        }

        private static void AttachKey(string argument)
        {
            string[] args = argument.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length != 3 || args[2] != "confirm")
            {
                DarkLog.Normal("Usage: /identity attachkey <uuid> <onlinePlayerName> confirm");
                return;
            }

            ClientObject sourceClient = ClientHandler.GetClientByName(args[1]);
            PlayerIdentityRecoveryResult result = PlayerIdentityStore.AttachKeyToIdentity(args[0], sourceClient, true);
            if (result.success)
            {
                DarkLog.Normal(result.message);
                DarkLog.Normal("Target player: " + result.targetPlayerName + ", attached fingerprint: " + result.attachedFingerprint);
            }
            else
            {
                DarkLog.Normal(result.message);
            }
        }

        private static void RenameIdentity(string argument)
        {
            string[] args = argument.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length != 3 || args[2] != "confirm")
            {
                DarkLog.Normal("Usage: /identity rename <uuid> <newPlayerName> confirm");
                return;
            }

            PlayerIdentityRecoveryResult result = PlayerIdentityStore.RenameIdentity(args[0], args[1], true);
            DarkLog.Normal(result.message);
        }

        private static void RevokeIdentity(string argument)
        {
            string[] args = argument.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length < 3 || args[args.Length - 1] != "confirm")
            {
                DarkLog.Normal("Usage: /identity revoke <uuid> <reason> confirm");
                return;
            }

            string reason = string.Join(" ", args, 1, args.Length - 2);
            PlayerIdentityRecoveryResult result = PlayerIdentityStore.RevokeIdentity(args[0], reason, true);
            DarkLog.Normal(result.message);
        }

        private static string FormatIdentitySummary(PlayerIdentityRecord record)
        {
            string revoked = string.IsNullOrEmpty(record.revokedUtc) ? "" : " revoked=" + record.revokedUtc;
            return record.uuid + " " + record.currentName + " " + record.publicKeyFingerprint + " lastSeen=" + record.lastSeenUtc + revoked;
        }
    }
}
