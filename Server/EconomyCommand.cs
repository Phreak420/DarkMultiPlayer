using System;

namespace DarkMultiPlayerServer
{
    public class EconomyCommand
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
                    SetResource(argument);
                    break;
                case "adjust":
                    AdjustResource(argument);
                    break;
                case "reset":
                    Reset(argument);
                    break;
                default:
                    DarkLog.Normal("Usage: /economy [status|set <resource> <value>|adjust <resource> <delta>|reset confirm]");
                    break;
            }
        }

        private static void ShowStatus()
        {
            if (!Settings.IsAgencyProgressionActive())
            {
                DarkLog.Normal("Economy state is disabled.");
                return;
            }

            DarkLog.Normal("Economy: " + EconomyState.FormatStatus());
            foreach (EconomyResource resource in EconomyState.Resources)
            {
                string modifier = resource.boundedModifier == 0 ? "" : " modifier=" + (resource.boundedModifier * 100).ToString("0.##") + "%";
                DarkLog.Normal(resource.id + " [" + resource.category + "] " + resource.title + " = " + resource.value.ToString("0.##") + "/" + resource.maxValue.ToString("0.##") + resource.unit + " " + resource.state + modifier);
                if (!string.IsNullOrEmpty(resource.recoveryContractHint) && resource.state == "Scarce")
                {
                    DarkLog.Normal("  recovery: " + resource.recoveryContractHint);
                }
            }
        }

        private static void SetResource(string argument)
        {
            string[] parts = argument.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double value;
            if (parts.Length != 2 || !double.TryParse(parts[1], out value))
            {
                DarkLog.Normal("Usage: /economy set <resource> <value>");
                return;
            }

            if (EconomyState.SetResource(parts[0], value, "server"))
            {
                DarkLog.Normal("Economy resource '" + parts[0] + "' set to " + value.ToString("0.##") + " with configured bounds applied.");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Economy resource update failed. Check that economy state is loaded and the resource id is valid.");
            }
        }

        private static void AdjustResource(string argument)
        {
            string[] parts = argument.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double delta;
            if (parts.Length != 2 || !double.TryParse(parts[1], out delta))
            {
                DarkLog.Normal("Usage: /economy adjust <resource> <delta>");
                return;
            }

            if (EconomyState.AdjustResource(parts[0], delta, "server"))
            {
                DarkLog.Normal("Economy resource '" + parts[0] + "' adjusted by " + delta.ToString("0.##") + " with configured bounds applied.");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Economy resource adjustment failed. Check that economy state is loaded and the resource id is valid.");
            }
        }

        private static void Reset(string argument)
        {
            if (argument != "confirm")
            {
                DarkLog.Normal("Usage: /economy reset confirm");
                return;
            }

            if (EconomyState.ResetState(true, "server"))
            {
                DarkLog.Normal("Economy state reset.");
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            else
            {
                DarkLog.Normal("Economy state reset failed.");
            }
        }
    }
}
