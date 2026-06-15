using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DarkMultiPlayerServer
{
    public static class CampaignState
    {
        private const int MaxMetrics = 50;
        private const int MaxPhases = 20;
        private const int MaxEvents = 50;
        private static readonly List<CampaignMetric> metrics = new List<CampaignMetric>();
        private static readonly List<CampaignPhase> phases = new List<CampaignPhase>();
        private static readonly List<CampaignEvent> events = new List<CampaignEvent>();
        private static readonly object stateLock = new object();

        public static string CampaignName { get; private set; }
        public static string CurrentPhaseId { get; private set; }

        public static CampaignMetric[] Metrics
        {
            get
            {
                lock (stateLock)
                {
                    return metrics.ToArray();
                }
            }
        }

        public static CampaignPhase[] Phases
        {
            get
            {
                lock (stateLock)
                {
                    return phases.ToArray();
                }
            }
        }

        public static CampaignEvent[] Events
        {
            get
            {
                lock (stateLock)
                {
                    return BuildEventView();
                }
            }
        }

        public static CampaignPhase CurrentPhase
        {
            get
            {
                lock (stateLock)
                {
                    return FindPhase(CurrentPhaseId);
                }
            }
        }

        public static void Load(bool enabled)
        {
            lock (stateLock)
            {
                metrics.Clear();
                phases.Clear();
                events.Clear();
                CampaignName = string.Empty;
                CurrentPhaseId = string.Empty;
            }

            if (!enabled)
            {
                return;
            }

            string configFile = Path.Combine(Server.configDirectory, "CampaignState.json");
            Directory.CreateDirectory(Server.configDirectory);
            if (!File.Exists(configFile))
            {
                WriteDefaultFile(configFile);
            }

            CampaignStateFile campaignFile = ReadCampaignFile(configFile);
            if (campaignFile == null)
            {
                DarkLog.Error("Campaign state file could not be loaded. Campaign world state is inactive.");
                return;
            }

            Dictionary<string, string> persistedState = ReadStateFile();
            lock (stateLock)
            {
                CampaignName = CleanText(campaignFile.campaignName, "Server Campaign");
                if (campaignFile.phases != null)
                {
                    foreach (CampaignPhase phase in campaignFile.phases)
                    {
                        if (phases.Count >= MaxPhases)
                        {
                            DarkLog.Error("Campaign phase limit reached. Extra phases were ignored.");
                            break;
                        }
                        if (phase == null || string.IsNullOrEmpty(phase.id) || !SafeFile.IsNameSafe(phase.id))
                        {
                            DarkLog.Error("Skipped campaign phase with an empty or unsafe id.");
                            continue;
                        }
                        phases.Add(new CampaignPhase
                        {
                            id = phase.id,
                            title = CleanText(phase.title, phase.id),
                            description = CleanText(phase.description, string.Empty),
                            autoAdvanceToPhaseId = CleanText(phase.autoAdvanceToPhaseId, string.Empty),
                            requiredObjectiveIds = CleanIds(phase.requiredObjectiveIds),
                            requiredMetricId = CleanText(phase.requiredMetricId, string.Empty),
                            requiredMetricMinimum = phase.requiredMetricMinimum
                        });
                    }
                }

                string configuredPhase = CleanText(campaignFile.currentPhaseId, string.Empty);
                string persistedPhase;
                CurrentPhaseId = persistedState.TryGetValue("currentPhaseId", out persistedPhase) ? persistedPhase : configuredPhase;
                if (string.IsNullOrEmpty(CurrentPhaseId) && phases.Count > 0)
                {
                    CurrentPhaseId = phases[0].id;
                }

                if (campaignFile.metrics != null)
                {
                    foreach (CampaignMetric metric in campaignFile.metrics)
                    {
                        if (metrics.Count >= MaxMetrics)
                        {
                            DarkLog.Error("Campaign metric limit reached. Extra metrics were ignored.");
                            break;
                        }
                        if (metric == null || string.IsNullOrEmpty(metric.id) || !SafeFile.IsNameSafe(metric.id))
                        {
                            DarkLog.Error("Skipped campaign metric with an empty or unsafe id.");
                            continue;
                        }

                        double value = metric.value;
                        string persistedValue;
                        if (persistedState.TryGetValue("metric." + metric.id, out persistedValue))
                        {
                            double.TryParse(persistedValue, out value);
                        }

                        metrics.Add(new CampaignMetric
                        {
                            id = metric.id,
                            title = CleanText(metric.title, metric.id),
                            category = CleanText(metric.category, "General"),
                            value = value,
                            target = Math.Max(0, metric.target),
                            unit = CleanText(metric.unit, string.Empty)
                        });
                    }
                }

                if (campaignFile.events != null)
                {
                    foreach (CampaignEvent campaignEvent in campaignFile.events)
                    {
                        if (events.Count >= MaxEvents)
                        {
                            DarkLog.Error("Campaign event limit reached. Extra events were ignored.");
                            break;
                        }
                        if (campaignEvent == null || string.IsNullOrEmpty(campaignEvent.id) || !SafeFile.IsNameSafe(campaignEvent.id))
                        {
                            DarkLog.Error("Skipped campaign event with an empty or unsafe id.");
                            continue;
                        }
                        string persistedStatus;
                        string status = persistedState.TryGetValue("event." + campaignEvent.id, out persistedStatus) ? persistedStatus : CleanEventStatus(campaignEvent.status);
                        events.Add(new CampaignEvent
                        {
                            id = campaignEvent.id,
                            title = CleanText(campaignEvent.title, campaignEvent.id),
                            description = CleanText(campaignEvent.description, string.Empty),
                            status = status,
                            startsAtPhase = CleanText(campaignEvent.startsAtPhase, string.Empty),
                            requiredMetricId = CleanText(campaignEvent.requiredMetricId, string.Empty),
                            requiredMetricMinimum = campaignEvent.requiredMetricMinimum,
                            objectiveIds = CleanIds(campaignEvent.objectiveIds)
                        });
                    }
                }
            }

            SaveState();
            EvaluateAutomation("load");
            DarkLog.Normal("Loaded campaign state '" + CampaignName + "' with " + Metrics.Length + " metrics, " + Phases.Length + " phases, and " + Events.Length + " events.");
        }

        public static bool SetMetric(string metricId, double value, string actor)
        {
            if (!IsMetricIdSafe(metricId))
            {
                return false;
            }
            lock (stateLock)
            {
                CampaignMetric metric = FindMetric(metricId);
                if (metric == null)
                {
                    return false;
                }
                double previousValue = metric.value;
                metric.value = value;
                SaveState();
                RecordAudit("metric-set", actor, metricId, "previous=" + previousValue.ToString("R") + ";value=" + value.ToString("R"));
                EvaluateAutomationLocked(actor);
                return true;
            }
        }

        public static bool TryGetMetricValue(string metricId, out double value)
        {
            value = 0;
            if (!IsMetricIdSafe(metricId))
            {
                return false;
            }
            lock (stateLock)
            {
                CampaignMetric metric = FindMetric(metricId);
                if (metric == null)
                {
                    return false;
                }
                value = metric.value;
                return true;
            }
        }

        public static bool AdvancePhase(string phaseId, string actor)
        {
            if (!IsMetricIdSafe(phaseId))
            {
                return false;
            }
            lock (stateLock)
            {
                CampaignPhase phase = FindPhase(phaseId);
                if (phase == null)
                {
                    return false;
                }
                string previousPhaseId = CurrentPhaseId;
                CurrentPhaseId = phase.id;
                SaveState();
                RecordAudit("phase-advanced", actor, phase.id, "previousPhaseId=" + previousPhaseId);
                EvaluateAutomationLocked(actor);
                return true;
            }
        }

        public static bool ActivateEvent(string eventId, string actor)
        {
            return SetEventStatus(eventId, "Active", actor, "event-activated");
        }

        public static bool CompleteEvent(string eventId, string actor)
        {
            return SetEventStatus(eventId, "Complete", actor, "event-completed");
        }

        public static bool IsEventActiveOrComplete(string eventId)
        {
            if (!IsMetricIdSafe(eventId))
            {
                return false;
            }
            lock (stateLock)
            {
                CampaignEvent campaignEvent = FindEvent(eventId);
                if (campaignEvent == null)
                {
                    return false;
                }
                string status = GetEventStatus(campaignEvent);
                return string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Complete", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool EvaluateAutomation(string actor)
        {
            lock (stateLock)
            {
                return EvaluateAutomationLocked(actor);
            }
        }

        public static bool ResetState(bool confirmed, string actor)
        {
            if (!confirmed)
            {
                return false;
            }
            lock (stateLock)
            {
                string stateFile = GetStateFile();
                if (File.Exists(stateFile))
                {
                    string backupFile = Path.Combine(GetStateDirectory(), "WorldState.reset-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak");
                    File.Copy(stateFile, backupFile, false);
                    File.Delete(stateFile);
                    RecordAudit("reset", actor, "campaign", "backup=" + Path.GetFileName(backupFile));
                }
            }
            Load(Settings.IsAgencyProgressionActive());
            return true;
        }

        public static string FormatStatus()
        {
            CampaignPhase phase = CurrentPhase;
            string phaseTitle = phase == null ? "(none)" : phase.title;
            return CampaignName + " phase=" + CurrentPhaseId + " " + phaseTitle + " metrics=" + Metrics.Length + " events=" + Events.Length;
        }

        private static bool SetEventStatus(string eventId, string status, string actor, string auditAction)
        {
            if (!IsMetricIdSafe(eventId))
            {
                return false;
            }
            lock (stateLock)
            {
                CampaignEvent campaignEvent = FindEvent(eventId);
                if (campaignEvent == null)
                {
                    return false;
                }
                string previousStatus = campaignEvent.status;
                campaignEvent.status = CleanEventStatus(status);
                SaveState();
                RecordAudit(auditAction, actor, eventId, "previousStatus=" + previousStatus + ";status=" + campaignEvent.status);
                return true;
            }
        }

        private static bool EvaluateAutomationLocked(string actor)
        {
            bool changed = false;
            CampaignPhase phase = FindPhase(CurrentPhaseId);
            if (phase != null && !string.IsNullOrEmpty(phase.autoAdvanceToPhaseId) && PhaseAutomationConditionsMet(phase))
            {
                CampaignPhase nextPhase = FindPhase(phase.autoAdvanceToPhaseId);
                if (nextPhase != null && !string.Equals(CurrentPhaseId, nextPhase.id, StringComparison.OrdinalIgnoreCase))
                {
                    string previousPhaseId = CurrentPhaseId;
                    CurrentPhaseId = nextPhase.id;
                    RecordAudit("phase-auto-advanced", actor, nextPhase.id, "previousPhaseId=" + previousPhaseId);
                    changed = true;
                }
            }

            foreach (CampaignEvent campaignEvent in events)
            {
                if (string.IsNullOrEmpty(campaignEvent.status) && EventConditionsMet(campaignEvent))
                {
                    campaignEvent.status = "Available";
                    RecordAudit("event-available", actor, campaignEvent.id, "phase=" + CurrentPhaseId);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveState();
            }
            return changed;
        }

        private static bool PhaseAutomationConditionsMet(CampaignPhase phase)
        {
            return MetricConditionMet(phase.requiredMetricId, phase.requiredMetricMinimum) && ObjectivesComplete(phase.requiredObjectiveIds);
        }

        private static bool EventConditionsMet(CampaignEvent campaignEvent)
        {
            if (!string.IsNullOrEmpty(campaignEvent.startsAtPhase) && !string.Equals(CurrentPhaseId, campaignEvent.startsAtPhase, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return MetricConditionMet(campaignEvent.requiredMetricId, campaignEvent.requiredMetricMinimum) && ObjectivesComplete(campaignEvent.objectiveIds);
        }

        private static bool MetricConditionMet(string metricId, double minimum)
        {
            if (string.IsNullOrEmpty(metricId))
            {
                return true;
            }
            CampaignMetric metric = FindMetric(metricId);
            return metric != null && metric.value >= minimum;
        }

        private static bool ObjectivesComplete(string[] objectiveIds)
        {
            if (objectiveIds == null || objectiveIds.Length == 0)
            {
                return true;
            }
            foreach (string objectiveId in objectiveIds)
            {
                if (!AgencyProgression.IsServerObjectiveComplete(objectiveId))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsMetricIdSafe(string value)
        {
            return !string.IsNullOrEmpty(value) && SafeFile.IsNameSafe(value);
        }

        private static CampaignMetric FindMetric(string metricId)
        {
            foreach (CampaignMetric metric in metrics)
            {
                if (string.Equals(metric.id, metricId, StringComparison.OrdinalIgnoreCase))
                {
                    return metric;
                }
            }
            return null;
        }

        private static CampaignPhase FindPhase(string phaseId)
        {
            foreach (CampaignPhase phase in phases)
            {
                if (string.Equals(phase.id, phaseId, StringComparison.OrdinalIgnoreCase))
                {
                    return phase;
                }
            }
            return null;
        }

        private static CampaignEvent FindEvent(string eventId)
        {
            foreach (CampaignEvent campaignEvent in events)
            {
                if (string.Equals(campaignEvent.id, eventId, StringComparison.OrdinalIgnoreCase))
                {
                    return campaignEvent;
                }
            }
            return null;
        }

        private static CampaignEvent[] BuildEventView()
        {
            List<CampaignEvent> view = new List<CampaignEvent>();
            foreach (CampaignEvent campaignEvent in events)
            {
                view.Add(new CampaignEvent
                {
                    id = campaignEvent.id,
                    title = campaignEvent.title,
                    description = campaignEvent.description,
                    status = GetEventStatus(campaignEvent),
                    startsAtPhase = campaignEvent.startsAtPhase,
                    requiredMetricId = campaignEvent.requiredMetricId,
                    requiredMetricMinimum = campaignEvent.requiredMetricMinimum,
                    objectiveIds = campaignEvent.objectiveIds
                });
            }
            return view.ToArray();
        }

        private static string GetEventStatus(CampaignEvent campaignEvent)
        {
            string status = CleanEventStatus(campaignEvent.status);
            if (!string.IsNullOrEmpty(status))
            {
                return status;
            }
            return EventConditionsMet(campaignEvent) ? "Available" : "Locked";
        }

        private static void SaveState()
        {
            Directory.CreateDirectory(GetStateDirectory());
            List<string> lines = new List<string>();
            lines.Add("currentPhaseId=" + CurrentPhaseId);
            foreach (CampaignMetric metric in metrics)
            {
                lines.Add("metric." + metric.id + "=" + metric.value.ToString("R"));
            }
            foreach (CampaignEvent campaignEvent in events)
            {
                lines.Add("event." + campaignEvent.id + "=" + CleanEventStatus(campaignEvent.status));
            }
            File.WriteAllLines(GetStateFile(), lines.ToArray());
        }

        private static Dictionary<string, string> ReadStateFile()
        {
            Dictionary<string, string> state = new Dictionary<string, string>();
            string stateFile = GetStateFile();
            if (!File.Exists(stateFile))
            {
                return state;
            }

            foreach (string line in File.ReadAllLines(stateFile))
            {
                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }
                state[line.Substring(0, separatorIndex)] = line.Substring(separatorIndex + 1);
            }
            return state;
        }

        private static void RecordAudit(string action, string actor, string target, string details)
        {
            try
            {
                Directory.CreateDirectory(GetStateDirectory());
                using (StreamWriter sw = new StreamWriter(GetAuditFile(), true))
                {
                    sw.WriteLine(DateTime.UtcNow.ToString("o") + "|" + CleanAudit(action) + "|" + CleanAudit(actor) + "|" + CleanAudit(target) + "|" + CleanAudit(details));
                }
            }
            catch (Exception e)
            {
                DarkLog.Debug("Failed to record campaign state audit entry: " + e);
            }
        }

        private static CampaignStateFile ReadCampaignFile(string campaignFile)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CampaignStateFile));
                using (FileStream fs = File.OpenRead(campaignFile))
                {
                    return (CampaignStateFile)serializer.ReadObject(fs);
                }
            }
            catch (Exception e)
            {
                DarkLog.Error("Error loading campaign state file '" + campaignFile + "': " + e);
                return null;
            }
        }

        private static void WriteDefaultFile(string campaignFile)
        {
            CampaignStateFile defaultFile = new CampaignStateFile
            {
                campaignName = "Server Campaign",
                currentPhaseId = "kerbin-foundation",
                phases = new CampaignPhase[]
                {
                    new CampaignPhase
                    {
                        id = "kerbin-foundation",
                        title = "Kerbin Foundation",
                        description = "Build early agency capability and orbital infrastructure.",
                        autoAdvanceToPhaseId = "mun-expansion",
                        requiredMetricId = "survey-progress",
                        requiredMetricMinimum = 25
                    },
                    new CampaignPhase
                    {
                        id = "mun-expansion",
                        title = "Mun Expansion",
                        description = "Expand shared operations beyond Kerbin orbit."
                    }
                },
                metrics = new CampaignMetric[]
                {
                    new CampaignMetric
                    {
                        id = "survey-progress",
                        title = "Survey Progress",
                        category = "Exploration",
                        value = 0,
                        target = 100,
                        unit = "%"
                    },
                    new CampaignMetric
                    {
                        id = "communications-strength",
                        title = "Communications Strength",
                        category = "Infrastructure",
                        value = 0,
                        target = 100,
                        unit = "%"
                    }
                },
                events = new CampaignEvent[]
                {
                    new CampaignEvent
                    {
                        id = "relay-buildout",
                        title = "Kerbin Relay Buildout",
                        description = "Mission Control is prioritizing communications infrastructure around Kerbin.",
                        startsAtPhase = "kerbin-foundation",
                        requiredMetricId = "communications-strength",
                        requiredMetricMinimum = 10,
                        objectiveIds = new string[] { "reach-orbit" }
                    }
                }
            };

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CampaignStateFile));
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, defaultFile);
                File.WriteAllText(campaignFile, Encoding.UTF8.GetString(ms.ToArray()));
            }
        }

        private static string GetStateDirectory()
        {
            return Path.Combine(Server.universeDirectory, "CampaignState");
        }

        private static string GetStateFile()
        {
            return Path.Combine(GetStateDirectory(), "WorldState.txt");
        }

        private static string GetAuditFile()
        {
            return Path.Combine(GetStateDirectory(), "CampaignAudit.log");
        }

        private static string CleanText(string value, string fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }
            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string CleanEventStatus(string status)
        {
            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return "Active";
            }
            if (string.Equals(status, "Complete", StringComparison.OrdinalIgnoreCase))
            {
                return "Complete";
            }
            if (string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase))
            {
                return "Available";
            }
            return string.Empty;
        }

        private static string[] CleanIds(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return new string[0];
            }
            List<string> cleanIds = new List<string>();
            foreach (string id in ids)
            {
                if (SafeFile.IsNameSafe(id))
                {
                    cleanIds.Add(id);
                }
                else
                {
                    DarkLog.Error("Skipped unsafe campaign objective id '" + id + "'.");
                }
            }
            return cleanIds.ToArray();
        }

        private static string CleanAudit(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Replace("\r", "").Replace("\n", "").Replace("|", "");
        }
    }

    [DataContract]
    public class CampaignStateFile
    {
        [DataMember]
        public string campaignName;

        [DataMember]
        public string currentPhaseId;

        [DataMember]
        public CampaignPhase[] phases;

        [DataMember]
        public CampaignMetric[] metrics;

        [DataMember]
        public CampaignEvent[] events;
    }

    [DataContract]
    public class CampaignPhase
    {
        [DataMember]
        public string id;

        [DataMember]
        public string title;

        [DataMember]
        public string description;

        [DataMember]
        public string autoAdvanceToPhaseId;

        [DataMember]
        public string[] requiredObjectiveIds;

        [DataMember]
        public string requiredMetricId;

        [DataMember]
        public double requiredMetricMinimum;
    }

    [DataContract]
    public class CampaignMetric
    {
        [DataMember]
        public string id;

        [DataMember]
        public string title;

        [DataMember]
        public string category;

        [DataMember]
        public double value;

        [DataMember]
        public double target;

        [DataMember]
        public string unit;
    }

    [DataContract]
    public class CampaignEvent
    {
        [DataMember]
        public string id;

        [DataMember]
        public string title;

        [DataMember]
        public string description;

        [DataMember]
        public string status;

        [DataMember]
        public string startsAtPhase;

        [DataMember]
        public string requiredMetricId;

        [DataMember]
        public double requiredMetricMinimum;

        [DataMember]
        public string[] objectiveIds;
    }
}
