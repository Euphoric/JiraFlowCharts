using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;

namespace Jira.FlowCharts
{
    public class StateFiltering
    {
        private readonly ITasksSourceJiraCacheAdapter _jiraCache;
        private readonly IStatesRepository _statesRepository;

        public ObservableCollection<string> AvailableStates { get; }
        public ObservableCollection<string> FilteredStates { get; }
        public ObservableCollection<string> ResetStates { get; }

        public StateFiltering(ITasksSourceJiraCacheAdapter jiraCacheAdapter, IStatesRepository statesRepository)
        {
            _jiraCache = jiraCacheAdapter;
            _statesRepository = statesRepository;

            AvailableStates = new ObservableCollection<string>();
            FilteredStates = new ObservableCollection<string>();
            ResetStates = new ObservableCollection<string>();
        }

        public async Task<string[]> GetAllStates()
        {
            return await _jiraCache.GetAllStates();
        }

        public async Task ReloadStates()
        {
            AvailableStates.Clear();
            FilteredStates.Clear();
            ResetStates.Clear();

            var allStates = await GetAllStates();
            var filteredStates = _statesRepository.GetFilteredStates();
            var resetStates = _statesRepository.GetResetStates();

            AvailableStates.AddRange(allStates.Except(filteredStates).Except(resetStates));
            FilteredStates.AddRange(filteredStates);
            ResetStates.AddRange(resetStates);
        }

        public void AddFilteredState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Remove(state);
            FilteredStates.Add(state);
            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        public void RemoveFilteredState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Add(state);
            FilteredStates.Remove(state);
            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        public void AddResetState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Remove(state);
            ResetStates.Add(state);
            _statesRepository.SetResetStates(ResetStates.ToArray());
        }

        public void RemoveResetState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Add(state);
            ResetStates.Remove(state);
            _statesRepository.SetResetStates(ResetStates.ToArray());
        }

        public void MoveFilteredStateLower(string state)
        {
            if (state == null)
                return;

            var stateAt = FilteredStates.IndexOf(state);
            if (stateAt == 0)
                return;

            stateAt--;
            var reinsertState = FilteredStates[stateAt];
            FilteredStates.RemoveAt(stateAt);
            FilteredStates.Insert(stateAt + 1, reinsertState);

            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        public void MoveFilteredStateHigher(string state)
        {
            if (state == null)
                return;

            var stateAt = FilteredStates.IndexOf(state);
            if (stateAt == FilteredStates.Count - 1)
                return;

            stateAt++;
            var reinsertState = FilteredStates[stateAt];
            FilteredStates.RemoveAt(stateAt);
            FilteredStates.Insert(stateAt - 1, reinsertState);

            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }
    }
}