using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DarkMultiPlayer
{
    enum OptionsTab
    {
        PLAYER,
        CACHE,
        CONTROLS,
        ADVANCED,
        AGENCY
    }

    enum AgencyObjectiveFilter
    {
        ALL,
        AVAILABLE,
        ACTIVE,
        COMPLETED,
        LOCKED,
        SHARED
    }

    public class OptionsWindow
    {
        public bool display;
        public bool showDebugWindow;
        private bool isWindowLocked;
        private bool safeDisplay;
        private bool initialized;
        //GUI Layout
        private Rect windowRect;
        private Rect moveRect;
        private GUILayoutOption[] layoutOptions;
        private GUILayoutOption[] smallOption;
        //Styles
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        //const
        private const float WINDOW_HEIGHT = 350;
        private const float WINDOW_WIDTH = 300;
        private const int AGENCY_OBJECTIVES_PER_PAGE = 3;
        private const int descWidth = 75;
        private const int sepWidth = 5;
        //Keybindings
        private bool settingChat;
        private bool settingScreenshot;
        private string settingKeyMessage = "cancel";
        private string toolbarMode;
        private string interpolatorMode;
        private string selectedAgencyObjectiveId;
        private string identityCopyMessage;
        private int agencyObjectivePage;
        private AgencyObjectiveFilter agencyObjectiveFilter = AgencyObjectiveFilter.ALL;
        // Toolbar
        private GUIStyle toolbarBtnStyle;
        private OptionsTab selectedTab = OptionsTab.PLAYER;
        // New style
        private GUIStyle descriptorStyle;
        private GUIStyle plrNameStyle;
        private GUIStyle textFieldStyle;
        private GUIStyle noteStyle;
        private GUIStyle sectionHeaderStyle;
        //Services
        private DMPGame dmpGame;
        private Settings dmpSettings;
        private NetworkWorker networkWorker;
        private PlayerColorWorker playerColorWorker;
        private UniverseSyncCache universeSyncCache;
        private ModWorker modWorker;
        private UniverseConverterWindow universeConverterWindow;
        private ServerListDisclaimerWindow serverListDisclaimerWindow;
        private ToolbarSupport toolbarSupport;

        public OptionsWindow(Settings dmpSettings, UniverseSyncCache universeSyncCache, ModWorker modWorker, UniverseConverterWindow universeConverterWindow, ToolbarSupport toolbarSupport, ServerListDisclaimerWindow serverListDisclaimerWindow)
        {
            this.dmpSettings = dmpSettings;
            this.universeSyncCache = universeSyncCache;
            this.modWorker = modWorker;
            this.universeConverterWindow = universeConverterWindow;
            this.toolbarSupport = toolbarSupport;
            this.serverListDisclaimerWindow = serverListDisclaimerWindow;
        }

        public void SetDependencies(DMPGame dmpGame, NetworkWorker networkWorker, PlayerColorWorker playerColorWorker)
        {
            this.networkWorker = networkWorker;
            this.playerColorWorker = playerColorWorker;
            this.dmpGame = dmpGame;
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width / 2f + WINDOW_WIDTH / 2f, Screen.height / 2f - WINDOW_HEIGHT / 2f, WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);

            windowStyle = new GUIStyle(GUI.skin.window);

            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.Width(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.Height(WINDOW_HEIGHT);
            layoutOptions[2] = GUILayout.ExpandWidth(true);
            layoutOptions[3] = GUILayout.ExpandHeight(true);

            smallOption = new GUILayoutOption[2];
            smallOption[0] = GUILayout.Width(100);
            smallOption[1] = GUILayout.ExpandWidth(false);

            toolbarBtnStyle = new GUIStyle();
            toolbarBtnStyle.alignment = TextAnchor.MiddleCenter;
            toolbarBtnStyle.normal.background = new Texture2D(1, 1);
            toolbarBtnStyle.normal.background.SetPixel(0, 0, Color.black);
            toolbarBtnStyle.normal.background.Apply();
            toolbarBtnStyle.normal.textColor = Color.white;
            toolbarBtnStyle.hover.background = new Texture2D(1, 1);
            toolbarBtnStyle.hover.background.SetPixel(0, 0, Color.grey);
            toolbarBtnStyle.hover.background.Apply();
            toolbarBtnStyle.hover.textColor = Color.white;
            toolbarBtnStyle.padding = new RectOffset(4, 4, 2, 2);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(4, 4, 2, 2);

            descriptorStyle = new GUIStyle();
            descriptorStyle.normal.textColor = Color.white;
            descriptorStyle.padding = new RectOffset(4, 4, 2, 2);
            descriptorStyle.alignment = TextAnchor.MiddleRight;

            plrNameStyle = new GUIStyle();
            plrNameStyle.normal.background = new Texture2D(1, 1);
            plrNameStyle.normal.background.SetPixel(0, 0, new Color(0, 0, 0, .54f));
            plrNameStyle.normal.background.Apply();
            plrNameStyle.normal.textColor = dmpSettings.playerColor;
            plrNameStyle.padding = new RectOffset(4, 4, 2, 2);
            plrNameStyle.alignment = TextAnchor.MiddleLeft;
            plrNameStyle.fontStyle = FontStyle.Bold;

            textFieldStyle = new GUIStyle();
            textFieldStyle.normal.background = new Texture2D(1, 1);
            textFieldStyle.normal.background.SetPixel(0, 0, new Color(0, 0, 0, .54f));
            textFieldStyle.normal.background.Apply();
            textFieldStyle.padding = new RectOffset(4, 4, 2, 2);
            textFieldStyle.normal.textColor = Color.white;

            noteStyle = new GUIStyle();
            noteStyle.normal.textColor = new Color(1, 1, 1, 0.75f);
            noteStyle.fontSize = 12;
            noteStyle.padding = new RectOffset(4, 4, 2, 2);
            noteStyle.alignment = TextAnchor.UpperCenter;
            noteStyle.wordWrap = true;

            sectionHeaderStyle = new GUIStyle();
            Texture2D sectionHeader = new Texture2D(1, 1);
            sectionHeader.SetPixel(0, 0, new Color(0, 0, 0, 0.87f));
            sectionHeader.Apply();
            sectionHeaderStyle.normal.background = sectionHeader;
            sectionHeaderStyle.normal.textColor = Color.white;
            sectionHeaderStyle.padding = new RectOffset(4, 4, 2, 2);
            sectionHeaderStyle.alignment = TextAnchor.MiddleCenter;
            sectionHeaderStyle.fontStyle = FontStyle.Bold;

            UpdateToolbarString();
            UpdateInterpolatorString();
        }

        private void UpdateToolbarString()
        {
            switch (dmpSettings.toolbarType)
            {
                case DMPToolbarType.DISABLED:
                    toolbarMode = "Toolbar: Disabled";
                    break;
                case DMPToolbarType.FORCE_STOCK:
                    toolbarMode = "Toolbar: Stock";
                    break;
                case DMPToolbarType.BLIZZY_IF_INSTALLED:
                    toolbarMode = "Toolbar: Blizzy's Toolbar";
                    break;
                case DMPToolbarType.BOTH_IF_INSTALLED:
                    toolbarMode = "Toolbar: Both";
                    break;
                default:
                    break;
            }
        }

        private void UpdateInterpolatorString()
        {
            switch (dmpSettings.interpolatorType)
            {
                case InterpolatorType.EXTRAPOLATE:
                    interpolatorMode = "Extrapolate with rotational acceleration";
                    break;
                case InterpolatorType.INTERPOLATE1S:
                    interpolatorMode = "Interpolate with 1 second delay (default)";
                    break;
                case InterpolatorType.INTERPOLATE3S:
                    interpolatorMode = "Interpolate with 3 seconds delay";
                    break;
            }
        }

        public void Update()
        {
            safeDisplay = display;
        }

        public void Draw()
        {
            if (safeDisplay)
            {
                if (!initialized)
                {
                    initialized = true;
                    InitGUI();
                }
                windowRect = DMPGuiUtil.PreventOffscreenWindow(GUILayout.Window(6711 + Client.WINDOW_OFFSET, windowRect, DrawContent, "DarkMultiPlayer - Options", windowStyle, layoutOptions));
            }
            CheckWindowLock();
        }

        private void DrawContent(int windowID)
        {
            display &= !GUI.Button(new Rect(windowRect.width - 24, 0, 19, 19), "X");
            //Player color
            GUI.DragWindow(moveRect);
            GUI.Box(new Rect(2, 20, windowRect.width - 4, 20), string.Empty, sectionHeaderStyle);
            if (selectedTab == OptionsTab.AGENCY && (dmpGame == null || !dmpGame.serverAgencyProgressionEnabled))
            {
                selectedTab = OptionsTab.ADVANCED;
            }
            selectedTab = (OptionsTab)GUILayout.Toolbar((int)selectedTab, GetOptionsTabStrings(), toolbarBtnStyle);

            int windowY = 17;
            windowY += 20 + 2;
            int groupY = 0;

            if (selectedTab == OptionsTab.PLAYER)
            {
                GUI.BeginGroup(new Rect(10, windowY, windowRect.width - 20, 198));
                groupY = 0;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Name:", descriptorStyle);
                plrNameStyle.normal.textColor = dmpSettings.playerColor;
                if (networkWorker != null && networkWorker.state == DarkMultiPlayerCommon.ClientState.RUNNING)
                    GUI.Label(new Rect(descWidth + sepWidth, groupY,
                        windowRect.width - (descWidth + sepWidth) - 20, 20),
                        dmpSettings.playerName, plrNameStyle);
                else
                {
                    string newName = GUI.TextField(new Rect(
                        descWidth + sepWidth,
                        0,
                        windowRect.width - (descWidth + sepWidth) - 20,
                        20), dmpSettings.playerName, plrNameStyle);

                    if (!newName.Equals(dmpSettings.playerName))
                    {
                        dmpSettings.playerName = newName;
                        dmpSettings.SaveSettings();
                    }
                }
                groupY += 20 + 4;


                Color playerColor = dmpSettings.playerColor;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Red:", descriptorStyle);
                playerColor.r = GUI.HorizontalSlider(new Rect(
                    descWidth + sepWidth,
                    groupY + 5,
                    windowRect.width - (descWidth + sepWidth) - 20,
                    12
                    ), dmpSettings.playerColor.r, 0, 1);
                groupY += 20;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Green:", descriptorStyle);
                playerColor.g = GUI.HorizontalSlider(new Rect(
                    descWidth + sepWidth,
                    groupY + 5,
                    windowRect.width - (descWidth + sepWidth) - 20,
                    12
                    ), dmpSettings.playerColor.g, 0, 1);
                groupY += 20;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Blue:", descriptorStyle);
                playerColor.b = GUI.HorizontalSlider(new Rect(
                    descWidth + sepWidth,
                    groupY + 5,
                    windowRect.width - (descWidth + sepWidth) - 20,
                    12
                    ), dmpSettings.playerColor.b, 0, 1);
                groupY += 22;

                if (GUI.Button(new Rect(0, groupY, windowRect.width - 20, 20), "Random Color", buttonStyle))
                    playerColor = PlayerColorWorker.GenerateRandomColor();
                groupY += 24;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Identity:", descriptorStyle);
                GUI.Label(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 88, 20), GetPlayerIdentityFingerprint(), textFieldStyle);
                if (GUI.Button(new Rect(windowRect.width - 82, groupY, 62, 20), "Copy ID", buttonStyle))
                {
                    GUIUtility.systemCopyBuffer = GetPlayerIdentityFingerprint();
                    identityCopyMessage = "Identity fingerprint copied";
                    DarkLog.Debug("Copied player identity fingerprint to clipboard");
                }
                groupY += 22;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "UUID:", descriptorStyle);
                GUI.Label(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 88, 20), GetPlayerUuidDisplay(), textFieldStyle);
                if (GUI.Button(new Rect(windowRect.width - 82, groupY, 62, 20), "Copy", buttonStyle))
                {
                    GUIUtility.systemCopyBuffer = GetPlayerUuidClipboardValue();
                    identityCopyMessage = "Player UUID copied";
                    DarkLog.Debug("Copied player UUID to clipboard");
                }
                groupY += 22;

                GUI.Label(new Rect(0, groupY, windowRect.width - 20, 20), string.IsNullOrEmpty(identityCopyMessage) ? "Keep your key files when moving installs." : identityCopyMessage, noteStyle);
                groupY += 22;

                if (GUI.Button(new Rect(0, groupY, 130, 20), "Backup ID", buttonStyle))
                {
                    identityCopyMessage = dmpSettings.BackupIdentityFiles() ? "Identity backup refreshed" : "Identity backup failed";
                }
                if (GUI.Button(new Rect(138, groupY, windowRect.width - 158, 20), "Copy Path", buttonStyle))
                {
                    GUIUtility.systemCopyBuffer = dmpSettings.IdentityBackupDirectory;
                    identityCopyMessage = "Backup path copied";
                }

                if (!playerColor.Equals(dmpSettings.playerColor))
                {
                    dmpSettings.playerColor = playerColor;
                    dmpSettings.SaveSettings();

                    if (networkWorker != null && playerColorWorker != null && networkWorker.state == DarkMultiPlayerCommon.ClientState.RUNNING)
                        playerColorWorker.SendPlayerColorToServer();
                }

                GUI.EndGroup();
                // windowY += 106 + 5;
            }
            if (selectedTab == OptionsTab.CACHE)
            {
                GUI.BeginGroup(new Rect(10, windowY, windowRect.width - 20, 84));
                groupY = 0;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Current:", descriptorStyle);
                GUI.Label(
                    new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 102, 20),
                    Mathf.Round(universeSyncCache.currentCacheSize / 1024 / 1024).ToString() + " MB");

                groupY += 20;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Maximum:", descriptorStyle);
                string newSizeStr = GUI.TextField(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 152, 20), dmpSettings.cacheSize.ToString(), textFieldStyle);
                GUI.Label(new Rect(descWidth + sepWidth + 80, groupY, 100, 20), "MegaBytes (MB)");
                int newSize;
                if (string.IsNullOrEmpty(newSizeStr)) newSize = 1;
                else
                {
                    if (int.TryParse(newSizeStr, out newSize))
                    {
                        if (newSize < 1) newSize = 1;
                        else if (newSize > 1000000) newSize = 1000000;
                    }
                    else newSize = 100000;
                }

                if (newSize != dmpSettings.cacheSize)
                {
                    dmpSettings.cacheSize = newSize;
                    dmpSettings.SaveSettings();
                }
                groupY += 22;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Manage:", descriptorStyle);
                if (GUI.Button(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 20, 20), "Expire"))
                    universeSyncCache.ExpireCache();

                groupY += 22;

                if (GUI.Button(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 20, 20), "Delete"))
                    universeSyncCache.DeleteCache();
                GUI.EndGroup();
            }
            //Key bindings
            if (selectedTab == OptionsTab.CONTROLS)
            {
                GUI.BeginGroup(new Rect(10, windowY, windowRect.width - 20, 92));
                groupY = 0;

                GUI.Label(new Rect(0, groupY, windowRect.width - 20, 48),
                    "Click a button below to select the action you want to change. Then press a key to set the binding. To cancel, click the button again or press Escape.",
                    noteStyle);
                groupY += 48;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Chat:", descriptorStyle);
                string chatKey = dmpSettings.chatKey.ToString();
                if (settingChat)
                {
                    chatKey = settingKeyMessage;
                    if (Event.current.isKey)
                    {
                        if (Event.current.keyCode != KeyCode.Escape)
                        {
                            dmpSettings.chatKey = Event.current.keyCode;
                            dmpSettings.SaveSettings();
                        }
                        settingChat = false;
                    }
                }

                if (GUI.Button(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 20, 20), chatKey, buttonStyle))
                {
                    settingScreenshot = false;
                    settingChat = !settingChat;
                }
                groupY += 22;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Screenshot:", descriptorStyle);
                string screenshotKey = dmpSettings.screenshotKey.ToString();
                if (settingScreenshot)
                {
                    screenshotKey = settingKeyMessage;
                    if (Event.current.isKey)
                    {
                        if (Event.current.keyCode != KeyCode.Escape)
                        {
                            dmpSettings.screenshotKey = Event.current.keyCode;
                            dmpSettings.SaveSettings();
                        }
                        settingScreenshot = false;
                    }
                }

                if (GUI.Button(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 20, 20), screenshotKey, buttonStyle))
                {
                    settingChat = false;
                    settingScreenshot = !settingScreenshot;
                }
                GUI.EndGroup();
            }
            if (selectedTab == OptionsTab.ADVANCED)
            {
                GUI.Box(new Rect(2, windowY, windowRect.width - 4, 20), "Mod Control", sectionHeaderStyle);
                windowY += 22;

                GUI.BeginGroup(new Rect(10, windowY, windowRect.width - 20, 42));
                groupY = 0;

                GUI.Label(new Rect(0, groupY, descWidth, 20), "Generate:", descriptorStyle);
                if (GUI.Button(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 20, 20), "Whitelist", buttonStyle))
                    modWorker.GenerateModControlFile(true, true);

                groupY += 22;

                if (GUI.Button(new Rect(descWidth + sepWidth, groupY, windowRect.width - (descWidth + sepWidth) - 20, 20), "Blacklist", buttonStyle))
                    modWorker.GenerateModControlFile(false, true);

                GUI.EndGroup();
                windowY += 47;

                GUI.Box(new Rect(2, windowY, windowRect.width - 4, 20), "Other", sectionHeaderStyle);
                windowY += 22;

                GUI.BeginGroup(new Rect(10, windowY, windowRect.width - 20, 200));
                groupY = 0;

                bool toggleCompression = GUI.Toggle(new Rect(0, groupY, windowRect.width - 20, 20), dmpSettings.compressionEnabled, "Compress Network Traffic");
                if (toggleCompression != dmpSettings.compressionEnabled)
                {
                    dmpSettings.compressionEnabled = toggleCompression;
                    dmpSettings.SaveSettings();
                }
                groupY += 22;

                bool toggleRevert = GUI.Toggle(new Rect(0, groupY, windowRect.width - 20, 20), dmpSettings.revertEnabled, "Enable Revert");
                if (toggleRevert != dmpSettings.revertEnabled)
                {
                    dmpSettings.revertEnabled = toggleRevert;
                    dmpSettings.SaveSettings();
                }
                groupY += 22;

                if (GUI.Button(new Rect(0, groupY, windowRect.width - 20, 20), interpolatorMode, buttonStyle))
                {
                    int newSetting = (int)dmpSettings.interpolatorType + 1;
                    //Overflow to 0
                    if (!Enum.IsDefined(typeof(InterpolatorType), newSetting))
                    {
                        newSetting = 0;
                    }
                    dmpSettings.interpolatorType = (InterpolatorType)newSetting;
                    dmpSettings.SaveSettings();
                    UpdateInterpolatorString();
                }

                groupY += 22;

                universeConverterWindow.display = GUI.Toggle(new Rect(0, groupY, windowRect.width - 20, 20), universeConverterWindow.display, "Generate DMP universe from saved game...", buttonStyle);
                groupY += 22;

                if (GUI.Button(new Rect(0, groupY, windowRect.width - 20, 20), "Reset Disclaimer", buttonStyle))
                {
                    dmpSettings.disclaimerAccepted = 0;
                    dmpSettings.SaveSettings();
                }
                groupY += 22;

                if (GUI.Button(new Rect(0, groupY, windowRect.width - 20, 20), "Reset Serverlist Disclaimer"))
                {
                    dmpSettings.serverlistMode = 0;
                    dmpSettings.SaveSettings();
                    serverListDisclaimerWindow.SpawnDialog();
                }
                groupY += 22;

                if (GUI.Button(new Rect(0, groupY, windowRect.width - 20, 20), toolbarMode, buttonStyle))
                {
                    int newSetting = (int)dmpSettings.toolbarType + 1;
                    //Overflow to 0
                    if (!Enum.IsDefined(typeof(DMPToolbarType), newSetting))
                    {
                        newSetting = 0;
                    }
                    dmpSettings.toolbarType = (DMPToolbarType)newSetting;
                    dmpSettings.SaveSettings();
                    UpdateToolbarString();
                    toolbarSupport.DetectSettingsChange();
                }

                groupY += 22;

                showDebugWindow = GUI.Toggle(new Rect(0, groupY, windowRect.width - 20, 20), showDebugWindow, "Show debug window", buttonStyle);


