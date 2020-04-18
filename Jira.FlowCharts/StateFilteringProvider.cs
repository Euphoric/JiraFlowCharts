namespace Jira.FlowCharts
{
    public class StateFilteringProvider : IStateFilteringProvider
    {
        private readonly StateFiltering _stateFiltering;

        public StateFilteringProvider(StateFiltering stateFiltering)
        {
            _stateFiltering = stateFiltering;
        }

        public StateFilteringParameter GetStateFilteringParameter()
        {
            return StateFilteringParameter.GetParameters(_stateFiltering);
        }
    }
}