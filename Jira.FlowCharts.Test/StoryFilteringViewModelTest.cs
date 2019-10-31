using System;
using System.Collections.Generic;
using System.Linq;
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

        public StoryFilteringViewModelTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();
            _statesRepository = new TestStatesRepository();
            var tasksSource = new TasksSource(_jiraCacheAdapter, _statesRepository);
            _vm = new StoryFilteringViewModel(tasksSource);
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
    }
}
