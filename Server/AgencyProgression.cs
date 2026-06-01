using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer
{
    public static class AgencyProgression
    {
        private const int MaxObjectives = 100;
        private const int MaxEvidenceIdLength = 128;
        private const string CompleteStatus = "Complete";
        private static readonly TimeSpan EvidenceRateLimit = TimeSpan.FromSeconds(1);
        private static readonly List<AgencyObjective> objectives = new List<AgencyObjective>();
        private static readonly Dictionary<string, AgencyObjectiveCompletion> completions = new Dictionary<string, AgencyObjectiveCompletion>();
        private static readonly Dictionary<string, long> lastEvidenceReceiveTicks = new Dictionary<string, long>();

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
            lock (completions)
            {
                completions.Clear();
            }
            lock (lastEvidenceReceiveTicks)
            {
                lastEvidenceReceiveTicks.Clear();
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

            LoadCompletions();

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
                    AgencyObjectiveCompletion completion = GetCompletion(objective.id);
                    objectives.Add(new AgencyObjective
                    {
                        id = CleanText(objective.id, string.Empty),
                        title = CleanText(objective.title, objective.id),
                        description = CleanText(objective.description, string.Empty),
                        status = completion == null ? CleanText(objective.status, "Available") : CompleteStatus,
                        scope = CleanText(objective.scope, "Personal"),
                        evidenceType = CleanText(objective.evidenceType, string.Empty),
                        evidenceId = CleanText(objective.evidenceId, string.Empty),
                        completedBy = completion == null ? string.Empty : completion.completedBy,
                        completedAtUtc = completion == null ? string.Empty : completion.completedAtUtc
                    });
                }
            }

            DarkLog.Normal("Loaded agency progression pack '" + PackName + "' with " + Objectives.Length + " objectives.");
        }

        public static bool RecordEvidence(ClientObject client, int evidenceType, string evidenceId, double gameTime)
        {
            if (!Settings.settingsStore.agencyProgressionEnabled)
            {
                return false;
            }
            if (!Enum.IsDefined(typeof(AgencyEvidenceType), evidenceType))
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for an invalid agency evidence type");
                return false;
            }
            if (!IsEvidenceIdSafe(evidenceId))
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for an invalid agency evidence id");
                return false;
            }
            if (IsRateLimited(client.playerName))
            {
                DarkLog.Debug("Ignored rate-limited agency evidence from " + client.playerName);
                return false;
            }

            string evidenceDirectory = Path.Combine(Server.universeDirectory, "AgencyEvidence");
            Directory.CreateDirectory(evidenceDirectory);
            string evidenceFile = Path.Combine(evidenceDirectory, client.playerName + ".log");
            string evidenceTypeName = ((AgencyEvidenceType)evidenceType).ToString();
            AgencyEvidenceRecord evidenceRecord = new AgencyEvidenceRecord
            {
                receivedAtUtc = DateTime.UtcNow,
                playerName = client.playerName,
                evidenceType = (AgencyEvidenceType)evidenceType,
                evidenceId = evidenceId,
                gameTime = gameTime
            };
            string record = FormatEvidenceRecord(evidenceRecord) + Environment.NewLine;

            lock (Server.universeSizeLock)
            {
                File.AppendAllText(evidenceFile, record);
            }
            DarkLog.Debug("Recorded agency evidence " + evidenceTypeName + ":" + evidenceId + " from " + client.playerName);
            if (CompleteMatchingObjectives(evidenceRecord))
            {
                DarkMultiPlayerServer.Messages.AgencyProgression.SendAgencyProgressionToAll();
            }
            return true;
        }

        public static AgencyEvidenceRecord[] GetEvidenceRecords()
        {
            string evidenceDirectory = Path.Combine(Server.universeDirectory, "AgencyEvidence");
            if (!Directory.Exists(evidenceDirectory))
            {
                return new AgencyEvidenceRecord[0];
            }

            List<AgencyEvidenceRecord> records = new List<AgencyEvidenceRecord>();
            foreach (string evidenceFile in Directory.GetFiles(evidenceDirectory, "*.log"))
            {
                records.AddRange(ReadEvidenceFile(evidenceFile));
            }
            return records.ToArray();
        }

        public static AgencyEvidenceRecord[] GetEvidenceRecords(string playerName)
        {
            if (!SafeFile.IsNameSafe(playerName))
            {
                return new AgencyEvidenceRecord[0];
            }

            string evidenceFile = Path.Combine(Server.universeDirectory, "AgencyEvidence", playerName + ".log");
            return ReadEvidenceFile(evidenceFile);
        }

        public static AgencyEvidenceRecord[] FindEvidence(AgencyEvidenceType evidenceType, string evidenceId)
        {
            List<AgencyEvidenceRecord> matches = new List<AgencyEvidenceRecord>();
            foreach (AgencyEvidenceRecord record in GetEvidenceRecords())
            {
                if (record.evidenceType == evidenceType && record.evidenceId == evidenceId)
                {
                    matches.Add(record);
                }
            }
            return matches.ToArray();
        }

        private static bool CompleteMatchingObjectives(AgencyEvidenceRecord evidenceRecord)
        {
            bool completedAny = false;
            lock (objectives)
            {
                foreach (AgencyObjective objective in objectives)
                {
                    if (objective.status == CompleteStatus || string.IsNullOrEmpty(objective.evidenceType) || string.IsNullOrEmpty(objective.evidenceId))
                    {
                        continue;
                    }
                    AgencyEvidenceType objectiveEvidenceType;
                    if (!Enum.TryParse(objective.evidenceType, out objectiveEvidenceType))
                    {
                        continue;
                    }
                    if (objectiveEvidenceType != evidenceRecord.evidenceType || objective.evidenceId != evidenceRecord.evidenceId)
                    {
                        continue;
                    }

                    string completedAt = DateTime.UtcNow.ToString("o");
                    objective.status = CompleteStatus;
                    objective.completedBy = evidenceRecord.playerName;
                    objective.completedAtUtc = completedAt;
                    lock (completions)
                    {
                        completions[objective.id] = new AgencyObjectiveCompletion
                        {
                            objectiveId = objective.id,
                            completedBy = evidenceRecord.playerName,
                            completedAtUtc = completedAt
                        };
                    }
                    completedAny = true;
                    DarkLog.Normal("Agency objective complete: " + objective.id + " by " + evidenceRecord.playerName);
                }
            }
            if (completedAny)
            {
                SaveCompletions();
            }
            return completedAny;
        }

        private static bool IsEvidenceIdSafe(string evidenceId)
        {
            if (string.IsNullOrEmpty(evidenceId) || evidenceId.Length > MaxEvidenceIdLength)
            {
                return false;
            }
            return SafeFile.IsNameSafe(evidenceId);
        }

        private static bool IsRateLimited(string playerName)
        {
            long now = DateTime.UtcNow.Ticks;
            lock (lastEvidenceReceiveTicks)
            {
                long lastReceive;
                if (lastEvidenceReceiveTicks.TryGetValue(playerName, out lastReceive) && now - lastReceive < EvidenceRateLimit.Ticks)
                {
                    return true;
                }
                lastEvidenceReceiveTicks[playerName] = now;
                return false;
            }
        }

        private static string FormatEvidenceRecord(AgencyEvidenceRecord record)
        {
            return record.receivedAtUtc.ToString("o") + "\t" + record.playerName + "\t" + record.evidenceType.ToString() + "\t" + record.evidenceId + "\t" + record.gameTime.ToString("R");
        }

        private static AgencyObjectiveCompletion GetCompletion(string objectiveId)
        {
            lock (completions)
            {
                AgencyObjectiveCompletion completion;
                completions.TryGetValue(objectiveId, out completion);
                return completion;
            }
        }

        private static string GetCompletionFile()
        {
            return Path.Combine(Server.universeDirectory, "AgencyProgression", "Objectives.log");
        }

        private static void LoadCompletions()
        {
            string completionFile = GetCompletionFile();
            if (!File.Exists(completionFile))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(completionFile))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] parts = line.Split('\t');
                if (parts.Length != 3 || string.IsNullOrEmpty(parts[0]))
                {
                    continue;
                }
                lock (completions)
                {
                    completions[parts[0]] = new AgencyObjectiveCompletion
                    {
                        objectiveId = parts[0],
                        completedBy = parts[1],
                        completedAtUtc = parts[2]
                    };
                }
            }
        }

        private static void SaveCompletions()
        {
            string completionFile = GetCompletionFile();
            Directory.CreateDirectory(Path.GetDirectoryName(completionFile));
            List<string> lines = new List<string>();
            lock (completions)
            {
                foreach (AgencyObjectiveCompletion completion in completions.Values)
                {
                    lines.Add(completion.objectiveId + "\t" + completion.completedBy + "\t" + completion.completedAtUtc);
                }
            }
            File.WriteAllLines(completionFile, lines.ToArray());
        }

        private static AgencyEvidenceRecord[] ReadEvidenceFile(string evidenceFile)
        {
            if (!File.Exists(evidenceFile))
            {
                return new AgencyEvidenceRecord[0];
            }

            List<AgencyEvidenceRecord> records = new List<AgencyEvidenceRecord>();
            foreach (string line in File.ReadAllLines(evidenceFile))
            {
                AgencyEvidenceRecord record;
                if (TryParseEvidenceRecord(line, out record))
                {
                    records.Add(record);
                }
            }
            return records.ToArray();
        }

        private static bool TryParseEvidenceRecord(string line, out AgencyEvidenceRecord record)
        {
            record = null;
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            string[] parts = line.Split('\t');
            if (parts.Length != 5)
            {
                return false;
            }

            DateTime receivedAtUtc;
            AgencyEvidenceType evidenceType;
            double gameTime;
            if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out receivedAtUtc))
            {
                return false;
            }
            if (!Enum.TryParse(parts[2], out evidenceType))
            {
                return false;
            }
            if (!double.TryParse(parts[4], out gameTime))
            {
                return false;
            }

            record = new AgencyEvidenceRecord
            {
                receivedAtUtc = receivedAtUtc,
                playerName = parts[1],
                evidenceType = evidenceType,
                evidenceId = parts[3],
                gameTime = gameTime
            };
            return true;
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
                        scope = "Personal",
                        evidenceType = AgencyEvidenceType.VESSEL_ORBITED.ToString(),
                        evidenceId = "orbit-Kerbin"
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

        [DataMember]
        public string evidenceType;

        [DataMember]
        public string evidenceId;

        public string completedBy;
        public string completedAtUtc;
    }

    public class AgencyEvidenceRecord
    {
        public DateTime receivedAtUtc;
        public string playerName;
        public AgencyEvidenceType evidenceType;
        public string evidenceId;
        public double gameTime;
    }

    public class AgencyObjectiveCompletion
    {
        public string objectiveId;
        public string completedBy;
        public string completedAtUtc;
    }

}
