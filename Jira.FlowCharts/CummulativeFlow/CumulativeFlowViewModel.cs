using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Jira.FlowCharts
{

    public class CumulativeFlowViewModel : Screen
    {
        private readonly TasksSource _source;
        private readonly IStateFilteringProvider _stateFilteringProvider;
        private readonly ICurrentProject _currentProject;

        public CumulativeFlowViewModel(TasksSource source, IStateFilteringProvider stateFilteringProvider, ICurrentProject currentProject)
        {
            _source = source;
            _stateFilteringProvider = stateFilteringProvider;
            _currentProject = currentProject;
            DisplayName = "Cumulative flow";

            SeriesCollection = new SeriesCollection();
            XFormatter = val => new DateTime((long)val).ToShortDateString();
        }

        public SeriesCollection SeriesCollection { get; private set; }
        public Func<double, string> XFormatter { get; private set; }

        protected override async Task OnActivateAsync(CancellationToken token)
        {
            var stateFiltering = await _stateFilteringProvider.GetStateFilteringParameter();
            var stories = (await _source.GetStories(_currentProject.ProjectKey, stateFiltering)).ToArray();
            var fromDate = stories.Where(x=>x.Ended.HasValue).Max(x=>x.Ended.Value).AddMonths(-3);
            var cfa = new CumulativeFlowAnalysis(stories, stateFiltering.FilteredStates, fromDate);

            SeriesCollection.Clear();

            foreach (var state in cfa.States)
            {
                SeriesCollection.Add(new StackedAreaSeries
                {
                    Title = state,
                    Values = new ChartValues<DateTimePoint>(),
                    LineSmoothness = 0
                });
            }

            foreach (var change in cfa.Changes)
            {
                for (int i = 0; i < cfa.States.Length; i++)
                {
                    SeriesCollection[i].Values.Add(new DateTimePoint(change.Date, change.StateCounts[i]));
                }
            }
        }
    }
}