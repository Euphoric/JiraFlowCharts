using System.Threading.Tasks;

namespace Jira.FlowCharts
{
    public interface IStateFilteringProvider
    {
        StateFiltering GetStateFiltering();
        Task<StateFilteringParameter> GetStateFilteringParameter();
    }
}