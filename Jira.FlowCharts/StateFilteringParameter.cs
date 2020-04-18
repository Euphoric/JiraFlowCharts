using System.Linq;

namespace Jira.FlowCharts
{
    public class StateFilteringParameter
    {
        public string[] FilteredStates { get; }
        public string[] ResetStates { get; }

        public StateFilteringParameter(string[] filteredStates, string[] resetStates)
        {
            FilteredStates = filteredStates;
            ResetStates = resetStates;
        }

        public static StateFilteringParameter GetParameters(StateFiltering stateFiltering)
        {
            var filteredStates = stateFiltering.FilteredStates.ToArray();
            var resetStates = stateFiltering.ResetStates.ToArray();

            var stateFilteringParams = new StateFilteringParameter(filteredStates, resetStates);
            return stateFilteringParams;
        }
    }
}