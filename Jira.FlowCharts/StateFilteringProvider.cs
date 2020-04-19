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

        public Task<StateFilteringParameter> GetStateFilteringParameter()
        {
            var result = StateFilteringParameter.GetParameters(_stateFiltering);
            return Task.FromResult(result);
        }
    }
}