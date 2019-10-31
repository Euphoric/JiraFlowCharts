namespace Jira.FlowCharts
{
    public class TestStatesRepository : IStatesRepository
    {
        public string[] FilteredStates { get; set; } = new string[0];

        public string[] GetFilteredStates()
        {
            return FilteredStates;
        }
    }
}