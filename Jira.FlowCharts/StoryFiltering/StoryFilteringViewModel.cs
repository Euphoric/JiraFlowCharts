using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using ReactiveUI;

namespace Jira.FlowCharts.StoryFiltering
{
    public class StoryFilteringViewModel : ReactiveScreen
    {
        private readonly TasksSource _tasksSource;

        public StoryFilteringViewModel(TasksSource tasksSource)
        {
            _tasksSource = tasksSource;

            DisplayName = "Story and state filtering";

            AvailableStates = new ObservableCollection<string>();

            MoveStateToFiltered = ReactiveCommand.CreateFromTask(MoveStateToFilteredInner);
            MoveStateFromFiltered = ReactiveCommand.CreateFromTask(MoveStateFromFilteredInner);
        }

        public string SelectedAvailableState { get; set; }
        public ObservableCollection<string> AvailableStates { get; }

        public string SelectedFilteredState { get; set; }
        public ObservableCollection<string> FilteredStates { get { return _tasksSource.FilteredStates; } }

        public ObservableCollection<string> ResetStates { get { return _tasksSource.ResetStates; } }

        public ReactiveCommand<Unit, Unit> MoveStateToFiltered { get; }

        public ReactiveCommand<Unit, Unit> MoveStateFromFiltered { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await _tasksSource.ReloadStates();

            var allStates = await _tasksSource.GetAllStates();
            var filteredStates = _tasksSource.FilteredStates;
            var resetStates = _tasksSource.ResetStates;

            AvailableStates.Clear();

            AvailableStates.AddRange(allStates.Except(filteredStates).Except(resetStates));

            await base.OnActivateAsync(cancellationToken);
        }

        private async Task MoveStateToFilteredInner()
        {
            string selectedAvailableState = SelectedAvailableState;
            if (selectedAvailableState != null)
            {
                AvailableStates.Remove(selectedAvailableState);
                _tasksSource.AddFilteredState(selectedAvailableState);
            }
        }

        private async Task MoveStateFromFilteredInner()
        {
            string selectedFilteredState = SelectedFilteredState;
            if (selectedFilteredState != null)
            {
                AvailableStates.Add(selectedFilteredState);
                _tasksSource.RemoveFilteredState(selectedFilteredState);
            }
        }
    }
}
