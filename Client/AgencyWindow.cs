using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkMultiPlayer
{
    public class AgencyWindow
    {
        private const float WindowWidth = 560;
        private const float WindowHeight = 420;
        private const int ObjectivesPerPage = 7;
        private readonly DMPGame dmpGame;
        private readonly NamedAction updateAction;
        private readonly NamedAction drawAction;
        private bool initialized;
        private bool safeDisplay;
        private bool isWindowLocked;
        private Rect windowRect;
        private Rect moveRect;
        private Vector2 detailScroll;
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle noteStyle;
        private string selectedObjectiveId;
        private int objectivePage;
        private AgencyObjectiveFilter objectiveFilter = AgencyObjectiveFilter.ALL;

        public AgencyWindow(DMPGame dmpGame)
        {
            this.dmpGame = dmpGame;
            updateAction = new NamedAction(Update);
            drawAction = new NamedAction(Draw);
            dmpGame.updateEvent.Add(updateAction);
            dmpGame.drawEvent.Add(drawAction);
        }

        public void Stop()
        {
            RemoveWindowLock();
            dmpGame.updateEvent.Remove(updateAction);
            dmpGame.drawEvent.Remove(drawAction);
        }

        private void InitGUI()
        {
            windowRect = new Rect(Screen.width / 2f - WindowWidth / 2f, Screen.height / 2f - WindowHeight / 2f, WindowWidth, WindowHeight);
            moveRect = new Rect(0, 0, 10000, 20);
            windowStyle = new GUIStyle(GUI.skin.window);
            buttonStyle = new GUIStyle(GUI.skin.button);
            selectedButtonStyle = new GUIStyle(GUI.skin.button);
            selectedButtonStyle.normal.textColor = Color.green;
            selectedButtonStyle.hover.textColor = Color.green;
            selectedButtonStyle.active.textColor = Color.green;

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 14;
            headerStyle.normal.textColor = Color.white;
            headerStyle.wordWrap = true;

            subHeaderStyle = new GUIStyle(GUI.skin.label);
            subHeaderStyle.fontStyle = FontStyle.Bold;
            subHeaderStyle.normal.textColor = Color.white;
            subHeaderStyle.wordWrap = true;

            noteStyle = new GUIStyle(GUI.skin.label);
            noteStyle.normal.textColor = new Color(1, 1, 1, 0.78f);
            noteStyle.wordWrap = true;
            initialized = true;
        }

        private void Update()
        {
            safeDisplay = dmpGame != null && dmpGame.running && dmpGame.serverAgencyProgressionEnabled && dmpGame.displayAgencyProgression;
        }

        private void Draw()
        {
            if (!safeDisplay)
            {
                RemoveWindowLock();
                return;
            }
            if (!initialized)
            {
                InitGUI();
            }

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;
            bool shouldLock = windowRect.Contains(mousePos);
            if (shouldLock && !isWindowLocked)
            {
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "DMP_AgencyWindowLock");
                isWindowLocked = true;
            }
            if (!shouldLock && isWindowLocked)
            {
                RemoveWindowLock();
            }

            windowRect = DMPGuiUtil.PreventOffscreenWindow(GUILayout.Window(6718 + Client.WINDOW_OFFSET, windowRect, DrawContent, "Server Space Agency", windowStyle, GUILayout.Width(WindowWidth), GUILayout.Height(WindowHeight)));
        }

        private void DrawContent(int windowId)
        {
            GUI.DragWindow(moveRect);
            GUILayout.BeginVertical();
            DrawHeader();
            GUILayout.Space(6);
            DrawCampaignState();
            GUILayout.Space(6);
            DrawMissionSummary();
            GUILayout.Space(4);
            DrawFilters();
            GUILayout.Space(6);
            DrawObjectiveBrowser();
            GUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            string packName = dmpGame.agencyProgressionWorker.PackName;
            if (string.IsNullOrEmpty(packName))
            {
                packName = "Server Agency";
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label(packName, headerStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(70)))
            {
                dmpGame.displayAgencyProgression = false;
            }
            GUILayout.EndHorizontal();

            string onboarding = dmpGame.agencyProgressionWorker.OnboardingText;
            if (string.IsNullOrEmpty(onboarding))
            {
                onboarding = "Review server-authored objectives, shared progress, and agency rewards.";
            }
            GUILayout.Label(onboarding, noteStyle, GUILayout.Height(36));
        }

        private void DrawFilters()
        {
            GUILayout.BeginHorizontal();
            DrawFilterButton(AgencyObjectiveFilter.ALL, "All");
            DrawFilterButton(AgencyObjectiveFilter.AVAILABLE, "Open");
            DrawFilterButton(AgencyObjectiveFilter.ACTIVE, "Active");
            DrawFilterButton(AgencyObjectiveFilter.COMPLETED, "Done");
            DrawFilterButton(AgencyObjectiveFilter.LOCKED, "Locked");
            DrawFilterButton(AgencyObjectiveFilter.SHARED, "Shared");
            GUILayout.EndHorizontal();
        }

        private void DrawMissionSummary()
        {
            AgencyObjectiveSummary[] objectives = dmpGame.agencyProgressionWorker.Objectives;
            if (objectives.Length == 0)
            {
                GUILayout.Label("Mission board: no objectives available.", noteStyle, GUILayout.Height(18));
                return;
            }

            int openCount = 0;
            int activeCount = 0;
            int completedCount = 0;
            int sharedCount = 0;
            for (int i = 0; i < objectives.Length; i++)
            {
                if (MatchesText(objectives[i].status, "available"))
                {
                    openCount++;
                }
                if (MatchesText(objectives[i].status, "active") || MatchesText(objectives[i].status, "in progress"))
                {
                    activeCount++;
                }
                if (MatchesText(objectives[i].status, "complete"))
                {
                    completedCount++;
                }
                if (MatchesText(objectives[i].scope, "server") || MatchesText(objectives[i].scope, "shared") || MatchesText(objectives[i].scope, "community"))
                {
                    sharedCount++;
                }
            }

            GUILayout.Label("Mission board: " + openCount + " open | " + activeCount + " active | " + sharedCount + " shared | " + completedCount + " done", noteStyle, GUILayout.Height(18));
        }

        private void DrawCampaignState()
        {
            string campaignName = dmpGame.agencyProgressionWorker.CampaignName;
            CampaignMetricSummary[] metrics = dmpGame.agencyProgressionWorker.CampaignMetrics;
            if (string.IsNullOrEmpty(campaignName) && metrics.Length == 0)
            {
                return;
            }

            string phaseTitle = dmpGame.agencyProgressionWorker.CampaignPhaseTitle;
            if (string.IsNullOrEmpty(phaseTitle))
            {
                phaseTitle = dmpGame.agencyProgressionWorker.CampaignPhaseId;
            }
            GUILayout.Label("Campaign: " + (string.IsNullOrEmpty(campaignName) ? "Server Campaign" : campaignName), subHeaderStyle);
            if (!string.IsNullOrEmpty(phaseTitle))
            {
                GUILayout.Label("Phase: " + phaseTitle, noteStyle, GUILayout.Height(18));
            }
            if (!string.IsNullOrEmpty(dmpGame.agencyProgressionWorker.CampaignPhaseDescription))
            {
                GUILayout.Label(dmpGame.agencyProgressionWorker.CampaignPhaseDescription, noteStyle, GUILayout.Height(28));
            }

            int shownMetrics = Math.Min(metrics.Length, 3);
            for (int i = 0; i < shownMetrics; i++)
            {
                GUILayout.Label(BuildMetricSummary(metrics[i]), noteStyle, GUILayout.Height(18));
            }
            DrawEconomyState();
            DrawCampaignEvents();
        }

        private void DrawEconomyState()
        {
            EconomyResourceSummary[] resources = dmpGame.agencyProgressionWorker.EconomyResources;
            if (resources.Length == 0)
            {
                return;
            }

            int shownResources = Math.Min(resources.Length, 2);
            for (int i = 0; i < shownResources; i++)
            {
                GUILayout.Label(BuildEconomySummary(resources[i]), noteStyle, GUILayout.Height(18));
            }
        }

        private void DrawCampaignEvents()
        {
            CampaignEventSummary[] events = dmpGame.agencyProgressionWorker.CampaignEvents;
            int shownEvents = 0;
            for (int i = 0; i < events.Length && shownEvents < 2; i++)
            {
                if (!MatchesText(events[i].status, "active") && !MatchesText(events[i].status, "available"))
                {
                    continue;
                }
                string title = string.IsNullOrEmpty(events[i].title) ? events[i].id : events[i].title;
                GUILayout.Label("Event: [" + events[i].status + "] " + title, noteStyle, GUILayout.Height(18));
                shownEvents++;
            }
        }

        private void DrawFilterButton(AgencyObjectiveFilter filter, string label)
        {
            GUIStyle style = objectiveFilter == filter ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(22)))
            {
                objectiveFilter = filter;
                objectivePage = 0;
                selectedObjectiveId = null;
            }
        }

        private void DrawObjectiveBrowser()
        {
            AgencyObjectiveSummary[] filteredObjectives = FilterAgencyObjectives(dmpGame.agencyProgressionWorker.Objectives);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(225));
            GUILayout.Label("Missions", subHeaderStyle);
            if (filteredObjectives.Length == 0)
            {
                selectedObjectiveId = null;
                objectivePage = 0;
                GUILayout.Label("No objectives match this filter.", noteStyle);
            }
            else
            {
                DrawObjectiveList(filteredObjectives);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Details", subHeaderStyle);
            AgencyObjectiveSummary selectedObjective = filteredObjectives.Length == 0 ? null : GetSelectedObjective(filteredObjectives);
            DrawObjectiveDetail(selectedObjective);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawObjectiveList(AgencyObjectiveSummary[] objectives)
        {
            int lastPage = (objectives.Length - 1) / ObjectivesPerPage;
            if (objectivePage > lastPage)
            {
                objectivePage = lastPage;
            }
            if (objectivePage < 0)
            {
                objectivePage = 0;
            }

            int startIndex = objectivePage * ObjectivesPerPage;
            int endIndex = Math.Min(objectives.Length, startIndex + ObjectivesPerPage);
            for (int i = startIndex; i < endIndex; i++)
            {
                AgencyObjectiveSummary objective = objectives[i];
                GUIStyle style = objective.id == selectedObjectiveId ? selectedButtonStyle : buttonStyle;
                if (GUILayout.Button(BuildObjectiveButtonLabel(objective), style, GUILayout.Height(24)))
                {
                    selectedObjectiveId = objective.id;
                }
            }

            GUILayout.FlexibleSpace();
            if (objectives.Length > ObjectivesPerPage)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Prev", buttonStyle, GUILayout.Width(64)) && objectivePage > 0)
                {
                    objectivePage--;
                    selectedObjectiveId = objectives[objectivePage * ObjectivesPerPage].id;
                }
                GUILayout.Label("Page " + (objectivePage + 1) + " / " + (lastPage + 1), noteStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Next", buttonStyle, GUILayout.Width(64)) && objectivePage < lastPage)
                {
                    objectivePage++;
                    selectedObjectiveId = objectives[objectivePage * ObjectivesPerPage].id;
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawObjectiveDetail(AgencyObjectiveSummary objective)
        {
            if (objective == null)
            {
                GUILayout.Label("Select an objective.", noteStyle);
                return;
            }

            detailScroll = GUILayout.BeginScrollView(detailScroll, false, true);
            GUILayout.Label(BuildObjectiveTitle(objective), headerStyle);
            GUILayout.Label(BuildObjectiveMetadata(objective), noteStyle);
            GUILayout.Space(6);
            GUILayout.Label(objective.description, noteStyle);
            GUILayout.Space(8);
            DrawDetailLine("Progress", BuildProgressSummary(objective));
            DrawDetailLine("Contributors", BuildContributorSummary(objective));
            DrawDetailLine("Contribution", BuildContributionSummary(objective));
            DrawDetailLine("Rewards", BuildRewardSummary(objective));
            DrawDetailLine("World State", BuildWorldStateSummary(objective));
            DrawDetailLine("Economy", BuildObjectiveEconomySummary(objective));
            DrawDetailLine("Category", string.IsNullOrEmpty(objective.category) ? "General" : objective.category);
            DrawDetailLine("Scope", string.IsNullOrEmpty(objective.scope) ? "Server" : objective.scope);
            DrawDetailLine("Status", string.IsNullOrEmpty(objective.status) ? "Available" : objective.status);
            GUILayout.EndScrollView();
        }

        private void DrawDetailLine(string label, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", subHeaderStyle, GUILayout.Width(88));
            GUILayout.Label(value, noteStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        private AgencyObjectiveSummary GetSelectedObjective(AgencyObjectiveSummary[] objectives)
        {
            if (string.IsNullOrEmpty(selectedObjectiveId))
            {
                selectedObjectiveId = objectives[0].id;
                return objectives[0];
            }
            for (int i = 0; i < objectives.Length; i++)
            {
                if (objectives[i].id == selectedObjectiveId)
                {
                    objectivePage = i / ObjectivesPerPage;
                    return objectives[i];
                }
            }
            selectedObjectiveId = objectives[0].id;
            objectivePage = 0;
            return objectives[0];
        }

        private AgencyObjectiveSummary[] FilterAgencyObjectives(AgencyObjectiveSummary[] objectives)
        {
            if (objectiveFilter == AgencyObjectiveFilter.ALL)
            {
                return objectives;
            }

            List<AgencyObjectiveSummary> filteredObjectives = new List<AgencyObjectiveSummary>();
            foreach (AgencyObjectiveSummary objective in objectives)
            {
                if (MatchesFilter(objective))
                {
                    filteredObjectives.Add(objective);
                }
            }
            return filteredObjectives.ToArray();
        }

        private bool MatchesFilter(AgencyObjectiveSummary objective)
        {
            switch (objectiveFilter)
            {
                case AgencyObjectiveFilter.AVAILABLE:
                    return MatchesText(objective.status, "available");
                case AgencyObjectiveFilter.ACTIVE:
                    return MatchesText(objective.status, "active") || MatchesText(objective.status, "in progress");
                case AgencyObjectiveFilter.COMPLETED:
                    return MatchesText(objective.status, "complete");
                case AgencyObjectiveFilter.LOCKED:
                    return MatchesText(objective.status, "locked") || MatchesText(objective.status, "hidden");
                case AgencyObjectiveFilter.SHARED:
                    return MatchesText(objective.scope, "server") || MatchesText(objective.scope, "shared") || MatchesText(objective.scope, "community");
                default:
                    return true;
            }
        }

        private bool MatchesText(string value, string match)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string BuildObjectiveButtonLabel(AgencyObjectiveSummary objective)
        {
            string title = string.IsNullOrEmpty(objective.title) ? objective.id : objective.title;
            string category = string.IsNullOrEmpty(objective.category) ? "" : objective.category + " | ";
            string progress = BuildCompactProgressSummary(objective);
            return category + "[" + objective.status + "] " + title + progress;
        }

        private string BuildObjectiveTitle(AgencyObjectiveSummary objective)
        {
            string title = string.IsNullOrEmpty(objective.title) ? objective.id : objective.title;
            return title + " [" + objective.status + "]";
        }

        private string BuildObjectiveMetadata(AgencyObjectiveSummary objective)
        {
            string category = string.IsNullOrEmpty(objective.category) ? "General" : objective.category;
            string contractType = string.IsNullOrEmpty(objective.contractType) ? "Objective" : objective.contractType;
            string scope = string.IsNullOrEmpty(objective.scope) ? "Server" : objective.scope;
            string issuer = string.IsNullOrEmpty(objective.issuer) ? "Server Agency" : objective.issuer;
            return category + " | " + contractType + " | " + scope + " | " + issuer;
        }

        private string BuildProgressSummary(AgencyObjectiveSummary objective)
        {
            if (objective.progressTarget <= 0)
            {
                return string.Empty;
            }
            string progressSummary = objective.progressValue.ToString("0.##") + " / " + objective.progressTarget.ToString("0.##");
            if (!string.IsNullOrEmpty(objective.progressUnit))
            {
                progressSummary += " " + objective.progressUnit;
            }
            if (objective.progressValue >= objective.progressTarget)
            {
                progressSummary += " complete";
            }
            return progressSummary;
        }

        private string BuildContributorSummary(AgencyObjectiveSummary objective)
        {
            if (objective.progressTarget <= 0 && objective.contributorCount <= 0)
            {
                return string.Empty;
            }
            string summary = objective.contributorCount.ToString() + " contributor" + (objective.contributorCount == 1 ? string.Empty : "s");
            if (objective.uniqueContributors)
            {
                summary += "; each player counts once";
            }
            else if (objective.progressTarget > 0)
            {
                summary += "; repeat contributions allowed";
            }
            if (!string.IsNullOrEmpty(objective.contributors))
            {
                summary += " (" + objective.contributors + ")";
            }
            return summary;
        }

        private string BuildContributionSummary(AgencyObjectiveSummary objective)
        {
            if (objective.progressTarget <= 0)
            {
                return string.Empty;
            }
            string label = string.IsNullOrEmpty(objective.contributionLabel) ? "Matching evidence" : objective.contributionLabel;
            string unit = string.IsNullOrEmpty(objective.progressUnit) ? string.Empty : " " + objective.progressUnit;
            return label + ": +" + objective.progressPerEvidence.ToString("0.##") + unit;
        }

        private string BuildRewardSummary(AgencyObjectiveSummary objective)
        {
            string rewardSummary = string.Empty;
            if (objective.rewardFunds != 0)
            {
                rewardSummary = "Funds " + objective.rewardFunds.ToString("0.##");
            }
            if (objective.rewardScience != 0)
            {
                rewardSummary = AppendReward(rewardSummary, "Science " + objective.rewardScience.ToString("0.##"));
            }
            if (objective.rewardReputation != 0)
            {
                rewardSummary = AppendReward(rewardSummary, "Rep " + objective.rewardReputation.ToString("0.##"));
            }
            return rewardSummary;
        }

        private string BuildWorldStateSummary(AgencyObjectiveSummary objective)
        {
            if (string.IsNullOrEmpty(objective.metricContributionId) || objective.metricContributionAmount == 0)
            {
                return string.Empty;
            }

            string amount = objective.metricContributionAmount > 0 ? "+" + objective.metricContributionAmount.ToString("0.##") : objective.metricContributionAmount.ToString("0.##");
            string metricName = GetMetricTitle(objective.metricContributionId);
            string summary = amount + " " + metricName;
            if (objective.metricContributionMax > 0)
            {
                summary += " up to " + objective.metricContributionMax.ToString("0.##");
            }
            return summary;
        }

        private string BuildObjectiveEconomySummary(AgencyObjectiveSummary objective)
        {
            if (string.IsNullOrEmpty(objective.economyResourceId) || objective.economyResourceDelta == 0)
            {
                return string.Empty;
            }

            string amount = objective.economyResourceDelta > 0 ? "+" + objective.economyResourceDelta.ToString("0.##") : objective.economyResourceDelta.ToString("0.##");
            return amount + " " + GetEconomyResourceTitle(objective.economyResourceId);
        }

        private string BuildCompactProgressSummary(AgencyObjectiveSummary objective)
        {
            if (objective.progressTarget <= 0)
            {
                return string.Empty;
            }
            string unit = string.IsNullOrEmpty(objective.progressUnit) ? string.Empty : " " + objective.progressUnit;
            return " (" + objective.progressValue.ToString("0.##") + "/" + objective.progressTarget.ToString("0.##") + unit + ")";
        }

        private string BuildMetricSummary(CampaignMetricSummary metric)
        {
            string title = string.IsNullOrEmpty(metric.title) ? metric.id : metric.title;
            string category = string.IsNullOrEmpty(metric.category) ? "General" : metric.category;
            string target = metric.target > 0 ? " / " + metric.target.ToString("0.##") : string.Empty;
            return category + ": " + title + " " + metric.value.ToString("0.##") + target + metric.unit;
        }

        private string BuildEconomySummary(EconomyResourceSummary resource)
        {
            string title = string.IsNullOrEmpty(resource.title) ? resource.id : resource.title;
            string category = string.IsNullOrEmpty(resource.category) ? "Economy" : resource.category;
            string maxValue = resource.maxValue > 0 ? " / " + resource.maxValue.ToString("0.##") : string.Empty;
            string modifier = resource.boundedModifier == 0 ? string.Empty : " (" + (resource.boundedModifier * 100).ToString("+0.##;-0.##") + "%)";
            return category + ": " + title + " " + resource.value.ToString("0.##") + maxValue + resource.unit + " " + resource.state + modifier;
        }

        private string GetMetricTitle(string metricId)
        {
            CampaignMetricSummary[] metrics = dmpGame.agencyProgressionWorker.CampaignMetrics;
            for (int i = 0; i < metrics.Length; i++)
            {
                if (metrics[i].id == metricId)
                {
                    return string.IsNullOrEmpty(metrics[i].title) ? metricId : metrics[i].title;
                }
            }
            return metricId;
        }

        private string GetEconomyResourceTitle(string resourceId)
        {
            EconomyResourceSummary[] resources = dmpGame.agencyProgressionWorker.EconomyResources;
            for (int i = 0; i < resources.Length; i++)
            {
                if (resources[i].id == resourceId)
                {
                    return string.IsNullOrEmpty(resources[i].title) ? resourceId : resources[i].title;
                }
            }
            return resourceId;
        }

        private string AppendReward(string rewardSummary, string reward)
        {
            if (string.IsNullOrEmpty(rewardSummary))
            {
                return reward;
            }
            return rewardSummary + ", " + reward;
        }

        private void RemoveWindowLock()
        {
            if (isWindowLocked)
            {
                isWindowLocked = false;
                InputLockManager.RemoveControlLock("DMP_AgencyWindowLock");
            }
        }
    }
}
