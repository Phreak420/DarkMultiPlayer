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
                    return BuildObjectiveView(string.Empty);
                }
            }
        }

        public static AgencyObjective[] GetObjectivesForPlayer(string playerName)
        {
            lock (objectives)
            {
                return BuildObjectiveView(playerName);
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
                    string scope = CleanText(objective.scope, "Personal");
                    AgencyObjectiveCompletion completion = IsServerObjective(scope) ? GetCompletion(objective.id, string.Empty) : null;
                    objectives.Add(new AgencyObjective
                    {
                        id = CleanText(objective.id, string.Empty),
                        title = CleanText(objective.title, objective.id),
                        description = CleanText(objective.description, string.Empty),
                        status = completion == null ? CleanText(objective.status, "Available") : CompleteStatus,
                        scope = scope,
                        evidenceType = CleanText(objective.evidenceType, string.Empty),
                        evidenceId = CleanText(objective.evidenceId, string.Empty),
                        rewardFunds = objective.rewardFunds,
                        rewardScience = objective.rewardScience,
                        rewardReputation = objective.rewardReputation,
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
            if (CompleteMatchingObjectives(client, evidenceRecord))
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

        public static AgencyRewardRecord[] GetRewardRecords()
        {
            string rewardDirectory = Path.Combine(Server.universeDirectory, "AgencyRewards");
            if (!Directory.Exists(rewardDirectory))
            {
                return new AgencyRewardRecord[0];
            }

            List<AgencyRewardRecord> records = new List<AgencyRewardRecord>();
            foreach (string rewardFile in Directory.GetFiles(rewardDirectory, "*.log"))
            {
                records.AddRange(ReadRewardFile(rewardFile));
            }
            return records.ToArray();
        }

        public static AgencyRewardRecord[] GetRewardRecords(string playerName)
        {
            if (!SafeFile.IsNameSafe(playerName))
            {
                return new AgencyRewardRecord[0];
            }

            string rewardFile = Path.Combine(Server.universeDirectory, "AgencyRewards", playerName + ".log");
            return ReadRewardFile(rewardFile);
        }

        public static bool ReplayReward(string playerName, string objectiveId)
        {
            if (!IsAdminTargetSafe(playerName, objectiveId))
            {
                return false;
            }

            AgencyObjective objective = FindObjective(objectiveId);
            if (objective == null || !HasCompletion(objective, playerName))
            {
                return false;
            }
            if (!ObjectiveHasReward(objective))
            {
                return false;
            }

            return RecordAndSendReward(playerName, objective.id, objective.rewardFunds, objective.rewardScience, objective.rewardReputation, true);
        }

        public static bool RevokeReward(string playerName, string objectiveId)
        {
            if (!IsAdminTargetSafe(playerName, objectiveId))
            {
                return false;
            }

            AgencyObjective objective = FindObjective(objectiveId);
            if (objective == null || !ObjectiveHasReward(objective))
            {
                return false;
            }

            return RecordAndSendReward(playerName, objective.id, -objective.rewardFunds, -objective.rewardScience, -objective.rewardReputation, true);
        }

        private static bool CompleteMatchingObjectives(ClientObject client, AgencyEvidenceRecord evidenceRecord)
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

                    string completionPlayer = IsServerObjective(objective.scope) ? string.Empty : evidenceRecord.playerName;
                    if (GetCompletion(objective.id, completionPlayer) != null)
                    {
                        continue;
                    }

                    string completedAt = DateTime.UtcNow.ToString("o");
                    lock (completions)
                    {
                        completions[BuildCompletionKey(objective.id, completionPlayer)] = new AgencyObjectiveCompletion
                        {
                            objectiveId = objective.id,
                            scope = objective.scope,
                            playerName = completionPlayer,
                            completedBy = evidenceRecord.playerName,
                            completedAtUtc = completedAt
                        };
                    }
                    completedAny = true;
                    DarkLog.Normal("Agency objective complete: " + objective.id + " by " + evidenceRecord.playerName);
                    RecordAndSendReward(client.playerName, objective.id, objective.rewardFunds, objective.rewardScience, objective.rewardReputation, true, client);
                }
            }
            if (completedAny)
            {
                SaveCompletions();
            }
            return completedAny;
        }

        private static bool RecordAndSendReward(string playerName, string objectiveId, double funds, float science, float reputation, bool sendIfOnline, ClientObject connectedClient = null)
        {
            if (funds == 0 && science == 0 && reputation == 0)
            {
                return false;
            }

            string rewardDirectory = Path.Combine(Server.universeDirectory, "AgencyRewards");
            Directory.CreateDirectory(rewardDirectory);
            string rewardFile = Path.Combine(rewardDirectory, playerName + ".log");
            string record = DateTime.UtcNow.ToString("o") + "\t" + playerName + "\t" + objectiveId + "\t" + funds.ToString("R") + "\t" + science.ToString("R") + "\t" + reputation.ToString("R") + Environment.NewLine;
            lock (Server.universeSizeLock)
            {
                File.AppendAllText(rewardFile, record);
            }

            ClientObject client = connectedClient ?? ClientHandler.GetClientByName(playerName);
            if (sendIfOnline && client != null && client.authenticated)
            {
                DarkMultiPlayerServer.Messages.AgencyReward.SendAgencyReward(client, objectiveId, funds, science, reputation);
            }
            return true;
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

        private static AgencyObjectiveCompletion GetCompletion(string objectiveId, string playerName)
        {
            lock (completions)
            {
                AgencyObjectiveCompletion completion;
                completions.TryGetValue(BuildCompletionKey(objectiveId, playerName), out completion);
                return completion;
            }
        }

        private static bool HasCompletion(AgencyObjective objective, string playerName)
        {
            string completionPlayer = IsServerObjective(objective.scope) ? string.Empty : playerName;
            return GetCompletion(objective.id, completionPlayer) != null;
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
                if ((parts.Length != 3 && parts.Length != 5) || string.IsNullOrEmpty(parts[0]))
                {
                    continue;
                }
                string objectiveId = parts[0];
                string scope = parts.Length == 5 ? parts[1] : "Server";
                string playerName = parts.Length == 5 ? parts[2] : string.Empty;
                string completedBy = parts.Length == 5 ? parts[3] : parts[1];
                string completedAtUtc = parts.Length == 5 ? parts[4] : parts[2];
                lock (completions)
                {
                    completions[BuildCompletionKey(objectiveId, playerName)] = new AgencyObjectiveCompletion
                    {
                        objectiveId = objectiveId,
                        scope = scope,
                        playerName = playerName,
                        completedBy = completedBy,
                        completedAtUtc = completedAtUtc
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
                    lines.Add(completion.objectiveId + "\t" + completion.scope + "\t" + completion.playerName + "\t" + completion.completedBy + "\t" + completion.completedAtUtc);
                }
            }
            File.WriteAllLines(completionFile, lines.ToArray());
        }

        private static AgencyObjective[] BuildObjectiveView(string playerName)
        {
            List<AgencyObjective> view = new List<AgencyObjective>();
            foreach (AgencyObjective objective in objectives)
            {
                string completionPlayer = IsServerObjective(objective.scope) ? string.Empty : playerName;
                AgencyObjectiveCompletion completion = GetCompletion(objective.id, completionPlayer);
                view.Add(new AgencyObjective
                {
                    id = objective.id,
                    title = objective.title,
                    description = objective.description,
                    status = completion == null ? objective.status : CompleteStatus,
                    scope = objective.scope,
                    evidenceType = objective.evidenceType,
                    evidenceId = objective.evidenceId,
                    rewardFunds = objective.rewardFunds,
                    rewardScience = objective.rewardScience,
                    rewardReputation = objective.rewardReputation,
                    completedBy = completion == null ? string.Empty : completion.completedBy,
                    completedAtUtc = completion == null ? string.Empty : completion.completedAtUtc
                });
            }
            return view.ToArray();
        }

        private static AgencyObjective FindObjective(string objectiveId)
        {
            lock (objectives)
            {
                foreach (AgencyObjective objective in objectives)
                {
                    if (objective.id == objectiveId)
                    {
                        return objective;
                    }
                }
            }
            return null;
        }

        private static bool ObjectiveHasReward(AgencyObjective objective)
        {
            return objective.rewardFunds != 0 || objective.rewardScience != 0 || objective.rewardReputation != 0;
        }

        private static bool IsServerObjective(string scope)
        {
            return string.Equals(scope, "Server", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildCompletionKey(string objectiveId, string playerName)
        {
            return objectiveId + "\t" + playerName;
        }

        private static bool IsAdminTargetSafe(string playerName, string objectiveId)
        {
            return SafeFile.IsNameSafe(playerName) && SafeFile.IsNameSafe(objectiveId);
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

        private static AgencyRewardRecord[] ReadRewardFile(string rewardFile)
        {
            if (!File.Exists(rewardFile))
            {
                return new AgencyRewardRecord[0];
            }

            List<AgencyRewardRecord> records = new List<AgencyRewardRecord>();
            foreach (string line in File.ReadAllLines(rewardFile))
            {
                AgencyRewardRecord record;
                if (TryParseRewardRecord(line, out record))
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

        private static bool TryParseRewardRecord(string line, out AgencyRewardRecord record)
        {
            record = null;
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            string[] parts = line.Split('\t');
            if (parts.Length != 6)
            {
                return false;
            }

            DateTime awardedAtUtc;
            double funds;
            float science;
            float reputation;
            if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out awardedAtUtc))
            {
                return false;
            }
            if (!double.TryParse(parts[3], out funds) || !float.TryParse(parts[4], out science) || !float.TryParse(parts[5], out reputation))
            {
                return false;
            }

            record = new AgencyRewardRecord
            {
                awardedAtUtc = awardedAtUtc,
                playerName = parts[1],
                objectiveId = parts[2],
                funds = funds,
                science = science,
                reputation = reputation
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
                        evidenceId = "orbit-Kerbin",
                        rewardFunds = 5000,
                        rewardScience = 5,
                        rewardReputation = 2
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

        [DataMember]
        public double rewardFunds;

        [DataMember]
        public float rewardScience;

        [DataMember]
        public float rewardReputation;

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
        public string scope;
        public string playerName;
        public string completedBy;
        public string completedAtUtc;
    }

    public class AgencyRewardRecord
    {
        public DateTime awardedAtUtc;
        public string playerName;
        public string objectiveId;
        public double funds;
        public float science;
        public float reputation;
    }

}
