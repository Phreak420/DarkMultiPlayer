using System.Collections.Generic;
using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayer
{
    public class AgencyProgressionWorker
    {
        private readonly List<AgencyObjectiveSummary> objectives = new List<AgencyObjectiveSummary>();
        private readonly DMPGame dmpGame;
        private readonly NetworkWorker networkWorker;

        public string PackName { get; private set; }

        public AgencyProgressionWorker(DMPGame dmpGame, NetworkWorker networkWorker)
        {
            this.dmpGame = dmpGame;
            this.networkWorker = networkWorker;
            GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
            GameEvents.OnScienceRecieved.Add(OnScienceRecieved);
        }

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
            GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
            GameEvents.OnScienceRecieved.Remove(OnScienceRecieved);
            lock (objectives)
            {
                objectives.Clear();
                PackName = null;
            }
        }

        private void OnTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> data)
        {
            if (!dmpGame.serverAgencyProgressionEnabled || data.host == null || string.IsNullOrEmpty(data.host.techID))
            {
                return;
            }
            networkWorker.SendAgencyEvidence(AgencyEvidenceType.TECHNOLOGY_RESEARCHED, data.host.techID);
        }

        private void OnScienceRecieved(float science, ScienceSubject subject, ProtoVessel vessel, bool reverseEngineered)
        {
            if (!dmpGame.serverAgencyProgressionEnabled || subject == null || string.IsNullOrEmpty(subject.id))
            {
                return;
            }
            networkWorker.SendAgencyEvidence(AgencyEvidenceType.SCIENCE_RECEIVED, subject.id);
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
