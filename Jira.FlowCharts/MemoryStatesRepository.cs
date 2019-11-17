namespace Jira.FlowCharts
{
    public class MemoryStatesRepository : IStatesRepository
    {
        public string[] FilteredStates { get; private set; }
        public string[] ResetStates { get; private set; }

        public MemoryStatesRepository(string[] filteredStates, string[] resetStates)
        {
            FilteredStates = filteredStates;
            ResetStates = resetStates;
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