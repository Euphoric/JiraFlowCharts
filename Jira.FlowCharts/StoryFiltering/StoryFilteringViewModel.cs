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
        private readonly StateFiltering _stateFiltering;

        public StoryFilteringViewModel(StateFiltering stateFiltering)
        {
            _stateFiltering = stateFiltering;

            DisplayName = "Story and state filtering";

            MoveStateToFiltered = ReactiveCommand.CreateFromTask(MoveStateToFilteredInner);
            MoveStateFromFiltered = ReactiveCommand.CreateFromTask(MoveStateFromFilteredInner);
            MoveStateToReset = ReactiveCommand.CreateFromTask(MoveStateToResetInner);
            MoveStateFromReset = ReactiveCommand.CreateFromTask(MoveStateFromResetInner);
            MoveFilteredStateLower = ReactiveCommand.CreateFromTask(MoveFilteredStateLowerInner);
            MoveFilteredStateHigher = ReactiveCommand.CreateFromTask(MoveFilteredStateHigherInner);
        }

        public string SelectedAvailableState { get; set; }
        public ObservableCollection<string> AvailableStates { get { return _stateFiltering.AvailableStates; } }

        public string SelectedFilteredState { get; set; }
        public ObservableCollection<string> FilteredStates { get { return _stateFiltering.FilteredStates; } }

        public string SelectedResetState { get; set; }
        public ObservableCollection<string> ResetStates { get { return _stateFiltering.ResetStates; } }

        public ReactiveCommand<Unit, Unit> MoveStateToFiltered { get; }

        public ReactiveCommand<Unit, Unit> MoveStateFromFiltered { get; }

        public ReactiveCommand<Unit, Unit> MoveStateToReset { get; }

        public ReactiveCommand<Unit, Unit> MoveStateFromReset { get; }

        public ReactiveCommand<Unit, Unit> MoveFilteredStateLower { get; }

        public ReactiveCommand<Unit, Unit> MoveFilteredStateHigher { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await _stateFiltering.ReloadStates();

            await base.OnActivateAsync(cancellationToken);
        }

        private async Task MoveStateToFilteredInner()
        {
            _stateFiltering.AddFilteredState(SelectedAvailableState);
        }

        private async Task MoveStateFromFilteredInner()
        {
            _stateFiltering.RemoveFilteredState(SelectedFilteredState);
        }

        private async Task MoveStateToResetInner()
        {
            _stateFiltering.AddResetState(SelectedAvailableState);
        }

        private async Task MoveStateFromResetInner()
        {
            _stateFiltering.RemoveResetState(SelectedResetState);
        }

        private async Task MoveFilteredStateLowerInner()
        {
            _stateFiltering.MoveFilteredStateLower(SelectedFilteredState);
        }

        private async Task MoveFilteredStateHigherInner()
        {
            _stateFiltering.MoveFilteredStateHigher(SelectedFilteredState);
        }
    }
}
