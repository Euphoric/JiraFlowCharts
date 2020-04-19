namespace Jira.FlowCharts
{
    public interface IStateFilteringProvider
    {
        StateFiltering GetStateFiltering();
        StateFilteringParameter GetStateFilteringParameter();
    }
}