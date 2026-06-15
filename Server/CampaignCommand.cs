using System;

namespace DarkMultiPlayerServer
{
    public class CampaignCommand
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
                case "set":
                    SetMetric(argument);
                    break;
                case "advance":
                    AdvancePhase(argument);
                    break;
                case "events":
                    ShowEvents();
                    break;
                case "activate":
                    ActivateEvent(argument);
                    break;
                case "complete":
                    CompleteEvent(argument);
                    break;
                case "reset":
                    Reset(argument);
                    break;
                default:
                    DarkLog.Normal("Usage: /campaign [status|set <metric> <value>|advance <phase>|events|activate <event>|complete <event>|reset confirm]");
                    break;
            }
        }

        private static void ShowStatus()
        {
            if (!Settings.IsAgencyProgressionActive())
            {
                DarkLog.Normal("Campaign state is disabled.");
                return;
            }

            DarkLog.Normal("Campaign: " + CampaignState.FormatStatus());
            CampaignPhase phase = CampaignState.CurrentPhase;
            if (phase != null)
            {
                DarkLog.Normal("Current phase: " + phase.id + " " + phase.title);
            }
            foreach (CampaignMetric metric in CampaignState.Metrics)
            {
                string target = metric.target > 0 ? "/" + metric.target.ToString("0.##") : "";
                DarkLog.Normal(metric.id + " [" + metric.category + "] " + metric.title + " = " + metric.value.ToString("0.##") + target + metric.unit);
            }
            ShowEvents();
        }

        private static void SetMetric(string argument)
        {
            string[] parts = argument.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double value;
            if (parts.Length != 2 || !double.TryParse(parts[1], out value))
            {
                DarkLog.Normal("Usage: /campaign set <metric> <value>");
                return;
            }

            if (CampaignState.SetMetric(parts[0], value, "server"))
            {
                DarkLog.Normal("Campaign metric '" + parts[0] + "' set to " + value.ToString("0.##") + ".");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Campaign metric update failed. Check that campaign state is loaded and the metric id is valid.");
            }
        }

        private static void AdvancePhase(string phaseId)
        {
            if (string.IsNullOrEmpty(phaseId))
            {
                DarkLog.Normal("Usage: /campaign advance <phase>");
                return;
            }

            if (CampaignState.AdvancePhase(phaseId, "server"))
            {
                DarkLog.Normal("Campaign advanced to phase '" + phaseId + "'.");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Campaign phase advance failed. Check that the phase id is valid.");
            }
        }

        private static void ShowEvents()
        {
            CampaignEvent[] events = CampaignState.Events;
            DarkLog.Normal("Campaign events: " + events.Length);
            foreach (CampaignEvent campaignEvent in events)
            {
                DarkLog.Normal(campaignEvent.id + " [" + campaignEvent.status + "] " + campaignEvent.title);
            }
        }

        private static void ActivateEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                DarkLog.Normal("Usage: /campaign activate <event>");
                return;
            }
            if (CampaignState.ActivateEvent(eventId, "server"))
            {
                DarkLog.Normal("Campaign event '" + eventId + "' activated.");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Campaign event activation failed. Check that the event id is valid.");
            }
        }

        private static void CompleteEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                DarkLog.Normal("Usage: /campaign complete <event>");
                return;
            }
            if (CampaignState.CompleteEvent(eventId, "server"))
            {
                DarkLog.Normal("Campaign event '" + eventId + "' completed.");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Campaign event completion failed. Check that the event id is valid.");
            }
        }

        private static void Reset(string argument)
        {
            if (argument != "confirm")
            {
                DarkLog.Normal("Usage: /campaign reset confirm");
                return;
            }

            if (CampaignState.ResetState(true, "server"))
            {
                DarkLog.Normal("Campaign state reset.");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Campaign state reset failed.");
            }
        }
    }
}
