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

        public StoryFilteringViewModelTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();

            var tasksSource = new TasksSource(_jiraCacheAdapter);
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
            Assert.Empty(_vm.States);
            Assert.Empty(_vm.ResetStates);
        }

        [Fact]
        public async Task Available_states_from_source_stories()
        {
            var allStates = new[] {"A", "B", "C"};
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            Assert.Equal(allStates, _vm.AvailableStates);

            Assert.Empty(_vm.States);
            Assert.Empty(_vm.ResetStates);
        }

        [Fact]
        public async Task Reactivating_should_keep_the_states_same()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            Assert.Equal(allStates, _vm.AvailableStates);

            await Reactivate();

            Assert.Equal(allStates, _vm.AvailableStates);
        }
    }
}
