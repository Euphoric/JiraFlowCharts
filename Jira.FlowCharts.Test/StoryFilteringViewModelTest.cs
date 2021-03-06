﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DynamicData.Binding;
using Jira.FlowCharts.StoryFiltering;
using Xunit;

namespace Jira.FlowCharts
{
    public class StoryFilteringViewModelTest : IAsyncLifetime
    {
        private readonly StoryFilteringViewModel _vm;
        private readonly TestJiraCacheAdapter _jiraCacheAdapter;
        private readonly MemoryStatesRepository _statesRepository;
        private StateFiltering StateFiltering => _stateFilteringProvider.GetStateFiltering();
        private readonly StateFilteringProvider _stateFilteringProvider;

        public StoryFilteringViewModelTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();
            _statesRepository = new MemoryStatesRepository(new string[0], new string[0]);
            _stateFilteringProvider = new StateFilteringProvider(_jiraCacheAdapter, _statesRepository);
            _vm = new StoryFilteringViewModel(_stateFilteringProvider);

            EmulateCollectionSelectedItemChanging();
        }

        /// <summary>
        /// Emulates WPF's behavior of changing selected item when it is removed from bound collection
        /// </summary>
        private void EmulateCollectionSelectedItemChanging()
        {
            void OnAvailableStatesOnCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.OfType<string>().SingleOrDefault() == _vm.SelectedAvailableState)
                {
                    _vm.SelectedAvailableState = null;
                }
            }

            void OnFilteredStatesOnCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.OfType<string>().SingleOrDefault() == _vm.SelectedFilteredState)
                {
                    _vm.SelectedFilteredState = null;
                }
            }

            void OnResetStatesOnCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.OfType<string>().SingleOrDefault() == _vm.SelectedResetState)
                {
                    _vm.SelectedResetState = null;
                }
            }

            ObservableCollection<string> prevAvailableStates = null;
            ObservableCollection<string> prevFilteredStates = null;
            ObservableCollection<string> prevResetStates = null;

            _vm.PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(_vm.AvailableStates):
                        if (prevAvailableStates != null)
                        {
                            prevAvailableStates.CollectionChanged -= OnAvailableStatesOnCollectionChanged;
                        }
                        _vm.AvailableStates.CollectionChanged += OnAvailableStatesOnCollectionChanged;
                        prevAvailableStates = _vm.AvailableStates;
                        break;

                    case nameof(_vm.FilteredStates):
                        if (prevFilteredStates != null)
                        {
                            prevFilteredStates.CollectionChanged -= OnFilteredStatesOnCollectionChanged;
                        }
                        _vm.FilteredStates.CollectionChanged += OnFilteredStatesOnCollectionChanged;
                        prevFilteredStates = _vm.FilteredStates;
                        break;

                    case nameof(_vm.ResetStates):
                        if (prevResetStates != null)
                        {
                            prevResetStates.CollectionChanged -= OnResetStatesOnCollectionChanged;
                        }
                        _vm.ResetStates.CollectionChanged += OnResetStatesOnCollectionChanged;
                        prevResetStates = _vm.ResetStates;
                        break;
                }
            };
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
        public async Task Notifies_collection_change_when_activating()
        {
            HashSet<string> notifiedProperties = new HashSet<string>();
            _vm.PropertyChanged += (sender, args) => notifiedProperties.Add(args.PropertyName);

            await Reactivate();

            Assert.Contains(nameof(_vm.AvailableStates), notifiedProperties);
            Assert.Contains(nameof(_vm.FilteredStates), notifiedProperties);
            Assert.Contains(nameof(_vm.ResetStates), notifiedProperties);
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
            _statesRepository.SetFilteredStates(filteredStates);

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
            _statesRepository.SetResetStates(resetStates);

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
            _statesRepository.SetFilteredStates(filteredStates);

            var resetStates = new[] { "D" };
            _statesRepository.SetResetStates(resetStates);

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
            Assert.Equal(new[] { "A" }, StateFiltering.FilteredStates);

            _vm.SelectedAvailableState = "C";
            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.FilteredStates);

            _vm.SelectedAvailableState = "B";
            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Empty(_vm.AvailableStates);
            Assert.Equal(new[] { "A", "C", "B" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C", "B" }, StateFiltering.FilteredStates);
        }

        [Fact]
        public async Task Move_to_filtered_state_persists()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            _vm.SelectedAvailableState = "A";
            await _vm.MoveStateToFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A" }, StateFiltering.FilteredStates);

            await Reactivate();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A" }, StateFiltering.FilteredStates);
        }

        [Fact]
        public async Task Move_from_filtered_state_when_none_selected()
        {
            var allStates = new[] { "A", "B", "C" };
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Empty(_vm.AvailableStates);
            Assert.Equal(allStates, _vm.FilteredStates);
        }

        [Fact]
        public async Task Move_from_filtered_state()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.FilteredStates);

            _vm.SelectedFilteredState = "C";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", }, StateFiltering.FilteredStates);

            _vm.SelectedFilteredState = "A";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B", "C", "A" }, _vm.AvailableStates);
            Assert.Equal(new string[] { }, _vm.FilteredStates);
            Assert.Equal(new string[] { }, StateFiltering.FilteredStates);
        }

        [Fact]
        public async Task Move_from_filtered_state_persists()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveStateFromFiltered.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.FilteredStates);

            await Reactivate();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.FilteredStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.FilteredStates);
        }


        [Fact]
        public async Task Move_to_reset_state_when_none_selected()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            await _vm.MoveStateToReset.Execute().ToTask();

            Assert.Equal(allStates, _vm.AvailableStates);
            Assert.Empty(_vm.ResetStates);
        }

        [Fact]
        public async Task Move_to_reset_state()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            _vm.SelectedAvailableState = "A";
            await _vm.MoveStateToReset.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A" }, _vm.ResetStates);
            Assert.Equal(new[] { "A" }, StateFiltering.ResetStates);

            _vm.SelectedAvailableState = "C";
            await _vm.MoveStateToReset.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.ResetStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.ResetStates);

            _vm.SelectedAvailableState = "B";
            await _vm.MoveStateToReset.Execute().ToTask();

            Assert.Empty(_vm.AvailableStates);
            Assert.Equal(new[] { "A", "C", "B" }, _vm.ResetStates);
            Assert.Equal(new[] { "A", "C", "B" }, StateFiltering.ResetStates);
        }

        [Fact]
        public async Task Move_to_reset_state_persists()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;

            await Reactivate();

            _vm.SelectedAvailableState = "A";
            await _vm.MoveStateToReset.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A" }, _vm.ResetStates);
            Assert.Equal(new[] { "A" }, StateFiltering.ResetStates);

            await Reactivate();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A" }, _vm.ResetStates);
            Assert.Equal(new[] { "A" }, StateFiltering.ResetStates);
        }

        [Fact]
        public async Task Move_from_reset_state_when_none_selected()
        {
            var allStates = new[] { "A", "B", "C" };
            _statesRepository.SetResetStates(allStates);

            await Reactivate();

            await _vm.MoveStateFromReset.Execute().ToTask();

            Assert.Empty(_vm.AvailableStates);
            Assert.Equal(allStates, _vm.ResetStates);
        }

        [Fact]
        public async Task Move_from_reset_state()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetResetStates(allStates);

            await Reactivate();

            _vm.SelectedResetState = "B";

            await _vm.MoveStateFromReset.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.ResetStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.ResetStates);

            _vm.SelectedResetState = "C";

            await _vm.MoveStateFromReset.Execute().ToTask();

            Assert.Equal(new[] { "B", "C" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", }, _vm.ResetStates);
            Assert.Equal(new[] { "A", }, StateFiltering.ResetStates);

            _vm.SelectedResetState = "A";

            await _vm.MoveStateFromReset.Execute().ToTask();

            Assert.Equal(new[] { "B", "C", "A" }, _vm.AvailableStates);
            Assert.Equal(new string[] { }, _vm.ResetStates);
            Assert.Equal(new string[] { }, StateFiltering.ResetStates);
        }

        [Fact]
        public async Task Move_from_reset_state_persists()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetResetStates(allStates);

            await Reactivate();

            _vm.SelectedResetState = "B";

            await _vm.MoveStateFromReset.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.ResetStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.ResetStates);

            await Reactivate();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.ResetStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.ResetStates);
        }

        [Fact]
        public async Task Selected_Reset_state_can_change_when_moving()
        {
            var allStates = new[] { "A", "B", "C" };
            _statesRepository.SetResetStates(allStates);

            // selection changes when item is removed from collection
            _vm.FilteredStates.CollectionChanged += (_, __) => { _vm.SelectedResetState = null; };

            await Reactivate();

            _vm.SelectedResetState = "B";

            await _vm.MoveStateFromReset.Execute().ToTask();

            Assert.Equal(new[] { "B" }, _vm.AvailableStates);
            Assert.Equal(new[] { "A", "C" }, _vm.ResetStates);
            Assert.Equal(new[] { "A", "C" }, StateFiltering.ResetStates);
        }

        [Fact]
        public async Task Move_filtered_state_lower()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveFilteredStateLower.Execute().ToTask();

            Assert.Equal(new[] { "B", "A", "C" }, _vm.FilteredStates);

            _vm.SelectedFilteredState = "A";

            await _vm.MoveFilteredStateLower.Execute().ToTask();

            Assert.Equal(new[] { "A", "B", "C" }, _vm.FilteredStates);

            _vm.SelectedFilteredState = "C";

            await _vm.MoveFilteredStateLower.Execute().ToTask();

            Assert.Equal(new[] { "A", "C", "B" }, _vm.FilteredStates);

            await _vm.MoveFilteredStateLower.Execute().ToTask();

            Assert.Equal(new[] { "C", "A", "B" }, _vm.FilteredStates);

            await _vm.MoveFilteredStateLower.Execute().ToTask();

            Assert.Equal(new[] { "C", "A", "B" }, _vm.FilteredStates);
        }

        [Fact]
        public async Task Move_filtered_state_lower_none_selected()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = null;

            await _vm.MoveFilteredStateLower.Execute().ToTask();

            Assert.Equal(new[] { "A", "B", "C" }, _vm.FilteredStates);
        }

        [Fact]
        public async Task Move_filtered_state_lower_persists()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveFilteredStateLower.Execute().ToTask();

            Assert.Equal(new[] { "B", "A", "C" }, _vm.FilteredStates);

            await Reactivate();

            Assert.Equal(new[] { "B", "A", "C" }, _vm.FilteredStates);
        }

        [Fact]
        public async Task Move_filtered_higher_lower()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveFilteredStateHigher.Execute().ToTask();

            Assert.Equal(new[] { "A", "C", "B" }, _vm.FilteredStates);

            _vm.SelectedFilteredState = "A";

            await _vm.MoveFilteredStateHigher.Execute().ToTask();

            Assert.Equal(new[] { "C", "A", "B" }, _vm.FilteredStates);

            _vm.SelectedFilteredState = "C";

            await _vm.MoveFilteredStateHigher.Execute().ToTask();

            Assert.Equal(new[] { "A", "C", "B" }, _vm.FilteredStates);

            await _vm.MoveFilteredStateHigher.Execute().ToTask();

            Assert.Equal(new[] { "A", "B", "C" }, _vm.FilteredStates);

            await _vm.MoveFilteredStateHigher.Execute().ToTask();

            Assert.Equal(new[] { "A", "B", "C" }, _vm.FilteredStates);
        }

        [Fact]
        public async Task Move_filtered_state_higher_none_selected()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = null;

            await _vm.MoveFilteredStateHigher.Execute().ToTask();

            Assert.Equal(new[] { "A", "B", "C" }, _vm.FilteredStates);
        }

        [Fact]
        public async Task Move_filtered_state_higher_persists()
        {
            var allStates = new[] { "A", "B", "C" };
            _jiraCacheAdapter.AllStates = allStates;
            _statesRepository.SetFilteredStates(allStates);

            await Reactivate();

            _vm.SelectedFilteredState = "B";

            await _vm.MoveFilteredStateHigher.Execute().ToTask();

            Assert.Equal(new[] { "A", "C", "B" }, _vm.FilteredStates);

            await Reactivate();

            Assert.Equal(new[] { "A", "C", "B" }, _vm.FilteredStates);
        }
    }
}
