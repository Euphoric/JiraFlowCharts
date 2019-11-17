namespace Jira.FlowCharts
{
    public class MemoryStatesRepository : IStatesRepository
    {
        private string[] FilteredStates { get; set; }
        private string[] ResetStates { get; set; }

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