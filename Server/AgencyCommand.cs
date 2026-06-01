using System;

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
                default:
                    DarkLog.Normal("Usage: /agency [status|reload|objectives|evidence [player]|rewards [player]]");
                    break;
            }
        }

        private static void ShowStatus()
        {
            if (!Settings.settingsStore.agencyProgressionEnabled)
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
            DarkLog.Normal("Reward records: " + AgencyProgression.GetRewardRecords().Length);
        }

        private static void Reload()
        {
            AgencyProgression.Load(Settings.settingsStore.agencyProgressionEnabled);
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
    }
}
