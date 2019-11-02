using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.FlowCharts.StoryFiltering;
using Xunit;

namespace Jira.FlowCharts
{
    public class StoryFilteringViewModelTest : IAsyncLifetime
    {
        private StoryFilteringViewModel _vm;
        private TestJiraCacheAdapter _jiraCacheAdapter;
        private TestStatesRepository _statesRepository;
        TasksSource _tasksSource;

        public StoryFilteringViewModelTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();
            _statesRepository = new TestStatesRepository();
            _tasksSource = new TasksSource(_jiraCacheAdapter, _statesRepository);
            _vm = new StoryFilteringViewModel(_tasksSource);
        }

        public async Task InitializeAsync()
        {
            await (_vm as IScreen).ActivateAsync();
        }

        public async Task DisposeAsync()
        {
            await (_vm as IScreen).DeactivateAsync(false);
        }

        private async Task Reactivate()
        {
            await (_vm as IScreen).DeactivateAsync(false);
            await (_vm as IScreen).ActivateAsync();
        }

        [Fact]
        public void No_states_have_empty_collections()
        {
            Assert.Empty(_vm.AvailableStates);
            Assert.Empty(_vm.FilteredStates);
            Assert.Empty(_vm.ResetStates);
        }

        [Fact]
        public async Task Available_states_from_source_stories()
        {
            var allStates = new[] {"A", "B", "C"};
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            Assert.Equal(allStates, _vm.AvailableStates);

            Assert.Empty(_vm.FilteredStates);
            Assert.Empty(_vm.ResetStates);
        }

        [Fact]
        public async Task Filtered_states_from_repository_are_in_filtered_states_but_not_in_Available_states()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            var filteredStates = new[] {"B", "C"};
            _statesRepository.FilteredStates = filteredStates;

            await Reactivate();

            Assert.Equal(new[] {"A"}, _vm.AvailableStates);

            Assert.Equal(filteredStates, _vm.FilteredStates);

            Assert.Empty(_vm.ResetStates);
        }

        [Fact]
        public async Task Reset_states_from_repository_are_in_Reset_states_but_not_in_Available_states()
        {
            var allStates = new[] { "A", "B", "C", "D" };
            _jiraCacheAdapter.AllStates = allStates;

            var resetStates = new[] { "D" };
            _statesRepository.ResetStates = resetStates;

            await Reactivate();

            Assert.Equal(new[] { "A", "B", "C" }, _vm.AvailableStates);

            Assert.Equal(resetStates, _vm.ResetStates);

            Assert.Empty(_vm.FilteredStates);
        }

        [Fact]
        public async Task Reactivating_multiple_should_keep_the_states_same()
        {
            var allStates = new[] { "A", "B", "C", "D" };
            _jiraCacheAdapter.AllStates = allStates;

            var filteredStates = new[] { "B", "C" };
            _statesRepository.FilteredStates = filteredStates;

            var resetStates = new[] { "D" };
            _statesRepository.ResetStates = resetStates;

            await Reactivate();
            await Reactivate();

            Assert.Equal(new[] { "A" }, _vm.AvailableStates);
            Assert.Equal(filteredStates, _vm.FilteredStates);
            Assert.Equal(resetStates, _vm.ResetStates);
        }

        [Fact]
        public async Task Move_to_filtered_state_when_none_selected()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Equal(allStates, _vm.AvailableStates);
            Assert.Empty(_vm.FilteredStates);
        }

        [Fact]
        public async Task Move_to_filtered_state()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            _vm.SelectedAvailableState = "A";
            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A" }, _tasksSource.FilteredStates);

            _vm.SelectedAvailableState = "C";
            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C" }, _tasksSource.FilteredStates);

            _vm.SelectedAvailableState = "B";
            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Empty(_vm.AvailableStates);
            Assert.Equal(new[] { "A", "C", "B" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C", "B" }, _tasksSource.FilteredStates);
        }

        [Fact]
        public async Task Selected_available_state_can_change_when_moving()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            _vm.SelectedAvailableState = "A";

            // selection changes when item is removed from collection
            _vm.AvailableStates.CollectionChanged += (_, __)=> { _vm.SelectedAvailableState = null; }; 

            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A" }, _tasksSource.FilteredStates);
        }

        [Fact]
        public async Task Move_from_filtered_state_when_none_selected()
        {
            var allStates = new[] { "A", "B", "C" };
            _statesRepository.FilteredStates = allStates;

            await Reactivate();

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Empty(_vm.AvailableStates);
            Assert.Equal(allStates, _vm.FilteredStates);
        }

        [Fact]
        public async Task Move_from_filtered_state()
        {
            var allStates = new[] { "A", "B", "C" };
            _statesRepository.FilteredStates = allStates;

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C" }, _tasksSource.FilteredStates);

            _vm.SelectedFilteredState = "C";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", }, _tasksSource.FilteredStates);

            _vm.SelectedFilteredState = "A";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B", "C", "A" }, _vm.AvailableStates);
            Assert.Equal(new string[] { }, _vm.FilteredStates);
            Assert.Equal(new string[] { }, _tasksSource.FilteredStates);
        }

        [Fact]
        public async Task Selected_filtered_state_can_change_when_moving()
        {
            var allStates = new[] { "A", "B", "C" };
            _statesRepository.FilteredStates = allStates;

            // selection changes when item is removed from collection
            _vm.FilteredStates.CollectionChanged += (_, __) => { _vm.SelectedFilteredState = null; };

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C" }, _tasksSource.FilteredStates);
        }
    }
}
