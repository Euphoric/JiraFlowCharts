using System.Threading.Tasks;

namespace Jira.FlowCharts
{
    public class StateFilteringProvider : IStateFilteringProvider
    {
        private readonly StateFiltering _stateFiltering;

        public StateFilteringProvider(ITasksSourceJiraCacheAdapter jiraCacheAdapter, IStatesRepository statesRepository)
        {
            _stateFiltering = new StateFiltering(jiraCacheAdapter, statesRepository);
        }

        public StateFiltering GetStateFiltering()
        {
            return _stateFiltering;
        }

        public async Task<StateFilteringParameter> GetStateFilteringParameter()
        {
            await _stateFiltering.ReloadStates();
            return StateFilteringParameter.GetParameters(_stateFiltering);
        }
    }
}