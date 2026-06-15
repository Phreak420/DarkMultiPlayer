using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DarkMultiPlayerServer
{
    public static class EconomyState
    {
        private const int MaxResources = 50;
        private static readonly List<EconomyResource> resources = new List<EconomyResource>();
        private static readonly object stateLock = new object();

        public static string EconomyName { get; private set; }

        public static EconomyResource[] Resources
        {
            get
            {
                lock (stateLock)
                {
                    List<EconomyResource> view = new List<EconomyResource>();
                    foreach (EconomyResource resource in resources)
                    {
                        view.Add(CloneResource(resource));
                    }
                    return view.ToArray();
                }
            }
        }

        public static void Load(bool enabled)
        {
            lock (stateLock)
            {
                resources.Clear();
                EconomyName = string.Empty;
            }

            if (!enabled)
            {
                return;
            }

            string configFile = Path.Combine(Server.configDirectory, "EconomyState.json");
            Directory.CreateDirectory(Server.configDirectory);
            if (!File.Exists(configFile))
            {
                WriteDefaultFile(configFile);
            }

            EconomyStateFile economyFile = ReadEconomyFile(configFile);
            if (economyFile == null)
            {
                DarkLog.Error("Economy state file could not be loaded. Economy state is inactive.");
                return;
            }

            Dictionary<string, string> persistedState = ReadStateFile();
            lock (stateLock)
            {
                EconomyName = CleanText(economyFile.economyName, "Server Economy");
                if (economyFile.resources != null)
                {
                    foreach (EconomyResource resource in economyFile.resources)
                    {
                        if (resources.Count >= MaxResources)
                        {
                            DarkLog.Error("Economy resource limit reached. Extra resources were ignored.");
                            break;
                        }
                        if (resource == null || string.IsNullOrEmpty(resource.id) || !SafeFile.IsNameSafe(resource.id))
                        {
                            DarkLog.Error("Skipped economy resource with an empty or unsafe id.");
                            continue;
                        }

                        double minValue = resource.minValue;
                        double maxValue = resource.maxValue <= minValue ? minValue : resource.maxValue;
                        double value = Clamp(resource.value, minValue, maxValue);
                        string persistedValue;
                        if (persistedState.TryGetValue("resource." + resource.id, out persistedValue))
                        {
                            double parsedValue;
                            if (double.TryParse(persistedValue, out parsedValue))
                            {
                                value = Clamp(parsedValue, minValue, maxValue);
                            }
                        }

                        resources.Add(new EconomyResource
                        {
                            id = resource.id,
                            title = CleanText(resource.title, resource.id),
                            category = CleanText(resource.category, "General"),
                            value = value,
                            minValue = minValue,
                            maxValue = maxValue,
                            unit = CleanText(resource.unit, string.Empty),
                            scarcityThreshold = Clamp(resource.scarcityThreshold, minValue, maxValue),
                            abundanceThreshold = Clamp(resource.abundanceThreshold, minValue, maxValue),
                            maxPositiveModifier = Clamp(Math.Abs(resource.maxPositiveModifier), 0, 1),
                            maxNegativeModifier = Clamp(Math.Abs(resource.maxNegativeModifier), 0, 1),
                            recoveryContractHint = CleanText(resource.recoveryContractHint, string.Empty)
                        });
                    }
                }
            }

            SaveState();
            DarkLog.Normal("Loaded economy state '" + EconomyName + "' with " + Resources.Length + " resources.");
        }

        public static bool SetResource(string resourceId, double value, string actor)
        {
            if (!IsResourceIdSafe(resourceId))
            {
                return false;
            }
            lock (stateLock)
            {
                EconomyResource resource = FindResource(resourceId);
                if (resource == null)
                {
                    return false;
                }
                double previousValue = resource.value;
                resource.value = Clamp(value, resource.minValue, resource.maxValue);
                SaveState();
                RecordAudit("resource-set", actor, resourceId, "previous=" + previousValue.ToString("R") + ";value=" + resource.value.ToString("R"));
                return true;
            }
        }

        public static bool AdjustResource(string resourceId, double delta, string actor)
        {
            if (!IsResourceIdSafe(resourceId))
            {
                return false;
            }
            lock (stateLock)
            {
                EconomyResource resource = FindResource(resourceId);
                if (resource == null)
                {
                    return false;
                }
                double previousValue = resource.value;
                resource.value = Clamp(resource.value + delta, resource.minValue, resource.maxValue);
                SaveState();
                RecordAudit("resource-adjusted", actor, resourceId, "previous=" + previousValue.ToString("R") + ";delta=" + delta.ToString("R") + ";value=" + resource.value.ToString("R"));
                return true;
            }
        }

        public static bool TryGetResourceValue(string resourceId, out double value)
        {
            value = 0;
            if (!IsResourceIdSafe(resourceId))
            {
                return false;
            }
            lock (stateLock)
            {
                EconomyResource resource = FindResource(resourceId);
                if (resource == null)
                {
                    return false;
                }
                value = resource.value;
                return true;
            }
        }

        public static bool TryGetResource(string resourceId, out EconomyResource resource)
        {
            resource = null;
            if (!IsResourceIdSafe(resourceId))
            {
                return false;
            }
            lock (stateLock)
            {
                EconomyResource foundResource = FindResource(resourceId);
                if (foundResource == null)
                {
                    return false;
                }
                resource = CloneResource(foundResource);
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
                    string backupFile = Path.Combine(GetStateDirectory(), "EconomyState.reset-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak");
                    File.Copy(stateFile, backupFile, false);
                    File.Delete(stateFile);
                    RecordAudit("reset", actor, "economy", "backup=" + Path.GetFileName(backupFile));
                }
            }
            Load(Settings.IsAgencyProgressionActive());
            return true;
        }

        public static string FormatStatus()
        {
            return EconomyName + " resources=" + Resources.Length;
        }

        public static string GetResourceState(EconomyResource resource)
        {
            if (resource.value <= resource.scarcityThreshold)
            {
                return "Scarce";
            }
            if (resource.abundanceThreshold > resource.scarcityThreshold && resource.value >= resource.abundanceThreshold)
            {
                return "Abundant";
            }
            return "Stable";
        }

        public static double GetBoundedModifier(EconomyResource resource)
        {
            string state = GetResourceState(resource);
            if (state == "Scarce")
            {
                return resource.maxPositiveModifier;
            }
            if (state == "Abundant")
            {
                return -resource.maxNegativeModifier;
            }
            return 0;
        }

        private static EconomyResource FindResource(string resourceId)
        {
            foreach (EconomyResource resource in resources)
            {
                if (string.Equals(resource.id, resourceId, StringComparison.OrdinalIgnoreCase))
                {
                    return resource;
                }
            }
            return null;
        }

        private static EconomyResource CloneResource(EconomyResource resource)
        {
            EconomyResource clone = new EconomyResource
            {
                id = resource.id,
                title = resource.title,
                category = resource.category,
                value = resource.value,
                minValue = resource.minValue,
                maxValue = resource.maxValue,
                unit = resource.unit,
                scarcityThreshold = resource.scarcityThreshold,
                abundanceThreshold = resource.abundanceThreshold,
                maxPositiveModifier = resource.maxPositiveModifier,
                maxNegativeModifier = resource.maxNegativeModifier,
                recoveryContractHint = resource.recoveryContractHint
            };
            clone.state = GetResourceState(resource);
            clone.boundedModifier = GetBoundedModifier(resource);
            return clone;
        }

        private static void SaveState()
        {
            Directory.CreateDirectory(GetStateDirectory());
            List<string> lines = new List<string>();
            foreach (EconomyResource resource in resources)
            {
                lines.Add("resource." + resource.id + "=" + resource.value.ToString("R"));
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
                DarkLog.Debug("Failed to record economy state audit entry: " + e);
            }
        }

        private static EconomyStateFile ReadEconomyFile(string economyFile)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(EconomyStateFile));
                using (FileStream fs = File.OpenRead(economyFile))
                {
                    return (EconomyStateFile)serializer.ReadObject(fs);
                }
            }
            catch (Exception e)
            {
                DarkLog.Error("Error loading economy state file '" + economyFile + "': " + e);
                return null;
            }
        }

        private static void WriteDefaultFile(string economyFile)
        {
            EconomyStateFile defaultFile = new EconomyStateFile
            {
                economyName = "Server Economy",
                resources = new EconomyResource[]
                {
                    new EconomyResource
                    {
                        id = "fuel-reserve",
                        title = "Fuel Reserve",
                        category = "Logistics",
                        value = 75,
                        minValue = 0,
                        maxValue = 100,
                        unit = "%",
                        scarcityThreshold = 25,
                        abundanceThreshold = 85,
                        maxPositiveModifier = 0.15,
                        maxNegativeModifier = 0.05,
                        recoveryContractHint = "Offer premium fuel delivery contracts when reserves are low."
                    },
                    new EconomyResource
                    {
                        id = "materials-stockpile",
                        title = "Materials Stockpile",
                        category = "Construction",
                        value = 60,
                        minValue = 0,
                        maxValue = 100,
                        unit = "%",
                        scarcityThreshold = 20,
                        abundanceThreshold = 90,
                        maxPositiveModifier = 0.10,
                        maxNegativeModifier = 0.05,
                        recoveryContractHint = "Offer infrastructure and resupply missions when materials are scarce."
                    }
                }
            };

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(EconomyStateFile));
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, defaultFile);
                File.WriteAllText(economyFile, Encoding.UTF8.GetString(ms.ToArray()));
            }
        }

        private static string GetStateDirectory()
        {
            return Path.Combine(Server.universeDirectory, "EconomyState");
        }

        private static string GetStateFile()
        {
            return Path.Combine(GetStateDirectory(), "EconomyState.txt");
        }

        private static string GetAuditFile()
        {
            return Path.Combine(GetStateDirectory(), "EconomyAudit.log");
        }

        private static bool IsResourceIdSafe(string value)
        {
            return !string.IsNullOrEmpty(value) && SafeFile.IsNameSafe(value);
        }

        private static double Clamp(double value, double minValue, double maxValue)
        {
            if (value < minValue)
            {
                return minValue;
            }
            if (value > maxValue)
            {
                return maxValue;
            }
            return value;
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
    public class EconomyStateFile
    {
        [DataMember]
        public string economyName;

        [DataMember]
        public EconomyResource[] resources;
    }

    [DataContract]
    public class EconomyResource
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
        public double minValue;

        [DataMember]
        public double maxValue;

        [DataMember]
        public string unit;

        [DataMember]
        public double scarcityThreshold;

        [DataMember]
        public double abundanceThreshold;

        [DataMember]
        public double maxPositiveModifier;

        [DataMember]
        public double maxNegativeModifier;

        [DataMember]
        public string recoveryContractHint;

        public string state;
        public double boundedModifier;
    }
}
