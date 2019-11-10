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

            MoveStateToFiltered = ReactiveCommand.CreateFromTask(MoveStateToFilteredInner);
            MoveStateFromFiltered = ReactiveCommand.CreateFromTask(MoveStateFromFilteredInner);
            MoveStateToReset = ReactiveCommand.CreateFromTask(MoveStateToResetInner);
            MoveStateFromReset = ReactiveCommand.CreateFromTask(MoveStateFromResetInner);
        }

        public string SelectedAvailableState { get; set; }
        public ObservableCollection<string> AvailableStates { get { return _tasksSource.AvailableStates; } }

        public string SelectedFilteredState { get; set; }
        public ObservableCollection<string> FilteredStates { get { return _tasksSource.FilteredStates; } }

        public string SelectedResetState { get; set; }
        public ObservableCollection<string> ResetStates { get { return _tasksSource.ResetStates; } }

        public ReactiveCommand<Unit, Unit> MoveStateToFiltered { get; }

        public ReactiveCommand<Unit, Unit> MoveStateFromFiltered { get; }

        public ReactiveCommand<Unit, Unit> MoveStateToReset { get; }

        public ReactiveCommand<Unit, Unit> MoveStateFromReset { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await _tasksSource.ReloadStates();

            await base.OnActivateAsync(cancellationToken);
        }

        private async Task MoveStateToFilteredInner()
        {
            string stelectedState = SelectedAvailableState;
            if (stelectedState != null)
            {
                _tasksSource.AddFilteredState(stelectedState);
            }
        }

        private async Task MoveStateFromFilteredInner()
        {
            string selectedState = SelectedFilteredState;
            if (selectedState != null)
            {
                _tasksSource.RemoveFilteredState(selectedState);
            }
        }

        private async Task MoveStateToResetInner()
        {
            string selectedState = SelectedAvailableState;
            if (selectedState != null)
            {
                _tasksSource.AddResetState(selectedState);
            }
        }

        private async Task MoveStateFromResetInner()
        {
            string selectedState = SelectedResetState;
            if (selectedState != null)
            {
                _tasksSource.RemoveResetState(selectedState);
            }
        }
    }
}
