using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace Jira.FlowCharts.StoryFiltering
{
    public class StoryFilteringViewModel : ReactiveScreen
    {
        private readonly IStateFilteringProvider _stateFilteringProvider;

        private StateFiltering _stateFiltering;

        public StoryFilteringViewModel(IStateFilteringProvider stateFilteringProvider)
        {
            _stateFilteringProvider = stateFilteringProvider;

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
            await base.OnActivateAsync(cancellationToken);

            _stateFiltering = _stateFilteringProvider.GetStateFiltering();
            await _stateFiltering.ReloadStates();
            NotifyOfPropertyChange(nameof(AvailableStates));
            NotifyOfPropertyChange(nameof(FilteredStates));
            NotifyOfPropertyChange(nameof(ResetStates));
        }

        private Task MoveStateToFilteredInner()
        {
            _stateFiltering.AddFilteredState(SelectedAvailableState);

            return Task.CompletedTask;
        }

        private Task MoveStateFromFilteredInner()
        {
            _stateFiltering.RemoveFilteredState(SelectedFilteredState);

            return Task.CompletedTask;
        }

        private Task MoveStateToResetInner()
        {
            _stateFiltering.AddResetState(SelectedAvailableState);

            return Task.CompletedTask;
        }

        private Task MoveStateFromResetInner()
        {
            _stateFiltering.RemoveResetState(SelectedResetState);

            return Task.CompletedTask;
        }

        private Task MoveFilteredStateLowerInner()
        {
            _stateFiltering.MoveFilteredStateLower(SelectedFilteredState);

            return Task.CompletedTask;
        }

        private Task MoveFilteredStateHigherInner()
        {
            _stateFiltering.MoveFilteredStateHigher(SelectedFilteredState);

            return Task.CompletedTask;
        }
    }
}
