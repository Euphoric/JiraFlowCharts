namespace Jira.FlowCharts
{
    public class MemoryStatesRepository : IStatesRepository
    {
        public string[] FilteredStates { get; private set; }
        public string[] ResetStates { get; private set; }

        public MemoryStatesRepository()
        {
            FilteredStates = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            ResetStates = new[] { "On Hold", "Not Started", "Withdrawn" };
        }

        public string[] GetFilteredStates()
        {
            return FilteredStates;
        }

        public void SetFilteredStates(string[] states)
        {
            FilteredStates = states;
        }

        public string[] GetResetStates()
        {
            return ResetStates;
        }

        public void SetResetStates(string[] states)
        {
            ResetStates = states;
        }
    }
}