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
        private static readonly List<CampaignMetric> metrics = new List<CampaignMetric>();
        private static readonly List<CampaignPhase> phases = new List<CampaignPhase>();
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
                            description = CleanText(phase.description, string.Empty)
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
            }

            SaveState();
            DarkLog.Normal("Loaded campaign state '" + CampaignName + "' with " + Metrics.Length + " metrics and " + Phases.Length + " phases.");
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
                return true;
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
            return CampaignName + " phase=" + CurrentPhaseId + " " + phaseTitle + " metrics=" + Metrics.Length;
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

        private static void SaveState()
        {
            Directory.CreateDirectory(GetStateDirectory());
            List<string> lines = new List<string>();
            lines.Add("currentPhaseId=" + CurrentPhaseId);
            foreach (CampaignMetric metric in metrics)
            {
                lines.Add("metric." + metric.id + "=" + metric.value.ToString("R"));
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
                        description = "Build early agency capability and orbital infrastructure."
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
}
