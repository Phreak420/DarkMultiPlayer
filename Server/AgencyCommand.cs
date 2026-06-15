using System;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer
{
    public class AgencyCommand
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
                case "status":
                    ShowStatus();
                    break;
                case "reload":
                    Reload();
                    break;
                case "objectives":
                    ShowObjectives();
                    break;
                case "evidence":
                    ShowEvidence(argument);
                    break;
                case "rewards":
                    ShowRewards(argument);
                    break;
                case "progress":
                    ShowProgress(argument);
                    break;
                case "contributions":
                    ShowContributions(argument);
                    break;
                case "resetprogress":
                    ResetProgress(argument);
                    break;
                case "record":
                    RecordEvidence(argument);
                    break;
                case "replay":
                    ReplayReward(argument);
                    break;
                case "revoke":
                    RevokeReward(argument);
                    break;
                default:
                    DarkLog.Normal("Usage: /agency [status|reload|objectives|evidence [player]|rewards [player]|progress [player]|contributions <objective>|resetprogress <player|server> <objective>|record <player|server> <evidenceType> <evidenceId>|replay <player> <objective>|revoke <player> <objective>]");
                    break;
            }
        }

        private static void ShowStatus()
        {
            if (!Settings.IsAgencyProgressionActive())
            {
                DarkLog.Normal("Agency progression is disabled.");
                return;
            }

            AgencyObjective[] objectives = AgencyProgression.Objectives;
            int complete = 0;
            foreach (AgencyObjective objective in objectives)
            {
                if (objective.status == "Complete")
                {
                    complete++;
                }
            }
            DarkLog.Normal("Agency pack: " + AgencyProgression.PackName);
            DarkLog.Normal("Objectives: " + complete + "/" + objectives.Length + " complete");
            DarkLog.Normal("Evidence records: " + AgencyProgression.GetEvidenceRecords().Length);
            DarkLog.Normal("Progress records: " + AgencyProgression.GetProgressRecords().Length);
            DarkLog.Normal("Reward records: " + AgencyProgression.GetRewardRecords().Length);
        }

        private static void Reload()
        {
            AgencyProgression.Load(Settings.IsAgencyProgressionActive());
            CampaignState.Load(Settings.IsAgencyProgressionActive());
            EconomyState.Load(Settings.IsAgencyProgressionActive());
            DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            DarkLog.Normal("Agency progression reloaded.");
        }

        private static void ShowObjectives()
        {
            foreach (AgencyObjective objective in AgencyProgression.Objectives)
            {
                DarkLog.Normal(objective.id + " [" + objective.status + "] " + objective.title);
            }
        }

        private static void ShowEvidence(string playerName)
        {
            AgencyEvidenceRecord[] records = string.IsNullOrEmpty(playerName) ? AgencyProgression.GetEvidenceRecords() : AgencyProgression.GetEvidenceRecords(playerName);
            DarkLog.Normal("Agency evidence records: " + records.Length);
            int start = Math.Max(0, records.Length - 10);
            for (int i = start; i < records.Length; i++)
            {
                AgencyEvidenceRecord record = records[i];
                DarkLog.Normal(record.receivedAtUtc.ToString("u") + " " + record.playerName + " " + record.evidenceType + " " + record.evidenceId);
            }
        }

        private static void ShowRewards(string playerName)
        {
            AgencyRewardRecord[] records = string.IsNullOrEmpty(playerName) ? AgencyProgression.GetRewardRecords() : AgencyProgression.GetRewardRecords(playerName);
            DarkLog.Normal("Agency reward records: " + records.Length);
            int start = Math.Max(0, records.Length - 10);
            for (int i = start; i < records.Length; i++)
            {
                AgencyRewardRecord record = records[i];
                DarkLog.Normal(record.awardedAtUtc.ToString("u") + " " + record.playerName + " " + record.objectiveId + " funds=" + record.funds + " science=" + record.science + " reputation=" + record.reputation);
            }
        }

        private static void ShowProgress(string playerName)
        {
            if (string.Equals(playerName, "server", StringComparison.OrdinalIgnoreCase))
            {
                playerName = string.Empty;
            }
            AgencyObjectiveProgress[] records = string.IsNullOrEmpty(playerName) ? AgencyProgression.GetProgressRecords() : AgencyProgression.GetProgressRecords(playerName);
            DarkLog.Normal("Agency progress records: " + records.Length);
            int start = Math.Max(0, records.Length - 10);
            for (int i = start; i < records.Length; i++)
            {
                AgencyObjectiveProgress record = records[i];
                string owner = string.IsNullOrEmpty(record.playerName) ? "server" : record.playerName;
                DarkLog.Normal(record.updatedAtUtc + " " + owner + " " + record.objectiveId + " progress=" + record.progressValue + " lastBy=" + record.lastContributedBy + " contributors=" + record.contributedBy);
            }
        }

        private static void ShowContributions(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId) || !SafeFile.IsNameSafe(objectiveId))
            {
                DarkLog.Normal("Usage: /agency contributions <objective>");
                return;
            }

            AgencyObjectiveProgress[] records = AgencyProgression.GetProgressRecordsForObjective(objectiveId);
            DarkLog.Normal("Agency contribution records for " + objectiveId + ": " + records.Length);
            for (int i = 0; i < records.Length; i++)
            {
                AgencyObjectiveProgress record = records[i];
                string owner = string.IsNullOrEmpty(record.playerName) ? "server" : record.playerName;
                string contributors = string.IsNullOrEmpty(record.contributedBy) ? "none" : record.contributedBy;
                DarkLog.Normal(record.updatedAtUtc + " owner=" + owner + " progress=" + record.progressValue + " lastBy=" + record.lastContributedBy + " contributors=" + contributors);
            }
        }

        private static void ResetProgress(string argument)
        {
            string playerName;
            string objectiveId;
            if (!TryReadPlayerObjective(argument, out playerName, out objectiveId))
            {
                DarkLog.Normal("Usage: /agency resetprogress <player|server> <objective>");
                return;
            }
            if (string.Equals(playerName, "server", StringComparison.OrdinalIgnoreCase))
            {
                playerName = string.Empty;
            }

            if (AgencyProgression.ResetProgress(playerName, objectiveId))
            {
                DarkLog.Normal("Reset agency progress for " + (string.IsNullOrEmpty(playerName) ? "server" : playerName) + " objective " + objectiveId + ".");
            }
            else
            {
                DarkLog.Normal("Agency progress reset failed. Check that the objective is valid, incomplete, and has progress.");
            }
        }

        private static void RecordEvidence(string argument)
        {
            string playerName;
            string evidenceTypeName;
            string evidenceId;
            if (!TryReadRecordEvidence(argument, out playerName, out evidenceTypeName, out evidenceId))
            {
                DarkLog.Normal("Usage: /agency record <player|server> <evidenceType> <evidenceId>");
                return;
            }

            AgencyEvidenceType evidenceType;
            if (!Enum.TryParse(evidenceTypeName, true, out evidenceType))
            {
                DarkLog.Normal("Unknown agency evidence type '" + evidenceTypeName + "'.");
                return;
            }

            if (AgencyProgression.RecordAdminEvidence(playerName, (int)evidenceType, evidenceId))
            {
                DarkLog.Normal("Recorded agency evidence " + evidenceType + ":" + evidenceId + " for " + playerName + ".");
            }
            else
            {
                DarkLog.Normal("Agency evidence record failed. Check that agency progression is enabled and player/evidence IDs are safe.");
            }
        }

        private static void ReplayReward(string argument)
        {
            string playerName;
            string objectiveId;
            if (!TryReadPlayerObjective(argument, out playerName, out objectiveId))
            {
                DarkLog.Normal("Usage: /agency replay <player> <objective>");
                return;
            }

            if (AgencyProgression.ReplayReward(playerName, objectiveId))
            {
                DarkLog.Normal("Replayed agency reward for " + playerName + " objective " + objectiveId + ".");
            }
            else
            {
                DarkLog.Normal("Agency reward replay failed. Check that the player/objective are valid, the objective is complete, and it has rewards.");
            }
        }

        private static void RevokeReward(string argument)
        {
            string playerName;
            string objectiveId;
            if (!TryReadPlayerObjective(argument, out playerName, out objectiveId))
            {
                DarkLog.Normal("Usage: /agency revoke <player> <objective>");
                return;
            }

            if (AgencyProgression.RevokeReward(playerName, objectiveId))
            {
                DarkLog.Normal("Queued agency reward revocation for " + playerName + " objective " + objectiveId + ".");
            }
            else
            {
                DarkLog.Normal("Agency reward revocation failed. Check that the player/objective are valid and the objective has rewards.");
            }
        }

        private static bool TryReadPlayerObjective(string argument, out string playerName, out string objectiveId)
        {
            playerName = string.Empty;
            objectiveId = string.Empty;
            if (string.IsNullOrEmpty(argument) || !argument.Contains(" "))
            {
                return false;
            }
            int split = argument.IndexOf(" ", StringComparison.Ordinal);
            playerName = argument.Substring(0, split).Trim();
            objectiveId = argument.Substring(split + 1).Trim();
            return !string.IsNullOrEmpty(objectiveId) && (!string.IsNullOrEmpty(playerName) || argument.StartsWith("server ", StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryReadRecordEvidence(string argument, out string playerName, out string evidenceType, out string evidenceId)
        {
            playerName = string.Empty;
            evidenceType = string.Empty;
            evidenceId = string.Empty;
            if (string.IsNullOrEmpty(argument))
            {
                return false;
            }

            string[] parts = argument.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                return false;
            }
            playerName = parts[0].Trim();
            evidenceType = parts[1].Trim();
            evidenceId = parts[2].Trim();
            return !string.IsNullOrEmpty(playerName) && !string.IsNullOrEmpty(evidenceType) && !string.IsNullOrEmpty(evidenceId);
        }
    }
}
