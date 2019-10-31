using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;

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
        }

        public ObservableCollection<string> AvailableStates { get; }

        public ObservableCollection<string> FilteredStates { get; }

        public ObservableCollection<string> ResetStates { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await _tasksSource.ReloadStates();

            string[] allStates = await _tasksSource.GetAllStates();
            string[] filteredStates = _tasksSource.States;

            AvailableStates.Clear();
            FilteredStates.Clear();

            AvailableStates.AddRange(allStates.Except(filteredStates));
            FilteredStates.AddRange(filteredStates);

            await base.OnActivateAsync(cancellationToken);
        }
    }
}
