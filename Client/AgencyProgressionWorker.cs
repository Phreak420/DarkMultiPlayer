using System.Collections.Generic;
using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayer
{
    public class AgencyProgressionWorker
    {
        private readonly List<AgencyObjectiveSummary> objectives = new List<AgencyObjectiveSummary>();

        public string PackName { get; private set; }

        public AgencyObjectiveSummary[] Objectives
        {
            get
            {
                lock (objectives)
                {
                    return objectives.ToArray();
                }
            }
        }

        public void HandleAgencyProgression(ByteArray messageData)
        {
            using (MessageReader mr = new MessageReader(messageData.data))
            {
                string packName = mr.Read<string>();
                string[] ids = mr.Read<string[]>();
                string[] titles = mr.Read<string[]>();
                string[] descriptions = mr.Read<string[]>();
                string[] statuses = mr.Read<string[]>();
                string[] scopes = mr.Read<string[]>();

                int objectiveCount = ids.Length;
                if (titles.Length != objectiveCount || descriptions.Length != objectiveCount || statuses.Length != objectiveCount || scopes.Length != objectiveCount)
                {
                    DarkLog.Debug("Received invalid agency progression data from server.");
                    return;
                }

                lock (objectives)
                {
                    objectives.Clear();
                    PackName = packName;
                    for (int i = 0; i < objectiveCount; i++)
                    {
                        objectives.Add(new AgencyObjectiveSummary
                        {
                            id = ids[i],
                            title = titles[i],
                            description = descriptions[i],
                            status = statuses[i],
                            scope = scopes[i]
                        });
                    }
                }
                DarkLog.Debug("Received " + objectiveCount + " agency progression objectives.");
            }
        }

        public void Stop()
        {
            lock (objectives)
            {
                objectives.Clear();
                PackName = null;
            }
        }
    }

    public class AgencyObjectiveSummary
    {
        public string id;
        public string title;
        public string description;
        public string status;
        public string scope;
    }
}