#if DEBUG
                groupY += 22;
                if (GUI.Button(new Rect(0, groupY, windowRect.width - 20, 20), "Check missing parts", buttonStyle))
                {
                    modWorker.CheckCommonStockParts();
                }

#endif
                groupY += 22;
                GUI.EndGroup();
            }
            if (selectedTab == OptionsTab.AGENCY)
            {
                DrawAgencyTab(windowY);
            }
        }

        private void DrawAgencyTab(int windowY)
        {
            GUI.BeginGroup(new Rect(10, windowY, windowRect.width - 20, 300));
            int groupY = 0;
            int contentWidth = (int)windowRect.width - 20;

            GUI.Box(new Rect(0, groupY, contentWidth, 20), "Server Agency", sectionHeaderStyle);
            groupY += 24;

            bool displayAgencyProgression = GUI.Toggle(new Rect(0, groupY, contentWidth, 20), dmpGame.displayAgencyProgression, "Show Space Agency Window");
            if (displayAgencyProgression != dmpGame.displayAgencyProgression)
            {
                dmpGame.displayAgencyProgression = displayAgencyProgression;
                DarkLog.Debug("Agency progression window display set to " + displayAgencyProgression);
            }
            groupY += 24;

            if (!dmpGame.displayAgencyProgression)
            {
                GUI.Label(new Rect(0, groupY, contentWidth, 48), "Agency Mode is server controlled. Enable the window to review objectives, progress, and rewards.", noteStyle);
                GUI.EndGroup();
                return;
            }

            string packName = dmpGame.agencyProgressionWorker.PackName;
            if (string.IsNullOrEmpty(packName))
            {
                packName = "Server Agency";
            }
            GUI.Label(new Rect(0, groupY, contentWidth, 20), packName, sectionHeaderStyle);
            groupY += 24;

            AgencyObjectiveSummary[] objectives = dmpGame.agencyProgressionWorker.Objectives;
            GUI.Label(new Rect(0, groupY, contentWidth, 60), "Space Agency window is open with " + objectives.Length + " server objective(s). Close it from the window or turn this toggle off.", noteStyle);
            GUI.EndGroup();
        }

        private void DrawAgencyFilterRow(int contentWidth, ref int groupY)
        {
            int buttonWidth = contentWidth / 6;
            DrawAgencyFilterButton(AgencyObjectiveFilter.ALL, "All", 0, buttonWidth, groupY);
            DrawAgencyFilterButton(AgencyObjectiveFilter.AVAILABLE, "Open", buttonWidth, buttonWidth, groupY);
            DrawAgencyFilterButton(AgencyObjectiveFilter.ACTIVE, "Active", buttonWidth * 2, buttonWidth, groupY);
            DrawAgencyFilterButton(AgencyObjectiveFilter.COMPLETED, "Done", buttonWidth * 3, buttonWidth, groupY);
            DrawAgencyFilterButton(AgencyObjectiveFilter.LOCKED, "Locked", buttonWidth * 4, buttonWidth, groupY);
            DrawAgencyFilterButton(AgencyObjectiveFilter.SHARED, "Shared", buttonWidth * 5, contentWidth - (buttonWidth * 5), groupY);
            groupY += 24;
        }

        private void DrawAgencyFilterButton(AgencyObjectiveFilter filter, string label, int x, int width, int groupY)
        {
            string displayLabel = agencyObjectiveFilter == filter ? "> " + label : label;
            if (GUI.Button(new Rect(x, groupY, width, 20), displayLabel, buttonStyle))
            {
                agencyObjectiveFilter = filter;
                agencyObjectivePage = 0;
                selectedAgencyObjectiveId = null;
            }
        }

        private AgencyObjectiveSummary[] FilterAgencyObjectives(AgencyObjectiveSummary[] objectives)
        {
            if (agencyObjectiveFilter == AgencyObjectiveFilter.ALL)
            {
                return objectives;
            }

            List<AgencyObjectiveSummary> filteredObjectives = new List<AgencyObjectiveSummary>();
            foreach (AgencyObjectiveSummary objective in objectives)
            {
                if (MatchesAgencyFilter(objective))
                {
                    filteredObjectives.Add(objective);
                }
            }
            return filteredObjectives.ToArray();
        }

        private bool MatchesAgencyFilter(AgencyObjectiveSummary objective)
        {
            switch (agencyObjectiveFilter)
            {
                case AgencyObjectiveFilter.AVAILABLE:
                    return MatchesAgencyText(objective.status, "available");
                case AgencyObjectiveFilter.ACTIVE:
                    return MatchesAgencyText(objective.status, "active") || MatchesAgencyText(objective.status, "in progress");
                case AgencyObjectiveFilter.COMPLETED:
                    return MatchesAgencyText(objective.status, "complete");
                case AgencyObjectiveFilter.LOCKED:
                    return MatchesAgencyText(objective.status, "locked") || MatchesAgencyText(objective.status, "hidden");
                case AgencyObjectiveFilter.SHARED:
                    return MatchesAgencyText(objective.scope, "server") || MatchesAgencyText(objective.scope, "shared") || MatchesAgencyText(objective.scope, "community");
                default:
                    return true;
            }
        }

        private bool MatchesAgencyText(string value, string match)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private AgencyObjectiveSummary GetSelectedAgencyObjective(AgencyObjectiveSummary[] objectives)
        {
            AgencyObjectiveSummary firstObjective = objectives[0];
            if (string.IsNullOrEmpty(selectedAgencyObjectiveId))
            {
                selectedAgencyObjectiveId = firstObjective.id;
                return firstObjective;
            }

            for (int i = 0; i < objectives.Length; i++)
            {
                if (objectives[i].id == selectedAgencyObjectiveId)
                {
                    agencyObjectivePage = i / AGENCY_OBJECTIVES_PER_PAGE;
                    return objectives[i];
                }
            }

            selectedAgencyObjectiveId = firstObjective.id;
            agencyObjectivePage = 0;
            return firstObjective;
        }

        private void DrawAgencyMissionList(AgencyObjectiveSummary[] objectives, int contentWidth, ref int groupY)
        {
            GUI.Box(new Rect(0, groupY, contentWidth, 20), "Missions", sectionHeaderStyle);
            groupY += 22;

            int lastPage = (objectives.Length - 1) / AGENCY_OBJECTIVES_PER_PAGE;
            if (agencyObjectivePage > lastPage)
            {
                agencyObjectivePage = lastPage;
            }
            if (agencyObjectivePage < 0)
            {
                agencyObjectivePage = 0;
            }

            int startIndex = agencyObjectivePage * AGENCY_OBJECTIVES_PER_PAGE;
            int endIndex = Math.Min(objectives.Length, startIndex + AGENCY_OBJECTIVES_PER_PAGE);
            for (int i = startIndex; i < endIndex; i++)
            {
                AgencyObjectiveSummary objective = objectives[i];
                string label = BuildAgencyMissionListLabel(objective);
                if (objective.id == selectedAgencyObjectiveId)
                {
                    label = "> " + label;
                }
                if (GUI.Button(new Rect(0, groupY, contentWidth, 22), label, buttonStyle))
                {
                    selectedAgencyObjectiveId = objective.id;
                }
                groupY += 24;
            }

            if (objectives.Length > AGENCY_OBJECTIVES_PER_PAGE)
            {
                int buttonWidth = 64;
                if (GUI.Button(new Rect(0, groupY, buttonWidth, 20), "Prev", buttonStyle) && agencyObjectivePage > 0)
                {
                    agencyObjectivePage--;
                    selectedAgencyObjectiveId = objectives[agencyObjectivePage * AGENCY_OBJECTIVES_PER_PAGE].id;
                }
                GUI.Label(new Rect(buttonWidth, groupY, contentWidth - (buttonWidth * 2), 20), "Page " + (agencyObjectivePage + 1) + " / " + (lastPage + 1), noteStyle);
                if (GUI.Button(new Rect(contentWidth - buttonWidth, groupY, buttonWidth, 20), "Next", buttonStyle) && agencyObjectivePage < lastPage)
                {
                    agencyObjectivePage++;
                    selectedAgencyObjectiveId = objectives[agencyObjectivePage * AGENCY_OBJECTIVES_PER_PAGE].id;
                }
                groupY += 20;
            }
        }

        private void DrawAgencyMissionDetail(AgencyObjectiveSummary objective, int contentWidth, ref int groupY)
        {
            GUI.Box(new Rect(0, groupY, contentWidth, 20), "Mission Detail", sectionHeaderStyle);
            groupY += 22;

            GUI.Label(new Rect(0, groupY, contentWidth, 20), BuildAgencyMissionTitle(objective), descriptorStyle);
            groupY += 20;
            GUI.Label(new Rect(0, groupY, contentWidth, 20), BuildAgencyObjectiveMetadata(objective), noteStyle);
            groupY += 20;

            string progressSummary = BuildAgencyProgressSummary(objective);
            if (!string.IsNullOrEmpty(progressSummary))
            {
                GUI.Label(new Rect(0, groupY, contentWidth, 20), progressSummary, noteStyle);
                groupY += 20;
            }

            string rewardSummary = BuildAgencyRewardSummary(objective);
            if (!string.IsNullOrEmpty(rewardSummary))
            {
                GUI.Label(new Rect(0, groupY, contentWidth, 20), rewardSummary, noteStyle);
                groupY += 20;
            }

            GUI.Label(new Rect(0, groupY, contentWidth, 48), objective.description, noteStyle);
        }

        private string BuildAgencyMissionListLabel(AgencyObjectiveSummary objective)
        {
            string title = string.IsNullOrEmpty(objective.title) ? objective.id : objective.title;
            return "[" + objective.status + "] " + title;
        }

        private string BuildAgencyMissionTitle(AgencyObjectiveSummary objective)
        {
            string title = string.IsNullOrEmpty(objective.title) ? objective.id : objective.title;
            return title + " [" + objective.status + "]";
        }

        private string BuildAgencyProgressSummary(AgencyObjectiveSummary objective)
        {
            if (objective.progressTarget <= 0)
            {
                return string.Empty;
            }
            string progressSummary = "Progress: " + objective.progressValue.ToString("0.##") + " / " + objective.progressTarget.ToString("0.##");
            if (objective.progressValue >= objective.progressTarget)
            {
                progressSummary += " complete";
            }
            return progressSummary;
        }

        private string GetPlayerIdentityFingerprint()
        {
            if (dmpSettings == null || string.IsNullOrEmpty(dmpSettings.playerPublicKey))
            {
                return "Unavailable";
            }

            string hash = DarkMultiPlayerCommon.Common.CalculateSHA256Hash(Encoding.UTF8.GetBytes(dmpSettings.playerPublicKey));
            if (string.IsNullOrEmpty(hash) || hash.Length < 16)
            {
                return "Unavailable";
            }
            return hash.Substring(0, 4) + "-" + hash.Substring(4, 4) + "-" + hash.Substring(8, 4) + "-" + hash.Substring(12, 4);
        }

        private string GetPlayerUuidDisplay()
        {
            if (dmpSettings == null || string.IsNullOrEmpty(dmpSettings.playerUuid))
            {
                return "Unavailable";
            }
            if (dmpSettings.playerUuid.Length > 13)
            {
                return dmpSettings.playerUuid.Substring(0, 8) + "..." + dmpSettings.playerUuid.Substring(dmpSettings.playerUuid.Length - 4);
            }
            return dmpSettings.playerUuid;
        }

        private string GetPlayerUuidClipboardValue()
        {
            if (dmpSettings == null || string.IsNullOrEmpty(dmpSettings.playerUuid))
            {
                return string.Empty;
            }
            return dmpSettings.playerUuid;
        }

        private string BuildAgencyObjectiveMetadata(AgencyObjectiveSummary objective)
        {
            string contractType = string.IsNullOrEmpty(objective.contractType) ? "Objective" : objective.contractType;
            string scope = string.IsNullOrEmpty(objective.scope) ? "Server" : objective.scope;
            string issuer = string.IsNullOrEmpty(objective.issuer) ? "Server Agency" : objective.issuer;
            return contractType + " | " + scope + " | " + issuer;
        }

        private string BuildAgencyRewardSummary(AgencyObjectiveSummary objective)
        {
            string rewardSummary = string.Empty;
            if (objective.rewardFunds != 0)
            {
                rewardSummary = "Funds " + objective.rewardFunds.ToString("0.##");
            }
            if (objective.rewardScience != 0)
            {
                rewardSummary = AppendAgencyReward(rewardSummary, "Science " + objective.rewardScience.ToString("0.##"));
            }
            if (objective.rewardReputation != 0)
            {
                rewardSummary = AppendAgencyReward(rewardSummary, "Rep " + objective.rewardReputation.ToString("0.##"));
            }
            return string.IsNullOrEmpty(rewardSummary) ? string.Empty : "Rewards: " + rewardSummary;
        }

        private string AppendAgencyReward(string rewardSummary, string reward)
        {
            if (string.IsNullOrEmpty(rewardSummary))
            {
                return reward;
            }
            return rewardSummary + ", " + reward;
        }

        private void CheckWindowLock()
        {
            if (dmpGame != null && !dmpGame.running)
            {
                RemoveWindowLock();
                return;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                RemoveWindowLock();
                return;
            }

            if (safeDisplay)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                bool shouldLock = windowRect.Contains(mousePos);

                if (shouldLock && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "DMP_OptionsLock");
                    isWindowLocked = true;
                }
                if (!shouldLock && isWindowLocked)
                {
                    RemoveWindowLock();
                }
            }

            if (!safeDisplay && isWindowLocked)
            {
                RemoveWindowLock();
            }
        }

        private void RemoveWindowLock()
        {
            if (isWindowLocked)
            {
                isWindowLocked = false;
                InputLockManager.RemoveControlLock("DMP_OptionsLock");
            }
        }

        private string[] GetOptionsTabStrings()
        {
            System.Collections.Generic.List<string> stringList = new System.Collections.Generic.List<string>();
            foreach (OptionsTab enumVal in Enum.GetValues(typeof(OptionsTab)))
            {
                if (enumVal == OptionsTab.PLAYER) stringList.Add("Player");
                if (enumVal == OptionsTab.CACHE) stringList.Add("Cache");
                if (enumVal == OptionsTab.CONTROLS) stringList.Add("Keys");
                if (enumVal == OptionsTab.AGENCY && dmpGame != null && dmpGame.serverAgencyProgressionEnabled) stringList.Add("Agency");
                if (enumVal == OptionsTab.ADVANCED) stringList.Add("Advanced");
            }
            return stringList.ToArray();
        }

        public void Stop()
        {
            networkWorker = null;
            playerColorWorker = null;
            dmpGame = null;
        }
    }
}
