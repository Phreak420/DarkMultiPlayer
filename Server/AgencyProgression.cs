using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DarkMultiPlayerServer
{
    public static class AgencyProgression
    {
        private const int MaxObjectives = 100;
        private static readonly List<AgencyObjective> objectives = new List<AgencyObjective>();

        public static string PackName { get; private set; }

        public static AgencyObjective[] Objectives
        {
            get
            {
                lock (objectives)
                {
                    return objectives.ToArray();
                }
            }
        }

        public static void Load(bool enabled)
        {
            lock (objectives)
            {
                objectives.Clear();
                PackName = string.Empty;
            }

            if (!enabled)
            {
                return;
            }

            string agencyFile = Path.Combine(Server.configDirectory, "AgencyProgression.json");
            Directory.CreateDirectory(Server.configDirectory);
            if (!File.Exists(agencyFile))
            {
                WriteDefaultFile(agencyFile);
            }

            AgencyProgressionFile agencyFileData = ReadAgencyFile(agencyFile);
            if (agencyFileData == null)
            {
                DarkLog.Error("Agency progression file could not be loaded. No agency objectives are active.");
                return;
            }

            PackName = CleanText(agencyFileData.packName, "Server Agency");
            if (agencyFileData.objectives == null)
            {
                DarkLog.Normal("Loaded agency progression pack '" + PackName + "' with 0 objectives.");
                return;
            }

            lock (objectives)
            {
                foreach (AgencyObjective objective in agencyFileData.objectives)
                {
                    if (objectives.Count >= MaxObjectives)
                    {
                        DarkLog.Error("Agency progression objective limit reached. Extra objectives were ignored.");
                        break;
                    }
                    if (objective == null || string.IsNullOrEmpty(objective.id))
                    {
                        DarkLog.Error("Skipped agency progression objective with an empty id.");
                        continue;
                    }
                    objectives.Add(new AgencyObjective
                    {
                        id = CleanText(objective.id, string.Empty),
                        title = CleanText(objective.title, objective.id),
                        description = CleanText(objective.description, string.Empty),
                        status = CleanText(objective.status, "Available"),
                        scope = CleanText(objective.scope, "Personal")
                    });
                }
            }

            DarkLog.Normal("Loaded agency progression pack '" + PackName + "' with " + Objectives.Length + " objectives.");
        }

        private static AgencyProgressionFile ReadAgencyFile(string agencyFile)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AgencyProgressionFile));
                using (FileStream fs = File.OpenRead(agencyFile))
                {
                    return (AgencyProgressionFile)serializer.ReadObject(fs);
                }
            }
            catch (Exception e)
            {
                DarkLog.Error("Error loading agency progression file '" + agencyFile + "': " + e);
                return null;
            }
        }

        private static void WriteDefaultFile(string agencyFile)
        {
            AgencyProgressionFile defaultFile = new AgencyProgressionFile
            {
                packName = "Server Agency",
                objectives = new AgencyObjective[]
                {
                    new AgencyObjective
                    {
                        id = "reach-orbit",
                        title = "Reach Kerbin Orbit",
                        description = "Place a crewed or uncrewed vessel into a stable Kerbin orbit.",
                        status = "Available",
                        scope = "Personal"
                    },
                    new AgencyObjective
                    {
                        id = "mun-flyby",
                        title = "Fly By the Mun",
                        description = "Send a vessel through the Mun's sphere of influence and return useful mission data.",
                        status = "Locked",
                        scope = "Server"
                    }
                }
            };

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AgencyProgressionFile));
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, defaultFile);
                string json = Encoding.UTF8.GetString(ms.ToArray());
                File.WriteAllText(agencyFile, json);
            }
        }

        private static string CleanText(string value, string fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }
            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }

    [DataContract]
    public class AgencyProgressionFile
    {
        [DataMember]
        public string packName;

        [DataMember]
        public AgencyObjective[] objectives;
    }

    [DataContract]
    public class AgencyObjective
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
        public string scope;
    }
}
