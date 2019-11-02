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
            FilteredStates = new ObservableCollection<string>();
            ResetStates = new ObservableCollection<string>();

            MoveStateToFiltered = ReactiveCommand.CreateFromTask(MoveStateToFilteredInner);
        }

        public string SelectedAvailableState { get; set; }
        public ObservableCollection<string> AvailableStates { get; }

        public ObservableCollection<string> FilteredStates { get; }

        public ObservableCollection<string> ResetStates { get; }

        public ReactiveCommand<Unit, Unit> MoveStateToFiltered { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await _tasksSource.ReloadStates();

            var allStates = await _tasksSource.GetAllStates();
            var filteredStates = _tasksSource.States;
            var resetStates = _tasksSource.ResetStates;

            AvailableStates.Clear();
            FilteredStates.Clear();
            ResetStates.Clear();

            AvailableStates.AddRange(allStates.Except(filteredStates).Except(resetStates));
            FilteredStates.AddRange(filteredStates);
            ResetStates.AddRange(resetStates);

            await base.OnActivateAsync(cancellationToken);
        }

        private async Task MoveStateToFilteredInner()
        {
            string selectedAvailableState = SelectedAvailableState;
            if (selectedAvailableState != null)
            {
                AvailableStates.Remove(selectedAvailableState);
                FilteredStates.Add(selectedAvailableState);
                _tasksSource.AddFilteredState(selectedAvailableState);
            }
        }
    }
}
